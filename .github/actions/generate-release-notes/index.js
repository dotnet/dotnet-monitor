const actionUtils = require('../action-utils.js');
const path = require('path');

const UpdateReleaseNotesLabel = "update-release-notes";
const BackportLabel = "backport";

async function run() {
    const [core, github] = await actionUtils.installAndRequirePackages("@actions/core", "@actions/github");

    const octokit = github.getOctokit(core.getInput("auth_token", { required: true }));

    const output = core.getInput("output", { required: true });
    const buildDescription = core.getInput("build_description", { required: true });
    const lastReleaseDate = core.getInput("last_release_date", { required: true });
    const branch = core.getInput("branch_name", { required: true });

    const repoOwner = github.context.payload.repository.owner.login;
    const repoName = github.context.payload.repository.name;

    try {
        const significantLabels = [
            {
                moniker: "⚠️",
                labelName: "breaking-change",
                description: "indicates a breaking change",
                inChangelog: false
            },
            {
                moniker: "🔬",
                labelName: "experimental-feature",
                description: "indicates an experimental feature",
                inChangelog: false
            }
        ];

        const parsedLastReleaseDate = new Date(lastReleaseDate);
        const jsISODateRepresentation = parsedLastReleaseDate.toISOString();

        const changelog = await generateChangelog(octokit, branch, repoOwner, repoName, jsISODateRepresentation, significantLabels);
        const monikerDescriptions = generateMonikerDescriptions(significantLabels);

        const releaseNotes = await generateReleaseNotes(path.join(__dirname, "releaseNotes.template.md"), buildDescription, changelog, monikerDescriptions);
        await actionUtils.writeFile(output, releaseNotes);
    } catch (error) {
        core.setFailed(error);
    }
}

function generateMonikerDescriptions(significantLabels) {
    let descriptions = [];
    for (const label of significantLabels) {
        if (label.inChangelog !== true) {
            continue;
        }

        descriptions.push(`\\*${label.moniker} **_${label.description}_**`);
    }

    return descriptions.join(" \\\n");
}

async function getPrsToMention(octokit, branch, repoOwner, repoName, minMergeDate) {
    // Identify potential PRs to mention the release notes.
    let candidatePrs = await getPRs(octokit, repoOwner, repoName, minMergeDate, UpdateReleaseNotesLabel);

    // Resolve the backport PRs to their origin PRs
    const maxRecursion = 3;
    const backportPrs = await getPRs(octokit, repoOwner, repoName, minMergeDate, BackportLabel);
    for (const pr of backportPrs) {
        const originPr = await resolveBackportPrToReleaseNotePr(octokit, pr, repoOwner, repoName, minMergeDate, maxRecursion);
        if (originPr !== undefined) {
            // Patch the origin PR information to have the backport PR number and URL
            // so that the release notes links to the backport, but grabs the rest of
            // the information from the origin PR.
            originPr.originNumber = originPr.number;
            originPr.number = pr.number;
            originPr.html_url = pr.html_url;

            candidatePrs.push(originPr);
        }
    }

    // Create a lookup table for every commit hash this release includes.
    const commitObjects = await octokit.paginate(octokit.rest.repos.listCommits, {
        owner: repoOwner,
        repo: repoName,
        sha: branch, // To filter by branch, set the sha field to the branch name.
        since: minMergeDate
    });

    let commitHashesInRelease = new Set();
    for (const commit of commitObjects) {
        commitHashesInRelease.add(commit.sha);
    }

    // Keep track of all of the prs we mention to avoid duplicates from resolved backports.
    let mentionedOriginNumbers = new Set();

    let prs = [];
    for (const pr of candidatePrs) {
        // Get a fully-qualified version of the pr that has all of the relevant information,
        // including the merge/squash/rebase commit.
        const fqPr = (await octokit.rest.pulls.get({
            owner: repoOwner,
            repo: repoName,
            pull_number: pr.number
        }))?.data;

        let originNumber = pr.originNumber ?? pr.number;
        if (commitHashesInRelease.has(fqPr.merge_commit_sha) && !mentionedOriginNumbers.has(originNumber)) {
            console.log(`Including: #${fqPr.number} -- origin:#${originNumber}`);
            mentionedOriginNumbers.add(originNumber);
            prs.push(pr);
        } else {
            console.log(`Skipping: #${fqPr.number} --- ${fqPr.merge_commit_sha}`);
        }
    }

    return prs;
}

async function generateChangelog(octokit, branch, repoOwner, repoName, minMergeDate, significantLabels) {
    const prs = await getPrsToMention(octokit, branch, repoOwner, repoName, minMergeDate);

    let changelog = [];
    for (const pr of prs) {
        let labelIndicesSeen = [];

        for (const label of pr.labels)
        {
            for(let i = 0; i < significantLabels.length; i++){
                if (label.name === significantLabels[i].labelName) {
                    significantLabels[i].inChangelog = true;
                    labelIndicesSeen.push(i);
                    break;
                }
            }
        }

        let entry = "-";
        for (const index of labelIndicesSeen) {
            entry += ` ${significantLabels[index].moniker}`;
        }

        const changelogRegex=/^###### Release Notes Entry\s+(?<releaseNotesEntry>.*)/m
        const userDefinedChangelogEntry = pr.body?.match(changelogRegex)?.groups?.releaseNotesEntry?.trim();
        if (userDefinedChangelogEntry !== undefined && userDefinedChangelogEntry.length !== 0) {
            entry += ` ${userDefinedChangelogEntry}`
        } else {
            entry += ` ${pr.title}`
        }

        entry += ` ([#${pr.number}](${pr.html_url}))`

        changelog.push(entry);
    }

    if (changelog.length === 0) {
        changelog.push("- Updated dependencies");
    }

    return changelog.join("\n");
}

async function generateReleaseNotes(templatePath, buildDescription, changelog, monikerDescriptions) {
    let releaseNotes = await actionUtils.readFile(templatePath);
    releaseNotes = releaseNotes.replace("${buildDescription}", buildDescription);
    releaseNotes = releaseNotes.replace("${changelog}", changelog);
    releaseNotes = releaseNotes.replace("${monikerDescriptions}", monikerDescriptions);

    return releaseNotes.trim();
}

async function getPRs(octokit, repoOwner, repoName, minMergeDate, labelFilter) {
    let searchQuery = `is:pr is:merged label:${labelFilter} repo:${repoOwner}/${repoName} merged:>=${minMergeDate}`;
    console.log(searchQuery);

    return await octokit.paginate(octokit.rest.search.issuesAndPullRequests, {
        q: searchQuery,
        sort: "created",
        order: "desc"
    });
}

async function resolveBackportPrToReleaseNotePr(octokit, pr, repoOwner, repoName, minMergeDate, maxRecursion) {
    const backportRegex=/backport (of )?#(?<prNumber>\d+)/mi
    const backportOriginPrNumber = pr.body?.match(backportRegex)?.groups?.prNumber;
    if (backportOriginPrNumber === undefined) {
        console.log(`Unable to determine origin PR for backport: ${pr.html_url}`)
        return undefined;
    }

    const originPr = (await octokit.rest.pulls.get({
        owner: repoOwner,
        repo: repoName,
        pull_number: backportOriginPrNumber
    }))?.data;

    if (originPr === undefined) {
        console.log(`Unable to find origin PR for backport: ${pr.html_url}`);
        return undefined;
    }

    console.log(`Mapped PR #${pr.number} as a backport of #${originPr.number}`)

    let originIsBackport = false;
    for (const label of originPr.labels)
    {
        if (label.name === UpdateReleaseNotesLabel) {
            console.log(`--> Potentially mentioning in release notes`)
            return originPr;
        }

        if (label.name === BackportLabel) {
            originIsBackport = true;
            // Keep searching incase there is also an update-release-notes label
        }
    }

    if (originIsBackport) {
        if (maxRecursion > 0) {
            return await resolveBackportPrToReleaseNotePr(octokit, originPr, repoOwner, repoName, minMergeDate, maxRecursion - 1);
        }
    }

    return undefined;
}

run();

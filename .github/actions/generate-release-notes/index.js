const fs = require('fs');
const path = require('path');
const util = require('util');
const jsExec = util.promisify(require("child_process").exec);
const readFile = (fileName) => util.promisify(fs.readFile)(fileName, 'utf8');
const writeFile = (fileName, contents) => util.promisify(fs.writeFile)(fileName, contents);

const UpdateReleaseNotesLabel = "update-release-notes";
const BackportLabel = "backport";

async function run() {
    console.log("Installing npm dependencies");
    const { stdout, stderr } = await jsExec("npm install @actions/core @actions/github");
    console.log("npm-install stderr:\n\n" + stderr);
    console.log("npm-install stdout:\n\n" + stdout);
    console.log("Finished installing npm dependencies");

    const github = require('@actions/github');
    const core = require('@actions/core');

    const octokit = github.getOctokit(core.getInput("auth_token", { required: true }));

    const output = core.getInput("output", { required: true });
    const buildDescription = core.getInput("build_description", { required: true });
    const lastReleaseDate = core.getInput("last_release_date", { required: true });

    const repoOwner = github.context.payload.repository.owner.login;
    const repoName = github.context.payload.repository.name;
    const branch = process.env.GITHUB_REF_NAME;

    try {
        const changelog = await generateChangelog(octokit, branch, repoOwner, repoName, lastReleaseDate,
            [
                {
                    labelName: "breaking-change",
                    moniker: "⚠️"
                },
                {
                    labelName: "experimental-feature",
                    moniker: "🔬"
                }
            ]);

            const releaseNotes = await generateReleaseNotes(path.join(__dirname, "releaseNotes.template.md"), buildDescription, changelog);
            await writeFile(output, releaseNotes);
    } catch (error) {
        core.setFailed(error);
    }
}

async function generateReleaseNotes(templatePath, buildDescription, changelog) {
    let releaseNotes = await readFile(templatePath);
    releaseNotes = releaseNotes.replace("${buildDescription}", buildDescription);
    releaseNotes = releaseNotes.replace("${changelog}", changelog);

    return releaseNotes;
}

async function getPRs(octokit, branchName, repoOwner, repoName, minMergeDate, labelFilter) {
    const searchQuery = `is:pr is:merged label:${labelFilter} repo:${repoOwner}/${repoName} base:${branchName} merged:>=${minMergeDate}`;
    console.log(searchQuery);

    return await octokit.paginate(octokit.rest.search.issuesAndPullRequests, {
        q: searchQuery,
        sort: "created",
        order: "desc"
    });
}

async function generateChangelog(octokit, branchName, repoOwner, repoName, minMergeDate, significantLabels) {
    let prs = await getPRs(octokit, branchName, repoOwner, repoName, minMergeDate, UpdateReleaseNotesLabel);

    // Resolve the backport PRs for their origin PRs
    const maxRecursion = 3;
    const backportPrs = await getPRs(octokit, branchName, repoOwner, repoName, minMergeDate, BackportLabel);
    for (const pr of backportPrs) {
        const originPr = await resolveBackportPrToReleaseNotePr(octokit, pr, repoOwner, repoName, minMergeDate, maxRecursion);
        if (originPr !== undefined) {
            prs.push(originPr);
        }
    }

    let changelog = [];
    for (const pr of prs) {
        let labelIndicesSeen = [];

        for (const label of pr.labels)
        {
            for(let i = 0; i < significantLabels.length; i++){
                if (label.name === significantLabels[i].labelName) {
                    labelIndicesSeen.push(i);
                    break;
                }
            }
        }

        let entry = "-";
        for (const index of labelIndicesSeen) {
            entry += ` ${significantLabels[index].moniker}`;
        }

        const changelogRegex=/^###### Release Notes Entry\r?\n(?<releaseNotesEntry>.*)/m
        const userDefinedChangelogEntry = pr.body?.match(changelogRegex)?.groups?.releaseNotesEntry;
        if (userDefinedChangelogEntry !== undefined) {
            entry += ` ${userDefinedChangelogEntry}`
        } else {
            entry += ` ${pr.title}`
        }

        entry += ` ([#${pr.number}](${pr.html_url}))`

        changelog.push(entry);
    }

    return changelog.join("\n");
}

async function resolveBackportPrToReleaseNotePr(octokit, pr, repoOwner, repoName, minMergeDate, maxRecursion) {
    const backportRegex=/^Backport of #(?<prNumber>\d+) to/m
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
            console.log(`--> Mentioning in release notes`)
            return originPr;
        }

        if (label.name === BackportLabel) {
            originIsBackport = true;
            // Keep searching incase there is also an update-release-notes label
        }
    }

    if (originIsBackport) {
        if (maxRecursion > 0) {
            return await resolveBackportPr(octokit, originPr, repoOwner, repoName, minMergeDate, maxRecursion - 1);
        }
    }

    return undefined;
}

run();

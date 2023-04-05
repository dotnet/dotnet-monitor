const actionUtils = require('../action-utils.js');
const path = require('path');

async function run() {
    const [core, github] = await actionUtils.installAndRequirePackages("@actions/core", "@actions/github");

    const releasesDataFile = core.getInput("releases_json_file", { required: true });

    let octokit = undefined;
    {
        const auth_token = core.getInput("auth_token", { required: false });
        if (auth_token !== undefined && auth_token.length > 0) {
            octokit = github.getOctokit(auth_token);
        }
    }

    const endOfSupportDiscussionCategory = core.getInput("end_of_support_discussion_category", { required: false });
    const supportedFrameworks = core.getInput("supported_frameworks", { required: false });

    const repoOwner = github.context.payload.repository.owner.login;
    const repoName = github.context.payload.repository.name;

    const releasePayload = github.context.payload.release;

    try {
        const releasesData = JSON.parse(await actionUtils.readFile(releasesDataFile));

        if (releasePayload !== undefined) {
            const deprecatedRelease = addNewReleaseAndDeprecatePriorVersion(releasePayload, supportedFrameworks, releasesData);
            if (endOfSupportDiscussionCategory !== undefined && endOfSupportDiscussionCategory.length > 0 && deprecatedRelease !== undefined) {
                _ = await tryToAnnounceVersionHasEndOfSupport(core, octokit, endOfSupportDiscussionCategory, repoName, repoOwner, deprecatedRelease);
            }

        }

        cleanupPreviewVersions(releasesData);
        cleanupSupportedVersions(releasesData);
        cleanupUnsupportedVersions(releasesData);

        // Save to disk.
        await actionUtils.writeFile(releasesDataFile, JSON.stringify(releasesData, null, 2));
    } catch (error) {
        core.setFailed(error);
    }
}

function cleanupPreviewVersions(releasesData) {
    let versionsStillInPreview = [];

    for (const releaseKey of releasesData.preview) {
        const releaseData = releasesData.releases[releaseKey];
        const [_, __, ___, iteration] = actionUtils.splitVersionTag(releaseData.tag);
        if (iteration !== undefined) {
            versionsStillInPreview.push(releaseKey);
        }
    }

    releasesData.preview = versionsStillInPreview;
}

function cleanupSupportedVersions(releasesData) {
    const currentDate = new Date();
    let stillSupportedVersion = [];

    for (const releaseKey of releasesData.supported) {
        const release = releasesData.releases[releaseKey];
        if (release.outOfSupportDate === undefined) {
            stillSupportedVersion.push(releaseKey);
            continue;
        }

        const endOfSupportDate = new Date(release.outOfSupportDate);
        if (currentDate >= endOfSupportDate) {
            releasesData.unsupported.unshift(releaseKey);
        } else {
            stillSupportedVersion.push(releaseKey);
        }
    }

    releasesData.supported = stillSupportedVersion;
}

function cleanupUnsupportedVersions(releasesData) {
    const currentDate = new Date();
    let versionsToStillMention = [];

    for (const releaseKey of releasesData.unsupported) {
        const release = releasesData.releases[releaseKey];

        const dateToNoLongerMention = new Date(release.outOfSupportDate);
        dateToNoLongerMention.setMonth(dateToNoLongerMention.getMonth() + releasesData.policy.cleanupUnsupportedReleasesAfterMonths);

        if (currentDate >= dateToNoLongerMention) {
            delete releasesData.releases[releaseKey];
        } else {
            versionsToStillMention.push(releaseKey);
        }
    }

    releasesData.unsupported = versionsToStillMention;
}

// Returns the release that is now out-of-support, if any.
function addNewReleaseAndDeprecatePriorVersion(releasePayload, supportedFrameworks, releasesData) {
    const releaseDate = new Date(releasePayload.published_at);
    // To keep things simple mark the release date as midnight.
    releaseDate.setHours(0, 0, 0, 0);

    const [majorVersion, minorVersion, patchVersion, iteration] = actionUtils.splitVersionTag(releasePayload.tag_name);

    const releaseMajorMinorVersion = `${majorVersion}.${minorVersion}`;

    // See if we're updating a release
    let existingRelease = releasesData.releases[releaseMajorMinorVersion];

    // Check if we're promoting a preview to RTM, if so re-create everything
    if (existingRelease !== undefined && iteration === undefined) {
        const [_, __, ___, existingIteration] = actionUtils.splitVersionTag(existingRelease.tag);
        if (existingIteration !== undefined) {
            existingRelease = undefined;
        }
    }

    const newRelease = {
        tag: releasePayload.tag_name,
        minorReleaseDate: releaseDate.toISOString(),
        patchReleaseDate: releaseDate.toISOString(),
        supportedFrameworks: supportedFrameworks.split(' ')
    };

    if (existingRelease === undefined) {
        if (iteration === undefined) {
            releasesData.supported.unshift(releaseMajorMinorVersion);
        } else {
            releasesData.preview.unshift(releaseMajorMinorVersion);
        }
    } else if (iteration !== undefined) {
        newRelease.minorReleaseDate = existingRelease.minorReleaseDate;
    }

    releasesData.releases[releaseMajorMinorVersion] = newRelease;

    // Check if we're going to be putting a version out-of-support.
    if (minorVersion > 0 && patchVersion === 0 && iteration === undefined) {
        const endOfSupportDate = new Date(releaseDate.valueOf());
        endOfSupportDate.setMonth(endOfSupportDate.getMonth() + releasesData.policy.additionalMonthsOfSupportOnNewMinorRelease);

        const previousMinorReleaseKey = `${majorVersion}.${minorVersion-1}`;
        releasesData.releases[previousMinorReleaseKey].outOfSupportDate = endOfSupportDate;
        return releasesData.releases[previousMinorReleaseKey];
    }

    return undefined;
}

async function tryToAnnounceVersionHasEndOfSupport(core, octokit, category, repoName, repoOwner, version) {
    try {
        // There's currently no REST API for creating a discussion,
        // however we can use the GraphQL API to do so.

        //
        // Get the repository id and map the category name to an id.
        //
        // Don't bother with pagination yet , we're only looking for a single category
        // and there are only a few registered.
        //
        const result = await octokit.graphql(`
        query ($repoName: String!, $owner: String!) {
            repository(name: $repoName, owner: $owner) {
            id
            discussionCategories (first: 15) {
                edges {
                node {
                    id
                    name
                }
                }
            }
            }
        }`,
        {
            owner: repoOwner,
            repoName: repoName
        });

        const repositoryId = result.repository.id;
        let categoryId = undefined;

        for (const edge of result.repository.discussionCategories.edges) {
            if (edge.node.name === category) {
                categoryId = edge.node.id;
                break;
            }
        }

        if (categoryId === undefined) {
            throw new Error(`Unable to determine category id for category ${category}`);
        }

        let discussionBody = await actionUtils.readFile(path.join(__dirname, "end_of_support_discussion.template.md"));
        const [major, minor] = actionUtils.splitVersionTag(version.tag);
        const friendlyDate = actionUtils.friendlyDateFromISODate(version.outOfSupportDate);

        const title =  `${major}.${minor}.X End of Support On ${friendlyDate}`;

        discussionBody = discussionBody.replace("${endOfSupportDate}", friendlyDate);
        discussionBody = discussionBody.replace("${majorMinorVersion}", `${major}.${minor}`); // todo: strio

        // https://docs.github.com/en/graphql/reference/mutations#creatediscussion
        // https://docs.github.com/en/graphql/reference/input-objects#creatediscussioninput
        const createDiscussionResult = await octokit.graphql(`
            mutation CreateDiscussion($repositoryId: ID!, $title: String!, $body: String!, $categoryId: ID!) {
                createDiscussion(input: {
                    repositoryId: $repositoryId,
                    categoryId: $categoryId,
                    title: $title,
                    body: $body
                }) {
                    discussion {
                        url
                    }
                }
            }`,
        {
            repositoryId: repositoryId,
            categoryId: categoryId,
            title: title,
            body: discussionBody
        });

        const discussionUrl = createDiscussionResult.createDiscussion.discussion.url;
        core.notice(`Created discussion at: ${discussionUrl}`);

        return discussionUrl;
    } catch (error) {
        core.warning(`Unable to create discussion: ${error}`);
    }

    return undefined;
}

run();

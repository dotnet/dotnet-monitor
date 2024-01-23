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
            const deprecatedRelease = addNewReleaseVersion(releasePayload, supportedFrameworks, releasesData);
            if (deprecatedRelease !== undefined && octokit !== undefined && endOfSupportDiscussionCategory !== undefined && endOfSupportDiscussionCategory.length > 0) {
                _ = await tryToAnnounceVersionHasEndOfSupport(core, octokit, endOfSupportDiscussionCategory, repoName, repoOwner, deprecatedRelease);
            }
        }

        regenerateCategories(releasesData);

        await actionUtils.writeFile(releasesDataFile, JSON.stringify(releasesData, null, 2));
    } catch (error) {
        core.setFailed(error);
    }
}

// Returns the release that is now out-of-support, if any.
function addNewReleaseVersion(releasePayload, supportedFrameworks, releasesData) {
    const releaseDate = new Date(releasePayload.published_at);
    // To keep things simple mark the release date as midnight.
    releaseDate.setHours(0, 0, 0, 0);

    const [majorVersion, minorVersion, patchVersion, iteration] = actionUtils.splitVersionTag(releasePayload.tag_name);
    const releaseMajorMinorVersion = `${majorVersion}.${minorVersion}`;
    const isRTMRelease = (iteration === undefined);

    // See if we're updating a release
    let existingRelease = releasesData.releases[releaseMajorMinorVersion];

    const newRelease = {
        tag: releasePayload.tag_name,
        minorReleaseDate: releaseDate.toISOString(),
        patchReleaseDate: releaseDate.toISOString(),
        supportedFrameworks: supportedFrameworks.split(' ')
    };

    // Preserve the original minor release date and out-of-support date for RTM releases
    if (existingRelease !== undefined &&
        actionUtils.isVersionTagRTM(existingRelease.tag) &&
        isRTMRelease) {

        newRelease.minorReleaseDate = existingRelease.minorReleaseDate;
        newRelease.outOfSupportDate = existingRelease.outOfSupportDate;
    }

    releasesData.releases[releaseMajorMinorVersion] = newRelease;

    // Check if we're going to be putting a version out-of-support.
    if (minorVersion > 0 && patchVersion === 0 && isRTMRelease) {
        const endOfSupportDate = new Date(releaseDate.valueOf());
        endOfSupportDate.setMonth(endOfSupportDate.getMonth() + releasesData.policy.additionalMonthsOfSupportOnNewMinorRelease);

        const previousMinorReleaseKey = `${majorVersion}.${minorVersion-1}`;
        releasesData.releases[previousMinorReleaseKey].outOfSupportDate = endOfSupportDate;
        return releasesData.releases[previousMinorReleaseKey];
    }

    return undefined;
}

function regenerateCategories(releasesData) {
    const currentDate = new Date();

    let previewVersions = [];
    let supportedVersions = [];
    let unsupportedVersions = [];
    let versionsToNoLongerMention = [];

    for (const releaseProperty in releasesData.releases) {
        const release = releasesData.releases[releaseProperty];

        const isRTM = actionUtils.isVersionTagRTM(release.tag);
        if (!isRTM) {
            // It's a preview
            previewVersions.push(releaseProperty);
        } else if (release.outOfSupportDate === undefined) {
            supportedVersions.push(releaseProperty);
        } else {
            const outOfSupportDate = new Date(release.outOfSupportDate);
            const dateToNoLongerMention = new Date(release.outOfSupportDate);
            dateToNoLongerMention.setMonth(dateToNoLongerMention.getMonth() + releasesData.policy.cleanupUnsupportedReleasesAfterMonths);

            if (currentDate >= dateToNoLongerMention) {
                versionsToNoLongerMention.push(releaseProperty);
            } else if (currentDate >= outOfSupportDate) {
                unsupportedVersions.push(releaseProperty);
            } else {
                supportedVersions.push(releaseProperty);
            }
        }
    }

    for (const releaseKey of versionsToNoLongerMention) {
        delete releasesData.releases[releaseKey];
    }

    // Sort the releases by their major.minor version
    previewVersions.sort(compareVersionKeys);
    supportedVersions.sort(compareVersionKeys);
    unsupportedVersions.sort(compareVersionKeys);

    releasesData.preview = previewVersions;
    releasesData.supported = supportedVersions;
    releasesData.unsupported = unsupportedVersions;
}

// Compares two version keys (major.minor)
function compareVersionKeys(a, b) {
    const aSegments = a.split('.');
    const bSegments = b.split('.');

    if (aSegments.length !== bSegments.length) {
        throw new Error(`Version keys have a different number of segments: ${a} ${b}`)
    }

    for (let i = 0; i < aSegments.length; i++) {
        const aSegmentValue = Number(aSegments[i]);
        const bSegmentValue = Number(bSegments[i]);

        if (isNaN(aSegmentValue) || isNaN(bSegmentValue)) {
            throw new Error(`Unexpected version keys: ${a} ${b}`)
        }

        if (aSegmentValue !== bSegmentValue) {
            return bSegmentValue - aSegmentValue;
        }
    }

    return 0;
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
        // Instead just limit the results to the first 10 (arbitrarily chosen)
        // as we currently have ~5 discussion categories.
        //
        const result = await octokit.graphql(`
        query ($repoName: String!, $owner: String!) {
            repository(name: $repoName, owner: $owner) {
            id
            discussionCategories (first: 10) {
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

        const title = `${major}.${minor}.X End of Support On ${friendlyDate}`;

        discussionBody = discussionBody.replace("${endOfSupportDate}", friendlyDate);
        discussionBody = discussionBody.replace("${majorMinorVersion}", `${major}.${minor}`);

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

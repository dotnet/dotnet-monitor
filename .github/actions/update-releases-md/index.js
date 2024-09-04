const actionUtils = require('../action-utils.js');

async function run() {
    const [core, github] = await actionUtils.installAndRequirePackages("@actions/core", "@actions/github");

    const releasesDataFile = core.getInput("releases_json_file", { required: true });
    const outputFile = core.getInput("releases_md_file", { required: true });

    const repoOwner = github.context.payload.repository.owner.login;
    const repoName = github.context.payload.repository.name;

    try {
        const releasesData = JSON.parse(await actionUtils.readFile(releasesDataFile));

        const releasesMdContent = generateReleasesMdContent(releasesData, repoOwner, repoName);

        await actionUtils.writeFile(outputFile, releasesMdContent);
    } catch (error) {
        core.setFailed(error);
    }
}

function generateReleasesMdContent(releasesData, repoOwner, repoName) {
    let supportedReleasesTable = '';
    let previewReleasesTable = '';
    let outOfSupportReleasesTable = '';

    for (const releaseKey of releasesData.supported) {
        supportedReleasesTable += `${generateTableRow(releasesData.releases[releaseKey], repoOwner, repoName, true)}\n`;
    }

    for (const releaseKey of releasesData.unsupported) {
        outOfSupportReleasesTable += `${generateTableRow(releasesData.releases[releaseKey], repoOwner, repoName, true)}\n`;
    }

    for (const releaseKey of releasesData.preview) {
        previewReleasesTable += `${generateTableRow(releasesData.releases[releaseKey], repoOwner, repoName, false)}\n`;
    }

    let content =`# Releases

`;

    if (supportedReleasesTable.length > 0) {
        content += `## Supported versions\n\n${generateTableHeader(true)}\n${supportedReleasesTable}\n\n`;
    }

    if (outOfSupportReleasesTable.length > 0) {
        content += `## Out of support versions\n\n${generateTableHeader(true)}\n${outOfSupportReleasesTable}\n\n`;
    }

    if (previewReleasesTable.length > 0) {
        content += `## Preview versions\n\n${generateTableHeader(false)}\n${previewReleasesTable}\n\n`;
    }


    return content;
}

function generateTableHeader(rtmVersions) {
    let headers = ['Version'];
    if (rtmVersions) {
        headers.push('Original Release Date', 'Latest Patch Version', 'Patch Release Date', 'End of Support');
    } else {
        headers.push('Release Date');
        headers.push('Latest Version');
    }

    headers.push('Runtime Frameworks');

    let headerString = `${convertArrayIntoTableRow(headers)}\n`;

    const separators = Array(headers.length).fill('---');
    headerString += convertArrayIntoTableRow(separators);

    return headerString;
}

function generateTableRow(release, repoOwner, repoName, rtmVersions) {
    const [major, minor, patch, iteration, versionLabel] = actionUtils.splitVersionTag(release.tag);
    const htmlUrl = `https://github.com/${repoOwner}/${repoName}/releases/tag/${release.tag}`

    let fqVersion = `${major}.${minor}.${patch}`;
    if (iteration !== undefined) {
        fqVersion += ` ${versionLabel} ${iteration}`;
    }

    let columns = [
        `${major}.${minor}`,
        actionUtils.friendlyDateFromISODate(release.minorReleaseDate),
        `[${fqVersion}](${htmlUrl})`
    ];

    if (rtmVersions) {
        columns.push(actionUtils.friendlyDateFromISODate(release.patchReleaseDate));
        columns.push(actionUtils.friendlyDateFromISODate(release.outOfSupportDate));
    }

    columns.push(release.supportedFrameworks.join("<br/>"));

    return convertArrayIntoTableRow(columns);
}

function convertArrayIntoTableRow(array) {
    // Don't bother with escaping for now as we have a limited set of input that we control.
    // If we want to re-use this code for generating other tables this will need to change.
    return `| ${array.join(' | ')} |`;
}

run();

const util = require("util");
const fs = require("fs");
const jsExec = util.promisify(require("child_process").exec);

module.exports.installAndRequirePackages = async function(...newPackages)
{
    console.log("Installing npm dependency");
    const { stdout, stderr } = await jsExec(`npm install ${newPackages.join(' ')}`);
    console.log("npm-install stderr:\n\n" + stderr);
    console.log("npm-install stdout:\n\n" + stdout);
    console.log("Finished installing npm dependencies");

    let requiredPackages = [];
    for (const packageName of newPackages) {
        requiredPackages.push(require(packageName));
    }

    return requiredPackages;
}

function splitVersionTag(tag) {
    const regex = /v(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(-(?<versionLabel>[a-zA-Z]+)\.(?<iteration>\d+))?/;
    const releaseVersion = regex.exec(tag);
    if (releaseVersion == null) throw "Error: Unexpected tag format";

    return [
        Number(releaseVersion.groups.major),
        Number(releaseVersion.groups.minor),
        Number(releaseVersion.groups.patch),
        (releaseVersion.groups.iteration === undefined) ? undefined : Number(releaseVersion.groups.iteration),
        releaseVersion.groups.versionLabel
    ];
}

module.exports.isVersionTagRTM = function(tag) {
    const [_, __, ___, iteration] = splitVersionTag(tag);

    return iteration === undefined;
}

module.exports.friendlyDateFromISODate = function(isoDate) {
    if (isoDate === undefined) {
        return undefined;
    }

    return new Date(isoDate).toLocaleString(
        "en-us", {
            dateStyle: "long"
        });
}

module.exports.splitVersionTag = splitVersionTag;
module.exports.readFile = (fileName) => util.promisify(fs.readFile)(fileName, 'utf8');
module.exports.writeFile = (fileName, contents) => util.promisify(fs.writeFile)(fileName, contents);

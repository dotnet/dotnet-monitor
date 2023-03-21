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

module.exports.readFile = (fileName) => util.promisify(fs.readFile)(fileName, 'utf8');
module.exports.writeFile = (fileName, contents) => util.promisify(fs.writeFile)(fileName, contents);
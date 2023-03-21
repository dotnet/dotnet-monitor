const util = require("util");
const jsExec = util.promisify(require("child_process").exec);

export async function installAndRequirePackages(...newPackages)
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

export async function readFile(fileName) {
    return await util.promisify(fs.readFile)(fileName, 'utf8');
}

export async function writeFile(fileName, contents) {
    return await util.promisify(fs.writeFile)(fileName, contents);
}
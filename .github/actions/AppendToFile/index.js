const actionUtils = require('../action-utils.js');
const path = require('path')

async function main() {
    const [core] = await actionUtils.installAndRequirePackages("@actions/core");

    try {
        const textToSearch = core.getInput('textToSearch', { required: true });
        const textToAdd = core.getInput('textToAdd', { required: true });
        const paths = core.getInput('paths', {required: false});

        const insertFileNameParameter = "{insertFileName}";

        if (paths === null || paths.trim() === "")
        {
            return;
        }

        console.log("Paths: " + paths);

        for (const currPath of paths.split(',')) {
            const content = await actionUtils.readFile(currPath);
            if (content && !content.includes(textToSearch))
            {
                var updatedTextToAdd = textToAdd;
                if (textToAdd.includes(insertFileNameParameter))
                {
                    const parsedPath = path.parse(currPath);
                    const encodedURIWithoutExtension = encodeURIComponent(path.join(parsedPath.dir, parsedPath.name))
                    updatedTextToAdd = textToAdd.replace(insertFileNameParameter, encodedURIWithoutExtension);
                }

                var contentStr = updatedTextToAdd + "\n\n" + content.toString();

                await actionUtils.writeFile(currPath, contentStr);
            }
        }
    } catch (error) {
        core.setFailed(error.message);
    }
}

// Call the main function to run the action
main();

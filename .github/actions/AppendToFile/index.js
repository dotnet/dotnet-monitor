const core = require('@actions/core');
const github = require('@actions/github');
const fs = require('fs');

const main = async () => {
  try {
    const textToSearch = core.getInput('textToSearch', { required: true });
    const textToAdd = core.getInput('textToAdd', { required: true });
    const paths = core.getInput('paths', {required: false});

    const insertFileNameParameter = "{insertFileName}";

    if (paths !== null && paths.trim() !== "")
    {
        for (const path of paths.split(' ')) {

            fs.readFile(path, (err, content) => {
                if (err) throw err;
    
                if (!content.includes(textToSearch))
                {
                    var updatedTextToAdd = textToAdd;
                    if (textToAdd.includes(insertFileNameParameter))
                    {
                        var encodedURI = encodeURIComponent(path);
                        var encodedURIWIthoutExtension = encodedURI.substring(0, encodedURI.length - 3) // remove the .md at the end
                        updatedTextToAdd = textToAdd.replace(insertFileNameParameter, encodedURIWIthoutExtension);
                    }
    
                    var contentStr = content.toString();
    
                    contentStr = updatedTextToAdd + "\n\n" + contentStr;
    
                    fs.writeFile(path, contentStr, (err) => {
    
                    });
                }
            });
        }
    }
  } catch (error) {
    core.setFailed(error.message);
  }
}

// Call the main function to run the action
main();

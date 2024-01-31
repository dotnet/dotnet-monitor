const actionUtils = require('../action-utils.js');

const suggestionsSeparator = ',';
const oldNewLinkSeparator = ' -> ';
let modifiedFiles = [];

function AppendModifiedFiles(path)
{
  modifiedFiles.push(path)
  core.setOutput('modifiedFiles', modifiedFiles.join(' '))
}

function ReplaceOldWithNewText(content, oldText, newText)
{
  return content.replaceAll(oldText, newText);
}

const main = async () => {

  const [core] = await actionUtils.installAndRequirePackages("@actions/core");

  try {
    const learningPathDirectory = core.getInput('learningPathsDirectory', { required: true });
    const learningPathHashFile = core.getInput('learningPathHashFile', { required: true });
    const suggestions = core.getInput('suggestions', { required: false });
    const oldHash = core.getInput('oldHash', { required: true });
    const newHash = core.getInput('newHash', { required: true });

    actionUtils.writeFile(learningPathHashFile, newHash);
    AppendModifiedFiles(learningPathHashFile)

    // Scan each file in the learningPaths directory
    actionUtils.readdir(learningPathDirectory, (_, files) => {
      files.forEach(learningPathFile => {
        try {
          const fullPath = learningPathDirectory + "/" + learningPathFile
          const content = actionUtils.readFileSync(fullPath, "utf8")

          var replacedContent = content

          if (suggestions !== null && suggestions.trim() !== "") {
            const suggestionsArray = suggestions.split(suggestionsSeparator)
            suggestionsArray.forEach(suggestion => {
              const suggestionArray = suggestion.split(oldNewLinkSeparator)
              var oldLink = suggestionArray[0]
              var newLink = suggestionArray[1]
              oldLink = oldLink.substring(oldLink.indexOf('(') + 1, oldLink.lastIndexOf(')'))
              newLink = newLink.substring(newLink.indexOf('(') + 1, newLink.lastIndexOf(')'))
              replacedContent = ReplaceOldWithNewText(replacedContent, oldLink, newLink)
            })
          }

          replacedContent = ReplaceOldWithNewText(replacedContent, oldHash, newHash)

          actionUtils.writeFile(learningPathDirectory + "/" + learningPathFile, replacedContent);

          if (content !== replacedContent) {
            AppendModifiedFiles(fullPath)
          }
        } catch (error) {
          console.log("Error: " + error)
          console.log("Could not find learning path file: " + learningPathFile)
        }
      });
    });

  } catch (error) {
    core.setFailed(error.message);
  }
}

// Call the main function to run the action
main();

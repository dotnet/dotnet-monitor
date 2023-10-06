const actionUtils = require('../action-utils.js');
const mergePathPrefix = "merge/";
const headPathPrefix = "head/";
const linePrefix = "#L";
const sourceDirectoryName = core.getInput('sourceDirectoryName', { required: true });

modifiedFilesDict = {};
modifiedFilesUrlToFileName = {};

var manuallyReview = new Set();
var suggestions = new Set();

// Modified Files - Any files that have been modified in the PR that are present in a learning path
function UpdateModifiedFiles(fileName, path, learningPathFile)
{
  modifiedFilesUrlToFileName[path] = fileName;

  modifiedFilesDict[path] = modifiedFilesDict[path] ? modifiedFilesDict[path] : new Set();;
  modifiedFilesDict[path].add(learningPathFile);

  var modifiedFiles = new Set();
  for (currPath in modifiedFilesDict)
  {
    const fileName = modifiedFilesUrlToFileName[currPath];
    modifiedFiles.add(AssembleOutput(fileName, currPath, undefined, undefined, Array.from(modifiedFilesDict[currPath]).join(" ")));
  }

  SetOutput('modifiedFiles', modifiedFiles)
}

// Manually Review - The PR Author should manually review these files to determine if they need to be updated;
// this could be due to deletions, renames, or references to ambiguous lines (such as a newline) that cannot
// be uniquely identified.
function UpdateManuallyReview(fileName, path, learningPathFile, learningPathLineNumber, lineNumber = undefined)
{
  manuallyReview.add(AssembleOutput(fileName, path, lineNumber, undefined, learningPathFile, learningPathLineNumber))
  SetOutput('manuallyReview', manuallyReview)
}

// Suggestions - A line reference has changed in this PR, and the PR Author should update the line accordingly.
// There are edge cases where this may make an incorrect recommendation, so the PR author should verify that
// this is the correct line to reference.
function UpdateSuggestions(fileName, path, learningPathFile, learningPathLineNumber, oldLineNumber, newLineNumber)
{
  suggestions.add(AssembleOutput(fileName, path, oldLineNumber, newLineNumber, learningPathFile, learningPathLineNumber))
  SetOutput('suggestions', suggestions)
}

function SetOutput(outputName, outputSet)
{
  core.setOutput(outputName, Array.from(outputSet).join(","))
}

function AssembleOutput(fileName, path, oldLineNumber, newLineNumber, learningPathFile, learningPathLineNumber)
{
  var codeFileLink = "[" + fileName + "]" + "(" + path + ")"
  codeFileLink = AppendLineNumber(codeFileLink, oldLineNumber, newLineNumber)
  return codeFileLink + " | " + "**" + AppendLineNumber(learningPathFile, learningPathLineNumber, undefined) + "**"
}

function AppendLineNumber(text, oldLineNumber, newLineNumber)
{
  if (oldLineNumber === undefined) { return text }

  return text + " " + linePrefix + oldLineNumber + (newLineNumber === undefined ? "" : " --> " + linePrefix + newLineNumber)
}

function CheckForEndOfLink(str, startIndex)
{
  const illegalCharIndex = str.substr(startIndex).search("[(), '\`\"\}\{]|\. "); // This regex isn't perfect, but should cover most cases.
  return illegalCharIndex;
}

function CompareFiles(headLearningPathFileContentStr, repoURLToSearch, modifiedPRFiles, learningPathFile)
{
  // Get all indices where a link to the repo is found within the current learning path file
  var linkIndices = [];
  for(var pos = headLearningPathFileContentStr.indexOf(repoURLToSearch); pos !== -1; pos = headLearningPathFileContentStr.indexOf(repoURLToSearch, pos + 1)) {
      linkIndices.push(pos);
  }

  for(let startOfLink of linkIndices)
  {
    // Clean up the link, determine if it has a line number suffix
    const endOfLink = startOfLink + CheckForEndOfLink(headLearningPathFileContentStr, startOfLink)
    const link = headLearningPathFileContentStr.substring(startOfLink, endOfLink);

    const pathStartIndex = link.indexOf(sourceDirectoryName);
    if (pathStartIndex === -1) { continue }

    const linePrefixIndex = link.indexOf(linePrefix);
    const linkHasLineNumber = linePrefixIndex !== -1;
    const pathEndIndex = linkHasLineNumber ? linePrefixIndex : endOfLink;

    // Check if the file being referenced by the link is one of the modified files in the PR
    const filePath = link.substring(pathStartIndex, pathEndIndex);
    if (modifiedPRFiles.includes(filePath))
    {
      const fileName = filePath.substring(filePath.lastIndexOf('/') + 1);

      UpdateModifiedFiles(
        fileName,
        linkHasLineNumber ? link.substring(0, linePrefixIndex) : link,
        learningPathFile);

      // This is the line number in the learning path file that contains the link - not the #L line number in the link itself
      const learningPathLineNumber = headLearningPathFileContentStr.substring(0, startOfLink).split("\n").length;

      // Get the contents of the referenced file from head (old) and merge (new) to compare them
      var mergeContent = ""
      try {
        mergeContent = actionUtils.readFileSync(mergePathPrefix + filePath, "utf8")
      }
      catch (error) {
        // If the merge branch doesn't have the file, then it was deleted/renamed in the PR and should be manually reviewed
        UpdateManuallyReview(
          fileName,
          link,
          learningPathFile,
          learningPathLineNumber);
        continue
      }

      if (!linkHasLineNumber) { continue }

      var headContent = ""
      try {
        headContent = actionUtils.readFileSync(headPathPrefix + filePath, "utf8")
      }
      catch (error) { continue }

      const linkLineNumber = Number(link.substring(linePrefixIndex + linePrefix.length, link.length));

      const mergeContentLines = mergeContent.toString().split("\n");
      const headContentLines = headContent.toString().split("\n");

      if (headContentLines.length < linkLineNumber) // This shouldn't happen, unless the learning path is already out of date.
      {
        UpdateManuallyReview(
          fileName,
          link,
          learningPathFile,
          learningPathLineNumber,
          linkLineNumber);
      }
      // If the referenced line in the merge branch is identical to the line in the head branch, then the line number is still considered correct.
      // Note that this can miss cases with ambiguous code that happens to align - this is a limitation of the heuristic. Learning Path authors
      // are encouraged to choose lines of code that are unique (e.g. not a newline, open brace, etc.)
      else if (mergeContentLines.length < linkLineNumber || headContentLines[linkLineNumber - 1].trim() !== mergeContentLines[linkLineNumber - 1].trim())
      {
        // Check for multiple instances of the referenced line in the file - if there are multiple, then we don't know
        // which one to reference, so we'll ask the PR author to manually review the file.
        const lastIndex = mergeContentLines.lastIndexOf(headContentLines[linkLineNumber - 1]) + 1;
        const firstIndex = mergeContentLines.indexOf(headContentLines[linkLineNumber - 1]) + 1;

        if (lastIndex != firstIndex) // Indeterminate; multiple matches found in the file
        {
          UpdateManuallyReview(
            fileName,
            link,
            learningPathFile,
            learningPathLineNumber,
            linkLineNumber);
        }
        else
        {
          UpdateSuggestions(
            fileName,
            link,
            learningPathFile,
            learningPathLineNumber,
            linkLineNumber,
            firstIndex)
        }
      }
    }
  }
}

const main = async () => {

  const [core] = await actionUtils.installAndRequirePackages("@actions/core");

  try {

    const learningPathDirectory = core.getInput('learningPathsDirectory', { required: true });
    const repoURLToSearch = core.getInput('repoURLToSearch', { required: true });
    const headLearningPathsDirectory = headPathPrefix + learningPathDirectory;
    const changedFilePaths = core.getInput('changedFilePaths', {required: false});
    
    if (changedFilePaths === null || changedFilePaths.trim() === "") { return }

    // Scan each file in the learningPaths directory
    actionUtils.readdir(headLearningPathsDirectory, (err, files) => {
      files.forEach(learningPathFile => {

        try {
          const headLearningPathFileContent = actionUtils.readFileSync(headLearningPathsDirectory + "/" + learningPathFile, "utf8")
          if (headLearningPathFileContent)
          {
            CompareFiles(headLearningPathFileContent, repoURLToSearch, changedFilePaths.split(' '), learningPathFile)
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

const actionUtils = require('../action-utils.js');
const prevPathPrefix = "prev/";
const linePrefix = "#L";
const separator = " | ";

modifiedFilesDict = {};
modifiedFilesUrlToFileName = {};

var outOfSync = new Set();
var manuallyReview = new Set();
var suggestions = new Set();
var modifiedFiles = new Set();

const oldNewLinkSeparator = ' -> ';

let modifiedFilesToCommit = [];

function AppendModifiedFilesToCommit(path, core)
{
  modifiedFilesToCommit.push(path)
  core.setOutput('modifiedFilesToCommit', modifiedFilesToCommit.join(' '))
}

function ReplaceOldWithNewText(content, oldText, newText)
{
  return content.replaceAll(oldText, newText);
}

function UpdateModifiedFiles(fileName, path, learningPathFile)
{
  modifiedFilesUrlToFileName[path] = fileName;

  modifiedFilesDict[path] = modifiedFilesDict[path] ? modifiedFilesDict[path] : new Set();;
  modifiedFilesDict[path].add(learningPathFile);

  modifiedFiles = new Set();
  for (currPath in modifiedFilesDict)
  {
    const fileName = modifiedFilesUrlToFileName[currPath];
    modifiedFiles.add(AssembleModifiedFilesOutput(fileName, currPath, Array.from(modifiedFilesDict[currPath])));
  }
}

function AssembleModifiedFilesOutput(fileName, path, learningPathFiles)
{
  return CreateLink(fileName, path, undefined) + separator + BoldedText(learningPathFiles.join(" "));
}

function BoldedText(text)
{
  return "**" + text + "**";
}

function UpdateManuallyReview(fileName, path, learningPathFile, learningPathLineNumber, lineNumber = undefined)
{
  manuallyReview.add(AssembleOutput(fileName, path, undefined, lineNumber, undefined, learningPathFile, learningPathLineNumber))
}

function UpdateOutOfSync(link, learningPathFile)
{
  outOfSync.add(link + separator + BoldedText(learningPathFile))
}

function UpdateSuggestions(fileName, oldPath, newPath, learningPathFile, learningPathLineNumber, oldLineNumber, newLineNumber)
{
  suggestions.add(AssembleOutput(fileName, oldPath, newPath, oldLineNumber, newLineNumber, learningPathFile, learningPathLineNumber))
}

function CreateLink(fileName, path, lineNumber)
{
  var codeFileLink = "[" + fileName + "]" + "(" + path + ")"
  return AppendLineNumber(codeFileLink, lineNumber)
}

function AssembleOutput(fileName, oldPath, newPath, oldLineNumber, newLineNumber, learningPathFile, learningPathLineNumber)
{
  var codeFileLink = CreateLink(fileName, oldPath, oldLineNumber)

  if (newPath && newLineNumber) {
    codeFileLink += oldNewLinkSeparator + CreateLink(fileName, newPath, newLineNumber)
  }

  return codeFileLink + separator + BoldedText(AppendLineNumber(learningPathFile, learningPathLineNumber, undefined));
}

function AppendLineNumber(text, lineNumber)
{
  if (!lineNumber) { return text }

  return text + " " + linePrefix + lineNumber
}

function CheckForEndOfLink(str, startIndex)
{
  const illegalCharIndex = str.substr(startIndex).search("[(), '\`\"\}\{]|\. "); // This regex isn't perfect, but should cover most cases.
  return illegalCharIndex;
}

function StripLineNumber(link, linePrefixIndex)
{
  return link.substring(0, linePrefixIndex);
}

function GetContent(path) {
  try {
    return actionUtils.readFileSync(path)
  }
  catch (error) {}

  return undefined;
}

function ConstructOutputText(core)
{
  var manuallyReviewSection = "<h2>Manually Review:</h2>" + Array.from(manuallyReview).join("<br />") + "<br />";
  if (manuallyReview.size === 0) { manuallyReviewSection = ""; }

  var outOfSyncSection = "<h2>Links With Out Of Sync Commit Hashes:</h2>" + Array.from(outOfSync).join("<br />") + "<br />";
  if (outOfSync.size === 0) { outOfSyncSection = ""; }  

  var suggestionsSection = "<h2>Auto-Applied Suggestions:</h2>" + Array.from(suggestions).join("<br />") + "<br />";
  if (suggestions.size === 0) { suggestionsSection = ""; }

  var modifiedFilesSection = "<h2>Modified Files:</h2>" + Array.from(modifiedFiles).join("<br />") + "<br />";
  if (modifiedFiles.size === 0) { modifiedFilesSection = ""; }

  var body = modifiedFilesSection + manuallyReviewSection + outOfSyncSection + suggestionsSection;
  console.log("body=" + body);

  core.setOutput('outputText', body);
}

function ValidateLinks(learningPathContents, repoURLToSearch, modifiedPRFiles, learningPathFile, oldHash, newHash, sourceDirectoryName, excludeLinksArray)
{
  // Get all indices where a link to the repo is found within the current learning path file
  var linkIndices = [];
  for(var pos = learningPathContents.indexOf(repoURLToSearch); pos !== -1; pos = learningPathContents.indexOf(repoURLToSearch, pos + 1)) {
      linkIndices.push(pos);
  }

  for(let startOfLink of linkIndices)
  {
    // Clean up the link, determine if it has a line number suffix
    const endOfLink = startOfLink + CheckForEndOfLink(learningPathContents, startOfLink)
    const link = learningPathContents.substring(startOfLink, endOfLink);

    if (excludeLinksArray.some(excludeLink => link.toLowerCase().includes(excludeLink))) { continue; }

    const pathStartIndex = link.indexOf(sourceDirectoryName);
    if (pathStartIndex === -1) { continue }

    if (!link.includes(oldHash))
    {
      UpdateOutOfSync(link, learningPathFile);
      continue
    }

    const linePrefixIndex = link.indexOf(linePrefix);
    const linkHasLineNumber = linePrefixIndex !== -1;
    const pathEndIndex = linkHasLineNumber ? linePrefixIndex : endOfLink;

    // Check if the file being referenced by the link is one of the modified files in the PR
    const linkFilePath = link.substring(pathStartIndex, pathEndIndex);
    if (modifiedPRFiles.includes(linkFilePath))
    {
      const fileName = linkFilePath.substring(linkFilePath.lastIndexOf('/') + 1);

      UpdateModifiedFiles(fileName, linkHasLineNumber ? StripLineNumber(link, linePrefixIndex) : link, learningPathFile);

      // This is the line number in the learning path file that contains the link - not the #L line number in the link itself
      const learningPathLineNumber = learningPathContents.substring(0, startOfLink).split("\n").length;

      var headContent = GetContent(linkFilePath)
      if (!headContent) {
        UpdateManuallyReview(fileName, link, learningPathFile, learningPathLineNumber);
        continue
      }
      const headContentLines = headContent.toString().split("\n");

      if (!linkHasLineNumber) { continue; }
      const oldLineNumber = Number(link.substring(linePrefixIndex + linePrefix.length, link.length));

      var prevContent = GetContent(prevPathPrefix + linkFilePath)
      if (!prevContent) { continue; }
      const prevContentLines = prevContent.toString().split("\n");

      if (prevContentLines.length < oldLineNumber)
      {
        UpdateManuallyReview(fileName, link, learningPathFile, learningPathLineNumber, oldLineNumber);
      }
      else if (headContentLines.length < oldLineNumber || prevContentLines[oldLineNumber - 1].trim() !== headContentLines[oldLineNumber - 1].trim())
      {
        const newLineNumberLast = headContentLines.lastIndexOf(prevContentLines[oldLineNumber - 1]) + 1;
        const newLineNumberFirst = headContentLines.indexOf(prevContentLines[oldLineNumber - 1]) + 1;

        if (newLineNumberLast !== newLineNumberFirst) // Multiple matches found in the file
        {
          UpdateManuallyReview(fileName, link, learningPathFile, learningPathLineNumber, oldLineNumber);
        }
        else
        {
          let updatedLink = StripLineNumber(link.replace(oldHash, newHash), linePrefixIndex) + linePrefix + newLineNumberFirst;
          UpdateSuggestions(fileName, link, updatedLink, learningPathFile, learningPathLineNumber, oldLineNumber, newLineNumberFirst);
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
    const changedFilePaths = core.getInput('changedFilePaths', {required: false});
    const learningPathHashFile = core.getInput('learningPathHashFile', { required: true });
    const sourceDirectoryName = core.getInput('sourceDirectoryName', { required: true });
    const oldHash = core.getInput('oldHash', { required: true });
    const newHash = core.getInput('newHash', { required: true });
    const excludeLinks = core.getInput('excludeLinks', { required: false });
    const excludeLinksArray = excludeLinks ? excludeLinks.split(',').map(function(item) { return item.toLowerCase().trim() }) : [];
    
    if (changedFilePaths === null || changedFilePaths.trim() === "") { return }

    // Scan each file in the learningPaths directory
    actionUtils.readdir(learningPathDirectory, (_, files) => {
      files.forEach(learningPathFile => {
        try {
          const learningPathContents = GetContent(learningPathDirectory + "/" + learningPathFile)
          if (learningPathContents)
          {
            ValidateLinks(learningPathContents, repoURLToSearch, changedFilePaths.split(' '), learningPathFile, oldHash, newHash, sourceDirectoryName, excludeLinksArray)
            ConstructOutputText(core);
          }
        } catch (error) {
          console.log("Error: " + error)
          console.log("Could not find learning path file: " + learningPathFile)
        }
      });
    });

    actionUtils.writeFileSync(learningPathHashFile, newHash);
    AppendModifiedFilesToCommit(learningPathHashFile, core)

    // Scan each file in the learningPaths directory
    actionUtils.readdir(learningPathDirectory, (_, files) => {
      files.forEach(learningPathFile => {
        try {
          const fullPath = learningPathDirectory + "/" + learningPathFile
          const content = actionUtils.readFileSync(fullPath)

          var replacedContent = content

          let suggestionsArray = Array.from(suggestions);
          if (suggestionsArray && suggestionsArray.length > 0) {
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

          actionUtils.writeFileSync(fullPath, replacedContent);

          if (content !== replacedContent) {
            AppendModifiedFilesToCommit(fullPath, core)
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

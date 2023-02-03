const util = require("util");

async function main() {
  
  const jsExec = util.promisify(require("child_process").exec);

  console.log("Installing npm dependencies");

  const { stdout, stderr } = await jsExec("npm install @actions/core");
  console.log("npm-install stderr:\n\n" + stderr);
  console.log("npm-install stdout:\n\n" + stdout);  
  const core = require('@actions/core');

  const { stdout2, stderr2 } = await jsExec("npm install @actions/github");
  console.log("npm-install stderr:\n\n" + stderr2);
  console.log("npm-install stdout:\n\n" + stdout2);
  const github = require('@actions/github');

  console.log("Finished installing npm dependencies");

  try {
    const issueNumber = core.getInput('issueNumber', { required: true });
    const assignees = core.getInput('assignee', { required: true });

    const token = core.getInput('githubToken', { required: true })
    const octokit = github.getOctokit(token)
    
    const { context } = github

    await octokit.rest.issues.addAssignees({
      ...context.repo,
      assignees,
      issue_number: issueNumber,
    })
  } catch (error) {
    core.setFailed(error.message);
  }
}

// Call the main function to run the action
main();

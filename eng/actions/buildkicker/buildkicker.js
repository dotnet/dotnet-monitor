// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

const { Console } = require("console");

function BuildKickerException(message, postToGitHub = true) {
    this.message = message;
    this.postToGitHub = postToGitHub;
}
function sleep(sec) {
    return new Promise(resolve => setTimeout(resolve, sec * 1000));
}

let commentPayload = null;
async function AppendCommentContent(text) {
    const core = require("@actions/core");
    const github = require("@actions/github");

    let commentId = core.getInput("commentId", { required: true });
    let octokit = github.getOctokit(core.getInput("auth_token", { required: true }));

    const repo_owner = github.context.payload.repository.owner.login;
    const repo_name = github.context.payload.repository.name;

    if (commentPayload === null) {
        console.log(`Missing comment payload, getting comment #${commentId}.`);
        let oldComment = await octokit.rest.issues.getComment({
            owner: repo_owner,
            repo: repo_name,
            comment_id: commentId
        });
        commentPayload = oldComment.data.body;
        console.log(`Got old comment, body='${commentPayload}'.`);
    }

    let newCommentPayload = `${commentPayload}\n${text}`;
    console.log(`Updating comment #${commentId} to '${newCommentPayload}'`);
    let newComment = await octokit.rest.issues.updateComment({
        owner: repo_owner,
        repo: repo_name,
        comment_id: commentId,
        body: newCommentPayload
    });
    commentPayload = newCommentPayload;
    console.log(`Completed update to comment #${commentId}. Requests remaining=${newComment.headers['x-ratelimit-remaining']}`);
}

function EvaluateRerun(run, allRuns) {
    const core = require("@actions/core");
    let requiredSuccesses = core.getInput("requiredSuccesses", { required: true });

    console.log(`Evaluating build ${run.name} for retry.`);
    if (!(run.status === "completed" && run.conclusion === "failure")) {
        // if it didn't fail yet, don't do anything
        return "wait";
    }

    if (run.status === "completed" && run.conclusion === "success") {
        return "success";
    }

    let appType = run.app.id;
    let totalRuns = 0;
    let successfulRuns = 0;
    let inProgress = 0;
    for (let i = 0; i < allRuns.total_count; i++) {
        let evalRun = allRuns.check_run[i];
        if (evalRun.app.id == appType) {
            totalRuns++;
            if (run.status === "completed" && run.conclusion === "success") {
                successfulRuns++;
            }
            if (run.status === "in_progress") {
                inProgress++;
            }
        }
    }
    console.log(`check stats: totalRuns: ${totalRuns}; successfulRuns: ${successfulRuns}; inProgress: ${inProgress}; requiredSuccesses: ${requiredSuccesses}.`);

    // subtract our check-run
    let otherBuilds = totalRuns - 1;
    if (otherBuilds < requiredSuccesses) {
        requiredSuccesses = otherBuilds;
    }

    if (successfulRuns >= requiredSuccesses) {
        return "rerun";
    }

    let maySucceed = successfulRuns + inProgress;
    if (maySucceed >= requiredSuccesses) {
        return "wait";
    }

    return "stop";
}

async function run() {
    const util = require("util");
    const jsExec = util.promisify(require("child_process").exec);

    console.log("Installing npm dependencies");
    const { stdout, stderr } = await jsExec("npm install @actions/core @actions/github @actions/exec");
    console.log("npm-install stderr:\n\n" + stderr);
    console.log("npm-install stdout:\n\n" + stdout);
    console.log("Finished installing npm dependencies");

    const core = require("@actions/core");
    const github = require("@actions/github");

    const repo_owner = github.context.payload.repository.owner.login;
    const repo_name = github.context.payload.repository.name;
    const pr_number = github.context.payload.issue.number;
    const comment_user = github.context.payload.comment.user.login;

    let octokit = github.getOctokit(core.getInput("auth_token", { required: true }));
    let retries = core.getInput("retries", { required: true });
    let commentId = core.getInput("commentId", { required: true });
    let pollInterval = core.getInput("pollInterval", { required: true });
    let waitSec = 60 * pollInterval;

    let failed = true;
    try {
        // verify the comment user is a repo collaborator
        try {
            await octokit.rest.repos.checkCollaborator({
                owner: repo_owner,
                repo: repo_name,
                username: comment_user
            });
            console.log(`Verified ${comment_user} is a repo collaborator.`);
        } catch (error) {
            console.log(error);
            throw new BuildKickerException(`Error: @${comment_user} is not a repo collaborator, using \`BuildKicker\` is not allowed.`);
        }

        const rerunCounts = new Map();

        let ctr = 100;
        while (ctr > 0) {
            ctr--;

            let myPr = (await octokit.rest.pulls.get({
                owner: repo_owner,
                repo: repo_name,
                pull_number: pr_number,
            })).data;

            console.log(`myPR: ${JSON.stringify(myPr)}`);

            if (myPr === undefined || myPr === null || myPr.head === undefined || myPr.head === null || myPr.head.sha === undefined || myPr.head.sha === null) {
                throw new BuildKickerException(`Error: Didn't get valid [PR].head.sha value when accessing PR #${pr_number}.`);
            }

            if (!myPr.mergeable) {
                throw new BuildKickerException(`Error: The PR must be mergeable before \`BuildKicker\` may be used.`);
            }

            if (myPr.merged) {
                failed = false;
                break;
            }

            let checkruns = (await octokit.rest.checks.listForRef({
                owner: repo_owner,
                repo: repo_name,
                ref: myPr.head.sha,
            })).data;
            console.log(`checkruns: ${JSON.stringify(checkruns)}`);

            let successfulNeeded = checkruns.total_count;
            for (let i = 0; i < checkruns.total_count; i++) {
                let run = checkruns.check_run[i];
                let rerunState = EvaluateRerun(run, checkruns);
                console.log(`Eval for build ${run.name}: ${rerunState}`);

                if (rerunState === "stop") {
                    throw new BuildKickerException(`Error: not enough successful runs to retry \`${run.name}\`.`);
                }
                else if (rerunState === "rerun") {
                    // We want to re-run this instance
                    if (!rerunCounts.has(run.id)) {
                        rerunCounts.set(run.Id, 0);
                    }

                    let reruns = rerun.get(run.Id);

                    if (reruns < retries) {
                        let newRerunCount = reruns + 1;
                        rerunCounts.set(run.Id, newRerunCount);

                        let timestamp = new Date(Date.now());
                        let newCommentEntry = `- \`${timestamp.toISOString()}\` Retrying \`${run.name}\`, attempt # \`${newRerunCount + 1}\`, \`${retries - newRerunCount}\` retries left.`;
                        await AppendCommentContent(newCommentEntry);

                        octokit.rest.checks.rerequestRun({
                            owner: repo_owner,
                            repo: repo_name,
                            check_run_id: run.Id,
                        });
                    }
                    else {
                        throw new BuildKickerException(`Error: out of retries for \`${run.name}\`.`);
                    }
                }
                else if (rerunState === "success") {
                    successfulNeeded--;
                }
            }

            if (successfulNeeded == 0) {
                console.log(`All checks completed`);
                let timestamp = new Date(Date.now());
                let newCommentEntry = `- \`${timestamp.toISOString()}\` Found ${checkruns.total_count} runs and all are in the \`completed\` + \`success\` state.`;
                await AppendCommentContent(newCommentEntry);
                failed = false;
                break;
            }

            console.log(`Waiting ${waitSec} Seconds...`);
            await sleep(waitSec);
        }

        if (ctr <= 0) {
            throw new BuildKickerException(`Error: exceeded max runtime.`);
        }
    }
    catch (error) {
        core.setFailed(error);

        if (error.postToGitHub === undefined || error.postToGitHub == true) {
            let timestamp = new Date(Date.now());
            await AppendCommentContent(`\n\`${timestamp.toISOString()}\` @${comment_user}, **Build Kicker** run failed!\n\n\`\`\`\n${error.message}\n\`\`\``);
            await octokit.rest.reactions.createForIssueComment({
                owner: repo_owner,
                repo: repo_name,
                comment_id: commentId,
                content: "confused"
            });
        }
    }

    if (!failed) {
        let timestamp = new Date(Date.now());
        await AppendCommentContent(`\n\`${timestamp.toISOString()}\` @${comment_user}, **Build Kicker** run complete!`)
        await octokit.rest.reactions.createForIssueComment({
            owner: repo_owner,
            repo: repo_name,
            comment_id: commentId,
            content: "hooray"
        });
    }
}

run();

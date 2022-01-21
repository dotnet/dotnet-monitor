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

    // Grab the comment in it's current state (if we don't already have it cached)
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

    // Append to the comment text
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

    // If this run is successful, bail out and call it a success
    if (run.status === "completed" && run.conclusion === "success") {
        return "success";
    }

    // If this run hasn't completed with a failure, wait until it completes
    if (!(run.status === "completed" && run.conclusion === "failure")) {
        // if it didn't fail yet, don't do anything
        return "wait";
    }

    // Azure DevOps creates a check_run for the "build" and a
    // separate check_run for each "job" in that "build".
    // We only ever want to evaluate and request re-runs for the "job"s.
    // The only way to evaluate if this check_run is a "build" or "job" is to do string
    // parsing on the name or check if the details URL links directly to a job (then we can assume it's a job).
    // So we should always say "wait" to ignore these check_runs.
    // note: 9426 is the Azure DevOps app ID
    // note: if this check_run succeeds, we count it as a success but we want to call it a wait until then
    if (run.app.id == 9426 && !run.details_url.includes("jobId=")) {
        return "wait";
    }

    // We want to know 3 values (each within the context of a given app).
    // We only evaluate things in the same app because we only care about a given
    // family of checks and not the whole check_suite. For example we don't care about
    // the status of the CLA check if we are trying to figure out if we want to re-run a Azure DevOps build.
    let totalRuns = 0; // The total number of check_runs
    let successfulRuns = 0; // The number of check_runs that succeeded
    // The number of check_runs that are still running. We use this value to determine
    // if a run may ever be allowed to re-run, so this can be thought of as the number of
    // check_runs that "May Succeed" at some point in the future
    let inProgress = 0;
    for (let i = 0; i < allRuns.total_count; i++) {
        let evalRun = allRuns.check_runs[i];
        // Again, ignore the Azure DevOps check_run for the "build". (See comment above)
        if (evalRun.app.id == 9426 && !evalRun.details_url.includes("jobId=")) {
            continue;
        }
        // Only evaluate check_runs of the same app (See comment above)
        if (evalRun.app.id == run.app.id) {
            // Add it to the total runs
            totalRuns++;

            // Add it to the successful runs if it succeeded
            if (evalRun.status === "completed" && evalRun.conclusion === "success") {
                successfulRuns++;
            }

            // Add it to the inProgress runs if it's still running
            if (evalRun.status === "in_progress") {
                inProgress++;
            }
        }
    }
    console.log(`check stats: totalRuns: ${totalRuns}; successfulRuns: ${successfulRuns}; inProgress: ${inProgress}; requiredSuccesses: ${requiredSuccesses}.`);

    // "requiredSuccesses" is the configuration value passed to the script.
    // We should cap this value at the total number of builds that actually exist.
    // note: this value may go to 0 if a build is a one-of-a-kind, and we should always allow it to be retried, because we have no other choice.
    let otherBuilds = totalRuns - 1;
    if (otherBuilds < requiredSuccesses) {
        requiredSuccesses = otherBuilds;
    }

    // If we have enough succeeded runs to meet the threshold configured with "requiredSuccesses", then do the re-run
    if (successfulRuns >= requiredSuccesses) {
        return "rerun";
    }

    // If we have don't have enough succeeded runs but do have enough in progress runs that MAY succeed, then wait
    let maySucceed = successfulRuns + inProgress;
    if (maySucceed >= requiredSuccesses) {
        return "wait";
    }

    // If we failed all the above checks, then we would never attempt a re-run, so we should bail out now and stop the GH action.
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
        // Verify the comment user is a repo collaborator
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

        // Cap this loop at 100 tries, the default pollInterval is 5 minutes, so this will probably get killed before 
        // it ever completes (100*5min = 500 minutes ~= 8 hours). We should make sure we will never run forever, so hence the cap.
        let ctr = 100;
        while (ctr > 0) {
            ctr--;

            // Grap the PR this is associated with (we need the head's commit sha to find the check runs)
            let myPr = (await octokit.rest.pulls.get({
                owner: repo_owner,
                repo: repo_name,
                pull_number: pr_number,
            })).data;

            console.log(`myPR: ${JSON.stringify(myPr)}`);

            // Don't allow on closed PRs, this, means that if the PR gets closed we should stop.
            // This means the PR was abandoned or merged, both use PR.state=="closed"
            if (myPr.state === "closed") {
                // If the PR was merged, yay! bail out with a success
                if (myPr.merged) {
                    failed = false;
                    break;
                }
                // If the PR was closed without a merge, it's an error
                throw new BuildKickerException(`Error: The PR must be open to kick the build.`);
            }

            // Sanity check that the PR has a head element and has a sha value
            if (myPr === undefined || myPr === null || myPr.head === undefined || myPr.head === null || myPr.head.sha === undefined || myPr.head.sha === null) {
                throw new BuildKickerException(`Error: Didn't get valid [PR].head.sha value when accessing PR #${pr_number}.`);
            }

            // Check that this is a mergeable PR the mergeable property can be null, which we will treat as false.
            // This ensures that the check runs can be launched, because it must be mergeable to make a PR build.
            if (myPr.mergeable === undefined || myPr.mergeable === null || !myPr.mergeable) {
                throw new BuildKickerException(`Error: The PR must be mergeable before \`BuildKicker\` may be used.`);
            }

            // Go query for the check runs associated with the PR
            let checkruns = (await octokit.rest.checks.listForRef({
                owner: repo_owner,
                repo: repo_name,
                ref: myPr.head.sha,
            })).data;
            console.log(`checkruns: ${JSON.stringify(checkruns)}`);

            let successfulNeeded = checkruns.total_count;
            console.log(`Found ${successfulNeeded} check run(s)`);
            // Iterate over each check run and potentially re-run each
            for (let i = 0; i < checkruns.total_count; i++) {
                let run = checkruns.check_runs[i];
                // Get the rerunability state from the EvaluateRerun function; it may return "success", "stop", "wait" or "rerun".
                let rerunState = EvaluateRerun(run, checkruns);
                console.log(`Eval for check[${i}]=${run.name}: ${rerunState}`);

                if (rerunState === "stop") {
                    // Stop code means to abort build kicker, lets bail out
                    throw new BuildKickerException(`Error: not enough successful runs to retry \`${run.name}\`.`);
                }
                else if (rerunState === "rerun") {
                    // We want to re-run this instance but we need to check the number of allowable re-runs

                    // Fill in "0" reruns if we haven't rerun it before
                    if (!rerunCounts.has(run.id)) {
                        console.log(`No reruns for [${run.id}], setting retry count = 0.`);
                        rerunCounts.set(run.id, 0);
                    }

                    // Grab the number of reruns that have been done on this run
                    let reruns = rerunCounts.get(run.id);

                    // If we still have retries remaining, lets do a rerun
                    if (reruns < retries) {
                        // Write the new retries done back to the rerunCounts Map
                        let newRerunCount = reruns + 1;
                        console.log(`Setting retry count[${run.id}]=${newRerunCount}.`);
                        rerunCounts.set(run.id, newRerunCount);

                        // Add a comment saying what we're doing
                        let timestamp = new Date(Date.now());
                        let newCommentEntry = `- \`${timestamp.toISOString()}\` Retrying \`${run.name}\`, attempt # \`${newRerunCount + 1}\`, \`${retries - newRerunCount}\` retries left.`;
                        await AppendCommentContent(newCommentEntry);

                        // Make the request to do a re-run
                        let reqParams = {
                            owner: repo_owner,
                            repo: repo_name,
                            check_run_id: run.id,
                          };
                        console.log("Executing rerequestRun on octokit: " + JSON.stringify(reqParams));
                        await octokit.rest.checks.rerequestRun(reqParams);
                    }
                    else {
                        // If we are out of retries, abort
                        throw new BuildKickerException(`Error: out of retries for \`${run.name}\`.`);
                    }
                }
                else if (rerunState === "success") {
                    successfulNeeded--;
                }
            }

            // If we had all checks succeed but still got here (probably because auto-merge is off) we should happily exit, we're all done!
            if (successfulNeeded == 0) {
                console.log(`All checks completed`);
                let timestamp = new Date(Date.now());
                let newCommentEntry = `- \`${timestamp.toISOString()}\` Found ${checkruns.total_count} runs and all are in the \`completed\` + \`success\` state.`;
                await AppendCommentContent(newCommentEntry);
                failed = false;
                break;
            }

            // Wait the pollInterval (*60)
            // pollInterval is in minutes but the sleep function takes seconds
            console.log(`Waiting ${waitSec} Seconds...`);
            await sleep(waitSec);
        }

        // If the while look took too many tries throw an error
        if (ctr <= 0) {
            throw new BuildKickerException(`Error: exceeded max runtime.`);
        }

        // If we exited the while loop for any other reason, throw an error.
        // This should never be hit because the only exit points are break for success, exceptions and the ctr going to 0.
        if (failed) {
            throw new BuildKickerException(`Error: we shouldn't get here, something went wrong.`);
        }
    }
    catch (error) {
        core.setFailed(error);

        console.log (`Build Kicker global Exception: ${error.message}`);
        // If we get an error, try to post it to github if requested
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

    // If we didn't fail, print the success message
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

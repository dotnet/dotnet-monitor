module.exports = async (github, context, baseSha, branchName) => {
    const refName = `heads/${branchName}`;

    // Check if the ref already exists, if so we will need to fast forward it.
    let needToCreateRef = true;
    try {
        await github.rest.git.getRef({
            owner: context.repo.owner,
            repo: context.repo.repo,
            ref: refName
        });
        needToCreateRef = false;
    } catch {
    }

    if (needToCreateRef) {
        await github.rest.git.createRef({
            owner: context.repo.owner,
            repo: context.repo.repo,
            sha: baseSha,
            ref: `refs/${refName}`
        });
    } else {
        await github.rest.git.updateRef({
            owner: context.repo.owner,
            repo: context.repo.repo,
            sha: baseSha,
            ref: refName,
            force: true
        });
    }
}
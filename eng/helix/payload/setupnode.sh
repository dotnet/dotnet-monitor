#!/bin/sh


#
# Create an npm alias.
#
# When helix transfers a correlation payload it will zip the contents for transit, resulting in symbolic links to be dereferenced.
# In the case of common node tools (e.g. npm) this is not ideal as the linked scripts contain relative file paths which will be broken.
#
# Ideally we could simply transfer a tar file instead and unpack it here, however not all helix containers
# have the necessary tooling to process tar files, let alone the ability to change the EUID to use
# the distro's package manager to install them.
#
# Alternatively we could recreate the symbolic links. However certain helix containers have read-only file systems which makes this impossible.
# Creating a custom alias that directly invokes node with the npm-cli script will work across all of these constraints on all *nix platforms.
#
alias helix_npm="node $HELIX_CORRELATION_PAYLOAD/nodejs/lib/node_modules/npm/bin/npm-cli.js"

export PATH=$HELIX_CORRELATION_PAYLOAD/nodejs/bin:$PATH
helix_npm config set prefix $HELIX_WORKITEM_ROOT/.npm || exit 1
export PATH=$HELIX_WORKITEM_ROOT/.npm/bin:$PATH

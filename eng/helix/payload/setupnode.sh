#!/bin/sh

#
# Recreate executable symlinks.
# When helix transfers a correlation payload it will zip the contents for transit, resulting in symbolic links being dereferenced.
# In the case of common node tools (e.g. npm) this is not ideal as the linked scripts contain relative file paths which will be broken.
#
# Ideally we could simply transfer a tar file instead and unpack it here, however not all helix containers have the necessary tooling
# to process tar files nor do all containers have ability to change the EUID, meaning the distro's package manager cannot be used to
# install the missing tools.
#
pushd $HELIX_CORRELATION_PAYLOAD/nodejs/bin
rm corepack || exit 1
ln -s ../lib/node_modules/corepack/dist/corepack.js corepack || exit 1

rm npm || exit 1
ln -s ../lib/node_modules/npm/bin/npm-cli.js npm || exit 1

rm npx || exit 1
ln -s ../lib/node_modules/npm/bin/npx-cli.js npx || exit 1
popd

export PATH=$HELIX_CORRELATION_PAYLOAD/nodejs/bin:$PATH
npm config set prefix $HELIX_WORKITEM_ROOT/.npm || exit 1
export PATH=$HELIX_WORKITEM_ROOT/.npm/bin:$PATH
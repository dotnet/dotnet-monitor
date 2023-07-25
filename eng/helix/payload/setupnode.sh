#!/bin/sh

setupNode() {
  export PATH=$HELIX_CORRELATION_PAYLOAD/nodejs/$1/bin:$PATH
  npm config set prefix $HELIX_WORKITEM_ROOT/.npm || exit 1
  export PATH=$HELIX_WORKITEM_ROOT/.npm/bin:$PATH
}

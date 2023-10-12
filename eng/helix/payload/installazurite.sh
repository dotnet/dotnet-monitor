#!/bin/sh

helix_npm install -g azurite || exit 1
export TEST_AZURITE_MUST_INITIALIZE=1

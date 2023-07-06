#!/usr/bin/env bash

testAssembly="$1"
configuration="$2"
targetFramework="$3"
architecture="$4"

filterArgs=""
if [[ ! -z "$filter" ]]; then
  filterArgs="--filter \"$5\""
fi

exit_code=0

echo "Start tests..."

dotnet test \
  "$HELIX_CORRELATION_PAYLOAD/$testAssembly/$configuration/$targetFramework/$testAssembly.dll" \
  --logger:"console;verbosity=normal" \
  --logger:"trx;LogFileName=${testAssembly}_${targetFramework}_${architecture}.trx" \
  --logger:"html;LogFileName=${testAssembly}_${targetFramework}_${architecture}.html" \
  --ResultsDirectory:$HELIX_WORKITEM_UPLOAD_ROOT \
  --blame "CollectHangDump;TestTimeout=15m" \
  $filterArgs

exit_code=$?

echo "Finished tests; exit code: $exit_code"

exit $exit_code

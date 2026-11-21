#!/usr/bin/env bash
# Harness self-test: compiles each selftest/*.cs snippet INTO the game assembly (without touching the repo) and
# checks that the expected diagnostic (// EXPECT: CSxxxx or OK) is produced in each // CONFIG (default Android).
set -uo pipefail
HARNESS="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
pass=0; fail=0
for f in "$HARNESS"/selftest/*.cs; do
  expect="$(sed -n 's#^// EXPECT: *\([A-Za-z0-9]*\).*#\1#p' "$f" | head -1)"
  cfgs="$(sed -n 's#^// CONFIG: *\([A-Za-z,]*\).*#\1#p' "$f" | head -1)"; cfgs="${cfgs:-Android}"
  out="$(HARNESS_EXTRA_SOURCES="$f" "$HARNESS/typecheck.sh" -c "$cfgs" "$@" 2>&1)"; rc=$?
  name="$(basename "$f")"
  if [ "$expect" = "OK" ]; then
    if [ $rc -eq 0 ]; then echo "PASS $name (OK in $cfgs)"; pass=$((pass+1)); else echo "FAIL $name: expected OK, got:"; echo "$out" | sed 's/^/    /'; fail=$((fail+1)); fi
  else
    if [ $rc -ne 0 ] && echo "$out" | grep -q "selftest/$name:[0-9]*: error $expect"; then
      echo "PASS $name ($expect in $cfgs)"; pass=$((pass+1))
    else echo "FAIL $name: expected $expect, got:"; echo "$out" | sed 's/^/    /'; fail=$((fail+1)); fi
  fi
done
# leave the incremental build in the baseline state
"$HARNESS/typecheck.sh" "$@" >/dev/null 2>&1
echo "SELFTEST: $pass passed, $fail failed"
[ $fail -eq 0 ]

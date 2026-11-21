#!/usr/bin/env bash
# Downloads the REAL sources of the Unity packages the game uses into lib/pkgsrc (idempotent), then applies
# tools/apply_pkg_deltas.py. Versions follow Packages/packages-lock.json of the game.
# Sources: github.com/needle-mirror (mirror of the Unity package registry) and github.com/ironsource-mobile/Unity-sdk.
# Exit code 0 when every package is present, 1 otherwise (typecheck.sh then falls back to stubs-fallback/).
set -uo pipefail
HARNESS="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DEST="${PKGSRC_DEST:-$HARNESS/lib/pkgsrc}"
mkdir -p "$DEST"
ok=1

mirror() { # <package> <tag> <dir>
  local pkg="$1" tag="$2" dir="$DEST/$3"
  [ -f "$dir/package.json" ] && return 0
  rm -rf "$dir"
  if ! timeout 600 git -c advice.detachedHead=false clone -q --depth 1 --branch "$tag" "https://github.com/needle-mirror/$pkg" "$dir" >/dev/null 2>&1; then
    echo "fetch_packages: could not clone needle-mirror/$pkg@$tag" >&2; rm -rf "$dir"; ok=0
  fi
}

mirror com.unity.services.core           1.16.0                "com.unity.services.core@1.16.0"
mirror com.unity.services.authentication 3.6.1                 "com.unity.services.authentication@3.6.1"
mirror com.unity.services.cloudsave      3.4.0                 "com.unity.services.cloudsave@3.4.0"
mirror com.unity.services.analytics      6.3.0                 "com.unity.services.analytics@6.3.0"
mirror com.unity.nuget.newtonsoft-json   3.2.2                 "com.unity.nuget.newtonsoft-json@3.2.2"
mirror com.unity.inputsystem             1.17.0                "com.unity.inputsystem@1.17.0"
mirror com.unity.addressables            2.8.1                 "com.unity.addressables@2.8.1"
mirror com.unity.profiling.core          1.0.3                 "com.unity.profiling.core@1.0.3"
mirror com.unity.textmeshpro             3.2.0-pre.15          "com.unity.textmeshpro@3.2.0-pre.15"
# ugui: last standalone release mirrored (Unity 2021.1.17f1). The tag contains a folder level: flatten it.
if [ ! -d "$DEST/com.unity.ugui@1.0.0-2021.1.17/Runtime" ]; then
  tmp="$DEST/.ugui-tmp"; rm -rf "$tmp"
  if timeout 600 git -c advice.detachedHead=false clone -q --depth 1 --branch "1.0.0/Unity-2021.1.17f1" https://github.com/needle-mirror/com.unity.ugui "$tmp" >/dev/null 2>&1; then
    rm -rf "$DEST/com.unity.ugui@1.0.0-2021.1.17"
    if [ -d "$tmp/Runtime" ]; then mv "$tmp" "$DEST/com.unity.ugui@1.0.0-2021.1.17"
    else mv "$tmp"/*/ "$DEST/com.unity.ugui@1.0.0-2021.1.17" && rm -rf "$tmp"; fi
  else echo "fetch_packages: could not clone needle-mirror/com.unity.ugui" >&2; rm -rf "$tmp"; ok=0; fi
fi
# LevelPlay 9.4.0 runtime sources from the official .unitypackage (the UPM package 9.4.1 is not mirrored).
if [ ! -f "$DEST/levelplay@9.4.0/Assets/LevelPlay/Runtime/Api/LevelPlay.cs" ]; then
  tmp="$DEST/.levelplay-tmp"; rm -rf "$tmp"
  if timeout 900 git -c advice.detachedHead=false clone -q --depth 1 --branch release/9.4.0 https://github.com/ironsource-mobile/Unity-sdk "$tmp" >/dev/null 2>&1 \
     && [ -f "$tmp/9.4.0/UnityLevelPlay_v9.4.0.unitypackage" ]; then
    python3 - "$tmp/9.4.0/UnityLevelPlay_v9.4.0.unitypackage" "$DEST/levelplay@9.4.0" <<'PY'
import os, sys, tarfile
pkg, out = sys.argv[1], sys.argv[2]
entries = {}
with tarfile.open(pkg, "r:gz") as tar:
    for m in tar.getmembers():
        parts = m.name.lstrip("./").split("/")
        if len(parts) == 2 and parts[1] in ("pathname", "asset") and m.isfile():
            entries.setdefault(parts[0], {})[parts[1]] = tar.extractfile(m).read()
n = 0
for guid, e in entries.items():
    if "pathname" in e and "asset" in e:
        path = e["pathname"].decode("utf-8", "replace").splitlines()[0].strip()
        if path.endswith(".cs"):
            dst = os.path.join(out, path)
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            open(dst, "wb").write(e["asset"]); n += 1
print(f"fetch_packages: extracted {n} LevelPlay C# files")
PY
  else echo "fetch_packages: could not fetch LevelPlay 9.4.0 from ironsource-mobile/Unity-sdk" >&2; ok=0; fi
  rm -rf "$tmp"
fi

[ "$ok" = 1 ] && python3 "$HARNESS/tools/apply_pkg_deltas.py" >/dev/null || ok=0
[ "$ok" = 1 ] && touch "$DEST/.complete"
[ "$ok" = 1 ]

#!/usr/bin/env bash
# Meowtel type-check harness: compiles the game's runtime scripts (in place) against UnityEngine reference DLLs
# + stubs, without Unity. Prints only compiler errors, then "TYPECHECK OK" or "TYPECHECK FAILED (N errors)".
#
# Usage: typecheck.sh [-c CONFIGS] [--repo PATH] [--warnings] [--clean] [-h]
#   -c, --config CONFIGS  comma-separated list among Android,Editor,Dev,WebGL, or "all" (default: Android,Editor)
#                         Android = release player (UNITY_ANDROID, no UNITY_EDITOR)  -> checks #if UNITY_ANDROID && !UNITY_EDITOR
#                         Editor  = editor with Android target (UNITY_EDITOR)       -> checks #if UNITY_EDITOR / DEVELOPMENT_BUILD code
#                         Dev     = Android development build (DEVELOPMENT_BUILD)
#                         WebGL   = experimental (UNITY_WEBGL, no Google.Play.Games assembly)
#   --repo PATH           game checkout to compile (default: $MEOWTEL_REPO, else the repo containing Tools/TypeCheck); works with worktrees
#   --warnings            also print compiler warnings for game files, including obsolete Unity APIs (CS0618/CS0612)
#   --stubs               use the hand-written stubs-fallback/ instead of the real package sources (automatic when
#                         the sources cannot be downloaded)
#   --clean               wipe build outputs first (not the downloaded/patched reference DLLs)
# Exit code: 0 = no error, 1 = compile errors, 2 = harness/setup failure.
set -uo pipefail

HARNESS="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="${MEOWTEL_REPO:-$(cd "$HARNESS/../.." && pwd)}"
CONFIGS="Android,Editor"
WARNINGS=0
CLEAN=0
STUBS_ONLY=0

while [ $# -gt 0 ]; do
  case "$1" in
    -c|--config) CONFIGS="$2"; shift 2 ;;
    --repo) REPO="$2"; shift 2 ;;
    --warnings) WARNINGS=1; shift ;;
    --clean) CLEAN=1; shift ;;
    --stubs) STUBS_ONLY=1; shift ;;
    -h|--help) sed -n '2,19p' "$0"; exit 0 ;;
    *) echo "typecheck.sh: unknown argument '$1' (see -h)" >&2; exit 2 ;;
  esac
done
[ "$CONFIGS" = "all" ] && CONFIGS="Android,Editor,Dev,WebGL"
REPO="$(cd "$REPO" 2>/dev/null && pwd)" || { echo "typecheck.sh: repo not found" >&2; exit 2; }
[ -d "$REPO/Assets/_Project/Scripts" ] || { echo "typecheck.sh: $REPO/Assets/_Project/Scripts not found" >&2; exit 2; }

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 MSBUILDTERMINALLOGGER=off

LIB="$HARNESS/lib"
NUGET="https://api.nuget.org/v3-flatcontainer"

fail_setup() { echo "typecheck.sh: setup failed: $*" >&2; echo "TYPECHECK FAILED (setup)"; exit 2; }

# ---------- one-time bootstrap: reference DLLs (download + de-publicize + Unity 6 members) ----------
bootstrap() {
  mkdir -p "$LIB"
  if [ ! -f "$LIB/unity/lib/netstandard2.0/UnityEngine.CoreModule.dll" ]; then
    curl -sSfL -o "$LIB/unityengine.modules.nupkg" "$NUGET/unityengine.modules/2021.3.33/unityengine.modules.2021.3.33.nupkg" || fail_setup "download UnityEngine.Modules"
    rm -rf "$LIB/unity" && mkdir -p "$LIB/unity" && (cd "$LIB/unity" && unzip -q -o ../unityengine.modules.nupkg 'lib/netstandard2.0/*') || fail_setup "unzip UnityEngine.Modules"
    chmod -R u+rwX,go+rX "$LIB/unity"
  fi
  if [ ! -f "$LIB/sdk2021.1/lib/UnityEngine.dll" ]; then
    curl -sSfL -o "$LIB/unity3d.sdk.2021.1.14.1.nupkg" "$NUGET/unity3d.sdk/2021.1.14.1/unity3d.sdk.2021.1.14.1.nupkg" || fail_setup "download Unity3D.SDK (accessibility oracle)"
    rm -rf "$LIB/sdk2021.1" && mkdir -p "$LIB/sdk2021.1" && (cd "$LIB/sdk2021.1" && unzip -q -o ../unity3d.sdk.2021.1.14.1.nupkg 'lib/UnityEngine.dll') || fail_setup "unzip Unity3D.SDK"
    chmod -R u+rwX,go+rX "$LIB/sdk2021.1"
  fi
  local tool="$HARNESS/tools/UnityRefPatcher"
  if [ ! -f "$LIB/unity-patched/UnityEngine.CoreModule.dll" ] || [ "$tool/Program.cs" -nt "$LIB/unity-patched/UnityEngine.CoreModule.dll" ]; then
    dotnet build "$tool" -c Release -o "$tool/out" -nologo -v q -clp:NoSummary >/dev/null 2>&1 || fail_setup "build tools/UnityRefPatcher"
    rm -rf "$LIB/unity-patched"
    dotnet "$tool/out/UnityRefPatcher.dll" "$LIB/unity/lib/netstandard2.0" "$LIB/sdk2021.1/lib/UnityEngine.dll" "$LIB/unity-patched" >/dev/null || fail_setup "run UnityRefPatcher"
  fi
}
bootstrap

# ---------- one-time bootstrap: real package sources (ugui, TMP, Input System, Addressables, UGS, LevelPlay) ----------
if [ "$STUBS_ONLY" = 0 ] && [ ! -f "$LIB/pkgsrc/.complete" ]; then
  if ! "$HARNESS/tools/fetch_packages.sh" >/dev/null; then
    echo "NOTE: package sources unavailable, using the less faithful stubs-fallback/ (see README.md)" >&2
    STUBS_ONLY=1
  fi
fi

if [ "$CLEAN" = 1 ]; then rm -rf "$HARNESS/obj" "$HARNESS/bin"; fi

PROJ="$HARNESS/game/Assembly-CSharp.csproj"
STUB_PROP="-p:HarnessStubsOnly=$([ "$STUBS_ONLY" = 1 ] && echo true || echo false)"
need_restore=0
for f in "$HARNESS"/game/*.csproj "$HARNESS"/firstpass/*.csproj "$HARNESS"/gpgs/*.csproj "$HARNESS"/stubs/*.csproj "$HARNESS"/stubs-fallback/*.csproj "$HARNESS"/pkgs/*.csproj; do
  a="$HARNESS/obj/$(basename "$f" .csproj)/project.assets.json"
  if [ ! -f "$a" ]; then need_restore=1; break; fi
  for g in "$HARNESS"/*.props "$HARNESS"/pkgs/*.props "$f"; do [ "$g" -nt "$a" ] && need_restore=1; done
done
if [ "$need_restore" = 1 ]; then
  for sp in false true; do
    out="$(dotnet restore "$PROJ" -nologo -v q -p:HarnessStubsOnly=$sp 2>&1)" || { echo "$out" | grep -E 'error' | head -5 >&2; fail_setup "dotnet restore"; }
  done
  touch "$HARNESS"/obj/*/project.assets.json 2>/dev/null
fi

EXTRA_PROPS=()
[ "$WARNINGS" = 1 ] && EXTRA_PROPS+=("-p:HarnessWarnObsolete=true")
[ -n "${HARNESS_EXTRA_SOURCES:-}" ] && EXTRA_PROPS+=("-p:HarnessExtraSources=$HARNESS_EXTRA_SOURCES")
# HARNESS_EXTRA_DEFINES=SYM (e.g. ENABLE_UNITY_CONSENT): extra scripting define for every assembly
[ -n "${HARNESS_EXTRA_DEFINES:-}" ] && EXTRA_PROPS+=("-p:ExtraDefines=$HARNESS_EXTRA_DEFINES")

TMP="$(mktemp -d)"; trap 'rm -rf "$TMP"' EXIT
IFS=',' read -r -a CFG_LIST <<< "$CONFIGS"
for cfg in "${CFG_LIST[@]}"; do
  case "$cfg" in Android|Editor|Dev|WebGL) ;; *) echo "typecheck.sh: unknown config '$cfg'" >&2; exit 2 ;; esac
done
for cfg in "${CFG_LIST[@]}"; do
  dotnet build "$PROJ" -c "$cfg" --no-restore -nologo -v q -clp:NoSummary \
    -p:MeowtelRepo="$REPO" "$STUB_PROP" "${EXTRA_PROPS[@]}" > "$TMP/$cfg.log" 2>&1
  echo $? > "$TMP/$cfg.rc"
done

# Normalize: "/abs/File.cs(12,5): error CS0103: msg [/abs/x.csproj]" -> "Assets/.../File.cs:12: error CS0103: msg"
python3 - "$TMP" "$REPO" "$HARNESS" "$WARNINGS" "${CFG_LIST[@]}" <<'PY'
import os, re, sys
tmp, repo, harness, warnings, cfgs = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4] == "1", sys.argv[5:]
diag = re.compile(r'^(?P<file>.+?)\((?P<line>\d+),(?P<col>\d+)\): (?P<sev>error|warning) (?P<code>[A-Z]+\d+): (?P<msg>.*?)(?: \[[^\]]*\])?$')
other_err = re.compile(r'^(?P<pre>.*?)\b(?P<sev>error) (?P<code>[A-Z]+\d+): (?P<msg>.*?)(?: \[[^\]]*\])?$')
errors, warns, order = {}, {}, []
def rel(p):
    p = os.path.normpath(p)
    if p.startswith(repo + os.sep): return p[len(repo) + 1:]
    if p.startswith(harness + os.sep): return "harness:" + p[len(harness) + 1:]
    return p
failed_without_diag = []
for cfg in cfgs:
    log = open(os.path.join(tmp, cfg + ".log"), errors="replace").read().splitlines()
    rc = int(open(os.path.join(tmp, cfg + ".rc")).read().strip() or "1")
    n_before = len(errors)
    for raw in log:
        line = raw.strip()
        m = diag.match(line)
        if m:
            key = f"{rel(m['file'])}:{m['line']}: {m['sev']} {m['code']}: {m['msg']}"
            if m['sev'] == 'error':
                errors.setdefault(key, []).append(cfg)
            elif warnings and rel(m['file']).startswith("Assets/_Project/"):
                warns.setdefault(key, []).append(cfg)
            continue
        m = other_err.match(line)
        if m:
            key = f"{rel(m['pre'].rstrip(': ')) or 'build'}: error {m['code']}: {m['msg']}"
            errors.setdefault(key, []).append(cfg)
    if rc != 0 and not any(cfg in v for v in errors.values()):
        failed_without_diag.append(cfg)
        errors.setdefault(f"build: error HARNESS: dotnet build failed for config {cfg} without a parsable diagnostic (see: dotnet build {harness}/game -c {cfg})", []).append(cfg)
tag = (lambda cs: "" if len(cfgs) == 1 else f" [{','.join(sorted(set(cs), key=cfgs.index))}]")
for k, cs in sorted(warns.items()): print(k + tag(cs))
for k, cs in sorted(errors.items()): print(k + tag(cs))
n = len(errors)
print("TYPECHECK OK" if n == 0 else f"TYPECHECK FAILED ({n} errors)")
sys.exit(0 if n == 0 else 1)
PY

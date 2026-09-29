#!/usr/bin/env python3
"""Idempotent source deltas applied to the downloaded third-party package sources in lib/pkgsrc.

ugui: the harness compiles the last standalone com.unity.ugui sources (1.0.0, Unity 2021.1.17f1 snapshot), while the
project runs ugui 2.0.0 (built into Unity 6). Only the members below, public in later ugui versions and required by
com.unity.inputsystem 1.17.0 (and usable by game code), are added. Every insertion is tagged HARNESS-DELTA.
"""
import os, sys

root = os.environ.get("PKGSRC_DEST") or os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "lib", "pkgsrc")
TAG = "// HARNESS-DELTA"

def insert_after(path, anchor, text):
    path = os.path.join(root, path)
    src = open(path, encoding="utf-8-sig").read()
    if TAG in src:
        return False
    if anchor not in src:
        sys.exit(f"apply_pkg_deltas: anchor not found in {path}: {anchor!r}")
    src = src.replace(anchor, anchor + text, 1)
    open(path, "w", encoding="utf-8").write(src)
    return True

changed = insert_after(
    "com.unity.ugui@1.0.0-2021.1.17/Runtime/EventSystem/EventData/PointerEventData.cs",
    "        public Vector2 radiusVariance { get; set; }\n",
    "\n"
    "        " + TAG + " (ugui 2022.1+/2.0): index of the display the pointer event comes from.\n"
    "        public int displayIndex { get; set; }\n"
    "        " + TAG + " (ugui 2021.2+/2.0): pointer exited the object and all its children.\n"
    "        public bool fullyExited { get; set; }\n"
    "        " + TAG + " (ugui 2021.2+/2.0): pointer re-entered the object from a child.\n"
    "        public bool reentered { get; set; }\n",
)
print("apply_pkg_deltas: " + ("applied" if changed else "already applied"))

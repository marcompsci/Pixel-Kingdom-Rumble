#!/usr/bin/env python3
"""
Creates missing Unity .meta files under Assets/ with deterministic GUIDs (md5 of the path),
so files authored outside the Unity Editor keep stable GUIDs across machines.
Never overwrites an existing .meta. Run from the repo root:  python3 Tools/gen_metas.py
"""
import hashlib, os, sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
ASSETS = os.path.join(ROOT, "Assets")

def guid(rel): return hashlib.md5(("PKR:" + rel.replace(os.sep, "/")).encode()).hexdigest()

FOLDER = "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
MONO = ("MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n"
        "  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
ASMDEF = "AssemblyDefinitionImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
TEXT = "TextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
IOS_PLUGIN = ("PluginImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  iconMap: {}\n  executionOrder: {}\n"
    "  defineConstraints: []\n  isPreloaded: 0\n  isOverridable: 1\n  isExplicitlyReferenced: 0\n  validateReferences: 1\n"
    "  platformData:\n  - first:\n      Any: \n    second:\n      enabled: 0\n      settings: {}\n"
    "  - first:\n      Editor: Editor\n    second:\n      enabled: 0\n      settings:\n        DefaultValueInitialized: true\n"
    "  - first:\n      iPhone: iOS\n    second:\n      enabled: 1\n      settings: {}\n"
    "  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
BY_EXT = {".cs": MONO, ".asmdef": ASMDEF, ".asmref": ASMDEF, ".md": TEXT, ".txt": TEXT, ".json": TEXT, ".mm": IOS_PLUGIN, ".m": IOS_PLUGIN}

def write(path, body):
    meta = path + ".meta"
    if os.path.exists(meta): return 0
    rel = os.path.relpath(path, ROOT)
    with open(meta, "w", newline="\n") as f:
        f.write(f"fileFormatVersion: 2\nguid: {guid(rel)}\n{body}")
    return 1

made = 0
for dirpath, dirnames, filenames in os.walk(ASSETS):
    dirnames[:] = [d for d in dirnames if not d.startswith(".")]
    for d in dirnames: made += write(os.path.join(dirpath, d), FOLDER)
    for fn in filenames:
        if fn.endswith(".meta") or fn.startswith("."): continue
        ext = os.path.splitext(fn)[1].lower()
        if ext not in BY_EXT:
            print("skip (Unity will create meta):", os.path.relpath(os.path.join(dirpath, fn), ROOT)); continue
        made += write(os.path.join(dirpath, fn), BY_EXT[ext])
print(f"created {made} meta files")

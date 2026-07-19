"""Fail publication before a Unity WebGL page can point at missing runtime files."""

import pathlib
import sys


REQUIRED_FILES = (
    "Build/IdeaZooWebGL.loader.js",
    "Build/IdeaZooWebGL.data.gz",
    "Build/IdeaZooWebGL.framework.js.gz",
    "Build/IdeaZooWebGL.wasm.gz",
)
REQUIRED_CONFIG = (
    'dataUrl: buildUrl + "/IdeaZooWebGL.data.gz"',
    'frameworkUrl: buildUrl + "/IdeaZooWebGL.framework.js.gz"',
    'codeUrl: buildUrl + "/IdeaZooWebGL.wasm.gz"',
)


def main() -> int:
    if len(sys.argv) != 2:
        print("Usage: validate_webgl_publish_contract.py <webgl-build-directory>", file=sys.stderr)
        return 2
    root = pathlib.Path(sys.argv[1])
    missing = [name for name in REQUIRED_FILES if not (root / name).is_file()]
    index = root / "index.html"
    if not index.is_file():
        missing.append("index.html")
    if missing:
        print("WebGL publish contract missing: " + ", ".join(missing), file=sys.stderr)
        return 1
    source = index.read_text(encoding="utf-8")
    wrong = [entry for entry in REQUIRED_CONFIG if entry not in source]
    if wrong:
        print("WebGL publish contract has stale Unity URLs: " + ", ".join(wrong), file=sys.stderr)
        return 1
    empty = [name for name in REQUIRED_FILES if (root / name).stat().st_size == 0]
    if empty:
        print("WebGL publish contract has empty runtime files: " + ", ".join(empty), file=sys.stderr)
        return 1
    print("WEBGL_PUBLISH_CONTRACT_PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

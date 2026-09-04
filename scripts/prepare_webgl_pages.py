#!/usr/bin/env python3
"""Prepare Unity's generated WebGL shell for responsive GitHub Pages hosting."""

from pathlib import Path
import sys


RESPONSIVE_STYLE = """<style id="idea-zoo-responsive-shell">
html, body { width: 100%; height: 100%; margin: 0; overflow: hidden; background: #030709; }
#unity-container.unity-desktop { position: fixed; inset: 0; width: 100%; height: 100%; }
#unity-canvas { display: block; width: 100% !important; height: 100% !important; background: #030709; }
#unity-footer { position: absolute; z-index: 2; right: 0; bottom: 0; left: 0; height: 38px; padding: 0 12px; box-sizing: border-box; background: rgba(3, 7, 9, .82); }
#unity-warning { position: absolute; z-index: 4; top: 8px; left: 50%; transform: translateX(-50%); }
@media (max-width: 720px), (max-height: 540px) { #unity-footer { display: none; } }
</style>"""


def prepare(index_path: Path) -> None:
    source = index_path.read_text(encoding="utf-8")
    replacements = {
        "Unity Web Player | unity": "The Idea Zoo — Browser Playtest",
        "IdeaZooWebGL.data.gz": "IdeaZooWebGL.data",
        "IdeaZooWebGL.framework.js.gz": "IdeaZooWebGL.framework.js",
        "IdeaZooWebGL.wasm.gz": "IdeaZooWebGL.wasm",
        'companyName: "DefaultCompany"': 'companyName: "Idea Zoo"',
        'productName: "unity"': 'productName: "The Idea Zoo"',
        '<div id="unity-build-title">unity</div>': '<div id="unity-build-title">The Idea Zoo</div>',
    }
    for old, new in replacements.items():
        source = source.replace(old, new)

    if 'id="idea-zoo-responsive-shell"' not in source:
        if "</head>" not in source:
            raise ValueError("Unity WebGL index has no closing head element")
        source = source.replace("</head>", RESPONSIVE_STYLE + "\n</head>", 1)

    index_path.write_text(source, encoding="utf-8")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("Usage: prepare_webgl_pages.py <WebGL index.html>")
    prepare(Path(sys.argv[1]))

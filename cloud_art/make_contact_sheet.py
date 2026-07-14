#!/usr/bin/env python3
from __future__ import annotations

import argparse
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    source = Path(args.input)
    files = sorted(path for path in source.rglob("*.png") if path.name != "IdeaZoo_CloudArt_ContactSheet.png")
    if not files:
        raise SystemExit("No preview images were generated")

    tile = 320
    label = 42
    cols = 4
    rows = (len(files) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * tile, rows * (tile + label)), (7, 18, 22))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()

    for index, path in enumerate(files):
        image = Image.open(path).convert("RGB")
        image.thumbnail((tile - 16, tile - 16))
        x = (index % cols) * tile + (tile - image.width) // 2
        y = (index // cols) * (tile + label) + (tile - image.height) // 2
        sheet.paste(image, (x, y))
        title = path.stem.replace("_", " ")
        tx = (index % cols) * tile + 12
        ty = (index // cols) * (tile + label) + tile + 10
        draw.text((tx, ty), title, fill=(225, 218, 190), font=font)

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output)
    print(f"Wrote {output} with {len(files)} assets")


if __name__ == "__main__":
    main()

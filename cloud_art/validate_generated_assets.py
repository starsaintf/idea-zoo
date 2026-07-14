#!/usr/bin/env python3
"""Validate the deterministic output of the cloud Blender art build."""
from __future__ import annotations

import argparse
import json
from pathlib import Path

EXPECTED_CHARACTERS = {
    "Keeper_0", "Keeper_1", "Keeper_2",
    "Mara_Rook", "Toma_Reed", "Sefu_Anik", "Elian_Thread", "Sen_Osei", "Nara_Voss",
    "Lio_Jury", "Amara_Jury", "Kweku_Jury",
}
EXPECTED_CREATURES = {"Avian", "BurdenBeast", "Lantern", "Serpentine", "Choir"}


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest", required=True)
    parser.add_argument("--repo-root", default=".")
    args = parser.parse_args()

    root = Path(args.repo_root).resolve()
    manifest_path = Path(args.manifest).resolve()
    data = json.loads(manifest_path.read_text(encoding="utf-8"))

    characters = {item["name"] for item in data.get("characters", [])}
    creatures = {item["name"] for item in data.get("creatures", [])}
    assert characters == EXPECTED_CHARACTERS, f"Character set mismatch: {characters ^ EXPECTED_CHARACTERS}"
    assert creatures == EXPECTED_CREATURES, f"Creature set mismatch: {creatures ^ EXPECTED_CREATURES}"
    assert data.get("asset_count") == 17, data.get("asset_count")

    failures = []
    for item in data["characters"] + data["creatures"]:
        for key in ("fbx", "glb", "blend", "preview"):
            path = Path(item[key])
            if not path.is_absolute():
                path = root / path
            if not path.exists() or path.stat().st_size < 128:
                failures.append(f"{item['name']}: missing or tiny {key}: {path}")
        if item.get("mesh_objects", 0) < 3:
            failures.append(f"{item['name']}: insufficient mesh objects")
        if item.get("triangles", 0) <= 0:
            failures.append(f"{item['name']}: no triangles")
        if item["category"] == "Characters" and item.get("triangles", 0) > 35000:
            failures.append(f"{item['name']}: exceeds 35k character triangle budget")
        if item["category"] == "Creatures" and item.get("triangles", 0) > 30000:
            failures.append(f"{item['name']}: exceeds 30k creature triangle budget")

    if failures:
        raise SystemExit("\n".join(failures))
    print(f"Validated {data['asset_count']} cloud-generated Idea Zoo art assets.")


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
from __future__ import annotations

import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity"

failures: list[str] = []


def check(condition: bool, message: str) -> None:
    if not condition:
        failures.append(message)


def text(path: pathlib.Path) -> str:
    if not path.is_file():
        failures.append(f"missing required file: {path.relative_to(ROOT)}")
        return ""
    return path.read_text(encoding="utf-8")


required = [
    UNITY / "ProjectSettings/ProjectVersion.txt",
    UNITY / "ProjectSettings/EditorBuildSettings.asset",
    UNITY / "Packages/manifest.json",
    UNITY / "Assets/IdeaZoo/Core/IdeaZooDomain.cs",
    UNITY / "Assets/IdeaZoo/Runtime/IdeaZooGame.cs",
    UNITY / "Assets/IdeaZoo/Runtime/IdeaZooWorld.cs",
    UNITY / "Assets/IdeaZoo/Runtime/IdeaZooActors.cs",
    UNITY / "Assets/IdeaZoo/Runtime/IdeaZooHud.cs",
    UNITY / "Assets/IdeaZoo/Runtime/SpecimenArchive.cs",
    UNITY / "Assets/IdeaZoo/Scenes/WhisperGate.unity",
    UNITY / "Assets/IdeaZoo/Scenes/WhisperGate.unity.meta",
]
for item in required:
    check(item.is_file(), f"missing required file: {item.relative_to(ROOT)}")

version = text(UNITY / "ProjectSettings/ProjectVersion.txt")
check("6000.5.0f1" in version, "project is not pinned to Unity 6.5")

manifest_path = UNITY / "Packages/manifest.json"
try:
    manifest = json.loads(text(manifest_path))
except json.JSONDecodeError as exc:
    manifest = {"dependencies": {}}
    failures.append(f"invalid package manifest: {exc}")

dependencies = manifest.get("dependencies", {})
for package in [
    "com.unity.cinemachine",
    "com.unity.timeline",
    "com.unity.addressables",
    "com.unity.inputsystem",
    "com.unity.render-pipelines.universal",
]:
    check(package in dependencies, f"missing production package: {package}")

build_settings = text(UNITY / "ProjectSettings/EditorBuildSettings.asset")
check("Assets/IdeaZoo/Scenes/WhisperGate.unity" in build_settings, "Whisper Gate is not in build settings")

core = text(UNITY / "Assets/IdeaZoo/Core/IdeaZooDomain.cs")
check("class IdeaZooCaseDirector" in core, "case state machine is missing")
check("Regex.Matches" in core, "strict token analysis is missing")
check("if (strength > 0)" in core, "zero-strength evidence may inflate evidence")
check("The idea did not change" in core, "no-op Molts are not rejected")

world = text(UNITY / "Assets/IdeaZoo/Runtime/IdeaZooWorld.cs")
for landmark in [
    "BuildWhisperGate",
    "BuildHatcheryRotunda",
    "BuildDesireYard",
    "BuildCommitmentPaddock",
    "BuildBurrowerTunnel",
    "BuildRefusalGate",
    "BuildMoltHouse",
    "BuildBoardWing",
    "BuildDecisionGarden",
    "BuildStaffAndAmbientLife",
]:
    check(landmark in world, f"world builder is missing {landmark}")
check(world.count("PrimitiveUnder(") >= 35, "world builder is still too sparse")

actors = text(UNITY / "Assets/IdeaZoo/Runtime/IdeaZooActors.cs")
check("class SafeAreaFitter" in actors, "safe-area handling is missing")
check("class MobileJoystick" in actors, "mobile joystick is missing")
check("ResetTransientInput" in actors, "interrupted touch recovery is missing")
check("class CreatureAssembler" in actors, "modular creature assembler is missing")
check("var evidence = EvidenceLevel" in actors, "Molt may reset accumulated evidence")

hud = text(UNITY / "Assets/IdeaZoo/Runtime/IdeaZooHud.cs")
check("0 · NO EVIDENCE YET" in hud and "3 · MONEY, PILOT OR COSTLY COMMITMENT" in hud, "explicit evidence strength controls are missing")
check("SafeAreaFitter" in hud, "HUD does not use safe-area fitting")
check("LockSubmit" in hud, "duplicate submit protection is missing")
check("RETURN WITHOUT RECORDING" in hud, "evidence cancellation path is missing")

runtime = text(UNITY / "Assets/IdeaZoo/Runtime/IdeaZooGame.cs")
check("RuntimeInitializeOnLoadMethod" in runtime, "starter scene cannot auto-boot")
check("_armedDecisionUntil" in runtime, "two-step ruling confirmation is missing")
check("_director.CancelMolt" in runtime, "Molt cancellation does not restore state")
check("_archive.Save" in runtime, "rulings are not archived")

archive = text(UNITY / "Assets/IdeaZoo/Runtime/SpecimenArchive.cs")
check(".corrupt-" in archive, "corrupt archive quarantine is missing")
check(".backup" in archive, "archive backup is missing")
check(".tmp" in archive, "atomic temporary write is missing")

bootstrap_files = list(ROOT.glob(".unity-bootstrap/**/*"))
check(not bootstrap_files, "temporary Unity transport files remain in the repository")

for cs in (UNITY / "Assets/IdeaZoo").rglob("*.cs"):
    source = cs.read_text(encoding="utf-8")
    check("NotImplementedException" not in source, f"unfinished code path in {cs.relative_to(ROOT)}")
    check("TODO" not in source, f"TODO remains in production source: {cs.relative_to(ROOT)}")

if failures:
    print("UNITY_FOUNDATION_VALIDATION_FAIL", file=sys.stderr)
    for failure in failures:
        print(f"- {failure}", file=sys.stderr)
    raise SystemExit(1)

print("UNITY_FOUNDATION_VALIDATION_PASS")

#!/usr/bin/env python3
from __future__ import annotations

import json
import pathlib
import re
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
    UNITY / "Assets/IdeaZoo/Presentation/CivicMaterials.cs",
    UNITY / "Assets/IdeaZoo/Presentation/CivicKit.cs",
    UNITY / "Assets/IdeaZoo/Presentation/CivicWorldArtPass.cs",
    UNITY / "Assets/IdeaZoo/Presentation/StaffEnsemble.cs",
    UNITY / "Assets/IdeaZoo/Presentation/SpecimenPresentation.cs",
    UNITY / "Assets/IdeaZoo/Presentation/PresentationCinematics.cs",
    UNITY / "Assets/IdeaZoo/Presentation/CivicAudio.cs",
    UNITY / "Assets/IdeaZoo/Presentation/GreyboxStaffCleanup.cs",
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
check("UnityEngine" not in core, "engine-independent domain imports Unity")

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
check("_rect = transform as RectTransform;" in actors, "joystick may add a duplicate RectTransform")
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

materials = text(UNITY / "Assets/IdeaZoo/Presentation/CivicMaterials.cs")
for surface in ["Paper", "Brass", "Clay", "Glass", "TealGlow", "Rust", "Moss"]:
    check(surface in materials, f"presentation material is missing {surface}")
check("BuildTexture" in materials and "Texture2D" in materials, "shared procedural surface textures are missing")

kit = text(UNITY / "Assets/IdeaZoo/Presentation/CivicKit.cs")
for module in ["LayeredFacade", "Rail", "PipeRun", "Workbench", "Vitrine", "Banner"]:
    check(module in kit, f"modular civic kit is missing {module}")

art = text(UNITY / "Assets/IdeaZoo/Presentation/CivicWorldArtPass.cs")
for department in [
    "01_WHISPER_GATE",
    "02_HATCHERY_ROTUNDA",
    "04_DESIRE_YARD",
    "05_COMMITMENT_PADDOCK",
    "06_BURROWER_TUNNEL",
    "07_REFUSAL_GATE",
    "08_MOLT_HOUSE",
    "09_SEALED_BOARD_WING",
    "10_DECISION_GARDEN",
]:
    check(department in art, f"presentation pass does not dress {department}")
for phrase in [
    "ASK BEFORE EXPLAINING",
    "INTEREST IS NOT A COMMITMENT",
    "WHO CLEANS UP WHEN IT WORKS?",
    "CLASSIFIED BEFORE HATCHING",
    "A RULING IS A BET ABOUT THE FUTURE",
]:
    check(phrase in art, f"environmental story phrase is missing: {phrase}")

staff = text(UNITY / "Assets/IdeaZoo/Presentation/StaffEnsemble.cs")
for specialist in ["Mara Rook", "Toma Reed", "Sefu Anik", "Elian Thread", "Sen Osei", "Nara Voss"]:
    check(specialist in staff, f"animated specialist is missing: {specialist}")
for tool in ["HatchFork", "ReleaseStaff", "AppetiteLens", "MoltSpool", "CounterfactualFrames", "MercyBell"]:
    check(tool in staff, f"specialist tool is missing: {tool}")

specimen = text(UNITY / "Assets/IdeaZoo/Presentation/SpecimenPresentation.cs")
for idea_class in ["Fleck", "Hand", "Mirror", "Teeth", "Swarm", "Weather", "Burrower"]:
    check(f"IdeaClass.{idea_class}" in specimen, f"authored specimen silhouette is missing {idea_class}")
for appetite in ["Attention", "Data", "Money", "Trust", "Obedience", "Labour", "Care", "Time"]:
    check(f"Appetite.{appetite}" in specimen, f"specimen appetite marking is missing {appetite}")
check("Authored_Hidden_Burden" in specimen, "hidden burden presentation is missing")
check("Guardrail_Rings" in specimen, "Molt guardrail presentation is missing")

cinematics = text(UNITY / "Assets/IdeaZoo/Presentation/PresentationCinematics.cs")
check("PlayableDirector" in cinematics and "PlayableAsset" in cinematics, "story sequences do not use Unity Playables")
check("CinemachineBrain" in cinematics and "CinemachineCamera" in cinematics, "Cinemachine bridge is missing")
for shot in ["Hatch", "Inspection", "Molt", "Decision", "Ruling"]:
    check(f"PresentationShot.{shot}" in cinematics, f"cinematic shot is missing: {shot}")
check("CinematicCanvas" in cinematics, "cinematic framing bars are missing")

sound = text(UNITY / "Assets/IdeaZoo/Presentation/CivicAudio.cs")
check("Civic_Ambience" in sound, "civic ambience is missing")
check("Hatch_Cue" in sound and "Ruling_Cue" in sound, "story audio cues are incomplete")

cleanup = text(UNITY / "Assets/IdeaZoo/Presentation/GreyboxStaffCleanup.cs")
check("STAFF_AND_AMBIENT_LIFE" in cleanup, "prototype staff replacement is missing")

bootstrap_files = list(ROOT.glob(".unity-bootstrap/**/*"))
check(not bootstrap_files, "temporary Unity transport files remain in the repository")

for cs in (UNITY / "Assets/IdeaZoo").rglob("*.cs"):
    source = cs.read_text(encoding="utf-8")
    check("NotImplementedException" not in source, f"unfinished code path in {cs.relative_to(ROOT)}")
    check("TODO" not in source, f"TODO remains in production source: {cs.relative_to(ROOT)}")
    stripped = re.sub(r'//.*?$|/\*.*?\*/|"(?:\\.|[^"\\])*"', '', source, flags=re.M | re.S)
    check(stripped.count("{") == stripped.count("}"), f"unbalanced braces in {cs.relative_to(ROOT)}")

if failures:
    print("UNITY_FOUNDATION_VALIDATION_FAIL", file=sys.stderr)
    for failure in failures:
        print(f"- {failure}", file=sys.stderr)
    raise SystemExit(1)

print("UNITY_FOUNDATION_VALIDATION_PASS")

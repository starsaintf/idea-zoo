#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
core = ROOT / "unity/Assets/IdeaZoo/HeroSlice/HeroSliceCore.cs"
world = ROOT / "unity/Assets/IdeaZoo/HeroSlice/HeroWorldProduction.cs"
performance = ROOT / "unity/Assets/IdeaZoo/HeroSlice/HeroPerformanceAndTransformation.cs"
editor = ROOT / "unity/Assets/IdeaZoo/Editor/HeroSlice/HeroSliceSceneBaker.cs"
tests = ROOT / "unity/Assets/IdeaZoo/Tests/EditMode/HeroSliceEditorTests.cs"
doc = ROOT / "unity/HERO_SLICE_PRODUCTION.md"

required_files = [core, world, performance, editor, tests, doc]
missing = [str(path.relative_to(ROOT)) for path in required_files if not path.exists()]
if missing:
    raise SystemExit("Missing hero-slice files: " + ", ".join(missing))

texts = {path.name: path.read_text(encoding="utf-8") for path in required_files}

checks = {
    "runtime auto installer": "RuntimeInitializeOnLoadMethod" in texts[core.name],
    "four authored districts": all(token in texts[world.name] for token in (
        "BuildZooEntrance", "BuildLanternFields", "BuildSilentStacks", "BuildEvidenceForge"
    )),
    "premium world language": all(token in texts[world.name] for token in (
        "THE IDEA ZOO", "LANTERN FIELDS", "SILENT STACKS", "EVIDENCE FORGE"
    )),
    "six visible creature stages": all(token in texts[core.name] for token in (
        "Unproven", "Observed", "Tested", "Trusted", "Burdened", "Transformed"
    )),
    "story state drives creature art": "Evaluate(IdeaProfile profile, CaseStage caseStage)" in texts[performance.name],
    "cinematic character beats": "CharacterGesture" in texts[performance.name] and "PresentationCameraRig" in texts[performance.name],
    "mobile adaptive budget": "HeroMobileBudgetMonitor" in texts[performance.name] and "AdaptiveQuality" in texts[performance.name],
    "four review scenes": "BakeReviewScenes" in texts[editor.name] and "HeroDistrictId" in texts[editor.name],
    "editor coverage": "HeroSliceBakerCreatesReviewableAssets" in texts[tests.name],
    "honest art boundary documented": "concept art" in texts[doc.name].lower() and "hero geometry" in texts[doc.name].lower(),
}

failed = [name for name, passed in checks.items() if not passed]
if failed:
    raise SystemExit("Hero-slice source contract failed: " + ", ".join(failed))

for path in (core, world, performance, editor, tests):
    text = path.read_text(encoding="utf-8")
    if text.count("{") != text.count("}"):
        raise SystemExit(f"Unbalanced braces in {path.relative_to(ROOT)}")

print("UNITY_CINEMATIC_HERO_SLICE_SOURCE_PASS")

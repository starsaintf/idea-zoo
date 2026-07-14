#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
core = ROOT / "unity/Assets/IdeaZoo/HeroSlice/HeroSliceCore.cs"
world = ROOT / "unity/Assets/IdeaZoo/HeroSlice/HeroWorldProduction.cs"
performance = ROOT / "unity/Assets/IdeaZoo/HeroSlice/HeroPerformanceAndTransformation.cs"
prefab_anchor = ROOT / "unity/Assets/IdeaZoo/HeroSlice/CinematicHeroSlicePrefabAnchor.cs"
review_anchor = ROOT / "unity/Assets/IdeaZoo/HeroSlice/HeroSliceReviewSceneAnchor.cs"
editor = ROOT / "unity/Assets/IdeaZoo/Editor/HeroSlice/HeroSliceSceneBaker.cs"
edit_tests = ROOT / "unity/Assets/IdeaZoo/Tests/EditMode/HeroSliceEditorTests.cs"
play_tests = ROOT / "unity/Assets/IdeaZoo/Tests/PlayMode/IdeaZooCloudPlayModeTests.cs"
doc = ROOT / "unity/HERO_SLICE_PRODUCTION.md"

required_files = [
    core, world, performance, prefab_anchor, review_anchor,
    editor, edit_tests, play_tests, doc,
]
missing = [str(path.relative_to(ROOT)) for path in required_files if not path.exists()]
if missing:
    raise SystemExit("Missing hero-slice files: " + ", ".join(missing))

texts = {path.name: path.read_text(encoding="utf-8") for path in required_files}
hero_runtime = "\n".join(texts[path.name] for path in (core, world, performance, prefab_anchor, review_anchor))

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
    "outcome-sensitive final art": all(token in texts[performance.name] for token in (
        "IsHopeful", "IsBreak", "IsHibernate", "SignalTransformation(Ruling? ruling)"
    )),
    "hibernate differs from destructive ruling": "hibernate ? CharacterEmotion.Concerned" in texts[performance.name]
        and "hibernate ? CharacterGesture.Inspect" in texts[performance.name],
    "cinematic ownership is deduplicated": "standardPresentationOwnsCaseShots" in texts[performance.name],
    "unique cinematic waits for shared camera": all(token in texts[performance.name] for token in (
        "ScheduleUniqueShot", "TryPlayPendingUniqueShot", "_camera.ShotActive"
    )),
    "mobile tiers remain authoritative": "Application.targetFrameRate =" not in texts[performance.name],
    "hero-only adaptive budget": "AdaptiveHeroQuality" in texts[performance.name] and "HeroPracticalLight" in texts[performance.name],
    "surface materials are shared": "MaterialFor(color, metallic, smoothness)" in texts[core.name]
        and "EqualHeroSurfacesShareMaterialInstances" in texts[edit_tests.name],
    "transparent surfaces are configured": all(token in texts[core.name] for token in (
        "_SURFACE_TYPE_TRANSPARENT", "RenderQueue.Transparent", "BlendMode.SrcAlpha", "BlendMode.OneMinusSrcAlpha"
    )) and "TransparentHeroSurfacesUseTransparentRendering" in texts[edit_tests.name],
    "imported creature emission uses stable material and property block": "SetEmissionOnly" in texts[core.name]
        and "EmissionReadyMaterialFor" in texts[core.name]
        and "MaterialPropertyBlock" in texts[core.name]
        and "renderer.material" not in texts[core.name]
        and "GetEntityId()" in texts[core.name]
        and "GetInstanceID()" not in texts[core.name]
        and "HeroSliceUtility.SetEmissionOnly(renderer" in texts[performance.name]
        and "ImportedCreatureEmissionUsesStableSharedMaterialAndPropertyBlocks" in texts[edit_tests.name],
    "manifest covers all creature families": "AllCreatureFamilies" in texts[editor.name]
        and "Manifest incorrectly narrows" in texts[edit_tests.name],
    "all mesh submeshes are counted": "for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)" in texts[core.name],
    "four review scenes": "BakeReviewScenes" in texts[editor.name] and "HeroDistrictId" in texts[editor.name],
    "serialization-safe anchors": "CinematicHeroSlicePrefabAnchor" in texts[editor.name]
        and "HeroSliceReviewSceneAnchor" in texts[editor.name],
    "playmode verifies hero boot": "HERO_SLICE_WORLD" in texts[play_tests.name]
        and "CountAnyComponents(cameraRigType)" in texts[play_tests.name],
    "modern non-ordering object lookup": "FindFirstObjectByType" not in hero_runtime
        and "FindObjectsSortMode" not in hero_runtime,
    "honest art boundary documented": "concept art" in texts[doc.name].lower()
        and "hero geometry" in texts[doc.name].lower(),
}

failed = [name for name, passed in checks.items() if not passed]
if failed:
    raise SystemExit("Hero-slice source contract failed: " + ", ".join(failed))

for path in required_files:
    if path.suffix != ".cs":
        continue
    text = path.read_text(encoding="utf-8")
    if text.count("{") != text.count("}"):
        raise SystemExit(f"Unbalanced braces in {path.relative_to(ROOT)}")

print("UNITY_CINEMATIC_HERO_SLICE_SOURCE_PASS")

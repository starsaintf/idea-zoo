#!/usr/bin/env python3
from __future__ import annotations
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
files = [
    ROOT / "unity/Assets/IdeaZoo/Creatures/CreatureProduction.cs",
    ROOT / "unity/Assets/IdeaZoo/Creatures/CreatureProductionLifecycle.cs",
]
failures: list[str] = []

def check(value: bool, message: str) -> None:
    if not value:
        failures.append(message)

sources = []
for path in files:
    if not path.is_file():
        failures.append(f"missing creature source: {path.relative_to(ROOT)}")
        sources.append("")
    else:
        sources.append(path.read_text(encoding="utf-8"))
source = "\n".join(sources)

for token in [
    "CreatureGenome", "CreatureProductionRig", "CreatureProductionLifecycle",
    "CreatureBodyFamily", "CreatureMotionFamily", "CreatureEmotion", "StableHash",
    "PRODUCTION_CREATURE_LAYER_", "LivingHiddenBurden", "LivingGuardrails",
    "Trust", "Hunger", "Fear", "Agitation", "CaptureBaseRotations"
]:
    check(token in source, f"creature production pass is missing {token}")
for family in ["Avian", "BurdenBeast", "Lantern", "Serpentine", "Choir"]:
    check(f"CreatureBodyFamily.{family}" in source, f"creature family is missing {family}")
for motion in ["Hop", "Trot", "Glide", "Coil", "Orbit"]:
    check(f"CreatureMotionFamily.{motion}" in source, f"creature motion is missing {motion}")
for idea_class in ["Fleck", "Hand", "Mirror", "Teeth", "Swarm", "Weather", "Burrower"]:
    check(f"IdeaClass.{idea_class}" in source, f"creature class production is missing {idea_class}")
for appetite in ["Attention", "Data", "Money", "Trust", "Obedience", "Labour", "Care", "Time"]:
    check(appetite in source, f"creature appetite anatomy is missing {appetite}")
check("5" in source and "4" in source and "Signature" in source, "creature genome does not expose combinatorial variants")
check("ReferenceEquals(_profile, _assembler.Profile)" in source, "new cases may reuse a stale creature genome")
check("DefaultExecutionOrder(1200)" in source, "motion stabilization may run before the production rig")
check("DisablePrototypeRenderers" in source, "prototype creature may remain visible under production anatomy")
check("FinalRuling" in source, "creature emotion does not respond to the final decision")
for path, text in zip(files, sources):
    stripped = re.sub(r'//.*?$|/\*.*?\*/|"(?:\\.|[^"\\])*"', '', text, flags=re.M | re.S)
    check(stripped.count("{") == stripped.count("}"), f"unbalanced braces in {path.relative_to(ROOT)}")
    check("TODO" not in text and "NotImplementedException" not in text, f"unfinished creature code in {path.relative_to(ROOT)}")

if failures:
    print("UNITY_CREATURE_PASS_FAIL", file=sys.stderr)
    for failure in failures:
        print("- " + failure, file=sys.stderr)
    raise SystemExit(1)
print("UNITY_CREATURE_PASS_PASS")

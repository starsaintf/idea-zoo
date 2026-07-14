#!/usr/bin/env python3
from __future__ import annotations
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
PATH = ROOT / "unity/Assets/IdeaZoo/Characters/CharacterProduction.cs"
failures: list[str] = []

def check(value: bool, message: str) -> None:
    if not value:
        failures.append(message)

if not PATH.is_file():
    failures.append("character production source is missing")
    source = ""
else:
    source = PATH.read_text(encoding="utf-8")

for token in [
    "KeeperAppearance", "CharacterPerformanceRig", "CharacterProductionDirector",
    "KeeperVisualBuilder", "CHILDRENS_JURY", "CharacterEmotion", "CharacterGesture",
    "AnimatorControllerParameterType", "Reset", "LookTarget", "RuntimeInitializeOnLoadMethod"
]:
    check(token in source, f"character pass is missing {token}")
for specialist in ["Mara", "Nara", "Toma"]:
    check(specialist in source, f"character emotional direction is missing {specialist}")
for jury in ["Lio", "Amara", "Kweku"]:
    check(jury in source, f"Children's Jury is missing {jury}")
for state in ["Curious", "Protective", "Concerned", "Defiant", "Grieving", "Hopeful"]:
    check(state in source, f"character emotion state is missing {state}")
check("PlayerPrefs" in source and "Save()" in source, "Keeper appearance persistence is incomplete")
check("GetComponentInChildren<Animator>" in source, "final humanoid Animator bridge is missing")
check("AnimateFallback" in source, "procedural animation fallback is missing")
check("FindObjectsByType<ProceduralSpecialist>" in source, "specialist upgrade path is missing")
check("FinalRuling" in source, "characters do not respond to final rulings")
stripped = re.sub(r'//.*?$|/\*.*?\*/|"(?:\\.|[^"\\])*"', '', source, flags=re.M | re.S)
check(stripped.count("{") == stripped.count("}"), "character production source has unbalanced braces")
check("TODO" not in source and "NotImplementedException" not in source, "unfinished character code remains")

if failures:
    print("UNITY_CHARACTER_PASS_FAIL", file=sys.stderr)
    for failure in failures:
        print("- " + failure, file=sys.stderr)
    raise SystemExit(1)
print("UNITY_CHARACTER_PASS_PASS")

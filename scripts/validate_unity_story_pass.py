#!/usr/bin/env python3
from __future__ import annotations
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
PATH = ROOT / "unity/Assets/IdeaZoo/Story/CampaignProduction.cs"
failures: list[str] = []

def check(value: bool, message: str) -> None:
    if not value:
        failures.append(message)

source = PATH.read_text(encoding="utf-8") if PATH.is_file() else ""
if not source:
    failures.append("campaign production source is missing")

for chapter in [
    "TwentyFourHours", "SmallThings", "WorkingAnimal", "TeethInsideTool", "CityOfMirrors",
    "MissingBurrowers", "WeatherWarning", "LockedRecord", "ZooIsBreeding", "LastRuling"
]:
    check(f"CampaignChapter.{chapter}" in source, f"campaign chapter is missing {chapter}")
for ending in ["Reform", "Release", "MoltTheZoo", "Sanctuary", "Destruction"]:
    check(f"CampaignEnding.{ending}" in source, f"campaign ending is missing {ending}")
for token in [
    "CampaignState", "RelationshipRecord", "FactionRecord", "PersistentSpecimenConsequence",
    "CampaignSaveService", "CampaignDirector", "CampaignWorldConsequences", "BoardEvidence",
    "FirstCreatureRecordId", "OptionalCases", "ApplyRulingConsequences", "AdvanceChapter",
    ".backup", ".corrupt-", ".tmp", "CAMPAIGN_CONSEQUENCES_", "ZooAppetiteOrgan_"
]:
    check(token in source, f"campaign pass is missing {token}")
for character in ["Mara Rook", "Toma Reed", "Sefu Anik", "Elian Thread", "Sen Osei", "Nara Voss", "Children's Jury"]:
    check(character in source, f"campaign relationship arc is missing {character}")
for faction in ["Hatchery", "ReleaseOffice", "Board", "WhiteRoom", "PublicWorks", "ChildrensJury"]:
    check(f"InstitutionFaction.{faction}" in source or faction in source, f"campaign faction is missing {faction}")
check(source.count("Beat(\"") >= 10, "campaign does not define ten authored chapters")
check(source.count("OptionalCases") >= 1 and source.count("A ") >= 8, "optional case library is too small")
check("profile.BoardClass != profile.Class" in source, "campaign ignores institutional misclassification")
check("profile.FinalRuling" in source, "campaign consequences ignore player rulings")
check("State.ZooNatureDiscovered = true" in source, "the Zoo's creature revelation is missing")
stripped = re.sub(r'//.*?$|/\*.*?\*/|"(?:\\.|[^"\\])*"', '', source, flags=re.M | re.S)
check(stripped.count("{") == stripped.count("}"), "campaign production source has unbalanced braces")
check("TODO" not in source and "NotImplementedException" not in source, "unfinished campaign code remains")

if failures:
    print("UNITY_STORY_PASS_FAIL", file=sys.stderr)
    for failure in failures:
        print("- " + failure, file=sys.stderr)
    raise SystemExit(1)
print("UNITY_STORY_PASS_PASS")

from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[1]
SLICE = ROOT / "unity" / "Assets" / "IdeaZoo" / "Rebuild" / "IdeaZooRebuiltSlice.cs"


class RebuiltGameplayContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source = SLICE.read_text(encoding="utf-8")

    def test_slice_boots_directly_into_third_person_gameplay(self):
        self.assertIn("RuntimeInitializeOnLoadMethod", self.source)
        self.assertIn("_keeper.SetLocked(false)", self.source)
        self.assertIn("SetMission(Mission.Track)", self.source)

    def test_core_loop_is_spatial_and_player_driven(self):
        for phase in ("Track", "Investigate", "Contain", "Decide", "Complete"):
            self.assertIn(phase, self.source)
        self.assertIn("_keeper.InteractRequested += Interact", self.source)
        self.assertIn("_keeper.LensChanged += LensChanged", self.source)
        self.assertIn("Vector3.Distance", self.source)

    def test_mission_has_three_clues_and_two_consequential_endings(self):
        self.assertEqual(self.source.count("AddClue("), 4)  # declaration plus three placements
        self.assertIn('BuildGate("RELEASE"', self.source)
        self.assertIn('BuildGate("CONTAIN"', self.source)
        self.assertIn("RULING: RELEASED WITH CONDITIONS", self.source)
        self.assertIn("RULING: CONTAINED", self.source)

    def test_webgl_materials_use_the_bundled_shader(self):
        self.assertIn('Resources.Load<Shader>("IdeaZooLit")', self.source)
        self.assertIn('Shader.Find("IdeaZoo/RuntimeLit")', self.source)


if __name__ == "__main__":
    unittest.main()

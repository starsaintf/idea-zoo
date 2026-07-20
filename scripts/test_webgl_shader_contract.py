from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[1]
MATERIALS = ROOT / "unity" / "Assets" / "IdeaZoo" / "Presentation" / "CivicMaterials.cs"
SHADER = ROOT / "unity" / "Assets" / "Resources" / "IdeaZooLit.shader"


class WebGlShaderContractTests(unittest.TestCase):
    def test_runtime_material_has_a_resources_backed_webgl_shader(self):
        self.assertTrue(SHADER.is_file(), "WebGL must ship a Resources-backed shader for runtime materials.")
        shader = SHADER.read_text(encoding="utf-8")
        materials = MATERIALS.read_text(encoding="utf-8")

        self.assertIn('Shader "IdeaZoo/RuntimeLit"', shader)
        self.assertIn('Resources.Load<Shader>("IdeaZooLit")', materials)
        self.assertIn('Shader.Find("IdeaZoo/RuntimeLit")', materials)
        self.assertIn('if (shader == null) throw new InvalidOperationException', materials)


if __name__ == "__main__":
    unittest.main()

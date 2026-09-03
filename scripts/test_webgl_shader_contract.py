from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[1]
MATERIALS = ROOT / "unity" / "Assets" / "IdeaZoo" / "Presentation" / "CivicMaterials.cs"
SHADER = ROOT / "unity" / "Assets" / "Resources" / "IdeaZooLit.shader"
RUNTIME_MATERIAL_FACTORIES = (
    ROOT / "unity" / "Assets" / "IdeaZoo" / "HeroSlice" / "HeroSliceCore.cs",
    ROOT / "unity" / "Assets" / "IdeaZoo" / "Presentation" / "ProceduralSpecialist.cs",
    ROOT / "unity" / "Assets" / "IdeaZoo" / "Runtime" / "IdeaZooActors.cs",
    ROOT / "unity" / "Assets" / "IdeaZoo" / "Characters" / "CharacterProduction.cs",
    ROOT / "unity" / "Assets" / "IdeaZoo" / "Runtime" / "IdeaZooWorld.cs",
)


class WebGlShaderContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.shader = SHADER.read_text(encoding="utf-8")
        cls.materials = MATERIALS.read_text(encoding="utf-8")

    def test_runtime_material_has_a_resources_backed_webgl_shader(self):
        self.assertTrue(SHADER.is_file(), "WebGL must ship a Resources-backed shader for runtime materials.")
        self.assertIn('Shader "IdeaZoo/RuntimeLit"', self.shader)
        self.assertIn('Resources.Load<Shader>("IdeaZooLit")', self.materials)
        self.assertIn('Shader.Find("IdeaZoo/RuntimeLit")', self.materials)
        self.assertIn('if (shader == null) throw new InvalidOperationException', self.materials)

    def test_shader_supports_the_material_library_contract(self):
        for required_property in (
            '_Metallic ("Metallic"',
            '_Smoothness ("Smoothness"',
            '_EmissionColor ("Emission"',
            '_ZWrite ("Depth Write"',
        ):
            self.assertIn(required_property, self.shader)

        self.assertIn('ZWrite [_ZWrite]', self.shader)
        self.assertIn('_EmissionColor.rgb', self.shader)
        self.assertIn('_Metallic', self.shader)
        self.assertIn('_Smoothness', self.shader)

    def test_shader_uses_urp_lighting_instead_of_flat_unlit_output(self):
        self.assertIn('"RenderPipeline" = "UniversalPipeline"', self.shader)
        self.assertIn('"LightMode" = "UniversalForward"', self.shader)
        self.assertIn('Lighting.hlsl', self.shader)
        self.assertIn('GetMainLight(', self.shader)
        self.assertIn('SampleSH(', self.shader)
        self.assertIn('normalOS : NORMAL', self.shader)
        self.assertNotIn('return tex2D(_BaseMap, input.uv) * _BaseColor;', self.shader)

    def test_glass_can_disable_depth_writes(self):
        self.assertIn('material.SetFloat("_ZWrite", 0f)', self.materials)
        self.assertIn('material.renderQueue = 3000', self.materials)
        self.assertIn('ZWrite [_ZWrite]', self.shader)

    def test_every_runtime_material_factory_uses_the_resources_shader(self):
        for path in RUNTIME_MATERIAL_FACTORIES:
            source = path.read_text(encoding="utf-8")
            self.assertIn(
                'Resources.Load<Shader>("IdeaZooLit")',
                source,
                f"{path.name} can create a null Material in stripped WebGL builds",
            )


if __name__ == "__main__":
    unittest.main()

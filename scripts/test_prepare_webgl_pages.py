from pathlib import Path
from tempfile import TemporaryDirectory
import unittest

from prepare_webgl_pages import prepare


class PrepareWebglPagesTests(unittest.TestCase):
    def test_makes_the_unity_shell_responsive_and_cache_safe(self):
        with TemporaryDirectory() as directory:
            index = Path(directory) / "index.html"
            index.write_text(
                '<html><head><title>Unity Web Player | unity</title></head><body>'
                '<div id="unity-container" class="unity-desktop"><canvas id="unity-canvas"></canvas>'
                '<div id="unity-build-title">unity</div></div>'
                '<script>var config={dataUrl:"IdeaZooWebGL.data.gz",frameworkUrl:"IdeaZooWebGL.framework.js.gz",'
                'codeUrl:"IdeaZooWebGL.wasm.gz",companyName: "DefaultCompany",productName: "unity"};</script>'
                '</body></html>',
                encoding="utf-8",
            )

            prepare(index)
            prepare(index)
            result = index.read_text(encoding="utf-8")

            self.assertIn('id="idea-zoo-responsive-shell"', result)
            self.assertEqual(1, result.count('id="idea-zoo-responsive-shell"'))
            self.assertIn("#unity-container.unity-desktop { position: fixed; inset: 0; width: 100%; height: 100%; transform: none; }", result)
            self.assertIn("#unity-canvas { display: block; width: 100% !important; height: 100% !important;", result)
            self.assertNotIn("IdeaZooWebGL.data.gz", result)
            self.assertIn("The Idea Zoo — Browser Playtest", result)


if __name__ == "__main__":
    unittest.main()

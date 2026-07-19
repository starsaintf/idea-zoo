import pathlib
import subprocess
import sys
import tempfile
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[1]
VALIDATOR = ROOT / "scripts" / "validate_webgl_publish_contract.py"


class WebglPublishContractTests(unittest.TestCase):
    def test_accepts_a_complete_decompressed_unity_build(self):
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            build = root / "Build"
            build.mkdir()
            (root / "index.html").write_text('var config = { dataUrl: buildUrl + "/IdeaZooWebGL.data", frameworkUrl: buildUrl + "/IdeaZooWebGL.framework.js", codeUrl: buildUrl + "/IdeaZooWebGL.wasm" };')
            for name in ("IdeaZooWebGL.loader.js", "IdeaZooWebGL.data", "IdeaZooWebGL.framework.js", "IdeaZooWebGL.wasm"):
                (build / name).write_bytes(b"ready")
            result = subprocess.run([sys.executable, str(VALIDATOR), str(root)], capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("WEBGL_PUBLISH_CONTRACT_PASS", result.stdout)

    def test_rejects_a_page_without_the_webassembly_payload(self):
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            build = root / "Build"
            build.mkdir()
            (root / "index.html").write_text('var config = { dataUrl: buildUrl + "/IdeaZooWebGL.data", frameworkUrl: buildUrl + "/IdeaZooWebGL.framework.js", codeUrl: buildUrl + "/IdeaZooWebGL.wasm" };')
            for name in ("IdeaZooWebGL.loader.js", "IdeaZooWebGL.data", "IdeaZooWebGL.framework.js"):
                (build / name).write_bytes(b"ready")
            result = subprocess.run([sys.executable, str(VALIDATOR), str(root)], capture_output=True, text=True)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("IdeaZooWebGL.wasm", result.stderr)


if __name__ == "__main__":
    unittest.main()

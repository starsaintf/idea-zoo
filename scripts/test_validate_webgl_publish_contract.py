import pathlib
import subprocess
import sys
import tempfile
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[1]
VALIDATOR = ROOT / "scripts" / "validate_webgl_publish_contract.py"
WEBGL_PUBLISH_WORKFLOWS = (
    ROOT / ".github" / "workflows" / "deploy-webgl-pages.yml",
    ROOT / ".github" / "workflows" / "publish-webgl-playtest.yml",
)


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

    def test_webgl_publication_workflows_check_out_the_validator_before_running_it(self):
        for workflow_path in WEBGL_PUBLISH_WORKFLOWS:
            workflow = workflow_path.read_text(encoding="utf-8")
            checkout = workflow.find("uses: actions/checkout@v4")
            validator = workflow.find("python scripts/validate_webgl_publish_contract.py")

            self.assertGreaterEqual(checkout, 0, f"{workflow_path.name} must check out the validator script")
            self.assertLess(checkout, validator, f"{workflow_path.name} must check out before the validator runs")


if __name__ == "__main__":
    unittest.main()

"""Exercise authenticated Gitea artifact redirects through the actual PowerShell helper."""
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import shutil
import subprocess
import tempfile
import threading
import unittest


@unittest.skipUnless(shutil.which("pwsh"), "PowerShell 7 is required for the private CI helper")
class ArtifactDownloadTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.output = Path(self.temp.name) / "artifact.zip"
        self.requests = []
        owner = self

        class Handler(BaseHTTPRequestHandler):
            def log_message(self, *args):
                pass

            def do_GET(self):
                owner.requests.append((self.path, self.headers.get("Authorization")))
                if self.path == "/zip":
                    self.send_response(302)
                    self.send_header("Location", "/zip/raw")
                    self.end_headers()
                elif self.path == "/foreign":
                    self.send_response(302)
                    self.send_header("Location", f"http://localhost:{self.server.server_port}/zip/raw")
                    self.end_headers()
                elif self.headers.get("Authorization") == "token test-only":
                    self.send_response(200)
                    self.end_headers()
                    self.wfile.write(b"original sealed archive bytes")
                else:
                    self.send_response(401)
                    self.end_headers()

        self.server = ThreadingHTTPServer(("127.0.0.1", 0), Handler)
        self.thread = threading.Thread(target=self.server.serve_forever, daemon=True)
        self.thread.start()
        self.addCleanup(self.server.server_close)
        self.addCleanup(self.thread.join)
        self.addCleanup(self.server.shutdown)

    def download(self, path):
        helper = str(Path(__file__).parents[1] / "ci-package-common.ps1").replace("'", "''")
        output = str(self.output).replace("'", "''")
        command = f". '{helper}'; Save-CIArtifact 'http://127.0.0.1:{self.server.server_port}{path}' 'test-only' '{output}'"
        return subprocess.run(["pwsh", "-NoProfile", "-Command", command], capture_output=True, text=True, timeout=20)

    def test_authenticated_same_origin_redirect_downloads_original_bytes(self):
        result = self.download("/zip")
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual("", result.stdout.strip(), "The helper must not pollute the retained-batch Boolean result.")
        self.assertEqual(b"original sealed archive bytes", self.output.read_bytes())
        self.assertEqual([("/zip", "token test-only"), ("/zip/raw", "token test-only")], self.requests)

    def test_changed_origin_is_rejected_before_forwarding_the_credential(self):
        result = self.download("/foreign")
        self.assertNotEqual(0, result.returncode)
        self.assertIn("changes origin", result.stderr)
        self.assertEqual([("/foreign", "token test-only")], self.requests)
        self.assertFalse(self.output.exists())

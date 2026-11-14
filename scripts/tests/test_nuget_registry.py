import contextlib
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import tempfile
import threading
import unittest
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

from test_nuget_release import package, COMMIT, VERSION

spec = importlib.util.spec_from_file_location("registry", Path(__file__).parents[1] / "nuget-registry.py")
registry = importlib.util.module_from_spec(spec)
spec.loader.exec_module(registry)


class MemoryRegistry:
    def __init__(self):
        self.files = {}
        self.uploads = []
        self.fail_after = None

    def fetch(self, entry, version):
        return self.files.get(entry["name"])

    def upload(self, path):
        if self.fail_after is not None and len(self.uploads) >= self.fail_after:
            raise RuntimeError("Interrupted upload")
        self.files[path.name] = path.read_bytes()
        self.uploads.append(path.name)


class RegistryTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.directory = Path(self.temp.name)
        self.repository = "example/PrivateLibrary"
        for symbols in (False, True):
            suffix = "snupkg" if symbols else "nupkg"
            (self.directory / f"Sphere10.Framework.{VERSION}.{suffix}").write_bytes(package(symbols=symbols))
        registry.seal(self.directory, VERSION, COMMIT, self.repository, registry.release.REPOSITORY_URL, [{"Id": "Sphere10.Framework"}])
        self.remote = MemoryRegistry()

    def publish(self, **kwargs):
        with contextlib.redirect_stdout(io.StringIO()):
            return registry.publish(self.directory, self.remote, self.repository, COMMIT, **kwargs)

    def test_batch_uploads_package_before_symbols_and_rerun_does_not_reupload(self):
        self.publish()
        self.assertTrue(self.remote.uploads[0].endswith(".nupkg"))
        self.assertTrue(self.remote.uploads[1].endswith(".snupkg"))
        self.publish()
        self.assertEqual(2, len(self.remote.uploads))

    def test_partial_batch_resumes_from_original_bytes(self):
        self.remote.fail_after = 1
        with self.assertRaises(RuntimeError):
            self.publish()
        self.remote.fail_after = None
        self.publish()
        self.assertEqual(2, len(self.remote.uploads))

    def test_symbol_conflict_prevents_any_upload(self):
        name = next(self.directory.glob("*.snupkg")).name
        self.remote.files[name] = b"different"
        with self.assertRaisesRegex(ValueError, "Conflicting"):
            self.publish()
        self.assertEqual([], self.remote.uploads)

    def test_verify_only_never_uploads(self):
        with self.assertRaisesRegex(ValueError, "missing"):
            self.publish(verify_only=True)
        self.assertEqual([], self.remote.uploads)

    def test_tampered_local_archive_is_rejected(self):
        next(self.directory.glob("*.nupkg")).write_bytes(b"changed")
        with self.assertRaisesRegex(ValueError, "changed"):
            self.publish()
        self.assertEqual([], self.remote.uploads)

    def test_wrong_repository_and_commit_are_rejected(self):
        with self.assertRaises(ValueError):
            registry.verify(self.directory, "other/repo", COMMIT)
        with self.assertRaises(ValueError):
            registry.verify(self.directory, self.repository, "b" * 40)

    def test_validation_versions_cannot_publish(self):
        path = self.directory / "release-manifest.json"
        manifest = json.loads(path.read_text())
        manifest["version"] = "0.0.0-validation.123.1"
        path.write_text(json.dumps(manifest))
        with self.assertRaisesRegex(ValueError, "Validation"):
            self.publish()

    def test_seal_requires_complete_evaluated_boundary(self):
        with self.assertRaises(ValueError):
            registry.seal(self.directory, VERSION, COMMIT, self.repository, registry.release.REPOSITORY_URL,
                          [{"Id": "Sphere10.Framework"}, {"Id": "Missing.Library"}])

    def test_private_main_and_tag_versions_use_the_same_release_rules(self):
        resolve = registry.release.release_version
        self.assertEqual("1.0.0-ci.1.99.2", resolve("owner/Private", "push", "refs/heads/main", "1.0.0", 1, 99, 2,
                                                expected_repository="owner/Private", default_branch="main"))
        self.assertEqual("1.1.0-rc.1", resolve("owner/Private", "push", "refs/tags/v1.1.0-rc.1", "1.0.0", 1, 99, 2,
                                             expected_repository="owner/Private", default_branch="main"))
        for event in ("pull_request", "workflow_dispatch"):
            with self.assertRaises(ValueError):
                resolve("owner/Private", event, "refs/heads/main", "1.0.0", 1, 99, 2, expected_repository="owner/Private", default_branch="main")

    def test_credentials_never_follow_a_different_registry_origin(self):
        client = object.__new__(registry.Registry)
        client.origin = ("https", "registry.example")
        with self.assertRaisesRegex(ValueError, "origin"):
            client.check_url("https://other.example/packages")

    def test_v3_http_upload_and_download_round_trip_including_symbols(self):
        stored, requests = {}, []
        class Handler(BaseHTTPRequestHandler):
            def log_message(self, *args):
                pass

            def do_GET(self):
                requests.append((self.command, self.headers.get("X-NuGet-ApiKey")))
                if self.path == "/index.json":
                    base = f"http://127.0.0.1:{self.server.server_port}"
                    body = json.dumps({"resources": [{"@type": kind, "@id": base + path} for kind, path in
                        [("PackageBaseAddress/3.0.0", "/package"), ("PackagePublish/2.0.0", "/upload"),
                         ("SymbolPackagePublish/4.9.0", "/symbols")]]}).encode()
                else:
                    body = stored.get(self.path.rsplit("/", 1)[-1])
                self.send_response(200 if body is not None else 404)
                self.end_headers()
                self.wfile.write(body or b"")

            def do_PUT(self):
                requests.append((self.command, self.headers.get("X-NuGet-ApiKey")))
                boundary = self.headers["Content-Type"].split("boundary=")[1].encode()
                body = self.rfile.read(int(self.headers["Content-Length"]))
                header, content = body.split(b"\r\n\r\n", 1)
                filename = header.split(b'filename="')[1].split(b'"')[0].decode()
                stored[filename.lower()] = content.removesuffix(b"\r\n--" + boundary + b"--\r\n")
                self.send_response(201)
                self.end_headers()
        server = ThreadingHTTPServer(("127.0.0.1", 0), Handler)
        self.addCleanup(server.server_close)
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()
        self.addCleanup(thread.join)
        self.addCleanup(server.shutdown)
        self.remote = registry.Registry(f"http://127.0.0.1:{server.server_port}/index.json", "test-credential")
        self.publish()
        self.publish(verify_only=True)
        self.assertEqual(2, sum(method == "PUT" for method, _ in requests))
        self.assertTrue(all(key == "test-credential" for _, key in requests))
        for path in self.directory.glob("*.nupkg"):
            self.assertEqual(path.read_bytes(), stored[path.name.lower()])


if __name__ == "__main__":
    unittest.main()

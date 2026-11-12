import importlib.util
import io
import json
from pathlib import Path
import tempfile
import unittest
import zipfile

spec = importlib.util.spec_from_file_location("nuget_release", Path(__file__).parents[1] / "nuget-release.py")
release = importlib.util.module_from_spec(spec)
spec.loader.exec_module(release)

COMMIT = "a" * 40
VERSION = "3.2.0-ci.123.456.1"


def package(package_id="Sphere10.Framework", version=VERSION, dependency="", symbols=False, payload=b"library"):
    data = io.BytesIO()
    with zipfile.ZipFile(data, "w") as archive:
        archive.writestr(package_id + ".nuspec", f'''<package><metadata>
<id>{package_id}</id><version>{version}</version><authors>Author</authors><description>Library</description>
<license type="expression">MIT</license><readme>README.md</readme><icon>icon.png</icon>
<repository type="git" url="{release.REPOSITORY_URL}" commit="{COMMIT}"/>
<dependencies>{dependency}</dependencies></metadata></package>''')
        if symbols:
            archive.writestr("lib/net11.0/Library.pdb", b"BSJB raw.githubusercontent.com/" + COMMIT.encode())
        else:
            archive.writestr("lib/net11.0/Library.dll", payload)
            for name in ("README.md", "icon.png", "LICENSE"):
                archive.writestr(name, name)
    return data.getvalue()


class VersionTests(unittest.TestCase):
    def resolve(self, ref, **kwargs):
        options = dict(repository=release.REPOSITORY, event="push", ref=ref, base="3.2.0", run_number=123, run_id=456, attempt=1)
        options.update(kwargs)
        return release.release_version(**options)

    def test_ci_unique_runs_and_reruns(self):
        self.assertEqual(VERSION, self.resolve("refs/heads/master"))
        self.assertNotEqual(VERSION, self.resolve("refs/heads/master", attempt=2))
        self.assertNotEqual(VERSION, self.resolve("refs/heads/master", run_id=789))

    def test_tags_are_exact_and_independent_of_next_base(self):
        for version in ("3.1.4", "3.2.0", "3.2.0-alpha.1", "3.2.0-rc.1"):
            with self.subTest(version=version):
                self.assertEqual(version, self.resolve("refs/tags/v" + version))

    def test_rejects_malformed_or_ambiguous_tags(self):
        for tag in ("3.2.0", "v3.2", "v03.2.0", "v3.2.0.1", "v3.2.0-rc.01", "v3.2.0+metadata", "v3.2.0-", "v3.2.0-a..1", "v3.2.0\n", "v70000.0.0"):
            with self.subTest(tag=tag), self.assertRaises(ValueError):
                self.resolve("refs/tags/" + tag)

    def test_pr_fork_dispatch_and_feature_branches_cannot_publish(self):
        for values in (dict(event="pull_request"), dict(event="workflow_dispatch"), dict(repository="other/Framework"), dict(ref="refs/heads/feature")):
            with self.subTest(values=values), self.assertRaises(ValueError):
                options = dict(ref="refs/heads/master")
                options.update(values)
                self.resolve(**options)


class PackageTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.directory = Path(self.temp.name)
        self.ids = ["Sphere10.Framework", "Sphere10.Framework.Windows"]
        for package_id in self.ids:
            dependency = '<dependency id="Sphere10.Framework" version="' + VERSION + '" />' if package_id.endswith("Windows") else ""
            for symbols in (False, True):
                suffix = "snupkg" if symbols else "nupkg"
                (self.directory / f"{package_id}.{VERSION}.{suffix}").write_bytes(package(package_id, dependency=dependency, symbols=symbols))

    def seal(self):
        return release.seal(self.directory, VERSION, COMMIT, [{"Id": name} for name in self.ids])

    def test_seal_and_verify_full_windows_batch(self):
        self.seal()
        self.assertEqual(4, len(release.verify(self.directory)["files"]))

    def test_rejects_missing_symbols_or_packages(self):
        next(self.directory.glob("*.snupkg")).unlink()
        with self.assertRaises(FileNotFoundError):
            self.seal()

    def test_rejects_mixed_versions_and_dependencies(self):
        path = self.directory / f"Sphere10.Framework.Windows.{VERSION}.nupkg"
        path.write_bytes(package("Sphere10.Framework.Windows", dependency='<dependency id="Sphere10.Framework" version="3.1.3"/>'))
        with self.assertRaisesRegex(ValueError, "dependency"):
            self.seal()

    def test_rejects_wrong_commit(self):
        with self.assertRaisesRegex(ValueError, "repository"):
            release.seal(self.directory, VERSION, "b" * 40, [{"Id": name} for name in self.ids])

    def test_rejects_modified_sealed_package(self):
        self.seal()
        path = next(self.directory.glob("*.nupkg"))
        path.write_bytes(path.read_bytes() + b"changed")
        with self.assertRaisesRegex(ValueError, "changed"):
            release.verify(self.directory)

    def test_accepts_repository_signature_and_zip_metadata_only(self):
        original = next(self.directory.glob("*.nupkg")).read_bytes()
        rewritten = io.BytesIO()
        with zipfile.ZipFile(rewritten, "w", compression=zipfile.ZIP_DEFLATED) as archive:
            for name, data in reversed(list(release.archive_entries(original).items())):
                archive.writestr(name, data)
            archive.writestr(".signature.p7s", b"NuGet repository signature")
        self.assertEqual(release.archive_entries(original), release.archive_entries(rewritten.getvalue()))

    def test_partial_batch_resumes_and_verifies_symbols(self):
        manifest = self.seal()
        remote = {}
        first = manifest["files"][0]
        remote[release.remote_url(first, VERSION)] = (self.directory / first["name"]).read_bytes()
        pushed = []

        def push(path):
            pushed.append(path.name)
            entry = next(entry for entry in manifest["files"] if entry["name"] == path.name)
            remote[release.remote_url(entry, VERSION)] = path.read_bytes()
            return 0

        release.publish(self.directory, push, fetch=remote.get, wait_seconds=0)
        self.assertEqual(3, len(pushed))
        release.publish(self.directory, lambda path: self.fail("An identical rerun must not push"), fetch=remote.get, wait_seconds=0)

    def test_conflict_in_late_package_prevents_all_pushes(self):
        manifest = self.seal()
        entry = manifest["files"][-2]
        remote = {release.remote_url(entry, VERSION): package(entry["id"], payload=b"different")}
        with self.assertRaisesRegex(ValueError, "Conflicting"):
            release.publish(self.directory, lambda path: self.fail("Preflight must reject the entire batch"), fetch=remote.get, wait_seconds=0)

    def test_push_race_requires_identical_contents(self):
        manifest = self.seal()
        remote = {}

        def race(path):
            entry = next(entry for entry in manifest["files"] if entry["name"] == path.name)
            remote[release.remote_url(entry, VERSION)] = path.read_bytes()
            return 1

        release.publish(self.directory, race, fetch=remote.get, wait_seconds=0)

    def test_unreadable_failure_is_not_silently_skipped(self):
        self.seal()
        with self.assertRaisesRegex(RuntimeError, "Push failed"):
            release.publish(self.directory, lambda path: 1, fetch=lambda url: None, wait_seconds=0)

    def test_disposable_validation_batch_cannot_publish(self):
        manifest = self.seal()
        manifest["version"] = "0.0.0-validation.123.1"
        (self.directory / "release-manifest.json").write_text(json.dumps(manifest))
        with self.assertRaisesRegex(ValueError, "never be published"):
            release.publish(self.directory, lambda path: self.fail("No upload is allowed"), fetch=lambda url: self.fail("No feed access is needed"))


if __name__ == "__main__":
    unittest.main()

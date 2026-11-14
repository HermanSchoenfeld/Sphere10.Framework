#!/usr/bin/env python3
"""Version, seal and safely resume a Framework NuGet publication (stdlib only)."""
import argparse
import hashlib
import io
import json
import os
from pathlib import Path
import re
import subprocess
import time
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

REPOSITORY = "HermanSchoenfeld/Sphere10.Framework"
REPOSITORY_URL = "https://github.com/" + REPOSITORY
PACKAGE_ID = re.compile(r"Sphere10\.(?:Framework(?:\.[A-Za-z][A-Za-z0-9_-]*)*|HashLib4CSharp)\Z")
SEMVER = re.compile(r"(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?\Z")


def validate_version(value):
    match = SEMVER.fullmatch(value)
    if not match or any(int(n) > 65534 for n in match.groups()[:3]):
        raise ValueError("Version must be major.minor.patch with an optional SemVer prerelease suffix (assembly components <= 65534).")
    if match[4] and any(s.isdigit() and len(s) > 1 and s[0] == "0" for s in match[4].split(".")):
        raise ValueError("Numeric prerelease identifiers cannot have leading zeros.")
    return value


def release_version(repository, event, ref, base, run_number, run_id, attempt, expected_repository=REPOSITORY, default_branch="master"):
    validate_version(base)
    if "-" in base:
        raise ValueError("VersionPrefix must be the numeric next release version.")
    if repository != expected_repository or event != "push":
        raise ValueError("Only push events in the intended repository may publish.")
    if ref == "refs/heads/" + default_branch:
        values = [str(run_number), str(run_id), str(attempt)]
        if any(not re.fullmatch(r"[1-9][0-9]*", s) for s in values):
            raise ValueError("A CI version requires positive run number, run ID and attempt.")
        return base + "-ci." + ".".join(values)
    if ref.startswith("refs/tags/v"):
        return validate_version(ref[len("refs/tags/v"):])
    raise ValueError("Publishing requires master or a valid v-prefixed version tag.")


def archive_entries(content):
    """Compare uncompressed payloads, ignoring only NuGet's added repository signature."""
    result = {}
    with zipfile.ZipFile(io.BytesIO(content)) as archive:
        for entry in archive.infolist():
            name = entry.filename
            if entry.is_dir() or name == ".signature.p7s":
                continue
            if name in result or name.startswith("/") or "\\" in name or ".." in name.split("/"):
                raise ValueError("Ambiguous package entry: " + name)
            result[name] = archive.read(entry)
    return result


def metadata(entries):
    manifests = [data for name, data in entries.items() if "/" not in name and name.endswith(".nuspec")]
    if len(manifests) != 1 or len(manifests[0]) > 1048576 or b"<!DOCTYPE" in manifests[0].upper():
        raise ValueError("Expected one bounded root nuspec without a DTD.")
    root = ET.fromstring(manifests[0])
    for node in root.iter():
        node.tag = node.tag.split("}")[-1]
    result = root.find("metadata")
    if result is None:
        raise ValueError("Missing package metadata.")
    return result


def seal(directory, version, commit, projects):
    validate_version(version)
    if not re.fullmatch(r"[0-9a-f]{40}", commit):
        raise ValueError("Expected a full source commit.")
    expected = {project["Id"] for project in projects}
    packages = sorted(directory.glob("*.nupkg"))
    if len(packages) != len(expected) or not expected:
        raise ValueError("Package count differs from the evaluated public projects.")
    found = set()
    files = []
    for package in packages:
        entries = archive_entries(package.read_bytes())
        spec = metadata(entries)
        package_id = spec.findtext("id")
        if package_id not in expected or package_id in found or not PACKAGE_ID.fullmatch(package_id):
            raise ValueError("Unexpected or duplicate package: " + str(package_id))
        found.add(package_id)
        if spec.findtext("version") != version or package.name != f"{package_id}.{version}.nupkg":
            raise ValueError("Package version or filename mismatch: " + package.name)
        repository = spec.find("repository")
        if repository is None or repository.get("commit") != commit or repository.get("url", "").rstrip("/") != REPOSITORY_URL or repository.get("type") != "git":
            raise ValueError("Incorrect source repository metadata: " + package.name)
        for field in ("authors", "description", "license", "readme", "icon"):
            if not spec.findtext(field):
                raise ValueError(f"{package_id}: missing {field}")
        for name in (spec.findtext("readme"), spec.findtext("icon"), "LICENSE"):
            if name not in entries:
                raise ValueError(f"{package_id}: missing {name}")
        for dependency in spec.findall(".//dependency"):
            dep_id, dep_version = dependency.get("id"), dependency.get("version")
            if dep_id.startswith("Sphere10."):
                # NuGet emits a minimum range for normal project references.
                if dep_id not in expected or dep_version not in (version, f"[{version}]", f"[{version}, )", f"[{version},)"):
                    raise ValueError(f"Internal dependency mismatch: {package_id} -> {dep_id} {dep_version}")
        symbols = package.with_suffix(".snupkg")
        symbol_entries = archive_entries(symbols.read_bytes())
        symbol_spec = metadata(symbol_entries)
        if symbol_spec.findtext("id") != package_id or symbol_spec.findtext("version") != version:
            raise ValueError("Symbol identity mismatch: " + symbols.name)
        pdbs = [data for name, data in symbol_entries.items() if name.endswith(".pdb")]
        if not pdbs or any(not data.startswith(b"BSJB") for data in pdbs):
            raise ValueError("Portable PDBs required: " + symbols.name)
        # SourceLink is emitted by the SDK's GitHub provider into each portable PDB.
        if any(commit.encode() not in data or b"raw.githubusercontent.com" not in data for data in pdbs):
            raise ValueError("Symbols must link to this source commit: " + symbols.name)
        for path in (package, symbols):
            files.append({"name": path.name, "id": package_id, "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
    if found != expected or len(list(directory.glob("*.snupkg"))) != len(expected):
        raise ValueError("Incomplete package/symbol set.")
    manifest = {"schema": 1, "repository": REPOSITORY, "version": version, "commit": commit, "files": files}
    (directory / "release-manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    return manifest


def verify(directory, repository=REPOSITORY):
    manifest = json.loads((directory / "release-manifest.json").read_text(encoding="utf-8"))
    if manifest["schema"] != 1 or manifest["repository"] != repository:
        raise ValueError("Unexpected release manifest.")
    actual = {p.name for pattern in ("*.nupkg", "*.snupkg") for p in directory.glob(pattern)}
    expected = {entry["name"] for entry in manifest["files"]}
    if actual != expected or len(expected) != len(manifest["files"]):
        raise ValueError("Package set differs from the sealed manifest.")
    for entry in manifest["files"]:
        if Path(entry["name"]).name != entry["name"] or hashlib.sha256((directory / entry["name"]).read_bytes()).hexdigest() != entry["sha256"]:
            raise ValueError("Package changed after validation: " + entry["name"])
    return manifest


def download(url):
    try:
        with urllib.request.urlopen(urllib.request.Request(url, headers={"User-Agent": "Sphere10-Framework-release"}), timeout=90) as response:
            return response.read()
    except urllib.error.HTTPError as error:
        if error.code == 404:
            return None
        raise


def remote_url(entry, version):
    package_id, version = entry["id"].lower(), version.lower()
    if entry["name"].endswith(".snupkg"):
        return f"https://www.nuget.org/api/v2/symbolpackage/{package_id}/{version}"
    return f"https://api.nuget.org/v3-flatcontainer/{package_id}/{version}/{package_id}.{version}.nupkg"


def matches_remote(directory, entry, version, fetch=download):
    content = fetch(remote_url(entry, version))
    if content is None:
        return False
    if archive_entries(content) != archive_entries((directory / entry["name"]).read_bytes()):
        raise ValueError("Conflicting published contents; refusing to continue: " + entry["name"])
    return True


def publish(directory, push, fetch=download, wait_seconds=900, sleep=time.sleep):
    manifest = verify(directory)
    if manifest["version"].startswith("0.0.0-validation."):
        raise ValueError("Disposable validation packages must never be published.")
    # Check the whole existing batch before the first mutation, including symbols.
    existing = {entry["name"] for entry in manifest["files"] if matches_remote(directory, entry, manifest["version"], fetch)}
    pending = []
    for entry in sorted(manifest["files"], key=lambda x: (x["name"].endswith(".snupkg"), x["name"])):
        if entry["name"] in existing:
            print("Verified existing " + entry["name"], flush=True)
            continue
        print("Publishing " + entry["name"], flush=True)
        # No --skip-duplicate. An error/race is accepted only after payload comparison.
        status = push(directory / entry["name"])
        pending.append((entry, status))
        if status != 0:
            deadline = time.monotonic() + wait_seconds
            while not matches_remote(directory, entry, manifest["version"], fetch):
                if time.monotonic() >= deadline:
                    raise RuntimeError("Push failed or package is not yet readable. Retry the original artifact: " + entry["name"])
                sleep(15)
    deadline = time.monotonic() + wait_seconds
    while pending:
        pending = [(entry, status) for entry, status in pending if not matches_remote(directory, entry, manifest["version"], fetch)]
        if pending:
            if time.monotonic() >= deadline:
                raise RuntimeError("NuGet indexing is pending. Retry the original artifact to verify publication.")
            sleep(15)
    print(f"Verified {len(manifest['files'])} published packages/symbols for {manifest['version']}.", flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    version_parser = commands.add_parser("version")
    version_parser.add_argument("--props", type=Path, required=True)
    version_parser.add_argument("--output", type=Path)
    version_parser.add_argument("--repository", default=REPOSITORY)
    version_parser.add_argument("--branch", default="master")
    for command in ("seal", "verify", "preflight", "publish"):
        child = commands.add_parser(command)
        child.add_argument("directory", type=Path)
        if command == "seal":
            child.add_argument("--version", required=True)
            child.add_argument("--commit", required=True)
    args = parser.parse_args()
    if args.command == "version":
        base = ET.parse(args.props).findtext(".//VersionPrefix")
        version = release_version(os.getenv("GITHUB_REPOSITORY"), os.getenv("GITHUB_EVENT_NAME"), os.getenv("GITHUB_REF", ""), base,
                                  os.getenv("GITHUB_RUN_NUMBER"), os.getenv("GITHUB_RUN_ID"), os.getenv("GITHUB_RUN_ATTEMPT"), args.repository, args.branch)
        print(version)
        if args.output:
            with args.output.open("a", encoding="utf-8") as output:
                output.write("version=" + version + "\n")
    elif args.command == "seal":
        projects = json.loads((args.directory / "package-projects.json").read_text(encoding="utf-8-sig"))
        seal(args.directory, args.version, args.commit, projects)
    elif args.command == "verify":
        print(json.dumps(verify(args.directory), indent=2))
    elif args.command == "preflight":
        manifest = verify(args.directory)
        for entry in manifest["files"]:
            matches_remote(args.directory, entry, manifest["version"])
        print("Publication preflight passed.")
    else:
        key = os.environ.get("NUGET_API_KEY")
        if not key:
            raise ValueError("NUGET_API_KEY must contain the short-lived OIDC credential.")

        def push(path):
            # Discover the symbol endpoint from V3. --no-symbols also suppresses
            # explicit snupkg pushes, so use it only to separate the nupkg batch.
            arguments = ["dotnet", "nuget", "push", str(path), "--source", "https://api.nuget.org/v3/index.json", "--api-key", key]
            if path.suffix == ".nupkg":
                arguments.append("--no-symbols")
            result = subprocess.run(arguments, capture_output=True, text=True)
            print((result.stdout + result.stderr).replace(key, "***"), flush=True)
            return result.returncode

        publish(args.directory, push)


if __name__ == "__main__":
    main()

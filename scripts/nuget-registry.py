#!/usr/bin/env python3
"""Seal and publish NuGet batches to a private V3 registry without repacking."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import urllib.error
import urllib.parse
import urllib.request
import uuid

spec = importlib.util.spec_from_file_location("nuget_release", Path(__file__).with_name("nuget-release.py"))
release = importlib.util.module_from_spec(spec)
spec.loader.exec_module(release)


def seal(directory, version, commit, repository, repository_url, projects):
    release.validate_version(version)
    if not re.fullmatch(r"[0-9a-f]{40}", commit):
        raise ValueError("Expected a full source commit.")
    expected = {p["Id"] for p in projects}
    packages = sorted(directory.glob("*.nupkg"))
    found, files = set(), []
    for package in packages:
        meta = release.metadata(release.archive_entries(package.read_bytes()))
        identity = meta.findtext("id")
        if identity not in expected or identity in found:
            raise ValueError("Unexpected or duplicate package: " + str(identity))
        if meta.findtext("version") != version or package.name != f"{identity}.{version}.nupkg":
            raise ValueError("Package identity differs from the selected version.")
        source = meta.find("repository")
        if source is None or source.get("commit") != commit or source.get("url") != repository_url or source.get("type") != "git":
            raise ValueError("Package source provenance does not match the checkout.")
        for dependency in meta.findall(".//dependency"):
            if dependency.get("id") in expected and dependency.get("version") != f"[{version}]":
                raise ValueError("Internal package dependencies must pin this exact batch.")
        found.add(identity)
        symbols = package.with_suffix(".snupkg")
        symbol_entries = release.archive_entries(symbols.read_bytes())
        symbol_meta = release.metadata(symbol_entries)
        if symbol_meta.findtext("id") != identity or symbol_meta.findtext("version") != version:
            raise ValueError("Symbol identity mismatch.")
        if not any(name.endswith(".pdb") and data.startswith(b"BSJB") for name, data in symbol_entries.items()):
            raise ValueError("Expected portable symbols.")
        for path in (package, symbols):
            files.append({"name": path.name, "id": identity, "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
    if not expected or found != expected or len(list(directory.glob("*.snupkg"))) != len(expected):
        raise ValueError("Package batch differs from the evaluated packable project set.")
    manifest = {"schema": 1, "repository": repository, "version": version, "commit": commit, "files": files}
    (directory / "release-manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    return manifest


def verify(directory, repository, commit=None):
    manifest = release.verify(directory, repository=repository)
    release.validate_version(manifest["version"])
    if commit is not None and manifest["commit"] != commit:
        raise ValueError("Artifact belongs to a different source commit.")
    return manifest


class Registry:
    def __init__(self, service_index, token):
        self.origin = urllib.parse.urlsplit(service_index)[:2]
        if self.origin[0] not in ("http", "https") or not self.origin[1] or not token:
            raise ValueError("A registry URL and credential are required.")
        self.token = token
        index = json.loads(self.request(service_index))
        self.resources = {r["@type"]: r["@id"].rstrip("/") for r in index["resources"]}
        for kind in ("PackageBaseAddress/3.0.0", "PackagePublish/2.0.0", "SymbolPackagePublish/4.9.0"):
            self.check_url(self.resources[kind])

    def check_url(self, url):
        if urllib.parse.urlsplit(url)[:2] != self.origin:
            raise ValueError("Registry endpoint changes origin; refusing to forward credentials.")

    def request(self, url, data=None, headers=None, method="GET"):
        self.check_url(url)
        values = {"Authorization": "token " + self.token, "X-NuGet-ApiKey": self.token, "User-Agent": "Sphere10-NuGet-CI"}
        values.update(headers or {})
        # Registry endpoints are local to the service index. Never forward credentials on redirects.
        class NoRedirect(urllib.request.HTTPRedirectHandler):
            def redirect_request(self, request, fp, code, message, response_headers, new_url):
                return None
        opener = urllib.request.build_opener(NoRedirect)
        try:
            with opener.open(urllib.request.Request(url, data=data, headers=values, method=method), timeout=180) as response:
                return response.read()
        except urllib.error.HTTPError as error:
            if error.code == 404 and method == "GET":
                return None
            raise RuntimeError(f"Registry {method} failed with HTTP {error.code}.") from None

    def fetch(self, entry, version):
        identity, version = entry["id"].lower(), version.lower()
        suffix = Path(entry["name"]).suffix
        base = self.resources["PackageBaseAddress/3.0.0"]
        return self.request(f"{base}/{identity}/{version}/{identity}.{version}{suffix}")

    def upload(self, path):
        kind = "SymbolPackagePublish/4.9.0" if path.suffix == ".snupkg" else "PackagePublish/2.0.0"
        boundary = "nuget-" + uuid.uuid4().hex
        body = (f'--{boundary}\r\nContent-Disposition: form-data; name="package"; filename="{path.name}"\r\n'
                'Content-Type: application/octet-stream\r\n\r\n').encode() + path.read_bytes() + f"\r\n--{boundary}--\r\n".encode()
        self.request(self.resources[kind], data=body, headers={"Content-Type": "multipart/form-data; boundary=" + boundary}, method="PUT")


def publish(directory, registry, repository, commit=None, verify_only=False):
    manifest = verify(directory, repository, commit)
    if manifest["version"].startswith("0.0.0-validation."):
        raise ValueError("Validation packages cannot be published.")
    existing = set()
    def matches(entry):
        content = registry.fetch(entry, manifest["version"])
        if content is None:
            return False
        if hashlib.sha256(content).hexdigest() != entry["sha256"]:
            raise ValueError("Conflicting immutable registry contents: " + entry["name"])
        return True
    # Check the entire batch, including symbols, before making any mutation.
    for entry in manifest["files"]:
        if matches(entry):
            existing.add(entry["name"])
        elif verify_only:
            raise ValueError("Registry file is missing: " + entry["name"])
    for entry in sorted(manifest["files"], key=lambda e: (e["name"].endswith(".snupkg"), e["name"])):
        if entry["name"] not in existing:
            try:
                registry.upload(directory / entry["name"])
            except RuntimeError:
                # A concurrent identical publisher may have won; differing bytes still fail.
                if not matches(entry):
                    raise
        if not matches(entry):
            raise RuntimeError("Publication is incomplete; retry this original batch: " + entry["name"])
        print("Verified " + entry["name"], flush=True)
    return manifest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    for name in ("seal", "verify", "publish", "verify-remote"):
        command = commands.add_parser(name)
        command.add_argument("directory", type=Path)
        command.add_argument("--repository", required=True)
        command.add_argument("--commit")
        if name == "seal":
            command.add_argument("--version", required=True)
            command.add_argument("--repository-url", required=True)
        if name in ("publish", "verify-remote"):
            command.add_argument("--source", default=os.getenv("NUGET_REGISTRY_URL"))
    args = parser.parse_args()
    if args.command == "seal":
        projects = json.loads((args.directory / "package-projects.json").read_text(encoding="utf-8-sig"))
        seal(args.directory, args.version, args.commit, args.repository, args.repository_url, projects)
    elif args.command == "verify":
        manifest = verify(args.directory, args.repository, args.commit)
        print(f"Verified {len(manifest['files'])} files for {manifest['version']} at {manifest['commit']}.")
    else:
        if not args.source:
            raise ValueError("Set NUGET_REGISTRY_URL or --source.")
        registry = Registry(args.source, os.environ.get("NUGET_REGISTRY_TOKEN"))
        publish(args.directory, registry, args.repository, args.commit, args.command == "verify-remote")


if __name__ == "__main__":
    main()

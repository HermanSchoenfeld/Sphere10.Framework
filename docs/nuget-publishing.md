# Framework NuGet publishing

The [Build workflow](../.github/workflows/main.yml) publishes the public Framework
libraries to NuGet.org from `HermanSchoenfeld/Sphere10.Framework`. It includes the
Windows libraries and `Sphere10.Framework.Web.VisualRenderer`. The public package
boundary is every distinct packable project under `src/` in the Windows solution.
Legacy mobile projects, internal runtime code, tests and demos are not packages.

## Private registry copy

The `publish-private` job sends the same validated, sealed package and symbol
archives to an additional NuGet V3 registry. It waits for the build, unit tests,
browser tests and package validation, independently of the NuGet.org upload. PRs,
feature branches and manual validation runs cannot publish. The public NuGet.org
workflow and Trusted Publishing policy are unchanged.

Configure the `private-packages` GitHub environment with `NUGET_REGISTRY_URL` as
a variable and `NUGET_REGISTRY_TOKEN` as a secret. Restrict that environment to
`master` and `v*` tags. The `sphere10-gitea-publisher` runner must reach the registry
and provide Git, Python 3 and the Node runtime used by Actions. It only receives
the publishing job; the existing hosted jobs perform the builds and tests.

[`nuget-registry.py`](../scripts/nuget-registry.py) checks the complete existing
batch before uploading anything, verifies downloaded archive hashes and resumes
only identical partial batches. Package and symbol files are both checked. Retry
the failed publishing job to reuse its original artifact. Conflicting versions
fail; expired tag artifacts must be recovered rather than replaced with a rebuild.
Registry endpoints must share the service index's origin, and credentials are
never forwarded through redirects.

The script also supports other repositories' evaluated package manifests through
its `seal` command. [`ci-package-common.ps1`](../scripts/ci-package-common.ps1)
provides package selection, isolated restore configuration, consumer archive-hash
checks and Gitea tag-artifact recovery. Products own their build/test selection
and pin these shared tools to a reviewed Framework commit.

## Stable and prerelease channels

Successful pushes to `master` produce a version of the form
`3.2.0-ci.<run-number>.<run-id>.<attempt>`. The numeric base comes from
`VersionPrefix` in `Directory.Build.props`, which records the next intended
release. For example, one run can publish `3.2.0-ci.123.43225298194.1`.

Run IDs distinguish concurrent runs, and attempts distinguish complete reruns.
Retrying only a failed publishing job reuses the original package job's version
and artifact. CI does not overwrite packages. Workflow concurrency preserves a
running batch, while newer pending pushes can supersede older pending runs.
The publication job serializes access to the feed.

Every suffixed version is a NuGet prerelease, including `ci`, `alpha` and `rc`.
Normal stable-only update checks exclude these versions. Alpha and release
candidate packages are deliberate tag releases, independent of automatic CI builds.
NuGet orders suffixes by SemVer, not by a configurable channel ranking, so consumers
that want only CI builds should use an explicit `ci` pattern or exact version.

## Tagged releases

A pushed tag builds that tagged commit and publishes the exact tag without its
leading `v`:

| Tag | NuGet version |
|---|---|
| `v3.1.4` | `3.1.4` |
| `v3.2.0-alpha.1` | `3.2.0-alpha.1` |
| `v3.2.0-rc.1` | `3.2.0-rc.1` |
| `v3.2.0` | `3.2.0` |

Three numeric components and an optional valid SemVer prerelease suffix are
required. Leading zeros, a fourth numeric component, empty identifiers and build
metadata are rejected. NuGet ignores build metadata for version identity, so it
cannot provide collision protection. Numeric components are limited to 65534 to
remain representable in the assembly version. A tag does not need to match the
checkout's next-release base, which permits maintenance releases from their own
commits. No fallback version is substituted.

After reviewing the intended commit and its checks:

```powershell
git tag -a v3.2.0-alpha.1 <full-commit-sha> -m "Sphere10 Framework 3.2.0 alpha 1"
git push <remote-for-HermanSchoenfeld/Sphere10.Framework> refs/tags/v3.2.0-alpha.1
```

Use `v3.2.0` for the stable release. Do not move a published tag. Advance
`VersionPrefix` to the next intended version, such as `3.2.1`, in a separate
reviewed change after the stable release. Review third-party prerelease dependencies
in `Directory.Packages.props` before announcing a stable release.

## Checks and package provenance

Publication waits for the build, both .NET 10/11 NUnit jobs, wizard browser tests,
release-contract tests and full Windows package validation. Pull requests, forks,
feature branches and manual workflow dispatches cannot enter the publishing job.
Their package checks use local `0.0.0-validation.*` artifacts, which are never
published by this workflow.

Packing uses one explicit version for all projects and internal dependencies. It
fixes `BuildRevision=0` rather than placing an unbounded workflow number in a
16-bit assembly component. Assembly/file versions use the numeric release base.
Informational versions contain the package version and full source commit.
Nuspec repository metadata identifies the GitHub source and commit, including for
the HashLib fork while preserving its upstream author and license metadata.

`release-manifest.json` records every package and symbol archive's SHA-256 hash.
The validator checks the evaluated package set, versions, internal dependencies,
license/readme/icon files, portable symbols and source-commit links. The established
consumer checks restore from an isolated local feed and empty cache, build the
desktop and library consumers, execute the renderer with its embedded themes and
copied notices, and exercise the published web host on both .NET 10 and .NET 11.

The `nuget-<run-id>-<attempt>` artifact contains the validated CI batch. Tag runs use
`nuget-<run-id>-tag`, retained for 90 days and reused on complete reruns. Keep a
separate durable copy of release artifacts if recovery may be needed after that
retention period. Unit/browser and package validation reports are separate artifacts.

## Trusted Publishing setup

The publishing job uses `NuGet/login` with GitHub OIDC and a short-lived credential.
It does not need a `NUGET_API_KEY` repository secret. Configure the NuGet.org policy
under the account that owns the packages:

| Field | Value |
|---|---|
| Package owner / NuGet login username | `Herman-Schoenfeld` |
| Provider | GitHub Actions |
| Repository owner | `HermanSchoenfeld` |
| Repository | `Sphere10.Framework` |
| Workflow file | `main.yml` |
| Environment | `nuget` |
| Scope | Push new packages and package versions |
| Package patterns, separate lines | `Sphere10.Framework*`, `Sphere10.HashLib4CSharp` |

Do not grant unlist/relist access for this workflow. The account must already own
existing package IDs or have permission to create new IDs. A newly created policy
may have a seven-day activation window until its first successful publication.

In GitHub, configure the `nuget` environment's deployment branch/tag rules to allow
`master` and version tags matching `v*`. Keep workflow changes and release tags
restricted to trusted maintainers. Only the publishing job receives `id-token: write`.
The workflow also checks the exact repository and push event before requesting a
credential. See [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing).

## Installing packages

The default command selects a stable version:

```shell
dotnet add package Sphere10.Framework
```

Opt into prereleases or select a deliberate alpha release explicitly:

```shell
dotnet add package Sphere10.Framework --prerelease
dotnet add package Sphere10.Framework --version 3.2.0-alpha.1
dotnet add package Sphere10.Framework.Web.VisualRenderer --prerelease
```

Visual Studio's **Include prerelease** checkbox enables preview versions in its
package UI. A broad prerelease query can select CI, alpha, RC or stable versions
according to NuGet ordering. An exact version is preferable for reproducible builds.

For a development project deliberately following the CI builds of one base:

```xml
<PackageReference Include="Sphere10.Framework" Version="3.2.0-ci.*" />
```

`3.2.*` floats stable patch releases. `3.2.0-*` allows all prereleases of that base
and its stable release. Floating versions resolve during restore, not while the
application runs. Keep related Framework references aligned rather than floating
them independently across release bases. To deliberately reevaluate a floating
reference and a lock file:

```shell
dotnet restore --force-evaluate --no-http-cache
```

Review and commit the updated `packages.lock.json`. CI consumers that require
reproducibility should use exact versions or a committed lock file with
`dotnet restore --locked-mode`. Existing local/global caches and the HTTP cache
can affect whether a restore contacts the feed. Clear only the relevant cache when
diagnosing a stale listing, rather than changing a project's dependencies implicitly.
See [NuGet dependency resolution](https://learn.microsoft.com/en-us/nuget/concepts/dependency-resolution).

## Retrying and recovering publication

NuGet.org cannot atomically publish a whole package family. A failed batch can leave
some packages public while later packages or symbols are missing. The publisher
preflights every existing package and symbol archive before uploading anything.
It compares uncompressed entry contents, allowing only NuGet's added repository
signature and ZIP container differences. Different contents at the same ID/version
stop publication. There is no unconditional `--skip-duplicate` path in sealed mode.

1. Fix account permissions or a transient service issue without changing the artifact.
2. Use **Re-run failed jobs**. The publishing job downloads the original validated
   batch and skips only archives whose published payload matches it.
3. An indexing delay can make an accepted upload temporarily unreadable. The job
   waits up to 15 minutes for each failed upload and for final batch verification.
   If it still fails, wait for NuGet indexing and retry the original artifact.
4. A complete CI rerun creates a new attempt version. A complete tag rerun reuses
   its retained artifact. If the tag artifact expired, a rebuild must still match
   already published contents. It may fail because toolchain or dependency inputs
   changed. Recover the saved original batch or publish a new version, never bypass
   the comparison or retag an existing public version.

For a manually recovered original artifact, validate before publishing:

```powershell
python scripts/nuget-release.py verify <batch-directory>
./scripts/publish.ps1 -SealedRelease -PackagesDirectory <batch-directory> -WhatIf
```

Actual sealed publication requires `NUGET_API_KEY` in the process environment, as
supplied by the OIDC job, and explicit authorization to publish. The existing
manual publisher remains available for its documented feeds and API-key workflow.
The [private folder-feed publisher](../scripts/publish-private.ps1) is unchanged
and does not contact NuGet.org.

## Local validation

Run the publication contract tests without any registry writes:

```powershell
python -m unittest discover -s scripts/tests -p "test_nuget_*.py" -v
./scripts/tests/test_ci_package_common.ps1 -ArtifactsRoot <isolated-test-output>
./scripts/validate-packages.ps1
```

The tests cover branch/tag identities, malformed tags, rerun uniqueness, missing
symbols, dependencies, source identity, changed archives, repository signatures,
partial batches, concurrent upload races and conflicting contents. All registry
operations in these tests use an in-memory fake or a local test HTTP server. Never publish disposable test
versions to NuGet.org.

GitHub's repository sidebar **Packages** lists GitHub Packages, a separate registry.
The public packages are on NuGet.org; an additional private registry can receive
the same validated batches. The README badges link to the public feed.

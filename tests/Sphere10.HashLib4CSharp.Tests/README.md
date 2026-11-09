# Sphere10.HashLib4CSharp.Tests

NUnit tests for [Sphere10.HashLib4CSharp](../../src/Sphere10.HashLib4CSharp/README.md), the Framework's fork of the HashLib4CSharp hashing library. The fixtures check every algorithm family against known test vectors and share a common set of behavior checks for cloning, incremental hashing, streams, spans, null input, cancellation and keys.

## Run the tests

Run these commands from the repository root. `dotnet test` restores and builds the referenced project first. `BuildRevision=0` supplies an explicit local build revision.

```powershell
dotnet test tests/Sphere10.HashLib4CSharp.Tests/Sphere10.HashLib4CSharp.Tests.csproj -c Release -p:BuildRevision=0
```

Run one algorithm family, for example the key derivation functions:

```powershell
dotnet test tests/Sphere10.HashLib4CSharp.Tests/Sphere10.HashLib4CSharp.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~PBKDF"
```

The fixtures use the `HashLib4CSharp.Tests` namespace. They are `internal` classes, which NUnit discovers like public ones. The suite needs no network, database or desktop session.

## Contents

### src

All fixtures, abstract test bases and test-vector tables. Each row lists every class declared in its file.

| Fixture | Purpose |
| --- | --- |
| [NullDigestTest](src/NullDigestTest.cs) | The null digest, which returns its input unchanged and does not implement hash or block sizes. |

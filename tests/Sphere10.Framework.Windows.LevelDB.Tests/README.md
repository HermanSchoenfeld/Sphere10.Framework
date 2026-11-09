# Sphere10.Framework.Windows.LevelDB.Tests

NUnit tests for the [LevelDB wrapper](../../src/Sphere10.Framework.Windows.LevelDB/README.md) in `Sphere10.Framework.Windows.LevelDB`. They open real LevelDB databases in temporary folders and check the managed `DB`, iterator, snapshot and write-batch APIs.

## Run the tests

Run these commands from the repository root. `dotnet test` restores and builds the referenced projects first. `BuildRevision=0` supplies an explicit local build revision.

```powershell
dotnet test tests/Sphere10.Framework.Windows.LevelDB.Tests/Sphere10.Framework.Windows.LevelDB.Tests.csproj -c Release -p:BuildRevision=0
```

Run the write-batch cases only:

```powershell
dotnet test tests/Sphere10.Framework.Windows.LevelDB.Tests/Sphere10.Framework.Windows.LevelDB.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~LevelDBTests.TestWriteBatch"
```

## Requirements

- Windows, in an x86 or x64 test process. `LevelDBInterop` embeds only those native builds and loads them through `kernel32`.
- Write access to the roaming application-data folder. The native `LevelDB.dll` is extracted there on first use, under `Sphere10.Framework`.
- Write access to the temporary folder. Each test creates a database under `Sphere10.Framework.Windows.LevelDB.Tests` and the teardown deletes it.

## Contents

### Project root

The LevelDB integration fixture.

| Fixture | Purpose |
| --- | --- |
| [LevelDBTests](LevelDBTests.cs) | Database creation and `ErrorIfExists`, put, get and delete, repair, iterators and enumeration, snapshot reads, `leveldb.stats` properties, write batches and their iteration, and disposing a snapshot after its database. |

# Native LevelDB Binaries

Holds the 32-bit and 64-bit native `LevelDB.dll` builds that the project embeds as resources.

The project file embeds both files, and `Properties/Resources.resx` exposes them as `Resources.LevelDB32_DLL` and `Resources.LevelDB64_DLL`. On first use, the static constructor of `LevelDBInterop` writes the build that matches the process architecture to `%APPDATA%\Sphere10.Framework\Sphere10.Framework.Windows.LevelDB-<assembly version>\x32` or `\x64` as `LevelDB.dll` and loads it. Other process architectures raise `PlatformNotSupportedException`. The binaries are LevelDB builds distributed under LevelDB's BSD 3-Clause License, as the [project README](../README.md) describes.

## Contents

This folder holds no source types.

| File | Purpose |
|---|---|
| `LevelDB32.dll` | Native LevelDB build loaded by x86 processes |
| `LevelDB64.dll` | Native LevelDB build loaded by x64 processes |

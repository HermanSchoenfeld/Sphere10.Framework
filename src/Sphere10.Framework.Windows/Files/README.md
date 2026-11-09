# Files

Provides Windows file operations beyond the BCL: alternate data streams, stream-preserving copies, open-handle discovery, PE and managed assembly inspection, signature checks and long Unicode paths.

`FileUtil` (`Tools.Windows.Files`) is the entry point. It lists, opens, reads, writes and deletes named `$DATA` streams, reads bounded file ranges, inspects PE images and creates the two services of this domain. `WindowsFileInspectionService` reads a file's size, SHA-256, version resources and embedded Authenticode signature without UI or network retrieval. `FileHandleService` captures open handles system-wide or for one verified `ProcessIdentity`, finds the processes that lock a path through Restart Manager, and force-closes one captured disk-file handle after rechecking it. Handle capture runs in an isolated child process, so an application that uses it calls `FileHandleService.TryRunWorker(args, out exitCode)` (or `FileUtil.TryRunHandleWorker`) first thing in `Main` and exits when it returns true. The child is started through `WindowsProcessWorkerHost` from the [Processes](../Processes/README.md) domain.

`ManagedAssemblyUtil` (`Tools.Windows.Assemblies`) parses .NET assemblies with the ILSpy decompiler and the BCL metadata reader, and never loads them into the runtime. It lists definitions, decompiles C# or IL by token, finds direct references, reads embedded resources and `.resources` entries without deserializing them, decompiles BAML to XAML and exports a project into an empty directory. `WindowsFile` copies a file with all its streams through `CopyFileEx` with progress and cancellation, and `WindowsVolume` reads and relabels a volume. `UnicodeNamedFile`, `UnicodeNamedDirectory` and `UnicodePath` are older static helpers that use `\\?\` paths to pass the `MAX_PATH` limit. Drive and disk inventories are in the [Storage](../Storage/README.md) domain.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`FileHandleCapture`](FileHandleCapture.cs) | record | Result of a handle capture: `FileHandleInfo` rows, capture time, partial flag and availability text |
| [`FileHandleInfo`](FileHandleInfo.cs) | record | One captured handle: owning process identity and name, handle value, object address and type, granted access and path |
| [`FileHandleService`](FileHandleService.cs) | class | Captures handles and path blockers through an isolated worker process and force-releases one rechecked disk-file handle |
| [`FileUtil`](FileUtil.cs) | class | `Tools.Windows.Files` facade for alternate data streams, bounded range reads, PE inspection, file inspection and handle-service creation |
| [`SafeFileHandle`](SafeFileHandle.cs) | class | Safe handle that closes a Win32 file handle with `CloseHandle` |
| [`SafeFindHandle`](SafeFindHandle.cs) | class | Safe handle that closes a `FindFirstFile` search handle with `FindClose` |
| [`UnicodeNamedDirectory`](UnicodeNamedDirectory.cs) | class | Static create, delete, exists and listing operations on directories through `\\?\` long paths |
| [`UnicodeNamedFile`](UnicodeNamedFile.cs) | class | Static create, delete, copy and exists operations on files through `\\?\` long paths |
| [`UnicodePath`](UnicodePath.cs) | class | Static full-path resolution and existence check for long Unicode paths |
| [`WindowsAlternateStreamInfo`](WindowsAlternateStreamInfo.cs) | record | One named `$DATA` stream of a file with its length |
| [`WindowsFile`](WindowsFile.cs) | static class | Copies a file with all its streams, with progress and cancellation, and lists, opens and deletes alternate streams |
| [`WindowsFileCopyProgress`](WindowsFileCopyProgress.cs) | struct | Record struct with total and transferred bytes across all streams of a copy |
| [`WindowsFileInspection`](WindowsFileInspection.cs) | class | Result of a file inspection: size, write time, SHA-256, version resources, trust status and signer certificate details |
| [`WindowsFileInspectionService`](WindowsFileInspectionService.cs) | class | Inspects a local file's hash, version information and embedded Authenticode signature |
| [`WindowsVolume`](WindowsVolume.cs) | static class | Reads a volume's type, format, label and capacity and changes its label after checking the expected current label |
| [`WindowsVolumeInfo`](WindowsVolumeInfo.cs) | record | Volume root, drive type, ready state, format, label and capacity |

### AlternateDataStreams

`AlternateDataStreamDataSource` is a [`PollingDataSource`](../../Sphere10.Framework/DataSource/README.md) over the named streams of one file, with create, update, delete, preview and export. Call `LoadFileAsync` to select the file, and the previous inventory stays readable until the new one is listed. Native stream operations go through the `FileUtil` passed to the constructor. Override `Logger` to record committed changes.

| Type | Kind | Purpose |
|---|---|---|

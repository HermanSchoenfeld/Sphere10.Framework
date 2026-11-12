# Managed and native diagnostic analysis

`Sphere10.Framework.Windows` owns the reusable readers and native resources.
Applications own screen layouts, capture policy, settings and user-selected
destinations. The three analysis engines are pinned through central package
management: ICSharpCode.Decompiler and ICSharpCode.BamlDecompiler
11.2.0.10050, and Microsoft.Diagnostics.Runtime 4.2.751201. They are referenced packages; their
upstream code has not been copied or relicensed. See the
[engine notices](../../src/Sphere10.Framework.Windows/THIRD-PARTY-NOTICES.md).

## Managed assembly inspection

Use `Tools.Windows.Assemblies.InspectAsync(path, token)` to capture metadata.
The detached result includes definition/reference/resource tokens, parent tokens,
names and attributes. A UI can build a hierarchy without reopening the file.

`ReadCodeAsync(image, token, il: false)` produces C# through ILSpy;
`il: true` uses its IL disassembler. Token zero requests the whole module.
`FindReferencesAsync` identifies direct IL-token references and base/interface
metadata links. These are specific observed references, not virtual-dispatch
resolution or whole-program reachability.

`ReadResourceAsync` returns bounded embedded resource bytes. Linked resources
remain references. `ReadResourceEntriesAsync(image, resourceToken, token)` lists
typed descriptors inside a `.resources` container; `ReadResourceEntryAsync`
reads a named payload. Stream/byte-array framing is removed without instantiating
serialized resource objects. Ordinary WPF `.g.resources` containers are supported.

`ReadBamlXamlAsync(image, resourceToken, entryName, token)` uses ILSpy's BAML
engine to produce XAML text. Omit `entryName` for a directly embedded BAML
resource. Raw reads remain available. No target assembly or markup is executed,
and the production/test projects do not acquire a WPF host reference. The
metadata resolver owns and disposes its local files for each request; missing
dependencies can limit recovered semantics.

Resource inventories admit at most 4,096 entries, 4,096 characters per name and
256 Ki characters across names. Container counts are validated before creating
the resource reader. The existing one-MiB resource and two-Mi-character code
limits also apply to BAML input and XAML output. Dependency metadata is bounded
to 16 MiB per image and 64 MiB in total, with cancellation throughout. Invalid,
unsupported, changed or oversized inputs fail explicitly; they do not become an
empty successful view.

`ExportProjectAsync` explicitly invokes ILSpy's project exporter into an empty
directory, with cancellation and one decompiler worker. It does not overwrite an
existing project or guarantee that recovered source compiles without resolving
the original target's dependencies.

The adapter never loads the target assembly into the CLR. It limits an image to
64 MiB, metadata to 16 MiB, retained metadata to 100,000 rows/8 Mi characters,
one code response to two Mi characters and one resource read to one MiB. The
file length and UTC write time captured with metadata are checked again under a
read-only shared handle before code/resource/export work. A changed file must be
reopened. Project export can produce multiple files; the code-response limit is
not a project-output quota.

## Managed process and dump capture

```csharp
var identity = await ProcessIdentity.CaptureAsync(processId, cancellationToken);
var inventory = await Tools.Windows.Managed.InspectProcessAsync(identity,
	new ManagedInspectionOptions { IncludeStacks = false }, cancellationToken);

// A heap walk is a separate explicit operation, never an inventory refresh side effect.
var heap = await Tools.Windows.Managed.InspectProcessAsync(identity,
	new ManagedInspectionOptions {
		IncludeHeap = true,
		MaximumObjects = 100000,
		MaximumReferences = 200000,
		MaximumRoots = 10000
	}, cancellationToken);
```

The process path verifies PID and creation time through a retained handle, then
captures one Windows PSS VA clone **and thread contexts**. Reading uses the clone;
it never falls back to pages or contexts from the changing original process.
Capturing the PSS state briefly pauses the original. Reading the captured state
does not leave it suspended. The current live snapshot backend supports an x64
host and x64 target on Windows with PSS support; unsupported architectures fail
explicitly. Cloneable pages are available; missing pages remain unavailable.

The snapshot owner closes ClrMD resources and terminates its own threadless clone
before freeing PSS. This prevents an external process inventory's retained query
handles from keeping the diagnostic clone alive indefinitely. It never terminates
the source process.

The detached result contains runtime/domain/module/thread information, current
exception identities and GC segments. Optional stacks retain native addresses
and managed method names. Optional heap capture retains bounded objects, type
counts/bytes, direct references and roots. The object limit bounds examined
objects, including free/invalid entries. Counts describe retained objects;
limit/error indicators accompany partial coverage. Roots/edges may refer to
objects outside the retained subset. A heap comparison should compare type
counts/bytes using runtime flavor/version and type identity, not assume stable
addresses across moving GCs or ASLR.

`InspectDumpAsync(path, options, token)` uses the same projection and budgets.
An offline result has a source file path and no live `ProcessIdentity`. Missing
memory or a mismatched/missing DAC is reported per runtime, while independent
runtime inventory remains visible.

Windows minidumps additionally include native module/thread/exception, captured
memory-range and virtual-memory-state rows even when no CLR is present. Native
stacks share the same DbgHelp captured-context walker: x64 user mode only, first
32 threads, at most 128 frames and one MiB of memory reads per thread. Matching
local PE files may provide unwind/symbol metadata; absent files leave raw
addresses. `NativeDump` retains the offline file revision and bounded backed
memory descriptors. `Tools.Windows.Dumps.ReadMemoryAsync(dump, address, count)`
returns up to 64 KiB from one unique recorded mapping and rejects missing,
ambiguous, out-of-file or changed-revision data. It never reads a live PID or
fills missing dump bytes from a local image. Managed and native rows share the
same overall `MaximumEntries` budget; native retained text is additionally
bounded to two Mi characters. Limits are reported as partial.

DAC lookup is local by default. A matching signed DAC next to the captured runtime
is preferred; callers may supply explicit `LocalLibraryDirectories`. ClrMD's
default public symbol-server downloads are disabled, and its DAC signature and
version checks stay enabled. When ClrMD requests Windows PE property checking, the local image locator verifies the requested PE timestamp and image size before returning a candidate; a changed same-path image is rejected. Explicit unchecked local lookup retains its existing behavior. No target assembly is loaded or evaluated.

## Native stacks and dump creation

`Tools.Windows.Symbols.CaptureStackAsync(threadIdentity, maximumFrames, symbolSearchPath, token)`
uses the same PSS owner, captured thread creation time/context and a serialized
DbgHelp backend. It supports bounded x64 user-mode unwinding. Local image
directories are the default symbol path; environment and embedded CodeView paths
are ignored. Only an explicit caller-supplied symbol search path enables another
symbol source. Unresolved frames retain instruction/stack/frame addresses and
captured module paths/base addresses. Kernel stacks, arbitrary architectures and
a full managed debugger are not claimed.

`Tools.Windows.Dumps.CaptureAsync(identity, path, includeFullMemory, maximumBytes, token)`
uses the application's isolated worker host. Route
`Tools.Windows.Dumps.TryRunWorker(args, out exitCode)` before starting the UI.
`WindowsProcessWorkerHost` can identify a different existing host for tests or
embedded applications. The worker verifies the original identity again and uses
its retained handle for `MiniDumpWriteDump`. A unique staging file is published
only on success. Cancellation/timeout or an exceeded file budget terminates and
reaps the owned helper and removes the staging file. Triage is the default; a
full-memory dump is explicit and subject to the configured size limit.

Native dump and symbol operations share one DbgHelp lock per process, following
the library's thread-safety requirement. All collection and parsing entry points
dispatch work away from a UI caller, and synchronous native cores assert that
they are not on the main thread.

## Detached historical address resolution

Tools.Windows.Symbols.ResolveAddressesAsync(addresses, capturedModules,
symbolSearchPath, token) uses retained addresses and caller-supplied
WindowsSymbolModule mappings only. It never opens a PID or captures PSS.
It shares serialized DbgHelp symbol resolution and option restoration with live
inspection. Returned addresses preserve their original order.

Image base/size/path and optional PE timestamp/checksum come from the capture.
Timestamp and size evidence are required for local image admission; checksum is
compared when supplied. Overlaps, missing/mismatched images and inaccessible files
remain unresolved with issues. Limits are 512 addresses, 20,000 input mappings,
512 relevant images and 512 MiB per local image. PE identity matching is not a
cryptographic trust check. Local directories are the default; another symbol
source requires an explicit search path.

## Validation

The NUnit suites exercise real metadata/C#/IL/reference output, changed-file
rejection, a consistent self-process snapshot, bounded heap capture, an owned
waiting thread's captured native stack, stale identities and cancellation.
SystemExpert's integration tests create an isolated full-memory dump of their
own test process, read its bounded heap through ClrMD, delete it, and verify real
WinForms metadata/runtime/heap screens through the native dispatcher.

## Primary references

- [ILSpy engine usage](https://github.com/icsharpcode/ILSpy/wiki/Getting-Started-With-ICSharpCode.Decompiler)
- [ClrMD source and documentation](https://github.com/microsoft/clrmd)
- [PSS capture flags](https://learn.microsoft.com/windows/win32/api/processsnapshot/ne-processsnapshot-pss_capture_flags)
- [DbgHelp StackWalk64](https://learn.microsoft.com/windows/win32/api/dbghelp/nf-dbghelp-stackwalk64)
- [MiniDumpWriteDump](https://learn.microsoft.com/windows/win32/api/minidumpapiset/nf-minidumpapiset-minidumpwritedump)

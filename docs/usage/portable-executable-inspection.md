# Portable Executable And Process Memory Inspection

`Tools.Windows.Files.InspectPortableExecutableAsync(path, token)` returns detached
PE structure without loading the target assembly or executing its code. It uses
the in-box `PEReader`/`MetadataReader` behind the existing `Tools.Windows.Files` facade; no
additional package is required. Synchronous inspection asserts that it runs off
the main thread. The asynchronous entry point dispatches the complete operation
to a worker.

The result contains headers, sections, data directories, imports, exports,
resources (including bounded manifest text), debug/CodeView identity, relocations,
TLS fields, supported load-configuration fields and CLR assembly/reference/type
metadata. AMD64 exception entries include runtime-function ranges and unwind
headers, code slots, handler addresses and chained records. Other exception
architectures remain raw directory ranges with an explicit issue. Unwind data is
structural evidence, not a stack-unwinding or debugging engine.

`TryGetFileOffset(rva, count, out offset)` and its reverse operation reject
unbacked virtual tails, truncated ranges, overflowing requests and ambiguous
overlapping mappings. The certificate directory uses a file-offset address space;
other PE directories use RVAs. TLS and load-configuration pointer values retain
their documented virtual-address interpretation.

Malformed file headers fail the inspection. A malformed optional table instead
adds a named `Issues` entry while preserving independently parsed tables. The
reader bounds structural entries (65,536), aggregate retained text (8 Mi characters),
per-entry text previews, resource tree nodes/depth, names,
metadata (16 MiB), debug payloads (1 MiB) and individual reads (1 MiB). These are
explicit inspection limits, not claims of complete parsing of every extension.

Use `ReadPortableExecutableRangeAsync(image, offset, count, token)` to supply a
bounded slice to an existing byte/hex control. It verifies the captured length and
last-write time while holding a read-sharing-only file handle. Those fields
identify an observed file revision; they are not a cryptographic content identity.
`ReadFileRangeAsync` provides the same bounded file IO without a captured PE
revision. Reuse `InspectAsync` for the existing hash, trust, certificate and
version workflow instead of implementing it again in the binary parser.

## Process Memory

Open `WindowsProcess` on a worker and retain/revalidate its `ProcessIdentity`.
`GetMemoryRegionsAsync` captures detached `VirtualQueryEx` regions. A normal
consumer binds that array to an existing snapshot data source: sorting, filtering
and paging must reuse it rather than query the process again. The default region
bound is 100,000; exceeding a requested bound fails explicitly.

`ReadMemoryAsync(address, count, token)` returns the actual bytes transferred,
requested length, address, process identity and native error code. A failed
partial read retains its bytes and error; it is not converted to a complete
successful read. Reads are limited to 1 MiB and do not write target memory.
The read handle validates process creation time and target pointer width before
access. A 32-bit caller cannot inspect a native 64-bit address space.

`WindowsProcessMemoryStream(process, address, length)` presents one address range
as a seekable, read-only `Stream` over those reads: each `Read`/`ReadAsync` fetches
only the requested bytes (at most 1 MiB per native call), `Position` and `Seek`
move within the range, and an unreadable page fails the read with a
`Win32Exception` carrying the Windows error rather than returning invented bytes.
Give it to a stream-backed viewer, such as the hex editor, to page through a large
region without copying the region.

The existing string scanner now recognizes strict UTF-8 in addition to ASCII and
UTF-16LE, preserving split code points across input chunks. Pure ASCII emits its
existing canonical row once. `WindowsProcessString.Encoding` identifies the
encoding; `IsUnicode` remains the compatibility indication for UTF-16LE.
`GetMemoryStringsInRangeAsync` and `GetImageStringsInRangeAsync` scan only an
explicit memory or image range, preserving virtual addresses or file offsets in
their results. Existing byte/result limits and cancellation remain in force.

SystemExpert's Binary Inspector and Inspector memory control are the integrated
consumers. The Binary Inspector opens the whole image read-only in
`HexEditorControl`, which pages the file on demand, and navigates to the selected
structure or mapped address; the memory control opens a selected region through
`WindowsProcessMemoryStream` with the region's virtual address as the viewer's
base address. Both reuse the existing grid data-source/query pipeline, settings,
row commands and exports and introduce no other hex editor, native enumerator or
hashing service.

Native format references: [Microsoft PE/COFF format](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format),
[AMD64 unwind data](https://learn.microsoft.com/en-us/cpp/build/exception-handling-x64),
[load configuration](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-image_load_config_directory64),
[VirtualQueryEx](https://learn.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-virtualqueryex)
and [ReadProcessMemory](https://learn.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-readprocessmemory).

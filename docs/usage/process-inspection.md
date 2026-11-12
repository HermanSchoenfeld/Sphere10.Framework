# Windows process inspection

WindowsProcess owns a handle tied to one process generation. Explicit native
operations use captured PID/creation time and run on workers; synchronous cores
assert that they are not on the main thread. Stale identities are not replaced
with the current occupant of a recycled PID.

## Tokens and mitigations

GetTokenInformationAsync preserves user/owner/groups/privileges and adds
restricted SIDs, capabilities, AppContainer/LPAC, UI access, mandatory policy,
virtualization and sandbox indicators. Optional classes are queried independently;
unsupported fields stay unavailable with per-field issues.

GetMitigationsAsync exposes raw policy flags, DEP permanence and per-policy
errors. Optional process metadata reuses this implementation. These are native
flags, not a combined security score.

## Modules

GetModulesAsync keeps inexpensive loader inventory behavior.
GetModulesWithDetailsAsync explicitly reads mapped PE headers, capped at 32,768
modules, 64 KiB per image and 16 MiB aggregate reads. PEReader consumes bounded
native memory; no target assembly is loaded. Per-module failures preserve
independent rows and availability.

Additional fields describe PE machine/characteristics, CLR flags, mapping type
and native/IL/mixed classification. AnyCPU does not establish runtime architecture.
Loader inventory does not discover unlisted manual maps or data-file loads.
File trust/version remains the existing file-inspection owner's responsibility.

## Page state and explicit watch

QueryWorkingSetAsync(addresses, token) is read-only and accepts 1-65,536 supplied
addresses. WindowsWorkingSetPage leaves valid-only fields unavailable for invalid
entries. It does not enable watch state, fault target pages in or determine I/O.

WindowsWorkingSetWatch.StartAsync(identity, token) explicitly mutates the target:
it calls InitializeProcessForWsWatch after verifying the retained process object.
Applications must obtain informed confirmation that monitoring persists until
target exit. Windows has no disable operation. Current Windows requires
process-set-information access for enablement in addition to query access;
denial remains an error, not a retry against an unverified target.

One reader per exact process is admitted in this application.
ReadAsync(maximumRecords, token) destructively drains GetWsChangesEx with a
bounded buffer and preserves the terminator's lost-record count. Other programs
can drain the same buffer. Insufficient capacity and concurrent/no available
records remain explicit. Native records have no timestamps; ObservedAtUtc is
the drain time. Reserved flags do not classify hard versus soft faults.
Disposal serializes with any native read and releases the reader handle only.

WindowsProcessCounters.PageFaultCount is an independent cumulative counter,
including soft faults. Consumers should use their existing shared baseline
for rates and preserve missing/reset/PID-reuse behavior.

## Validation

WindowsProcessInspectionTests checks token/policy availability, mapped kernel32
headers, resident/unmapped flags and historical symbol identity rejection.
Its working-set test launches and owns a dedicated helper; it does not enable
monitoring on arbitrary applications.

Primary references:

- [Token classes](https://learn.microsoft.com/windows/win32/api/winnt/ne-winnt-token_information_class)
- [Mitigations](https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-getprocessmitigationpolicy)
- [QueryWorkingSetEx](https://learn.microsoft.com/windows/win32/api/psapi/nf-psapi-queryworkingsetex)
- [InitializeProcessForWsWatch](https://learn.microsoft.com/windows/win32/api/psapi/nf-psapi-initializeprocessforwswatch)
- [GetWsChangesEx](https://learn.microsoft.com/windows/win32/api/psapi/nf-psapi-getwschangesex)

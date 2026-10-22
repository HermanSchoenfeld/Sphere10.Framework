# Windows Power Inventories

`Tools.Windows.Computer.GetPowerPlansAsync` captures installed plan GUIDs, localized
names/descriptions and the active plan. `GetPowerSettingsAsync(planId)` captures
that plan's subgroups and settings, including hidden settings, raw AC/DC indexes,
localized choice labels, units, ranges, increments and descriptions. A failed
value read remains unavailable with its Windows error; zero is preserved.

Machine/user `ACSettingIndex` and `DCSettingIndex` values found under
`Software\Policies\Microsoft\Power\PowerSettings\<setting-guid>` are reported
separately with their registry provenance. Stored plan values and registry policy
presence do not establish effective runtime precedence, MDM state or overlay
power-mode behavior. Captures can race external plan or policy changes; reload
for a new observation. These APIs perform no changes.

`GetPowerDiagnosticsAsync` reads current Windows sleep/wake capabilities, including
S1–S5 and S0 low-power idle, hibernation-file/fast-startup support, wake capabilities
and aggregate system/display-required flags. Last sleep/wake values are interrupt
time in 100-nanosecond units, **not UTC timestamps** or a wake cause.

Synchronous variants require a worker thread. Async variants dispatch native IO
to a worker and accept cancellation. `PowerGetActiveScheme` memory is released
with `LocalFree`; the capability structure uses the modern 76-byte Windows layout.
All returned rows are immutable records without native pointers or open handles.

`WindowsPowerPlanDataSource`, `WindowsPowerSettingDataSource(planId)` and
`WindowsPowerDiagnosticDataSource` use `ReloadableSnapshotDataSource<T>`: capture
once, then search/sort/page the detached inventory. `ReloadAsync` explicitly
refreshes it. Paging does not enumerate native settings again.

`PowerCfgSentry.ReadReportAsync` offers only the typed read-only reports in
`PowerCfgReport`: Requests, WakeTimers, LastWake, AvailableSleepStates and
WakeArmedDevices. It launches the in-box executable on a worker with structured
arguments and a 15-second deadline. The owned process is terminated/drained on
cancellation. Localized stdout/stderr and exit status are returned verbatim;
callers must not treat English text parsing as a native typed inventory.
Some reports require elevation. The API never elevates automatically.

Microsoft references: [PowerEnumerate](https://learn.microsoft.com/en-us/windows/win32/api/powrprof/nf-powrprof-powerenumerate),
[PowerReadPossibleValue](https://learn.microsoft.com/en-us/windows/win32/api/powrprof/nf-powrprof-powerreadpossiblevalue),
[SYSTEM_POWER_CAPABILITIES](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-system_power_capabilities),
[CallNtPowerInformation](https://learn.microsoft.com/en-us/windows/win32/api/powerbase/nf-powerbase-callntpowerinformation),
[Power policy registry mappings](https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-power),
and [powercfg reports](https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options).

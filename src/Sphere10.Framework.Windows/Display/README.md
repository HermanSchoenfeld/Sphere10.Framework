# Display

Reads the active display paths, modes, timings and EDID data, and provides a frame clock paced by the desktop compositor.

`WindowsDisplayUtil` (`Tools.Windows.Display`) reads the current display configuration through the CCD API, the registry and WMI. `GetDisplays` returns one `WindowsDisplayInfo` per active target path, with adapter and monitor identities, desktop position, DPI, active and preferred `WindowsDisplayTiming`, advanced-colour and HDR state, driver-enumerated `WindowsDisplayMode` values and the cached `WindowsMonitorEdid`. `GetDisplayNames` maps each GDI device name such as `\\.\DISPLAY1` to the monitor name Windows Settings shows. The synchronous methods assert in debug builds when called on the main thread, so UI code uses the async variants. `CreateDataSource` returns a `WindowsDisplayDataSource` that captures once on first load and then pages the detached rows.

`DisplayFrameClock` raises `Frame` on a synchronization context once per composition frame, or less often when `MinimumInterval` is set. A background thread waits on a high-resolution waitable timer that follows the compositor's refresh rate, and a frame still queued is never queued twice. Start, stop and dispose it on the thread that owns the context. The [Magnification](../Magnification/README.md) renderer uses it to refresh capture windows. Monitor identity records come from the [System](../System/README.md) domain.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`WindowsDisplayDataSource`](WindowsDisplayDataSource.cs) | class | Read-only `AsyncListDataSource<WindowsDisplayInfo>` that captures the display inventory once and pages it |
| [`WindowsDisplayInfo`](WindowsDisplayInfo.cs) | record | One active CCD target path with adapter and monitor identities, desktop geometry, DPI, timings, colour and HDR state, modes and EDID |
| [`WindowsDisplayMode`](WindowsDisplayMode.cs) | record | One driver-enumerated mode of a GDI source: size, bit depth, refresh rate and interlacing |
| [`WindowsDisplayTiming`](WindowsDisplayTiming.cs) | record | Active and total pixel size, pixel clock, sync rates and scan-line ordering of a target timing |
| [`WindowsDisplayUtil`](WindowsDisplayUtil.cs) | class | Reads current displays and their Settings names with sync and async methods, and creates display data sources |
| [`WindowsMonitorEdid`](WindowsMonitorEdid.cs) | record | Parsed and validated EDID 1.x base block from the registry cache: version, input type, physical size, gamma and raw hex |

# Display

Reads the active display paths, modes, timings and EDID data, provides a frame clock paced by the desktop compositor, and reads and compares the frames a display shows.

`Tools.Windows.Display.GetBounds(deviceName)` reads a display's current desktop
rectangle in physical pixels without WMI or cached UI geometry. Use it for
capture and overlay bounds in processes with mixed DPI awareness. It throws when
the named display's current mode is unavailable.

`Tools.Windows.Display` reads the current display configuration through the CCD API, the registry and WMI. `GetDisplays` returns one `WindowsDisplayInfo` per active target path, with adapter and monitor identities, desktop position, DPI, active and preferred `WindowsDisplayTiming`, advanced-colour and HDR state, driver-enumerated `WindowsDisplayMode` values and the cached `WindowsMonitorEdid`. `GetDisplayNames` maps each GDI device name such as `\\.\DISPLAY1` to the monitor name Windows Settings shows. The synchronous methods assert in debug builds when called on the main thread, so UI code uses the async variants. `CreateDataSource` returns a `WindowsDisplayDataSource` that captures once on first load and then pages the detached rows.

`DisplayFrameClock` raises the parameterless `EventHandlerEx` event `Frame` on a synchronization context once per composition frame, or less often when `MinimumInterval` is set. A background thread waits on a high-resolution waitable timer that follows the compositor's refresh rate, and a frame still queued is never queued twice. Start, stop and dispose it on the thread that owns the context. The [Magnification](../Magnification/README.md) renderer uses it to refresh capture windows. Monitor identity records come from the [System](../System/README.md) domain.

`DisplayFrameCapture` reads rectangles of one display's frames through DXGI Desktop Duplication, which is part of Windows and costs other applications nothing. `Tools.Windows.Display.TryCreateFrameCapture(point, out capture)` creates one for the display that shows a physical desktop point and returns false when no display does, the display is rotated or its adapter refuses. `TryRead(timeout, areas, pixels)` waits for the next frame the compositor presents, skipping frames that only moved the pointer, and copies just the requested rectangles into `DisplayFramePixels` buffers of 32-bit BGRA rows. A narrow strip therefore costs a few milliseconds a frame and nothing while the desktop is still. A frame holds what the display shows, including a full-screen colour effect, and leaves out windows marked as excluded from capture. Windows withdraws the duplication when the display mode changes or the secure desktop appears; the next read asks for it again. Use an instance from one thread at a time. `IDisplayFrameSource` is its contract (`Bounds` and `TryRead`), so a consumer can be handed frames from elsewhere.

`DisplayFrameMotion` measures how far content moved vertically between two frames. `ReadRowSignature` reduces each row of a strip to one number that stays the same when colours are inverted, `EstimateVerticalShift` returns the distance that best lines up two signatures (positive when the content moved up, as it does when a page scrolls down) with the share of the frame-to-frame difference it explains, and `Combine` takes the distance that several strips agree on, so one strip across a playing video does not pass for scrolling. On a page scrolling at a constant speed the distances summed to that speed with a median position error of about one pixel. `ScreenMotionMediaTracker` in the [Windowing](../Windowing/README.md) domain is the consumer of both.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DisplayFrameCapture`](DisplayFrameCapture.cs) | class | Reads requested rectangles of each frame one display presents, through DXGI Desktop Duplication |
| [`DisplayFrameClock`](DisplayFrameClock.cs) | class | Raises `Frame` on a synchronization context at the compositor's refresh rate, optionally throttled, without queuing a backlog |
| [`DisplayFrameMotion`](DisplayFrameMotion.cs) | static class | Row signatures of a strip of pixels, the vertical distance between two of them and the distance several strips agree on |
| [`DisplayFramePixels`](DisplayFramePixels.cs) | class | A reusable buffer of 32-bit BGRA rows read from a frame |
| [`IDisplayFrameSource`](IDisplayFrameSource.cs) | interface | A display's bounds and a read of rectangles from its next frame |
| [`WindowsDisplayDataSource`](WindowsDisplayDataSource.cs) | class | Read-only `AsyncListDataSource<WindowsDisplayInfo>` that captures the display inventory once and pages it |
| [`WindowsDisplayInfo`](WindowsDisplayInfo.cs) | record | One active CCD target path with adapter and monitor identities, desktop geometry, DPI, timings, colour and HDR state, modes and EDID |
| [`WindowsDisplayMode`](WindowsDisplayMode.cs) | record | One driver-enumerated mode of a GDI source: size, bit depth, refresh rate and interlacing |
| [`WindowsDisplayTiming`](WindowsDisplayTiming.cs) | record | Active and total pixel size, pixel clock, sync rates and scan-line ordering of a target timing |
| [`Tools.Windows.Display`](WindowsDisplayUtil.cs) | static class | Reads current displays and their Settings names with sync and async methods, and creates display data sources and frame captures |
| [`WindowsMonitorEdid`](WindowsMonitorEdid.cs) | record | Parsed and validated EDID 1.x base block from the registry cache: version, input type, physical size, gamma and raw hex |

# Magnification

Applies colour transformations to the desktop or to single displays through the Windows Magnification API.

`DisplayColorEffectRenderer` is the high-level entry point. `Render` takes one `DisplayColorEffect` per display. When every display shares one matrix it uses the full-screen colour effect, which the compositor applies at no per-frame cost and which can fade over `FadeDuration` (150 ms by default). When displays differ it shows each non-identity display through its own `MagnifierWindow`, refreshed by a `DisplayFrameClock` from the [Display](../Display/README.md) domain, so those displays trail the screen by about one frame. Switching between the two paths is immediate. `Clear` restores normal colours, and `Failed` and `RenderingChanged` report errors and path switches.

`MagnificationSession` owns the process-level `MagInitialize`/`MagUninitialize` lifetime and the full-screen effect. `MagnifierWindow` is a click-through, topmost layered popup whose `WC_MAGNIFIER` child re-renders a desktop region at 1:1 scale through a colour matrix. It can exclude windows from the capture and restrict rendering to a source rectangle. Create, use and dispose all of these on one UI thread with a message loop, and keep the session alive longer than its windows. Only one application can own the full-screen effect, so `Render` throws a `Win32Exception` when another one holds it. Coordinates are physical desktop pixels. The native declarations are in `WinAPI.MAGNIFICATION` in the [WinAPI](../WinAPI/README.md) domain.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`MagnificationSession`](MagnificationSession.cs) | class | Owns the process's Magnification API initialization and sets, reads and resets the full-screen colour effect |
| [`MagnifierWindow`](MagnifierWindow.cs) | class | Topmost click-through magnifier that re-renders a desktop region through a colour matrix, with excluded windows and source clipping |

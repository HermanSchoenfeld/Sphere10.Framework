# Magnification

Applies colour transformations to the desktop or to single displays through the Windows Magnification API.

`DisplayColorEffectRenderer` is the high-level entry point. `Render` takes one `DisplayColorEffect` per display and copies the request's matrices, so subsequent caller changes require another call. When every display shares one matrix it uses the full-screen colour effect, which the compositor applies without recurring application callbacks. When displays differ it shows each non-identity display through its own `MagnifierWindow`, refreshed by a `DisplayFrameClock` from the [Display](../Display/README.md) domain, so those displays trail the screen by about one frame. Capture membership and configuration are reconciled when a request is applied, while capture refresh and passive-overlay discovery continue on each frame.

Full-screen changes can fade over `FadeDuration` (150 ms by default). Fades sample elapsed time into retained matrices, so a delayed frame skips obsolete samples and reaches the destination on the first available frame after the duration. Nonpositive durations and switches between rendering paths apply immediately. `Clear` restores normal colours. `RenderingChanged` is an `EventHandlerEx` notification without arguments, and `Failed` is an `EventHandlerEx<Exception>` notification raised after restoring normal colours.

`MagnificationSession` owns the process-level `MagInitialize`/`MagUninitialize` lifetime and the full-screen effect. `MagnifierWindow` is a click-through, topmost layered popup whose `WC_MAGNIFIER` child re-renders a desktop region at 1:1 scale through a colour matrix. It can exclude windows from the capture and restrict rendering to a source rectangle. Create, use and dispose all of these on one UI thread with a message loop, and keep the session alive longer than its windows. Only one application can own the full-screen effect, so `Render` throws a `Win32Exception` when another one holds it. Coordinates are physical desktop pixels. The native declarations are in `WinAPI.MAGNIFICATION` in the [WinAPI](../WinAPI/README.md) domain.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DisplayColorEffect`](DisplayColorEffect.cs) | record | The GDI device name, desktop bounds and `ColorMatrix` one display should show |
| [`DisplayColorEffectRenderer`](DisplayColorEffectRenderer.cs) | class | Shows per-display colour matrices through the full-screen effect when they match, with fades, or through per-display magnifier windows when they differ |
| [`MagnificationSession`](MagnificationSession.cs) | class | Owns the process's Magnification API initialization and sets, reads and resets the full-screen colour effect |
| [`MagnifierWindow`](MagnifierWindow.cs) | class | Topmost click-through magnifier that re-renders a desktop region through a colour matrix, with excluded windows and source clipping |

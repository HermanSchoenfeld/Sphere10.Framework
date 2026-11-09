# Loading Circle

A DPI-aware animated activity indicator for forms, tool strips and temporary overlays.

`LoadingCircle` draws one of the `LoadingCircleAnimationStyle` forms (`Spokes`, `Dots`, `Arc` or `Pulse`) in `Color`. `StylePreset` applies the original geometry presets, and `SetCircleAppearance` sets the spoke count, thickness and radii together. Setting `Active` requests animation, which pauses while the control is hidden, disabled or has no native handle. `StartAnimating` and `StopAnimating` can also disable the controls of a container while work runs and restore only the controls they disabled.

`BeginAnimationScope` animates an existing indicator for the lifetime of the returned scope. The static `EnterAnimationScope` creates an owned overlay on a container and removes and disposes it when the scope ends. `LoadingCircleToolStripMenuItem` hosts an owned indicator in a tool strip or menu.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`LoadingCircleAnimationStyle`](LoadingCircleAnimationStyle.cs) | enum | Drawing form of the indicator, which is rotating spokes, rotating dots, a rotating arc or pulsing rings |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`LoadingCircle`](Controls/LoadingCircle.cs) | class | Animated indicator with style, colour, geometry presets, container disabling and animation scopes |
| [`LoadingCircleToolStripMenuItem`](Controls/LoadingCircleToolStripMenuItem.cs) | class | Tool strip host that owns a `LoadingCircle` and sizes itself to the indicator |

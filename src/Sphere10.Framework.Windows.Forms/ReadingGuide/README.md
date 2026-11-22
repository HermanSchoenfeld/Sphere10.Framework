# Reading guide

`ReadingGuideForm` shows a horizontal line or translucent band across one display.
It is nonactivating, click-through and positioned in physical desktop pixels.
Create, configure and dispose it on one UI thread. The caller owns its lifetime.

Set `BackColor`, `Thickness`, `Band` and `BandOpacity`, then call `ShowGuide()`.
`FollowPointer` uses the existing `DisplayFrameClock` only while visible and following.
For a fixed guide, set it to false and call `MoveTo` or `MoveVertical`; neither moves
the pointer. Hidden guides stop their clock, and disposal releases it.

The form uses native topmost styles and the existing inactive-show operation,
rather than `Form.TopMost`, so displaying it does not take keyboard focus. Its
layered, transparent style lets input pass through to applications underneath.
It is a reading aid, not magnification or a colour-vision correction.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ReadingGuideForm`](ReadingGuideForm.cs) | class | Passive horizontal line or translucent band in physical desktop coordinates, with optional display-clock pointer following. |

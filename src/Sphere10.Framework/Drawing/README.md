# Drawing

Platform-neutral enumerations that describe how an image is resized into a requested size.

The core package holds only these options, so that code without a graphics dependency can describe a resize. The `Resize` and `FocusZoom` image extensions in [Sphere10.Framework.Drawing](../../Sphere10.Framework.Drawing/README.md) consume them, and so does the WinForms visual grid.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ResizeAlignment`](ResizeAlignment.cs) | enum | Anchor of the resized image within the requested size, from `TopLeft` to `BottomRight`. |
| [`ResizeMethod`](ResizeMethod.cs) | enum | Resize strategy: `Stretch`, `AspectFill`, `AspectFitPadded` or `AspectFit`. |

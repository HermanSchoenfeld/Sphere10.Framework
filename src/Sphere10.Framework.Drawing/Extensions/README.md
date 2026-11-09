# Drawing Extensions

Provides extension methods for `System.Drawing` colors, color matrices, graphics, icons, images, byte arrays, points, sizes and rectangles.

The geometry extensions for `Point`, `PointF`, `Size`, `SizeF`, `Rectangle` and `RectangleF` are plain arithmetic and work on any platform, except `GetRoundPath`. The `Graphics`, `ColorMatrix`, `Image`, `Icon` and byte-array extensions use GDI+ and are marked Windows-only. All classes are in the `Sphere10.Framework` namespace. `ColorMatrixExtensions` complements the matrix builders in [`ColorMatrices`](../ColorMatrices.cs), and `GraphicsExtensions` draws the [`RoundedRectangle`](../RoundedRectangle.cs), [`RoundedCorners`](../RoundedCorners.cs) and [`Gradient3DBorderStyle`](../Gradient3DBorderStyle.cs) types from the project folder.

Methods ending in `AndDispose`, such as `ResizeAndDispose` and `ZoomAndDispose`, dispose the source image and return a new bitmap. The other image methods leave the source open, and the caller owns every returned `Bitmap`.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ColorExtensions`](ColorExtensions.cs) | static class | Adds `ToARGBString`, `GetShade` (toward white or black) and `GetTranslucentShade` (toward opaque or transparent) to `Color` |
| [`ColorMatrixExtensions`](ColorMatrixExtensions.cs) | static class | Adds clone, array conversion, composition with `Then`, brightness and contrast, per-color `Transform`, `ApplyTo` an image and tolerance-based equality to `ColorMatrix` |
| [`DrawingByteArrayExtensions`](ByteArrayExtensions.cs) | static class | Adds `ToImage` to `byte[]`, which decodes the bytes into an `Image` |
| [`GraphicsExtensions`](GraphicsExtensions.cs) | static class | Adds rounded-rectangle fills, borders and gradients, linear and angled gradient fills, 3D gradient borders, circles, alpha blits and justified or ellipsis-truncated text to `Graphics` |
| [`IconExtensions`](IconExtensions.cs) | static class | Adds `ToBitmap(width, height)` to `Icon` |
| [`ImageExtensions`](ImageExtensions.cs) | static class | Adds `Copy` of a section, `Resize`, `Zoom` and `FocusZoom` (with disposing variants), `ToByteArray` and `SaveJpegWithCompression` to `Image` |
| [`PointExtensions`](PointExtensions.cs) | static class | Adds `DistanceTo`, `Add`, `Subtract` and `ToPointF` to `Point` |
| [`PointFExtensions`](PointFExtensions.cs) | static class | Adds `DistanceTo`, `Add`, `Subtract` and `ToPoint` to `PointF` |
| [`RectangleExtensions`](RectangleExtensions.cs) | static class | Adds corner and center points, `SetLocation`, `SetSize`, `Translate`, `ExpandBy`, `ShrinkBy`, `IntersectWith`, orientation helpers and `GetRoundPath` to `Rectangle` |
| [`RectangleFExtensions`](RectangleFExtensions.cs) | static class | Adds the same corner, center, location, size, translation, expansion and rounding helpers to `RectangleF` |
| [`SizeExtensions`](SizeExtensions.cs) | static class | Adds `Clip`, `ExpandBy`, `ShrinkBy`, `ScaleBy` and `ToSizeF` to `Size` |
| [`SizeFExtensions`](SizeFExtensions.cs) | static class | Adds `Clip`, `ExpandBy`, `ShrinkBy`, `ScaleBy` and `ToSize` to `SizeF` |

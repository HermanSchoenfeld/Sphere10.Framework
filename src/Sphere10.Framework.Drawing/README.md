<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 🎨 Sphere10.Framework.Drawing

Sphere10.Framework.Drawing adds color, geometry, image and GDI+ helpers on top of `System.Drawing`. Use it for color conversion and gradients, point and rectangle arithmetic, image resizing and encoding, rounded and bordered shapes, and GDI+ color matrices. Sphere10.Framework.Windows, Sphere10.Framework.Windows.Forms and Sphere10.Framework.Web.AspNetCore.MVC depend on it.

The color helpers and the `Point`, `Size` and `Rectangle` extensions run on any platform. The image, icon, codec, graphics-path and GDI+ drawing members use System.Drawing.Common, which supports them only on Windows, so they carry `[SupportedOSPlatform("windows")]` or `[SupportedOSPlatform("windows6.1")]`.

The `ResizeMethod` and `ResizeAlignment` enums live in the core `Sphere10.Framework` package, in the `Sphere10.Framework` namespace, so that browser components such as the Blazor grid can use them without `System.Drawing`. This assembly forwards both types, so binaries compiled against their earlier home in Drawing keep working.

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.Drawing` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|
| [Extensions](Extensions/README.md) | Provides extension methods for `System.Drawing` colors, color matrices, graphics, icons, images, byte arrays, points, sizes and rectangles. | [source](Extensions/) |

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`AnchorArea`](AnchorArea.cs) | class | Left, top, right and bottom anchor distances with center and middle flags, and `CalculateArea` that places content of a given size inside an area |
| [`BorderLine`](BorderLine.cs) | struct | One border edge: color, width, dash style and padding, with `NoBorder` and `Black1Width` presets |
| [`BrushCache`](BrushCache.cs) | class | Disposable cache of `SolidBrush`es by color with a fixed number of reusable slots, which owns every brush it returns until disposed |
| [`ColorMatrices`](ColorMatrices.cs) | static class | Builds and multiplies 5x5 GDI+ `ColorMatrix` values for brightness, contrast, inversion, monochrome, sepia, saturation, hue rotation, threshold and colour-vision-deficiency simulation, and interpolates between matrices |
| [`Gradient3DBorderStyle`](Gradient3DBorderStyle.cs) | enum | Raised or sunken style for `GraphicsExtensions.DrawGradient3DBorder` |
| [`GradientFillDirection`](GradientFillDirection.cs) | enum | Horizontal or vertical direction for `GraphicsExtensions.GradientFill` |
| [`GraphicsCache`](GraphicsCache.cs) | class | Borrows a `Graphics` and a clip rectangle and owns a `PenCache` and `BrushCache` for one drawing pass, disposing the caches but not the graphics |
| [`IBorder`](IBorder.cs) | interface | Border contract: content rectangle, extent, `Draw` into a `GraphicsCache` and hit-testing of a point against the border parts |
| [`Padding`](Padding.cs) | struct | Left, right, top and bottom float padding with content-rectangle and extent calculations |
| [`PenCache`](PenCache.cs) | class | Disposable cache of `Pen`s by color, width and dash style with a fixed number of reusable slots, which owns every pen it returns until disposed |
| [`PointFP`](FPoint.cs) | struct | Point with `FixedPoint` coordinates and conversion and vector add, subtract and divide helpers |
| [`RectangleBorder`](RectangleBorder.cs) | struct | `IBorder` with separate top, bottom, left and right `BorderLine`s, copy-on-set modifiers and inset and raised factory methods |
| [`RectanglePartType`](RectanglePartType.cs) | enum | Part of a bordered rectangle hit by a point: none, content area or one of the four borders |
| [`RoundedCorners`](RoundedCorners.cs) | enum | Flags that select which corners of a rectangle are rounded |
| [`RoundedRectangle`](RoundedRectangle.cs) | struct | Rectangle with a 0 to 1 rounding value that converts to a `GraphicsPath` |
| [`Tools.Drawing`](DrawingTool.cs) | tools | Color conversion, gradients and contrast, content-alignment and `StringFormat` mapping, image-format detection, JPEG compression, icon resizing, disabled images, and rounded-rectangle paths in `DrawingTool.Paths.cs` |
<!-- domains:end -->

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Drawing
```

The package targets `net10.0`, and also `net11.0` when it is built with a .NET 11 SDK. Projects inside this repository reference `src/Sphere10.Framework.Drawing/Sphere10.Framework.Drawing.csproj` instead.

## ⚡ 10-Second Example

```csharp
using System.Drawing;
using System.Drawing.Imaging;
using Sphere10.Framework;

// Colors work on any platform.
Color[] steps = Tools.Drawing.CalculateColorGradient(Color.Red, Color.Blue, 5);
Color accent = Tools.Drawing.ConvertStringToColor("255:0:128");   // "R:G:B" or "A:R:G:B"
Color caption = Tools.Drawing.GetContrastingTextColor(accent);     // Color.Black or Color.White

// Images need Windows.
using var photo = new Bitmap("photo.jpg");
using var thumbnail = photo.Resize(new Size(256, 256), ResizeMethod.AspectFitPadded,
	ResizeAlignment.CenterCenter, newPixelFormat: PixelFormat.Format32bppArgb);
thumbnail.Save("thumbnail.png", ImageFormat.Png);
```

## 🏗️ What It Contains

The extension classes and the `RoundedCorners`, `GradientFillDirection` and `PointFP` types are in the `Sphere10.Framework` namespace. The other drawing types are in `Sphere10.Framework.Drawing`, and `Tools.Drawing` is in the global `Tools` namespace.

| Area | Entry points | Platform |
|---|---|---|
| Color | `Tools.Drawing.CalculateColorGradient`, `CalculateMiddleColor`, `CalculateLightDarkColor`, `GetContrastingTextColor`, `ConvertStringToColor`, `ConvertColorToString` and `RandomColor`, and the `Color` extensions `ToARGBString`, `GetShade` and `GetTranslucentShade` | Any |
| Geometry | Extensions for `Point`, `PointF`, `Size`, `SizeF`, `Rectangle` and `RectangleF`, such as `CenterCenter`, `Translate`, `ExpandBy`, `ShrinkBy`, `IntersectWith`, `DistanceTo`, `ScaleBy` and `Clip`, and the fixed-point `PointFP` | Any, except `GetRoundPath` |
| Images | The `Image` extensions `Resize`, `Zoom`, `FocusZoom`, `Copy`, `ToByteArray` and `SaveJpegWithCompression`, `byte[].ToImage`, `Icon.ToBitmap(width, height)`, and the `Tools.Drawing` helpers for disabled images, image formats and codecs | Windows |
| GDI+ drawing | `Graphics` extensions for gradient fills, rounded rectangles and borders, 3D gradient borders, circles, and justified or ellipsis-truncated text, with `RoundedRectangle`, `RoundedCorners` and `GraphicsCache` | Windows |
| Borders and layout | `RectangleBorder`, `BorderLine`, `Padding`, `AnchorArea` and `IBorder`, which the Windows Forms visual elements use | Layout on any platform, `Draw` on Windows |
| Color matrices | `ColorMatrices` and the `ColorMatrix` extensions | Windows |

## 🔧 Core Examples

### Colors

```csharp
using System.Drawing;
using Sphere10.Framework;

Color[] gradient = Tools.Drawing.CalculateColorGradient(Color.White, Color.SteelBlue, 10);
Color middle = Tools.Drawing.CalculateMiddleColor(Color.White, Color.SteelBlue);
Color darker = Tools.Drawing.CalculateLightDarkColor(Color.SteelBlue, -0.3f);   // -1 is black, 1 is white
string stored = Color.FromArgb(200, 100, 150, 50).ToARGBString();               // "200:100:150:50"
Color restored = Tools.Drawing.ConvertStringToColor(stored);
```

`CalculateColorGradient` returns the requested number of colors, at least two, in equal steps from the first color to the second. `CalculateMiddleColor` returns the color halfway between them. `ConvertStringToColor` reads `R:G:B` or `A:R:G:B` decimal parts, and it returns `Color.Empty` for empty input or for any other number of parts.

### Resizing Images

```csharp
using System.Drawing;
using Sphere10.Framework;

using var original = new Bitmap("large_image.jpg");

using var stretched = original.Resize(new Size(400, 300));                        // ResizeMethod.Stretch
using var fitted = original.Resize(new Size(800, 600), ResizeMethod.AspectFit);   // can be smaller than 800 x 600
using var cropped = original.Resize(new Size(300, 300), ResizeMethod.AspectFill, ResizeAlignment.TopCenter);
using var half = original.Zoom(0.5f);

fitted.SaveJpegWithCompression("fitted.jpg", 85);                                 // JPEG quality from 0 to 100
```

| `ResizeMethod` | Result |
|---|---|
| `Stretch` (default) | Scales the width and height independently to the requested size. |
| `AspectFit` | Keeps the aspect ratio and fits inside the requested size, so the result can be smaller. |
| `AspectFitPadded` | Keeps the aspect ratio, returns the requested size and fills the margin with `paddingColor`. `ResizeAlignment` places the image. |
| `AspectFill` | Keeps the aspect ratio, returns the requested size and crops the source. `ResizeAlignment` chooses the crop. |

- `paddingColor` defaults to `Color.Transparent`, and the result keeps the source pixel format unless `newPixelFormat` sets another. Pass `PixelFormat.Format32bppArgb` for transparent padding around a source without an alpha channel.
- `interpolationMode` takes a `System.Drawing.Drawing2D.InterpolationMode` and defaults to `HighQualityBicubic`.
- Each method returns a new bitmap that the caller owns, and it leaves the source open. `ResizeAndDispose`, `ZoomAndDispose` and `FocusZoomAndDispose` dispose the source after use.
- A request that rounds to a zero width or height returns a 1 x 1 bitmap.
- `FocusZoom(zoomFactor, focusX, focusY)` magnifies the area around a point and fits it to the source dimensions. Near an edge that area is clipped to the source, and `resizeMethod` and `resizeAlignment` choose how the clipped area fills the result, so `Stretch` distorts it and `AspectFitPadded` pads it. `icon.ToBitmap(width, height)` renders an `Icon` at the requested size with `AspectFitPadded`.

### Disabled Images, Encoding and Formats

```csharp
using System.Drawing;
using System.Drawing.Imaging;
using Sphere10.Framework;

using var icon = Image.FromFile("save.png");
using var disabled = Tools.Drawing.CreateDisabledImage(icon, SystemColors.Control);   // grayed copy for disabled UI

byte[] png = icon.ToByteArray();                     // PNG unless another ImageFormat is given
byte[] jpeg = icon.ToByteArray(ImageFormat.Jpeg);

bool recognized = Tools.Drawing.TryGetImageFormatFromHeader("upload.bin", out ImageFormat format);
```

`CreateDisabledImage` returns a lightened grayscale copy, and its `background` argument does not change the result. `byte[].ToImage()` keeps its memory stream open for the image's lifetime, as GDI+ requires, and disposing the image is enough.

`TryGetImageFormatFromHeader` reads the first 11 bytes and recognizes the JPEG (JFIF or Exif), PNG, GIF, BMP and TIFF signatures. Its path overload opens the file for reading only and shares it with other readers and writers. Its stream overload reads from the current position and needs a stream that reports its `Length`. It also handles streams that return fewer bytes per read. `TryGetImageFormatFromFilenameExtension`, `TryGetFileExtensionFromImageFormat`, `IsRecognisedImageFile` and `GetEncoderInfo` map between file names, image formats and the installed codecs.

### Drawing with GDI+

```csharp
using System.Drawing;
using Sphere10.Framework;
using Sphere10.Framework.Drawing;

using var bitmap = new Bitmap(200, 80);
using var graphics = Graphics.FromImage(bitmap);
graphics.GradientFill(new Rectangle(0, 0, 200, 80), Color.SteelBlue, Color.White, GradientFillDirection.Vertical);

using var brush = new SolidBrush(Color.Orange);
graphics.FillRoundedRectangle(new RoundedRectangle(new Rectangle(10, 10, 180, 60), 0.25), brush);
graphics.DrawRoundedBorder(Color.DarkSlateGray, new Rectangle(0, 0, 200, 80), 8, 2, RoundedCorners.All);
```

`RoundedRectangle.RoundValue` runs from 0 for square corners to 1 for a fully rounded shape. The constructor and the setter throw `ApplicationException` for any other value, and `ToGraphicsPath()` converts the shape for other GDI+ calls. The `RoundedCorners` flags choose which corners the integer-radius helpers round. `GraphicsCache` wraps a borrowed `Graphics` with a `PenCache` and a `BrushCache` that own the pens and brushes they return. Disposing the cache releases those pens and brushes and leaves the `Graphics` open.

### Geometry

```csharp
using System.Drawing;
using Sphere10.Framework;

var bounds = new Rectangle(10, 10, 100, 50);
Point center = bounds.CenterCenter();                           // (60, 35)
Rectangle moved = bounds.Translate(5, 5);                       // X = 15, Y = 15
Rectangle larger = bounds.ExpandBy(20, 10);                     // 120 x 60 at the same location
Rectangle visible = bounds.IntersectWith(0, 0, 50, 50);         // (10, 10, 40, 40)
float distance = new Point(0, 0).DistanceTo(new Point(3, 4));   // 5
Size half = new Size(800, 600).ScaleBy(0.5);                    // 400 x 300
```

The rectangle helpers return modified copies, because `Rectangle` is a value type. `ExpandBy` and `ShrinkBy` change only the width and height. `PointF`, `SizeF` and `RectangleF` have matching extensions.

### Color Matrices

`ColorMatrices` builds and combines the 5x5 GDI+ `ColorMatrix`. It uses the row-vector convention of `ImageAttributes.SetColorMatrix`, which the Windows Magnification API shares, so one matrix can drive both an image preview and a desktop color effect.

```csharp
using Sphere10.Framework;
using Sphere10.Framework.Drawing;

var negative = ColorMatrices.FromArray(new[,] {
	{ -1f, 0f, 0f, 0f, 0f }, { 0f, -1f, 0f, 0f, 0f }, { 0f, 0f, -1f, 0f, 0f }, { 0f, 0f, 0f, 1f, 0f }, { 1f, 1f, 1f, 0f, 1f }
});
var adjusted = negative.WithBrightnessContrast(brightness: 0.1f, contrast: 1.2f);   // negative, then contrast, then brightness
var frames = ColorMatrices.Interpolate(ColorMatrices.Identity, adjusted, steps: 10);  // fade frames ending exactly at the target
var same = adjusted.ApproximatelyEquals(negative.Then(ColorMatrices.CreateContrast(1.2f)).Then(ColorMatrices.CreateBrightness(0.1f)));
```

`Multiply(first, second)` and `first.Then(second)` apply `first` before `second`. `CreateBrightness` adds an offset to the red, green and blue channels, and `CreateContrast` scales those channels around mid-gray. `Interpolate` returns `steps` frames that exclude the start matrix and end exactly at the target. `ToArray`, `ToJaggedArray` and `Clone` return independent copies. `ApproximatelyEquals`, `IsIdentity` and `ColorMatrices.AreEqual` compare matrices within a tolerance.

## ⚠️ Constraints

- Dispose every `Bitmap`, `Image`, `Graphics`, `Pen` and `Brush` you create. Methods that return a bitmap or an image give it to the caller.
- Windows-only members can fail at run time on other platforms. The .NET platform compatibility analyzer reports calls to them from cross-platform code at build time.
- `CalculateColorGradient` throws `ArgumentException` for fewer than two colors, and `CalculateLightDarkColor` throws it for a value outside -1 to 1.

## 📦 Dependencies

- **Sphere10.Framework**: the core library, which also defines `ResizeMethod` and `ResizeAlignment`
- **System.Drawing.Common**: GDI+ imaging and drawing, supported on Windows only
- **System.Formats.Nrbf**: referenced directly, because the System.Drawing.Common 11 release candidate omits this serialization dependency on .NET 10

## 📖 Related Projects

- [Sphere10.Framework](../Sphere10.Framework/README.md) - Core framework
- [Sphere10.Framework.Windows.Forms](../Sphere10.Framework.Windows.Forms/README.md) - Windows Forms controls and visual elements that draw with these types
- [Sphere10.Framework.Windows](../Sphere10.Framework.Windows/README.md) - Windows integration, whose `FileIconCache` sizes icons with the image helpers
- [Tools reference](../../docs/tools-reference.md) - The `Tools.*` catalog, including `Tools.Drawing`
- [SourceGrid and reusable desktop components](../../docs/usage/sourcegrid.md) - The helpers that moved here from the grid engine
- [Sphere10.Framework.Windows.Forms.Tests](../../tests/Sphere10.Framework.Windows.Forms.Tests/README.md) - `ColorMatricesTests`, `DrawingExtractionTests` and `DevAgeDrawingReuseTests` cover this library

## ⚖️ License

Distributed under the **MIT License**.

See the [LICENSE](../../LICENSE) file for full details. More information: [MIT License](https://opensource.org/license/mit)

Parts of `Tools.Drawing`, `GraphicsExtensions`, `GraphicsCache`, `PenCache`, `BrushCache`, `RoundedRectangle`, `RectangleBorder`, `BorderLine`, `Padding`, `AnchorArea`, `IBorder`, `RectanglePartType` and `Gradient3DBorderStyle` derive from the SourceGrid and DevAge code of Davide Icardi and Darius Damalakas (copyright 2005-2012), as their file headers record.

## 👤 Author

**Herman Schoenfeld** - Software Engineer


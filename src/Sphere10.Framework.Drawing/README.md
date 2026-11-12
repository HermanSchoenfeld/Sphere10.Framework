<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 🎨 Sphere10.Framework.Drawing

Sphere10.Framework.Drawing adds color, geometry, image and GDI+ helpers on top of `System.Drawing`. Use it for color conversion and gradients, point and rectangle arithmetic, image resizing and encoding, rounded and bordered shapes, and GDI+ color matrices. Sphere10.Framework.Windows, Sphere10.Framework.Windows.Forms and Sphere10.Framework.Web.AspNetCore.MVC depend on it.

The color helpers and the `Point`, `Size` and `Rectangle` extensions run on any platform. The image, icon, codec, graphics-path and GDI+ drawing members use System.Drawing.Common, which supports them only on Windows, so they carry `[SupportedOSPlatform("windows")]` or `[SupportedOSPlatform("windows6.1")]`.

The `ResizeMethod` and `ResizeAlignment` enums live in the core `Sphere10.Framework` package, in the `Sphere10.Framework` namespace, so that browser components such as the Blazor grid can use them without `System.Drawing`. This assembly forwards both types, so binaries compiled against their earlier home in Drawing keep working.

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
| [`ColorVisionDeficiency`](ColorVisionDeficiency.cs) | enum | Deficiencies that `ColorMatrices.CreateColorVisionDeficiency` simulates, from protanopia to achromatomaly |
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
| [`Tools.Drawing`](DrawingTool.cs) | tools | Color conversion, gradients and contrast, content-alignment and `StringFormat` mapping, image-format detection, JPEG compression, icon resizing, disabled images, and rounded-rectangle paths in [`DrawingTool.Paths.cs`](DrawingTool.Paths.cs) |
<!-- domains:end -->

## 🏗️ What It Contains

The extension classes and the `RoundedCorners`, `GradientFillDirection` and `PointFP` types are in the `Sphere10.Framework` namespace. The other drawing types are in `Sphere10.Framework.Drawing`, and `Tools.Drawing` is in the global `Tools` namespace.

| Area | Entry points | Platform |
|---|---|---|
| Color | `Tools.Drawing.CalculateColorGradient`, `CalculateMiddleColor`, `CalculateLightDarkColor`, `GetContrastingTextColor`, `ConvertStringToColor`, `ConvertColorToString` and `RandomColor`, and the `Color` extensions `ToARGBString`, `GetShade` and `GetTranslucentShade` | Any |
| Geometry | Extensions for `Point`, `PointF`, `Size`, `SizeF`, `Rectangle` and `RectangleF`, such as `CenterCenter`, `Translate`, `ExpandBy`, `ShrinkBy`, `IntersectWith`, `DistanceTo`, `ScaleBy` and `Clip`, and the fixed-point `PointFP` | Any, except `GetRoundPath` |
| Images | The `Image` extensions `Resize`, `Zoom`, `FocusZoom`, `Copy`, `ToByteArray` and `SaveJpegWithCompression`, `byte[].ToImage`, `Icon.ToBitmap(width, height)`, and the `Tools.Drawing` helpers for disabled images, image formats and codecs | Windows |
| GDI+ drawing | `Graphics` extensions for gradient fills, rounded rectangles and borders, 3D gradient borders, circles, and justified or ellipsis-truncated text, with `RoundedRectangle`, `RoundedCorners` and `GraphicsCache`, and `Tools.Drawing.CreateRoundedRectanglePath(bounds, radius, corners)` for a `GraphicsPath` with any combination of rounded corners | Windows |
| Borders and layout | `RectangleBorder`, `BorderLine`, `Padding`, `AnchorArea` and `IBorder`, which the Windows Forms visual elements use | Layout on any platform, `Draw` on Windows |
| Color matrices | `ColorMatrices` and the `ColorMatrix` extensions, including `Transform(color)` for one colour and `ApplyTo(image)`, which draws an image through the matrix into a new bitmap. Standard factories: `CreateInversion`, `CreateMonochrome` (BT.709 luma, optionally tinted), `CreateSepia`, `CreateSaturation` and `CreateHueRotation` (W3C Filter Effects), `CreateThreshold`, `CreateBrightness`, `CreateContrast` and `CreateColorVisionDeficiency` (Machado, Oliveira and Fernandes 2009, for the eight `ColorVisionDeficiency` values), plus `FromRgbTransform` for 3x3 column-vector matrices from papers and specifications. Compose with `Then`; for example a smart inversion is `CreateInversion().Then(CreateHueRotation(180))` | Windows |

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

### Color Strings

```csharp
using System;
using System.Drawing;
using Sphere10.Framework;

string[] colorStrings = {
	"255:0:0",          // red, as R:G:B
	"0:255:0",          // green
	"0:0:255",          // blue
	"128:255:128:0"     // half-transparent orange, as A:R:G:B
};

foreach (var colorString in colorStrings) {
	Color color = Tools.Drawing.ConvertStringToColor(colorString);
	Console.WriteLine($"{colorString} -> ARGB({color.A}, {color.R}, {color.G}, {color.B})");
}

// Round trip through the A:R:G:B text form.
Color original = Color.FromArgb(200, 100, 150, 50);
string text = Tools.Drawing.ConvertColorToString(original);   // "200:100:150:50", the same as original.ToARGBString()
Color converted = Tools.Drawing.ConvertStringToColor(text);
Console.WriteLine($"Match: {original == converted}");       // True
```

Three parts give an opaque color. Each part is parsed with `Int32.Parse`, so a part that is not a number throws `FormatException`. A part outside 0 to 255 makes `Color.FromArgb` throw `ArgumentException`. `ConvertColorToString` and `ToARGBString` return `null` for `Color.Empty`.

### Painting Gradients

```csharp
using System.Drawing;
using System.Drawing.Imaging;
using Sphere10.Framework;

Color[] gradient = Tools.Drawing.CalculateColorGradient(Color.Red, Color.Blue, 8);

using var swatches = new Bitmap(gradient.Length * 32, 80);
using (var graphics = Graphics.FromImage(swatches)) {
	// One solid swatch per gradient step across the top half.
	for (var i = 0; i < gradient.Length; i++) {
		using var brush = new SolidBrush(gradient[i]);
		graphics.FillRectangle(brush, i * 32, 0, 32, 40);
	}

	// A continuous GDI+ gradient between the same colors across the bottom half.
	graphics.GradientFill(new Rectangle(0, 40, swatches.Width, 40), Color.Red, Color.Blue, GradientFillDirection.Horizontal);
}
swatches.Save("gradient.png", ImageFormat.Png);
```

`GradientFill` paints with a horizontal or vertical `LinearGradientBrush`, and `GradientFillAtAngle` takes any angle in degrees. Both accept an optional `Blend`, and both fill with a solid brush when the two colors are equal.

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

### Rounded Images

```csharp
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Sphere10.Framework;

using var photo = new Bitmap("photo.jpg");

using var rounded = CreateRoundedImage(photo, 30);
rounded.Save("rounded_photo.png", ImageFormat.Png);

using var bordered = CreateRoundedImage(photo, 30, 5, Color.DarkGray);
bordered.Save("rounded_with_border.png", ImageFormat.Png);

// A radius of half the shorter side turns a square image into a circle.
using var square = photo.Resize(new Size(256, 256), ResizeMethod.AspectFill);
using var circular = CreateRoundedImage(square, 128);
circular.Save("circular_photo.png", ImageFormat.Png);

static Bitmap CreateRoundedImage(Image source, int radius, int borderWidth = 0, Color borderColor = default) {
	var bounds = new Rectangle(0, 0, source.Width, source.Height);
	var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
	using var graphics = Graphics.FromImage(result);
	graphics.SmoothingMode = SmoothingMode.AntiAlias;

	// Draw the image through a rounded clip, which leaves the corners transparent.
	using (var clip = Tools.Drawing.CreateRoundedRectanglePath(bounds, radius)) {
		graphics.SetClip(clip);
		graphics.DrawImage(source, bounds);
		graphics.ResetClip();
	}

	// Inset the border path by half the pen width so the whole stroke stays inside the image.
	if (borderWidth > 0) {
		var inset = borderWidth / 2f;
		using var borderPath = Tools.Drawing.CreateRoundedRectanglePath(RectangleF.Inflate(bounds, -inset, -inset), Math.Max(0, radius - inset));
		using var pen = new Pen(borderColor, borderWidth);
		graphics.DrawPath(pen, borderPath);
	}
	return result;
}
```

The library has no single call that rounds an image. Clip to `Tools.Drawing.CreateRoundedRectanglePath` and draw the image through it, as `CreateRoundedImage` above does. The path limits the radius to half the shorter side, so a larger radius gives a pill or a circle. Pass `RoundedCorners` flags to round only some corners, for example `RoundedCorners.TopLeft | RoundedCorners.TopRight` for a tab. Save the result as PNG, because JPEG has no alpha channel and loses the transparent corners.

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

### Points and Rectangles

```csharp
using System.Drawing;
using Sphere10.Framework;

var rect = new Rectangle(10, 10, 100, 100);
Point topLeft = rect.TopLeft();                                  // (10, 10)
Point bottomRight = rect.BottomRight();                          // (110, 110)
Point rightMiddle = rect.CenterRight();                          // (110, 60)

Point offset = new Point(10, 20).Add(new Size(5, 10));           // (15, 30)
Point span = bottomRight.Subtract(topLeft);                      // (100, 100)

Rectangle placed = rect.SetLocation(0, 0).SetSize(200, 50);      // (0, 0, 200, 50)
Rectangle upright = placed.AsLongRectangle();                    // (0, 0, 50, 200)
Rectangle grown = rect.TranslateAndExpandBy(-20, -20, 40, 40);   // 20 px more on every side: (-10, -10, 140, 140)
bool inside = grown.Contains(new Point(-5, -5));                 // true
int area = rect.Width * rect.Height;                             // 10000
```

`TopLeft`, `TopCenter`, `CenterLeft`, `CenterCenter`, `BottomRight` and the other position helpers return the edge and center points of a rectangle. `SetLocation`, `SetSize`, `SetWidth` and `SetHeight` return a copy with one part replaced. `TranslateAndExpandBy` moves and grows a rectangle in one call, and `TranslateNegativeAndShrinkBy` reverses it. `AsWideRectangle` and `AsLongRectangle` swap the width and height when needed, so that the longer side runs across or down. Containment and area come from `System.Drawing` itself.

### Sizes

```csharp
using System;
using System.Drawing;
using Sphere10.Framework;

var original = new Size(800, 600);
Size larger = original.ScaleBy(1.5);                                     // 1200 x 900
Size limited = larger.Clip(new Size(320, 240), new Size(1024, 768));     // 1024 x 768
Size framed = original.ExpandBy(new Size(20, 20));                       // 820 x 620
Size inner = original.ShrinkBy(new Point(16, 16));                       // 784 x 584

// Fit inside 400 x 400 and keep the 4:3 aspect ratio.
var bounds = new Size(400, 400);
Size fitted = original.ScaleBy(Math.Min((double)bounds.Width / original.Width, (double)bounds.Height / original.Height));   // 400 x 300

Size pixels = new SizeF(120.4f, 33.6f).ToSize();                         // 121 x 34, rounded up
```

`Clip(minSize, maxSize)` ignores a minimum or maximum dimension of zero. `ScaleBy` rounds to the nearest pixel, and `SizeF.ToSize()` rounds each dimension up. The size helpers do not keep an aspect ratio by themselves. Scale by the smaller of the two ratios as above, or let `Image.Resize` with `ResizeMethod.AspectFit` size a bitmap.

### Borders and Layout

```csharp
using System.Drawing;
using Sphere10.Framework.Drawing;

var cell = new RectangleF(0, 0, 120, 40);
var border = RectangleBorder.CreateRaisedBorder(2, Color.Gray, Color.White);
RectangleF content = border.GetContentRectangle(cell);                    // (2, 2, 116, 36)
SizeF required = border.GetExtent(new SizeF(80, 20));                     // 84 x 24
RectanglePartType part = border.GetPointPartType(cell, new PointF(1, 20), out float fromEdge);   // LeftBorder, 1

// Place 16 x 16 content 4 pixels from the right edge, centered vertically.
var anchor = new AnchorArea { Right = 4, Middle = true };
RectangleF icon = AnchorArea.CalculateArea(content, new SizeF(16, 16), anchor);   // (98, 12, 16, 16)

using var bitmap = new Bitmap(120, 40);
using var graphics = Graphics.FromImage(bitmap);
using var cache = new GraphicsCache(graphics);
border.Draw(cache, cell);
```

`RectangleBorder` holds one `BorderLine` per edge, and `CreateInsetBorder` and `CreateRaisedBorder` shade the edges for a 3D look. `GetContentRectangle` and `GetExtent` convert between the outer area and the content inside the border widths and paddings. `GetPointPartType` reports which part a point hits. Its `out` distance is measured from the outer edge, and it is -1 for a point in the content area or outside the area. `AnchorArea.CalculateArea` places content inside an area from its `Left`, `Top`, `Right` and `Bottom` distances, or centers it when `Center` or `Middle` is set. A distance of `float.NaN` is unset. The layout members run on any platform, and `Draw` needs Windows.

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


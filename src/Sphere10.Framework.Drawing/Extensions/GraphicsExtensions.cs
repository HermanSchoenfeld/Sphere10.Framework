// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework;

[SupportedOSPlatform("windows")]
public static class GraphicsExtensions {
	// Auxillary Fields & Methods
	private static readonly Bitmap _measurementBitmap = new Bitmap(1, 1);

	/// <summary>Fills a fractional rounded rectangle within its inclusive pixel boundary.</summary>
	public static void FillRoundedRectangle(this Graphics graphics, RoundedRectangle rectangle, Brush brush) {
		// Keep the one-pixel fill inset used by native visual elements.
		var border = new Rectangle(rectangle.Rectangle.X, rectangle.Rectangle.Y, rectangle.Rectangle.Width - 1, rectangle.Rectangle.Height - 1);
		var shape = new RoundedRectangle(border, rectangle.RoundValue);
		using var path = shape.ToGraphicsPath();
		using var region = new Region(path);
		graphics.FillRegion(brush, region);
	}

	/// <summary>Draws raised or sunken gradient borders inside a rectangle.</summary>
	public static void DrawGradient3DBorder(this Graphics graphics, Rectangle rectangle, Color backColor, Color darkColor, Color lightColor,
		int darkBorderWidth, int lightBorderWidth, Gradient3DBorderStyle style) {
		var topLeftColor = style == Gradient3DBorderStyle.Raised ? lightColor : darkColor;
		var topLeftWidth = style == Gradient3DBorderStyle.Raised ? lightBorderWidth : darkBorderWidth;
		var bottomRightColor = style == Gradient3DBorderStyle.Raised ? darkColor : lightColor;
		var bottomRightWidth = style == Gradient3DBorderStyle.Raised ? darkBorderWidth : lightBorderWidth;

		// The top and left edges run from the strongest shade towards the background.
		var topLeftGradient = Tools.Drawing.CalculateColorGradient(backColor, topLeftColor, topLeftWidth);
		using (var pen = new Pen(topLeftGradient[0])) {
			for (var index = 0; index < topLeftGradient.Length; index++) {
				pen.Color = topLeftGradient[topLeftGradient.Length - index - 1];
				graphics.DrawLine(pen, rectangle.Left + index, rectangle.Top + index, rectangle.Right - index - 1, rectangle.Top + index);
				graphics.DrawLine(pen, rectangle.Left + index, rectangle.Top + index, rectangle.Left + index, rectangle.Bottom - index - 1);
			}
		}

		// The bottom and right edges use the opposite shade with the same inward geometry.
		var bottomRightGradient = Tools.Drawing.CalculateColorGradient(backColor, bottomRightColor, bottomRightWidth);
		using (var pen = new Pen(bottomRightGradient[0])) {
			for (var i = 0; i < bottomRightGradient.Length; i++) {
				pen.Color = bottomRightGradient[bottomRightGradient.Length - i - 1];
				graphics.DrawLine(pen, rectangle.Left + i, rectangle.Bottom - i - 1, rectangle.Right - i - 1, rectangle.Bottom - i - 1);
				graphics.DrawLine(pen, rectangle.Right - i - 1, rectangle.Top + i, rectangle.Right - i - 1, rectangle.Bottom - i - 1);
			}
		}
	}

	public static void DrawCircle(this Graphics graphics, Pen pen, int x, int y, int radius) {
		graphics.DrawEllipse(pen, new Rectangle(x - radius, y - radius, radius * 2, radius * 2));
	}

	public static void DrawAlphaColor(this Graphics graphics, Bitmap image, Color color, int x, int y) {
		var imageAttributes = new ImageAttributes();
		var destinationRectangle = new Rectangle(x, y, image.Width, image.Height);

		imageAttributes.SetColorKey(color, color);
		graphics.DrawImage(image, destinationRectangle, x, y, image.Width, image.Height, GraphicsUnit.Pixel, imageAttributes);
	}

	public static void DrawAlphaColor(this Graphics graphics, Bitmap image, Color color, Rectangle destinationRectangle, Rectangle sourceRectangle) {
		var imageAttributes = new ImageAttributes();

		imageAttributes.SetColorKey(color, color);
		graphics.DrawImage(image, destinationRectangle, sourceRectangle.X, sourceRectangle.Y, sourceRectangle.Width, sourceRectangle.Height, GraphicsUnit.Pixel, imageAttributes);
	}

	public static Bitmap CopyRegion(Bitmap sourceBitmap, Rectangle section) {
		// Create the new bitmap and associated graphics object
		var bitmap = new Bitmap(section.Width, section.Height);
		using (var graphics = Graphics.FromImage(bitmap)) {
			// Draw the specified section of the source bitmap to the new one
			graphics.DrawImage(sourceBitmap, section.X, section.Y);

			// Clean up
			graphics.Dispose();
		}
		// Return the bitmap
		return bitmap;
	}

	public static void DrawJustifiedString(this Graphics graphics, int x, int y, int width, string text, Font textFont, Color color) {
		DrawJustifiedString(graphics, x, y, width, text, textFont, color, false);
	}

	public static void DrawJustifiedString(this Graphics graphics, int x, int y, int width, string text, Font textFont, Color color, bool format) {
		// Prepare the wrapped text and initial drawing position.
		if (format)
			text = GetSizedString(width, text, textFont);
		int currentX = x, currentY = y;

		// Draw the final line naturally and distribute spacing across the other lines.
		using (var textBrush = new SolidBrush(color)) {
			foreach (var line in text.Split('\n')) {
				if (text.Split('\n').Last() == line)
					graphics.DrawString(line, textFont, textBrush, currentX, currentY);
				else {
					var words = line.Split(' ');
					var lineMeasure = GetLineMeasure(line, graphics, textFont);
					var spaceWidth = (width - (int)lineMeasure.Width) / (words.Length > 1 ? words.Length - 1 : words.Length);

					foreach (var word in words) {
						var wordMeasure = graphics.MeasureString(word, textFont);
						graphics.DrawString(word, textFont, textBrush, currentX, currentY);
						currentX += (int)wordMeasure.Width + spaceWidth;
					}

					// Return to the left margin for the next line.
					currentY += (int)lineMeasure.Height;
					currentX = x;
				}
			}
		}
	}

	public static int GetSizedStringHeight(string text, Font textFont) {
		int height;

		using (var measurementGraphics = Graphics.FromImage(_measurementBitmap))
			height = (int)measurementGraphics.MeasureString(text, textFont).Height + 5;

		return height;
	}

	public static SizeF GetEllipsisStringMeasure(int width, string text, Font textFont) {
		using (var measurementGraphics = Graphics.FromImage(_measurementBitmap)) {
			text = EllipsisWord(width, text, textFont);
			var result = measurementGraphics.MeasureString(text, textFont);

			return result;
		}
	}

	public static string EllipsisWord(int width, string text, Font textFont) {
		var resultBuilder = new StringBuilder();
		var lineBuilder = new StringBuilder();

		using (var measurementGraphics = Graphics.FromImage(_measurementBitmap)) {
			if (measurementGraphics.MeasureString(text, textFont).Width < width)
				return text;

			foreach (var character in text) {
				lineBuilder.Append(character).Append("...");

				var textMeasure = measurementGraphics.MeasureString(lineBuilder.ToString(), textFont);

				if (textMeasure.Width > width) {
					resultBuilder.Append("...");
					break;
				}
				resultBuilder.Append(character);
				lineBuilder = new StringBuilder(resultBuilder.ToString());
			}
		}

		return resultBuilder.ToString();
	}

	public static string GetSizedString(int width, string text, Font textFont) {
		if (string.IsNullOrEmpty(text))
			return "";

		// Accumulate complete lines separately from the current line buffer.
		var buffer = "";
		var formattedText = "";

		// Fit words to each measured line, splitting oversized words when needed.
		using (var measurementGraphics = Graphics.FromImage(_measurementBitmap)) {
			var words = text.Split(' ');
			foreach (var word in words) {
				string candidateText;
				if (string.IsNullOrEmpty(buffer))
					candidateText = word;
				else
					candidateText = buffer + " " + word;
				var textMeasure = measurementGraphics.MeasureString(candidateText, textFont);

				if (textMeasure.Width > width) {
					if (string.IsNullOrEmpty(formattedText))
						formattedText = buffer;
					else
						formattedText += "\n" + buffer;
					buffer = word;

					if (measurementGraphics.MeasureString(buffer, textFont).Width > width)
						buffer = SplitWord(width, buffer, textFont);
				} else
					buffer = candidateText;
			}
		}

		// Flush the final buffered line after the fitting pass.
		if (string.IsNullOrEmpty(formattedText))
			formattedText = buffer;
		else
			formattedText += "\n" + buffer;

		return formattedText;
	}

	public static void DrawRoundedBorder(this Graphics graphics, Color color, Rectangle rectangle, int radius, int borderWidth, RoundedCorners corners) {
		using (var borderBitmap = new Bitmap(rectangle.Width, rectangle.Height))
		using (var borderGraphics = Graphics.FromImage(borderBitmap)) {
			// Paint the outer rounded shape onto the transparency-key background.
			var gradientRectangle = new Rectangle(0, 0, rectangle.Width, rectangle.Height);
			borderGraphics.Clear(Color.Green);
			borderGraphics.DrawRoundedRectangle(color, gradientRectangle, radius, corners);

			// Cut out the inner area to retain only the requested border width.
			gradientRectangle.Height -= borderWidth << 1;
			gradientRectangle.Width -= borderWidth << 1;
			gradientRectangle.X += borderWidth;
			gradientRectangle.Y += borderWidth;
			borderGraphics.DrawRoundedRectangle(Color.Green, gradientRectangle, radius - borderWidth, corners);

			// Composite the border while leaving the keyed pixels transparent.
			var maskAttr = new ImageAttributes();
			maskAttr.SetColorKey(Color.Green, Color.Green);
			graphics.DrawImage(borderBitmap, rectangle, 0, 0, borderBitmap.Width, borderBitmap.Height, GraphicsUnit.Pixel, maskAttr);
		}
	}

	public static void DrawRoundedRectangleGradiantFill(this Graphics graphics, Color start, Color end, Rectangle rectangle, int radius, RoundedCorners corners) {
		DrawRoundedRectangleBorderGradiantFill(graphics, start, end, Color.White, rectangle, radius, 0, corners);
	}

	public static void DrawRoundedRectangleGradiantFill(this Graphics graphics, Color start, Color end, Rectangle rectangle, int radius) {
		DrawRoundedRectangleBorderGradiantFill(graphics, start, end, Color.White, rectangle, radius, 0);
	}

	public static void DrawRoundedRectangleBorderGradiantFill(this Graphics graphics, Color start, Color end, Color borderColor,
		Rectangle rectangle, int radius, int borderWidth, RoundedCorners corners) {
		using (var gradientBitmap = new Bitmap(rectangle.Width, rectangle.Height))
		using (var gradientGraphics = Graphics.FromImage(gradientBitmap))
		using (var maskBitmap = new Bitmap(rectangle.Width, rectangle.Height))
		using (var maskGraphics = Graphics.FromImage(maskBitmap)) {
			// Prepare the gradient across the complete temporary surface.
			var gradientRectangle = new Rectangle(0, 0, rectangle.Width, rectangle.Height);
			gradientGraphics.GradientFill(gradientRectangle, start, end, GradientFillDirection.Vertical);

			// Build the rounded mask and apply its keyed interior to the gradient.
			maskGraphics.Clear(Color.White);
			maskGraphics.DrawRoundedRectangleBorder(Color.Green, borderColor, gradientRectangle, radius, borderWidth, corners);
			var maskAttr = new ImageAttributes();
			maskAttr.SetColorKey(Color.Green, Color.Green);
			gradientGraphics.DrawImage(maskBitmap, gradientRectangle, 0, 0, gradientRectangle.Width, gradientRectangle.Height, GraphicsUnit.Pixel, maskAttr);

			// Blit the composed surface using its existing transparency key.
			graphics.DrawAlphaFirstPix(gradientBitmap, rectangle, gradientRectangle);
		}
	}

	public static void DrawAlphaFirstPix(this Graphics graphics, Bitmap image, int x, int y, int width, int height) {
		var imageAttributes = new ImageAttributes();
		var color = GetTransparentColor(image);
		var clientArea = new Rectangle(x, y, width, height);

		imageAttributes.SetColorKey(color, color);
		graphics.DrawImage(image, clientArea, 0, 0, clientArea.Width, clientArea.Height, GraphicsUnit.Pixel, imageAttributes);
	}

	public static void DrawAlphaFirstPix(this Graphics graphics, Bitmap image, int x, int y, int width, int height, int offset) {
		var imageAttributes = new ImageAttributes();
		var color = GetTransparentColor(image);
		var clientArea = new Rectangle(x, y, width, height);

		imageAttributes.SetColorKey(color, color);
		graphics.DrawImage(image, clientArea, offset, 0, clientArea.Width, clientArea.Height, GraphicsUnit.Pixel, imageAttributes);
	}

	public static void DrawAlphaFirstPix(this Graphics graphics, Bitmap image, Rectangle destinationRectangle, Rectangle sourceRectangle) {
		var imageAttributes = new ImageAttributes();
		var color = GetTransparentColor(image);

		imageAttributes.SetColorKey(color, color);
		graphics.DrawImage(image, destinationRectangle, sourceRectangle.X, sourceRectangle.Y, sourceRectangle.Width, sourceRectangle.Height, GraphicsUnit.Pixel, imageAttributes);
	}

	public static void DrawRoundedRectangleBorder(this Graphics graphics, Color color, Color borderColor, Rectangle rectangle, int radius, int borderWidth, RoundedCorners corners) {
		graphics.DrawRoundedRectangle(borderColor, rectangle, radius, corners);

		if (borderWidth == 0)
			return;

		rectangle.Height -= borderWidth * 2;
		rectangle.Width -= borderWidth * 2;
		rectangle.X += borderWidth;
		rectangle.Y += borderWidth;
		graphics.DrawRoundedRectangle(color, rectangle, radius - borderWidth, corners);
	}

	/// <summary>Draws a fractional rounded rectangle inside the specified pen boundary.</summary>
	public static void DrawRoundedRectangle(this Graphics graphics, RoundedRectangle rectangle, Pen pen) {
		// Inset the shape by half the pen width, retaining the original integer boundary calculation.
		var penWidth = (int)pen.Width;
		var border = new Rectangle(rectangle.Rectangle.X + penWidth / 2, rectangle.Rectangle.Y + penWidth / 2,
			rectangle.Rectangle.Width - penWidth, rectangle.Rectangle.Height - penWidth);
		var shape = new RoundedRectangle(border, rectangle.RoundValue);
		using var path = shape.ToGraphicsPath();
		graphics.DrawPath(pen, path);
	}

	public static void DrawRoundedRectangle(this Graphics graphics, Color color, Rectangle rectangle, int radius, RoundedCorners corners) {
		using (var brush = new SolidBrush(color)) {
			var x = rectangle.X;
			var y = rectangle.Y;
			var diameter = radius * 2;
			var horizontalRectangle = new Rectangle(x, y + radius, rectangle.Width, rectangle.Height - diameter);
			var verticalRectangle = new Rectangle(x + radius, y, rectangle.Width - diameter, rectangle.Height);

			graphics.FillRectangle(brush, horizontalRectangle);
			graphics.FillRectangle(brush, verticalRectangle);

			if ((corners & RoundedCorners.TopLeft) == RoundedCorners.TopLeft)
				graphics.FillEllipse(brush, x, y, diameter, diameter);
			else
				graphics.FillRectangle(brush, x, y, diameter, diameter);

			if ((corners & RoundedCorners.TopRight) == RoundedCorners.TopRight)
				graphics.FillEllipse(brush, x + rectangle.Width - (diameter + 1), y, diameter, diameter);
			else
				graphics.FillRectangle(brush, x + rectangle.Width - (diameter + 1), y, diameter, diameter);

			if ((corners & RoundedCorners.BottomLeft) == RoundedCorners.BottomLeft)
				graphics.FillEllipse(brush, x, y + rectangle.Height - (diameter + 1), diameter, diameter);
			else
				graphics.FillRectangle(brush, x, y + rectangle.Height - (diameter + 1), diameter, diameter);

			if ((corners & RoundedCorners.BottomRight) == RoundedCorners.BottomRight)
				graphics.FillEllipse(brush, x + rectangle.Width - (diameter + 1), y + rectangle.Height - (diameter + 1), diameter, diameter);
			else
				graphics.FillRectangle(brush, x + rectangle.Width - (diameter + 1), y + rectangle.Height - (diameter + 1), diameter, diameter);
		}
	}

	public static void DrawRoundedRectangleBorder(this Graphics graphics, Color color, Color borderColor, Rectangle rectangle, int radius, int borderWidth) {
		DrawRoundedRectangleBorder(graphics, color, borderColor, rectangle, radius, borderWidth, RoundedCorners.All);
	}

	public static void GradientFill(this Graphics graphics, Rectangle rectangle, Color fromColor, Color toColor, GradientFillDirection direction, Blend blend = null) {
		var angle = 0;
		switch (direction) {
			case GradientFillDirection.Horizontal:
				angle = 0;
				break;
			case GradientFillDirection.Vertical:
				angle = 90;
				break;
		}
		graphics.GradientFillAtAngle(rectangle, fromColor, toColor, angle, blend);
	}

	public static void GradientFillAtAngle(this Graphics graphics, Rectangle rectangle, Color fromColor, Color toColor, int angle = 0, Blend blend = null) {
		if ((fromColor != Color.Empty || toColor != Color.Empty) && (rectangle.Width != 0 && rectangle.Height != 0)) {
			if (fromColor == toColor) {
				using (Brush brush = new SolidBrush(fromColor)) {
					graphics.FillRectangle(brush, rectangle);
				}
			} else {
				using (var brush = new LinearGradientBrush(rectangle, fromColor, toColor, angle)) {
					if (blend != null)
						brush.Blend = blend;

					graphics.FillRectangle(brush, rectangle);
				}
			}
		}
	}

	public static void DrawRoundedRectangleBorderGradiantFill(this Graphics graphics, Color start, Color end, Color borderColor, Rectangle rectangle, int radius, int borderWidth) {
		DrawRoundedRectangleBorderGradiantFill(graphics, start, end, borderColor, rectangle, radius, borderWidth, RoundedCorners.All);
	}

	// Uses the top-left image pixel as the existing transparency key.
	private static Color GetTransparentColor(Bitmap image) {
		return image.GetPixel(0, 0);
	}

	// Breaks an oversized word into measured fragments without changing its characters.
	private static string SplitWord(int width, string text, Font textFont) {
		// Prepare the fragment buffers and their measurement surface.
		var buffer = "";
		var formattedText = "";
		var measurementBitmap = new Bitmap(100, 100);
		var measurementGraphics = Graphics.FromImage(measurementBitmap);

		// Fit one character at a time before flushing a completed fragment.
		foreach (var character in text.ToCharArray()) {
			var candidateText = "";
			if (string.IsNullOrEmpty(buffer))
				candidateText = character + "";
			else
				candidateText = buffer + character;
			var textMeasure = measurementGraphics.MeasureString(candidateText, textFont);

			if (textMeasure.Width > width) {
				if (string.IsNullOrEmpty(formattedText))
					formattedText = buffer;
				else
					formattedText += "\n" + buffer;
				buffer = character + "";

				if (measurementGraphics.MeasureString(buffer, textFont).Width > width)
					buffer = SplitWord(width, buffer, textFont);
			} else
				buffer = candidateText;
		}

		// Release the measurement resources in their existing order.
		measurementBitmap.Dispose();
		measurementGraphics.Dispose();

		// Flush the final buffered line after the fitting pass.
		if (string.IsNullOrEmpty(formattedText))
			formattedText = buffer;
		else
			formattedText += "\n" + buffer;

		return formattedText;
	}

	// Measures the visible characters of a line without its spaces.
	private static SizeF GetLineMeasure(string line, Graphics graphics, Font textFont) {
		var candidateText = line.Replace(" ", "");
		return graphics.MeasureString(candidateText, textFont);
	}

	private struct Rect {
		public int Left, Top, Right, Bottom;

		public Rect(Rectangle rectangle) {
			Left = rectangle.Left;
			Top = rectangle.Top;
			Bottom = rectangle.Bottom;
			Right = rectangle.Right;
		}
	}
}

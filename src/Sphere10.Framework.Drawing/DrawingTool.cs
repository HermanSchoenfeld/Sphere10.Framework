// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Sphere10.Framework;

// ReSharper disable CheckNamespace
namespace Tools;

public partial class Drawing {
	/// <summary>Returns the content alignment represented by a string format.</summary>
	[SupportedOSPlatform("windows")]
	public static ContentAlignment StringFormatToContentAlignment(StringFormat format) {
		return (format.LineAlignment, format.Alignment) switch {
			(StringAlignment.Far, StringAlignment.Near) => ContentAlignment.BottomLeft,
			(StringAlignment.Far, StringAlignment.Far) => ContentAlignment.BottomRight,
			(StringAlignment.Far, StringAlignment.Center) => ContentAlignment.BottomCenter,
			(StringAlignment.Near, StringAlignment.Near) => ContentAlignment.TopLeft,
			(StringAlignment.Near, StringAlignment.Far) => ContentAlignment.TopRight,
			(StringAlignment.Near, StringAlignment.Center) => ContentAlignment.TopCenter,
			(StringAlignment.Center, StringAlignment.Near) => ContentAlignment.MiddleLeft,
			(StringAlignment.Center, StringAlignment.Far) => ContentAlignment.MiddleRight,
			_ => ContentAlignment.MiddleCenter
		};
	}

	/// <summary>Applies content alignment to the horizontal and vertical string-format axes.</summary>
	[SupportedOSPlatform("windows")]
	public static void ApplyContentAlignmentToStringFormat(ContentAlignment alignment, StringFormat format) {
		format.LineAlignment = IsBottom(alignment) ? StringAlignment.Far : IsMiddle(alignment) ? StringAlignment.Center : StringAlignment.Near;
		format.Alignment = IsRight(alignment) ? StringAlignment.Far : IsCenter(alignment) ? StringAlignment.Center : StringAlignment.Near;
	}

	/// <summary>Tests whether content is aligned to the bottom edge.</summary>
	[SupportedOSPlatform("windows")]
	public static bool IsBottom(ContentAlignment alignment) => alignment is ContentAlignment.BottomCenter or ContentAlignment.BottomLeft or ContentAlignment.BottomRight;

	/// <summary>Tests whether content is aligned to the top edge.</summary>
	[SupportedOSPlatform("windows")]
	public static bool IsTop(ContentAlignment alignment) => alignment is ContentAlignment.TopCenter or ContentAlignment.TopLeft or ContentAlignment.TopRight;

	/// <summary>Tests whether content is vertically centered.</summary>
	[SupportedOSPlatform("windows")]
	public static bool IsMiddle(ContentAlignment alignment) => alignment is ContentAlignment.MiddleCenter or ContentAlignment.MiddleLeft or ContentAlignment.MiddleRight;

	/// <summary>Tests whether content is horizontally centered.</summary>
	[SupportedOSPlatform("windows")]
	public static bool IsCenter(ContentAlignment alignment) => alignment is ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter;

	/// <summary>Tests whether content is aligned to the left edge.</summary>
	[SupportedOSPlatform("windows")]
	public static bool IsLeft(ContentAlignment alignment) => alignment is ContentAlignment.BottomLeft or ContentAlignment.MiddleLeft or ContentAlignment.TopLeft;

	/// <summary>Tests whether content is aligned to the right edge.</summary>
	[SupportedOSPlatform("windows")]
	public static bool IsRight(ContentAlignment alignment) => alignment is ContentAlignment.BottomRight or ContentAlignment.MiddleRight or ContentAlignment.TopRight;

	/// <summary>Tests whether a string format aligns content to the bottom edge.</summary>
	[SupportedOSPlatform("windows")]
	public static bool IsBottom(StringFormat format) => format.LineAlignment == StringAlignment.Far;

	/// <summary>Tests whether a string format aligns content to the top edge.</summary>
	[SupportedOSPlatform("windows")]
	public static bool IsTop(StringFormat format) => format.LineAlignment == StringAlignment.Near;

	/// <summary>Tests whether a string format centers content vertically.</summary>
	[SupportedOSPlatform("windows")]
	public static bool IsMiddle(StringFormat format) => format.LineAlignment == StringAlignment.Center;

	/// <summary>Tests whether a string format centers content horizontally.</summary>
	[SupportedOSPlatform("windows")]
	public static bool IsCenter(StringFormat format) => format.Alignment == StringAlignment.Center;

	/// <summary>Tests whether a string format aligns content to the left edge.</summary>
	[SupportedOSPlatform("windows")]
	public static bool IsLeft(StringFormat format) => format.Alignment == StringAlignment.Near;

	/// <summary>Tests whether a string format aligns content to the right edge.</summary>
	[SupportedOSPlatform("windows")]
	public static bool IsRight(StringFormat format) => format.Alignment == StringAlignment.Far;

	/// <summary>
	/// Converts a string to a color
	/// </summary>
	/// <param name="colorText">The string to be converted</param>
	/// <returns>The converted Color</returns>
	public static Color ConvertStringToColor(string colorText) {
		if (string.IsNullOrWhiteSpace(colorText))
			return Color.Empty;

		var parts = colorText.Split(new char[] { ':' });
		switch (parts.Length) {
			case 3:
				return Color.FromArgb(Int32.Parse(parts[0]), Int32.Parse(parts[1]), Int32.Parse(parts[2]));
			case 4:
				return Color.FromArgb(Int32.Parse(parts[0]), Int32.Parse(parts[1]), Int32.Parse(parts[2]), Int32.Parse(parts[3]));
		}
		return Color.Empty;
	}

	/// <summary>
	/// Calculate a darker or lighter color using the source specified.
	/// A light of 1 is White, a light of -1 is black. All the other values are an interpolation from the source color.
	/// </summary>
	/// <param name="source"></param>
	/// <param name="light"></param>
	/// <returns></returns>
	public static Color CalculateLightDarkColor(Color source, float light) {
		if (light > 1 || light < -1)
			throw new ArgumentException("Must be between 1 and -1", nameof(light));

		// Keep this public entry point's argument contract while sharing the Framework shading calculation.
		return source.GetShade(light);
	}

	/// <summary>Chooses black or white text for the greater contrast against the supplied background.</summary>
	public static Color GetContrastingTextColor(Color background) {
		// Convert sRGB channels to relative luminance before comparing both text colors.
		static double Linearize(byte channel) {
			var value = channel / 255.0;
			return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
		}
		var luminance = 0.2126 * Linearize(background.R) + 0.7152 * Linearize(background.G) + 0.0722 * Linearize(background.B);
		return (luminance + 0.05) / 0.05 >= 1.05 / (luminance + 0.05) ? Color.Black : Color.White;
	}

	/// <summary>
	/// Calculate the middle color between the start and the end color.
	/// </summary>
	/// <param name="startColor"></param>
	/// <param name="endColor"></param>
	/// <returns></returns>
	public static Color CalculateMiddleColor(Color startColor, Color endColor) => CalculateColorGradient(startColor, endColor, 3)[1];

	/// <summary>
	/// Interpolate the specified number of times between start and end color
	/// </summary>
	/// <param name="startColor"></param>
	/// <param name="endColor"></param>
	/// <param name="numberOfGradients"></param>
	/// <returns></returns>
	public static Color[] CalculateColorGradient(Color startColor, Color endColor, int numberOfGradients) {
		if (numberOfGradients < 2)
			throw new ArgumentException("Invalid Number of gradients, must be 2 or more");
		var colors = new Color[numberOfGradients];
		colors[0] = startColor;
		colors[colors.Length - 1] = endColor;

		// The colors span numberOfGradients - 1 equal steps from the start color to the end color.
		var steps = (float)(numberOfGradients - 1);
		var incrementA = ((float)(endColor.A - startColor.A)) / steps;
		var incrementR = ((float)(endColor.R - startColor.R)) / steps;
		var incrementG = ((float)(endColor.G - startColor.G)) / steps;
		var incrementB = ((float)(endColor.B - startColor.B)) / steps;

		for (var index = 1; index < (colors.Length - 1); index++)
			colors[index] = Color.FromArgb((int)(startColor.A + incrementA * (float)index),
				(int)(startColor.R + incrementR * (float)index),
				(int)(startColor.G + incrementG * (float)index),
				(int)(startColor.B + incrementB * (float)index));

		return colors;
	}

	public static string ConvertColorToString(Color color) => color.ToARGBString();

	public static Color RandomColor(bool solid = true) => Color.FromArgb(solid ? 255 : Tools.Maths.RNG.Next(256), Tools.Maths.RNG.Next(256), Tools.Maths.RNG.Next(256));

	[SupportedOSPlatform("windows6.1")]
	public static bool IsRecognisedImageFile(string fileName) {
		var targetExtension = Path.GetExtension(fileName);
		if (String.IsNullOrEmpty(targetExtension))
			return false;
		targetExtension = "*" + targetExtension.ToLowerInvariant();

		var recognisedImageExtensions = new List<string>();

		foreach (var imageCodec in ImageCodecInfo.GetImageEncoders())
			recognisedImageExtensions.AddRange(imageCodec.FilenameExtension.ToLowerInvariant().Split(";".ToCharArray()));

		return recognisedImageExtensions.Any(extension => extension.Equals(targetExtension));
	}

	/// <summary>
	/// Save an Image as a JPeg with a given compression
	///  Note: Filename suffix will not affect mime type which will be Jpeg.
	/// </summary>
	/// <param name="image">Image to save</param>
	/// <param name="fileName">File name to save the image as. Note: suffix will not affect mime type which will be Jpeg.</param>
	/// <param name="compression">Value between 0 and 100.</param>
	[SupportedOSPlatform("windows")]
	public static void SaveJpegWithCompression(Image image, string fileName, int compression) {
		if (!(0 <= compression && compression <= 100))
			throw new ArgumentOutOfRangeException("compression", "Must be between 0 and 100");

		var encoderParameters = new EncoderParameters(1);
		encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, compression);
		var imageCodec = GetEncoderInfo("image/jpeg");
		image.Save(fileName, imageCodec, encoderParameters);
	}

	/// <summary>
	/// Save an Image as a JPeg with a given compression
	///  Note: Filename suffix will not affect mime type which will be Jpeg.
	/// </summary>
	/// <param name="image">Image to save</param>
	/// <param name="stream">The stream where the image will be saved.</param>
	/// <param name="compression">Value between 0 and 100.</param>
	[SupportedOSPlatform("windows")]
	public static void SaveJpegWithCompression(Image image, Stream stream, int compression) {
		if (!(0 <= compression && compression <= 100))
			throw new ArgumentOutOfRangeException("compression", "Must be between 0 and 100");

		var encoderParameters = new EncoderParameters(1);
		encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, compression);
		var imageCodec = GetEncoderInfo("image/jpeg");
		image.Save(stream, imageCodec, encoderParameters);
	}

	/// <summary>
	/// Retrieves the Encoder Information for a given MimeType
	/// </summary>
	/// <param name="mimeType">String: Mimetype</param>
	/// <returns>ImageCodecInfo: Mime info or null if not found</returns>
	[SupportedOSPlatform("windows")]
	public static ImageCodecInfo GetEncoderInfo(String mimeType) {
		var encoders = ImageCodecInfo.GetImageEncoders();
		return encoders.FirstOrDefault(t => t.MimeType == mimeType);
	}

	[SupportedOSPlatform("windows")]
	public static bool TryGetImageFormatFromHeader(string filePath, out ImageFormat imageFormat) {
		using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
			return TryGetImageFormatFromHeader(fileStream, out imageFormat);
		}
	}

	[SupportedOSPlatform("windows")]
	public static bool TryGetImageFormatFromHeader(Stream imageStream, out ImageFormat imageFormat) {
		const int mostBytesNeeded = 11; //For JPEG
		imageFormat = null;

		if (imageStream.Length < mostBytesNeeded)
			return false;

		var headerBytes = new byte[mostBytesNeeded];
		if (imageStream.ReadAtLeast(headerBytes, mostBytesNeeded, throwOnEndOfStream: false) != mostBytesNeeded)
			return false;

		//Sources:
		//http://stackoverflow.com/questions/9354747
		//http://en.wikipedia.org/wiki/Magic_number_%28programming%29#Magic_numbers_in_files
		//http://www.mikekunz.com/image_file_header.html

		//JPEG:
		if (headerBytes[0] == 0xFF && //FF D8
			headerBytes[1] == 0xD8 &&
			(
				(headerBytes[6] == 0x4A && //'JFIF'
					headerBytes[7] == 0x46 &&
					headerBytes[8] == 0x49 &&
					headerBytes[9] == 0x46)
				||
				(headerBytes[6] == 0x45 && //'EXIF'
					headerBytes[7] == 0x78 &&
					headerBytes[8] == 0x69 &&
					headerBytes[9] == 0x66)
			) &&
			headerBytes[10] == 00) {
			imageFormat = ImageFormat.Jpeg;
			return true;
		}
		//PNG
		if (headerBytes[0] == 0x89 && //89 50 4E 47 0D 0A 1A 0A
			headerBytes[1] == 0x50 &&
			headerBytes[2] == 0x4E &&
			headerBytes[3] == 0x47 &&
			headerBytes[4] == 0x0D &&
			headerBytes[5] == 0x0A &&
			headerBytes[6] == 0x1A &&
			headerBytes[7] == 0x0A) {
			imageFormat = ImageFormat.Png;
			return true;
		}
		//GIF
		if (headerBytes[0] == 0x47 && //'GIF'
			headerBytes[1] == 0x49 &&
			headerBytes[2] == 0x46) {
			imageFormat = ImageFormat.Gif;
			return true;
		}
		//BMP
		if (headerBytes[0] == 0x42 && //42 4D
			headerBytes[1] == 0x4D) {
			imageFormat = ImageFormat.Bmp;
			return true;
		}
		//TIFF
		if ((headerBytes[0] == 0x49 && //49 49 2A 00
				headerBytes[1] == 0x49 &&
				headerBytes[2] == 0x2A &&
				headerBytes[3] == 0x00)
			||
			(headerBytes[0] == 0x4D && //4D 4D 00 2A
				headerBytes[1] == 0x4D &&
				headerBytes[2] == 0x00 &&
				headerBytes[3] == 0x2A)) {
			imageFormat = ImageFormat.Tiff;
			return true;
		}

		return false;
	}

	[SupportedOSPlatform("windows")]
	public static bool TryGetImageFormatFromFilenameExtension(string filePath, out ImageFormat imageFormat) {
		var extension = Path.GetExtension(filePath);
		if (string.IsNullOrEmpty(extension))
			throw new ArgumentException(string.Format("Unable to determine file extension for fileName: {0}", filePath));

		switch (extension.ToLower()) {
			case @".bmp":
				imageFormat = ImageFormat.Bmp;
				return true;
			case @".gif":
				imageFormat = ImageFormat.Gif;
				return true;
			case @".ico":
				imageFormat = ImageFormat.Icon;
				return true;
			case @".jpg":
			case @".jpeg":
				imageFormat = ImageFormat.Jpeg;
				return true;
			case @".png":
				imageFormat = ImageFormat.Png;
				return true;
			case @".tif":
			case @".tiff":
				imageFormat = ImageFormat.Tiff;
				return true;
			case @".wmf":
				imageFormat = ImageFormat.Wmf;
				return true;
			default:
				imageFormat = null;
				return false;
		}
	}

	[SupportedOSPlatform("windows")]
	public static bool TryGetFileExtensionFromImageFormat(ImageFormat format, out string extension) {
		extension = null;
		var encoders = ImageCodecInfo.GetImageEncoders().ToArray();
		if (encoders.Any(x => x.FormatID == format.Guid)) {
			var extensions = encoders.First(x => x.FormatID == format.Guid).FilenameExtension.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			if (extensions.Any()) {
				extension = extensions.First().Trim('*').ToLower();
				return true;
			}
		}
		return false;
	}

	[SupportedOSPlatform("windows")]
	public static Icon Resizeicon(Icon icon, int squareDimension) => new Icon(icon, new Size(squareDimension, squareDimension));

	/// <summary>
	/// Create a disabled version of the image.
	/// </summary>
	/// <param name="image">The image to convert</param>
	/// <param name="background">The Color of the background behind the image. It is accepted for parity with ControlPaint.DrawImageDisabled and does not change the result, which is always a lightened grayscale copy.</param>
	/// <returns></returns>
	[SupportedOSPlatform("windows")]
	public static Image CreateDisabledImage(Image image, Color background) {
		if (image == null)
			return null;

		// GDI+ objects are not thread-safe, so each call uses its own image attributes.
		var imgSize = image.Size;
		var arrayJagged = new float[5][];
		arrayJagged[0] = new float[5] { 0.2125f, 0.2125f, 0.2125f, 0f, 0f };
		arrayJagged[1] = new float[5] { 0.2577f, 0.2577f, 0.2577f, 0f, 0f };
		arrayJagged[2] = new float[5] { 0.0361f, 0.0361f, 0.0361f, 0f, 0f };
		var arraySingle = new float[5];
		arraySingle[3] = 1f;
		arrayJagged[3] = arraySingle;
		arrayJagged[4] = new float[5] { 0.38f, 0.38f, 0.38f, 0f, 1f };
		var matrix = new System.Drawing.Imaging.ColorMatrix(arrayJagged);

		var bitmap = new Bitmap(image.Width, image.Height);
		using (var disabledImageAttributes = new System.Drawing.Imaging.ImageAttributes())
		using (var graphics = Graphics.FromImage(bitmap)) {
			disabledImageAttributes.ClearColorKey();
			disabledImageAttributes.SetColorMatrix(matrix);
			graphics.DrawImage(image, new Rectangle(0, 0, imgSize.Width, imgSize.Height), 0, 0, imgSize.Width, imgSize.Height, GraphicsUnit.Pixel, disabledImageAttributes);
		}

		return bitmap;
	}
}

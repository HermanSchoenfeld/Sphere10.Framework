// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;
using Sphere10.Framework.Drawing;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;
using Sphere10.Framework.Windows.Forms.SourceGrid.Exporter;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Checks native grid HTML rendering, image destinations and borrowed resources through its public exporter.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class SourceGridHtmlExporterTests {

	/// <summary>Image destinations use the requested directory without an unused temporary placeholder.</summary>
	[TestCase("")]
	[TestCase("images")]
	[TestCase("images/")]
	[TestCase("https://example.test/assets/")]
	public void EmbeddedImagesUseTheRequestedDirectoryAndHtmlPath(string relativePath) {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		try {
			using var image = new Bitmap(3, 2);
			using var stream = new MemoryStream();
			var exporter = new HTML(ExportHTMLMode.Default, directory, relativePath, stream);

			var htmlPath = exporter.ExportImage(image);

			var exportedFile = exporter.EmbeddedImagesPath.Single();
			Assert.That(Path.GetDirectoryName(exportedFile), Is.EqualTo(directory));
			Assert.That(Path.GetExtension(exportedFile), Is.EqualTo(".jpg"));
			Assert.That(Directory.GetFiles(directory), Is.EquivalentTo(new[] { exportedFile }), "Export only the JPEG, without reserving a separate empty .tmp file.");
			var expectedPath = relativePath.Length == 0 ? Path.GetFileName(exportedFile) : relativePath.TrimEnd('/') + "/" + Path.GetFileName(exportedFile);
			Assert.That(htmlPath, Is.EqualTo(expectedPath));

			using var restored = System.Drawing.Image.FromFile(exportedFile);
			Assert.That(restored.RawFormat.Guid, Is.EqualTo(ImageFormat.Jpeg.Guid));
			Assert.That(restored.Size, Is.EqualTo(image.Size));
		} finally {
			Directory.Delete(directory, true);
		}
	}

	/// <summary>Image reuse follows object identity, while the caller retains images and output stream ownership.</summary>
	[Test]
	public void RepeatedImagesShareOneHtmlPathAndClearingTheCacheKeepsBorrowedResourcesAlive() {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		try {
			using var image = new Bitmap(3, 2);
			using var equalPixels = new Bitmap(image);
			using var grid = new Grid();
			grid.Redim(1, 3);
			grid[0, 0] = new Cell("First") { Image = image };
			grid[0, 1] = new Cell("Repeated") { Image = image };
			grid[0, 2] = new Cell("Separate") { Image = equalPixels };
			using var stream = new MemoryStream();
			var exporter = new HTML(ExportHTMLMode.Default, directory, "images", stream);

			exporter.Export(grid);

			var document = ReadDocument(stream);
			var imagePaths = document.Descendants("img").Select(element => (string)element.Attribute("src")).ToArray();
			Assert.That(imagePaths, Has.Length.EqualTo(3));
			Assert.That(imagePaths[1], Is.EqualTo(imagePaths[0]));
			Assert.That(imagePaths[2], Is.Not.EqualTo(imagePaths[0]));
			var exportedFiles = exporter.EmbeddedImagesPath;
			Assert.That(exportedFiles, Has.Length.EqualTo(2));
			Assert.That(Directory.GetFiles(directory), Is.EquivalentTo(exportedFiles));

			exporter.ClearEmbeddedImages();

			Assert.That(exporter.EmbeddedImagesPath, Is.Empty);
			Assert.That(exportedFiles.All(File.Exists), Is.True);
			Assert.That(image.Size, Is.EqualTo(new Size(3, 2)));
			Assert.That(equalPixels.Size, Is.EqualTo(image.Size));
			Assert.That(stream.CanWrite, Is.True);
			stream.WriteByte(0);
		} finally {
			Directory.Delete(directory, true);
		}
	}

	/// <summary>Spans, styled text and empty cells remain siblings in both full-document and table-only output.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public void ExportRetainsSpansStylesEscapingAndEmptyCells(bool includeDocument) {
		using var font = new Font(SystemFonts.DefaultFont, FontStyle.Bold | FontStyle.Italic | FontStyle.Underline);
		using var grid = new Grid();
		grid.Redim(3, 3);
		grid[0, 0] = new Cell("Merged <&> text") {
			ColumnSpan = 2,
			RowSpan = 2,
			View = new SourceGrid.Cells.Views.Cell {
				BackColor = Color.CornflowerBlue,
				ForeColor = Color.DarkRed,
				TextAlignment = ContentAlignment.BottomRight,
				Border = new RectangleBorder(new BorderLine(Color.Black, 2)),
				Font = font
			}
		};
		grid[0, 2] = new Cell("Right");
		grid[1, 2] = new Cell("Second row");
		grid[2, 1] = new Cell("After empty");
		grid[2, 2] = new Cell("Last");
		using var stream = new MemoryStream();
		var mode = includeDocument ? ExportHTMLMode.Default : ExportHTMLMode.Default & ~ExportHTMLMode.HTMLAndBody;
		var exporter = new HTML(mode, string.Empty, string.Empty, stream);

		exporter.Export(grid);

		var document = ReadDocument(stream);
		Assert.That(document.Root.Name.LocalName, Is.EqualTo(includeDocument ? "html" : "table"));
		var table = includeDocument ? document.Root.Element("body").Element("table") : document.Root;
		var rows = table.Elements("tr").ToArray();
		Assert.That(rows, Has.Length.EqualTo(3));
		Assert.That(rows.Select(row => row.Elements("td").Count()).ToArray(), Is.EqualTo(new[] { 2, 1, 3 }));
		var merged = rows[0].Elements("td").First();
		Assert.That((string)merged.Attribute("colspan"), Is.EqualTo("2"));
		Assert.That((string)merged.Attribute("rowspan"), Is.EqualTo("2"));
		Assert.That((string)merged.Attribute("bgcolor"), Is.EqualTo("#6495ED"));
		Assert.That((string)merged.Attribute("style"), Does.Contain("border-top:2px solid #000000"));
		Assert.That((string)merged.Attribute("align"), Is.EqualTo("right"));
		Assert.That((string)merged.Attribute("valign"), Is.EqualTo("bottom"));
		Assert.That((string)merged.Element("font").Attribute("color"), Is.EqualTo("#8B0000"));
		Assert.That(merged.Element("font").Element("b").Element("u").Element("i").Value, Is.EqualTo("Merged <&> text"));
		Assert.That(rows[2].Elements("td").Select(cell => cell.Value).ToArray(), Is.EqualTo(new[] { "\u00A0", "After empty", "Last" }));
		Assert.That(font.Size, Is.GreaterThan(0));
		Assert.That(stream.CanWrite, Is.True);
	}

	/// <summary>Derived cell rendering remains in control, and successful export appends at the supplied cursor.</summary>
	[Test]
	public void ExportUsesTheCellContentHookWithoutRewindingOrClosingTheStream() {
		using var grid = new Grid();
		grid.Redim(1, 2);
		grid[0, 0] = new Cell("First");
		grid[0, 1] = new Cell("Second");
		using var stream = new MemoryStream();
		stream.Write(Encoding.UTF8.GetBytes("prefix:"));
		var exporter = new CellContentExporter(stream);

		exporter.Export(grid);

		Assert.That(exporter.ExportedCellCount, Is.EqualTo(2));
		Assert.That(Encoding.UTF8.GetString(stream.ToArray()), Does.StartWith("prefix:<html>").And.Contain("custom:First").And.Contain("custom:Second"));
		Assert.That(stream.CanWrite, Is.True);
		stream.WriteByte(0);
	}

	/// <summary>A failed custom cell renderer propagates its error while leaving the borrowed stream usable.</summary>
	[Test]
	public void FailedCellContentHookDoesNotCloseTheBorrowedStream() {
		using var grid = new Grid();
		grid.Redim(1, 1);
		grid[0, 0] = new Cell("First");
		using var stream = new MemoryStream();
		var exporter = new CellContentExporter(stream, true);

		Assert.That(() => exporter.Export(grid), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Cell content failed."));

		Assert.That(exporter.ExportedCellCount, Is.EqualTo(1));
		Assert.That(stream.CanWrite, Is.True);
		stream.WriteByte(0);
	}

	// Normalize HTML's named nonbreaking-space entity so the XML parser can verify table structure.
	private static XDocument ReadDocument(MemoryStream stream) {
		var html = Encoding.UTF8.GetString(stream.ToArray()).TrimStart('\uFEFF');
		return XDocument.Parse(html.Replace("&nbsp;", "\u00A0", StringComparison.Ordinal));
	}

	#region Inner Types

	/// <summary>Exercises the existing protected renderer without substituting the grid or output writer.</summary>
	private sealed class CellContentExporter : HTML {
		private readonly bool _fail;

		/// <summary>Retains the caller's stream and optionally rejects the first rendered cell.</summary>
		public CellContentExporter(Stream stream, bool fail = false)
			: base(ExportHTMLMode.Default, string.Empty, string.Empty, stream) {
			_fail = fail;
		}

		/// <summary>The number of nonempty cell contexts delivered to the custom content hook.</summary>
		public int ExportedCellCount { get; private set; }

		/// <summary>Writes recognizable cell text or propagates the caller's rendering failure.</summary>
		protected override void ExportHTMLCellContent(CellContext context, XmlTextWriter writer) {
			ExportedCellCount++;
			if (_fail)
				throw new InvalidOperationException("Cell content failed.");

			writer.WriteString("custom:" + context.DisplayText);
		}
	}

	#endregion
}

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;
using Sphere10.Framework.Windows.Forms.SourceGrid.Exporter;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Checks print and editor owners independently from borrowed grid data and presentation resources.</summary>
[TestFixture]
[Apartment(ApartmentState.STA)]
public class SourceGridResourceOwnershipTests {

	[Test]
	public void PrintDocumentDisposesItsDefaultsAndPreservesBorrowedFontReplacements() {
		using var grid = new Grid();
		using var borrowedFont = new Font(grid.Font, FontStyle.Italic);
		using var document = new GridPrintDocument(grid);
		var defaults = new[] { document.PageHeaderFont, document.PageTitleFont, document.PageFooterFont };
		document.PageHeaderFont = borrowedFont;
		document.PageTitleFont = borrowedFont;
		document.PageFooterFont = borrowedFont;

		document.Dispose();

		foreach (var font in defaults)
			Assert.That(() => CloneAndDispose(font), Throws.TypeOf<ArgumentException>());
		using var borrowedClone = (Font)borrowedFont.Clone();
		Assert.That(borrowedClone.Style, Is.EqualTo(FontStyle.Italic));
		Assert.That(grid.IsDisposed, Is.False);
	}

	[Test]
	public void CellsBorrowSharedEditorsAndTheEditorOwnsItsEagerNativeControl() {
		using var grid = new Grid();
		using var editor = new SourceGrid.Cells.Editors.TextBox(typeof(string));
		var control = editor.Control;
		grid.Redim(1, 2);
		grid[0, 0] = new Cell("First", editor);
		grid[0, 1] = new Cell("Second", editor);

		grid.Redim(0, 0);

		Assert.That(control.IsDisposed, Is.False, "Removing cells must not dispose their shared editor.");
		editor.Dispose();
		Assert.That(control.IsDisposed, Is.True);
		Assert.That(grid.IsDisposed, Is.False);
	}

	[Test]
	public void ListEditorReleasesItsFactoriesAndBorrowsExplicitEditorReplacements() {
		using var listEditor = new ListEditor { ItemType = typeof(SampleRow) };
		var original = (SourceGrid.Cells.Editors.EditorControlBase)listEditor.Editors[0];
		listEditor.ItemType = typeof(SampleRow);

		Assert.That(original.Control.IsDisposed, Is.True);
		var replacement = (SourceGrid.Cells.Editors.EditorControlBase)listEditor.Editors[0];
		Assert.That(replacement.Control.IsDisposed, Is.False);
		using var borrowed = new SourceGrid.Cells.Editors.TextBox(typeof(string));
		listEditor.Editors = new SourceGrid.Cells.Editors.EditorBase[] { borrowed };

		Assert.That(replacement.Control.IsDisposed, Is.True);
		listEditor.Dispose();
		Assert.That(borrowed.Control.IsDisposed, Is.False);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void HtmlExportsImagesOnlyWhenTheModeIncludesThem(bool includeImages) {
		using var grid = new Grid();
		using var image = new Bitmap(2, 2);
		grid.Redim(1, 1);
		grid[0, 0] = new Cell("Icon") { Image = image };
		using var stream = new MemoryStream();
		var mode = includeImages ? ExportHTMLMode.Default : ExportHTMLMode.Default & ~ExportHTMLMode.CellImages;
		var exporter = new RecordingHtmlExporter(mode, stream);

		exporter.Export(grid);

		var html = Encoding.UTF8.GetString(stream.ToArray());
		Assert.That(exporter.ImagesExported, Is.EqualTo(includeImages ? 1 : 0));
		Assert.That(html.Contains("<img", StringComparison.Ordinal), Is.EqualTo(includeImages));
		Assert.That(html, Does.Contain("Icon"));
	}

	// Dispose any successful clone as well, so a failed ownership assertion cannot leak a test font.
	private static void CloneAndDispose(Font font) {
		using var clone = (Font)font.Clone();
	}

	private sealed class SampleRow {

		public SampleRow() {
		}

		public string Name { get; set; }
	}

	private sealed class RecordingHtmlExporter : HTML {

		public RecordingHtmlExporter(ExportHTMLMode mode, Stream stream)
			: base(mode, string.Empty, string.Empty, stream) {
		}

		public int ImagesExported { get; private set; }

		public override string ExportImage(System.Drawing.Image image) {
			ImagesExported++;
			return "sample.png";
		}
	}
}

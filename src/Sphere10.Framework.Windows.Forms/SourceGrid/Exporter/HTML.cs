// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using Tools;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Exporter;

/// <summary>
/// An utility class to export the grid in a html format file.
/// </summary>
public class HTML {
	protected ExportHTMLMode ExportMode = ExportHTMLMode.Default;
	protected System.IO.Stream OutputStream;
	protected string ImageFullPath;
	protected string ImageRelativePath;

	/// <summary>
	/// Key:Image, Value:ImageFileName
	/// </summary>
	protected Dictionary<System.Drawing.Image, string> EmbeddedImagePaths = new Dictionary<System.Drawing.Image, string>();

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="mode"></param>
	/// <param name="imageFullPath">The path to write embedded images files</param>
	/// <param name="imageRelativePath">The path used in the HTML source. If you save the images in the same path of the HTML file you can leave this path empty.</param>
	/// <param name="htmlStream">The stream to write</param>
	public HTML(ExportHTMLMode mode, string imageFullPath, string imageRelativePath, System.IO.Stream htmlStream) {
		ExportMode = mode;
		OutputStream = htmlStream;
		ImageFullPath = imageFullPath;
		ImageRelativePath = imageRelativePath;
	}

	/// <summary>
	/// Export mode
	/// </summary>
	public virtual ExportHTMLMode Mode {
		get => ExportMode;
		set => ExportMode = value;
	}

	/// <summary>
	/// List of images exported during HTML export
	/// </summary>
	public virtual string[] EmbeddedImagesPath {
		get {
			var images = new string[EmbeddedImagePaths.Count];
			EmbeddedImagePaths.Values.CopyTo(images, 0);
			return images;
		}
	}

	public virtual System.IO.Stream Stream => OutputStream;

	/// <summary>
	/// Clear the list of embedded images. This method don't delete the files only clear the list.
	/// </summary>
	public virtual void ClearEmbeddedImages() => EmbeddedImagePaths.Clear();

	/// <summary>Exports grid presentation at the stream's current position without closing the borrowed stream.</summary>
	public void Export(GridVirtual grid) {
		var writer = new System.Xml.XmlTextWriter(Stream, System.Text.Encoding.UTF8);

		//write HTML and BODY
		if ((Mode & ExportHTMLMode.HTMLAndBody) == ExportHTMLMode.HTMLAndBody) {
			writer.WriteStartElement("html");
			writer.WriteStartElement("body");
		}

		writer.WriteStartElement("table");

		writer.WriteAttributeString("cellspacing", "0");
		writer.WriteAttributeString("cellpadding", "0");

		for (var r = 0; r < grid.Rows.Count; r++) {
			writer.WriteStartElement("tr");

			for (var c = 0; c < grid.Columns.Count; c++) {
				var cell = grid.GetCell(r, c);
				var position = new Position(r, c);
				var context = new CellContext(grid, position, cell);
				ExportHTMLCell(context, writer);
			}

			//tr
			writer.WriteEndElement();
		}

		//table
		writer.WriteEndElement();

		//write end HTML and BODY
		if ((Mode & ExportHTMLMode.HTMLAndBody) == ExportHTMLMode.HTMLAndBody) {
			//body
			writer.WriteEndElement();
			//html
			writer.WriteEndElement();
		}

		writer.Flush();
	}

	/// <summary>
	/// Save the Image to file and returns the file
	/// </summary>
	/// <param name="image"></param>
	/// <returns>Returns the path where the image is exported valid for the HTML page</returns>
	public virtual string ExportImage(System.Drawing.Image image) {
		if (!EmbeddedImagePaths.TryGetValue(image, out var fileName)) {
			// Allocate the image name in its requested directory without creating an unused temporary file.
			fileName = System.IO.Path.ChangeExtension(Tools.FileSystem.GetTempFileName(ImageFullPath, create: false), ".jpg");
			image.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);

			EmbeddedImagePaths.Add(image, fileName);
		}

		//change to use relative path
		var relativeFileName = System.IO.Path.GetFileName(fileName);
		return string.IsNullOrEmpty(ImageRelativePath) ? relativeFileName : Tools.Url.Combine(ImageRelativePath, relativeFileName);
	}

	public static string BorderToHTMLStyle(Sphere10.Framework.Drawing.BorderLine border) {
		if (border.Width > 0)
			return border.Width.ToString() + "px solid " + ColorToHTML(border.Color);
		return "none";
	}

	/// <summary>
	/// Convert a Color to HTML compatible string
	/// </summary>
	/// <param name="color"></param>
	/// <returns></returns>
	public static string ColorToHTML(System.Drawing.Color color) {
		//non uso direttamente ToHtml su p_Color petchè se questo contiene dei valori tipo (Window, Control, cioè valori definiti in base al sistema) su Mozilla non funzionano
		return System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B));
	}

	public static string BorderToHTMLStyle(Sphere10.Framework.Drawing.IBorder border) {
		if (border is Sphere10.Framework.Drawing.RectangleBorder) {
			var brd = (Sphere10.Framework.Drawing.RectangleBorder)border;

			return "border-top:" + BorderToHTMLStyle(brd.Top) +
					";border-right:" + BorderToHTMLStyle(brd.Right) +
					";border-bottom:" + BorderToHTMLStyle(brd.Bottom) +
					";border-left:" + BorderToHTMLStyle(brd.Left) + ";";
		}
		return string.Empty;
	}

	/// <summary>
	/// Export a font html element with the specified font and text
	/// </summary>
	/// <param name="writer"></param>
	/// <param name="displayText"></param>
	/// <param name="font"></param>
	public static void ExportHTMLElementFont(System.Xml.XmlTextWriter writer, string displayText, System.Drawing.Font font) {
		if (font != null) {
			writer.WriteAttributeString("style", "font-size:" + font.SizeInPoints + "pt");

			if (font.Bold)
				writer.WriteStartElement("b");

			if (font.Underline)
				writer.WriteStartElement("u");

			if (font.Italic)
				writer.WriteStartElement("i");
		}

		//displaytext
		if (displayText == null || displayText.Trim().Length <= 0)
			writer.WriteRaw("&nbsp;");
		else            //l_Display = l_Display.Replace("\r\n","<br>");
			writer.WriteString(displayText);

		if (font != null) {
			//i
			if (font.Italic)
				writer.WriteEndElement();

			//u
			if (font.Underline)
				writer.WriteEndElement();

			//b
			if (font.Bold)
				writer.WriteEndElement();
		}
	}

	/// <summary>
	/// Export the specified cell to HTML
	/// </summary>
	/// <param name="context"></param>
	/// <param name="writer"></param>
	protected virtual void ExportHTMLCell(CellContext context, System.Xml.XmlTextWriter writer) {
		if (context.Cell == null) {
			//start element td
			writer.WriteStartElement("td");
			writer.WriteRaw("&nbsp;");
			writer.WriteEndElement();
		} else {

			#region TD style

			var rangeCell = context.Grid.PositionToCellRange(context.Position);
			if (rangeCell.Start != context.Position)
				return;

			//start element td
			writer.WriteStartElement("td");

			//check for rowspan and colspan
			if (rangeCell.ColumnsCount > 1 || rangeCell.RowsCount > 1) {
				//colspan, rowspan
				writer.WriteAttributeString("colspan", rangeCell.ColumnsCount.ToString());
				writer.WriteAttributeString("rowspan", rangeCell.RowsCount.ToString());
			}

			if (context.Cell.View is Cells.Views.Cell) {
				var viewCell = (Cells.Views.Cell)context.Cell.View;

				//write backcolor
				if ((Mode & ExportHTMLMode.CellBackColor) == ExportHTMLMode.CellBackColor)
					writer.WriteAttributeString("bgcolor", ColorToHTML(context.Cell.View.BackColor));

				var style = "";

				//border
				style = BorderToHTMLStyle(viewCell.Border);

				//style
				writer.WriteAttributeString("style", style);

				//alignment
				writer.WriteAttributeString("align", Tools.WinForms.ContentToHorizontalAlignment(viewCell.TextAlignment).ToString().ToLower());
				if (Tools.Drawing.IsBottom(viewCell.TextAlignment))
					writer.WriteAttributeString("valign", "bottom");
				else if (Tools.Drawing.IsTop(viewCell.TextAlignment))
					writer.WriteAttributeString("valign", "top");
				else if (Tools.Drawing.IsMiddle(viewCell.TextAlignment))
					writer.WriteAttributeString("valign", "middle");
			}

			#endregion

			if (context.Cell.View is Cells.Views.CheckBox) {
				var checkModel = (Cells.Models.ICheckBox)context.Cell.Model.FindModel(typeof(Cells.Models.ICheckBox));
				var status = checkModel.GetCheckBoxStatus(context);
				if (status.Checked == true)
					writer.WriteRaw("<input type=\"checkbox\" checked>");
				else
					writer.WriteRaw("<input type=\"checkbox\">");
			}

			#region Font

			if (context.Cell.View is Cells.Views.Cell) {
				//Read the image
				System.Drawing.Image img = null;
				var imgModel = (Cells.Models.IImage)context.Cell.Model.FindModel(typeof(Cells.Models.IImage));
				if (imgModel != null)
					img = imgModel.GetImage(context);

				var viewCell = (Cells.Views.Cell)context.Cell.View;

				if (img != null && Mode.HasFlag(ExportHTMLMode.CellImages)) {
					writer.WriteStartElement("img");

					writer.WriteAttributeString("align", Tools.WinForms.ContentToHorizontalAlignment(viewCell.ImageAlignment).ToString().ToLower());
					writer.WriteAttributeString("src", ExportImage(img));

					//img
					writer.WriteEndElement();
				}

				writer.WriteStartElement("font");

				writer.WriteAttributeString("color", ColorToHTML(viewCell.ForeColor));

				ExportHTMLCellContent(context, writer);

				//font
				writer.WriteEndElement();
			} else
				ExportHTMLCellContent(context, writer);

			//td
			writer.WriteEndElement();

			#endregion
		}
	}

	protected virtual void ExportHTMLCellContent(CellContext context, System.Xml.XmlTextWriter writer) {
		if (context.Cell.View is Cells.Views.CheckBox) {
			var checkModel = (Cells.Models.ICheckBox)context.Cell.Model.FindModel(typeof(Cells.Models.ICheckBox));
			var status = checkModel.GetCheckBoxStatus(context);
			ExportHTMLElementFont(writer, status.Caption, context.Cell.View.GetDrawingFont(context.Grid));
		} else
			ExportHTMLElementFont(writer, context.DisplayText, context.Cell.View.GetDrawingFont(context.Grid));
	}
}

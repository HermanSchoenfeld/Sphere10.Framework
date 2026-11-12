// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors;

/// <summary>
///  A model that use a TextBoxButton for Image editing, allowing to select a source image file. Returns null as DisplayString. Write and read byte[] values.
/// </summary>
[System.ComponentModel.ToolboxItem(false)]
public class ImagePicker : EditorControlBase {
	public readonly static ImagePicker Default = new ImagePicker();

	#region Constructor

	/// <summary>
	/// Construct an Editor of type ImagePicker.
	/// </summary>
	public ImagePicker()
		: base(typeof(byte[])) {
	}

	#endregion

	/// <summary>
	/// Gets the control used for editing the cell.
	/// </summary>
	public new Sphere10.Framework.Windows.Forms.TextBoxUITypeEditor Control {
		get => (Sphere10.Framework.Windows.Forms.TextBoxUITypeEditor)base.Control;
	}

	public override object GetEditedValue() {
		var val = Control.Value;
		if (val == null)
			return null;
		if (val is System.Drawing.Image) {
			var imageValidator = new Sphere10.Framework.ComponentModel.TypeConverterValidator(typeof(System.Drawing.Image));
			return imageValidator.ValueToObject(val, typeof(byte[]));
			//Stranamente questo codice in caso di ico va in eccezione!
			//				System.Drawing.Image img = (System.Drawing.Image)val;
			//				using (System.IO.MemoryStream memStream = new System.IO.MemoryStream())
			//				{
			//					img.Save(memStream, System.Drawing.Imaging.ImageCodecInfo.);
			//
			//					return memStream.ToArray();
			//				}
		}
		if (val is byte[])
			return val;
		throw new SourceGridException("Invalid edited value, expected byte[] or Image");
	}

	public override void SetEditValue(object editValue) {
		Control.Value = editValue;
		Control.TextBox.SelectAll();
	}

	/// <summary>
	/// Used to returns the display string for a given value. In this case return null.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public override string ValueToDisplayString(object value) => null;

	#region Edit Control

	/// <summary>
	/// Create the editor control
	/// </summary>
	/// <returns></returns>
	protected override Control CreateControl() {
		var editor = new Sphere10.Framework.Windows.Forms.TextBoxUITypeEditor();
		editor.BorderStyle = Sphere10.Framework.Windows.Forms.Drawing.BorderStyle.None;
		editor.Validator = new Sphere10.Framework.ComponentModel.TypeConverterValidator(typeof(System.Drawing.Image));
		return editor;
	}

	protected override void OnSendCharToEditor(char key) {
		//No implementation
	}

	#endregion
}

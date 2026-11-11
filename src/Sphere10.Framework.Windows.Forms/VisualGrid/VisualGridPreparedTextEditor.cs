// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A nonvisual, read-only converter whose prepared value never needs formatting during layout or painting.</summary>
internal sealed class VisualGridPreparedTextEditor : EditorBase {
	private object _value;
	private string _displayText;
	private bool _hasPreparedValue;
	private VisualGridPreparedTextEditor _converter;

	/// <summary>Creates a nonvisual converter for the bound value type.</summary>
	public VisualGridPreparedTextEditor(Type type)
		: base(type) {
		EnableEdit = false;
		EditableMode = EditableMode.None;
	}

	/// <summary>Formats a worker value using the ordinary cell conversion failure presentation.</summary>
	public string PrepareValue(object value) {
		string text = null;
		Tools.Exceptions.ExecuteCapturingException(() => text = base.ValueToDisplayString(value), out var error);
		// Match CellContext.DisplayText's normal presentation of a failed conversion.
		return error == null ? text : "Error:" + error.Message;
	}

	/// <summary>Copies prepared converter metadata for one native cell's retained value.</summary>
	public VisualGridPreparedTextEditor CopyForValue(object value, string displayText) {
		// Clone the worker-initialized converter metadata, without running constructors or type factories on the UI thread.
		var editor = (VisualGridPreparedTextEditor)MemberwiseClone();
		editor.ReplacePreparedValue(value, displayText, this);
		return editor;
	}

	/// <summary>Replaces cached cell text while retaining the worker's converter metadata.</summary>
	public void ReplacePreparedValue(object value, string displayText, VisualGridPreparedTextEditor converter) {
		_value = value;
		_displayText = displayText;
		_hasPreparedValue = true;
		_converter = converter;
	}

	/// <summary>Returns cached text for the prepared value and delegates other conversions.</summary>
	public override string ValueToDisplayString(object value)
		=> _hasPreparedValue && ReferenceEquals(value, _value) ? _displayText : _converter != null ? _converter.ValueToDisplayString(value) : base.ValueToDisplayString(value);
}

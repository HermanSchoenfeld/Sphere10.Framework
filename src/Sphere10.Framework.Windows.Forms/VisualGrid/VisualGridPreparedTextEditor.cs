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
internal sealed class CrudGridPreparedTextEditor : EditorBase {
	private object _value;
	private string _displayText;
	private bool _hasPreparedValue;
	private CrudGridPreparedTextEditor _converter;

	public CrudGridPreparedTextEditor(Type type)
		: base(type) {
		EnableEdit = false;
		EditableMode = EditableMode.None;
	}

	public string PrepareValue(object value) {
		string text = null;
		Tools.Exceptions.ExecuteCapturingException(() => text = base.ValueToDisplayString(value), out var error);
		// Match CellContext.DisplayText's normal presentation of a failed conversion.
		return error == null ? text : "Error:" + error.Message;
	}

	public CrudGridPreparedTextEditor CopyForValue(object value, string displayText) {
		// Clone the worker-initialized converter metadata, without running constructors or type factories on the UI thread.
		var editor = (CrudGridPreparedTextEditor)MemberwiseClone();
		editor.ReplacePreparedValue(value, displayText, this);
		return editor;
	}

	public void ReplacePreparedValue(object value, string displayText, CrudGridPreparedTextEditor converter) {
		_value = value;
		_displayText = displayText;
		_hasPreparedValue = true;
		_converter = converter;
	}

	public override string ValueToDisplayString(object value)
		=> _hasPreparedValue && ReferenceEquals(value, _value) ? _displayText : _converter != null ? _converter.ValueToDisplayString(value) : base.ValueToDisplayString(value);
}

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Worker-prepared data, independent of a native cell's model, controllers and editor lifetime.</summary>
internal readonly struct VisualGridPreparedValue {
	private readonly VisualGridPreparedRowStyle _rowStyle;
	/// <summary>Captures a detached cell value and its worker-prepared display state.</summary>
	public VisualGridPreparedValue(bool hasValue, object value, string displayText, VisualGridPreparedTextEditor textEditor, VisualGridPreparedRowStyle rowStyle = null) {
		HasValue = hasValue;
		Value = value;
		DisplayText = displayText;
		TextEditor = textEditor;
		_rowStyle = rowStyle;
	}

	/// <summary>Whether the source supplied a value for this cell.</summary>
	public bool HasValue {
		get;
	}

	/// <summary>The exact source value retained for native publication.</summary>
	public object Value {
		get;
	}

	/// <summary>The prepared text shown without reformatting during native layout.</summary>
	public string DisplayText {
		get;
	}

	/// <summary>The prepared converter, or null for an image-only value.</summary>
	public VisualGridPreparedTextEditor TextEditor {
		get;
	}

	public VisualGridRowStyle RowStyle => _rowStyle?.Value ?? default;
}

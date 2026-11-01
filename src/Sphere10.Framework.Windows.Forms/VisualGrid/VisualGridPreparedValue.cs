// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Worker-prepared data, independent of a native cell's model, controllers and editor lifetime.</summary>
internal readonly struct CrudGridPreparedValue {
	public CrudGridPreparedValue(bool hasValue, object value, string displayText, CrudGridPreparedTextEditor textEditor) {
		HasValue = hasValue;
		Value = value;
		DisplayText = displayText;
		TextEditor = textEditor;
	}

	public bool HasValue { get; }

	public object Value { get; }

	public string DisplayText { get; }

	public CrudGridPreparedTextEditor TextEditor { get; }
}

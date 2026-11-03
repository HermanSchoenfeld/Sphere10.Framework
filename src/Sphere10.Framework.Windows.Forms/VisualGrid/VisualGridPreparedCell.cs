// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A native cell slot whose value, image model and cached text editor survive compatible snapshot updates.</summary>
internal sealed class VisualGridPreparedCell : Cell {
	private readonly VisualGridPreparedValueModel _valueModel = new();
	private readonly SourceGrid.Cells.Models.ModelContainer _modelContainer;
	private readonly SourceGrid.Cells.Models.Image _imageModel;
	private VisualGridPreparedTextEditor _textEditor;

	/// <summary>Creates a native slot backed by retained snapshot value and image models.</summary>
	public VisualGridPreparedCell(VisualGridPreparedValue prepared) {
		_modelContainer = Model;
		Model.ValueModel = _valueModel;
		_imageModel = (SourceGrid.Cells.Models.Image)Model.FindModel(typeof(SourceGrid.Cells.Models.Image));
		ApplyPreparedValue(prepared);
	}

	/// <summary>Whether callers have retained the models required for an in-place snapshot update.</summary>
	public bool CanApplyPreparedValue {
		get {
			return ReferenceEquals(Model, _modelContainer) && ReferenceEquals(Model.ValueModel, _valueModel)
					&& ReferenceEquals(Model.FindModel(typeof(SourceGrid.Cells.Models.Image)), _imageModel);
		}
	}

	/// <summary>Publishes a prepared value without entering SourceGrid's cell-edit lifecycle.</summary>
	public void ApplyPreparedValue(VisualGridPreparedValue prepared) {
		// Snapshot publication is not an edit: replace exact raw values without notifying model/controller callbacks.
		_valueModel.ReplaceValue(prepared.HasValue && prepared.TextEditor != null ? prepared.Value : string.Empty);
		_imageModel.ImageValue = prepared.HasValue && prepared.TextEditor == null ? prepared.Value as System.Drawing.Image : null;
		if (prepared.HasValue && prepared.TextEditor != null) {
			if (_textEditor == null || _textEditor.ValueType != prepared.TextEditor.ValueType)
				_textEditor = prepared.TextEditor.CopyForValue(prepared.Value, prepared.DisplayText);
			else
				_textEditor.ReplacePreparedValue(prepared.Value, prepared.DisplayText, prepared.TextEditor);
			Editor = _textEditor;
		} else {
			_textEditor?.ReplacePreparedValue(null, string.Empty, prepared.TextEditor);
			Editor = null;
		}
	}
}

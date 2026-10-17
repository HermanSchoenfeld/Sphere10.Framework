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
internal sealed class CrudGridPreparedCell : Cell {
	private readonly CrudGridPreparedValueModel _valueModel = new();
	private readonly SourceGrid.Cells.Models.ModelContainer _modelContainer;
	private readonly SourceGrid.Cells.Models.Image _imageModel;
	private CrudGridPreparedTextEditor _textEditor;

	public CrudGridPreparedCell(CrudGridPreparedValue prepared) {
		_modelContainer = Model;
		Model.ValueModel = _valueModel;
		_imageModel = (SourceGrid.Cells.Models.Image)Model.FindModel(typeof(SourceGrid.Cells.Models.Image));
		ApplyPreparedValue(prepared);
	}

	public bool CanApplyPreparedValue => ReferenceEquals(Model, _modelContainer) && ReferenceEquals(Model.ValueModel, _valueModel)
		&& ReferenceEquals(Model.FindModel(typeof(SourceGrid.Cells.Models.Image)), _imageModel);

	public void ApplyPreparedValue(CrudGridPreparedValue prepared) {
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

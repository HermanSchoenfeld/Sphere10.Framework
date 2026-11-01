// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester.Screens;

/// <summary>Adds a live provisional preview to the reusable native property editor for the GPU registration.</summary>
public class ScreenHostingPreferencesEditor : DefaultCrudEntityEditor {
	private readonly ScreenHostingPreferences _values;
	private readonly Label _preview;

	/// <summary>Receives only the edit session's detached values; no operational screen or storage is activated.</summary>
	public ScreenHostingPreferencesEditor(ScreenHostingPreferences values) {
		Guard.ArgumentNotNull(values, nameof(values));
		_values = values;
		HiddenProperties = new[] { "ID", "Provider" };
		PropertySort = PropertySort.Categorized;
		_preview = new Label { Name = "PreferencePreview", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8), ForeColor = Color.RoyalBlue };
		Controls.Add(_preview);
		_preview.SendToBack();
		UpdatePreview();
	}

	// Previewing this edit-session model does not publish it to the live screen.
	protected override void OnPropertyChanged(object entity, object propertyName, object oldValue, object newValue) {
		base.OnPropertyChanged(entity, propertyName, oldValue, newValue);
		UpdatePreview();
	}

	// The custom surface retains the same nested/collection/grid editing and validation as the default editor.
	private void UpdatePreview() {
		_preview.Text = $"GPU preferences preview: {_values.Heading} ({_values.Details})";
	}
}

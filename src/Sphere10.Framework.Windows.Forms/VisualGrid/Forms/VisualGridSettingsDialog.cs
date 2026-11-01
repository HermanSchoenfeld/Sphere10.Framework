// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Edits one detached grid settings value without owning a profile or persistence provider.</summary>
public partial class VisualGridSettingsDialog : Form {
	/// <summary>Creates a designer-editable native surface without resolving settings or services.</summary>
	public VisualGridSettingsDialog() {
		InitializeComponent();
		_ok.Enabled = false;
		_editor.PropertyFilter = (owner, property) => !(owner is SettingsObject && property.Name is nameof(SettingsObject.ID) or nameof(SettingsObject.Provider));
	}

	/// <summary>Edits the supplied detached value; cancellation leaves the owning grid unchanged.</summary>
	public VisualGridSettingsDialog(VisualGridSettings settings)
		: this() {
		Guard.ArgumentNotNull(settings, nameof(settings));
		Settings = settings;
		_ok.Enabled = true;
		_editor.SetEntity(DataSourceCapabilities.CanUpdate, settings, false);
	}

	/// <summary>The provisional value, accepted by the grid only when the dialog returns OK.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public VisualGridSettings Settings { get; }

	// Validation keeps invalid values visible for correction; it does not open a store or save a record.
	private void AcceptClick(object sender, EventArgs args) {
		var validation = Settings.Validate();
		if (validation.IsFailure) {
			_error.Text = string.Join(Environment.NewLine, validation.ErrorMessages);
			_error.ForeColor = Color.Firebrick;
			return;
		}
		DialogResult = DialogResult.OK;
		Close();
	}
}

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

public partial class VisualGrid {
	private VisualGridTraits _traits = VisualGridTraits.AllowsSettingsChange;

	/// <summary>Optional interaction features; bound settings remain visible when editing is disabled.</summary>
	[Category("Behavior"), DefaultValue(VisualGridTraits.AllowsSettingsChange)]
	public VisualGridTraits Traits {
		get {
			return _traits;
		}
		set {
			_traits = value;
			UpdateSettingsButton();
		}
	}

	/// <summary>Enables the settings button without changing whether this grid has settings.</summary>
	[Category("Behavior"), DefaultValue(true)]
	public bool AllowsSettingsChange {
		get {
			return Traits.HasFlag(VisualGridTraits.AllowsSettingsChange);
		}
		set {
			Traits = Traits.CopyAndSetFlags(VisualGridTraits.AllowsSettingsChange, value);
		}
	}

	/// <summary>Edits a detached grid value and publishes accepted changes through the grid's existing settings owner.</summary>
	public async Task<bool> ShowSettingsAsync() {
		if (!AllowsSettingsChange || _preferences == null && _gridSettings == null)
			return false;

		using var dialog = new VisualGridSettingsDialog(CaptureGridPreferences());
		if (await dialog.ShowDialogAsync(this) != DialogResult.OK || IsDisposed || Disposing)
			return false;

		ApplyGridPreferences(dialog.Settings);
		SaveGridSettings(true);
		await RefreshGrid();
		return true;
	}

	// Presence controls visibility; the trait controls enabled state and never creates a settings object.
	private void UpdateSettingsButton() {
		if (_settingsButton == null)
			return;
		_settingsButton.Visible = _preferences != null || _gridSettings != null;
		_settingsButton.Enabled = AllowsSettingsChange;
		UpdateButtonBarVisibility();
	}

	// The ordinary grid value editor owns only its provisional copy; persistence stays on the existing accepted-change path.
	private async void SettingsButtonClick(object sender, EventArgs args) {
		try {
			await ShowSettingsAsync();
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}
}

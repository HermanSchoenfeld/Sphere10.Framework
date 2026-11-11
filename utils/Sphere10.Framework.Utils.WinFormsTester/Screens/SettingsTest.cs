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
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Demonstrates attribute-declared screen preferences alongside standalone user/global records.</summary>
[Settings(typeof(SettingsTest.SettingsDemoScreenSettings))]
public partial class SettingsTest : ApplicationScreen {
	private const string _demoRecordId = "WinFormsTester.SettingsDemo";

	/// <summary>Creates the example without loading or writing a user's settings.</summary>
	public SettingsTest() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		_outputTextBox.Text = "This screen declares its preferences with [Settings(typeof(SettingsDemoScreenSettings))]." + Environment.NewLine
			+ "Open Options > Screen Options to edit its default scope; Apply updates the Global scope checkbox." + Environment.NewLine
			+ "The standalone example retains the existing SettingsObject API and an encrypted field." + Environment.NewLine
			+ "Load and Save act only on the displayed scope and this demo's isolated record ID. Secret values are never printed.";
	}

	// The class attribute declares the schema; this accessor only reads the existing typed owner value.
	private SettingsDemoScreenSettings Preferences => (SettingsDemoScreenSettings)base.ScreenSettings;

	protected override void CopyModelToUI() {
		base.CopyModelToUI();
		if (!HasSettings)
			return;
		using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing))
			_globalScope.Checked = Preferences.PreferGlobalScope;
	}

	private void ScopeChanged(object sender, EventArgs e) {
		if (!Updating && HasSettings) {
			Preferences.PreferGlobalScope = _globalScope.Checked;
			MarkSettingsChanged();
		}
		_status.Text = "Selected " + (_globalScope.Checked ? "global" : "user") + " scope. Load to inspect its isolated record.";
	}

	private async void LoadClicked(object sender, EventArgs e) => await LoadDemoAsync();

	// The selected standalone provider owns this record; loading never starts during screen activation.
	private async Task LoadDemoAsync() {
		try {
			var global = _globalScope.Checked;
			var provider = global ? GlobalSettings.Provider : UserSettings.Provider;
			SetBusy(true);
			var values = await Task.Run(() => provider.Get<Test1Settings>(_demoRecordId));
			if (IsDisposed || Disposing)
				return;
			_language.Text = values.ActiveLanguage;
			_counter.Value = Math.Clamp(values.RunCount, 0, int.MaxValue);
			_secret.Clear();
			_outputTextBox.Text = "Scope: " + (global ? "Global" : "User") + Environment.NewLine
				+ "Language: " + values.ActiveLanguage + Environment.NewLine
				+ "Counter: " + values.RunCount + Environment.NewLine
				+ "First run: " + values.FirstRunDate.ToString("u") + Environment.NewLine
				+ "Encrypted field present: " + !string.IsNullOrEmpty(values.Secret);
			_status.Text = "Loaded. Editing these fields does not save until Save selected scope is pressed.";
		} catch (Exception error) {
			if (!IsDisposed && !Disposing)
				_status.Text = "Load failed: " + error.Message;
		} finally {
			if (!IsDisposed && !Disposing)
				SetBusy(false);
		}
	}

	// Save is deliberate and never exposes the encrypted value in a log or diagnostic pane.
	private async void SaveClicked(object sender, EventArgs e) => await SaveDemoAsync();

	// Persist a detached snapshot so provider encryption never mutates a value still bound to the editor.
	private async Task SaveDemoAsync() {
		try {
			if (string.IsNullOrWhiteSpace(_language.Text)) {
				_status.Text = "Enter a language code before saving.";
				return;
			}
			var provider = _globalScope.Checked ? GlobalSettings.Provider : UserSettings.Provider;
			var language = _language.Text.Trim();
			var counter = decimal.ToInt32(_counter.Value);
			var secret = _secret.Text;
			SetBusy(true);
			var current = await Task.Run(() => provider.Get<Test1Settings>(_demoRecordId));
			var snapshot = new Test1Settings {
				ID = _demoRecordId,
				Provider = provider,
				DBMSType = current.DBMSType,
				ConnectionString = current.ConnectionString,
				ActiveLanguage = language,
				RunCount = counter,
				FirstTimeRun = current.FirstTimeRun,
				FirstRunDate = current.FirstRunDate,
				Secret = string.IsNullOrEmpty(secret) ? current.Secret : secret
			};
			await Task.Run(() => provider.SaveSetting(snapshot));
			if (IsDisposed || Disposing)
				return;
			_secret.Clear();
			_status.Text = "Saved the selected isolated record. Reload to verify persistence.";
		} catch (Exception error) {
			if (!IsDisposed && !Disposing)
				_status.Text = "Save failed: " + error.Message;
		} finally {
			if (!IsDisposed && !Disposing)
				SetBusy(false);
		}
	}

	// One operation owns the editor until its captured provider read/write finishes.
	private void SetBusy(bool busy) {
		_loadButton.Enabled = !busy;
		_saveButton.Enabled = !busy;
		_globalScope.Enabled = !busy;
	}

	#region Inner Types

	/// <summary>Ordinary registration-owned settings for the settings demonstration.</summary>
	[UserEditable]
	public class SettingsDemoScreenSettings : SettingsObject {
		/// <summary>The initial scope selected for the isolated standalone demonstration.</summary>
		[DefaultValue(false)]
		public bool PreferGlobalScope { get; set; } = false;
	}
	#endregion
}

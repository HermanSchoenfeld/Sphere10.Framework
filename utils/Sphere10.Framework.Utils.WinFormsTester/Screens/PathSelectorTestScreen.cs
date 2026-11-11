// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Compares all four native path-selection modes and their validation behavior.</summary>
public partial class PathSelectorTestScreen : ApplicationScreen {
	/// <summary>Creates the Designer-owned selectors without opening a picker.</summary>
	public PathSelectorTestScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	/// <summary>The preferences declared by this screen type and restored for its exact registration.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override PathSelectorScreenSettings ScreenSettings => (PathSelectorScreenSettings)base.ScreenSettings;

	/// <summary>Restores preferences and applies accepted Options through the same visible checkbox.</summary>
	protected override void CopyModelToUI() {
		base.CopyModelToUI();
		if (HasSettings)
			ApplySettings();
	}

	// Each mode shares nonmodal, visible result feedback.
	private void PathSelectorControl1_PathChanged() => ShowSelection(_pathSelectorControl1);

	// Save-file selection does not write a file.
	private void PathSelectorControl2_PathChanged() => ShowSelection(_pathSelectorControl2);

	// Generic file selection retains its original mode.
	private void PathSelectorControl3_PathChanged() => ShowSelection(_pathSelectorControl3);

	// Folder selection follows the same result path.
	private void PathSelectorControl4_PathChanged() => ShowSelection(_pathSelectorControl4);

	// The mode and selected path remain visible without interrupting another selection.
	private void ShowSelection(PathSelectorControl selector) => _selectionSummary.Text = $"{selector.Mode}: {selector.Path}";

	// Options updates do not become new local preference edits.
	private void ApplySettings() {
		using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing)) {
			_checkBox1.Checked = ScreenSettings.RequireExistingPaths;
			CheckBox1_CheckedChanged(this, EventArgs.Empty);
		}
	}

	// Apply one validation choice consistently to all four selectors.
	private void CheckBox1_CheckedChanged(object sender, EventArgs args) {
		foreach (var selector in new[] { _pathSelectorControl1, _pathSelectorControl2, _pathSelectorControl3, _pathSelectorControl4 })
			selector.ForcePathExists = _checkBox1.Checked;
		if (HasSettings && !Updating) {
			ScreenSettings.RequireExistingPaths = _checkBox1.Checked;
			MarkSettingsChanged();
		}
	}

	#region Inner Types

	/// <summary>Controls path validation without retaining selected personal paths.</summary>
	[UserEditable]
	public class PathSelectorScreenSettings : SettingsObject {
		/// <summary>Whether all four selectors require an existing path.</summary>
		[DefaultValue(false)]
		[DisplayName("Require paths to exist")]
		public bool RequireExistingPaths {
			get; set;
		}
	}
	#endregion
}

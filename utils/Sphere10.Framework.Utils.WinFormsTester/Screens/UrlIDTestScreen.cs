// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Shows numeric values and their deterministic URL-safe representations.</summary>
public partial class UrlIDTestScreen : ApplicationScreen {
	/// <summary>Creates the range controls without generating work during construction.</summary>
	public UrlIDTestScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	/// <summary>Restores this registration's range choices before use.</summary>

	/// <summary>The preferences declared by this screen type and restored for its exact registration.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override UrlIdScreenSettings ScreenSettings {
		get {
			return (UrlIdScreenSettings)base.ScreenSettings;
		}
	}

	// Options changes affect choices, never an already generated result.
	protected override void CopyModelToUI() {
		base.CopyModelToUI();
		if (!HasSettings)
			return;
		var values = ScreenSettings;
		using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing)) {
			_start.Value = values.FirstValue;
			_count.Value = Math.Clamp(values.Count, 1, 10000);
			_format.SelectedIndex = Math.Clamp((int)values.Format - 1, 0, 2);
		}
	}

	// Capture only deliberate configuration edits for accepted screen closure.
	protected override void CopyUIToModel() {
		base.CopyUIToModel();
		if (Updating || !HasSettings)
			return;
		var values = ScreenSettings;
		var changed = values.FirstValue != decimal.ToUInt32(_start.Value)
			|| values.Count != decimal.ToInt32(_count.Value)
			|| values.Format != (UrlID.Format)(_format.SelectedIndex + 1);
		if (!changed)
			return;
		values.FirstValue = decimal.ToUInt32(_start.Value);
		values.Count = decimal.ToInt32(_count.Value);
		values.Format = (UrlID.Format)(_format.SelectedIndex + 1);
		MarkSettingsChanged();
	}

	private void PreferencesChanged(object sender, EventArgs e) {
		CopyUIToModel();
	}

	// Validate overflow before generating the complete bounded range locally.
	private void _generateButton_Click(object sender, EventArgs e) {
		var first = decimal.ToUInt32(_start.Value);
		var count = decimal.ToInt32(_count.Value);
		if ((ulong)first + (uint)count - 1 > uint.MaxValue) {
			_status.Text = "The requested range exceeds UInt32.MaxValue.";
			return;
		}
		var output = new StringBuilder("Numeric value\tURL identifier" + Environment.NewLine);
		for (var index = 0; index < count; index++) {
			var value = first + (uint)index;
			output.Append(value).Append('\t').AppendLine(UrlID.Generate(value, (UrlID.Format)(_format.SelectedIndex + 1)));
		}
		_textBoxEx.Text = output.ToString();
		_status.Text = $"Generated {count} identifier(s), starting at {first}.";
	}

	private void ClearClicked(object sender, EventArgs e) {
		_textBoxEx.Clear();
		_status.Text = "Output cleared. The configured range is unchanged.";
	}

	#region Inner Types

	/// <summary>Range preferences for the URL identifier screen.</summary>
	[UserEditable]
	public class UrlIdScreenSettings : SettingsObject {
		/// <summary>The first unsigned numeric value.</summary>
		[DefaultValue(1)]
		public uint FirstValue { get; set; } = 1;

		/// <summary>The number of examples, from one to ten thousand.</summary>
		[DefaultValue(20)]
		[Range(1, 10000)]
		public int Count { get; set; } = 20;

		/// <summary>The identifier encoding.</summary>
		[DefaultValue(UrlID.Format.Base62)]
		public UrlID.Format Format { get; set; } = UrlID.Format.Base62;
	}
	#endregion
}

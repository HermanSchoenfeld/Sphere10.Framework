// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Exercises sentence formatting and paragraph boundaries through the existing builder.</summary>
public partial class ParagraphBuilderScreen : ApplicationScreen {
	/// <summary>Creates the editable sentence and output surfaces.</summary>
	public ParagraphBuilderScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		Builder = new ParagraphBuilder();
	}

	/// <summary>The caller-supplied or default paragraph builder demonstrated by this screen.</summary>
	public ParagraphBuilder Builder { get; set; }

	/// <summary>The preferences declared by this screen type and restored for its exact registration.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override ParagraphBuilderScreenSettings ScreenSettings {
		get {
			return (ParagraphBuilderScreenSettings)base.ScreenSettings;
		}
	}

	protected override void CopyModelToUI() {
		base.CopyModelToUI();
		if (!HasSettings)
			return;
		using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing))
			_formatSentence.Checked = ScreenSettings.FormatSentences;
	}

	protected override void CopyUIToModel() {
		base.CopyUIToModel();
		if (Updating || !HasSettings)
			return;
		var values = ScreenSettings;
		var changed = values.FormatSentences != _formatSentence.Checked;
		if (!changed)
			return;
		values.FormatSentences = _formatSentence.Checked;
		MarkSettingsChanged();
	}

	private void PreferencesChanged(object sender, EventArgs e) {
		CopyUIToModel();
	}

	// Empty drafts do not silently alter or replace an existing paragraph.
	private void _appendSentenceButton_Click(object sender, EventArgs e) {
		if (string.IsNullOrWhiteSpace(_sentenceTextBox.Text)) {
			_status.Text = "Enter a sentence before appending it.";
			return;
		}
		Builder ??= new ParagraphBuilder();
		Builder.AppendSentence(_sentenceTextBox.Text, _formatSentence.Checked);
		RefreshParagraphTextBox();
	}

	private void _appendBreakButton_Click(object sender, EventArgs e) {
		Builder ??= new ParagraphBuilder();
		Builder.AppendParagraphBreak();
		RefreshParagraphTextBox();
	}

	private void ClearClicked(object sender, EventArgs e) {
		// A fresh builder also resets its remembered punctuation boundary.
		Builder = new ParagraphBuilder();
		RefreshParagraphTextBox();
	}

	private void CopyClicked(object sender, EventArgs e) {
		if (!string.IsNullOrEmpty(_paragraphTextBox.Text))
			Clipboard.SetText(_paragraphTextBox.Text);
		_status.Text = "Paragraph copied.";
	}

	private void RefreshParagraphTextBox() {
		_paragraphTextBox.Text = Builder.ToString();
		_status.Text = $"Paragraph contains {Builder.Length} character(s).";
	}

	#region Inner Types

	/// <summary>Presentation preferences for the paragraph builder.</summary>
	[UserEditable]
	public class ParagraphBuilderScreenSettings : SettingsObject {
		/// <summary>Whether appended sentences receive normal punctuation and capitalization.</summary>
		[DefaultValue(true)]
		public bool FormatSentences { get; set; } = true;
	}
	#endregion
}

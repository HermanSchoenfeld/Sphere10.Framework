// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Shows typed enum choices, description attributes and an explicit empty option.</summary>
public partial class EnumComboScreen : WinFormsApplicationScreen {
	/// <summary>Creates the Designer-owned enum example.</summary>
	public EnumComboScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		_enumComboBox.AllowEmptyOption = true;
		_enumComboBox.EmptyOptionText = "(No selection)";
		_enumComboBox.EnumType = typeof(TestEnum);
		UpdateSelection();
	}

	// Programmatic selection uses the same typed contract as the user's dropdown choice.
	private void SetNullButton_Click(object sender, EventArgs args) {
		_enumComboBox.SelectedEnum = null;
	}

	// Description attributes affect presentation, while the actual value remains an enum.
	private void SetEnumVal1Button_Click(object sender, EventArgs args) {
		_enumComboBox.SelectedEnum = TestEnum.WithDescription;
	}

	// An undescribed value falls back to its ordinary member name.
	private void SetEnumValue2Button_Click(object sender, EventArgs args) {
		_enumComboBox.SelectedEnum = TestEnum.WithoutDescription;
	}

	// Inspection stays on the page rather than opening a modal message for each choice.
	private void GetValueButton_Click(object sender, EventArgs args) {
		_outputTextBox.AppendText(_selectionSummary.Text + Environment.NewLine);
	}

	// The readout always reflects the completed selection.
	private void SelectionChanged(object sender, EventArgs args) {
		UpdateSelection();
	}

	// Keep the displayed caption, CLR value and numeric value distinct.
	private void UpdateSelection() {
		var value = _enumComboBox.SelectedEnum;
		_selectionSummary.Text = value == null ? "Value: null (no selection)" : $"Value: {value} · Numeric value: {Convert.ToInt64(value)} · Caption: {_enumComboBox.Text}";
	}

	/// <summary>The original public example enum retains its two values.</summary>
	[Description("Enum choice example")]
	public enum TestEnum {
		/// <summary>A member whose friendly caption comes from DescriptionAttribute.</summary>
		[Description("A friendly description")]
		WithDescription,

		/// <summary>A member displayed with its ordinary identifier.</summary>
		WithoutDescription
	}
}

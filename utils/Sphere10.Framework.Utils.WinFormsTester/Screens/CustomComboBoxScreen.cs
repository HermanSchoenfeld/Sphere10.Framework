// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Demonstrates tabular, composite and rich-text content hosted by the same dropdown control.</summary>
public partial class CustomComboBoxScreen : WinFormsApplicationScreen {
	/// <summary>Creates real Designer-owned popup controls; no popup is opened during construction.</summary>
	public CustomComboBoxScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		Disposables.Add(_gridPopup);
		Disposables.Add(_editorPopup);
		Disposables.Add(_textPopup);
	}

	/// <summary>Seeds the sample catalog once, independently of popup opening.</summary>
	protected override void OnLoad(EventArgs e) {
		base.OnLoad(e);
		if (Tools.Runtime.IsDesignMode || _gridPopup.Rows.Count != 0)
			return;
		_gridPopup.Rows.Add("Sensor module", "Electronics", "$24.00");
		_gridPopup.Rows.Add("Filter cartridge", "Maintenance", "$12.50");
		_gridPopup.Rows.Add("Motor assembly", "Mechanical", "$185.00");
	}

	/// <summary>Closes popups before the screen's owned content is disposed.</summary>
	protected override void OnDestroyScreen() {
		_customComboBox1.HideDropDown();
		_customComboBox2.HideDropDown();
		_customComboBox3.HideDropDown();
		base.OnDestroyScreen();
	}

	// Accepting a row demonstrates a real value transition in the owning combo.
	private void CatalogChosen(object sender, DataGridViewCellEventArgs args) {
		if (args.RowIndex < 0)
			return;
		_customComboBox1.Text = _gridPopup.Rows[args.RowIndex].Cells[0].Value.ToString();
		_customComboBox1.HideDropDown();
		_label1.Text = "Selected catalog item: " + _customComboBox1.Text;
	}

	// The composite's acceptance event follows the same visible value/close workflow as the table.
	private void CompositeChosen(object sender, EventArgs args) {
		_customComboBox2.Text = _editorPopup.SelectedCategories.Length == 0 ? "No categories selected" : string.Join(", ", _editorPopup.SelectedCategories);
		_customComboBox2.HideDropDown();
		_label1.Text = "Accepted categories: " + _customComboBox2.Text;
	}

	// The native open/close events remain visible for each hosted control family.
	private void CustomComboBox1_DropDown(object sender, EventArgs args) {
		_label1.Text = "Catalog popup opened.";
	}

	// Closing retains the table and its selection for the next opening.
	private void CustomComboBox1_DropDownClosed(object sender, EventArgs args) {
		_label1.Text = "Catalog popup closed. Current value: " + _customComboBox1.Text;
	}

	// The composite popup contains native options and a checked list.
	private void CustomComboBox2_DropDown(object sender, EventArgs args) {
		_label1.Text = "Composite editor opened.";
	}

	// The same composite instance survives closing.
	private void CustomComboBox2_DropDownClosed(object sender, EventArgs args) {
		_label1.Text = "Composite editor closed; its draft is retained.";
	}

	// Rich text retains its own native editing and selection behavior.
	private void CustomComboBox3_DropDown(object sender, EventArgs args) {
		_label1.Text = "Rich-text editor opened.";
	}

	// Summarize the result without copying a potentially large editor document into the combo caption.
	private void CustomComboBox3_DropDownClosed(object sender, EventArgs args) {
		_label1.Text = $"Rich-text editor closed · {_textPopup.TextLength:N0} characters retained.";
	}
}

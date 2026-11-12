// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

partial class PlaceHolderTestScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private IContainer _components;
	private TableLayoutPanel _examples;
	private PlaceHolderTextExtender _cueTextExtender1;
	private TextBox _textBox1;
	private ComboBox _comboBox1;
	private MaskedTextBox _maskedTextBox1;
	private RichTextBox _richTextBox1;
	private SearchTextBox _searchTextBox1;
	private TextBoxEx _textBoxEx1;
	private MoneyBox _moneyBox1;
	private SearchTextBox _searchTextBox2;
	private ComboBoxEx _comboBoxEx1;
	private FlowLayoutPanel _actions;
	private Button _clearInputs;
	private Label _textBox1Label;
	private Label _comboBox1Label;
	private Label _maskedTextBox1Label;
	private Label _richTextBox1Label;
	private Label _searchTextBox1Label;
	private Label _textBoxEx1Label;
	private Label _moneyBox1Label;
	private Label _searchTextBox2Label;
	private Label _comboBoxEx1Label;

	// The component container owns the native UI timer.
	protected override void Dispose(bool disposing) {
		try {
			if (disposing)
				_components?.Dispose();
		} finally {
			base.Dispose(disposing);
		}
	}

	// Static layout and named event wiring remain editable in the native Designer.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_layout.Name = "_layout";
		_heading = new Label();
		_heading.Name = "_heading";
		_introduction = new Label();
		_introduction.Name = "_introduction";
		_components = new Container();
		_examples = new TableLayoutPanel();
		_examples.Name = "_examples";
		_cueTextExtender1 = new PlaceHolderTextExtender(_components);
		_textBox1 = new TextBox();
		_comboBox1 = new ComboBox();
		_maskedTextBox1 = new MaskedTextBox();
		_richTextBox1 = new RichTextBox();
		_searchTextBox1 = new SearchTextBox();
		_textBoxEx1 = new TextBoxEx();
		_moneyBox1 = new MoneyBox();
		_searchTextBox2 = new SearchTextBox();
		_comboBoxEx1 = new ComboBoxEx();
		_actions = new FlowLayoutPanel();
		_actions.Name = "_actions";
		_clearInputs = new Button();
		_clearInputs.Name = "_clearInputs";
		_textBox1Label = new Label();
		_textBox1Label.Name = "_textBox1Label";
		_comboBox1Label = new Label();
		_comboBox1Label.Name = "_comboBox1Label";
		_maskedTextBox1Label = new Label();
		_maskedTextBox1Label.Name = "_maskedTextBox1Label";
		_richTextBox1Label = new Label();
		_richTextBox1Label.Name = "_richTextBox1Label";
		_searchTextBox1Label = new Label();
		_searchTextBox1Label.Name = "_searchTextBox1Label";
		_textBoxEx1Label = new Label();
		_textBoxEx1Label.Name = "_textBoxEx1Label";
		_moneyBox1Label = new Label();
		_moneyBox1Label.Name = "_moneyBox1Label";
		_searchTextBox2Label = new Label();
		_searchTextBox2Label.Name = "_searchTextBox2Label";
		_comboBoxEx1Label = new Label();
		_comboBoxEx1Label.Name = "_comboBoxEx1Label";
		_layout.SuspendLayout();
		SuspendLayout();

		// The heading and instructions remain with this screen when it is detached.
		_layout.Dock = DockStyle.Fill;
		_layout.Padding = new Padding(20);
		_layout.ColumnCount = 1;
		_layout.RowCount = 4;
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_heading.AutoSize = true;
		_heading.Dock = DockStyle.Fill;
		_heading.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 17F, FontStyle.Bold);
		_heading.Margin = new Padding(0, 0, 0, 10);
		_heading.Text = "Placeholder Editors";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 18);
		_introduction.Text = "Compare an extender on ordinary native controls with built-in Framework placeholder properties. Enter text, move focus and clear the examples to inspect each cue.";
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);

		_examples.AutoSize = true;
		_examples.Dock = DockStyle.Top;
		_examples.ColumnCount = 2;
		_examples.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240F));
		_examples.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_textBox1Label.AutoSize = true;
		_textBox1Label.Dock = DockStyle.Fill;
		_textBox1Label.Margin = new Padding(0, 8, 0, 8);
		_textBox1Label.Text = "Extender · TextBox";
		_textBox1.Name = "textBox1";
		_textBox1.Dock = DockStyle.Fill;
		_textBox1.Margin = new Padding(3, 5, 3, 5);
		_examples.Controls.Add(_textBox1Label, 0, 0);
		_examples.Controls.Add(_textBox1, 1, 0);
		_comboBox1Label.AutoSize = true;
		_comboBox1Label.Dock = DockStyle.Fill;
		_comboBox1Label.Margin = new Padding(0, 8, 0, 8);
		_comboBox1Label.Text = "Extender · ComboBox";
		_comboBox1.Name = "comboBox1";
		_comboBox1.Dock = DockStyle.Fill;
		_comboBox1.Margin = new Padding(3, 5, 3, 5);
		_examples.Controls.Add(_comboBox1Label, 0, 1);
		_examples.Controls.Add(_comboBox1, 1, 1);
		_maskedTextBox1Label.AutoSize = true;
		_maskedTextBox1Label.Dock = DockStyle.Fill;
		_maskedTextBox1Label.Margin = new Padding(0, 8, 0, 8);
		_maskedTextBox1Label.Text = "Extender · MaskedTextBox";
		_maskedTextBox1.Name = "maskedTextBox1";
		_maskedTextBox1.Dock = DockStyle.Fill;
		_maskedTextBox1.Margin = new Padding(3, 5, 3, 5);
		_examples.Controls.Add(_maskedTextBox1Label, 0, 2);
		_examples.Controls.Add(_maskedTextBox1, 1, 2);
		_richTextBox1Label.AutoSize = true;
		_richTextBox1Label.Dock = DockStyle.Fill;
		_richTextBox1Label.Margin = new Padding(0, 8, 0, 8);
		_richTextBox1Label.Text = "Extender · RichTextBox";
		_richTextBox1.Name = "richTextBox1";
		_richTextBox1.Dock = DockStyle.Fill;
		_richTextBox1.Margin = new Padding(3, 5, 3, 5);
		_examples.Controls.Add(_richTextBox1Label, 0, 3);
		_examples.Controls.Add(_richTextBox1, 1, 3);
		_searchTextBox1Label.AutoSize = true;
		_searchTextBox1Label.Dock = DockStyle.Fill;
		_searchTextBox1Label.Margin = new Padding(0, 8, 0, 8);
		_searchTextBox1Label.Text = "Extender · SearchTextBox";
		_searchTextBox1.Name = "searchTextBox1";
		_searchTextBox1.Dock = DockStyle.Fill;
		_searchTextBox1.Margin = new Padding(3, 5, 3, 5);
		_examples.Controls.Add(_searchTextBox1Label, 0, 4);
		_examples.Controls.Add(_searchTextBox1, 1, 4);
		_textBoxEx1Label.AutoSize = true;
		_textBoxEx1Label.Dock = DockStyle.Fill;
		_textBoxEx1Label.Margin = new Padding(0, 8, 0, 8);
		_textBoxEx1Label.Text = "TextBoxEx";
		_textBoxEx1.Name = "textBoxEx1";
		_textBoxEx1.Dock = DockStyle.Fill;
		_textBoxEx1.Margin = new Padding(3, 5, 3, 5);
		_examples.Controls.Add(_textBoxEx1Label, 0, 5);
		_examples.Controls.Add(_textBoxEx1, 1, 5);
		_textBoxEx1.PlaceHolderText = "Built-in placeholder property";
		_moneyBox1Label.AutoSize = true;
		_moneyBox1Label.Dock = DockStyle.Fill;
		_moneyBox1Label.Margin = new Padding(0, 8, 0, 8);
		_moneyBox1Label.Text = "MoneyBox";
		_moneyBox1.Name = "moneyBox1";
		_moneyBox1.Dock = DockStyle.Fill;
		_moneyBox1.Margin = new Padding(3, 5, 3, 5);
		_examples.Controls.Add(_moneyBox1Label, 0, 6);
		_examples.Controls.Add(_moneyBox1, 1, 6);
		_moneyBox1.PlaceHolderText = "Enter an amount";
		_searchTextBox2Label.AutoSize = true;
		_searchTextBox2Label.Dock = DockStyle.Fill;
		_searchTextBox2Label.Margin = new Padding(0, 8, 0, 8);
		_searchTextBox2Label.Text = "SearchTextBox";
		_searchTextBox2.Name = "searchTextBox2";
		_searchTextBox2.Dock = DockStyle.Fill;
		_searchTextBox2.Margin = new Padding(3, 5, 3, 5);
		_examples.Controls.Add(_searchTextBox2Label, 0, 7);
		_examples.Controls.Add(_searchTextBox2, 1, 7);
		_searchTextBox2.PlaceHolderText = "Search…";
		_comboBoxEx1Label.AutoSize = true;
		_comboBoxEx1Label.Dock = DockStyle.Fill;
		_comboBoxEx1Label.Margin = new Padding(0, 8, 0, 8);
		_comboBoxEx1Label.Text = "ComboBoxEx";
		_comboBoxEx1.Name = "comboBoxEx1";
		_comboBoxEx1.Dock = DockStyle.Fill;
		_comboBoxEx1.Margin = new Padding(3, 5, 3, 5);
		_examples.Controls.Add(_comboBoxEx1Label, 0, 8);
		_examples.Controls.Add(_comboBoxEx1, 1, 8);
		_comboBoxEx1.PlaceHolderText = "Choose or type a value";
		_richTextBox1.Height = 50;
		_comboBox1.Items.AddRange(new object[] { "First choice", "Second choice", "Third choice" });
		_comboBoxEx1.Items.AddRange(new object[] { "First choice", "Second choice", "Third choice" });
		_clearInputs.AutoSize = true;
		_clearInputs.MinimumSize = new Size(110, 34);
		_clearInputs.Text = "Clear all examples";
		_clearInputs.UseVisualStyleBackColor = true;
		_clearInputs.Click += ClearInputsClicked;
		_actions.AutoSize = true;
		_actions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_actions.Dock = DockStyle.Fill;
		_actions.Margin = Padding.Empty;
		_actions.Controls.AddRange(new Control[] { _clearInputs });

		// Content-sized command rows keep their complete buttons visible at smaller widths.
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_examples, 0, 2);
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_actions, 0, 3);
		Font = SystemFonts.MessageBoxFont;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		BackColor = SystemColors.Window;
		Name = "PlaceHolderTestScreen";
		Text = "Placeholder Editors";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}

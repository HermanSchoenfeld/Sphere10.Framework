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

partial class BloomFilterAnalysisScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private FlowLayoutPanel _inputs;
	private FlowLayoutPanel _commands;
	private TextBoxEx _standardTextBox;
	private Label _status;
	private Label _itemsLabel;
	private NumericUpDown _expectedItems;
	private Label _errorLabel;
	private NumericUpDown _targetError;
	private Label _roundsLabel;
	private NumericUpDown _hashRounds;
	private Button _estimateButton;
	private Button _metricsTableButton;
	private Button _clearButton;

	// The complete static demonstration layout remains editable on the designer surface.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_heading = new Label();
		_introduction = new Label();
		_inputs = new FlowLayoutPanel();
		_commands = new FlowLayoutPanel();
		_standardTextBox = new TextBoxEx();
		_status = new Label();
		_itemsLabel = new Label();
		_expectedItems = new NumericUpDown();
		_errorLabel = new Label();
		_targetError = new NumericUpDown();
		_roundsLabel = new Label();
		_hashRounds = new NumericUpDown();
		_estimateButton = new Button();
		_metricsTableButton = new Button();
		_clearButton = new Button();
		_layout.SuspendLayout();
		_inputs.SuspendLayout();
		_commands.SuspendLayout();
		SuspendLayout();
		((ISupportInitialize)_expectedItems).BeginInit();
		((ISupportInitialize)_targetError).BeginInit();
		((ISupportInitialize)_hashRounds).BeginInit();

		_layout.Dock = DockStyle.Fill;
		_layout.ColumnCount = 1;
		_layout.RowCount = 6;
		_layout.Padding = new Padding(20);
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);
		_layout.Controls.Add(_inputs, 0, 2);
		_layout.Controls.Add(_commands, 0, 3);
		_layout.Controls.Add(_standardTextBox, 0, 4);
		_layout.Controls.Add(_status, 0, 5);

		_heading.Text = "Bloom Filter Sizing";
		_heading.AutoSize = true;
		_heading.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
		_heading.Margin = new Padding(0, 0, 0, 12);
		_introduction.Text = "Estimate the bit budget and expected false-positive rate. Values describe a filter; this screen does not allocate the estimated storage.";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 16);

		_inputs.AutoSize = true;
		_inputs.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_inputs.Dock = DockStyle.Fill;
		_inputs.Margin = new Padding(0, 0, 0, 10);

		_commands.AutoSize = true;
		_commands.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_commands.Dock = DockStyle.Fill;
		_commands.Margin = new Padding(0, 0, 0, 10);

		_itemsLabel.Name = "_itemsLabel";
		_itemsLabel.Text = "Expected items";
		_itemsLabel.AutoSize = true;
		_itemsLabel.Padding = new Padding(0, 5, 0, 0);
		_itemsLabel.Margin = new Padding(0, 3, 12, 3);
		_inputs.Controls.Add(_itemsLabel);

		_expectedItems.Name = "_expectedItems";
		_expectedItems.Minimum = 1M;
		_expectedItems.Maximum = 1000000M;
		_expectedItems.DecimalPlaces = 0;
		_expectedItems.Value = 100000M;
		_expectedItems.Width = 110;
		_expectedItems.ThousandsSeparator = true;
		_expectedItems.Margin = new Padding(0, 3, 12, 3);
		_inputs.Controls.Add(_expectedItems);
		_expectedItems.ValueChanged += PreferencesEdited;

		_errorLabel.Name = "_errorLabel";
		_errorLabel.Text = "Target error";
		_errorLabel.AutoSize = true;
		_errorLabel.Padding = new Padding(0, 5, 0, 0);
		_errorLabel.Margin = new Padding(0, 3, 12, 3);
		_inputs.Controls.Add(_errorLabel);

		_targetError.Name = "_targetError";
		_targetError.Minimum = 0.0001M;
		_targetError.Maximum = 0.5M;
		_targetError.DecimalPlaces = 4;
		_targetError.Value = 0.01M;
		_targetError.Width = 110;
		_targetError.ThousandsSeparator = true;
		_targetError.Margin = new Padding(0, 3, 12, 3);
		_inputs.Controls.Add(_targetError);
		_targetError.ValueChanged += PreferencesEdited;

		_roundsLabel.Name = "_roundsLabel";
		_roundsLabel.Text = "Hash rounds";
		_roundsLabel.AutoSize = true;
		_roundsLabel.Padding = new Padding(0, 5, 0, 0);
		_roundsLabel.Margin = new Padding(0, 3, 12, 3);
		_inputs.Controls.Add(_roundsLabel);

		_hashRounds.Name = "_hashRounds";
		_hashRounds.Minimum = 1M;
		_hashRounds.Maximum = 16M;
		_hashRounds.DecimalPlaces = 0;
		_hashRounds.Value = 4M;
		_hashRounds.Width = 110;
		_hashRounds.ThousandsSeparator = true;
		_hashRounds.Margin = new Padding(0, 3, 12, 3);
		_inputs.Controls.Add(_hashRounds);
		_hashRounds.ValueChanged += PreferencesEdited;

		_estimateButton.Name = "_estimateButton";
		_estimateButton.Text = "Estimate filter";
		_estimateButton.AutoSize = true;
		_estimateButton.MinimumSize = new Size(110, 32);
		_estimateButton.Margin = new Padding(0, 0, 8, 0);
		_estimateButton.Click += EstimateClicked;
		_commands.Controls.Add(_estimateButton);

		_metricsTableButton.Name = "_metricsTableButton";
		_metricsTableButton.Text = "Compare configurations";
		_metricsTableButton.AutoSize = true;
		_metricsTableButton.MinimumSize = new Size(110, 32);
		_metricsTableButton.Margin = new Padding(0, 0, 8, 0);
		_metricsTableButton.Click += MetricsClicked;
		_commands.Controls.Add(_metricsTableButton);

		_clearButton.Name = "_clearButton";
		_clearButton.Text = "Clear results";
		_clearButton.AutoSize = true;
		_clearButton.MinimumSize = new Size(110, 32);
		_clearButton.Margin = new Padding(0, 0, 8, 0);
		_clearButton.Click += ClearClicked;
		_commands.Controls.Add(_clearButton);

		_standardTextBox.Name = "_standardTextBox";
		_standardTextBox.Dock = DockStyle.Fill;
		_standardTextBox.Multiline = true;
		_standardTextBox.ReadOnly = true;
		_standardTextBox.ScrollBars = ScrollBars.Both;
		_standardTextBox.WordWrap = false;
		_standardTextBox.BackColor = SystemColors.Window;
		_standardTextBox.Font = new Font("Consolas", 9F);
		_standardTextBox.Margin = new Padding(0, 0, 0, 10);
		_status.Name = "showcaseStatus";
		_status.AutoSize = true;
		_status.Dock = DockStyle.Fill;
		_status.Text = "Ready. Choose an example to see its results.";

		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		Font = SystemFonts.MessageBoxFont;
		BackColor = SystemColors.Window;
		Name = "BloomFilterAnalysisScreen";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		((ISupportInitialize)_expectedItems).EndInit();
		((ISupportInitialize)_targetError).EndInit();
		((ISupportInitialize)_hashRounds).EndInit();
		_commands.ResumeLayout(false);
		_commands.PerformLayout();
		_inputs.ResumeLayout(false);
		_inputs.PerformLayout();
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}

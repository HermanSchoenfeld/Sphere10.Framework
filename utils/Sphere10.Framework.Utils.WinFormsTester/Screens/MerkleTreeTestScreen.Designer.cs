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

partial class MerkleTreeTestScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private FlowLayoutPanel _inputs;
	private FlowLayoutPanel _commands;
	private TextBoxEx _outputTextBox;
	private Label _status;
	private Label _countLabel;
	private NumericUpDown _leafCount;
	private Label _comparisonHint;
	private Button _printAlphabetButton;
	private Button _printIntegersButton;
	private Button _sumPow2Button;
	private Button _printInvPow2Button;
	private Button _perfectMerkleButton;
	private Button _treePerfTestsButton;
	private Button _cancelButton;
	private Button _clearButton;

	// The complete static demonstration layout remains editable on the designer surface.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_heading = new Label();
		_introduction = new Label();
		_inputs = new FlowLayoutPanel();
		_commands = new FlowLayoutPanel();
		_outputTextBox = new TextBoxEx();
		_status = new Label();
		_countLabel = new Label();
		_leafCount = new NumericUpDown();
		_comparisonHint = new Label();
		_printAlphabetButton = new Button();
		_printIntegersButton = new Button();
		_sumPow2Button = new Button();
		_printInvPow2Button = new Button();
		_perfectMerkleButton = new Button();
		_treePerfTestsButton = new Button();
		_cancelButton = new Button();
		_clearButton = new Button();
		_layout.SuspendLayout();
		_inputs.SuspendLayout();
		_commands.SuspendLayout();
		SuspendLayout();
		((ISupportInitialize)_leafCount).BeginInit();

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
		_layout.Controls.Add(_outputTextBox, 0, 4);
		_layout.Controls.Add(_status, 0, 5);

		_heading.Text = "Merkle Trees And Partitions";
		_heading.AutoSize = true;
		_heading.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
		_heading.Margin = new Padding(0, 0, 0, 12);
		_introduction.Text = "Inspect readable concatenation trees, verify power-of-two partitions, or compare actual SHA-256 roots from the simple and flat implementations.";
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

		_countLabel.Name = "_countLabel";
		_countLabel.Text = "Comparison leaves";
		_countLabel.AutoSize = true;
		_countLabel.Padding = new Padding(0, 5, 0, 0);
		_countLabel.Margin = new Padding(0, 3, 12, 3);
		_inputs.Controls.Add(_countLabel);

		_leafCount.Name = "_leafCount";
		_leafCount.Minimum = 1M;
		_leafCount.Maximum = 20000M;
		_leafCount.DecimalPlaces = 0;
		_leafCount.Value = 256M;
		_leafCount.Width = 110;
		_leafCount.ThousandsSeparator = true;
		_leafCount.Margin = new Padding(0, 3, 12, 3);
		_inputs.Controls.Add(_leafCount);
		_leafCount.ValueChanged += PreferencesEdited;

		_comparisonHint.Name = "_comparisonHint";
		_comparisonHint.Text = "Comparison work runs in a cancellable background task.";
		_comparisonHint.AutoSize = true;
		_comparisonHint.Padding = new Padding(0, 5, 0, 0);
		_comparisonHint.Margin = new Padding(0, 3, 12, 3);
		_inputs.Controls.Add(_comparisonHint);

		_printAlphabetButton.Name = "_printAlphabetButton";
		_printAlphabetButton.Text = "Alphabet tree";
		_printAlphabetButton.AutoSize = true;
		_printAlphabetButton.MinimumSize = new Size(110, 32);
		_printAlphabetButton.Margin = new Padding(0, 0, 8, 0);
		_printAlphabetButton.Click += AlphabetClicked;
		_commands.Controls.Add(_printAlphabetButton);

		_printIntegersButton.Name = "_printIntegersButton";
		_printIntegersButton.Text = "Integer tree";
		_printIntegersButton.AutoSize = true;
		_printIntegersButton.MinimumSize = new Size(110, 32);
		_printIntegersButton.Margin = new Padding(0, 0, 8, 0);
		_printIntegersButton.Click += IntegersClicked;
		_commands.Controls.Add(_printIntegersButton);

		_sumPow2Button.Name = "_sumPow2Button";
		_sumPow2Button.Text = "Power-of-two partitions";
		_sumPow2Button.AutoSize = true;
		_sumPow2Button.MinimumSize = new Size(110, 32);
		_sumPow2Button.Margin = new Padding(0, 0, 8, 0);
		_sumPow2Button.Click += PartitionsClicked;
		_commands.Controls.Add(_sumPow2Button);

		_printInvPow2Button.Name = "_printInvPow2Button";
		_printInvPow2Button.Text = "Inverse powers";
		_printInvPow2Button.AutoSize = true;
		_printInvPow2Button.MinimumSize = new Size(110, 32);
		_printInvPow2Button.Margin = new Padding(0, 0, 8, 0);
		_printInvPow2Button.Click += InversePowersClicked;
		_commands.Controls.Add(_printInvPow2Button);

		_perfectMerkleButton.Name = "_perfectMerkleButton";
		_perfectMerkleButton.Text = "Compare roots";
		_perfectMerkleButton.AutoSize = true;
		_perfectMerkleButton.MinimumSize = new Size(110, 32);
		_perfectMerkleButton.Margin = new Padding(0, 0, 8, 0);
		_perfectMerkleButton.Click += CompareClicked;
		_commands.Controls.Add(_perfectMerkleButton);

		_treePerfTestsButton.Name = "_treePerfTestsButton";
		_treePerfTestsButton.Text = "Compare append batches";
		_treePerfTestsButton.AutoSize = true;
		_treePerfTestsButton.MinimumSize = new Size(110, 32);
		_treePerfTestsButton.Margin = new Padding(0, 0, 8, 0);
		_treePerfTestsButton.Click += AppendClicked;
		_commands.Controls.Add(_treePerfTestsButton);

		_cancelButton.Name = "_cancelButton";
		_cancelButton.Text = "Cancel";
		_cancelButton.AutoSize = true;
		_cancelButton.MinimumSize = new Size(110, 32);
		_cancelButton.Margin = new Padding(0, 0, 8, 0);
		_cancelButton.Click += CancelClicked;
		_commands.Controls.Add(_cancelButton);
		_cancelButton.Enabled = false;

		_clearButton.Name = "_clearButton";
		_clearButton.Text = "Clear results";
		_clearButton.AutoSize = true;
		_clearButton.MinimumSize = new Size(110, 32);
		_clearButton.Margin = new Padding(0, 0, 8, 0);
		_clearButton.Click += ClearClicked;
		_commands.Controls.Add(_clearButton);

		_outputTextBox.Name = "_outputTextBox";
		_outputTextBox.Dock = DockStyle.Fill;
		_outputTextBox.Multiline = true;
		_outputTextBox.ReadOnly = true;
		_outputTextBox.ScrollBars = ScrollBars.Both;
		_outputTextBox.WordWrap = false;
		_outputTextBox.BackColor = SystemColors.Window;
		_outputTextBox.Font = new Font("Consolas", 9F);
		_outputTextBox.Margin = new Padding(0, 0, 0, 10);
		_status.Name = "showcaseStatus";
		_status.AutoSize = true;
		_status.Dock = DockStyle.Fill;
		_status.Text = "Ready. Choose an example to see its results.";

		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		Font = SystemFonts.MessageBoxFont;
		BackColor = SystemColors.Window;
		Name = "MerkleTreeTestScreen";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		((ISupportInitialize)_leafCount).EndInit();
		_commands.ResumeLayout(false);
		_commands.PerformLayout();
		_inputs.ResumeLayout(false);
		_inputs.PerformLayout();
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}

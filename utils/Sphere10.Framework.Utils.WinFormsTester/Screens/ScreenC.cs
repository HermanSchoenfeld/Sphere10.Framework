// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Demonstrates independently populated native grids in expandable task-pane groups.</summary>
public partial class ScreenC : ApplicationScreen {
	/// <summary>Creates the real Designer layout; sample data waits for first native load.</summary>
	public ScreenC() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	// The three distinct data shapes generate their native columns through ordinary data binding.
	protected override void InitializeUIPrimingData() {
		base.InitializeUIPrimingData();
		_dataGridView1.DataSource = new[] {
			new { Milestone = "Requirements", Owner = "Product", State = "Complete" },
			new { Milestone = "Implementation", Owner = "Engineering", State = "In progress" },
			new { Milestone = "Acceptance", Owner = "Review", State = "Planned" }
		};
		_dataGridView2.DataSource = new[] {
			new { Resource = "Build agent", Allocation = "Continuous integration", Available = true },
			new { Resource = "Test station", Allocation = "Native UI verification", Available = true },
			new { Resource = "Review environment", Allocation = "Acceptance", Available = false }
		};
		_dataGridView3.DataSource = new[] {
			new { Item = "Data model", Priority = "High", Reviewer = "Architecture" },
			new { Item = "User flow", Priority = "Normal", Reviewer = "Product" },
			new { Item = "Documentation", Priority = "Normal", Reviewer = "Engineering" }
		};
	}
}

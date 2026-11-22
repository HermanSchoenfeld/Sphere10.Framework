// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ControlLayoutScopeTests {

	[Test]
	public void ALayoutScopeLaysOutItsTreeOnceWhenItEnds() {
		using var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-19000, -19000), ShowInTaskbar = false };
		var table = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, RowCount = 4, Dock = DockStyle.Top };
		var labels = Enumerable.Range(0, 4).Select(index => new Label { AutoSize = true, Text = "Label " + index }).ToArray();
		foreach (var label in labels)
			table.Controls.Add(label);
		form.Controls.Add(table);
		form.Show();
		System.Windows.Forms.Application.DoEvents();
		var layouts = 0;
		table.Layout += (_, _) => layouts++;
		foreach (var label in labels)
			label.Text += " changed once more without a scope";
		Assert.That(layouts, Is.GreaterThanOrEqualTo(labels.Length), "Without a scope each label lays out its table.");
		layouts = 0;
		using (table.EnterLayoutScope()) {
			foreach (var label in labels)
				label.Text = "A much longer caption than before, " + label.Text;
			Assert.That(layouts, Is.Zero, "Layout waits for the scope.");
		}
		Assert.Multiple(() => {
			Assert.That(layouts, Is.EqualTo(1), "The table lays out once, with its labels' final sizes.");
			foreach (var label in labels)
				Assert.That(label.Width, Is.EqualTo(label.PreferredWidth));
		});
	}
}

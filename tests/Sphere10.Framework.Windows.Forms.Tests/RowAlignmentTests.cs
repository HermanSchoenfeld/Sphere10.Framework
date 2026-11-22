// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Covers keeping buttons as tall as the box beside them and labels centred on it, at any display scale.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class RowAlignmentTests {

	[Test]
	public void AFixedSizeButtonKeepsItsWidthWhileTakingTheBoxHeight() {
		using var form = new Form();
		var box = new TextBox { Location = new Point(0, 0), Width = 200 };
		var button = new Button { Text = "...", Location = new Point(205, 0), Size = new Size(27, 20) };
		form.Controls.AddRange(new Control[] { box, button });
		button.KeepHeightOf(box);
		Assert.That(button.Width, Is.EqualTo(27), "A browse button without auto-sizing was cut to nothing (21 November 2026).");
		Assert.That(button.Height, Is.EqualTo(box.Height));
	}

	[Test]
	public void AnAutoSizedButtonKeepsTheBoxHeightAfterRescaling() {
		using var form = new Form { AutoScaleMode = AutoScaleMode.Font };
		var box = new TextBox { Location = new Point(0, 0), Width = 200 };
		var button = new Button { Text = "Find File Blockers", Location = new Point(205, 0), AutoSize = true };
		form.Controls.AddRange(new Control[] { box, button });
		button.KeepHeightOf(box);
		form.Font = new Font(form.Font.FontFamily, form.Font.Size * 1.5F);
		Assert.That(button.Height, Is.EqualTo(box.Height));
		Assert.That(button.Width, Is.GreaterThanOrEqualTo(TextRenderer.MeasureText(button.Text, button.Font).Width), "The width still fits the text.");
	}

	[Test]
	public void ALabelStaysCentredOnTheBoxItNames() {
		using var form = new Form();
		var box = new TextBox { Location = new Point(60, 10), Width = 200 };
		var label = new Label { Text = "Host", Location = new Point(0, 0), AutoSize = true };
		form.Controls.AddRange(new Control[] { label, box });
		label.KeepCentredOn(box);
		box.Top = 40;
		Assert.That(label.Top + label.Height / 2, Is.EqualTo(box.Top + box.Height / 2).Within(1));
	}

}

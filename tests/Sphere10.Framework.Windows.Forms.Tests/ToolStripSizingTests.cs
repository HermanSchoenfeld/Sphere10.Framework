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

/// <summary>Covers how tool strip items size themselves so their text fits at any display scale.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ToolStripSizingTests {

	[Test]
	public void AChoiceSelectorFitsItsWidestChoiceSoTheToolbarKeepsItsLayout() {
		using var toolbar = new ToolStrip();
		var selector = new ToolStripChoiceSelector();
		toolbar.Items.Add(selector);
		selector.SetItems(new object[] { "1 sec", "10 sec", "A Much Longer Interval" });
		selector.SelectedIndex = 0;
		var shortSelected = selector.GetPreferredSize(Size.Empty);
		selector.SelectedIndex = 2;
		var longSelected = selector.GetPreferredSize(Size.Empty);
		Assert.That(shortSelected.Width, Is.EqualTo(longSelected.Width), "Selecting another choice must not resize the selector.");
		Assert.That(shortSelected.Width, Is.GreaterThanOrEqualTo(TextRenderer.MeasureText("A Much Longer Interval", selector.Font).Width));
		Assert.That(shortSelected.Height, Is.GreaterThanOrEqualTo(selector.Font.Height), "The height follows the font, so text is not cut at the bottom.");
	}

	[Test]
	public void AMaximumWidthCapsAnUnboundedListOfChoices() {
		using var toolbar = new ToolStrip();
		var selector = new ToolStripChoiceSelector { MaximumWidth = 120 };
		toolbar.Items.Add(selector);
		selector.SetItems(new object[] { "Application", "Microsoft-Windows-Kernel-Power/Thermal-Operational-With-A-Very-Long-Channel-Name" });
		selector.SelectedIndex = 0;
		Assert.That(selector.GetPreferredSize(Size.Empty).Width, Is.EqualTo(toolbar.LogicalToDeviceUnits(120)));
	}

	[Test]
	public void FixedSizesAreLeftAloneAtOneHundredPercent() {
		using var toolbar = new ToolStrip();
		Assume.That(toolbar.DeviceDpi, Is.EqualTo(96), "This process runs at 96 DPI; scaling is checked by rendering the scaled application.");
		var fixedButton = new ToolStripDropDownButton("2000") { AutoSize = false, Size = new Size(65, 22) };
		var textBox = new ToolStripTextBox { Width = 280 };
		var menu = new ToolStripDropDownButton("Query");
		var nested = new ToolStripTextBox { AutoSize = false, Size = new Size(200, 23) };
		menu.DropDownItems.Add(nested);
		toolbar.Items.AddRange(new ToolStripItem[] { fixedButton, textBox, menu });
		toolbar.ScaleFixedItemSizes();
		Assert.That((fixedButton.Size, textBox.Width, nested.Size), Is.EqualTo((new Size(65, 22), 280, new Size(200, 23))));
	}
}

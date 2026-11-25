// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Checks that the block selector takes its shape, colour style, metrics and colours from the explorer bar theme and its overrides.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationBarThemeTests {
	private const int ColorTolerance = 24;
	private static readonly string[] BlockNames = ["Workspace", "Notes", "Controls", "Data And Collections"];
	private bool _startedFramework;

	[OneTimeSetUp]
	public void StartFramework() {
		_startedFramework = !Sphere10Framework.Instance.IsStarted;
		if (_startedFramework)
			Sphere10Framework.Instance
				.Build()
				.Start();
	}

	[OneTimeTearDown]
	public void StopFramework() {
		if (_startedFramework)
			Sphere10Framework.Instance.EndFramework();
	}

	[Test]
	public void LunaThemeUsesFullWidthButtonsInExpandoHeaderColours() {
		using var theme = ExplorerBarInfo.Default;
		Assert.That(theme.BlockSelector.Shape, Is.EqualTo(BlockSelectorShape.Buttons));
		Assert.That(theme.BlockSelector.ColorStyle, Is.EqualTo(BlockSelectorColorStyle.ExpandoHeader));
		Assert.That(theme.BlockSelector.ButtonHeight, Is.Null, "Metrics come from the shape unless the theme overrides them.");
		Assert.That(theme.BlockSelector.SelectedGradientEndColor.IsEmpty, Is.True, "Colours come from the colour style unless the theme overrides them.");
	}

	[Test]
	public void BlockSelectorSettingsRoundTripThroughThemeXml() {
		using var theme = ExplorerBarInfo.Default;
		theme.BlockSelector.Shape = BlockSelectorShape.Tags;
		theme.BlockSelector.ColorStyle = BlockSelectorColorStyle.SoftOffice;
		theme.BlockSelector.ButtonHeight = 40;
		theme.BlockSelector.ButtonSpacing = 0;
		theme.BlockSelector.GlossAlpha = 90;
		theme.BlockSelector.SelectedGradientEndColor = Color.FromArgb(200, 30, 40);
		var surrogate = new ExplorerBarInfoSurrogate();
		surrogate.Load(theme);
		var xml = Tools.Xml.WriteToString(surrogate);
		Assert.That(xml, Does.Not.Contain("<CornerRadius"), "An unset metric is omitted, so the shape's default applies when read back.");
		using var loaded = Tools.Xml.ReadFromString<ExplorerBarInfoSurrogate>(xml).Save();
		Assert.That(loaded.BlockSelector.Shape, Is.EqualTo(BlockSelectorShape.Tags));
		Assert.That(loaded.BlockSelector.ColorStyle, Is.EqualTo(BlockSelectorColorStyle.SoftOffice));
		Assert.That(loaded.BlockSelector.ButtonHeight, Is.EqualTo(40));
		Assert.That(loaded.BlockSelector.ButtonSpacing, Is.EqualTo(0));
		Assert.That(loaded.BlockSelector.CornerRadius, Is.Null);
		Assert.That(loaded.BlockSelector.GlossAlpha, Is.EqualTo(90));
		Assert.That(loaded.BlockSelector.SelectedGradientEndColor.ToArgb(), Is.EqualTo(Color.FromArgb(200, 30, 40).ToArgb()));
		Assert.That(loaded.BlockSelector.NormalGradientStartColor.IsEmpty, Is.True);
	}

	[Test]
	public void ThemeSavedWithoutBlockSelectorSectionKeepsTheDefaults() {
		using var theme = ExplorerBarInfo.Default;
		var surrogate = new ExplorerBarInfoSurrogate();
		surrogate.Load(theme);
		surrogate.BlockSelectorInfoSurrogate = null;
		var xml = Tools.Xml.WriteToString(surrogate);
		Assert.That(xml, Does.Not.Contain(nameof(BlockSelectorInfoSurrogate)));
		using var loaded = Tools.Xml.ReadFromString<ExplorerBarInfoSurrogate>(xml).Save();
		Assert.That(loaded.BlockSelector.Shape, Is.EqualTo(BlockSelectorShape.Buttons));
		Assert.That(loaded.BlockSelector.ColorStyle, Is.EqualTo(BlockSelectorColorStyle.ExpandoHeader));
	}

	[Test]
	public void ButtonsSpanTheBarEdgeToEdge() {
		using var form = CreateForm();
		var bar = Bar(form);
		var buttons = Buttons(bar);
		Assert.That(buttons, Has.Length.EqualTo(BlockNames.Length));
		var height = bar.LogicalToDeviceUnits(32);
		Assert.That(bar.ButtonHeight, Is.EqualTo(height));
		for (var index = 0; index < buttons.Length; index++) {
			Assert.That(buttons[index].Bounds, Is.EqualTo(new Rectangle(0, index * height, buttons[index].Parent.ClientSize.Width, height)), BlockNames[index]);
		}
		Capture(bar, "block-selector-buttons-expando-header.png");
	}

	[Test]
	public void TagsLineUpWithTheExpandos() {
		using var form = CreateForm();
		var bar = Bar(form);
		var pane = (TaskPane)bar.ApplicationBarControl;
		var theme = ExplorerBarInfo.Default;
		theme.BlockSelector.Shape = BlockSelectorShape.Tags;
		pane.UseCustomTheme(theme);
		var buttons = Buttons(bar);
		var height = bar.LogicalToDeviceUnits(28);
		var pitch = height + bar.LogicalToDeviceUnits(4);
		for (var index = 0; index < buttons.Length; index++) {
			var button = buttons[index];
			Assert.That(button.Height, Is.EqualTo(height));
			Assert.That(button.Left, Is.EqualTo(bar.LogicalToDeviceUnits(pane.Padding.Left)), "Tags line up with the expandos' left edge.");
			Assert.That(button.Parent.ClientSize.Width - button.Right, Is.EqualTo(bar.LogicalToDeviceUnits(pane.Padding.Right)), "Tags line up with the expandos' right edge.");
			Assert.That(button.Top, Is.EqualTo(bar.LogicalToDeviceUnits(pane.Padding.Top) + index * pitch));
		}
		using var bitmap = Render(buttons[1]);
		Assert.That(bitmap.GetPixel(0, 0).ToArgb(), Is.EqualTo(pane.GradientEndColor.ToArgb()), "A tag's rounded corner shows the task pane's gradient end colour.");
		Capture(bar, "block-selector-tags-expando-header.png");
	}

	[Test]
	public void ExpandoHeaderStyleTakesTheThemesHeaderColours() {
		using var form = CreateForm();
		var bar = Bar(form);
		var buttons = Buttons(bar);
		Assert.That(buttons[0].Pressed, Is.True);
		// Luna's special header image runs from 0,73,181 to 41,93,206; a selected button puts the lighter colour on top.
		using (var selected = Render(buttons[0])) {
			AssertNear(selected.GetPixel(selected.Width - 6, 3), Color.FromArgb(41, 93, 206));
			AssertNear(selected.GetPixel(selected.Width - 6, selected.Height - 2), Color.FromArgb(0, 73, 181));
		}
		// Luna's normal header image runs from white to 206,215,247.
		using (var normal = Render(buttons[1]))
			AssertNear(normal.GetPixel(normal.Width - 6, normal.Height - 2), Color.FromArgb(206, 215, 247));

		var pane = (TaskPane)bar.ApplicationBarControl;
		var classic = ExplorerBarInfo.Default;
		classic.UseClassicTheme();
		pane.UseCustomTheme(classic);
		using (var selected = Render(buttons[0]))
			AssertNear(selected.GetPixel(selected.Width - 6, selected.Height - 2), SystemColors.Highlight);
		using (var normal = Render(buttons[1]))
			AssertNear(normal.GetPixel(normal.Width - 6, normal.Height - 2), SystemColors.Control);
		Capture(bar, "block-selector-buttons-classic.png");
	}

	[Test]
	public void BarOverridesTakePrecedenceOverTheTheme() {
		using var form = CreateForm();
		var bar = Bar(form);
		var buttons = Buttons(bar);
		form.BlockSelectorColorStyle = BlockSelectorColorStyle.SoftOffice;
		form.BlockSelectorShape = BlockSelectorShape.Tags;
		Assert.That(bar.ColorStyle, Is.EqualTo(BlockSelectorColorStyle.SoftOffice));
		Assert.That(bar.Shape, Is.EqualTo(BlockSelectorShape.Tags));
		Assert.That(buttons[0].Height, Is.EqualTo(bar.LogicalToDeviceUnits(28)));
		using (var selected = Render(buttons[0]))
			AssertNear(selected.GetPixel(selected.Width - 6, selected.Height / 2), Color.FromArgb(249, 190, 92), "Soft Office tags end in amber on the right.");
		Capture(bar, "block-selector-tags-soft-office.png");

		form.BlockSelectorShape = null;
		Assert.That(buttons[0].Height, Is.EqualTo(bar.LogicalToDeviceUnits(32)), "Clearing the shape override returns to the theme's shape.");
		using (var selected = Render(buttons[0]))
			AssertNear(selected.GetPixel(selected.Width - 6, selected.Height - 2), Color.FromArgb(249, 190, 92), "Soft Office buttons end in amber at the bottom.");
		Capture(bar, "block-selector-buttons-soft-office.png");

		form.BlockSelectorColorStyle = null;
		using (var selected = Render(buttons[0]))
			AssertNear(selected.GetPixel(selected.Width - 6, selected.Height - 2), Color.FromArgb(0, 73, 181));
	}

	[Test]
	public void ExplicitThemeColoursOverrideTheColourStyle() {
		using var form = CreateForm();
		var bar = Bar(form);
		var pane = (TaskPane)bar.ApplicationBarControl;
		var theme = ExplorerBarInfo.Default;
		theme.BlockSelector.ColorStyle = BlockSelectorColorStyle.SoftOffice;
		theme.BlockSelector.SelectedGradientEndColor = Color.FromArgb(200, 30, 40);
		pane.UseCustomTheme(theme);
		using var selected = Render(Buttons(bar)[0]);
		AssertNear(selected.GetPixel(selected.Width - 6, selected.Height - 2), Color.FromArgb(200, 30, 40));
	}

	[Test]
	public void NavigationThemeStylesEveryBlocksPane() {
		using var form = CreateForm();
		var bar = Bar(form);
		using var theme = ExplorerBarInfo.Default;
		theme.TaskPane.GradientEndColor = Color.Teal;
		theme.BlockSelector.Shape = BlockSelectorShape.Tags;
		var surrogate = new ExplorerBarInfoSurrogate();
		surrogate.Load(theme);
		form.NavigationTheme = surrogate;
		var tagHeight = bar.LogicalToDeviceUnits(28);
		Assert.That(((TaskPane)bar.ApplicationBarControl).GradientEndColor.ToArgb(), Is.EqualTo(Color.Teal.ToArgb()));
		Assert.That(bar.ButtonHeight, Is.EqualTo(tagHeight), "The navigation theme's tag shape reaches the block selector.");
		form.SelectedBlock = form.Blocks[2];
		System.Windows.Forms.Application.DoEvents();
		Assert.That(((TaskPane)bar.ApplicationBarControl).GradientEndColor.ToArgb(), Is.EqualTo(Color.Teal.ToArgb()), "A block selected later gets the navigation theme too.");
		Assert.That(bar.ButtonHeight, Is.EqualTo(tagHeight));
		form.NavigationTheme = null;
		Assert.That(bar.ButtonHeight, Is.EqualTo(bar.LogicalToDeviceUnits(32)), "Clearing the navigation theme returns to the default theme's buttons.");
	}

	[Test]
	public void CustomPaneSettingsRecolourTheButtonStrip() {
		using var form = CreateForm();
		var bar = Bar(form);
		var pane = (TaskPane)bar.ApplicationBarControl;
		var buttons = Buttons(bar);
		pane.CustomSettings.GradientEndColor = Color.Teal;
		Assert.That(buttons[0].Parent.BackColor.ToArgb(), Is.EqualTo(Color.Teal.ToArgb()));
		pane.ResetCustomSettings();
		Assert.That(buttons[0].Parent.BackColor.ToArgb(), Is.EqualTo(pane.GradientEndColor.ToArgb()));
	}

	[Test]
	public void BlockButtonsArePushButtons() {
		using var form = CreateForm();
		var button = Buttons(Bar(form))[1];
		Assert.That(button.AccessibilityObject.Role, Is.EqualTo(AccessibleRole.PushButton));
		button.PerformClick();
		Assert.That(button.Pressed, Is.True, "Activating a block button by keyboard or automation selects its block.");
		Assert.That(form.SelectedBlock.Name, Is.EqualTo(BlockNames[1]));
	}

	private static BlockMainForm CreateForm() {
		var form = new BlockMainForm { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(700, 640) };
		foreach (var name in BlockNames)
			form.RegisterBlock(new ApplicationBlock { Name = name, Image32x32 = CreateIcon() });
		form.Show();
		System.Windows.Forms.Application.DoEvents();
		return form;
	}

	private static Bitmap CreateIcon() {
		var icon = new Bitmap(32, 32);
		using var graphics = Graphics.FromImage(icon);
		graphics.Clear(Color.Transparent);
		graphics.FillEllipse(Brushes.SteelBlue, 2, 2, 28, 28);
		return icon;
	}

	private static ApplicationBar Bar(BlockMainForm form) => form.Controls.OfType<SplitContainer>().Single().Panel1.Controls.OfType<ApplicationBar>().Single();

	private static ApplicationBarButton[] Buttons(ApplicationBar bar) => bar.Items.Select(item => item.Button).ToArray();

	private static void AssertNear(Color actual, Color expected, string message = null) {
		var distance = Math.Abs(actual.R - expected.R) + Math.Abs(actual.G - expected.G) + Math.Abs(actual.B - expected.B);
		Assert.That(distance, Is.LessThanOrEqualTo(ColorTolerance), $"{message} Expected about {expected}, got {actual}.");
	}

	private static Bitmap Render(Control control) {
		var bitmap = new Bitmap(control.Width, control.Height, PixelFormat.Format32bppRgb);
		control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, control.Size));
		return bitmap;
	}

	private static void Capture(Control control, string filename) {
		var directory = TestContext.Parameters.Get("UiCaptureDirectory", string.Empty);
		if (string.IsNullOrWhiteSpace(directory))
			return;
		Directory.CreateDirectory(directory);
		using var bitmap = Render(control);
		bitmap.Save(Path.Combine(directory, filename), ImageFormat.Png);
	}
}

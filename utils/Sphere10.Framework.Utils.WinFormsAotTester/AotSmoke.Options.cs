// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;
using InspectorPreferences = Sphere10.Framework.Utils.WinFormsTester.Screens.ScreenHostingDesignTestScreen.ScreenHostingPreferences;

namespace Sphere10.Framework.Utils.WinFormsTester;

internal static partial class AotSmoke {
	private static async Task VerifyScreenOptions(BlockMainForm main) {
		var definitions = main.ScreenHost.Blocks.SelectMany(block => block.ScreenDefinitions).ToArray();
		var menuFree = definitions.Single(item => item.StableKey == "menu-free-inspector");
		var cpu = definitions.Single(item => item.StableKey == "cpu-inspector");
		var gpu = definitions.Single(item => item.StableKey == "gpu-inspector");
		var preferences = (InspectorPreferences)menuFree.Settings;
		var cpuBefore = SettingsObjectJsonSerializer.SerializeValues(cpu.Settings);
		var gpuBefore = SettingsObjectJsonSerializer.SerializeValues(gpu.Settings);
		var services = Sphere10Framework.Instance.ServiceProvider;
		var application = services.GetRequiredService<Sphere10.Framework.Windows.Forms.IApplication>();
		using var dialog = new ApplicationSettingsDialog(application, services, selectedOwner: menuFree);
		dialog.Show(main);
		await Task.Yield();
		dialog.SelectOwner(menuFree);
		await Task.Yield();
		var status = Descendants(dialog).OfType<RichTextBox>().Single(control => control.Name == "_status");
		Record("OptionsStatus=" + status.Text);
		Record("OptionsRawProperties=" + TypeDescriptor.GetProperties(preferences).Count);
		Record("OptionsBrowsableProperties=" + TypeDescriptor.GetProperties(preferences, new Attribute[] { BrowsableAttribute.Yes }).Count);
		var grid = Descendants(dialog).OfType<PropertyGrid>().Single();
		Record("OptionsSelectedTab=" + grid.SelectedTab?.GetType().FullName);
		Guard.Ensure(grid.SelectedObject != null, "Options has no selected object: " + status.Text);
		var descriptors = TypeDescriptor.GetProperties(grid.SelectedObject);
		Record("OptionsWrappedProperties=" + descriptors.Count);
		var root = grid.SelectedGridItem;
		while (root?.Parent != null)
			root = root.Parent;
		Record("OptionsRootItems=" + (root?.GridItems.Count ?? 0));
		CaptureControl(dialog, "options-before.png");
		Guard.Ensure(root != null, "Options property grid is empty: " + status.Text);
		var items = GridDescendants(root).ToArray();
		Guard.Ensure(items.Any(item => item.PropertyDescriptor?.Name == nameof(InspectorPreferences.Heading)));
		var appearance = items.Single(item => item.PropertyDescriptor?.Name == nameof(InspectorPreferences.Appearance));
		Guard.Ensure(appearance.Expandable, "Presentation values are not expandable.");
		appearance.Expanded = true;
		var children = GridDescendants(appearance).ToArray();
		Guard.Ensure(children.Any(item => item.PropertyDescriptor?.Name == "Prefix"));
		Guard.Ensure(children.Any(item => item.PropertyDescriptor?.Name == "HighlightHeading"));
		var choices = descriptors[nameof(InspectorPreferences.Details)].Converter.GetStandardValues();
		Guard.Ensure(choices != null && choices.Count == 2);
		var heading = descriptors[nameof(InspectorPreferences.Heading)];
		Guard.Ensure(heading != null && !heading.IsReadOnly);
		heading.SetValue(grid.SelectedObject, "AOT options probe");
		Guard.Ensure(preferences.Heading == "AOT options probe");
		Guard.Ensure(await dialog.ApplySelectedAsync(), "Options Apply failed: " + status.Text);
		Guard.Ensure(SettingsObjectJsonSerializer.SerializeValues(preferences).Contains("AOT options probe"));
		Guard.Ensure(SettingsObjectJsonSerializer.SerializeValues(cpu.Settings) == cpuBefore);
		Guard.Ensure(SettingsObjectJsonSerializer.SerializeValues(gpu.Settings) == gpuBefore);
		dialog.ResetSelectedDefaults();
		Guard.Ensure(preferences.Heading == "Inspector summary");
		Guard.Ensure(await dialog.ApplySelectedAsync(), "Options Reset Apply failed: " + status.Text);
		Guard.Ensure(SettingsObjectJsonSerializer.SerializeValues(cpu.Settings) == cpuBefore);
		Guard.Ensure(SettingsObjectJsonSerializer.SerializeValues(gpu.Settings) == gpuBefore);
		CaptureControl(dialog, "options-after.png");
		Record("OptionsVerified");
		dialog.DialogResult = DialogResult.OK;
		dialog.Close();
	}

	private static IEnumerable<Control> Descendants(Control control) {
		yield return control;
		foreach (Control child in control.Controls)
			foreach (var descendant in Descendants(child))
				yield return descendant;
	}

	private static IEnumerable<GridItem> GridDescendants(GridItem item) {
		yield return item;
		foreach (GridItem child in item.GridItems)
			foreach (var descendant in GridDescendants(child))
				yield return descendant;
	}
}

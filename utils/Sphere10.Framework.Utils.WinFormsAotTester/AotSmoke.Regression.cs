// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Windows.Forms.AppointmentBook;

namespace Sphere10.Framework.Utils.WinFormsTester;

internal static partial class AotSmoke {
	private static int _constructedNavigationWidth;

	public static void ObserveNavigationConstruction(BlockMainForm main) {
		if (!IsActive)
			return;
		_constructedNavigationWidth = main.NavigationPaneWidth;
		Record("NavigationWidthAtConstruction=" + _constructedNavigationWidth);
	}

	private static async Task VerifyScreens(BlockMainForm main) {
		if (_case is "all" or "navigation")
			VerifyNavigation(main);
		main.Size = new Size(1280, 900);
		await Task.Yield();
		if (_case is "all" or "enums")
			VerifyEnumControls();
		if (_case is "all" or "appointment")
			await VerifyAppointmentBook(main);
		if (_case is "all" or "visualgrid")
			await VerifyVisualGrid(main);
		if (_case is "all" or "options")
			await VerifyScreenOptions(main);
		if (_case is "all" or "sourcegrid")
			await VerifySourceGrid(main);
		if (_case is "all" or "charting")
			await VerifyDataVisualization(main);
		if (_case is "all" or "objectspace")
			await VerifyObjectSpace(main);
		if (_case is "all" or "crudeditor")
			await VerifyCrudEditor(main);
	}

	private static void VerifyNavigation(BlockMainForm main) {
		var split = main.Controls.OfType<SplitContainer>().Single();
		Record("NavigationPreferredWidth=" + main.NavigationPaneWidth);
		Record("NavigationActualWidth=" + split.SplitterDistance);
		Record("NavigationPanelWidth=" + split.Panel1.Width);
		Record("NavigationSplitBounds=" + split.Bounds);
		Record("NavigationSplitDock=" + split.Dock);
		var properties = TypeDescriptor.GetProperties(split);
		Record("NavigationPropertyCount=" + properties.Count);
		Record("NavigationSplitterPropertyReadOnly=" + properties[nameof(SplitContainer.SplitterDistance)]?.IsReadOnly);
		Guard.Ensure(_constructedNavigationWidth == 320, "Resource-based navigation defaults were not applied.");
		Guard.Ensure(split.Dock == DockStyle.Fill && split.Width > 500);
		Guard.Ensure(split.SplitterDistance == main.NavigationPaneWidth && split.Panel1.Width >= 200);
		CaptureControl(main, "navigation.png");
		Record("NavigationVerified");
	}

	private static void VerifyEnumControls() {
		using var combo = new EnumComboBox();
		Guard.Ensure(combo.Items.Count == 0, "The empty default enum must populate without a typed enum array.");
		combo.EnumType = typeof(DayOfWeek);
		Guard.Ensure(combo.Items.Count == 7);
		combo.SelectedEnum = DayOfWeek.Monday;
		Guard.Ensure(Equals(combo.SelectedEnum, DayOfWeek.Monday) && combo.Text == "Monday");
		combo.IgnoreEnums = new object[] { DayOfWeek.Sunday };
		combo.Filter = value => !Equals(value, DayOfWeek.Saturday);
		combo.EnumType = typeof(DayOfWeek);
		Guard.Ensure(combo.Items.Count == 5);
		using var flags = new FlagsCheckedListBox { EnumType = typeof(DataSourceCapabilities) };
		Guard.Ensure(flags.Items.Count == 11 && flags.GetItemText(flags.Items[0]) == "CanCreate");
		flags.SelectedEnum = DataSourceCapabilities.Default;
		Guard.Ensure(Equals(flags.SelectedEnum, DataSourceCapabilities.Default));
		Record("EnumControlsVerified");
	}

	private static async Task<TScreen> OpenRegistered<TScreen>(BlockMainForm main) where TScreen : ApplicationScreen {
		var block = main.ScreenHost.Blocks.Single(item => item.ScreenDefinitions.Any(definition => definition.ScreenType == typeof(TScreen)));
		var definition = block.ScreenDefinitions.Single(item => item.ScreenType == typeof(TScreen));
		return (TScreen)await main.ScreenHost.OpenScreenAsync(block, definition);
	}

	private static async Task VerifyAppointmentBook(BlockMainForm main) {
		Record("AppointmentActivate");
		var screen = await OpenRegistered<AppointmentBookScreen>(main);
		await Task.Yield();
		var controls = Descendants(screen).ToArray();
		var time = controls.OfType<EnumComboBox>().Single(item => item.Name == "_timeViewSelector");
		var filter = controls.OfType<EnumComboBox>().Single(item => item.Name == "_columnFilterComboBox");
		var book = controls.OfType<Sphere10.Framework.Windows.Forms.AppointmentBook.AppointmentBook>().Single(item => item.Name == "AppointmentBook");
		var unallocated = controls.OfType<UnallocatedBook>().Single();
		Guard.Ensure(time.Items.Count == 4 && filter.Items.Count == 3);
		Guard.Ensure(book.DataSource != null && ReferenceEquals(book.DataSource, unallocated.DataSource));
		Guard.Ensure(book.DataSource.GetColumns().Count() == 10);
		var unallocatedCount = book.DataSource.GetAppointments(new UnallocatedColumn()).Count();
		Guard.Ensure(unallocatedCount >= DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
		time.SelectedEnum = TimePeriodType.DailyHalfHourly;
		await Task.Yield();
		Guard.Ensure(time.Text == "By Half Hour");
		Guard.Ensure(book.TimePeriodType == TimePeriodType.DailyHalfHourly && unallocated.TimePeriodType == TimePeriodType.DailyHalfHourly);
		filter.SelectedEnum = AppointmentBookViewModelFilter.All;
		Guard.Ensure(book.ColumnFilter == AppointmentBookViewModelFilter.All);
		screen.Refresh();
		CaptureControl(screen, "appointment.png");
		Record("AppointmentUnallocatedCount=" + unallocatedCount);
		Record("AppointmentVerified");
	}

	private static async Task VerifyVisualGrid(BlockMainForm main) {
		Record("VisualGridActivate");
		var screen = await OpenRegistered<VisualGridTestScreen>(main);
		await Task.Yield();
		await screen.Initialization;
		var controls = Descendants(screen).ToArray();
		var records = controls.OfType<VisualGrid>().Single(item => item.Name == "_crudGrid");
		var live = controls.OfType<VisualGrid>().Single(item => item.Name == "_liveGrid");
		await records.RefreshCompletion;
		await live.RefreshCompletion;
		var flags = controls.OfType<FlagsCheckedListBox>().Single(item => item.Name == "_flagsCheckedListBox");
		Guard.Ensure(flags.Items.Count == 11 && Equals(flags.SelectedEnum, DataSourceCapabilities.Default));
		Guard.Ensure(records.GridBindings.Count() == 12 && live.GridBindings.Count() == 7);
		Guard.Ensure(records.VisibleEntities.Any() && live.VisibleEntities.Any());
		screen.Refresh();
		CaptureControl(screen, "visualgrid.png");
		Record("VisualGridRows=" + records.VisibleEntities.Count() + "/" + live.VisibleEntities.Count());
		Record("VisualGridVerified");
	}
}

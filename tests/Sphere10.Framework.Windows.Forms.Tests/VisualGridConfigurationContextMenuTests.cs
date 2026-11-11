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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows.Forms.SourceGrid;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Exercises native header-menu attachment, targeting and component ownership without desktop pointer changes.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class VisualGridConfigurationContextMenuTests {

	[Test]
	public void DesignerAttachmentKeepsTheSupplyingContainerAsTheMenuOwner() {
		using var container = new Container();
		var menu = new VisualGridConfigurationContextMenu(container);
		using var grid = new VisualGrid { HeaderContextMenuStrip = menu };
		var property = TypeDescriptor.GetProperties(grid)[nameof(VisualGrid.HeaderContextMenuStrip)];
		Assert.That(property.PropertyType, Is.EqualTo(typeof(ContextMenuStrip)));
		Assert.That(property.SerializationVisibility, Is.EqualTo(DesignerSerializationVisibility.Visible));
		Assert.That(((DefaultValueAttribute)property.Attributes[typeof(DefaultValueAttribute)]).Value, Is.Null);
		Assert.That(container.Components.Cast<IComponent>(), Does.Contain(menu));
		Assert.That(menu.Items.Cast<ToolStripItem>().Select(item => item.Name), Is.EqualTo(new[] {
			"_saveConfigurationMenuItem", "_restoreDefaultsMenuItem"
		}));
		Assert.That(menu.Items.Cast<ToolStripItem>().Select(item => item.Text), Is.EqualTo(new[] {
			"Save configuration", "Restore defaults"
		}));
		var items = TypeDescriptor.GetProperties(menu)[nameof(VisualGridConfigurationContextMenu.Items)];
		Assert.That(items.SerializationVisibility, Is.EqualTo(DesignerSerializationVisibility.Hidden));
		Assert.That(items.IsBrowsable, Is.False);
		Assert.That(items.ShouldSerializeValue(menu), Is.False);

		var restorePolicy = TypeDescriptor.GetProperties(menu)[nameof(VisualGridConfigurationContextMenu.DefaultsRestorePolicy)];
		Assert.That(restorePolicy.IsBrowsable, Is.True);
		Assert.That(restorePolicy.ShouldSerializeValue(menu), Is.False);
		menu.DefaultsRestorePolicy = VisualGridDefaultsRestorePolicy.RestoreAndSave;
		Assert.That(restorePolicy.ShouldSerializeValue(menu), Is.True, "The designer must retain a configured persistence policy.");

		grid.Dispose();
		Assert.That(menu.IsDisposed, Is.False, "The grid borrows the supplied native component.");
		container.Dispose();
		Assert.That(menu.IsDisposed, Is.True);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void OnlyAHeaderRightClickOpensTheConfiguredMenuAndRowsKeepTheirOwnCommands(bool rowCommands) {
		RunWithMessageLoop(async owner => {
			using var menu = new ContextMenuStrip();
			menu.Items.Add("Custom header command");
			using var grid = new VisualGrid {
				Dock = DockStyle.Fill,
				HeaderContextMenuStrip = menu,
				RightClickForContextMenu = rowCommands
			};
			owner.Controls.Add(grid);
			await BindAsync(grid, "Row");
			var native = FindNativeGrid(grid);
			var opened = 0;
			menu.Opening += (_, _) => opened++;
			native.Selection.SelectRow(2, true);
			var previousSelection = grid.SelectedEntity;

			// A header menu never changes the committed row selection or depends on the row-menu option.
			Click(native, Center(native, new Position(0, 0)), MouseButtons.Right);
			Assert.That(menu.Visible, Is.True);
			Assert.That(menu.SourceControl, Is.SameAs(grid));
			Assert.That(grid.SelectedEntity, Is.SameAs(previousSelection));
			Assert.That(opened, Is.EqualTo(1));
			menu.Close();
			Click(native, Center(native, new Position(0, 0)), MouseButtons.Left);
			Assert.That(menu.Visible, Is.False);
			Assert.That(opened, Is.EqualTo(1));
			await grid.RefreshGrid();

			// The existing body-row path still opens only its independently enabled CRUD menu.
			Click(native, Center(native, new Position(1, 0)), MouseButtons.Right);
			var rowMenu = (ContextMenuStrip)typeof(VisualGrid).GetField("_selectionContextMenuStrip", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(grid);
			Assert.That(rowMenu.Visible, Is.EqualTo(rowCommands));
			Assert.That(menu.Visible, Is.False);
			Assert.That(opened, Is.EqualTo(1));
			rowMenu.Close();

			var blank = new Point(5, native.ClientRectangle.Bottom - 4);
			Assert.That(native.PositionAtPoint(blank).IsEmpty(), Is.True);
			Click(native, blank, MouseButtons.Right);
			Assert.That(menu.Visible, Is.False);
			Assert.That(opened, Is.EqualTo(1));
		});
	}

	[Test]
	public void OneNativeMenuUsesTheHeaderOfItsCurrentSourceGrid() {
		RunWithMessageLoop(async owner => {
			using var menu = new ContextMenuStrip();
			menu.Items.Add("Shared header command");
			using var first = new VisualGrid { Dock = DockStyle.Left, Width = 400, HeaderContextMenuStrip = menu };
			using var second = new VisualGrid { Dock = DockStyle.Fill, HeaderContextMenuStrip = menu };
			owner.Controls.Add(second);
			owner.Controls.Add(first);
			await BindAsync(first, "First");
			await BindAsync(second, "Second");
			var firstNative = FindNativeGrid(first);
			var secondNative = FindNativeGrid(second);

			Click(firstNative, Center(firstNative, new Position(0, 0)), MouseButtons.Right);
			Assert.That(menu.Visible, Is.True);
			Assert.That(menu.SourceControl, Is.SameAs(first));
			menu.Close();
			Click(secondNative, Center(secondNative, new Position(0, 0)), MouseButtons.Right);
			Assert.That(menu.Visible, Is.True);
			Assert.That(menu.SourceControl, Is.SameAs(second));
			menu.Close();
		});
	}

	[Test]
	public void DefaultCommandsDoNotOpenForAnUnboundTarget() {
		RunWithMessageLoop(async owner => {
			using var menu = new VisualGridConfigurationContextMenu();
			using var grid = new VisualGrid { Dock = DockStyle.Fill, HeaderContextMenuStrip = menu };
			owner.Controls.Add(grid);
			await BindAsync(grid, "Unbound");
			var native = FindNativeGrid(grid);
			Click(native, Center(native, new Position(0, 0)), MouseButtons.Right);
			Assert.That(menu.SourceControl, Is.SameAs(grid));
			Assert.That(menu.Visible, Is.False);
			Assert.That(menu.Items.Cast<ToolStripItem>().All(item => !item.Enabled), Is.True);
		});
	}

	// Bind real native header and body cells while leaving settings ownership outside this fixture.
	private static async Task BindAsync(VisualGrid grid, string prefix) {
		grid.GridBindings = new[] {
			new VisualGridColumn<string> { ColumnName = "Value", SortName = "Value", DataType = typeof(string), PropertyValue = value => value }
		};
		await grid.SetDataSource(new ListDataSource<string>(new ExtendedList<string>(new[] { prefix + " one", prefix + " two" })),
			DataSourceCapabilities.CanRead | DataSourceCapabilities.CanSort);
		await grid.RefreshGrid();
	}

	// Native SourceGrid cells provide the same geometry used by the production hit test.
	private static Point Center(Grid grid, Position position) {
		var bounds = grid.PositionToRectangle(position);
		Assert.That(bounds.IsEmpty, Is.False);
		return new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
	}

	// Run the actual native control's event route without moving the user's desktop pointer.
	private static void Click(Grid grid, Point location, MouseButtons button) {
		var args = new MouseEventArgs(button, 1, location.X, location.Y, 0);
		foreach (var method in new[] { "OnMouseDown", "OnMouseClick", "OnMouseUp" })
			typeof(Control).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(grid, new object[] { args });
	}

	// The public control owns one embedded SourceGrid input surface.
	private static Grid FindNativeGrid(VisualGrid grid) => grid.Controls.Find("_grid", true).OfType<Grid>().Single();

	// Keep all menu opening, input, asynchronous binding and cleanup on one bounded native message loop.
	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form {
			ShowInTaskbar = false,
			StartPosition = FormStartPosition.Manual,
			Location = new Point(-20000, -20000),
			ClientSize = new Size(950, 450)
		};
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The native header menu fixture did not complete.");
			owner.Close();
		};
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			failure = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => test(owner));
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}
}

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
public class VisualGridNotificationTests {
	private bool _started;

	[OneTimeSetUp]
	public void StartFramework() {
		_started = !Sphere10Framework.Instance.IsStarted;
		if (_started)
			Sphere10Framework.Instance.Build().Start();
	}

	[OneTimeTearDown]
	public void StopFramework() {
		if (_started)
			Sphere10Framework.Instance.EndFramework();
	}

	[Test]
	public void AWorkerPublicationRefreshesTheBoundGridSilentlyAndRejectsAnOlderPreparedRead() {
		RunWithMessageLoop(async owner => {
			using var grid = NewGrid(owner);
			var source = new Source();
			source.Publish(Rows("Original"));
			grid.ConfigureDataSource(source, selected => source.Data.SingleOrDefault(row => row.Id == selected.Id));
			await grid.RefreshDataSourceAsync();
			Assert.That(source.Subscribers, Is.EqualTo(1));
			grid.SelectedEntity = grid.VisibleEntities.Cast<Row>().First();
			var native = grid.Controls.Find("_grid", true).OfType<Grid>().Single();
			var width = native.Columns[0].Width;
			source.HoldNextRead();
			await Task.Run(() => source.Publish(Rows("Obsolete")));
			await source.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(native.Enabled, Is.True, "CanNotify updates must use the unobtrusive path even when SuppressLoadingIndicator is false.");
			Assert.That(grid.VisibleEntities.Cast<Row>().All(row => row.Name.StartsWith("Original", StringComparison.Ordinal)), Is.True);
			ClickRow(native, 2);
			Assert.That(((Row)grid.SelectedEntity).Id, Is.EqualTo(2), "The real row mouse route must stay active during source-driven reads.");
			var obsoleteAccepted = false;
			grid.DataBindingComplete += _ => obsoleteAccepted |= grid.VisibleEntities.Cast<Row>().Any(row => row.Name.StartsWith("Obsolete", StringComparison.Ordinal));
			await Task.Run(() => source.Publish(Rows("Latest")));
			source.Release.TrySetResult();
			await UntilAsync(() => grid.VisibleEntities.Cast<Row>().All(row => row.Name.StartsWith("Latest", StringComparison.Ordinal)));
			Assert.That(obsoleteAccepted, Is.False);
			Assert.That(((Row)grid.SelectedEntity).Id, Is.EqualTo(2));
			Assert.That(native.Columns[0].Width, Is.EqualTo(width));
			Assert.That(grid.Capabilities.HasFlag(DataSourceCapabilities.CanNotify), Is.True);
		});
	}

	[Test]
	public void AnExplicitPresentationInvalidatesThePendingReadAndPresentsTheLatestData() {
		RunWithMessageLoop(async owner => {
			using var grid = NewGrid(owner);
			var source = new Source();
			source.Publish(Rows("Original"));
			grid.ConfigureDataSource(source);
			await grid.RefreshDataSourceAsync();
			var reads = source.Reads;
			source.HoldNextRead();
			using var releaseOnFailure = Tools.Scope.ExecuteOnDispose(() => source.Release.TrySetResult());
			await Task.Run(() => source.Publish(Rows("Next")));
			await source.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			var presentation = grid.RefreshDataSourceAsync();
			source.Release.TrySetResult();
			await presentation;
			await Task.Delay(30);
			Assert.That(source.Reads, Is.EqualTo(reads + 2), "An explicit refresh invalidates the already pending query before presenting the latest data.");
			Assert.That(grid.VisibleEntities.Cast<Row>().All(row => row.Name.StartsWith("Next", StringComparison.Ordinal)), Is.True);
		});
	}

	[Test]
	public void ASourcePublicationWaitsForTheNativeCellDraftToEnd() {
		RunWithMessageLoop(async owner => {
			using var grid = NewGrid(owner);
			grid.AllowCellEditing = true;
			var source = new Source();
			source.Publish(Rows("Original"));
			await grid.SetDataSource(source);
			await grid.RefreshGrid();
			var native = grid.Controls.Find("_grid", true).OfType<Grid>().Single();
			grid.SelectedEntity = grid.VisibleEntities.First();
			typeof(GridVirtual).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)
				.Invoke(native, new object[] { new KeyEventArgs(Keys.F2) });
			var cell = new CellContext(native, new Position(1, 0));
			Assert.That(cell.IsEditing(), Is.True);
			var editor = ((EditorControlBase)cell.Cell.Editor).Control;
			editor.Text = "My live draft";
			var reads = source.Reads;
			await Task.Run(() => source.Publish(Rows("External")));
			await Task.Delay(50);
			Assert.That(source.Reads, Is.EqualTo(reads));
			Assert.That(editor.Text, Is.EqualTo("My live draft"));
			Assert.That(cell.IsEditing(), Is.True);
			cell.EndEdit(true);
			await UntilAsync(() => grid.VisibleEntities.Cast<Row>().All(row => row.Name.StartsWith("External", StringComparison.Ordinal)));
		});
	}

	[Test]
	public void RebindingClearingAndDisposalRemoveSourceHandlersWithoutTakingSourceOwnership() {
		RunWithMessageLoop(async owner => {
			using var grid = NewGrid(owner);
			var first = new Source();
			var second = new Source();
			var silent = new Source { Notifies = false };
			first.Publish(Rows("First"));
			second.Publish(Rows("Second"));
			silent.Publish(Rows("Silent"));
			await grid.SetDataSource(first, DataSourceCapabilities.CanRead);
			await grid.RefreshGrid();
			Assert.That(first.Subscribers, Is.EqualTo(1));
			grid.Capabilities = DataSourceCapabilities.CanRead;
			Assert.That(grid.Capabilities.HasFlag(DataSourceCapabilities.CanNotify), Is.True, "Changing allowed commands cannot remove the bound source's notification capability.");
			await grid.SetDataSource(second, DataSourceCapabilities.CanRead);
			await grid.RefreshGrid();
			Assert.That(first.Subscribers, Is.Zero);
			Assert.That(second.Subscribers, Is.EqualTo(1));
			grid.ClearDataSource();
			Assert.That(second.Subscribers, Is.Zero);
			await grid.SetDataSource(silent, DataSourceCapabilities.CanRead);
			await grid.RefreshGrid();
			Assert.That(silent.Subscribers, Is.Zero);
			await grid.SetDataSource(second);
			await grid.RefreshGrid();
			grid.Dispose();
			Assert.That(second.Subscribers, Is.Zero);
			Assert.That(second.Count, Is.EqualTo(3));
		});
	}

	[Test]
	public void GridUsesActualHostedAndDetachedPresentationAndKeepsOnePendingUpdate() {
		RunWithMessageLoop(async owner => {
			using var host = new ApplicationScreenHost { Dock = DockStyle.Fill, ScreenMode = ScreenMode.MultiView };
			owner.Controls.Add(host);
			var first = new ObservedScreen { Title = "Observed" };
			var second = new ObservedScreen { Title = "Other" };
			host.ShowScreen(first);
			var source = new Source();
			using var grid = NewGrid(first);
			await grid.SetDataSource(source);
			await grid.RefreshGrid();
			var changes = 0;
			grid.DataBindingComplete += _ => changes++;
			host.ShowScreen(second);
			await Task.Run(() => { source.Publish(Rows("One")); source.Publish(Rows("Two")); });
			await Task.Delay(50);
			Assert.That(changes, Is.Zero, "A retained inactive tab can remain native-visible and must still defer its presentation.");
			host.ShowScreen(first);
			await UntilAsync(() => changes == 1);
			Assert.That(host.UndockScreen(first), Is.True);
			host.ShowScreen(second);
			await Task.Run(() => source.Publish(Rows("Detached")));
			await UntilAsync(() => changes == 2);
			Assert.That(host.DockScreen(first), Is.True);
			host.ShowScreen(second);
			await Task.Run(() => source.Publish(Rows("Redocked inactive")));
			await Task.Delay(50);
			Assert.That(changes, Is.EqualTo(2));
			grid.Visible = false;
			host.ShowScreen(first);
			await Task.Delay(30);
			Assert.That(changes, Is.EqualTo(2), "An explicitly hidden grid must stay deferred across host events.");
			grid.Visible = true;
			await UntilAsync(() => changes == 3);
			grid.ClearDataSource();
			Assert.That(source.Subscribers, Is.Zero);
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void AnInactiveSourceRefreshRetainsPendingChangesUnlessAnExplicitRefreshJoins(bool explicitRefresh) {
		RunWithMessageLoop(async owner => {
			using var host = new ApplicationScreenHost { Dock = DockStyle.Fill, ScreenMode = ScreenMode.MultiView };
			owner.Controls.Add(host);
			var first = new ObservedScreen { Title = "Observed" };
			var second = new ObservedScreen { Title = "Other" };
			host.ShowScreen(first);
			using var grid = NewGrid(first);
			var source = new Source();
			source.Publish(Rows("Original"));
			grid.ConfigureDataSource(source);
			await grid.RefreshDataSourceAsync();
			var changes = 0;
			grid.DataBindingComplete += _ => changes++;
			source.HoldNextRead();
			using var release = Tools.Scope.ExecuteOnDispose(() => {
				if (!source.Release.Task.IsCompleted)
					source.Release.SetResult();
			});
			await Task.Run(() => source.Publish(Rows("In flight")));
			await source.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			host.ShowScreen(second);
			await Task.Run(() => source.Publish(Rows("Latest")));
			var reads = source.Reads;
			var refresh = explicitRefresh ? grid.RefreshDataSourceAsync() : grid.RefreshCompletion;
			source.Release.SetResult();
			await refresh;
			Assert.That(source.Reads, Is.EqualTo(reads + (explicitRefresh ? 1 : 0)), "Only an explicit request may retry the held read while its screen is inactive.");
			Assert.That(changes, Is.EqualTo(explicitRefresh ? 1 : 0));
			Assert.That(grid.VisibleEntities.Cast<Row>().All(row => row.Name.StartsWith(explicitRefresh ? "Latest" : "Original", StringComparison.Ordinal)), Is.True);
			host.ShowScreen(first);
			await UntilAsync(() => changes == 1);
			Assert.That(source.Reads, Is.EqualTo(reads + 1));
			Assert.That(grid.VisibleEntities.Cast<Row>().All(row => row.Name.StartsWith("Latest", StringComparison.Ordinal)), Is.True);
		});
	}

	private static Row[] Rows(string name) => Enumerable.Range(1, 3).Select(id => new Row { Id = id, Name = name + " " + id }).ToArray();

	private static VisualGrid NewGrid(Control owner) {
		var grid = new VisualGrid {
			Dock = DockStyle.Fill, PageMode = VisualGridPageMode.Single, PrepareReadOnlyCellsInBackground = true,
			GridBindings = new[] { new VisualGridColumn<Row> {
				ColumnName = "Name", SortName = nameof(Row.Name), DataType = typeof(string), PropertyValue = row => row.Name,
				SetPropertyValue = (row, value) => row.Name = (string)value, CanEditCell = true
			} }
		};
		owner.Controls.Add(grid);
		return grid;
	}

	private static void ClickRow(Grid grid, int row) {
		var rectangle = grid.RangeToRectangle(new CellRange(row, 0, row, 0));
		var point = new Point(rectangle.Left + 8, rectangle.Top + rectangle.Height / 2);
		var args = new MouseEventArgs(MouseButtons.Left, 1, point.X, point.Y, 0);
		foreach (var method in new[] { "OnMouseDown", "OnMouseClick" })
			typeof(GridVirtual).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(grid, new object[] { args });
		grid.Controller.OnClick(new CellContext(grid, new Position(row, 0)), args);
		typeof(GridVirtual).GetMethod("OnMouseUp", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(grid, new object[] { args });
	}

	private static async Task UntilAsync(Func<bool> condition) {
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		while (!condition())
			await Task.Delay(5, timeout.Token);
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(1000, 650) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("The data source notification fixture timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			try { await test(owner); }
			catch (Exception error) { failure = error; }
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}

	#region Inner Types

	private sealed class ObservedScreen : ApplicationScreen {
		public ObservedScreen() => ActivationMode = ScreenActivationMode.MultiInstance;
	}

	public sealed class Row {
		public int Id { get; set; }
		public string Name { get; set; }
	}

	private sealed class Source : ArrayDataSource<Row> {
		private int _reads;
		private int _holdNext;
		public int Reads => Volatile.Read(ref _reads);
		public int Subscribers { get; private set; }
		public bool Notifies { get; set; } = true;
		public TaskCompletionSource Entered { get; private set; }
		public TaskCompletionSource Release { get; private set; }
		public override event EventHandlerEx Changed { add { Subscribers++; base.Changed += value; } remove { Subscribers--; base.Changed -= value; } }
		public override DataSourceCapabilities Capabilities => DataSourceCapabilities.Default | (Notifies ? DataSourceCapabilities.CanNotify : 0);

		public void Publish(Row[] rows) => ReplaceData(rows);

		public void HoldNextRead() {
			Entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			Release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			Interlocked.Exchange(ref _holdNext, 1);
		}

		public override async Task<DataSourceItems<Row>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
			Interlocked.Increment(ref _reads);
			var read = await base.ReadRangeAsync(searchTerm, pageLength, page, sortProperty, sortDirection);
			if (Interlocked.Exchange(ref _holdNext, 0) != 0) {
				Entered.TrySetResult();
				await Release.Task;
			}
			return read;
		}
	}

	#endregion
}

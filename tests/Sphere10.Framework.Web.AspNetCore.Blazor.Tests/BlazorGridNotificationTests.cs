// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
public class BlazorGridNotificationTests {
	[Test]
	public async Task PublishedDataAutomaticallyRefreshesTheCurrentQueryAndSelectionWithoutAnIndicator() {
		var source = new Source(Enumerable.Range(1, 6).Select(id => new Row { Id = id, Name = $"Group {id}" }).ToArray());
		using var controller = NewController();
		await controller.BindAsync(source, DataSourceCapabilities.Default, 2);
		await controller.SearchAsync("Group");
		await controller.SortAsync(nameof(Row.Id), SortDirection.Descending);
		await controller.SetPageAsync(1);
		controller.Select(controller.Items[1]);
		var selected = controller.SelectedItem.Id;
		var loading = false;
		controller.Changed += () => loading |= controller.IsLoading;
		await Task.Run(() => source.Publish(source.Data.Select(row => new Row { Id = row.Id, Name = row.Name + " Updated" }).ToArray()));
		await UntilAsync(() => controller.Items.All(row => row.Name.EndsWith("Updated", StringComparison.Ordinal)));
		Assert.That(controller.Capabilities.HasFlag(DataSourceCapabilities.CanNotify), Is.True, "Notification support is independent of the allowed CRUD mask.");
		Assert.That(controller.CurrentPage, Is.EqualTo(1));
		Assert.That(controller.SearchTerm, Is.EqualTo("Group"));
		Assert.That(controller.SortProperty, Is.EqualTo(nameof(Row.Id)));
		Assert.That(controller.SortDirection, Is.EqualTo(SortDirection.Descending));
		Assert.That(controller.SelectedItem.Id, Is.EqualTo(selected));
		Assert.That(controller.SelectedItem.Name, Does.EndWith("Updated"));
		Assert.That(loading, Is.False);
	}

	[Test]
	public async Task NotificationsWaitForCancelAndASuccessfulWriteReadsOnlyOnce() {
		var source = new Source(new Row { Id = 1, Name = "Original" });
		using var controller = NewController();
		await controller.BindAsync(source, DataSourceCapabilities.Default, 10);
		controller.BeginEdit(controller.Items[0]);
		controller.SetEditValue(controller.Columns[0], "Draft");
		var reads = source.Reads;
		await Task.Run(() => source.Publish(new Row { Id = 1, Name = "External" }));
		await Task.Delay(50);
		Assert.That(controller.IsEditing, Is.True);
		Assert.That(controller.GetEditValue(controller.Columns[0]), Is.EqualTo("Draft"));
		Assert.That(source.Reads, Is.EqualTo(reads));
		controller.CancelEdit();
		await UntilAsync(() => controller.Items.Single().Name == "External");
		controller.BeginEdit(controller.Items[0]);
		controller.SetEditValue(controller.Columns[0], "Saved");
		reads = source.Reads;
		Assert.That(await controller.SaveAsync(), Is.True);
		await Task.Delay(50);
		Assert.That(source.Reads, Is.EqualTo(reads + 1), "The source event and the grid's post-save read must share one query.");
		Assert.That(controller.Items.Single().Name, Is.EqualTo("Saved"));
	}

	[Test]
	public async Task ANotificationReadInterruptedByANewDraftRemainsPendingUntilCancel() {
		var source = new Source(new Row { Id = 1, Name = "Original" });
		using var controller = NewController();
		await controller.BindAsync(source, DataSourceCapabilities.Default, 10);
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var releaseOnFailure = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
		source.Delay = async () => {
			entered.TrySetResult();
			await release.Task;
		};
		await Task.Run(() => source.Publish(new Row { Id = 1, Name = "External" }));
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		controller.BeginEdit(controller.Items[0]);
		controller.SetEditValue(controller.Columns[0], "Draft");
		source.Delay = null;
		release.TrySetResult();
		await UntilAsync(() => !controller.IsRefreshing);
		Assert.That(controller.GetEditValue(controller.Columns[0]), Is.EqualTo("Draft"));
		Assert.That(controller.Items.Single().Name, Is.EqualTo("Original"));
		controller.CancelEdit();
		await UntilAsync(() => controller.Items.Single().Name == "External" && !controller.IsRefreshing);
	}

	[Test]
	public async Task AWorkerPublicationRejectsAnOlderReadBeforeItsNotificationIsPresented() {
		var source = new Source(new Row { Id = 1, Name = "Original" });
		using var controller = NewController();
		await controller.BindAsync(source, DataSourceCapabilities.Default, 10);
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var oldRead = 0;
		source.Delay = async () => {
			if (Interlocked.Increment(ref oldRead) == 1) {
				entered.TrySetResult();
				await release.Task;
			}
		};
		var staleAccepted = false;
		controller.Changed += () => staleAccepted |= controller.Items.Any(row => row.Name == "Obsolete");
		source.Publish(new Row { Id = 1, Name = "Obsolete" });
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		await Task.Run(() => source.Publish(new Row { Id = 1, Name = "Latest" }));
		release.TrySetResult();
		await UntilAsync(() => controller.Items.Single().Name == "Latest" && !controller.IsRefreshing);
		Assert.That(staleAccepted, Is.False);
		Assert.That(source.MaximumActiveReads, Is.EqualTo(1));
	}

	[Test]
	public async Task ReplacementAndDisposalDetachTheBorrowedSourceAndNonNotifyingSourcesAreNotObserved() {
		var first = new Source(new Row { Id = 1, Name = "First" });
		var second = new Source(new Row { Id = 2, Name = "Second" });
		var silent = new Source(new Row { Id = 3, Name = "Silent" }) { Notifies = false };
		using var controller = NewController();
		await controller.BindAsync(first, DataSourceCapabilities.CanRead, 10);
		Assert.That(first.Subscribers, Is.EqualTo(1));
		await controller.BindAsync(second, DataSourceCapabilities.CanRead, 10);
		Assert.That(first.Subscribers, Is.Zero);
		Assert.That(second.Subscribers, Is.EqualTo(1));
		var reads = second.Reads;
		first.Publish(new Row { Id = 99, Name = "Unrelated" });
		await Task.Delay(30);
		Assert.That(second.Reads, Is.EqualTo(reads));
		await controller.BindAsync(silent, DataSourceCapabilities.CanRead, 10);
		Assert.That(second.Subscribers, Is.Zero);
		Assert.That(silent.Subscribers, Is.Zero);
		await controller.BindAsync(second, DataSourceCapabilities.CanRead, 10);
		controller.Dispose();
		Assert.That(second.Subscribers, Is.Zero);
		Assert.That(second.Count, Is.EqualTo(1), "The grid borrows its source and never disposes it.");
	}

	[Test]
	public async Task WorkerNotificationsUseTheSynchronizationContextCapturedDuringBinding() {
		var source = new Source(new Row { Id = 1, Name = "Before" });
		using var controller = NewController();
		var owner = new OwnerContext();
		var previous = SynchronizationContext.Current;
		Task binding;
		try {
			SynchronizationContext.SetSynchronizationContext(owner);
			binding = controller.BindAsync(source, DataSourceCapabilities.CanRead, 10);
		} finally {
			SynchronizationContext.SetSynchronizationContext(previous);
		}
		await owner.CompleteAsync(binding);
		var changedOutsideOwner = false;
		controller.Changed += () => changedOutsideOwner |= !owner.IsDispatching;
		await Task.Run(() => source.Publish(new Row { Id = 1, Name = "After" }));
		Assert.That(controller.Items.Single().Name, Is.EqualTo("Before"), "A source worker may invalidate but must not mutate renderer-owned presentation.");
		await owner.UntilAsync(() => controller.Items.Single().Name == "After" && !controller.IsRefreshing);
		Assert.That(changedOutsideOwner, Is.False);
	}

	private static BlazorGridController<Row> NewController() {
		return new BlazorGridController<Row> {
			Columns = new[] { BlazorGridColumn<Row>.For(row => row.Name) },
			EntityComparer = new RowComparer()
		};
	}

	private static async Task UntilAsync(Func<bool> condition) {
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		while (!condition())
			await Task.Delay(5, timeout.Token);
	}

	#region Inner Types

	public sealed class Row {
		public int Id { get; set; }
		public string Name { get; set; }
	}

	private sealed class RowComparer : IEqualityComparer<Row> {
		public bool Equals(Row left, Row right) => left?.Id == right?.Id;

		public int GetHashCode(Row row) => row.Id;
	}

	private sealed class Source(params Row[] rows) : ArrayDataSource<Row> {
		private int _reads;
		private int _active;
		private int _maximumActive;
		public int Reads => Volatile.Read(ref _reads);
		public int MaximumActiveReads => Volatile.Read(ref _maximumActive);
		public int Subscribers { get; private set; }
		public bool Notifies { get; set; } = true;
		public Func<Task> Delay { get; set; }
		public override event EventHandlerEx Changed { add { Subscribers++; base.Changed += value; } remove { Subscribers--; base.Changed -= value; } }
		public override DataSourceCapabilities Capabilities => DataSourceCapabilities.Default | (Notifies ? DataSourceCapabilities.CanNotify : 0);

		public void Publish(params Row[] values) => ReplaceData(values);

		public override async Task<DataSourceItems<Row>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
			if (Interlocked.Increment(ref _reads) == 1)
				PublishData(rows);
			var active = Interlocked.Increment(ref _active);
			Interlocked.Exchange(ref _maximumActive, Math.Max(active, _maximumActive));
			using var finished = Tools.Scope.ExecuteOnDispose(() => Interlocked.Decrement(ref _active));
			var result = await base.ReadRangeAsync(searchTerm, pageLength, page, sortProperty, sortDirection);
			if (Delay != null)
				await Delay();
			return result;
		}

		public override void UpdateRange(IEnumerable<Row> values) {
			var replacements = values.ToDictionary(row => row.Id);
			ReplaceData(Data.Select(row => replacements.GetValueOrDefault(row.Id, row)).ToArray());
		}

		public override Result ValidateRange(IEnumerable<(Row entity, CrudAction action)> actions) => Result.Success;
	}

	private sealed class OwnerContext : SynchronizationContext {
		private readonly ConcurrentQueue<(SendOrPostCallback Callback, object State)> _pending = new();
		public bool IsDispatching { get; private set; }

		public override void Post(SendOrPostCallback callback, object state) => _pending.Enqueue((callback, state));

		public async Task CompleteAsync(Task task) {
			await UntilAsync(() => task.IsCompleted);
			await task;
		}

		public async Task UntilAsync(Func<bool> condition) {
			using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
			while (!condition()) {
				while (_pending.TryDequeue(out var work)) {
					var previous = Current;
					try {
						IsDispatching = true;
						SetSynchronizationContext(this);
						work.Callback(work.State);
					} finally {
						SetSynchronizationContext(previous);
						IsDispatching = false;
					}
				}
				await Task.Delay(5, timeout.Token);
			}
		}
	}

	#endregion
}

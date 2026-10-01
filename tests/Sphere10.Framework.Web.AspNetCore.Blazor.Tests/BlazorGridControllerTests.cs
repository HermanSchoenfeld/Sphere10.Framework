// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BlazorGridControllerTests {
	[Test]
	public async Task CompositeCapabilitiesDoNotGrantActionsFromTheirReadBit() {
		var source = new ProbeSource(new Row { Id = 1, Name = "One" }) { AvailableCapabilities = DataSourceCapabilities.CanRead };
		using var controller = CreateController();
		await controller.BindAsync(source, DataSourceCapabilities.Default, 10);
		controller.BeginEdit(controller.Items[0]);
		Assert.That(controller.IsEditing, Is.False);
		Assert.That(controller.Capabilities.HasFlag(DataSourceCapabilities.CanUpdate), Is.False);
		Assert.That(controller.Capabilities.HasFlag(DataSourceCapabilities.CanSearch), Is.False);
		Assert.That(controller.Capabilities.HasFlag(DataSourceCapabilities.CanSort), Is.False);
		await controller.SearchAsync("Denied");
		await controller.SortAsync(nameof(Row.Name), SortDirection.Descending);
		controller.BeginCreate();
		Assert.That(controller.IsEditing, Is.False);
		Assert.That(source.Queries, Has.Count.EqualTo(1));
		Assert.That(source.Created, Is.Zero);
	}

	[Test]
	public async Task AllowedCapabilitiesIntersectSourceCapabilities() {
		var source = new ProbeSource(new Row { Id = 1, Name = "One" });
		using var controller = CreateController();
		await controller.BindAsync(source, DataSourceCapabilities.CanRead, 10);
		controller.Select(controller.Items[0]);
		Assert.That(await controller.DeleteAsync(), Is.False);
		Assert.That(controller.Capabilities, Is.EqualTo(DataSourceCapabilities.CanRead));
		Assert.That(source.Deleted, Is.Zero);
	}

	[Test]
	public async Task SearchSortAndPagingArePassedToTheSourceWithZeroBasedPages() {
		var source = new ProbeSource(Enumerable.Range(1, 5).Select(index => new Row { Id = index, Name = $"Row {index}" }).ToArray());
		using var controller = CreateController();
		await controller.BindAsync(source, DataSourceCapabilities.Default, 2);
		await controller.SetPageAsync(2);
		Assert.That(controller.CurrentPage, Is.EqualTo(2));
		Assert.That(controller.Items.Single().Id, Is.EqualTo(5));
		Assert.That(source.Queries.Last().Page, Is.EqualTo(2));
		await controller.SortAsync(nameof(Row.Name), SortDirection.Descending);
		Assert.That(controller.CurrentPage, Is.Zero);
		Assert.That(controller.Items.Select(row => row.Id), Is.EqualTo(new[] { 5, 4 }));
		await controller.SearchAsync("Row 3");
		Assert.That(controller.Items.Single().Id, Is.EqualTo(3));
		Assert.That(source.Queries.Last(), Is.EqualTo(new Query("Row 3", 2, 0, nameof(Row.Name), SortDirection.Descending)));
		await controller.SetPageSizeAsync(5);
		Assert.That(controller.PageSize, Is.EqualTo(5));
		Assert.That(controller.TotalPages, Is.EqualTo(1));
	}

	[Test]
	public async Task ReadsAreSequentialAndOnlyTheLatestQueuedQueryRuns() {
		var source = new ProbeSource(new Row { Id = 1, Name = "Latest" });
		using var controller = CreateController();
		await controller.BindAsync(source, DataSourceCapabilities.Default, 10);
		var delayed = new TaskCompletionSource<DataSourceItems<Row>>(TaskCreationOptions.RunContinuationsAsynchronously);
		source.Read = query => query.SearchTerm == "" ? delayed.Task : Task.FromResult(source.ReadItems(query));
		var refresh = controller.RefreshAsync();
		var middle = controller.SearchAsync("Obsolete");
		var latest = controller.SearchAsync("Latest");
		Assert.That(source.ActiveReads, Is.EqualTo(1));
		Assert.That(refresh, Is.SameAs(middle));
		Assert.That(refresh, Is.SameAs(latest));
		delayed.SetResult(new DataSourceItems<Row> { Items = new[] { new Row { Id = 99, Name = "Stale" } }, Page = 0, TotalCount = 1 });
		await Task.WhenAll(refresh, middle, latest).WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(source.MaxActiveReads, Is.EqualTo(1));
		Assert.That(source.Queries.Select(query => query.SearchTerm), Is.EqualTo(new[] { "", "", "Latest" }));
		Assert.That(controller.Items.Single().Name, Is.EqualTo("Latest"));
		Assert.That(controller.IsLoading, Is.False);
	}

	[Test]
	public async Task SourceReplacementRejectsDelayedResultsAndDoesNotDisposeSources() {
		var delayed = new TaskCompletionSource<DataSourceItems<Row>>(TaskCreationOptions.RunContinuationsAsynchronously);
		var oldSource = new ProbeSource(new Row { Id = 1, Name = "Old" }) { Read = _ => delayed.Task };
		var newSource = new ProbeSource(new Row { Id = 2, Name = "New" });
		using var controller = CreateController();
		var first = controller.BindAsync(oldSource, DataSourceCapabilities.Default, 10);
		var second = controller.BindAsync(newSource, DataSourceCapabilities.Default, 10);
		Assert.That(controller.Items, Is.Empty);
		delayed.SetException(new InvalidOperationException("Old-source failure"));
		await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(controller.Items.Single().Name, Is.EqualTo("New"));
		Assert.That(controller.ErrorMessage, Is.Null);
		Assert.That(oldSource.Disposed, Is.False);
		Assert.That(newSource.Disposed, Is.False);
	}

	[Test]
	public async Task DisposalIgnoresOutstandingReadWithoutNotifyingOrDisposingItsSource() {
		var delayed = new TaskCompletionSource<DataSourceItems<Row>>(TaskCreationOptions.RunContinuationsAsynchronously);
		var source = new ProbeSource { Read = _ => delayed.Task };
		var controller = CreateController();
		var changes = 0;
		controller.Changed += () => changes++;
		var binding = controller.BindAsync(source, DataSourceCapabilities.Default, 10);
		controller.Dispose();
		var changesAtDisposal = changes;
		delayed.SetResult(new DataSourceItems<Row> { Items = new[] { new Row { Name = "Late" } }, TotalCount = 1 });
		await binding.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(controller.Items, Is.Empty);
		Assert.That(changes, Is.EqualTo(changesAtDisposal));
		Assert.That(source.Disposed, Is.False);
	}

	[Test]
	public async Task FailedValidationRestoresTheEntityAndKeepsTheDraftForRetry() {
		var originalReference = new object();
		var replacementReference = new object();
		var row = new Row { Id = 1, Name = "Original", Reference = originalReference };
		var source = new ProbeSource(row) { Validation = (_, _) => Task.FromResult(Result.Error("Name rejected")) };
		using var controller = CreateController();
		await controller.BindAsync(source, DataSourceCapabilities.Default, 10);
		var name = controller.Columns[0];
		var reference = controller.Columns[1];
		controller.BeginEdit(row);
		controller.SetEditValue(name, "Draft");
		controller.SetEditValue(reference, replacementReference);
		Assert.That(row.Name, Is.EqualTo("Original"));
		Assert.That(row.Reference, Is.SameAs(originalReference));
		Assert.That(await controller.SaveAsync(), Is.False);
		Assert.That(row.Name, Is.EqualTo("Original"));
		Assert.That(row.Reference, Is.SameAs(originalReference));
		Assert.That(controller.GetEditValue(name), Is.EqualTo("Draft"));
		Assert.That(controller.GetEditValue(reference), Is.SameAs(replacementReference));
		Assert.That(controller.ValidationErrors, Is.EqualTo(new[] { "Name rejected" }));
		Assert.That(source.Updated, Is.Zero);
		source.Validation = (_, _) => Task.FromResult(Result.Success);
		Assert.That(await controller.SaveAsync(), Is.True);
		Assert.That(source.LastUpdated, Is.SameAs(row));
		Assert.That(row.Name, Is.EqualTo("Draft"));
		Assert.That(row.Reference, Is.SameAs(replacementReference));
		Assert.That(controller.IsEditing, Is.False);
	}

	[Test]
	public async Task WriteFailureRestoresValuesAndLeavesARetryableDraft() {
		var row = new Row { Id = 1, Name = "Original" };
		var source = new ProbeSource(row) { UpdateHandler = _ => Task.FromException(new InvalidOperationException("Offline")) };
		using var controller = CreateController();
		await controller.BindAsync(source, DataSourceCapabilities.Default, 10);
		controller.BeginEdit(row);
		controller.SetEditValue(controller.Columns[0], "Draft");
		Assert.That(await controller.SaveAsync(), Is.False);
		Assert.That(row.Name, Is.EqualTo("Original"));
		Assert.That(controller.GetEditValue(controller.Columns[0]), Is.EqualTo("Draft"));
		Assert.That(controller.ErrorMessage, Is.EqualTo("Offline"));
		Assert.That(controller.IsSaving, Is.False);
		source.UpdateHandler = null;
		Assert.That(await controller.SaveAsync(), Is.True);
		Assert.That(row.Name, Is.EqualTo("Draft"));
	}

	[Test]
	public async Task CreationDoesNotPersistBeforeSaveAndCancelDiscardsTheDraft() {
		var source = new ProbeSource();
		using var controller = CreateController();
		await controller.BindAsync(source, DataSourceCapabilities.Default, 10);
		controller.BeginCreate();
		Assert.That(controller.IsNewItem, Is.True);
		controller.SetEditValue(controller.Columns[0], "Cancelled");
		controller.CancelEdit();
		Assert.That(source.Created, Is.Zero);
		controller.BeginCreate();
		controller.SetEditValue(controller.Columns[0], "Saved");
		Assert.That(await controller.SaveAsync(), Is.True);
		Assert.That(source.Created, Is.EqualTo(1));
		Assert.That(source.Rows.Single().Name, Is.EqualTo("Saved"));
		Assert.That(source.Validations.Last(), Is.EqualTo(CrudAction.Create));
	}

	[Test]
	public async Task DeleteValidatesAndRepairsTheLastPageAfterRemoval() {
		var source = new ProbeSource(new Row { Id = 1, Name = "First" }, new Row { Id = 2, Name = "Last" }) { ClampPages = false };
		using var controller = CreateController();
		await controller.BindAsync(source, DataSourceCapabilities.Default, 1);
		await controller.SetPageAsync(1);
		controller.Select(controller.Items[0]);
		source.Validation = (_, _) => Task.FromResult(Result.Error("Protected"));
		Assert.That(await controller.DeleteAsync(), Is.False);
		Assert.That(source.Deleted, Is.Zero);
		Assert.That(controller.HasSelection, Is.True);
		source.Validation = (_, _) => Task.FromResult(Result.Success);
		Assert.That(await controller.DeleteAsync(), Is.True);
		Assert.That(controller.CurrentPage, Is.Zero);
		Assert.That(controller.Items.Single().Name, Is.EqualTo("First"));
		Assert.That(controller.HasSelection, Is.False);
		Assert.That(source.Validations, Is.All.EqualTo(CrudAction.Delete));
	}

	[TestCase(true)]
	[TestCase(false)]
	public async Task BindingChangesDuringValidationPreventPersistenceAndRestoreOriginalValues(bool replaceSource) {
		var row = new Row { Id = 1, Name = "Original" };
		var validation = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
		var source = new ProbeSource(row) { Validation = (_, _) => validation.Task };
		using var controller = CreateController();
		await controller.BindAsync(source, DataSourceCapabilities.Default, 10);
		controller.BeginEdit(row);
		controller.SetEditValue(controller.Columns[0], "Draft");
		var save = controller.SaveAsync();
		var replacement = replaceSource ? new ProbeSource(new Row { Name = "Replacement" }) : source;
		var binding = controller.BindAsync(replacement, DataSourceCapabilities.CanRead, 10);
		validation.SetResult(Result.Success);
		Assert.That(await save.WaitAsync(TimeSpan.FromSeconds(5)), Is.False);
		await binding.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(source.Updated, Is.Zero);
		Assert.That(row.Name, Is.EqualTo("Original"));
		Assert.That(controller.IsEditing, Is.False);
		Assert.That(controller.IsSaving, Is.False);
	}

	[Test]
	public async Task CompletedOldSourceWriteDoesNotReplaceTheNewView() {
		var row = new Row { Id = 1, Name = "Original" };
		var persistence = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var source = new ProbeSource(row) { UpdateHandler = _ => persistence.Task };
		var replacement = new ProbeSource(new Row { Id = 2, Name = "Replacement" });
		using var controller = CreateController();
		await controller.BindAsync(source, DataSourceCapabilities.Default, 10);
		controller.BeginEdit(row);
		controller.SetEditValue(controller.Columns[0], "Committed");
		var save = controller.SaveAsync();
		var binding = controller.BindAsync(replacement, DataSourceCapabilities.Default, 10);
		persistence.SetResult();
		Assert.That(await save.WaitAsync(TimeSpan.FromSeconds(5)), Is.False);
		await binding.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(row.Name, Is.EqualTo("Committed"));
		Assert.That(controller.Items.Single().Name, Is.EqualTo("Replacement"));
		Assert.That(controller.IsEditing, Is.False);
	}

	[Test]
	public async Task ManualNavigationCannotLoseADraftAndSelectionOnlyContainsVisibleRows() {
		var source = new ProbeSource(new Row { Id = 1, Name = "First" }, new Row { Id = 2, Name = "Second" });
		using var controller = CreateController();
		await controller.BindAsync(source, DataSourceCapabilities.Default, 1);
		controller.Select(controller.Items[0]);
		controller.BeginEdit(controller.SelectedItem);
		controller.SetEditValue(controller.Columns[0], "Draft");
		Assert.That(() => controller.SearchAsync("Second"), Throws.InvalidOperationException);
		Assert.That(() => controller.ClearSelection(), Throws.InvalidOperationException);
		controller.CancelEdit();
		await controller.SetPageAsync(1);
		Assert.That(controller.HasSelection, Is.False);
		Assert.That(() => controller.Select(source.Rows[0]), Throws.ArgumentException);
	}

	[Test]
	public async Task ReadFailureRetainsTheSameQueryRowsAndAllowsRefreshRetry() {
		var source = new ProbeSource(new Row { Id = 1, Name = "Known" });
		using var controller = CreateController();
		await controller.BindAsync(source, DataSourceCapabilities.Default, 10);
		source.Read = _ => Task.FromException<DataSourceItems<Row>>(new InvalidOperationException("Offline"));
		await controller.RefreshAsync();
		Assert.That(controller.Items.Single().Name, Is.EqualTo("Known"));
		Assert.That(controller.ErrorMessage, Is.EqualTo("Offline"));
		Assert.That(controller.IsLoading, Is.False);
		source.Read = null;
		await controller.RefreshAsync();
		Assert.That(controller.ErrorMessage, Is.Null);
	}

	[Test]
	public async Task EditingSelectsTheVisibleEntityAndCreationClearsSelection() {
		var row = new Row { Id = 1, Name = "First" };
		using var controller = CreateController();
		await controller.BindAsync(new ProbeSource(row), DataSourceCapabilities.Default, 10);
		controller.BeginEdit(row);
		Assert.That(controller.SelectedItem, Is.SameAs(row));
		Assert.That(controller.HasSelection, Is.True);
		controller.CancelEdit();
		controller.BeginCreate();
		Assert.That(controller.HasSelection, Is.False);
	}

	[Test]
	public async Task SaveOnlyInvokesChangedSettersAndRechecksColumnPermissions() {
		var row = new Row { Id = 1, Name = "Original" };
		var source = new ProbeSource(row);
		using var controller = CreateController();
		var column = controller.Columns[0];
		var setter = column.SetPropertyValue;
		var setterCalls = 0;
		column.SetPropertyValue = (item, value) => {
			setterCalls++;
			setter(item, value);
		};
		await controller.BindAsync(source, DataSourceCapabilities.Default, 10);
		controller.BeginEdit(row);
		Assert.That(await controller.SaveAsync(), Is.True);
		Assert.That(setterCalls, Is.Zero);
		controller.BeginEdit(row);
		controller.SetEditValue(column, "Forbidden");
		column.CanEditCell = false;
		Assert.That(await controller.SaveAsync(), Is.False);
		Assert.That(setterCalls, Is.Zero);
		Assert.That(row.Name, Is.EqualTo("Original"));
		Assert.That(controller.ErrorMessage, Does.Contain("no longer editable"));
	}

	[Test]
	public async Task DynamicSourcePermissionRevocationPreventsTheWrite() {
		var row = new Row { Id = 1, Name = "Original" };
		var source = new ProbeSource(row);
		using var controller = CreateController();
		await controller.BindAsync(source, DataSourceCapabilities.Default, 10);
		controller.BeginEdit(row);
		controller.SetEditValue(controller.Columns[0], "Revoked");
		source.AvailableCapabilities = DataSourceCapabilities.CanRead;
		Assert.That(await controller.SaveAsync(), Is.False);
		Assert.That(source.Updated, Is.Zero);
		Assert.That(source.Validations, Is.Empty);
		Assert.That(row.Name, Is.EqualTo("Original"));
		Assert.That(controller.IsEditing, Is.False);
	}

	[TestCase(0)]
	[TestCase(10000)]
	public void InvalidPageSizesAreRejected(int pageSize) {
		using var controller = CreateController();
		Assert.That(() => controller.BindAsync(new ProbeSource(), DataSourceCapabilities.Default, pageSize), Throws.InstanceOf<ArgumentOutOfRangeException>());
		Assert.That(() => controller.SetPageSizeAsync(pageSize), Throws.InstanceOf<ArgumentOutOfRangeException>());
	}

	[Test]
	public async Task SynchronousChangedReentryJoinsTheCurrentReadDrain() {
		var source = new ProbeSource(new Row { Id = 1, Name = "Latest" });
		using var controller = CreateController();
		await controller.BindAsync(source, DataSourceCapabilities.Default, 10);
		var reentered = false;
		var queued = Task.CompletedTask;
		controller.Changed += () => {
			if (controller.IsLoading && !reentered) {
				reentered = true;
				queued = controller.SearchAsync("Latest");
			}
		};
		var refresh = controller.RefreshAsync();
		await Task.WhenAll(refresh, queued).WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(source.Queries.Select(query => query.SearchTerm), Is.EqualTo(new[] { "", "Latest" }));
		Assert.That(source.MaxActiveReads, Is.EqualTo(1));
		Assert.That(controller.SearchTerm, Is.EqualTo("Latest"));
		Assert.That(controller.IsLoading, Is.False);
	}

	[TestCase(CrudAction.Update)]
	[TestCase(CrudAction.Delete)]
	public async Task PermissionRevocationDuringValidationPreventsPersistence(CrudAction action) {
		var row = new Row { Id = 1, Name = "Original" };
		var validation = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
		var source = new ProbeSource(row) { Validation = (_, _) => validation.Task };
		using var controller = CreateController();
		await controller.BindAsync(source, DataSourceCapabilities.Default, 10);
		controller.Select(row);
		if (action == CrudAction.Update) {
			controller.BeginEdit(row);
			controller.SetEditValue(controller.Columns[0], "Revoked");
		}
		var operation = action == CrudAction.Update ? controller.SaveAsync() : controller.DeleteAsync();
		source.AvailableCapabilities = DataSourceCapabilities.CanRead;
		validation.SetResult(Result.Success);
		Assert.That(await operation.WaitAsync(TimeSpan.FromSeconds(5)), Is.False);
		Assert.That(source.Updated, Is.Zero);
		Assert.That(source.Deleted, Is.Zero);
		Assert.That(row.Name, Is.EqualTo("Original"));
		Assert.That(controller.ErrorMessage, Does.Contain("permissions"));
		Assert.That(controller.IsSaving, Is.False);
		Assert.That(controller.IsEditing, Is.False);
	}

	[Test]
	public async Task ReplacingReturnedItemsDoesNotChangeTheEditablePage() {
		var row = new Row { Id = 1, Name = "Original" };
		using var controller = CreateController();
		await controller.BindAsync(new ProbeSource(row), DataSourceCapabilities.Default, 10);
		var items = controller.Items;
		items[0] = new Row { Id = 2, Name = "Replacement" };
		controller.BeginEdit(row);
		Assert.That(controller.Items[0], Is.SameAs(row));
		Assert.That(controller.EditingItem, Is.SameAs(row));
	}

	[Test]
	public async Task ReplacingColumnArrayEntriesCannotChangeAnActiveDraft() {
		var row = new Row { Id = 1, Name = "Original" };
		var name = BlazorGridColumn<Row>.For(item => item.Name);
		var columns = new[] { name };
		using var controller = new BlazorGridController<Row> { Columns = columns };
		await controller.BindAsync(new ProbeSource(row), DataSourceCapabilities.Default, 10);
		controller.BeginEdit(row);
		columns[0] = null;
		controller.Columns[0] = null;
		controller.SetEditValue(name, "Saved");
		Assert.That(await controller.SaveAsync(), Is.True);
		Assert.That(controller.Columns[0], Is.SameAs(name));
		Assert.That(row.Name, Is.EqualTo("Saved"));
	}

	[Test]
	public async Task ReplacingReturnedValidationErrorsDoesNotChangeTheFailure() {
		var row = new Row { Id = 1, Name = "Original" };
		var source = new ProbeSource(row) { Validation = (_, _) => Task.FromResult(Result.Error("Rejected")) };
		using var controller = CreateController();
		await controller.BindAsync(source, DataSourceCapabilities.Default, 10);
		controller.BeginEdit(row);
		Assert.That(await controller.SaveAsync(), Is.False);
		controller.ValidationErrors[0] = "Overwritten";
		Assert.That(controller.ValidationErrors, Is.EqualTo(new[] { "Rejected" }));
		Assert.That(controller.IsEditing, Is.True);
	}

	private static BlazorGridController<Row> CreateController() {
		var name = BlazorGridColumn<Row>.For(row => row.Name);
		var reference = BlazorGridColumn<Row>.For(row => row.Reference);
		reference.CanEditCell = true;
		return new BlazorGridController<Row> { Columns = new[] { name, reference } };
	}

	public sealed class Row {
		public int Id { get; set; }

		public string Name { get; set; }

		public object Reference { get; set; }
	}

	private sealed record Query(string SearchTerm, int PageSize, int Page, string SortProperty, SortDirection Direction);

	private sealed class ProbeSource : AsyncBatchDataSourceBase<Row>, IDisposable {
		public ProbeSource(params Row[] rows) {
			Rows.AddRange(rows);
		}

		public List<Row> Rows { get; } = new();

		public List<Query> Queries { get; } = new();

		public List<CrudAction> Validations { get; } = new();

		public DataSourceCapabilities AvailableCapabilities { get; set; } = DataSourceCapabilities.Default;

		public Func<Query, Task<DataSourceItems<Row>>> Read { get; set; }

		public Func<Row, CrudAction, Task<Result>> Validation { get; set; }

		public Func<Row, Task> UpdateHandler { get; set; }

		public bool ClampPages { get; set; } = true;

		public bool Disposed { get; private set; }

		public int ActiveReads { get; private set; }

		public int MaxActiveReads { get; private set; }

		public int Created { get; private set; }

		public int Updated { get; private set; }

		public int Deleted { get; private set; }

		public Row LastUpdated { get; private set; }

		public override Task<long> CountAsync => Task.FromResult((long)Rows.Count);

		public override Task<DataSourceCapabilities> CapabilitiesAsync => Task.FromResult(AvailableCapabilities);

		public override IEnumerable<Row> NewRange(int count) => Enumerable.Range(0, count).Select(_ => new Row { Name = "New" });

		public override Task CreateRangeAsync(IEnumerable<Row> entities) {
			var items = entities.ToArray();
			Created += items.Length;
			Rows.AddRange(items);
			return Task.CompletedTask;
		}

		public override async Task<DataSourceItems<Row>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
			var query = new Query(searchTerm, pageLength, page, sortProperty, sortDirection);
			Queries.Add(query);
			ActiveReads++;
			MaxActiveReads = Math.Max(MaxActiveReads, ActiveReads);
			using var completion = Tools.Scope.ExecuteOnDispose(() => ActiveReads--);
			return Read == null ? ReadItems(query) : await Read(query);
		}

		public DataSourceItems<Row> ReadItems(Query query) {
			var rows = Rows.Where(row => string.IsNullOrEmpty(query.SearchTerm) || row.Name.Contains(query.SearchTerm, StringComparison.OrdinalIgnoreCase));
			if (query.SortProperty == nameof(Row.Name))
				rows = query.Direction == SortDirection.Descending ? rows.OrderByDescending(row => row.Name) : rows.OrderBy(row => row.Name);
			var items = rows.ToArray();
			var page = ClampPages ? Math.Clamp(query.Page, 0, Math.Max(0, (items.Length - 1) / query.PageSize)) : query.Page;
			return new DataSourceItems<Row> { Items = items.Skip(page * query.PageSize).Take(query.PageSize).ToArray(), Page = page, TotalCount = items.Length };
		}

		public override Task RefreshRangeAsync(Row[] entities) => Task.CompletedTask;

		public override async Task UpdateRangeAsync(IEnumerable<Row> entities) {
			foreach (var item in entities) {
				Updated++;
				LastUpdated = item;
				if (UpdateHandler != null)
					await UpdateHandler(item);
			}
		}

		public override Task DeleteRangeAsync(IEnumerable<Row> entities) {
			foreach (var item in entities) {
				Deleted++;
				Rows.Remove(item);
			}
			return Task.CompletedTask;
		}

		public override async Task<Result> ValidateRangeAsync(IEnumerable<(Row entity, CrudAction action)> actions) {
			var results = new List<Result>();
			foreach (var (item, action) in actions) {
				Validations.Add(action);
				results.Add(Validation == null ? Result.Success : await Validation(item, action));
			}
			return Result.Combine(results);
		}

		public void Dispose() => Disposed = true;
	}
}
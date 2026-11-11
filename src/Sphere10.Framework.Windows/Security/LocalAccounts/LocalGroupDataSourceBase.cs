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
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework;

namespace Sphere10.Framework.Windows.Security;

/// <summary>Async CRUD over the complete group inventory; paging, sorting and searching remain Framework behavior.</summary>
/// <remarks>
/// A template: subclasses confirm group changes in <see cref="ConfirmAsync"/> and report them to <see cref="Logger"/>; clients which compose a
/// source without deriving one use <see cref="ActionLocalGroupDataSource"/>. Each refresh swaps in a fresh <see cref="Inventory"/>; edits change the
/// Windows group in the served inventory and update its rows in place.
/// </remarks>
public abstract class LocalGroupDataSourceBase : PollingDataSource<LocalGroupEntry, LocalGroupDataSourceBase.Inventories> {

	/// <param name="service">The account service that reads and changes Windows groups.</param>
	/// <param name="lifetime">Cancels the native operations of every inventory this source serves.</param>
	protected LocalGroupDataSourceBase(LocalAccountsService service, CancellationToken lifetime = default)
		: base(new Inventories(Checked(service), lifetime)) {
		InternalDataSource.Owner = this;
	}

	public LocalGroupEntry[] Rows => InternalDataSource.Current.Rows;

	/// <summary>The logger which records committed group changes.</summary>
	protected virtual ILogger Logger => new NoOpLogger();

	/// <summary>Confirms a group change; false cancels it.</summary>
	protected abstract Task<bool> ConfirmAsync(CrudAction action, LocalGroupEntry entry);

	private static LocalAccountsService Checked(LocalAccountsService service) {
		Guard.ArgumentNotNull(service, nameof(service));
		return service;
	}

	#region Inner Types

	/// <summary>Refreshes the served inventory with a fresh capture of the local groups.</summary>
	public sealed class Inventories : RefreshableDataSourceBase<LocalGroupEntry, Inventory> {
		private readonly LocalAccountsService _service;
		private readonly CancellationToken _lifetime;

		internal Inventories(LocalAccountsService service, CancellationToken lifetime)
			: base(new Inventory(service, lifetime, Array.Empty<LocalGroupEntry>()), true) {
			_service = service;
			_lifetime = lifetime;
		}

		/// <summary>The owning data source, assigned once by its constructor.</summary>
		internal LocalGroupDataSourceBase Owner {
			get;
			set {
				field = value;
				Current.Owner = value;
			}
		}

		protected override Inventory CreateDataSource() => new Inventory(_service, _lifetime) { Owner = Owner };
	}

	/// <summary>One capture of the local groups; account changes happen here and the rows follow.</summary>
	public sealed class Inventory : AsyncListDataSource<LocalGroupEntry> {
		private readonly LocalAccountsService _service;
		private readonly CancellationToken _lifetime;

		internal Inventory(LocalAccountsService service, CancellationToken lifetime) {
			_service = service;
			_lifetime = lifetime;
			DefaultSortProperty = nameof(LocalGroupEntry.Name);
		}

		internal Inventory(LocalAccountsService service, CancellationToken lifetime, IEnumerable<LocalGroupEntry> rows)
			: base(rows) {
			_service = service;
			_lifetime = lifetime;
			DefaultSortProperty = nameof(LocalGroupEntry.Name);
		}

		public LocalGroupEntry[] Rows => List.ToArray();

		public override DataSourceCapabilities Capabilities => DataSourceCapabilities.ReadOnly | DataSourceCapabilities.CanNotify | DataSourceCapabilities.CanCreate | DataSourceCapabilities.CanUpdate | DataSourceCapabilities.CanDelete;

		internal LocalGroupDataSourceBase Owner { get; set; }

		public override async Task RefreshRangeAsync(LocalGroupEntry[] entities) {
			Guard.ArgumentNotNull(entities, nameof(entities));
			for (var index = 0; index < entities.Length; index++) {
				var entity = entities[index];
				EnsureOwned(entity, true);
				var current = await _service.ReadGroupAsync(entity.Name, _lifetime).ConfigureAwait(false);
				Guard.Ensure(current != null && current.SID == entity.SID, "This group was removed or replaced. Refresh the inventory.");
				var original = entity.Origin ?? entity;
				original.Accept(current);
				entities[index] = original;
			}
			NotifyChanged();
		}

		public override async Task<Result> ValidateRangeAsync(IEnumerable<(LocalGroupEntry entity, CrudAction action)> actions) {
			var result = new Result();
			foreach (var (entity, action) in actions)
				result.Merge(await ValidateOneAsync(entity, action).ConfigureAwait(false));
			return result;
		}

		public override async Task CreateRangeAsync(IEnumerable<LocalGroupEntry> entities) {
			foreach (var entity in entities)
				await SaveAsync(entity, false).ConfigureAwait(false);
		}

		public override async Task UpdateRangeAsync(IEnumerable<LocalGroupEntry> entities) {
			foreach (var entity in entities)
				await SaveAsync(entity, true).ConfigureAwait(false);
		}

		public override async Task DeleteRangeAsync(IEnumerable<LocalGroupEntry> entities) {
			foreach (var entity in entities) {
				EnsureOwned(entity, true);
				await _service.DeleteGroupAsync(entity.Baseline, Owner.Logger, _lifetime).ConfigureAwait(false);
				var original = entity.Origin ?? entity;
				List.Remove(original);
				NotifyChanged();
			}
		}

		protected override LocalGroupEntry NewMethod() => new LocalGroupEntry { Owner = Owner };

		protected override string FormatValue(object value) => DataSourceFormatting.Value(value);

		protected override async Task<IEnumerable<LocalGroupEntry>> FetchAsync(CancellationToken cancellationToken) {
			var inventory = await _service.GetGroupsAsync(cancellationToken).ConfigureAwait(false);
			return inventory.Select(item => {
				var row = NewMethod();
				row.Accept(item);
				return row;
			}).ToArray();
		}

		private async Task<Result> ValidateOneAsync(LocalGroupEntry entity, CrudAction action) {
			var result = new Result();
			var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<InvalidOperationException>(async () => {
				// Validate the proposed values
				EnsureOwned(entity, action != CrudAction.Create);
				LocalAccountsService.ValidateName(entity.Name, true);
				LocalAccountsService.ValidateText(entity.Description, 256, "Description");

				// Check the group against Windows, then confirm
				var current = await _service.ReadGroupAsync(entity.Name, _lifetime).ConfigureAwait(false);
				if (action == CrudAction.Create && current != null)
					result.AddError("An account with this name already exists.");
				if (action != CrudAction.Create && !entity.Baseline.MatchesEditableState(current, action == CrudAction.Delete))
					result.AddError("The account changed or was replaced. Refresh it before applying changes.");
				if (result.IsSuccess && !await Owner.ConfirmAsync(action, entity).ConfigureAwait(false))
					result.AddError("The change was cancelled; no Windows account was changed.");
			}).ConfigureAwait(false);
			if (error != null)
				result.AddError(error.Message);
			return result;
		}

		private async Task SaveAsync(LocalGroupEntry entity, bool existing) {
			EnsureOwned(entity, existing);
			var saved = existing
				? await _service.UpdateGroupAsync(entity.Baseline, entity.Proposed(_service.HostName), Owner.Logger, _lifetime).ConfigureAwait(false)
				: await _service.CreateGroupAsync(entity.Proposed(_service.HostName), Owner.Logger, _lifetime).ConfigureAwait(false);
			var original = entity.Origin ?? entity;
			original.Accept(saved);
			if (!existing)
				List.Add(original);
			NotifyChanged();
		}

		private void EnsureOwned(LocalGroupEntry entity, bool existing) {
			Guard.ArgumentNotNull(entity, nameof(entity));
			Guard.Ensure(ReferenceEquals(entity.Owner, Owner), "This row belongs to a different account data source.");
			var original = entity.Origin ?? entity;
			Guard.Ensure(existing == original.IsPersisted, "Use Create for a new account or select an existing inventory row.");
			if (existing) {
				Guard.Ensure(List.Any(row => ReferenceEquals(row, original)), "This row is no longer in the inventory. Refresh and select it again.");
				Guard.Ensure(entity.Name == entity.Baseline.Name, "An existing account name cannot be changed.");
			}
		}
	}

	#endregion
}

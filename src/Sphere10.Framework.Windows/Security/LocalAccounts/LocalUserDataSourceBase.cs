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

/// <summary>Async CRUD over the complete user inventory, with local sorting and searching.</summary>
/// <remarks>
/// A template: subclasses confirm account changes in <see cref="ConfirmAsync"/> and report them to <see cref="Logger"/>; clients which compose a
/// source without deriving one use <see cref="ActionLocalUserDataSource"/>. Each refresh swaps in a fresh <see cref="Inventory"/>; edits change the
/// Windows account in the served inventory and update its rows in place.
/// </remarks>
public abstract class LocalUserDataSourceBase : PollingDataSource<LocalUserEntry, LocalUserDataSourceBase.Inventories> {

	/// <param name="service">The account service that reads and changes Windows accounts.</param>
	/// <param name="lifetime">Cancels the native operations of every inventory this source serves.</param>
	protected LocalUserDataSourceBase(LocalAccountsService service, CancellationToken lifetime = default)
		: base(new Inventories(Checked(service), lifetime)) {
		InternalDataSource.Owner = this;
	}

	public LocalUserEntry[] Rows => InternalDataSource.Current.Rows;

	/// <summary>The logger which records committed account changes.</summary>
	protected virtual ILogger Logger => new NoOpLogger();

	/// <summary>Confirms an account change; false cancels it.</summary>
	protected abstract Task<bool> ConfirmAsync(CrudAction action, LocalUserEntry entry);

	private static LocalAccountsService Checked(LocalAccountsService service) {
		Guard.ArgumentNotNull(service, nameof(service));
		return service;
	}

	#region Inner Types

	/// <summary>Refreshes the served inventory with a fresh capture of the local users.</summary>
	public sealed class Inventories : RefreshableDataSourceBase<LocalUserEntry, Inventory> {
		private readonly LocalAccountsService _service;
		private readonly CancellationToken _lifetime;

		internal Inventories(LocalAccountsService service, CancellationToken lifetime)
			: base(new Inventory(service, lifetime, Array.Empty<LocalUserEntry>()), true) {
			_service = service;
			_lifetime = lifetime;
		}

		/// <summary>The owning data source, assigned once by its constructor.</summary>
		internal LocalUserDataSourceBase Owner {
			get;
			set {
				field = value;
				Current.Owner = value;
			}
		}

		protected override Inventory CreateDataSource() => new Inventory(_service, _lifetime) { Owner = Owner };
	}

	/// <summary>One capture of the local users; account changes happen here and the rows follow.</summary>
	public sealed class Inventory : AsyncListDataSource<LocalUserEntry> {
		private readonly LocalAccountsService _service;
		private readonly CancellationToken _lifetime;

		internal Inventory(LocalAccountsService service, CancellationToken lifetime) {
			_service = service;
			_lifetime = lifetime;
			DefaultSortProperty = nameof(LocalUserEntry.Name);
		}

		internal Inventory(LocalAccountsService service, CancellationToken lifetime, IEnumerable<LocalUserEntry> rows)
			: base(rows) {
			_service = service;
			_lifetime = lifetime;
			DefaultSortProperty = nameof(LocalUserEntry.Name);
		}

		public LocalUserEntry[] Rows => List.ToArray();

		public override DataSourceCapabilities Capabilities => DataSourceCapabilities.ReadOnly | DataSourceCapabilities.CanNotify | DataSourceCapabilities.CanCreate | DataSourceCapabilities.CanUpdate | DataSourceCapabilities.CanDelete;

		internal LocalUserDataSourceBase Owner { get; set; }

		public override async Task RefreshRangeAsync(LocalUserEntry[] entities) {
			Guard.ArgumentNotNull(entities, nameof(entities));
			for (var index = 0; index < entities.Length; index++) {
				var entity = entities[index];
				EnsureOwned(entity, true);
				var current = await _service.ReadUserAsync(entity.Name, _lifetime).ConfigureAwait(false);
				Guard.Ensure(current != null && current.SID == entity.SID, "This user was removed or replaced. Refresh the inventory.");
				var original = entity.Origin ?? entity;
				original.Accept(current);
				entities[index] = original;
			}
			NotifyChanged();
		}

		public override async Task<Result> ValidateRangeAsync(IEnumerable<(LocalUserEntry entity, CrudAction action)> actions) {
			var result = new Result();
			foreach (var (entity, action) in actions)
				result.Merge(await ValidateOneAsync(entity, action).ConfigureAwait(false));
			return result;
		}

		public override async Task CreateRangeAsync(IEnumerable<LocalUserEntry> entities) {
			foreach (var entity in entities)
				await SaveAsync(entity, false).ConfigureAwait(false);
		}

		public override async Task UpdateRangeAsync(IEnumerable<LocalUserEntry> entities) {
			foreach (var entity in entities)
				await SaveAsync(entity, true).ConfigureAwait(false);
		}

		public override async Task DeleteRangeAsync(IEnumerable<LocalUserEntry> entities) {
			foreach (var entity in entities) {
				EnsureOwned(entity, true);
				await _service.DeleteUserAsync(entity.Baseline, Owner.Logger, _lifetime).ConfigureAwait(false);
				var original = entity.Origin ?? entity;
				List.Remove(original);
				NotifyChanged();
			}
		}

		protected override LocalUserEntry NewMethod() => new LocalUserEntry { Owner = Owner };

		protected override string FormatValue(object value) => DataSourceFormatting.Value(value);

		protected override async Task<IEnumerable<LocalUserEntry>> FetchAsync(CancellationToken cancellationToken) {
			var inventory = await _service.GetUsersAsync(cancellationToken).ConfigureAwait(false);
			return inventory.Select(item => {
				var row = NewMethod();
				row.Accept(item);
				return row;
			}).ToArray();
		}

		private async Task<Result> ValidateOneAsync(LocalUserEntry entity, CrudAction action) {
			var result = new Result();
			var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<InvalidOperationException>(async () => {
				// Validate the proposed values
				EnsureOwned(entity, action != CrudAction.Create);
				LocalAccountsService.ValidateName(entity.Name, false);
				LocalAccountsService.ValidateText(entity.Description, 256, "Description");
				LocalAccountsService.ValidateText(entity.FullName, 256, "Full name");
				if (action == CrudAction.Create && entity.PasswordRequired && string.IsNullOrEmpty(entity.InitialPassword))
					result.AddError("Enter an initial password or explicitly choose Password not required.");

				// Check the account against Windows, then confirm
				var current = await _service.ReadUserAsync(entity.Name, _lifetime).ConfigureAwait(false);
				if (action == CrudAction.Create && current != null)
					result.AddError("An account with this name already exists.");
				if (action != CrudAction.Create && !entity.Baseline.MatchesEditableState(current))
					result.AddError("The account changed or was replaced. Refresh it before applying changes.");
				if (result.IsSuccess && !await Owner.ConfirmAsync(action, entity).ConfigureAwait(false))
					result.AddError("The change was cancelled; no Windows account was changed.");
			}).ConfigureAwait(false);
			if (error != null)
				result.AddError(error.Message);
			return result;
		}

		private async Task SaveAsync(LocalUserEntry entity, bool existing) {
			EnsureOwned(entity, existing);
			using (Tools.Scope.ExecuteOnDispose(() => entity.InitialPassword = null)) {
				var saved = existing
					? await _service.UpdateUserAsync(entity.Baseline, entity.Proposed(_service.HostName), Owner.Logger, _lifetime).ConfigureAwait(false)
					: await _service.CreateUserAsync(entity.Proposed(_service.HostName), entity.InitialPassword, Owner.Logger, _lifetime).ConfigureAwait(false);
				var original = entity.Origin ?? entity;
				original.Accept(saved);
				if (!existing)
					List.Add(original);
			}
			NotifyChanged();
		}

		private void EnsureOwned(LocalUserEntry entity, bool existing) {
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

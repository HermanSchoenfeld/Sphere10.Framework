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
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework;

namespace Sphere10.Framework.Windows;

/// <summary>Uses native async environment writes and complete-snapshot search and sorting.</summary>
/// <remarks>
/// A template: subclasses confirm persistent changes in <see cref="ConfirmAsync"/> and report them to <see cref="Logger"/>; clients which compose a
/// source without deriving one use <see cref="ActionEnvironmentVariableDataSource"/>. Each refresh swaps in a fresh <see cref="Inventory"/>; edits
/// perform the native write in the served inventory and update its rows in place.
/// </remarks>
public abstract class EnvironmentVariableDataSourceBase : PollingDataSource<EnvironmentVariableEntry, EnvironmentVariableDataSourceBase.Inventories> {
	private const string PermissionMessage = "Windows denied permission to edit this environment scope. Machine variables normally require administrator rights; no elevation is requested automatically.";

	/// <param name="lifetime">Cancels the native operations of every inventory this source serves.</param>
	protected EnvironmentVariableDataSourceBase(CancellationToken lifetime = default)
		: base(new Inventories(lifetime)) {
		InternalDataSource.Owner = this;
	}

	public EnvironmentVariableEntry[] Rows => InternalDataSource.Current.Rows;

	/// <summary>The logger which records committed environment changes.</summary>
	protected virtual ILogger Logger => new NoOpLogger();

	/// <summary>Confirms a change to a persistent (User or Machine) scope; false cancels it. Process-scope changes are not confirmed here.</summary>
	protected abstract Task<bool> ConfirmAsync(CrudAction action, EnvironmentVariableEntry entry);

	#region Inner Types

	/// <summary>Refreshes the served inventory with a fresh capture of the environment.</summary>
	public sealed class Inventories : RefreshableDataSourceBase<EnvironmentVariableEntry, Inventory> {
		private readonly CancellationToken _lifetime;

		internal Inventories(CancellationToken lifetime)
			: base(new Inventory(lifetime, Array.Empty<EnvironmentVariableEntry>()), true) {
			_lifetime = lifetime;
		}

		/// <summary>The owning data source, assigned once by its constructor.</summary>
		internal EnvironmentVariableDataSourceBase Owner {
			get;
			set {
				field = value;
				Current.Owner = value;
			}
		}

		protected override Inventory CreateDataSource() => new Inventory(_lifetime) { Owner = Owner };
	}

	/// <summary>One capture of the environment with its editable raw values; native writes happen here and the rows follow.</summary>
	public sealed class Inventory : AsyncListDataSource<EnvironmentVariableEntry> {
		private readonly CancellationToken _lifetime;

		internal Inventory(CancellationToken lifetime) => _lifetime = lifetime;

		internal Inventory(CancellationToken lifetime, IEnumerable<EnvironmentVariableEntry> rows)
			: base(rows) {
			_lifetime = lifetime;
		}

		public EnvironmentVariableEntry[] Rows => List.ToArray();

		public override DataSourceCapabilities Capabilities => DataSourceCapabilities.ReadOnly | DataSourceCapabilities.CanNotify | DataSourceCapabilities.CanCreate | DataSourceCapabilities.CanUpdate | DataSourceCapabilities.CanDelete;

		internal EnvironmentVariableDataSourceBase Owner { get; set; }

		public override async Task RefreshRangeAsync(EnvironmentVariableEntry[] entities) {
			Guard.ArgumentNotNull(entities, nameof(entities));
			for (var index = 0; index < entities.Length; index++) {
				var entity = entities[index];
				EnsureOwned(entity, true);
				var original = entity.Origin ?? entity;
				var value = await Tools.Windows.Environment.ReadAsync(original.OriginalName, original.OriginalScope, _lifetime).ConfigureAwait(false);
				Guard.Ensure(value != null, "The selected variable was removed. Refresh the environment list.");
				original.Value = original.OriginalValue = value;
				entities[index] = original;
			}
			NotifyChanged();
		}

		public override async Task<Result> ValidateRangeAsync(IEnumerable<(EnvironmentVariableEntry entity, CrudAction action)> actions) {
			var result = new Result();
			foreach (var (entity, action) in actions)
				result.Merge(await ValidateOneAsync(entity, action).ConfigureAwait(false));
			return result;
		}

		public override async Task CreateRangeAsync(IEnumerable<EnvironmentVariableEntry> entities) {
			foreach (var entity in entities)
				await SaveAsync(entity, false).ConfigureAwait(false);
		}

		public override async Task UpdateRangeAsync(IEnumerable<EnvironmentVariableEntry> entities) {
			foreach (var entity in entities)
				await SaveAsync(entity, true).ConfigureAwait(false);
		}

		public override async Task DeleteRangeAsync(IEnumerable<EnvironmentVariableEntry> entities) {
			foreach (var entity in entities) {
				EnsureOwned(entity, true);
				var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
					var note = await Tools.Windows.Environment.DeleteAsync(entity.Name, entity.Scope, entity.OriginalValue, _lifetime).ConfigureAwait(false);
					var original = entity.Origin ?? entity;
					List.Remove(original);
					NotifyChanged();
					ReportChange("deleted", entity, note);
				}, capturedException => capturedException is UnauthorizedAccessException or SecurityException).ConfigureAwait(false);
				if (error != null)
					throw new InvalidOperationException(PermissionMessage, error);
			}
		}

		protected override EnvironmentVariableEntry NewMethod() => new EnvironmentVariableEntry { Owner = Owner };

		protected override string FormatValue(object value) => DataSourceFormatting.Value(value);

		protected override async Task<IEnumerable<EnvironmentVariableEntry>> FetchAsync(CancellationToken cancellationToken) {
			var rows = new List<EnvironmentVariableEntry>();
			foreach (var scope in new[] { EnvironmentVariableTarget.Process, EnvironmentVariableTarget.User, EnvironmentVariableTarget.Machine }) {
				// Capture the scope's variables
				cancellationToken.ThrowIfCancellationRequested();
				var captured = new List<EnvironmentVariableEntry>();
				var scopeError = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
					foreach (var variable in await Tools.Windows.Environment.ReadAllAsync(scope, cancellationToken).ConfigureAwait(false))
						captured.Add(new EnvironmentVariableEntry { Owner = Owner, Name = variable.Key?.ToString() ?? string.Empty, Value = variable.Value?.ToString() ?? string.Empty, Scope = scope });
				}, capturedException => capturedException is not OperationCanceledException).ConfigureAwait(false);
				if (scopeError != null) {
					// A scope Windows refuses still appears, as one row explaining why, so the inventory is never silently partial.
					rows.Add(new EnvironmentVariableEntry { Owner = Owner, Scope = scope, Availability = scopeError.Message });
					continue;
				}

				// Read each variable's exact stored value
				foreach (var row in captured) {
					cancellationToken.ThrowIfCancellationRequested();
					if (string.IsNullOrEmpty(row.Availability) && Tools.Windows.Environment.Validate(row.Name, string.Empty, scope).IsSuccess) {
						var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
							// Inventory strings can be expanded; editors and stale checks require the exact stored value.
							row.Value = await Tools.Windows.Environment.ReadAsync(row.Name, scope, cancellationToken).ConfigureAwait(false);
							if (row.Value != null)
								row.Accept(row);
						}, exception => exception is UnauthorizedAccessException or SecurityException).ConfigureAwait(false);
						if (error != null) {
							row.Availability = "Windows denied access to the raw environment value: " + error.Message;
						} else if (row.Value == null)
							continue;
					}
					rows.Add(row);
				}
			}
			cancellationToken.ThrowIfCancellationRequested();
			return rows.OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.Scope).ToArray();
		}

		private async Task<Result> ValidateOneAsync(EnvironmentVariableEntry entity, CrudAction action) {
			Guard.ArgumentNotNull(entity, nameof(entity));
			var validation = Tools.Windows.Environment.Validate(entity.Name, action == CrudAction.Delete ? string.Empty : entity.Value, entity.Scope);
			if (validation.IsFailure)
				return validation;
			var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
				EnsureOwned(entity, action != CrudAction.Create);
				var current = await Tools.Windows.Environment.ReadAsync(entity.Name, entity.Scope, _lifetime).ConfigureAwait(false);
				if (action == CrudAction.Create && current != null)
					validation.AddError("A variable with this name already exists in that scope. Refresh and edit that variable instead.");
				else if (action != CrudAction.Create && !string.Equals(current, entity.OriginalValue, StringComparison.Ordinal))
					validation.AddError("The variable changed or was removed since it was opened. Refresh and edit its current value.");
				// VisualGrid already confirms deletion. Only persistent scopes need an additional Windows lifetime/permission explanation.
				if (validation.IsSuccess && entity.Scope != EnvironmentVariableTarget.Process && !await Owner.ConfirmAsync(action, entity).ConfigureAwait(false))
					validation.AddError("The change was cancelled. No environment value was changed.");
			}, exception => exception is UnauthorizedAccessException or SecurityException or InvalidOperationException).ConfigureAwait(false);
			if (error != null)
				validation.AddError(error is InvalidOperationException ? error.Message : PermissionMessage);
			return validation;
		}

		private async Task SaveAsync(EnvironmentVariableEntry entity, bool existing) {
			EnsureOwned(entity, existing);
			var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
				var note = await Tools.Windows.Environment.SetAsync(entity.Name, entity.Value, entity.Scope, existing ? entity.OriginalValue : null, _lifetime).ConfigureAwait(false);
				var original = entity.Origin ?? entity;
				original.Accept(entity);
				if (!existing)
					List.Add(original);
				NotifyChanged();
				ReportChange("saved", entity, note);
			}, capturedException => capturedException is UnauthorizedAccessException or SecurityException).ConfigureAwait(false);
			if (error != null)
				throw new InvalidOperationException(PermissionMessage, error);
		}

		private void EnsureOwned(EnvironmentVariableEntry entity, bool existing) {
			Guard.ArgumentNotNull(entity, nameof(entity));
			Guard.Ensure(ReferenceEquals(entity.Owner, Owner), "This environment entry belongs to a different data source.");
			var original = entity.Origin ?? entity;
			Guard.Ensure(existing == original.IsPersisted, "Refresh and select an existing variable, or use Create for a new variable.");
			Guard.Ensure(string.IsNullOrEmpty(original.Availability), "This inventory row does not contain an editable environment value.");
			if (existing) {
				Guard.Ensure(List.Any(row => ReferenceEquals(row, original)), "The selected variable is no longer in this inventory. Refresh the environment list.");
				Guard.Ensure(entity.Name == entity.OriginalName && entity.Scope == entity.OriginalScope, "An existing variable's name and scope cannot be changed.");
			}
		}

		private void ReportChange(string action, EnvironmentVariableEntry entity, string note) {
			var logger = Owner.Logger ?? new NoOpLogger();
			logger.Info($"Environment: {action} {entity.Scope} variable {entity.Name}.");
			if (!string.IsNullOrEmpty(note))
				logger.Warning(note);
		}
	}

	#endregion
}

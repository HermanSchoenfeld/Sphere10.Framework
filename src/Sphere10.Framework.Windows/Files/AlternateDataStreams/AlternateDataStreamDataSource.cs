// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework;

namespace Sphere10.Framework.Windows.Files;

/// <summary>Typed CRUD source over one file's named streams; native stream operations belong to Tools.Windows.Files.</summary>
/// <remarks>
/// Each load or refresh swaps in a fresh <see cref="Inventory"/> of the requested file's named streams; stream edits happen in the served inventory
/// and its rows follow. Subclasses report committed changes to their own <see cref="Logger"/>.
/// </remarks>
public class AlternateDataStreamDataSource : PollingDataSource<AlternateDataStreamEntry, AlternateDataStreamDataSource.Inventories> {

	/// <param name="lifetime">Cancels the native operations of every inventory this source serves.</param>
	public AlternateDataStreamDataSource(CancellationToken lifetime = default)
		: base(new Inventories(lifetime)) {
		InternalDataSource.Owner = this;
	}

	/// <summary>The file whose streams are served, or null before the first load.</summary>
	public string FilePath => InternalDataSource.Current.FilePath;

	/// <summary>The next explicitly selected file. Changing it does not perform IO or alter the accepted inventory.</summary>
	public string RequestedFilePath {
		get => InternalDataSource.RequestedFilePath;
		set => InternalDataSource.RequestedFilePath = value;
	}

	public AlternateDataStreamEntry[] Rows => InternalDataSource.Current.Rows;

	/// <summary>The logger which records committed stream changes and exports.</summary>
	protected virtual ILogger Logger => new NoOpLogger();

	/// <summary>Serves the named streams of the given file once they are listed; the previous inventory stays readable until then.</summary>
	public Task LoadFileAsync(string filePath, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNullOrEmpty(filePath, nameof(filePath));
		RequestedFilePath = filePath;
		return RefreshAsync(cancellationToken);
	}

	public Task<byte[]> ReadBytesAsync(AlternateDataStreamEntry entry) => InternalDataSource.Current.ReadBytesAsync(entry);

	public Task<(byte[] Bytes, long Length)> ReadPreviewAsync(AlternateDataStreamEntry entry) => InternalDataSource.Current.ReadPreviewAsync(entry);

	/// <summary>Streams the full raw data to a regular file while preventing the primary file, including its aliases, from opening for writing.</summary>
	public Task ExportAsync(AlternateDataStreamEntry entry, string destinationPath) => InternalDataSource.Current.ExportAsync(entry, destinationPath);

	#region Native stream operations

	/// <summary>Lists the named streams of a file. A subclass overrides these hooks to route the native stream operations elsewhere.</summary>
	protected internal virtual Task<WindowsAlternateStreamInfo[]> GetAlternateStreamsAsync(string filePath, CancellationToken cancellationToken)
		=> Tools.Windows.Files.GetAlternateStreamsAsync(filePath, cancellationToken);

	protected internal virtual FileStream OpenAlternateStream(string filePath, string streamName, FileMode mode, FileAccess access)
		=> Tools.Windows.Files.OpenAlternateStream(filePath, streamName, mode, access);

	protected internal virtual Task<byte[]> ReadAlternateStreamAsync(string filePath, string streamName, int maximumBytes, CancellationToken cancellationToken)
		=> Tools.Windows.Files.ReadAlternateStreamAsync(filePath, streamName, maximumBytes, cancellationToken);

	protected internal virtual Task WriteAlternateStreamAsync(string filePath, string streamName, byte[] data, bool createNew, CancellationToken cancellationToken)
		=> Tools.Windows.Files.WriteAlternateStreamAsync(filePath, streamName, data, createNew, cancellationToken);

	protected internal virtual Task DeleteAlternateStreamAsync(string filePath, string streamName, CancellationToken cancellationToken)
		=> Tools.Windows.Files.DeleteAlternateStreamAsync(filePath, streamName, cancellationToken);

	#endregion

	#region Inner Types

	/// <summary>Refreshes the served inventory from the requested file, or the loaded file again when none is requested.</summary>
	public sealed class Inventories : RefreshableDataSourceBase<AlternateDataStreamEntry, Inventory> {
		private readonly CancellationToken _lifetime;
		private readonly AsyncSynchronizedGate _operations = new();
		private volatile string _requestedFilePath;

		internal Inventories(CancellationToken lifetime)
			: base(new Inventory(lifetime, new AsyncSynchronizedGate())) {
			_lifetime = lifetime;
		}

		public string RequestedFilePath {
			get => _requestedFilePath;
			set => _requestedFilePath = value;
		}

		/// <summary>The owning data source, assigned once by its constructor.</summary>
		internal AlternateDataStreamDataSource Owner {
			get;
			set {
				field = value;
				Current.Owner = value;
			}
		}

		protected override Inventory CreateDataSource() {
			var path = _requestedFilePath ?? Current.FilePath;
			var inventory = path == null
				? new Inventory(_lifetime, _operations)
				: new Inventory(_lifetime, _operations, Path.GetFullPath(path));
			inventory.Owner = Owner;
			return inventory;
		}
	}

	/// <summary>The named streams of one primary file; stream operations serialize on the owner's gate and native changes update the rows.</summary>
	public sealed class Inventory : AsyncListDataSource<AlternateDataStreamEntry> {
		private readonly CancellationToken _lifetime;
		private readonly AsyncSynchronizedGate _operations;

		/// <summary>The empty inventory served before a load.</summary>
		internal Inventory(CancellationToken lifetime, AsyncSynchronizedGate operations)
			: base(Array.Empty<AlternateDataStreamEntry>()) {
			_lifetime = lifetime;
			_operations = operations;
		}

		/// <summary>The named streams of one primary file, listed on load.</summary>
		internal Inventory(CancellationToken lifetime, AsyncSynchronizedGate operations, string filePath) {
			Guard.ArgumentNotNullOrEmpty(filePath, nameof(filePath));
			_lifetime = lifetime;
			_operations = operations;
			FilePath = filePath;
		}

		/// <summary>The primary file, or null for the empty inventory served before a load.</summary>
		public string FilePath { get; }

		public AlternateDataStreamEntry[] Rows => List.ToArray();

		public override DataSourceCapabilities Capabilities => DataSourceCapabilities.ReadOnly | DataSourceCapabilities.CanNotify | DataSourceCapabilities.CanCreate | DataSourceCapabilities.CanUpdate | DataSourceCapabilities.CanDelete;

		internal AlternateDataStreamDataSource Owner { get; set; }

		private ILogger Logger => Owner?.Logger ?? new NoOpLogger();

		public Task<byte[]> ReadBytesAsync(AlternateDataStreamEntry entry) {
			return Task.Run(async () => {
				Debug.AssertNotMainThread();
				using var operation = await _operations.EnterScopeAsync(_lifetime);
				EnsureOwned(entry, true);
				return await Owner.ReadAlternateStreamAsync(entry.FilePath, entry.Name, AlternateDataStreamContent.MaximumEditBytes, _lifetime);
			}, _lifetime);
		}

		public Task<(byte[] Bytes, long Length)> ReadPreviewAsync(AlternateDataStreamEntry entry) {
			return Task.Run(async () => {
				Debug.AssertNotMainThread();
				using var operation = await _operations.EnterScopeAsync(_lifetime);
				EnsureOwned(entry, true);
				await using var stream = Owner.OpenAlternateStream(entry.FilePath, entry.Name, FileMode.Open, FileAccess.Read);
				var length = stream.Length;
				var bytes = new byte[(int)Math.Min(length, AlternateDataStreamContent.MaximumPreviewBytes)];
				await stream.ReadExactlyAsync(bytes, _lifetime);
				return (bytes, length);
			}, _lifetime);
		}

		/// <summary>Streams the full raw data to a regular file while preventing the primary file, including its aliases, from opening for writing.</summary>
		public Task ExportAsync(AlternateDataStreamEntry entry, string destinationPath) {
			return Task.Run(async () => {
				// Validate the destination
				Debug.AssertNotMainThread();
				using var operation = await _operations.EnterScopeAsync(_lifetime);
				EnsureOwned(entry, true);
				Guard.ArgumentNotNullOrEmpty(destinationPath, nameof(destinationPath));
				var destination = Path.GetFullPath(destinationPath);
				Guard.Ensure(destination[Path.GetPathRoot(destination).Length..].IndexOf(':') < 0, "Choose a regular export file, not another named stream.");
				Guard.Ensure(!string.Equals(destination, entry.FilePath, StringComparison.OrdinalIgnoreCase), "Export must not overwrite the primary file.");

				// Share only reads until output creation finishes and copying completes. A hard link to this same primary file also fails Windows' write-sharing check before truncation.
				using var primary = File.OpenHandle(entry.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
				await using var input = Owner.OpenAlternateStream(entry.FilePath, entry.Name, FileMode.Open, FileAccess.Read);
				await using var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.Asynchronous);
				await input.CopyToAsync(output, 64 * 1024, _lifetime);
				await output.FlushAsync(_lifetime);
				Logger.Info($"Named streams: exported named stream '{entry.Name}' to '{destination}'.");
			}, _lifetime);
		}

		public override Task RefreshRangeAsync(AlternateDataStreamEntry[] entities) {
			return NotifyAfterOperationAsync(committed => Task.Run(async () => {
				Debug.AssertNotMainThread();
				Guard.ArgumentNotNull(entities, nameof(entities));
				using var operation = await _operations.EnterScopeAsync(_lifetime);
				for (var index = 0; index < entities.Length; index++) {
					var entity = entities[index];
					EnsureOwned(entity, true);
					var original = entity.Origin ?? entity;
					var bytes = await Owner.ReadAlternateStreamAsync(original.FilePath, original.Name, AlternateDataStreamContent.MaximumEditBytes, _lifetime);
					original.Baseline = bytes;
					SetContent(original, bytes);
					original.Length = bytes.LongLength;
					entities[index] = original;
				}
				committed();
			}, _lifetime));
		}

		public override async Task<Result> ValidateRangeAsync(IEnumerable<(AlternateDataStreamEntry entity, CrudAction action)> actions) {
			var result = new Result();
			foreach (var (entity, action) in actions)
				result.Merge(await ValidateOneAsync(entity, action).ConfigureAwait(false));
			return result;
		}

		public override async Task CreateRangeAsync(IEnumerable<AlternateDataStreamEntry> entities) {
			foreach (var entity in entities)
				await CreateOneAsync(entity).ConfigureAwait(false);
		}

		public override async Task UpdateRangeAsync(IEnumerable<AlternateDataStreamEntry> entities) {
			foreach (var entity in entities)
				await UpdateOneAsync(entity).ConfigureAwait(false);
		}

		public override async Task DeleteRangeAsync(IEnumerable<AlternateDataStreamEntry> entities) {
			foreach (var entity in entities)
				await DeleteOneAsync(entity).ConfigureAwait(false);
		}

		protected override AlternateDataStreamEntry NewMethod() {
			Guard.Ensure(FilePath != null, "Load a primary file before adding a named stream.");
			return new AlternateDataStreamEntry { Owner = Owner, FilePath = FilePath };
		}

		protected override string FormatValue(object value) => DataSourceFormatting.Value(value);

		protected override async Task<IEnumerable<AlternateDataStreamEntry>> FetchAsync(CancellationToken cancellationToken) {
			Debug.AssertNotMainThread();
			using var linked = CancellationTokenSource.CreateLinkedTokenSource(_lifetime, cancellationToken);
			using var operation = await _operations.EnterScopeAsync(linked.Token).ConfigureAwait(false);
			var streams = await Owner.GetAlternateStreamsAsync(FilePath, linked.Token).ConfigureAwait(false);
			linked.Token.ThrowIfCancellationRequested();
			return streams.Select(stream => new AlternateDataStreamEntry {
				Owner = Owner, FilePath = FilePath, Name = stream.Name, OriginalName = stream.Name, Length = stream.Length, IsPersisted = true
			}).ToArray();
		}

		private Task<Result> ValidateOneAsync(AlternateDataStreamEntry entity, CrudAction action) {
			return Task.Run(async () => {
				Debug.AssertNotMainThread();
				var result = Result.Default;
				var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
					// Validate the proposed stream
					using var operation = await _operations.EnterScopeAsync(_lifetime);
					EnsureOwned(entity, action != CrudAction.Create);
					ValidateName(entity.Name);
					if (action != CrudAction.Delete)
						EncodeContent(entity);

					// Check it against the file's current streams
					var streams = await Owner.GetAlternateStreamsAsync(FilePath, _lifetime);
					var current = streams.FirstOrDefault(stream => string.Equals(stream.Name, entity.Name, StringComparison.OrdinalIgnoreCase));
					if (action == CrudAction.Create)
						Guard.Ensure(current == null, "A stream with this name already exists. Refresh and edit the current stream.");
					else {
						Guard.Ensure(current != null && current.Length == entity.Length, "The stream changed or was removed. Refresh its current contents before changing it.");
						if (action == CrudAction.Update)
							Guard.Ensure(entity.Baseline != null, "Load the stream's bytes before editing it.");
						if (entity.Baseline != null) {
							var bytes = await Owner.ReadAlternateStreamAsync(FilePath, entity.Name, AlternateDataStreamContent.MaximumEditBytes, _lifetime);
							Guard.Ensure(bytes.SequenceEqual(entity.Baseline), "The stream changed since the editor opened. Refresh its current contents before changing it.");
						}
					}
				}, capturedException => capturedException is ArgumentException or InvalidOperationException or IOException);
				if (error != null)
					result.AddError(error.Message);
				return result;
			}, _lifetime);
		}

		private Task CreateOneAsync(AlternateDataStreamEntry entity) {
			return NotifyAfterOperationAsync(committed => Task.Run(async () => {
				Debug.AssertNotMainThread();
				using var operation = await _operations.EnterScopeAsync(_lifetime);
				EnsureOwned(entity, false);
				ValidateName(entity.Name);
				var bytes = EncodeContent(entity);
				await Owner.WriteAlternateStreamAsync(FilePath, entity.Name, bytes, createNew: true, _lifetime);
				Accept(entity, bytes);
				List.Add(entity.Origin ?? entity);
				committed();
				Log("created", entity);
			}, _lifetime));
		}

		private Task UpdateOneAsync(AlternateDataStreamEntry entity) {
			return NotifyAfterOperationAsync(committed => Task.Run(async () => {
				Debug.AssertNotMainThread();
				using var operation = await _operations.EnterScopeAsync(_lifetime);
				EnsureOwned(entity, true);
				Guard.Ensure(entity.Baseline != null, "Load the stream's bytes before editing it.");
				var bytes = EncodeContent(entity);
				// Hold the writable stream while comparing and replacing, so a competing writer cannot alter this baseline between reads and writes.
				await using (var stream = Owner.OpenAlternateStream(FilePath, entity.Name, FileMode.Open, FileAccess.ReadWrite)) {
					Guard.Ensure(stream.Length == entity.Baseline.Length, "The stream changed since the editor opened. Refresh and edit its current contents.");
					var current = new byte[entity.Baseline.Length];
					await stream.ReadExactlyAsync(current, _lifetime);
					Guard.Ensure(current.SequenceEqual(entity.Baseline), "The stream changed since the editor opened. Refresh and edit its current contents.");
					stream.Position = 0;
					await stream.WriteAsync(bytes, _lifetime);
					stream.SetLength(bytes.Length);
					await stream.FlushAsync(_lifetime);
				}
				Accept(entity, bytes);
				committed();
				Log("updated", entity);
			}, _lifetime));
		}

		private Task DeleteOneAsync(AlternateDataStreamEntry entity) {
			return NotifyAfterOperationAsync(committed => Task.Run(async () => {
				// Check the stream is unchanged
				Debug.AssertNotMainThread();
				using var operation = await _operations.EnterScopeAsync(_lifetime);
				EnsureOwned(entity, true);
				var current = (await Owner.GetAlternateStreamsAsync(FilePath, _lifetime)).FirstOrDefault(stream => string.Equals(stream.Name, entity.Name, StringComparison.OrdinalIgnoreCase));
				Guard.Ensure(current != null && current.Length == entity.Length, "The stream changed or was removed. Refresh before deleting it.");
				if (entity.Baseline != null) {
					var bytes = await Owner.ReadAlternateStreamAsync(FilePath, entity.Name, AlternateDataStreamContent.MaximumEditBytes, _lifetime);
					Guard.Ensure(bytes.SequenceEqual(entity.Baseline), "The stream changed since its contents were loaded. Refresh before deleting it.");
				}

				// Delete it
				await Owner.DeleteAlternateStreamAsync(FilePath, entity.Name, _lifetime);
				List.Remove(entity.Origin ?? entity);
				committed();
				Log("removed", entity);
			}, _lifetime));
		}

		private async Task NotifyAfterOperationAsync(Func<Action, Task> operation) {
			var committed = false;
			using var notification = Tools.Scope.ExecuteOnDispose(() => {
				if (committed)
					NotifyChanged();
			});
			await operation(() => committed = true).ConfigureAwait(false);
		}

		private void EnsureOwned(AlternateDataStreamEntry entity, bool existing) {
			Guard.ArgumentNotNull(entity, nameof(entity));
			Guard.Ensure(ReferenceEquals(entity.Owner, Owner) && string.Equals(entity.FilePath, FilePath, StringComparison.OrdinalIgnoreCase), "This stream belongs to a different file or data source. Load and select its current file.");
			var original = entity.Origin ?? entity;
			Guard.Ensure(original.IsPersisted == existing, "Use Add for a new stream, or select an existing stream.");
			if (existing) {
				Guard.Ensure(List.Any(row => ReferenceEquals(row, original)), "This stream is no longer in the loaded inventory. Refresh and select its current row.");
				Guard.Ensure(entity.Name == original.OriginalName, "An existing stream cannot be renamed in the editor.");
			}
		}

		private static void ValidateName(string name)
			=> Guard.Argument(!string.IsNullOrWhiteSpace(name) && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0, nameof(name), "Enter a named stream without colons, slashes or invalid filename characters.");

		private static void Accept(AlternateDataStreamEntry changes, byte[] bytes) {
			var original = changes.Origin ?? changes;
			original.Name = original.OriginalName = changes.Name;
			SetContent(original, bytes);
			original.Length = bytes.LongLength;
			original.Baseline = bytes;
			original.IsPersisted = true;
		}

		private static byte[] EncodeContent(AlternateDataStreamEntry entity) {
			Guard.Ensure(string.IsNullOrEmpty(entity.ValidationError), entity.ValidationError);
			var bytes = entity.RawBytes ?? AlternateDataStreamContent.Encode(entity.Text, entity.HasUtf8Bom);
			Guard.Ensure(bytes.Length <= AlternateDataStreamContent.MaximumEditBytes, "Editing is limited to 16 MiB. Inspect previews or Export Stream retains the complete raw data.");
			return bytes;
		}

		private static void SetContent(AlternateDataStreamEntry entity, byte[] bytes) {
			var text = AlternateDataStreamContent.TryDecode(bytes, out var decoded, out var bom);
			entity.Text = text ? decoded : null;
			entity.HasUtf8Bom = text && bom;
			entity.RawBytes = text ? null : bytes;
		}

		private void Log(string action, AlternateDataStreamEntry entry) => Logger.Info($"Named streams: {action} named stream '{entry.Name}' on '{entry.FilePath}'.");
	}

	#endregion
}

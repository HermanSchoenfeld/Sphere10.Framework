// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Exercises real collection backends, bounded writes and explicit transaction boundaries.</summary>
public partial class TransactionalCollectionScreen : ApplicationScreen {
	private CancellationTokenSource _operationCancellation;

	/// <summary>Creates a workbench with no files, collection sessions or background work.</summary>
	public TransactionalCollectionScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		Disposables.Add(_outputTextBox.Font);
		_listTypeComboBox.EnumType = typeof(ListType);
		_listTypeComboBox.SelectedEnum = ListType.Transactional;
		_policyBox.EnumType = typeof(ClusteredStreamsPolicy);
		_policyBox.SelectedEnum = ClusteredStreamsPolicy.Default;
		UpdateAvailability();
	}

	/// <summary>The preferences declared by this screen type and restored for its exact registration.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override TransactionalCollectionScreenSettings ScreenSettings => (TransactionalCollectionScreenSettings)base.ScreenSettings;

	// Preferences restore before the user creates a temporary storage session.

	// Options configures the next operation without changing a live worker request.
	protected override void CopyModelToUI() {
		base.CopyModelToUI();
		ApplyPreferences();
	}

	// Local worker resources unwind on their owner after cancellation.
	protected override void OnDestroyScreen() {
		_operationCancellation?.Cancel();
		base.OnDestroyScreen();
	}

	// Each native action shares the same admission, capture, cancellation and result path.
	private async void ListClicked(object sender, EventArgs args) => await RunAsync(Scenario.List);

	// Buffer work is enabled only for actual buffer backends.
	private async void BufferClicked(object sender, EventArgs args) => await RunAsync(Scenario.Buffer);

	// Dictionary storage is explicitly transactional, independent of the list/backend selector.
	private async void DictionaryClicked(object sender, EventArgs args) => await RunAsync(Scenario.Dictionary);

	// Clustered streams retain the original append scenario with configured geometry.
	private async void StreamClicked(object sender, EventArgs args) => await RunAsync(Scenario.Stream);

	// Verify persisted membership, rollback and reopened data rather than a warmed cache.
	private async void RollbackClicked(object sender, EventArgs args) => await RunAsync(Scenario.Rollback);

	// Cancellation remains available while every other operation is disabled.
	private void CancelClicked(object sender, EventArgs args) => _operationCancellation?.Cancel();

	// Copy generated results only when a nonempty result exists.
	private void CopyClicked(object sender, EventArgs args) {
		if (_outputTextBox.TextLength > 0)
			Clipboard.SetText(_outputTextBox.Text);
	}

	// Clear does not reset the chosen backend or sizing preferences.
	private void ClearClicked(object sender, EventArgs args) {
		_outputTextBox.Clear();
		_status.Text = "Results cleared.";
	}

	// Capture all native inputs before dispatch; no background code reads a control.
	private async Task RunAsync(Scenario scenario) {
		if (_operationCancellation != null)
			return;
		try {
			var request = CaptureRequest();
			using var cancellation = new CancellationTokenSource();
			_operationCancellation = cancellation;
			UpdateAvailability();
			_status.Text = $"Running {scenario} with {request.Items:N0} items…";
			try {
				var result = await Task.Run(() => Execute(request, scenario, cancellation.Token), cancellation.Token);
				if (!IsDisposed && !Disposing) {
					_outputTextBox.Text = result;
					_status.Text = "Operation complete. Its owned temporary files have been released.";
				}
			} finally {
				_operationCancellation = null;
				if (!IsDisposed && !Disposing)
					UpdateAvailability();
			}
		} catch (OperationCanceledException) {
			if (!IsDisposed && !Disposing)
				_status.Text = "Operation cancelled; the worker released its collection and temporary files.";
		} catch (Exception error) {
			SystemLog.Exception(error, "The collection demonstration failed.");
			if (!IsDisposed && !Disposing)
				_status.Text = "Could not complete the operation: " + error.Message;
		}
	}

	// Cross-field limits keep this interactive example bounded; performance studies belong in the profiler.
	private Request CaptureRequest() {
		var request = new Request(
			Positive(_itemsIntBox.Value, "Items"), Positive(_batchIntBox.Value, "Batch size"), Positive(_itemSizeIntBox.Value, "Item size"),
			Positive(_clusterSizeIntBox.Value, "Cluster bytes"), Positive(_pageSizeIntBox.Value, "Page bytes"), Positive(_cacheSizeIntBox.Value, "Cache bytes"),
			(ListType)_listTypeComboBox.SelectedEnum, (ClusteredStreamsPolicy)_policyBox.SelectedEnum, _commitCheckBox.Checked
		);
		if (request.Items > 100000 || request.Batch > 10000 || request.ItemSize > 65536 || (long)request.Items * request.ItemSize > 64 * 1024 * 1024)
			throw new InvalidOperationException("Use at most 100,000 items, 10,000 per batch, 65,536 bytes per item and 64 MiB of payload.");
		if (request.PageSize > 16 * 1024 * 1024 || request.CacheSize < request.PageSize || request.CacheSize > 512 * 1024 * 1024)
			throw new InvalidOperationException("Choose a page up to 16 MiB and a cache between one page and 512 MiB.");
		if (request.Policy.HasFlag(ClusteredStreamsPolicy.MetaData))
			throw new InvalidOperationException("This raw collection example does not configure a metadata blob. Use the ObjectSpace example for self-describing storage.");
		return request;
	}

	// Every file and transactional page is created beneath this one owned, verified temporary directory.
	private static string Execute(Request request, Scenario scenario, CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		var temporaryRoot = Path.GetFullPath(Path.GetTempPath());
		var directoryName = "Sphere10.CollectionDemo-" + Guid.NewGuid().ToString("N");
		var directory = Path.Combine(temporaryRoot, directoryName);
		Directory.CreateDirectory(directory);
		try {
			var pages = Path.Combine(directory, "pages");
			Directory.CreateDirectory(pages);
			var descriptor = Sphere10FrameworkFileDescriptor.From(Path.Combine(directory, "data.s10"), pages, request.PageSize, request.CacheSize, request.ClusterSize, request.Policy);
			return scenario switch {
				Scenario.List => RunList(request, descriptor, cancellationToken),
				Scenario.Buffer => RunBuffer(request, descriptor, cancellationToken),
				Scenario.Dictionary => RunDictionary(request, descriptor, cancellationToken),
				Scenario.Stream => RunStream(request, descriptor, cancellationToken),
				Scenario.Rollback => VerifyTransactions(descriptor, cancellationToken),
				_ => throw new InvalidOperationException("Select a supported scenario.")
			};
		} finally {
			var resolved = Path.GetFullPath(directory);
			Guard.Ensure(string.Equals(Path.GetDirectoryName(resolved), Path.TrimEndingDirectorySeparator(temporaryRoot), StringComparison.OrdinalIgnoreCase)
				&& string.Equals(Path.GetFileName(resolved), directoryName, StringComparison.Ordinal), "Unexpected collection-demo cleanup target.");
			if (Directory.Exists(resolved))
				Directory.Delete(resolved, true);
		}
	}

	// The selected list and stream both remain on this worker for their complete lifetime.
	private static string RunList(Request request, Sphere10FrameworkFileDescriptor descriptor, CancellationToken token) {
		if (request.Backend == ListType.Transactional) {
			using var list = new TransactionalList<byte[]>(descriptor, ByteArraySerializer.Instance);
			if (list.RequiresLoad)
				list.Load();
			return AppendList(list, list, request, token);
		}

		var (stream, transaction) = CreateStream(request, descriptor);
		using (stream) {
			using var list = request.Backend == ListType.MerkleizedList
				? (IStreamMappedList<byte[]>)new StreamMappedMerkleList<byte[]>(stream, CHF.SHA2_256, request.ClusterSize, ByteArraySerializer.Instance, policy: request.Policy)
				: StreamMappedFactory.CreateList(stream, request.ClusterSize, ByteArraySerializer.Instance, policy: request.Policy);
			if (list.RequiresLoad)
				list.Load();
			return AppendList(list, transaction, request, token);
		}
	}

	// Batches report real append/commit durations and a verified final collection count.
	private static string AppendList(IExtendedList<byte[]> list, ITransactionalObject transaction, Request request, CancellationToken token) {
		var output = new StringBuilder();
		var total = Stopwatch.StartNew();
		var processed = 0;
		var reports = 0;
		foreach (var batch in Tools.Collection.Partition(request.Items, request.Batch)) {
			token.ThrowIfCancellationRequested();
			var clock = Stopwatch.StartNew();
			list.AddRange(Enumerable.Range(0, batch).Select(_ => new byte[request.ItemSize]).ToArray());
			if (request.Commit && transaction != null)
				transaction.Commit();
			processed += batch;
			if (reports++ < 100)
				output.AppendLine($"Appended {batch:N0}; total {processed:N0}; append/commit {clock.Elapsed.TotalMilliseconds:N3} ms.");
		}
		output.AppendLine($"Verified count: {list.Count:N0}. Payload: {(long)request.Items * request.ItemSize:N0} bytes. Total {total.Elapsed.TotalMilliseconds:N3} ms.");
		output.AppendLine($"Backend: {request.Backend}; commit each batch requested: {request.Commit}; transactional backend: {transaction != null}.");
		if (reports > 100)
			output.AppendLine("Only the first 100 batch reports are displayed.");
		return output.ToString();
	}

	// Buffer runs keep the original paging/file choices but never leave their disposable buffers open.
	private static string RunBuffer(Request request, Sphere10FrameworkFileDescriptor descriptor, CancellationToken token) {
		var buffer = CreateBuffer(request, descriptor);
		try {
			if (buffer is ILoadable loadable && loadable.RequiresLoad)
				loadable.Load();
			var clock = Stopwatch.StartNew();
			foreach (var batch in Tools.Collection.Partition(request.Items, request.Batch)) {
				token.ThrowIfCancellationRequested();
				for (var index = 0; index < batch; index++)
					buffer.AddRange(new byte[request.ItemSize]);
				if (request.Commit && buffer is ITransactionalObject transaction)
					transaction.Commit();
			}
			return $"Buffer bytes: {buffer.Count:N0}\r\nExpected payload: {(long)request.Items * request.ItemSize:N0} bytes\r\nBackend: {request.Backend}\r\nTotal: {clock.Elapsed.TotalMilliseconds:N3} ms";
		} finally {
			(buffer as IDisposable)?.Dispose();
		}
	}

	// Dictionary keys are deterministic SHA-256 values and its committed/read counts are checked.
	private static string RunDictionary(Request request, Sphere10FrameworkFileDescriptor descriptor, CancellationToken token) {
		token.ThrowIfCancellationRequested();

		// Own the file before dictionary activation so a rejected serializer/configuration still releases its handle.
		using var root = new TransactionalStream(descriptor, FileAccessMode.OpenOrCreate);
		if (root.RequiresLoad)
			root.Load();
		token.ThrowIfCancellationRequested();
		using var dictionary = new TransactionalDictionary<byte[], byte[]>(
			root,
			new ConstantSizeByteArraySerializer(Hashers.GetDigestSizeBytes(CHF.SHA2_256)),
			ByteArraySerializer.Instance.AsNullableSerializer(),
			keyComparer: ByteArrayEqualityComparer.Instance,
			valueComparer: ByteArrayEqualityComparer.Instance,
			clusterSize: request.ClusterSize,
			policy: request.Policy
		);
		if (dictionary.RequiresLoad)
			dictionary.Load();
		var clock = Stopwatch.StartNew();
		var next = 0;
		foreach (var batch in Tools.Collection.Partition(request.Items, request.Batch)) {
			token.ThrowIfCancellationRequested();
			for (var index = 0; index < batch; index++) {
				var key = Hashers.Hash(CHF.SHA2_256, Encoding.UTF8.GetBytes($"Key {next++}"));
				dictionary.Add(key, new byte[request.ItemSize]);
			}
			if (request.Commit)
				dictionary.Commit();
		}
		return $"Transactional dictionary count: {dictionary.Count:N0}\r\nExpected items: {request.Items:N0}\r\nCommit each batch: {request.Commit}\r\nTotal: {clock.Elapsed.TotalMilliseconds:N3} ms";
	}

	// A single stream uses the configured clustered/file geometry and reports its actual byte length.
	private static string RunStream(Request request, Sphere10FrameworkFileDescriptor descriptor, CancellationToken token) {
		using var buffer = new TransactionalFileMappedBuffer(TransactionalFileDescriptor.From(descriptor.Path, descriptor.PagesDirectoryPath, request.PageSize, request.CacheSize));
		using var root = new ExtendedMemoryStream(buffer);
		if (root.RequiresLoad)
			root.Load();
		using var streams = new ClusteredStreams(root, request.ClusterSize, request.Policy);
		if (streams.RequiresLoad)
			streams.Load();
		long length;
		using (var stream = streams.Add()) {
			var bytes = new byte[request.ItemSize];
			for (var index = 0; index < request.Items; index++) {
				if (index % request.Batch == 0)
					token.ThrowIfCancellationRequested();
				stream.WriteBytes(bytes);
			}
			length = stream.Length;
		}
		if (request.Commit)
			buffer.Commit();
		return $"Clustered-stream length: {length:N0} bytes\r\nExpected payload: {(long)request.Items * request.ItemSize:N0} bytes\r\nCluster bytes: {request.ClusterSize:N0}\r\nCommitted: {request.Commit}";
	}

	// Reopen after commit and confirm that a rolled-back append does not reappear.
	private static string VerifyTransactions(Sphere10FrameworkFileDescriptor descriptor, CancellationToken token) {
		token.ThrowIfCancellationRequested();
		using (var list = new TransactionalList<byte[]>(descriptor, ByteArraySerializer.Instance)) {
			if (list.RequiresLoad)
				list.Load();
			list.Add(new byte[] { 1 });
			list.Commit();
			list.Add(new byte[] { 2 });
			list.Rollback();
			if (list.Count != 1)
				throw new InvalidOperationException("Rollback did not restore the committed item count.");
			list.Add(new byte[] { 3 });
			list.Commit();
		}
		using var reopened = new TransactionalList<byte[]>(descriptor, ByteArraySerializer.Instance);
		if (reopened.RequiresLoad)
			reopened.Load();
		if (reopened.Count != 2 || reopened[0][0] != 1 || reopened[1][0] != 3)
			throw new InvalidOperationException("Reopened contents did not match the two committed values.");
		return "PASS: committed [1], rolled back [2], committed [3].\r\nA new collection reopened the file and verified exactly [1, 3].";
	}

	// The root stream owns its supplied buffer; the optional transaction reference is borrowed.
	private static (Stream Stream, ITransactionalObject Transaction) CreateStream(Request request, Sphere10FrameworkFileDescriptor descriptor) {
		if (request.Backend == ListType.MemoryStream)
			return (new MemoryStream(), null);
		if (request.Backend == ListType.MerkleizedList)
			return (new MemoryStream(), null);
		var buffer = CreateBuffer(request, descriptor);
		return (new ExtendedMemoryStream(buffer, true), buffer as ITransactionalObject);
	}

	// Existing buffer families supply storage; no application-specific persistence wrapper is needed.
	private static IBuffer CreateBuffer(Request request, Sphere10FrameworkFileDescriptor descriptor) {
		return request.Backend switch {
			ListType.MemoryBuffer => new MemoryBuffer(request.PageSize, request.PageSize),
			ListType.MemoryPagedBuffer => new MemoryPagedBuffer(request.PageSize, request.CacheSize),
			ListType.FilePaged => new FileMappedBuffer(TransactionalFileDescriptor.From(descriptor.Path, request.PageSize, request.CacheSize)),
			ListType.TransactionalFilePaged => new TransactionalFileMappedBuffer(TransactionalFileDescriptor.From(descriptor.Path, descriptor.PagesDirectoryPath, request.PageSize, request.CacheSize)),
			_ => throw new InvalidOperationException("Choose a memory or file buffer backend for the buffer test.")
		};
	}

	// Selection changes update availability and retain the ordinary next-run preferences.
	private void BackendChanged(object sender, EventArgs args) {
		UpdateAvailability();
		PreferencesEdited(sender, args);
	}

	// No current worker request changes when Options edits its detached next-run values.
	private void PreferencesEdited(object sender, EventArgs args) {
		if (!HasSettings || Updating || _listTypeComboBox.SelectedEnum == null)
			return;
		var settings = ScreenSettings;
		settings.Items = _itemsIntBox.Value ?? settings.Items;
		settings.BatchSize = _batchIntBox.Value ?? settings.BatchSize;
		settings.ItemBytes = _itemSizeIntBox.Value ?? settings.ItemBytes;
		settings.ClusterBytes = _clusterSizeIntBox.Value ?? settings.ClusterBytes;
		settings.PageBytes = _pageSizeIntBox.Value ?? settings.PageBytes;
		settings.CacheBytes = _cacheSizeIntBox.Value ?? settings.CacheBytes;
		settings.Backend = (ListType)_listTypeComboBox.SelectedEnum;
		settings.CommitEachBatch = _commitCheckBox.Checked;
		MarkSettingsChanged();
	}

	// Keep design-time and standalone construction independent of services.
	private void ApplyPreferences() {
		if (!HasSettings || IsDisposed || Disposing)
			return;
		var settings = ScreenSettings;
		using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing)) {
			_itemsIntBox.Value = settings.Items;
			_batchIntBox.Value = settings.BatchSize;
			_itemSizeIntBox.Value = settings.ItemBytes;
			_clusterSizeIntBox.Value = settings.ClusterBytes;
			_pageSizeIntBox.Value = settings.PageBytes;
			_cacheSizeIntBox.Value = settings.CacheBytes;
			_listTypeComboBox.SelectedEnum = settings.Backend;
			_commitCheckBox.Checked = settings.CommitEachBatch;
		}
		UpdateAvailability();
	}

	// Disable competing commands while retaining cancellation; incompatible buffer choices stay explicit.
	private void UpdateAvailability() {
		if (_commands == null)
			return;
		var busy = _operationCancellation != null;
		foreach (Control control in _commands.Controls)
			control.Enabled = !busy;
		_inputs.Enabled = !busy;
		_cancelButton.Enabled = busy;
		var backend = _listTypeComboBox.SelectedEnum is ListType selected ? selected : ListType.Transactional;
		_bufferTestButton.Enabled = !busy && (backend is ListType.MemoryBuffer or ListType.MemoryPagedBuffer or ListType.FilePaged or ListType.TransactionalFilePaged);
	}

	// Invalid native numeric input never silently becomes a zero-sized storage operation.
	private static int Positive(int? value, string title) {
		if (value == null || value <= 0)
			throw new InvalidOperationException(title + " must be a positive whole number.");
		return value.Value;
	}

	// One immutable captured request belongs to one worker operation.
	private sealed record Request(int Items, int Batch, int ItemSize, int ClusterSize, int PageSize, int CacheSize, ListType Backend, ClusteredStreamsPolicy Policy, bool Commit);

	#region Inner Types

	private enum Scenario {
		List,
		Buffer,
		Dictionary,
		Stream,
		Rollback
	}

	/// <summary>The original list/buffer backend choices supported by this laboratory.</summary>
	public enum ListType {
		Transactional,
		MemoryStream,
		MemoryBuffer,
		MemoryPagedBuffer,
		FilePaged,
		TransactionalFilePaged,
		MerkleizedList
	}

	/// <summary>The next collection experiment's inputs, separate from its temporary data and measured results.</summary>
	[UserEditable]
	public class TransactionalCollectionScreenSettings : SettingsObject {
		/// <summary>The bounded number of records to append.</summary>
		[DefaultValue(1000)]
		[Range(1, 100000)]
		public int Items { get; set; } = 1000;

		/// <summary>The number of records in one append/commit batch.</summary>
		[DefaultValue(100)]
		[DisplayName("Batch size"), Range(1, 10000)]
		public int BatchSize { get; set; } = 100;

		/// <summary>The payload size of each generated record.</summary>
		[DefaultValue(64)]
		[DisplayName("Bytes per item"), Range(1, 65536)]
		public int ItemBytes { get; set; } = 64;

		/// <summary>The clustered storage allocation size.</summary>
		[DefaultValue(1024)]
		[DisplayName("Cluster bytes"), Range(1, 1048576)]
		public int ClusterBytes { get; set; } = 1024;

		/// <summary>The memory/file page size.</summary>
		[DefaultValue(65536)]
		[DisplayName("Page bytes"), Range(1, 16777216)]
		public int PageBytes { get; set; } = 65536;

		/// <summary>The maximum memory budget for the paged backend.</summary>
		[DefaultValue(8388608)]
		[DisplayName("Cache bytes"), Range(1, 536870912)]
		public int CacheBytes { get; set; } = 8388608;

		/// <summary>The list/buffer implementation chosen for the next operation.</summary>
		[DefaultValue(TransactionalCollectionScreen.ListType.Transactional)]
		public TransactionalCollectionScreen.ListType Backend { get; set; } = TransactionalCollectionScreen.ListType.Transactional;

		/// <summary>Commits each completed batch when the selected backend is transactional.</summary>
		[DefaultValue(false)]
		[DisplayName("Commit each batch")]
		public bool CommitEachBatch {
			get; set;
		}
	}
	#endregion
}

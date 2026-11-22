// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// The editable content behind a hex editor: a seekable stream read through a bounded <see cref="StreamByteCache"/>
/// plus a piece table of pending edits. Overwrites, insertions and deletions never touch the stream until
/// <see cref="Save"/> or <see cref="SaveAsync"/>: pure overwrites are written in place as coalesced runs, and a length change
/// rewrites the stream from the first structural change (bounded in memory, spilling to a temporary file beyond
/// <see cref="MaxInMemoryRewriteBytes"/>). Members are synchronized so a background search can read while the UI edits, and a
/// save holds the lock one bounded step at a time, so reads made while it runs wait for one step at most and see the pending content.
/// </summary>
public class HexEditorDocument : SyncDisposable {
	/// <summary>Raised when content or its modified state changes through an edit, save or revert.</summary>
	public event EventHandlerEx Changed;

	/// <summary>Raised after pending edits have been written and flushed to the stream.</summary>
	public event EventHandlerEx Saved;

	public const long DefaultMaxInMemoryRewriteBytes = 64L * 1024 * 1024;
	private const int RewriteChunkSize = 64 * 1024;
	private readonly object _lock = new object();
	private readonly Stream _stream;
	private readonly bool _ownsStream;
	private readonly List<Piece> _pieces;
	private readonly MemoryStream _added;
	private long _originalLength;
	private bool _readOnly;
	private volatile bool _saving;
	private Stream _commitStaging;
	private long _commitStart;

	public HexEditorDocument(Stream stream)
		: this(stream, true) {
	}

	public HexEditorDocument(Stream stream, bool ownsStream)
		: this(stream, ownsStream, StreamByteCache.DefaultBlockSize, StreamByteCache.DefaultMaxBlocks) {
	}

	public HexEditorDocument(Stream stream, bool ownsStream, int cacheBlockSize, int cacheMaxBlocks) {
		Guard.ArgumentNotNull(stream, nameof(stream));
		Guard.Argument(stream.CanSeek, nameof(stream), "A hex editor requires a seekable stream.");
		Guard.Argument(stream.CanRead, nameof(stream), "A hex editor requires a readable stream.");
		_stream = stream;
		_ownsStream = ownsStream;
		_pieces = new List<Piece>();
		_added = new MemoryStream();
		_readOnly = !stream.CanWrite;
		_originalLength = stream.Length;
		Length = _originalLength;
		MaxInMemoryRewriteBytes = DefaultMaxInMemoryRewriteBytes;
		Cache = new StreamByteCache(stream, cacheBlockSize, cacheMaxBlocks);
		ResetPieces();
	}

	/// <summary>Opens a file for in-place editing, or for reading only.</summary>
	public static HexEditorDocument OpenFile(string path, bool readOnly) {
		Guard.ArgumentNotNullOrWhitespace(path, nameof(path));
		var stream = new FileStream(path, FileMode.Open, readOnly ? FileAccess.Read : FileAccess.ReadWrite, FileShare.Read);
		return new HexEditorDocument(stream, true);
	}

	/// <summary>The current content length, including pending insertions and deletions.</summary>
	public long Length { get; private set; }

	/// <summary>The stream length as captured at open or the last save.</summary>
	public long OriginalLength => _originalLength;

	/// <summary>Whether the underlying stream accepts writes at all.</summary>
	public bool CanWrite => _stream.CanWrite;

	/// <summary>Whether edits may change the document length. Disable for a fixed-size caller-owned byte buffer.</summary>
	public bool CanResize { get; set; } = true;

	/// <summary>Whether edits are rejected. A document over a non-writable stream is always read-only, and every document is while it saves.</summary>
	public bool IsReadOnly {
		get => _readOnly || _saving;
		set {
			Guard.Ensure(value || _stream.CanWrite, "The underlying stream does not support writing.");
			_readOnly = value;
		}
	}

	/// <summary>Whether a save is writing the pending edits to the stream. Edits are refused meanwhile; reads go on and see the pending content.</summary>
	public bool IsSaving => _saving;

	/// <summary>Whether there are pending edits not yet written to the stream.</summary>
	public bool HasChanges {
		get {
			lock (_lock)
				return !IsPristine;
		}
	}

	/// <summary>Whether a pending edit changed the length, so saving rewrites the stream tail instead of patching in place.</summary>
	public bool HasLengthChanged {
		get {
			lock (_lock)
				return Length != _originalLength;
		}
	}

	/// <summary>The number of bytes whose pending value does not come from the stream (overwritten or inserted).</summary>
	public long ModifiedByteCount {
		get {
			lock (_lock) {
				var count = 0L;
				foreach (var piece in _pieces)
					if (piece.IsAdded)
						count += piece.Length;
				return count;
			}
		}
	}

	/// <summary>The largest tail a length-changing save rewrites through memory; longer tails stage through a temporary file.</summary>
	public long MaxInMemoryRewriteBytes { get; set; }

	/// <summary>The block cache serving viewport reads; exposes its budget and fetch statistics.</summary>
	public StreamByteCache Cache { get; }

	/// <summary>Reads through the block cache, observing pending edits. Returns the number of bytes available.</summary>
	public int Read(long offset, Span<byte> buffer) => ReadCore(offset, buffer, true);

	/// <summary>Reads directly from the stream, bypassing the block cache so bulk scans do not evict viewport blocks.</summary>
	public int ReadUncached(long offset, Span<byte> buffer) => ReadCore(offset, buffer, false);

	/// <summary>Reads one byte, returning false at or beyond the end.</summary>
	public bool TryReadByte(long offset, out byte value) {
		Span<byte> single = stackalloc byte[1];
		var read = offset >= 0 && offset < Length ? Read(offset, single) : 0;
		value = read == 1 ? single[0] : (byte)0;
		return read == 1;
	}

	/// <summary>Whether the byte at an offset has a pending value that does not come from the stream.</summary>
	public bool IsModified(long offset) {
		lock (_lock) {
			if (offset < 0 || offset >= Length)
				return false;
			var index = Locate(offset, out _);
			return _pieces[index].IsAdded;
		}
	}

	/// <summary>Overwrites one byte. Writing the stream's own value back clears that modification.</summary>
	public void WriteByte(long offset, byte value) {
		EnsureEditable();
		Guard.ArgumentInRange(offset, 0, Length - 1, nameof(offset));
		bool changed;
		lock (_lock) {
			var index = Locate(offset, out var pieceStart);
			var piece = _pieces[index];
			var relative = offset - pieceStart;
			if (piece.IsAdded) {
				var current = _added.GetBuffer()[piece.Start + relative];
				changed = current != value;
				if (changed) {
					if (piece.Shadow >= 0 && ReadOriginalByte(piece.Shadow + relative) == value) {
						// The byte returns to its stream value: the shadowed original byte takes its place again.
						Replace(offset, 1, Piece.Original(piece.Shadow + relative, 1));
					} else
						_added.GetBuffer()[piece.Start + relative] = value;
				}
			} else {
				changed = ReadOriginalByte(piece.Start + relative) != value;
				if (changed)
					Replace(offset, 1, Piece.Added(Append(stackalloc byte[] { value }), 1, piece.Start + relative));
			}
		}
		if (changed)
			NotifyChanged();
	}

	/// <summary>Overwrites a range starting at an offset; bytes beyond the current end are appended.</summary>
	public void Overwrite(long offset, ReadOnlySpan<byte> bytes) {
		EnsureEditable();
		Guard.ArgumentInRange(offset, 0, Length, nameof(offset));
		Guard.Ensure(CanResize || bytes.Length <= Length - offset, "The byte buffer has a fixed length.");
		if (bytes.IsEmpty)
			return;
		lock (_lock) {
			var overwritten = Math.Min(bytes.Length, Length - offset);
			// The replaced bytes keep their stream identity only when they all come from one stream piece.
			var shadow = -1L;
			if (overwritten > 0) {
				var index = Locate(offset, out var pieceStart);
				var piece = _pieces[index];
				if (!piece.IsAdded && offset - pieceStart + overwritten <= piece.Length)
					shadow = piece.Start + offset - pieceStart;
			}
			Replace(offset, overwritten, Piece.Added(Append(bytes), bytes.Length, shadow));
			Length += bytes.Length - overwritten;
		}
		NotifyChanged();
	}

	/// <summary>Inserts bytes before an offset (or at the end), growing the content.</summary>
	public void Insert(long offset, ReadOnlySpan<byte> bytes) {
		EnsureEditable();
		Guard.ArgumentInRange(offset, 0, Length, nameof(offset));
		if (bytes.IsEmpty)
			return;
		Guard.Ensure(CanResize, "The byte buffer has a fixed length.");
		lock (_lock) {
			Replace(offset, 0, Piece.Added(Append(bytes), bytes.Length, -1));
			Length += bytes.Length;
		}
		NotifyChanged();
	}

	/// <summary>Removes a range, shrinking the content.</summary>
	public void Delete(long offset, long count) {
		EnsureEditable();
		Guard.ArgumentInRange(offset, 0, Length, nameof(offset));
		Guard.ArgumentInRange(count, 0, Length - offset, nameof(count));
		if (count == 0)
			return;
		Guard.Ensure(CanResize, "The byte buffer has a fixed length.");
		lock (_lock) {
			Replace(offset, count, null);
			Length -= count;
		}
		NotifyChanged();
	}

	/// <summary>Writes pending edits to the stream and flushes on the calling thread. Pure overwrites patch in place as coalesced runs; a changed length
	/// rewrites from the first structural change, which for a large file can take long: a UI calls <see cref="SaveAsync"/> instead.</summary>
	public void Save() {
		if (!BeginSave())
			return;
		WriteChanges(CancellationToken.None);
		NotifySaveCompleted();
	}

	/// <summary>Writes pending edits to the stream on a worker thread, as <see cref="Save"/> does, and raises <see cref="Changed"/> and <see cref="Saved"/>
	/// in the caller's synchronization context once the stream is flushed. <see cref="IsSaving"/> is true from the call until then. The token is
	/// observed until the stream is first written; a save that has begun writing completes, so the stream never holds part of the edits.</summary>
	public async Task SaveAsync(CancellationToken cancellationToken = default) {
		if (!BeginSave())
			return;
		// Not given the token: the worker must run to clear IsSaving even when the token is already cancelled.
		await Task.Run(() => WriteChanges(cancellationToken));
		NotifySaveCompleted();
	}

	/// <summary>Discards every pending edit.</summary>
	public void Revert() {
		EnsureNotDisposed();
		Guard.Ensure(!_saving, "The document is being saved.");
		bool reverted;
		lock (_lock) {
			reverted = !IsPristine;
			Length = _originalLength;
			ResetPieces();
		}
		if (reverted)
			NotifyChanged();
	}

	// A save that is writing is waited for, because closing its stream midway would leave part of the edits in the file.
	protected override void DisposeInternal() {
		lock (_lock) {
			while (_saving)
				Monitor.Wait(_lock);
			Cache.Clear();
			_added.Dispose();
			if (_ownsStream)
				_stream.Dispose();
		}
	}

	protected virtual void OnChanged() {
	}

	protected void NotifyChanged() {
		OnChanged();
		Changed?.Invoke();
	}

	protected virtual void OnSaved() {
	}

	protected void NotifySaved() {
		OnSaved();
		Saved?.Invoke();
	}

	// Both read paths walk the pieces covering the range: stream pieces come from the cache (or the stream), added pieces from the edit buffer.
	// While a rewrite is copying its staged tail into the stream, that tail is read from the staged copy instead.
	private int ReadCore(long offset, Span<byte> buffer, bool useCache) {
		EnsureNotDisposed();
		Guard.ArgumentNotNegative(offset, nameof(offset));
		lock (_lock) {
			if (offset >= Length || buffer.IsEmpty)
				return 0;
			if (buffer.Length > Length - offset)
				buffer = buffer.Slice(0, (int)(Length - offset));
			if (_commitStaging == null || offset + buffer.Length <= _commitStart)
				return ReadPieces(offset, buffer, useCache);
			var head = (int)Math.Max(0, _commitStart - offset);
			var headRead = head > 0 ? ReadPieces(offset, buffer.Slice(0, head), useCache) : 0;
			if (headRead < head)
				return headRead;
			_commitStaging.Position = offset + head - _commitStart;
			_commitStaging.ReadExactly(buffer.Slice(head));
			return buffer.Length;
		}
	}

	// Runs under the lock.
	private int ReadPieces(long offset, Span<byte> buffer, bool useCache) {
		var index = Locate(offset, out var pieceStart);
		var total = 0;
		while (total < buffer.Length && index < _pieces.Count) {
			var piece = _pieces[index];
			var relative = offset + total - pieceStart;
			var count = (int)Math.Min(piece.Length - relative, buffer.Length - total);
			var slice = buffer.Slice(total, count);
			int read;
			if (piece.IsAdded) {
				_added.GetBuffer().AsSpan((int)(piece.Start + relative), count).CopyTo(slice);
				read = count;
			} else
				read = useCache ? Cache.Read(piece.Start + relative, slice) : ReadDirect(piece.Start + relative, slice);
			total += read;
			if (read < count)
				break;
			pieceStart += piece.Length;
			index++;
		}
		return total;
	}

	private int ReadDirect(long offset, Span<byte> buffer) {
		_stream.Position = offset;
		var total = 0;
		while (total < buffer.Length) {
			var read = _stream.Read(buffer.Slice(total));
			if (read <= 0)
				break;
			total += read;
		}
		return total;
	}

	private byte ReadOriginalByte(long streamOffset) {
		Span<byte> single = stackalloc byte[1];
		return Cache.Read(streamOffset, single) == 1 ? single[0] : (byte)0;
	}

	// Runs under the lock. The piece containing the offset (or the end piece when offset == Length) and the document offset where it starts.
	private int Locate(long offset, out long pieceStart) {
		pieceStart = 0;
		for (var index = 0; index < _pieces.Count; index++) {
			var piece = _pieces[index];
			if (offset < pieceStart + piece.Length)
				return index;
			pieceStart += piece.Length;
		}
		return _pieces.Count;
	}

	// Runs under the lock. Replaces a document range with one piece (or nothing), splitting the boundary pieces and merging neighbours.
	private void Replace(long offset, long count, Piece? replacement) {
		var first = Split(offset);
		var last = Split(offset + count);
		_pieces.RemoveRange(first, last - first);
		if (replacement is { Length: > 0 } piece)
			_pieces.Insert(first, piece);
		Merge(first);
	}

	// Runs under the lock. Ensures a piece boundary at the offset and returns the index of the piece starting there.
	private int Split(long offset) {
		var index = Locate(offset, out var pieceStart);
		if (index == _pieces.Count || offset == pieceStart)
			return index;
		var piece = _pieces[index];
		var head = offset - pieceStart;
		_pieces[index] = piece with { Length = head };
		_pieces.Insert(index + 1, piece with { Start = piece.Start + head, Length = piece.Length - head, Shadow = piece.Shadow >= 0 ? piece.Shadow + head : -1 });
		return index + 1;
	}

	// Runs under the lock. Merges the piece at the index with contiguous neighbours on either side.
	private void Merge(int index) {
		for (var candidate = Math.Min(index, _pieces.Count - 1); candidate >= Math.Max(0, index - 1); candidate--) {
			while (candidate + 1 < _pieces.Count && TryMerge(_pieces[candidate], _pieces[candidate + 1], out var merged)) {
				_pieces[candidate] = merged;
				_pieces.RemoveAt(candidate + 1);
			}
		}
	}

	private static bool TryMerge(Piece left, Piece right, out Piece merged) {
		merged = default;
		if (left.IsAdded != right.IsAdded || left.Start + left.Length != right.Start)
			return false;
		var shadow = left.Shadow >= 0 && right.Shadow == left.Shadow + left.Length ? left.Shadow : -1;
		merged = new Piece(left.IsAdded, left.Start, left.Length + right.Length, left.IsAdded ? shadow : -1);
		return true;
	}

	private long Append(ReadOnlySpan<byte> bytes) {
		var start = _added.Length;
		_added.Position = start;
		_added.Write(bytes);
		return start;
	}

	private bool IsPristine => _pieces.Count == 0 ? _originalLength == 0 && Length == 0
		: _pieces.Count == 1 && !_pieces[0].IsAdded && _pieces[0].Start == 0 && _pieces[0].Length == _originalLength && Length == _originalLength;

	// Runs under the lock. Whether every stream piece still sits at its own stream offset; otherwise reports where the mapping first deviates.
	private bool IsIdentityMapping(out long firstDeviation) {
		var position = 0L;
		foreach (var piece in _pieces) {
			if (!piece.IsAdded && piece.Start != position) {
				firstDeviation = position;
				return false;
			}
			position += piece.Length;
		}
		firstDeviation = position;
		return true;
	}

	// Admits one save: from here edits are refused, so the pieces stay as they are until the save ends.
	private bool BeginSave() {
		EnsureNotDisposed();
		lock (_lock) {
			Guard.Ensure(!_readOnly, "The document is read-only.");
			Guard.Ensure(!_saving, "The document is already being saved.");
			if (IsPristine)
				return false;
			_saving = true;
			return true;
		}
	}

	// Runs after BeginSave, on any thread. Each step takes the lock, so the stream is never used by two threads at once.
	private void WriteChanges(CancellationToken cancellationToken) {
		try {
			cancellationToken.ThrowIfCancellationRequested();
			bool inPlace;
			lock (_lock)
				inPlace = Length == _originalLength && IsIdentityMapping(out _);
			if (inPlace)
				SaveInPlace();
			else
				SaveRewriting(cancellationToken);
		} finally {
			lock (_lock) {
				_saving = false;
				_commitStaging = null;
				Monitor.PulseAll(_lock);
			}
		}
	}

	private void NotifySaveCompleted() {
		NotifyChanged();
		NotifySaved();
	}

	// Consecutive added pieces are written as one run to minimize seeks, a bounded step at a time. Until the save completes the pieces still
	// serve the written bytes from the edit buffer, so a read between steps sees the same content.
	private void SaveInPlace() {
		var position = 0L;
		var index = 0;
		while (index < _pieces.Count) {
			if (!_pieces[index].IsAdded) {
				position += _pieces[index].Length;
				index++;
				continue;
			}
			var runStart = position;
			var run = new MemoryStream();
			while (index < _pieces.Count && _pieces[index].IsAdded) {
				run.Write(_added.GetBuffer(), (int)_pieces[index].Start, (int)_pieces[index].Length);
				position += _pieces[index].Length;
				index++;
			}
			for (var written = 0L; written < run.Length; written += RewriteChunkSize) {
				var count = (int)Math.Min(RewriteChunkSize, run.Length - written);
				lock (_lock) {
					_stream.Position = runStart + written;
					_stream.Write(run.GetBuffer(), (int)written, count);
					Cache.Invalidate(runStart + written, count);
				}
			}
		}
		lock (_lock)
			CompleteSave();
	}

	// The tail from the first structural change is staged completely before the stream is resized and rewritten, because the rewrite
	// overwrites the stream bytes that tail still references. Cancellation is observed only while staging, before the stream changes.
	private void SaveRewriting(CancellationToken cancellationToken) {
		var first = 0L;
		var position = 0L;
		foreach (var piece in _pieces) {
			if (piece.IsAdded || piece.Start != position)
				break;
			position += piece.Length;
			first = position;
		}
		var tailLength = Length - first;
		using Stream staging = tailLength <= MaxInMemoryRewriteBytes
			? new MemoryStream(checked((int)tailLength))
			: new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, RewriteChunkSize, FileOptions.DeleteOnClose);
		var chunk = new byte[(int)Math.Min(RewriteChunkSize, Math.Max(1, tailLength))];
		var staged = 0L;
		while (staged < tailLength) {
			cancellationToken.ThrowIfCancellationRequested();
			var read = ReadCore(first + staged, chunk.AsSpan(0, (int)Math.Min(chunk.Length, tailLength - staged)), false);
			Guard.Ensure(read > 0, "The stream ended before the staged content was read.");
			staging.Write(chunk, 0, read);
			staged += read;
		}
		cancellationToken.ThrowIfCancellationRequested();
		try {
			// From here reads of the tail come from the staged copy until the stream holds it.
			lock (_lock) {
				if (Length != _originalLength)
					_stream.SetLength(Length);
				_commitStart = first;
				_commitStaging = staging;
			}
			for (var copied = 0L; copied < tailLength; copied += chunk.Length) {
				var count = (int)Math.Min(chunk.Length, tailLength - copied);
				lock (_lock) {
					staging.Position = copied;
					staging.ReadExactly(chunk, 0, count);
					_stream.Position = first + copied;
					_stream.Write(chunk, 0, count);
				}
			}
			lock (_lock) {
				Cache.Clear();
				CompleteSave();
			}
		} finally {
			lock (_lock)
				_commitStaging = null;
		}
	}

	// Runs under the lock. The stream now holds the pending content, which becomes the document's original content.
	private void CompleteSave() {
		_stream.Flush();
		_originalLength = Length;
		ResetPieces();
		_commitStaging = null;
	}

	// Runs under the lock. One stream piece covering the saved content, or nothing for an empty stream.
	private void ResetPieces() {
		_pieces.Clear();
		_added.SetLength(0);
		if (Length > 0)
			_pieces.Add(Piece.Original(0, Length));
	}

	private void EnsureEditable() {
		EnsureNotDisposed();
		Guard.Ensure(!_readOnly, "The document is read-only.");
		Guard.Ensure(!_saving, "The document is being saved.");
	}

	private void EnsureNotDisposed() => ObjectDisposedException.ThrowIf(!_added.CanRead, this);

	#region Inner Types

	/// <summary>A run of content: bytes of the stream starting at <see cref="Start"/>, or bytes of the edit buffer. An added piece that overwrote stream bytes records their offset in <see cref="Shadow"/>.</summary>
	private readonly record struct Piece(bool IsAdded, long Start, long Length, long Shadow) {
		public static Piece Original(long start, long length) => new Piece(false, start, length, -1);

		public static Piece Added(long start, long length, long shadow) => new Piece(true, start, length, shadow);
	}

	#endregion
}

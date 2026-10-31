// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;

namespace Sphere10.Framework.ObjectSpaces;

/// <summary>
/// Object space backed by a transactional file, wiring commit/rollback semantics to the underlying <see cref="TransactionalStream"/>.
/// </summary>
public class FileObjectSpace : ObjectSpace, ITransactionalObject{

	public event EventHandlerEx Committing { add => _fileStream.Committing += value; remove => _fileStream.Committing -= value; }

	public event EventHandlerEx Committed { add => _fileStream.Committed += value; remove => _fileStream.Committed -= value; }

	public event EventHandlerEx RollingBack { add => _fileStream.RollingBack += value; remove => _fileStream.RollingBack -= value; }

	public event EventHandlerEx RolledBack { add => _fileStream.RolledBack += value; remove => _fileStream.RolledBack -= value; }

	private readonly TransactionalStream _fileStream;

	/// <summary>Creates a transactional ObjectSpace with one permanent file-backed container and optional metadata.</summary>
	public FileObjectSpace(Sphere10FrameworkFileDescriptor file, ObjectSpaceDefinition objectSpaceDefinition, SerializerFactory serializerFactory, ComparerFactory comparerFactory, FileAccessMode accessMode = FileAccessMode.Default)
		: base(ObjectSpaceStorageHelper.CreateFileStorage(file, objectSpaceDefinition, serializerFactory, comparerFactory, accessMode, out var fileStream), objectSpaceDefinition, serializerFactory, comparerFactory, accessMode, Sphere10FrameworkDefaults.FileObjectSpaceDimensionClusterSize) {
		Guard.ArgumentNotNull(file, nameof(file));
		Guard.ArgumentNotNull(objectSpaceDefinition, nameof(objectSpaceDefinition));
		Guard.ArgumentNotNull(serializerFactory, nameof(serializerFactory));
		Guard.ArgumentNotNull(comparerFactory, nameof(comparerFactory));

		File = file;
		AccessMode = accessMode;

		FlushOnDispose = false; // this flushes on Commiting event, on Dispose we don't want to risk changing the file (Rollback scenario)

		// Create the file stream
		_fileStream = fileStream;
		SubscribeToFileStreamEvents();


		if (AccessMode.HasFlag(FileAccessMode.AutoLoad))
			Load();
	}

	public FileAccessMode AccessMode { get; }

	public Sphere10FrameworkFileDescriptor File { get; }

	public void Commit()  {
		using (EnterAccessScope()) {
			// flush all changed (stored in uncommitted pages)
			_fileStream.Commit();
		}
	}

	public Task CommitAsync() => throw new NotSupportedException();

	public void Rollback() {
		using (EnterAccessScope()) {
			_fileStream.Rollback();
		}
	}

	public Task RollbackAsync() => throw new NotSupportedException();

	public override void Dispose() {
		UnsubscribeToFileStreamEvents();
		base.Dispose();
	}

	/// <summary>Flushes loaded dimensions before commit while allowing initial transaction recovery.</summary>
	protected virtual void OnCommitting() {
		// Recovery can commit before this ObjectSpace has loaded any dimensions to flush.
		if (!RequiresLoad)
			Flush();
	}

	protected virtual void OnCommitted() {
	}

	protected virtual void OnRollingBack() {
		Unload();
	}

	/// <summary>Reloads an initialized container after rollback without reentering initial recovery.</summary>
	protected virtual void OnRolledBack() {
		// Initial transaction recovery already precedes container initialization; avoid reentering that load.
		if (_streams.Initialized) {
			_streams.Initialize();
			Load();
		}
	}

	private void SubscribeToFileStreamEvents() {
		_fileStream.Committing += OnCommitting;
		_fileStream.Committed += OnCommitted;
		_fileStream.RollingBack += OnRollingBack;
		_fileStream.RolledBack += OnRolledBack;
	}

	private void UnsubscribeToFileStreamEvents() {
		if (_fileStream is null)
			return;

		_fileStream.Committing -= OnCommitting;
		_fileStream.Committed -= OnCommitted;
		_fileStream.RollingBack -= OnRollingBack;
		_fileStream.RolledBack -= OnRolledBack;
	}
}



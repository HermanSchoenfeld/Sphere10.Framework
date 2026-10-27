// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework;

// ReSharper disable CheckNamespace
namespace Tools;

public static class Scope {

	public static ActionScope ExecuteOnDispose(Action finalizer) => new(finalizer);

	public static ActionScope<T> ExecuteOnDispose<T>(Action<T> finalizer, T item) => new(item, finalizer);

	public static TaskScope ExecuteOnDisposeAsync(Func<ValueTask> finalizer) => new(finalizer);

	public static TaskScope<T> ExecuteOnDisposeAsync<T>(Func<T, ValueTask> finalizer, T item) => new(item, finalizer);

	public static NoOpScope EnterNoOpScope() => new();

	public static NoOpScope<T> EnterValueScope<T>(T item) => new(item);

	public static BorrowArrayScope<byte> EnterBorrowArrayScope(int length, bool clearArray = false) => new(length, clearArray);

	public static BorrowArrayScope<T> EnterBorrowArrayScope<T>(int length, bool clearArray = false) => new(length, clearArray);

	public static BorrowArrayScope<byte> EnterBorrowArrayScope(ArrayPool<byte> pool, int length, bool clearArray = false) => new(pool, length, clearArray);

	public static BorrowArrayScope<T> EnterBorrowArrayScope<T>(ArrayPool<T> pool, int length, bool clearArray = false) => new(pool, length, clearArray);

	public static ReadScope EnterReadScope(ISynchronizedObject owner) {
		Guard.ArgumentNotNull(owner, nameof(owner));
		return owner.EnterReadScope();
	}

	public static ReadScope<TReadScope> EnterReadScope<TReadScope, TWriteScope>(ISynchronizedObject<TReadScope, TWriteScope> owner)
		where TReadScope : struct, IScope
		where TWriteScope : struct, IScope {
		Guard.ArgumentNotNull(owner, nameof(owner));
		return owner.EnterReadScope();
	}

	public static WriteScope EnterWriteScope(ISynchronizedObject owner) {
		Guard.ArgumentNotNull(owner, nameof(owner));
		return owner.EnterWriteScope();
	}

	public static WriteScope<TWriteScope> EnterWriteScope<TReadScope, TWriteScope>(ISynchronizedObject<TReadScope, TWriteScope> owner)
		where TReadScope : struct, IScope
		where TWriteScope : struct, IScope {
		Guard.ArgumentNotNull(owner, nameof(owner));
		return owner.EnterWriteScope();
	}

	public static AccessScope EnterAccessScope(ICriticalObject owner) {
		Guard.ArgumentNotNull(owner, nameof(owner));
		return owner.EnterAccessScope();
	}

	public static RestorePositionScope EnterRestorePositionScope(Stream stream) {
		Guard.ArgumentNotNull(stream, nameof(stream));
		return stream.EnterRestorePositionScope();
	}

	public static RestorePositionScope EnterRestorePositionSeek(Stream stream, long offset, SeekOrigin origin = SeekOrigin.Begin) {
		Guard.ArgumentNotNull(stream, nameof(stream));
		return stream.EnterRestorePositionSeek(offset, origin);
	}

	public static Hashers.HasherScope EnterBorrowHasherScope(CHF algorithm, out IHashFunction hasher) => Hashers.EnterBorrowHasherScope(algorithm, out hasher);

	public static PageScope<T> EnterOpenPageScope<T>(IPagedList<T> owner, IPage<T> page) {
		Guard.ArgumentNotNull(owner, nameof(owner));
		return owner.EnterOpenPageScope(page);
	}

	public static PageScope<T> EnterOpenPageScope<T>(IPagedList<T> owner, long pageIndex) {
		Guard.ArgumentNotNull(owner, nameof(owner));
		return owner.EnterOpenPageScope(owner.Pages[Collection.CheckNotImplemented64bitAddressingIndex(pageIndex)]);
	}

	public static SyncScope<ReaderWriterLockSlim> EnterLockScope(FastLock gate) {
		Guard.ArgumentNotNull(gate, nameof(gate));
		return gate.EnterLockScope();
	}

	public static SyncScope<NonReentrantLock> EnterLockScope(NonReentrantLock gate) {
		Guard.ArgumentNotNull(gate, nameof(gate));
		return gate.EnterLockScope();
	}

	public static MonitorScope EnterLockScope(object syncRoot) => Threads.EnterLockScope(syncRoot);

	public static ActionScope<SemaphoreSlim> EnterWaitScope(SemaphoreSlim semaphore) {
		Guard.ArgumentNotNull(semaphore, nameof(semaphore));
		return semaphore.EnterWaitScope();
	}

	/// <summary>Uses the existing single-instance policy, including its process-exit option.</summary>
	public static SingleApplicationInstanceScope EnterSingleApplicationInstanceScope(bool throwInsteadOfQuit = false, int exitCode = -1)
		=> new(throwInsteadOfQuit, exitCode);

	public static SyncScope EnterSyncScope(Action action) => new(action);

	public static SyncScope<T> EnterSyncScope<T>(Action<T> action, T usingThesePrefetchedValues) {
		return new SyncScope<T>(usingThesePrefetchedValues, action);
	}

	public static AsyncScope EnterAsyncScope(Func<ValueTask> action) => new(action);

	public static AsyncScope<T> EnterAsyncScope<T>(Func<T, ValueTask> action, T usingThesePrefetchedValues) => new(usingThesePrefetchedValues, action);

	public static ActionScope<string> DeleteFileOnDispose(string filename)
		=> ExecuteOnDispose(static path => File.Delete(path), filename);

	public static TaskScope<string> DeleteFileOnDisposeAsync(string filename)
		=> ExecuteOnDisposeAsync(static path => new ValueTask(Tools.FileSystem.DeleteFileAsync(path)), filename);

	public static ActionScope<string> DeleteDirOnDispose(string directory)
		=> ExecuteOnDispose(static path => Tools.FileSystem.DeleteDirectories(path), directory);

	public static TaskScope<string> DeleteDirOnDisposeAsync(string directory)
		=> ExecuteOnDisposeAsync(static path => new ValueTask(Tools.FileSystem.DeleteDirectoriesAsync(path)), directory);
}


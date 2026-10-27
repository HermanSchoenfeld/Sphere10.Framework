// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[NonParallelizable]
public class TransactionalPageBufferStorageTests {

	[Test]
	public void PageStorageStartsEmptyAndClearDetachesItsOwnedArray() {
		var buffer = new MemoryBuffer(0, 256, 256);
		Assert.That(buffer.Count, Is.Zero);
		buffer.ExpandBy(17);
		Assert.That(buffer.AsSpan().ToArray(), Is.EqualTo(new byte[17]));
		var original = buffer.AsSpan();
		original.Fill(0xa5);
		buffer.Clear();
		Assert.That(buffer.Count, Is.Zero);
		Assert.That(original.ToArray(), Is.All.EqualTo(0xa5));
		buffer.Write(8, new byte[] { 7, 8 }.AsSpan());
		Assert.That(buffer.AsSpan().ToArray(), Is.EqualTo(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 7, 8 }));
		buffer.Clear();
		buffer.Clear();
		Assert.That(buffer.Count, Is.Zero);
	}

	[Test]
	public void EscapedPublicSpansKeepTheirOriginalArrayAfterClear([Values(false, true)] bool writable) {
		var buffer = new MemoryBuffer(0, 256, 256);
		var original = new byte[] { 1, 2, 3, 4 };
		buffer.AddRange(original.AsSpan());
		ReadOnlySpan<byte> borrowed = writable ? buffer.AsSpan() : buffer.ReadSpan(0, original.Length);
		buffer.Clear();
		buffer.AddRange(new byte[] { 9, 8, 7, 6 }.AsSpan());
		Assert.That(borrowed.ToArray(), Is.EqualTo(original));
		buffer.Clear();
		Assert.That(borrowed.ToArray(), Is.EqualTo(original));
	}

	[Test]
	public void OwnedPageArrayRespectsItsExactCapacity() {
		const int PageSize = 17;
		var buffer = new MemoryBuffer(0, PageSize, PageSize);
		buffer.ExpandBy(PageSize);
		Assert.That(buffer.AsSpan().ToArray(), Is.EqualTo(new byte[PageSize]));
		Assert.That(() => buffer.Write(PageSize, new byte[] { 1 }.AsSpan()), Throws.ArgumentException);
		Assert.That(() => buffer.AsSpan(0, PageSize + 1).Length, Throws.InstanceOf<ArgumentOutOfRangeException>());
		buffer.Clear();
		Assert.That(buffer.Count, Is.Zero);
	}

	[Test]
	public void ReadFlushCyclesPreserveDataAndUnloadTheOwnedPage() {
		const int PageSize = 256 * 1024;
		const int Iterations = 32;
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var path = Path.Combine(directory, "pages.dat");
		var original = Enumerable.Repeat((byte)0xa5, PageSize).ToArray();
		File.WriteAllBytes(path, original);
		using var file = new TransactionalFileMappedBuffer(TransactionalFileDescriptor.From(path, PageSize, PageSize));
		var destination = new byte[32];
		for (var i = 0; i < 4; i++) {
			file.ReadRange(0, destination.AsSpan());
			file.Flush();
		}
		for (var i = 0; i < Iterations; i++) {
			file.ReadRange(0, destination.AsSpan());
			file.Flush();
		}
		Assert.That(destination, Is.EqualTo(original.Take(destination.Length).ToArray()));
		Assert.That(file.CurrentOpenPages, Is.Zero);
		Assert.That(file.Pages.All(page => page.State == PageState.Unloaded), Is.True);
	}

	[Test]
	public void OnePageBudgetStillEvictsAndFlushStillUnloads([Values(256, 257)] int pageSize) {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var path = Path.Combine(directory, "pages.dat");
		var original = Enumerable.Range(0, pageSize * 3 + 7).Select(i => (byte)i).ToArray();
		File.WriteAllBytes(path, original);
		using var file = new TransactionalFileMappedBuffer(TransactionalFileDescriptor.From(path, pageSize, pageSize));
		var loads = 0;
		var unloads = 0;
		file.PageLoaded += (_, _) => loads++;
		file.PageUnloaded += (_, _) => unloads++;
		var destination = new byte[7];
		for (var offset = 0; offset < original.Length; offset += pageSize) {
			file.ReadRange(offset, destination.AsSpan());
			Assert.That(destination, Is.EqualTo(original.Skip(offset).Take(destination.Length).ToArray()));
			Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
		}
		file.Flush();
		Assert.That(file.CurrentOpenPages, Is.Zero);
		Assert.That(loads, Is.EqualTo(4));
		Assert.That(unloads, Is.EqualTo(loads));
	}

	[Test]
	public void RetainedPublicPageSpanSurvivesUnloadAndOtherPageReuse() {
		const int PageSize = 256;
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var path = Path.Combine(directory, "pages.dat");
		File.WriteAllBytes(path, Enumerable.Repeat((byte)0xa5, PageSize * 2).ToArray());
		using var file = new TransactionalFileMappedBuffer(TransactionalFileDescriptor.From(path, PageSize, PageSize));
		var destination = new byte[8];
		file.ReadRange(0, destination.AsSpan());
		var borrowed = file.Pages[0].ReadSpan(0, destination.Length);
		file.Flush();
		file.UpdateRange(PageSize, new byte[] { 9, 8, 7, 6 }.AsSpan());
		file.Flush();
		file.ReadRange(PageSize, destination.AsSpan());
		Assert.That(borrowed.ToArray(), Is.EqualTo(Enumerable.Repeat((byte)0xa5, destination.Length).ToArray()));
		Assert.That(destination.Take(4), Is.EqualTo(new byte[] { 9, 8, 7, 6 }));
	}

	[Test]
	public void CommitRollbackAndReopenPreservePartialPagesAndZeroExtension([Values(256, 257)] int pageSize) {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var path = Path.Combine(directory, "pages.dat");
		var expected = Enumerable.Range(0, pageSize * 2 + 7).Select(i => (byte)i).ToArray();
		File.WriteAllBytes(path, expected);
		var descriptor = TransactionalFileDescriptor.From(path, pageSize, pageSize);
		using (var file = new TransactionalFileMappedBuffer(descriptor)) {
			file.UpdateRange(pageSize - 1, new byte[] { 21, 22, 23 }.AsSpan());
			new byte[] { 21, 22, 23 }.CopyTo(expected, pageSize - 1);
			file.Flush();
			file.Commit();
			file.UpdateRange(0, new byte[] { 99, 98 }.AsSpan());
			file.Flush();
			file.Rollback();
			Assert.That(file.ReadRange(0, file.Count).ToArray(), Is.EqualTo(expected));
			using var stream = new ExtendedMemoryStream(file);
			stream.SetLength(expected.Length + 19);
			Array.Resize(ref expected, expected.Length + 19);
			file.Flush();
			file.Commit();
		}
		Assert.That(File.ReadAllBytes(path), Is.EqualTo(expected));
		using var reopened = new TransactionalFileMappedBuffer(descriptor);
		Assert.That(reopened.ReadRange(0, reopened.Count).ToArray(), Is.EqualTo(expected));
	}

	[Test]
	public void FailedFlushKeepsDirtyPageAccountedAndCanBeRetried([Values(false, true)] bool failNotification) {
		const int PageSize = 256;
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var path = Path.Combine(directory, "pages.dat");
		var expected = Enumerable.Range(0, PageSize).Select(i => (byte)i).ToArray();
		File.WriteAllBytes(path, expected);
		using (var file = new TransactionalFileMappedBuffer(TransactionalFileDescriptor.From(path, PageSize, PageSize))) {
			expected[0] = 99;
			file.UpdateRange(0, expected.AsSpan(0, 1));
			FileStream markerLock = null;
			using var unlock = Tools.Scope.ExecuteOnDispose(() => markerLock?.Dispose());
			EventHandlerEx<object, IMemoryPage<byte>> failOnce = (_, _) => {
				if (failNotification)
					throw new IOException("Injected PageSaving failure");
				markerLock = File.Open(file.PageMarkerRepo.GetMarkerFilename(0, TransactionalFileMappedBuffer.PageMarkerType.UncommittedPage), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			};
			file.PageSaving += failOnce;
			Assert.That(() => file.Flush(), Throws.InstanceOf<IOException>());
			Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
			Assert.That(file.Pages[0].State, Is.EqualTo(PageState.Loaded));
			Assert.That(file.Pages[0].Dirty, Is.True);
			Assert.That(file.ReadRange(0, file.Count).ToArray(), Is.EqualTo(expected));
			file.PageSaving -= failOnce;
			markerLock?.Dispose();
			markerLock = null;
			file.Flush();
			Assert.That(file.CurrentOpenPages, Is.Zero);
			file.Commit();
		}
		Assert.That(File.ReadAllBytes(path), Is.EqualTo(expected));
	}

	[Test]
	public void FailedCapacityEvictionCanBeRetriedThenCommittedOrRolledBack([Values(false, true)] bool failNotification, [Values(false, true)] bool commit) {
		const int PageSize = 256;
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var path = Path.Combine(directory, "pages.dat");
		var original = Enumerable.Range(0, PageSize * 2).Select(value => (byte)value).ToArray();
		var expected = (byte[])original.Clone();
		expected[0] = 99;
		File.WriteAllBytes(path, original);
		var descriptor = TransactionalFileDescriptor.From(path, PageSize, PageSize);
		using (var file = new TransactionalFileMappedBuffer(descriptor)) {
			file.UpdateRange(0, expected.AsSpan(0, 1));
			var residentPage = file.Pages[0];
			FileStream markerLock = null;
			using var unlock = Tools.Scope.ExecuteOnDispose(() => markerLock?.Dispose());
			EventHandlerEx<object, IMemoryPage<byte>> failSave = (_, page) => {
				Assert.That(page, Is.SameAs(residentPage));
				Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
				Assert.That(file[0], Is.EqualTo(expected[0]));
				if (failNotification)
					throw new IOException("Injected eviction save failure");
				markerLock = File.Open(file.PageMarkerRepo.GetMarkerFilename(0, TransactionalFileMappedBuffer.PageMarkerType.UncommittedPage), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			};
			file.PageSaving += failSave;
			using var detach = Tools.Scope.ExecuteOnDispose(() => file.PageSaving -= failSave);
			var destination = new byte[1];

			Assert.That(() => file.ReadRange(PageSize, destination.AsSpan()), Throws.InstanceOf<IOException>());
			Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
			Assert.That(residentPage.State, Is.EqualTo(PageState.Loaded));
			Assert.That(residentPage.Dirty, Is.True);
			Assert.That(file.Pages[1].State, Is.EqualTo(PageState.Unloaded), "Failed admission must not orphan the incoming loaded page.");
			Assert.That(file[0], Is.EqualTo(expected[0]));
			file.PageSaving -= failSave;
			markerLock?.Dispose();
			markerLock = null;
			file.ReadRange(PageSize, destination.AsSpan());
			Assert.That(destination[0], Is.EqualTo(original[PageSize]));
			Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
			Assert.That(residentPage.State, Is.EqualTo(PageState.Unloaded));
			file.Flush();
			if (commit)
				file.Commit();
			else
				file.Rollback();
			Assert.That(file.ReadRange(0, file.Count).ToArray(), Is.EqualTo(commit ? expected : original));
		}
		Assert.That(File.ReadAllBytes(path), Is.EqualTo(commit ? expected : original));
		using var reopened = new TransactionalFileMappedBuffer(descriptor);
		Assert.That(reopened.ReadRange(0, reopened.Count).ToArray(), Is.EqualTo(commit ? expected : original));
	}

	[Test]
	public void ReentrantPageOpenCannotConsumeCapacityReservedForTheSavingPage([Values(false, true)] bool failNotification) {
		const int PageSize = 256;
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var path = Path.Combine(directory, "pages.dat");
		var expected = Enumerable.Range(0, PageSize * 2).Select(value => (byte)value).ToArray();
		File.WriteAllBytes(path, expected);
		var descriptor = TransactionalFileDescriptor.From(path, PageSize, PageSize);
		using (var file = new TransactionalFileMappedBuffer(descriptor)) {
			expected[0] = 99;
			file.UpdateRange(0, expected.AsSpan(0, 1));
			var callbacks = 0;
			var destination = new byte[1];
			EventHandlerEx<object, IMemoryPage<byte>> openOtherPage = (_, page) => {
				callbacks++;
				Assert.That(callbacks, Is.EqualTo(1), "Capacity pressure must not recursively save the active page.");
				Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
				Assert.That(() => file.ReadRange(PageSize, destination.AsSpan()), Throws.InstanceOf<InvalidOperationException>());
				Assert.That(file.Pages[1].State, Is.EqualTo(PageState.Unloaded));
				Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
				Assert.That(page.State, Is.EqualTo(PageState.Loaded));
				Assert.That(file[0], Is.EqualTo(expected[0]));
				if (failNotification)
					throw new IOException("Injected failure after reentrant admission");
			};
			file.PageSaving += openOtherPage;
			using var detach = Tools.Scope.ExecuteOnDispose(() => file.PageSaving -= openOtherPage);

			if (failNotification) {
				Assert.That(() => file.Flush(), Throws.TypeOf<IOException>().With.Message.EqualTo("Injected failure after reentrant admission"));
				Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
				Assert.That(file.Pages[0].Dirty, Is.True);
			} else {
				file.Flush();
				Assert.That(file.CurrentOpenPages, Is.Zero);
			}
			Assert.That(callbacks, Is.EqualTo(1));
			file.PageSaving -= openOtherPage;
			file.Flush();
			Assert.That(file.CurrentOpenPages, Is.Zero);
			Assert.That(file.Pages.All(page => page.State == PageState.Unloaded), Is.True);
			file.Commit();
		}
		Assert.That(File.ReadAllBytes(path), Is.EqualTo(expected));
		using var reopened = new TransactionalFileMappedBuffer(descriptor);
		Assert.That(reopened.ReadRange(0, reopened.Count).ToArray(), Is.EqualTo(expected));
	}

	[Test]
	public void RollbackAfterNestedFlushAndSaveFailureDiscardsBothResidentAndStagedChanges() {
		const int PageSize = 256;
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var path = Path.Combine(directory, "pages.dat");
		var original = Enumerable.Range(0, PageSize * 2).Select(value => (byte)value).ToArray();
		File.WriteAllBytes(path, original);
		var descriptor = TransactionalFileDescriptor.From(path, PageSize, PageSize * 2);
		using (var file = new TransactionalFileMappedBuffer(descriptor)) {
			file.UpdateRange(0, new byte[] { 99 }.AsSpan());
			file.UpdateRange(PageSize, new byte[] { 98 }.AsSpan());
			var nested = false;
			EventHandlerEx<object, IMemoryPage<byte>> failAfterNestedFlush = (_, page) => {
				if (nested)
					return;
				nested = true;
				file.Flush();
				Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
				Assert.That(page.State, Is.EqualTo(PageState.Loaded));
				Assert.That(file.Pages.Count(candidate => candidate.State == PageState.Unloaded), Is.EqualTo(1));
				throw new IOException("Injected failure after another page was staged");
			};
			file.PageSaving += failAfterNestedFlush;
			using var detach = Tools.Scope.ExecuteOnDispose(() => file.PageSaving -= failAfterNestedFlush);

			Assert.That(() => file.Flush(), Throws.TypeOf<IOException>().With.Message.EqualTo("Injected failure after another page was staged"));
			Assert.That(nested, Is.True);
			Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
			Assert.That(file.Pages.Count(page => page.State == PageState.Loaded && page.Dirty), Is.EqualTo(1));
			file.PageSaving -= failAfterNestedFlush;
			file.Rollback();
			Assert.That(file.ReadRange(0, file.Count).ToArray(), Is.EqualTo(original));
			file.UpdateRange(0, new byte[] { 77 }.AsSpan());
			file.Commit();
		}
		original[0] = 77;
		Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
		using var reopened = new TransactionalFileMappedBuffer(descriptor);
		Assert.That(reopened.ReadRange(0, reopened.Count).ToArray(), Is.EqualTo(original));
	}

	[Test]
	public void LoadingNotificationReservesCapacityAndReleasesItAfterFailure([Values(false, true)] bool failNotification) {
		const int PageSize = 256;
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var path = Path.Combine(directory, "pages.dat");
		var original = Enumerable.Range(0, PageSize * 2).Select(value => (byte)value).ToArray();
		File.WriteAllBytes(path, original);
		using var file = new TransactionalFileMappedBuffer(TransactionalFileDescriptor.From(path, PageSize, PageSize));
		var callbacks = 0;
		var destination = new byte[1];
		EventHandlerEx<object, IMemoryPage<byte>> openOtherPage = (_, page) => {
			callbacks++;
			Assert.That(callbacks, Is.EqualTo(1));
			Assert.That(page.State, Is.EqualTo(PageState.Unloaded));
			Assert.That(() => file.ReadRange(PageSize, destination.AsSpan()), Throws.InstanceOf<InvalidOperationException>());
			Assert.That(file.CurrentOpenPages, Is.Zero);
			Assert.That(file.Pages.All(candidate => candidate.State == PageState.Unloaded), Is.True);
			if (failNotification)
				throw new IOException("Injected PageLoading failure");
		};
		file.PageLoading += openOtherPage;
		using var detach = Tools.Scope.ExecuteOnDispose(() => file.PageLoading -= openOtherPage);

		if (failNotification) {
			Assert.That(() => file.ReadRange(0, destination.AsSpan()), Throws.TypeOf<IOException>().With.Message.EqualTo("Injected PageLoading failure"));
			Assert.That(file.CurrentOpenPages, Is.Zero);
			Assert.That(file.Pages.All(page => page.State == PageState.Unloaded), Is.True);
		} else {
			file.ReadRange(0, destination.AsSpan());
			Assert.That(destination[0], Is.EqualTo(original[0]));
			Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
		}
		Assert.That(callbacks, Is.EqualTo(1));
		file.PageLoading -= openOtherPage;
		file.ReadRange(PageSize, destination.AsSpan());
		Assert.That(destination[0], Is.EqualTo(original[PageSize]));
		Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
		file.ReadRange(0, destination.AsSpan());
		Assert.That(destination[0], Is.EqualTo(original[0]));
		Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
	}

	[Test]
	public void LoadedNotificationKeepsTheCurrentPageResidentAcrossReentrantReads() {
		const int PageSize = 256;
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var path = Path.Combine(directory, "pages.dat");
		var original = Enumerable.Range(0, PageSize * 2).Select(value => (byte)value).ToArray();
		File.WriteAllBytes(path, original);
		using var file = new TransactionalFileMappedBuffer(TransactionalFileDescriptor.From(path, PageSize, PageSize));
		var callbacks = 0;
		var destination = new byte[1];
		EventHandlerEx<object, IMemoryPage<byte>> openOtherPage = (_, page) => {
			callbacks++;
			Assert.That(callbacks, Is.EqualTo(1));
			Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
			Assert.That(file[0], Is.EqualTo(original[0]));
			Assert.That(() => file.ReadRange(PageSize, destination.AsSpan()), Throws.InstanceOf<InvalidOperationException>());
			Assert.That(page.State, Is.EqualTo(PageState.Loaded));
			Assert.That(file.Pages[1].State, Is.EqualTo(PageState.Unloaded));
			Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
		};
		file.PageLoaded += openOtherPage;
		using var detach = Tools.Scope.ExecuteOnDispose(() => file.PageLoaded -= openOtherPage);

		file.ReadRange(0, destination.AsSpan());
		Assert.That(destination[0], Is.EqualTo(original[0]));
		Assert.That(callbacks, Is.EqualTo(1));
		file.PageLoaded -= openOtherPage;
		file.ReadRange(PageSize, destination.AsSpan());
		Assert.That(destination[0], Is.EqualTo(original[PageSize]));
		Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
		Assert.That(file.Pages[0].State, Is.EqualTo(PageState.Unloaded));
	}

	[Test]
	public void LoadedNotificationFailureKeepsItsReentrantChangesCachedAndCommittable() {
		const int PageSize = 256;
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var path = Path.Combine(directory, "pages.dat");
		var expected = Enumerable.Range(0, PageSize).Select(value => (byte)value).ToArray();
		File.WriteAllBytes(path, expected);
		var descriptor = TransactionalFileDescriptor.From(path, PageSize, PageSize);
		using (var file = new TransactionalFileMappedBuffer(descriptor)) {
			var callbacks = 0;
			EventHandlerEx<object, IMemoryPage<byte>> changeThenFail = (_, _) => {
				callbacks++;
				Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
				file.UpdateRange(0, new byte[] { 99 }.AsSpan());
				throw new IOException("Injected PageLoaded failure after mutation");
			};
			file.PageLoaded += changeThenFail;
			using var detach = Tools.Scope.ExecuteOnDispose(() => file.PageLoaded -= changeThenFail);
			var destination = new byte[1];

			Assert.That(() => file.ReadRange(0, destination.AsSpan()), Throws.TypeOf<IOException>().With.Message.EqualTo("Injected PageLoaded failure after mutation"));
			Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
			Assert.That(file.Pages[0].State, Is.EqualTo(PageState.Loaded));
			Assert.That(file.Pages[0].Dirty, Is.True);
			file.ReadRange(0, destination.AsSpan());
			Assert.That(destination[0], Is.EqualTo(99));
			Assert.That(callbacks, Is.EqualTo(1), "Retry must use the already admitted page and preserve changes made by the failed notification.");
			file.PageLoaded -= changeThenFail;
			file.Flush();
			Assert.That(file.CurrentOpenPages, Is.Zero);
			file.Commit();
		}
		expected[0] = 99;
		Assert.That(File.ReadAllBytes(path), Is.EqualTo(expected));
		using var reopened = new TransactionalFileMappedBuffer(descriptor);
		Assert.That(reopened.ReadRange(0, reopened.Count).ToArray(), Is.EqualTo(expected));
	}

	[Test]
	public void LoadedNotificationExplicitFlushAbortsTheOuterOpenAndAllowsRetry([Values(false, true)] bool reopenSamePage) {
		const int PageSize = 256;
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var path = Path.Combine(directory, "pages.dat");
		var original = Enumerable.Range(0, PageSize * 2).Select(value => (byte)value).ToArray();
		File.WriteAllBytes(path, original);
		using var file = new TransactionalFileMappedBuffer(TransactionalFileDescriptor.From(path, PageSize, PageSize));
		var callbacks = 0;
		var destination = new byte[1];
		EventHandlerEx<object, IMemoryPage<byte>> flushLoadedPage = (_, page) => {
			callbacks++;
			if (callbacks > 1)
				return;
			Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
			file.Flush();
			Assert.That(file.CurrentOpenPages, Is.Zero);
			Assert.That(page.State, Is.EqualTo(PageState.Unloaded));
			if (reopenSamePage) {
				file.ReadRange(0, destination.AsSpan());
				Assert.That(destination[0], Is.EqualTo(original[0]));
				Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
				Assert.That(page.State, Is.EqualTo(PageState.Loaded));
			}
		};
		file.PageLoaded += flushLoadedPage;
		using var detach = Tools.Scope.ExecuteOnDispose(() => file.PageLoaded -= flushLoadedPage);

		Assert.That(() => file.ReadRange(0, destination.AsSpan()), Throws.InstanceOf<InvalidOperationException>());
		Assert.That(callbacks, Is.EqualTo(reopenSamePage ? 2 : 1));
		Assert.That(file.CurrentOpenPages, Is.EqualTo(reopenSamePage ? 1 : 0));
		Assert.That(file.Pages[0].State, Is.EqualTo(reopenSamePage ? PageState.Loaded : PageState.Unloaded));
		file.PageLoaded -= flushLoadedPage;
		file.ReadRange(0, destination.AsSpan());
		Assert.That(destination[0], Is.EqualTo(original[0]));
		Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
		using (file.EnterOpenPageScope(file.Pages[0])) {
			Assert.That(() => file.ReadRange(PageSize, destination.AsSpan()), Throws.InstanceOf<InvalidOperationException>());
			Assert.That(file.Pages[0].State, Is.EqualTo(PageState.Loaded));
			Assert.That(file.Pages[1].State, Is.EqualTo(PageState.Unloaded));
			Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
		}
		file.ReadRange(PageSize, destination.AsSpan());
		Assert.That(destination[0], Is.EqualTo(original[PageSize]));
		Assert.That(file.Pages[0].State, Is.EqualTo(PageState.Unloaded));
		Assert.That(file.CurrentOpenPages, Is.EqualTo(1));
		file.Flush();
		Assert.That(file.CurrentOpenPages, Is.Zero);
	}

	[Test]
	public void FailedPageOpenCanBeRetriedAndDisposalRollsBackItsUncommittedData() {
		const int PageSize = 256;
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var path = Path.Combine(directory, "pages.dat");
		var original = Enumerable.Range(0, PageSize).Select(i => (byte)i).ToArray();
		File.WriteAllBytes(path, original);
		using (var file = new TransactionalFileMappedBuffer(TransactionalFileDescriptor.From(path, PageSize, PageSize))) {
			file.UpdateRange(0, new byte[] { 99 }.AsSpan());
			file.Flush();
			var destination = new byte[1];
			using (File.Open(file.PageMarkerRepo.GetMarkerFilename(0, TransactionalFileMappedBuffer.PageMarkerType.UncommittedPage), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
				Assert.That(() => file.ReadRange(0, destination.AsSpan()), Throws.InstanceOf<IOException>());
			Assert.That(file.CurrentOpenPages, Is.Zero);
			Assert.That(file.Pages[0].State, Is.EqualTo(PageState.Unloaded));
			file.ReadRange(0, destination.AsSpan());
			Assert.That(destination[0], Is.EqualTo(99));
		}
		Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
	}
}

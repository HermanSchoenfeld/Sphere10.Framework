// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using NUnit.Framework;
using MarkerRepository = Sphere10.Framework.TransactionalFileMappedBuffer.MarkerRepository;
using PageMarkerType = Sphere10.Framework.TransactionalFileMappedBuffer.PageMarkerType;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class TransactionalMarkerMemoryTests {

	[Test]
	public void SparsePageMarkersAllocateSpaceForTheirActualValues() {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		MarkerRepository.ScanPersistedFileMarkers(directory, Guid.NewGuid(), out var pageMarkers, out _);
		pageMarkers.Add(-1, PageMarkerType.UncommittedPage);
		pageMarkers.Add(-1, PageMarkerType.DeletedMarker);
		pageMarkers.Clear();
		var before = GC.GetAllocatedBytesForCurrentThread();
		for (var pageNumber = 0; pageNumber < 100; pageNumber++) {
			pageMarkers.Add(pageNumber, PageMarkerType.UncommittedPage);
			pageMarkers.Add(pageNumber, PageMarkerType.DeletedMarker);
		}
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert.That(pageMarkers.Count, Is.EqualTo(100));
		foreach (var markers in pageMarkers)
			Assert.That(markers, Is.EquivalentTo(new[] { PageMarkerType.UncommittedPage, PageMarkerType.DeletedMarker }));
		Assert.That(allocated, Is.LessThan(512 * 1024), "Two marker values per page must not reserve 4096 enum slots per group (over 1.6 MB for 100 pages).");
	}

	[Test]
	public void BothMarkersForOnePageSurviveRecoveryAndCanBeRemovedIndependently() {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var fileID = Guid.NewGuid();
		using (var original = new MarkerRepository(directory, fileID, false)) {
			original.Add(PageMarkerType.UncommittedPage, 7);
			original.Add(PageMarkerType.DeletedMarker, 7);
			original.Add(PageMarkerType.UncommittedPage, 7);
			Assert.That(original.PageMarkers[7], Is.EquivalentTo(new[] { PageMarkerType.UncommittedPage, PageMarkerType.DeletedMarker }));
		}
		using var recovered = new MarkerRepository(directory, fileID, false);
		Assert.That(recovered.PageMarkers.Count, Is.EqualTo(1));
		Assert.That(recovered.PageMarkers[7], Is.EquivalentTo(new[] { PageMarkerType.UncommittedPage, PageMarkerType.DeletedMarker }));
		recovered.Remove(PageMarkerType.UncommittedPage, 7);
		Assert.That(recovered.Contains(PageMarkerType.UncommittedPage, 7), Is.False);
		Assert.That(recovered.Contains(PageMarkerType.DeletedMarker, 7), Is.True);
		recovered.Remove(PageMarkerType.DeletedMarker, 7);
		Assert.That(recovered.PageMarkers[7], Is.Empty);
	}
}

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
public class GuardInterpolationTests {

	[Test]
	public void PassingGuardsEvaluatePredicatesOnceAndSkipDiagnosticExpressions() {
		var predicates = 0;
		var diagnostics = 0;
		Guard.Ensure(++predicates > 0, $"Ensure diagnostic {++diagnostics}");
		Guard.Against(++predicates < 0, $"Against diagnostic {++diagnostics}");
#if OMIT_GUARD && !DEBUG
		Assert.That(predicates, Is.Zero, "Conditional guards omit the complete call, including its predicate.");
#else
		Assert.That(predicates, Is.EqualTo(2));
#endif
		Assert.That(diagnostics, Is.Zero);
	}

	[Test]
	public void PassingGuardsDoNotFormatOrAllocate() {
		var value = new FormattableProbe();
		RunPassingGuards(true, false, value, 1);
		var before = GC.GetAllocatedBytesForCurrentThread();
		RunPassingGuards(true, false, value, 10000);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert.That(value.FormatCalls, Is.Zero);
		Assert.That(allocated, Is.Zero);
		TestContext.WriteLine($"20,000 passing interpolated guards allocated {allocated} bytes and made {value.FormatCalls} formatting calls.");
	}

	[Test]
	public void ShortClusterWalksDoNotAllocateForPassingGuards([Values] bool integrityChecks) {
		var itemFirstMap = new InMemoryClusterMap(new ExtendedListAdapter<Cluster>(), 1);
		var (itemFirstStart, itemFirstEnd) = itemFirstMap.NewClusterChain(16, 99);
		var itemFirstSeeker = new ClusterSeeker(itemFirstMap, 99, itemFirstStart, itemFirstEnd, 16, integrityChecks);
		var itemFirstAllocated = MeasureWalk(itemFirstSeeker);
		TestContext.WriteLine($"16,000 item-first cluster walk steps (integrity={integrityChecks}) allocated {itemFirstAllocated:N0} B.");
		Assert.That(itemFirstSeeker.Pointer.CurrentIndex, Is.Zero);
		Assert.That(itemFirstSeeker.Pointer.CurrentCluster, Is.EqualTo(itemFirstStart));
		Assert.That(itemFirstAllocated, Is.Zero, "Passing guards and short-walk bookkeeping must not allocate when cluster reads do not allocate.");

		var rangeFirstMap = new InMemoryClusterMap(new ExtendedList<Cluster>(), 1);
		var (rangeFirstStart, rangeFirstEnd) = rangeFirstMap.NewClusterChain(16, 99);
		ReadLinks(rangeFirstMap, rangeFirstStart, 64);
		var before = GC.GetAllocatedBytesForCurrentThread();
		var finalCluster = ReadLinks(rangeFirstMap, rangeFirstStart, 1000);
		var linkReadAllocated = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert.That(finalCluster, Is.EqualTo(rangeFirstStart));
		var seeker = new ClusterSeeker(rangeFirstMap, 99, rangeFirstStart, rangeFirstEnd, 16, integrityChecks);
		var controlSeeker = new ClusterSeeker(rangeFirstMap, 99, rangeFirstStart, rangeFirstEnd, 16, !integrityChecks);
		var allocated = MeasureWalk(seeker);
		var controlAllocated = MeasureWalk(controlSeeker);
		Assert.That(seeker.Pointer.CurrentIndex, Is.Zero);
		Assert.That(seeker.Pointer.CurrentCluster, Is.EqualTo(rangeFirstStart));
		Assert.That(controlSeeker.Pointer.CurrentIndex, Is.Zero);
		Assert.That(controlSeeker.Pointer.CurrentCluster, Is.EqualTo(rangeFirstStart));
		TestContext.WriteLine($"16,000 cluster walk steps: item-first backend (integrity={integrityChecks}) {itemFirstAllocated:N0} B; range-first link-read baseline {linkReadAllocated:N0} B; walks (integrity={integrityChecks}) {allocated:N0} B, (integrity={!integrityChecks}) {controlAllocated:N0} B.");
		Assert.That(allocated, Is.EqualTo(linkReadAllocated), "The range-first backend may allocate for scalar reads; the seeker and passing guards must add nothing to those same 16,000 reads.");
		Assert.That(controlAllocated, Is.EqualTo(linkReadAllocated), "Enabling integrity checks must not add diagnostic allocations.");

		static long MeasureWalk(ClusterSeeker seeker) {
			for (var index = 0; index < 64; index++) {
				seeker.SeekForward(8);
				seeker.SeekBackwards(8);
			}
			var before = GC.GetAllocatedBytesForCurrentThread();
			for (var index = 0; index < 1000; index++) {
				seeker.SeekForward(8);
				seeker.SeekBackwards(8);
			}
			return GC.GetAllocatedBytesForCurrentThread() - before;
		}

		static long ReadLinks(ClusterMap map, long currentCluster, int iterations) {
			for (var iteration = 0; iteration < iterations; iteration++) {
				for (var step = 0; step < 8; step++)
					currentCluster = map.ReadClusterNext(currentCluster);
				for (var step = 0; step < 8; step++)
					currentCluster = map.ReadClusterPrev(currentCluster);
			}
			return currentCluster;
		}
	}

#if !OMIT_GUARD || DEBUG
	[TestCase(false)]
	[TestCase(true)]
	[SetCulture("fr-FR")]
	public void FailingGuardsPreserveStandardInterpolationTextAndCurrentCulture(bool against) {
		const decimal number = 1234.5M;
		var date = new DateTime(2026, 10, 22, 13, 14, 15);
		const string text = "abc";
		string nullText = null;
		object nullObject = null;
		object boxed = 42;
		int? nullable = null;
		ReadOnlySpan<char> characters = stackalloc[] { 'x', 'y' };
		var expected = $"{{{number}|{number:N2}|{number,12}|{number,-12:N1}|{date:dd MMMM yyyy}|{text}|{text,6}|{nullText}|{nullObject,4}|{boxed,5:X}|{nullable}|{characters}|{characters,5}|{characters,-5:ignored}}}";
		InvalidOperationException failure = null;
		try {
			if (against)
				Guard.Against(true, $"{{{number}|{number:N2}|{number,12}|{number,-12:N1}|{date:dd MMMM yyyy}|{text}|{text,6}|{nullText}|{nullObject,4}|{boxed,5:X}|{nullable}|{characters}|{characters,5}|{characters,-5:ignored}}}");
			else
				Guard.Ensure(false, $"{{{number}|{number:N2}|{number,12}|{number,-12:N1}|{date:dd MMMM yyyy}|{text}|{text,6}|{nullText}|{nullObject,4}|{boxed,5:X}|{nullable}|{characters}|{characters,5}|{characters,-5:ignored}}}");
		} catch (InvalidOperationException error) {
			failure = error;
		}
		Assert.That(failure, Is.Not.Null);
		Assert.That(failure.Message, Is.EqualTo(expected));
		Assert.That(failure.Message, Does.Contain(number.ToString("N2", CultureInfo.CurrentCulture)));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void FailingGuardsEvaluatePredicatesAndDiagnosticsOnce(bool against) {
		var predicates = 0;
		var diagnostics = 0;
		var value = new FormattableProbe();
		if (against)
			Assert.That(() => Guard.Against(++predicates > 0, $"Diagnostic {++diagnostics}: {value}"),
				Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Diagnostic 1: formatted"));
		else
			Assert.That(() => Guard.Ensure(++predicates < 0, $"Diagnostic {++diagnostics}: {value}"),
				Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Diagnostic 1: formatted"));
		Assert.That(predicates, Is.EqualTo(1));
		Assert.That(diagnostics, Is.EqualTo(1));
		Assert.That(value.FormatCalls, Is.EqualTo(1));
	}

	[Test]
	public void PublicStringOverloadsPreserveDefaultAndEmptyMessages() {
		Assert.That(() => Guard.Ensure(false), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Internal error"));
		Assert.That(() => Guard.Against(true, null), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Internal error"));
		Assert.That(() => Guard.Ensure(false, ""), Throws.TypeOf<InvalidOperationException>().With.Message.Empty);
		Assert.That(() => Guard.Against(true, "provided"), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("provided"));
	}
#else
	[Test]
	public void OmittedFailingGuardsDoNotEvaluatePredicatesOrDiagnosticExpressions() {
		var predicates = 0;
		var diagnostics = 0;
		Guard.Ensure(++predicates < 0, $"Diagnostic {++diagnostics}");
		Guard.Against(++predicates > 0, $"Diagnostic {++diagnostics}");
		Assert.That(predicates, Is.Zero);
		Assert.That(diagnostics, Is.Zero);
	}
#endif

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void RunPassingGuards(bool ensureCondition, bool againstCondition, FormattableProbe value, int count) {
		ReadOnlySpan<char> characters = "cluster";
		for (var index = 0; index < count; index++) {
			Guard.Ensure(ensureCondition, $"Ensure {index,8:X}: {value} {characters}");
			Guard.Against(againstCondition, $"Against {index}: {value} {characters,-8}");
		}
	}

	private sealed class FormattableProbe : IFormattable {
		public int FormatCalls { get; private set; }

		public string ToString(string format, IFormatProvider formatProvider) {
			FormatCalls++;
			return "formatted";
		}

		public override string ToString() => ToString(null, CultureInfo.CurrentCulture);
	}
}

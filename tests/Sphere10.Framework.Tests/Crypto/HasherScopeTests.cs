// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[NonParallelizable]
public class HasherScopeTests {
	[Test]
	public void ConstructionRequiresBothPoolAndHasher([Values] bool missingPool) {
		using var hasher = new ConcatBytes();
		var pool = new ConcurrentStack<IHashFunction>();
		Assert.That(() => new Hashers.HasherScope(missingPool ? null : pool, missingPool ? hasher : null),
			Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo(missingPool ? "pool" : "hasher"));
	}

	[Test]
	public void OwnedScopeReturnsItsHasher() {
		using var hasher = new ConcatBytes();
		var pool = new ConcurrentStack<IHashFunction>();
		var scope = new Hashers.HasherScope(pool, hasher);
		scope.Dispose();
		Assert.That(pool.Count, Is.EqualTo(1));
		Assert.That(pool.TryPop(out var returned), Is.True);
		Assert.That(returned, Is.SameAs(hasher));
		Assert.That(pool.TryPop(out _), Is.False);
	}

	[Test]
	public void ReturningABorrowDoesNotShareAStillHeldHasherWithAnotherBorrow() {
		// These reuse assertions inspect the global pool, so this fixture runs without parallel tests.
		var firstScope = Hashers.EnterBorrowHasherScope(CHF.SHA2_256, out var first);
		using var secondScope = Hashers.EnterBorrowHasherScope(CHF.SHA2_256, out var second);
		Assert.That(first, Is.Not.SameAs(second));
		firstScope.Dispose();
		using var thirdScope = Hashers.EnterBorrowHasherScope(CHF.SHA2_256, out var third);
		using var fourthScope = Hashers.EnterBorrowHasherScope(CHF.SHA2_256, out var fourth);
		Assert.That(third, Is.SameAs(first));
		Assert.That(third, Is.Not.SameAs(second));
		Assert.That(fourth, Is.Not.SameAs(second));
		Assert.That(fourth, Is.Not.SameAs(third));
		third.Transform("abc"u8);
		second.Transform("abc"u8);
		Assert.That(third.GetResult(), Is.EqualTo(AbcDigest));
		Assert.That(second.GetResult(), Is.EqualTo(AbcDigest));
	}

	[Test]
	public void ReborrowResetsAnUnfinishedPreimageBeforeKnownHash() {
		var firstScope = Hashers.EnterBorrowHasherScope(CHF.SHA2_256, out var first);
		first.Transform("unfinished preimage"u8);
		firstScope.Dispose();
		using var nextScope = Hashers.EnterBorrowHasherScope(CHF.SHA2_256, out var next);
		Assert.That(next, Is.SameAs(first));
		next.Transform("abc"u8);
		Assert.That(next.GetResult(), Is.EqualTo(AbcDigest));
	}

	[Test]
	public void ReborrowResetsAFinalizedPreimageForTheEmptyHash() {
		var firstScope = Hashers.EnterBorrowHasherScope(CHF.SHA2_256, out var first);
		first.Transform("abc"u8);
		Assert.That(first.GetResult(), Is.EqualTo(AbcDigest));
		firstScope.Dispose();
		using var nextScope = Hashers.EnterBorrowHasherScope(CHF.SHA2_256, out var next);
		Assert.That(next, Is.SameAs(first));
		Assert.That(next.GetResult(), Is.EqualTo(System.Convert.FromHexString("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855")));
	}

	[Test]
	public async Task RepeatedHashingStreamDisposalReturnsItsBorrowOnlyOnce([Values] bool asynchronous) {
		var seedScope = Hashers.EnterBorrowHasherScope(CHF.SHA2_256, out var expected);
		seedScope.Dispose();
		using var stream = new HashingStream(CHF.SHA2_256);
		stream.WriteByte((byte)'a');
		stream.Write("bc"u8);
		Assert.That(stream.GetDigest(), Is.EqualTo(AbcDigest));
		if (asynchronous) {
			await stream.DisposeAsync();
			await stream.DisposeAsync();
		} else {
			stream.Close();
			stream.Dispose();
		}
		using var firstScope = Hashers.EnterBorrowHasherScope(CHF.SHA2_256, out var first);
		using var secondScope = Hashers.EnterBorrowHasherScope(CHF.SHA2_256, out var second);
		Assert.That(first, Is.SameAs(expected), "Closing the stream must return its actual hasher rather than leave its lease outstanding.");
		Assert.That(first, Is.Not.SameAs(second), "A disposed stream must not return the same pooled hasher twice.");
		Assert.That(() => stream.WriteByte(7), Throws.TypeOf<ObjectDisposedException>());
		Assert.That(() => stream.GetDigest(), Throws.TypeOf<ObjectDisposedException>());
		first.Transform("ab"u8);
		Assert.That(second.GetResult(), Is.EqualTo(System.Convert.FromHexString("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855")));
		first.Transform("c"u8);
		Assert.That(first.GetResult(), Is.EqualTo(AbcDigest));
	}

	private static byte[] AbcDigest => System.Convert.FromHexString("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD");
}

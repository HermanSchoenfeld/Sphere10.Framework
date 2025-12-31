// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using NUnit.Framework;

namespace Sphere10.Framework.CryptoEx.Tests;

[TestFixture]
[Parallelizable(ParallelScope.None)]
// Tests the slow Blake2b hasher in Sphere10.Framework proj
public class Blake2bSlowConsistency {

	[Test]
	public void Empty() {
		var slowHasher = new Sphere10.Framework.Blake2b(Sphere10.Framework.Blake2b._512Config);
		using var _ = Hashers.BorrowHasher(CHF.Blake2b_512_Fast, out var fastHasher);
		Assert.That(slowHasher.GetResult(), Is.EqualTo(fastHasher.GetResult()));
	}

	[Test]
	public void Empty_2() {
		var slowHasher = new Sphere10.Framework.Blake2b(Sphere10.Framework.Blake2b._512Config);
		using var _ = Hashers.BorrowHasher(CHF.Blake2b_512_Fast, out var fastHasher);
		slowHasher.Transform(Array.Empty<byte>());
		fastHasher.Transform(Array.Empty<byte>());
		Assert.That(slowHasher.GetResult(), Is.EqualTo(fastHasher.GetResult()));
	}


	[Test]
	public void Random() {
		var slowHasher = new Sphere10.Framework.Blake2b(Sphere10.Framework.Blake2b._512Config);
		using var _ = Hashers.BorrowHasher(CHF.Blake2b_512_Fast, out var fastHasher);
		var bytes = new Random(31337).NextBytes(100);
		slowHasher.Transform(bytes);
		fastHasher.Transform(bytes);
		Assert.That(slowHasher.GetResult(), Is.EqualTo(fastHasher.GetResult()));
	}


	[Test]
	public void Complex() {
		var rng = new Random(31337);
		var slowHasher = new Sphere10.Framework.Blake2b(Sphere10.Framework.Blake2b._512Config);
		using var _ = Hashers.BorrowHasher(CHF.Blake2b_512_Fast, out var fastHasher);

		for (var i = 0; i < rng.Next(0, 100); i++) {
			var block = rng.NextBytes(rng.Next(0, 100));
			slowHasher.Transform(block);
			fastHasher.Transform(block);
		}
		Assert.That(slowHasher.GetResult(), Is.EqualTo(fastHasher.GetResult()));
		Assert.That(slowHasher.GetResult(), Is.EqualTo(fastHasher.GetResult()));
		Assert.That(slowHasher.GetResult(), Is.EqualTo(fastHasher.GetResult()));
		Assert.That(slowHasher.GetResult(), Is.EqualTo(fastHasher.GetResult()));
		Assert.That(slowHasher.GetResult(), Is.EqualTo(fastHasher.GetResult()));
	}


}


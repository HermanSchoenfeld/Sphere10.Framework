// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.


using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
public class SynchronizedFutureTests {

	[Test]
	public void ConcurrentFirstRequestsRunTheLoaderOnce() {
		var calls = 0;
		using var start = new ManualResetEventSlim(false);
		var future = Tools.Values.Future.SynchronizedLazyLoad(() => {
			Interlocked.Increment(ref calls);
			Thread.Sleep(50);
			return new object();
		});
		var readers = Enumerable.Range(0, 16).Select(_ => Task.Run(() => {
			start.Wait();
			return future.Value;
		})).ToArray();
		start.Set();
		Task.WaitAll(readers);
		Assert.That(calls, Is.EqualTo(1));
		Assert.That(readers.Select(reader => reader.Result).Distinct().Count(), Is.EqualTo(1));
		Assert.That(future.Loaded, Is.True);
	}

	[Test]
	public void AFailedLoadIsRetriedOnTheNextRequest() {
		var calls = 0;
		var future = SynchronizedLazyLoad<int>.From(() => ++calls == 1 ? throw new InvalidOperationException("First load fails") : calls);
		Assert.That(() => future.Value, Throws.InvalidOperationException);
		Assert.That(future.Loaded, Is.False);
		Assert.That(future.Value, Is.EqualTo(2));
		Assert.That(future.Value, Is.EqualTo(2));
	}

	[Test]
	public void AnExplicitValueSetOnOneThreadIsSeenOnAnother() {
		var future = Tools.Values.Future.SynchronizedExplicit<string>();
		Assert.That(future.IsSet, Is.False);
		Assert.That(() => future.Value, Throws.InvalidOperationException);
		Task.Run(() => future.Value = "set").Wait();
		Assert.That(Task.Run(() => future.Value).Result, Is.EqualTo("set"));
		Assert.That(() => future.Value = "again", Throws.InvalidOperationException);
		Assert.That(Tools.Values.Future.SynchronizedExplicit(7).Value, Is.EqualTo(7));
	}
}

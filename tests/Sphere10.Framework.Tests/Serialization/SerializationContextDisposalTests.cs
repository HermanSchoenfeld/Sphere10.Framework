// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class SerializationContextDisposalTests {

	[Test]
	public async Task RepeatedDisposalFinalizesTheSharedContextOnce([Values] bool firstAsync, [Values] bool secondAsync) {
		var context = new ObservedContext();
		var item = new object();
		context.NotifyDeserializingObject(out var index);
		context.NotifyDeserializedObject(item, index);
		var finalizations = 0;
		context.RegisterFinalizationAction(() => {
			Assert.That(context.GetDeserializedObject(index), Is.SameAs(item));
			finalizations++;
		});
		IScope reference = context;
		if (firstAsync)
			await context.DisposeAsync();
		else
			context.Dispose();
		if (secondAsync)
			await reference.DisposeAsync();
		else
			reference.Dispose();
		Assert.That(context.FinalizationAttempts, Is.EqualTo(1));
		Assert.That(finalizations, Is.EqualTo(1));
		Assert.That(() => context.GetDeserializedObject(index), Throws.TypeOf<KeyNotFoundException>());
	}

	[Test]
	public async Task FailedFinalizationRetainsObjectsAndAllowsAnotherDisposalAttempt([Values] bool firstAsync, [Values] bool retryAsync) {
		var context = new ObservedContext();
		var item = new object();
		context.NotifyDeserializingObject(out var index);
		context.NotifyDeserializedObject(item, index);
		var callbackAttempts = 0;
		var failure = new IOException("Owned context finalization failed.");
		context.RegisterFinalizationAction(() => {
			Assert.That(context.GetDeserializedObject(index), Is.SameAs(item));
			if (++callbackAttempts == 1)
				throw failure;
		});
		if (firstAsync)
			await Assert.ThatAsync(async () => await context.DisposeAsync(), Throws.Exception.SameAs(failure));
		else
			Assert.That(context.Dispose, Throws.Exception.SameAs(failure));
		Assert.That(context.GetDeserializedObject(index), Is.SameAs(item));
		if (retryAsync)
			await context.DisposeAsync();
		else
			context.Dispose();
		context.Dispose();
		await context.DisposeAsync();
		Assert.That(callbackAttempts, Is.EqualTo(2));
		Assert.That(context.FinalizationAttempts, Is.EqualTo(2));
		Assert.That(() => context.GetDeserializedObject(index), Throws.TypeOf<KeyNotFoundException>());
	}

	private sealed class ObservedContext : SerializationContext {
		public int FinalizationAttempts { get; private set; }

		protected override void OnScopeEnd() {
			FinalizationAttempts++;
			base.OnScopeEnd();
		}
	}
}

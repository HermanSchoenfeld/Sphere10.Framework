// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BorrowArrayScopeTests {

	[TestCase(false)]
	[TestCase(true)]
	public void ReturnsTheSameRentalToItsOriginalPoolWithTheRequestedClearPolicy(bool clearArray) {
		var array = new[] { 1, 2, 3, 4 };
		var pool = new TrackingPool<int>(array);
		using (var scope = new BorrowArrayScope<int>(pool, 2, clearArray)) {
			Assert.That(scope.Item, Is.SameAs(array));
			Assert.That(scope.Length, Is.EqualTo(2));
			Assert.That(scope.Span.Length, Is.EqualTo(2));
			Assert.That(pool.RequestedLength, Is.EqualTo(2));
			Assert.That(() => { scope.Span[2] = 99; }, Throws.TypeOf<IndexOutOfRangeException>());
			scope.Span[1] = 17;
			Assert.That(array, Is.EqualTo(new[] { 1, 17, 3, 4 }));
			Assert.That(pool.ReturnCalls, Is.Zero);
		}
		Assert.That(pool.RentCalls, Is.EqualTo(1));
		Assert.That(pool.ReturnCalls, Is.EqualTo(1));
		Assert.That(pool.Returned, Is.SameAs(array));
		Assert.That(pool.ClearArray, Is.EqualTo(clearArray));
		Assert.That(array, Is.EqualTo(clearArray ? new int[4] : new[] { 1, 17, 3, 4 }));
	}

	[Test]
	public void UsingReturnsTheRentalWhenTheBodyThrows() {
		var array = new byte[8];
		var pool = new TrackingPool<byte>(array);
		Assert.That(() => {
			using var scope = Tools.Scope.EnterBorrowArrayScope(pool, 3);
			scope.Span.Fill(7);
			throw new InvalidOperationException("Body failed.");
		}, Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Body failed."));
		Assert.That(pool.Returned, Is.SameAs(array));
		Assert.That(pool.ReturnCalls, Is.EqualTo(1));
		Assert.That(array.AsSpan(0, 3).ToArray(), Is.EqualTo(new byte[] { 7, 7, 7 }));
	}

	[TestCase(-1)]
	[TestCase(int.MinValue)]
	public void RejectsNegativeLengthsBeforeRenting(int length) {
		var pool = new TrackingPool<byte>(new byte[8]);
		Assert.That(() => new BorrowArrayScope<byte>(pool, length),
			Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("minimumLength"));
		Assert.That(() => Tools.Scope.EnterBorrowArrayScope(length), Throws.TypeOf<ArgumentOutOfRangeException>());
		Assert.That(pool.RentCalls, Is.Zero);
		Assert.That(pool.ReturnCalls, Is.Zero);
	}

	[Test]
	public void RejectsANullPoolBeforeRenting() {
		Assert.That(() => new BorrowArrayScope<byte>(null, 4),
			Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("pool"));
		Assert.That(() => Tools.Scope.EnterBorrowArrayScope<byte>(null, 4), Throws.TypeOf<ArgumentNullException>());
	}

	[Test]
	public void ZeroLogicalLengthStillReturnsTheActualRental() {
		var array = new byte[8];
		var pool = new TrackingPool<byte>(array);
		using (var scope = new BorrowArrayScope<byte>(pool, 0)) {
			Assert.That(scope.Item, Is.SameAs(array));
			Assert.That(scope.Length, Is.Zero);
			Assert.That(scope.Span.IsEmpty, Is.True);
		}
		Assert.That(pool.RequestedLength, Is.Zero);
		Assert.That(pool.Returned, Is.SameAs(array));
		Assert.That(pool.ReturnCalls, Is.EqualTo(1));
	}

	[Test]
	public void FactoriesPreserveConcreteByteAndGenericRentalTypes() {
		using BorrowArrayScope<byte> bytes = Tools.Scope.EnterBorrowArrayScope(7);
		using BorrowArrayScope<int> numbers = Tools.Scope.EnterBorrowArrayScope<int>(5);
		var array = new[] { "first", "second", "unused" };
		var pool = new TrackingPool<string>(array);
		using (BorrowArrayScope<string> words = Tools.Scope.EnterBorrowArrayScope(pool, 2)) {
			Assert.That(words.Item, Is.SameAs(array));
			words.Span[0] = "changed";
			Assert.That(array[0], Is.EqualTo("changed"));
		}
		Assert.That(bytes.Length, Is.EqualTo(7));
		Assert.That(bytes.Item.Length, Is.GreaterThanOrEqualTo(7));
		Assert.That(numbers.Span.Length, Is.EqualTo(5));
		Assert.That(pool.Returned, Is.SameAs(array));
	}

	[Test]
	public void ClearingAReferenceRentalAlsoClearsTheUnusedCapacity() {
		var array = new[] { "used", "outside logical range" };
		var pool = new TrackingPool<string>(array);
		using (var scope = Tools.Scope.EnterBorrowArrayScope(pool, 1, true))
			Assert.That(scope.Span[0], Is.EqualTo("used"));
		Assert.That(array, Is.EqualTo(new string[2]));
		Assert.That(pool.ClearArray, Is.True);
	}

	[Test]
	public async Task UnconstructedRentalsDoNotSilentlyFinalize() {
		var scope = default(BorrowArrayScope<byte>);
		Assert.That(() => scope.Dispose(), Throws.TypeOf<NullReferenceException>());
		await Assert.ThatAsync(async () => await scope.DisposeAsync(), Throws.TypeOf<NullReferenceException>());
	}

	[Test]
	public void FailedRentDoesNotReturnAnUnacquiredArray() {
		var failure = new InvalidOperationException("Rent failed.");
		var pool = new TrackingPool<byte>(new byte[8]) { RentFailure = failure };
		Assert.That(() => new BorrowArrayScope<byte>(pool, 4), Throws.Exception.SameAs(failure));
		Assert.That(pool.ReturnCalls, Is.Zero);
	}

	[Test]
	public void ReturnFailurePropagatesWithoutChangingOwnershipPolicy() {
		var failure = new InvalidOperationException("Return failed.");
		var pool = new TrackingPool<byte>(new byte[8]) { BeforeReturn = () => throw failure };
		var scope = new BorrowArrayScope<byte>(pool, 4);
		Assert.That(() => scope.Dispose(), Throws.Exception.SameAs(failure));
		Assert.That(pool.Returned, Is.SameAs(scope.Item));
	}

	[Test]
	public async Task AsyncDisposalReturnsBeforeBlockedPoolReturnAndRepresentsItsCompletion() {
		using var entered = new ManualResetEventSlim();
		using var release = new ManualResetEventSlim();
		using var returned = new ManualResetEventSlim();
		var array = new byte[] { 1, 2, 3, 4 };
		var pool = new TrackingPool<byte>(array) {
			BeforeReturn = () => {
				entered.Set();
				if (!release.Wait(TimeSpan.FromSeconds(10)))
					throw new TimeoutException("Pool return was not released.");
			}
		};
		var scope = new BorrowArrayScope<byte>(pool, 3, true);
		var operation = ValueTask.CompletedTask;
		Exception callFailure = null;
		var caller = new Thread(() => {
			try {
				operation = scope.DisposeAsync();
			} catch (Exception error) {
				callFailure = error;
			} finally {
				returned.Set();
			}
		});
		caller.Start();
		try {
			Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
			Assert.That(returned.Wait(TimeSpan.FromSeconds(5)), Is.True, "Async disposal must return while the pool callback remains blocked.");
			Assert.That(callFailure, Is.Null);
			Assert.That(operation.IsCompleted, Is.False);
			Assert.That(pool.ReturnCalls, Is.Zero);
		} finally {
			release.Set();
			Assert.That(caller.Join(TimeSpan.FromSeconds(5)), Is.True);
			await operation.AsTask().WaitAsync(TimeSpan.FromSeconds(10));
		}
		Assert.That(pool.Returned, Is.SameAs(array));
		Assert.That(pool.ReturnCalls, Is.EqualTo(1));
		Assert.That(pool.ClearArray, Is.True);
		Assert.That(array, Is.EqualTo(new byte[4]));
	}

	[Test]
	public async Task AsyncReturnFailureFaultsTheReturnedOperation() {
		var failure = new InvalidOperationException("Return failed.");
		var pool = new TrackingPool<int>(new int[8]) { BeforeReturn = () => throw failure };
		var scope = new BorrowArrayScope<int>(pool, 3);
		var operation = ValueTask.CompletedTask;
		Assert.That(() => { operation = scope.DisposeAsync(); }, Throws.Nothing);
		await Assert.ThatAsync(async () => await operation, Throws.Exception.SameAs(failure));
		Assert.That(pool.Returned, Is.SameAs(scope.Item));
	}



	private sealed class TrackingPool<T> : ArrayPool<T> {
		private readonly T[] _array;

		public TrackingPool(T[] array) {
			_array = array;
		}

		public int RentCalls { get; private set; }
		public int ReturnCalls { get; private set; }
		public int RequestedLength { get; private set; }
		public T[] Returned { get; private set; }
		public bool ClearArray { get; private set; }
		public Exception RentFailure { get; set; }
		public Action BeforeReturn { get; set; }

		public override T[] Rent(int minimumLength) {
			RentCalls++;
			RequestedLength = minimumLength;
			if (RentFailure is not null)
				throw RentFailure;
			return _array;
		}

		public override void Return(T[] array, bool clearArray = false) {
			Returned = array;
			ClearArray = clearArray;
			BeforeReturn?.Invoke();
			if (clearArray)
				array.AsSpan().Clear();
			ReturnCalls++;
		}
	}
}

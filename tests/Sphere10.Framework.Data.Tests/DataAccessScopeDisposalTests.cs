// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using global::NHibernate;
using NUnit.Framework;
using Sphere10.Framework.Data.NHibernate;

namespace Sphere10.Framework.Data.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class DataAccessScopeDisposalTests {

	[Test]
	public async Task AsyncDisposalReturnsWhileSessionCloseIsBlockedAndStillDisposesAfterAFailure([Values] bool failClose) {
		using var entered = new ManualResetEventSlim();
		using var release = new ManualResetEventSlim();
		using var returned = new ManualResetEventSlim();
		var events = new ConcurrentQueue<string>();
		var session = Proxy<ISession>((method, _) => {
			switch (method.Name) {
				case nameof(ISession.FlushAsync):
					events.Enqueue("flush");
					return Task.CompletedTask;
				case nameof(ISession.Close):
					events.Enqueue("close");
					entered.Set();
					if (!release.Wait(TimeSpan.FromSeconds(10)))
						throw new TimeoutException("Session close was not released.");
					if (failClose)
						throw new IOException("Close failed.");
					return null;
				case nameof(IDisposable.Dispose):
					events.Enqueue("dispose");
					return null;
				default:
					throw new NotSupportedException(method.Name);
			}
		});
		var manager = Manager(session);
		var operation = ValueTask.CompletedTask;
		TrackingScope scope = null;
		Exception invocationError = null;
		var caller = new Thread(() => {
			try {
				scope = new TrackingScope(manager);
				operation = scope.DisposeAsync();
			} catch (Exception error) {
				invocationError = error;
			} finally {
				returned.Set();
			}
		}) { IsBackground = true };
		caller.Start();
		try {
			Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
			Assert.That(returned.Wait(TimeSpan.FromSeconds(5)), Is.True, "Session.Close must not block the DisposeAsync caller.");
			Assert.That(invocationError, Is.Null);
			Assert.That(operation.IsCompleted, Is.False);
			Assert.That(events.ToArray(), Is.EqualTo(new[] { "flush", "close" }));
		} finally {
			release.Set();
			Assert.That(caller.Join(TimeSpan.FromSeconds(5)), Is.True);
			await operation.AsTask().WaitAsync(TimeSpan.FromSeconds(5));
		}
		Assert.That(events.ToArray(), Is.EqualTo(new[] { "flush", "close", "dispose" }));
		Assert.That(scope.Errors.Select(error => error.Message), Is.EqualTo(failClose ? new[] { "Close failed." } : Array.Empty<string>()));
	}

	[Test]
	public async Task FailedFlushStillClosesAndDisposesAndRetainsErrorsInOrder() {
		var events = new ConcurrentQueue<string>();
		var session = Proxy<ISession>((method, _) => {
			switch (method.Name) {
				case nameof(ISession.FlushAsync):
					events.Enqueue("flush");
					return Task.FromException(new IOException("Flush failed."));
				case nameof(ISession.Close):
					events.Enqueue("close");
					throw new IOException("Close failed.");
				case nameof(IDisposable.Dispose):
					events.Enqueue("dispose");
					throw new IOException("Dispose failed.");
				default:
					throw new NotSupportedException(method.Name);
			}
		});
		var scope = new TrackingScope(Manager(session));
		await scope.DisposeAsync();
		Assert.That(events.ToArray(), Is.EqualTo(new[] { "flush", "close", "dispose" }));
		Assert.That(scope.Errors.Select(error => error.Message), Is.EqualTo(new[] { "Flush failed.", "Close failed.", "Dispose failed." }));
	}

	private static INHDatabaseManager Manager(ISession session) {
		var factory = Proxy<ISessionFactory>((method, _) => method.Name == nameof(ISessionFactory.OpenSession)
			? session : throw new NotSupportedException(method.Name));
		return Proxy<INHDatabaseManager>((method, _) => method.Name == nameof(INHDatabaseManager.OpenDatabase)
			? factory : throw new NotSupportedException(method.Name));
	}

	private static T Proxy<T>(Func<MethodInfo, object[], object> invoke)
		where T : class {
		var proxy = DispatchProxy.Create<T, InterfaceProxy>();
		((InterfaceProxy)(object)proxy).Handler = invoke;
		return proxy;
	}

	public class InterfaceProxy : DispatchProxy {
		public Func<MethodInfo, object[], object> Handler { get; set; }
		protected override object Invoke(MethodInfo targetMethod, object[] args) => Handler(targetMethod, args);
	}

	private sealed class TrackingScope : DataAccessScope {
		private List<Exception> _errors;

		public TrackingScope(INHDatabaseManager manager)
			: base(manager, new DBReference(DBMSType.Sqlite, "unused"), ContextScopePolicy.MustBeRoot, Guid.NewGuid().ToString("N")) {
		}

		public Exception[] Errors => _errors?.ToArray() ?? Array.Empty<Exception>();
		protected override async Task OnTransactionalScopeEndAsync(List<Exception> errors) {
			_errors = errors;
			await base.OnTransactionalScopeEndAsync(errors);
		}
	}
}

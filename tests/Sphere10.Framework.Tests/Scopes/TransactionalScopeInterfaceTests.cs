// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.UnitTests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class TransactionalScopeInterfaceTests {
	[Test]
	public void NonGenericDefaultNarrowsContextRoot() {
		using var root = new DirectTransactionalScope();
		using var nested = new DirectTransactionalScope { RootScope = root };

		Assert.That(((ITransactionalScope)root).RootScope, Is.SameAs(root));
		Assert.That(((ITransactionalScope)nested).RootScope, Is.SameAs(root));
		Assert.That(((IContextScope)nested).RootScope, Is.SameAs(root));
	}

	[Test]
	public void NonGenericDefaultAndDecoratorPreserveNullForNonTransactionalRoot() {
		var contextID = Guid.NewGuid().ToString();
		using var root = new ActionContextScope(() => { }, ContextScopePolicy.MustBeRoot, contextID);
		using var inner = new DirectTransactionalScope { RootScope = root };
		var decorator = new TransactionalScopeDecorator<ITransactionalScope>(inner);

		Assert.That(((IContextScope)inner).RootScope, Is.SameAs(root));
		Assert.That(((ITransactionalScope)inner).RootScope, Is.Null);
		Assert.That(decorator.RootScope, Is.Null);
		Assert.That(((ITransactionalScope)decorator).RootScope, Is.Null);
	}

	[Test]
	public void NestedActionScopesShareRootAndCommitOwningTransaction() {
		var contextID = Guid.NewGuid().ToString();
		var transaction = new object();
		var commits = 0;
		var closes = 0;
		using (var root = new ActionTransactionalScope<object>(ContextScopePolicy.MustBeRoot, contextID, () => transaction, _ => commits++, _ => { }, _ => closes++)) {
			root.BeginTransaction();
			using (var nested = new ActionTransactionalScope<object>(ContextScopePolicy.MustBeNested, contextID, () => new object(), _ => { }, _ => { }, _ => { })) {
				var typed = (ITransactionalScope<TransactionalScopeBase<object>>)nested;
				var untyped = (ITransactionalScope)nested;
				var decorator = new TransactionalScopeDecorator<ITransactionalScope>(nested);

				Assert.That(nested.RootScope, Is.SameAs(root));
				Assert.That(typed.RootScope, Is.SameAs(root));
				Assert.That(untyped.RootScope, Is.SameAs(root));
				Assert.That(((IContextScope)nested).RootScope, Is.SameAs(root));
				Assert.That(((ITransactionalScope)decorator).RootScope, Is.SameAs(root));
				Assert.That(nested.Transaction, Is.SameAs(transaction));
				nested.BeginTransaction();
				nested.Commit();
				Assert.That(commits, Is.Zero);
			}
			root.Commit();
			Assert.That(root.Transaction, Is.Null);
		}

		Assert.That(commits, Is.EqualTo(1));
		Assert.That(closes, Is.EqualTo(1));
		Assert.That(CallContext.LogicalGetData(contextID), Is.Null);
	}

	[Test]
	public void IncompatibleConcreteRootStillThrowsDuringNestedConstruction() {
		var contextID = Guid.NewGuid().ToString();
		using var incompatible = new ActionTransactionalScope<string>(ContextScopePolicy.MustBeRoot, contextID, () => "transaction", _ => { }, _ => { }, _ => { });

		Assert.That(
			() => new ActionTransactionalScope<object>(ContextScopePolicy.MustBeNested, contextID, () => new object(), _ => { }, _ => { }, _ => { }),
			Throws.TypeOf<InvalidCastException>()
		);
		Assert.That(CallContext.LogicalGetData(contextID), Is.SameAs(incompatible));
	}

	[Test]
	public void DecoratorRetainsClassRootWhenContextInterfacePropertyIsReimplemented() {
		using var root = new DirectTransactionalScope();
		using var remappedRoot = new DirectTransactionalScope();
		var decorator = new RemappedTransactionalScopeDecorator(root, remappedRoot);

		Assert.That(decorator.RootScope, Is.SameAs(root));
		Assert.That(((IContextScope)decorator).RootScope, Is.SameAs(remappedRoot));
		Assert.That(((ITransactionalScope)decorator).RootScope, Is.SameAs(root));
	}

	private sealed class RemappedTransactionalScopeDecorator : TransactionalScopeDecorator<ITransactionalScope>, IContextScope {
		private readonly IContextScope _contextRoot;

		public RemappedTransactionalScopeDecorator(ITransactionalScope inner, IContextScope contextRoot) : base(inner)
			=> _contextRoot = contextRoot;

		IContextScope IContextScope.RootScope => _contextRoot;
	}

	private sealed class DirectTransactionalScope : Scope, ITransactionalScope {
		public event EventHandlerEx Committing { add { } remove { } }
		public event EventHandlerEx Committed { add { } remove { } }
		public event EventHandlerEx RollingBack { add { } remove { } }
		public event EventHandlerEx RolledBack { add { } remove { } }

		public DirectTransactionalScope() => RootScope = this;

		public string ContextID => "direct";

		public ContextScopePolicy Policy => ContextScopePolicy.None;

		public IContextScope RootScope { get; set; }

		public void BeginTransaction() { }

		public Task BeginTransactionAsync() => Task.CompletedTask;

		public void Commit() { }

		public Task CommitAsync() => Task.CompletedTask;

		public void Rollback() { }

		public Task RollbackAsync() => Task.CompletedTask;
	}
}

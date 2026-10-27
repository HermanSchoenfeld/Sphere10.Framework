// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>
/// Base class to handle scopes which are aware of other scope instances activated within the call context. These can be
/// used share state and behaviour within the disparate call contexts (e.g. to implement database style commit/abort transactional scopes).
/// </summary>
public abstract class ContextScope : Disposable, IContextScope {
	private readonly object _previousContext;

	protected ContextScope(ContextScopePolicy policy, string contextID) {
		Guard.ArgumentNotNullOrWhitespace(contextID, nameof(contextID));
		Policy = policy;
		ContextID = contextID;
		_previousContext = CallContext.LogicalGetData(ContextID);
		if (_previousContext is IContextScope contextObject) {
			// Nested
			if (Policy == ContextScopePolicy.MustBeRoot)
				throw new SoftwareException($"A scope context '{contextID}' has already been declared within the calling context");
			RootScope = contextObject;
			OnContextResume();
		} else {
			// Root
			if (Policy == ContextScopePolicy.MustBeNested)
				throw new SoftwareException($"No scope context '{contextID}' exists within the calling context");
			RootScope = this;
			CallContext.LogicalSetData(ContextID, this);
			try {
				OnContextStart();
			} catch {
				RestoreContext();
				throw;
			}
		}
	}

	/// <summary>
	/// An identifier used to distinguish scopes types within a call context. This generally 1-1 to the repository
	/// instance name that the scope context is being used on.
	/// </summary>
	public string ContextID { get; }

	public ContextScopePolicy Policy { get; }

	public IContextScope RootScope { get; }

	public bool IsRootScope => ReferenceEquals(this, RootScope);

	public override void Dispose() {
		RestoreContext();
		base.Dispose();
	}

	public override ValueTask DisposeAsync() {
		// AsyncLocal assignments must happen in the caller before asynchronous cleanup can suspend.
		RestoreContext();
		return base.DisposeAsync();
	}

	protected sealed override void FreeManagedResources() {
		OnScopeEndInternal();
		if (IsRootScope)
			OnContextEnd();
	}

	protected sealed override async ValueTask FreeManagedResourcesAsync() {
		await OnScopeEndInternalAsync();
		if (IsRootScope)
			await OnContextEndAsync();
	}

	protected virtual void OnScopeEndInternal() {
	}

	protected virtual ValueTask OnScopeEndInternalAsync() => ValueTask.CompletedTask;

	protected virtual void OnContextStart() {
	}

	protected virtual void OnContextResume() {
	}

	protected abstract void OnContextEnd();

	protected abstract ValueTask OnContextEndAsync();

	protected static IContextScope GetCurrent(string contextID) {
		return CallContext.LogicalGetData(contextID) as IContextScope;
	}

	private void RestoreContext() {
		if (IsRootScope && ReferenceEquals(CallContext.LogicalGetData(ContextID), this))
			CallContext.LogicalSetData(ContextID, _previousContext);
	}
}


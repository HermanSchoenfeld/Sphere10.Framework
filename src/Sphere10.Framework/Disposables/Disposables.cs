// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>
/// A scope that carries with it a collection of disposable items. If not salvaged, the items are
/// disposed at the end of the DisposalScope.
/// </summary>
public class Disposables : ListDecorator<IDisposable>, IDisposable, IAsyncDisposable {
	private readonly bool _ignoreExceptions;

	static Disposables() {
		None = new Disposables();
	}

	public Disposables(params IDisposable[] disposals)
		: this(false, disposals) {

	}

	public Disposables(bool ignoreExceptions, params IDisposable[] disposals)
		: base(new List<IDisposable>()) {
		_ignoreExceptions = ignoreExceptions;
		if (disposals.Any())
			this.AddRangeSequentially(disposals);
	}


	public void Dispose() {
		if (_ignoreExceptions)
			this.ForEach(disposable => Tools.Exceptions.ExecuteIgnoringException(disposable.Dispose));
		else
			this.ForEach(disposable => disposable.Dispose());
	}

	public async ValueTask DisposeAsync() {
		foreach (var disposable in this) {
			if (_ignoreExceptions)
				await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => DisposeItemAsync(disposable).AsTask()).ConfigureAwait(false);
			else
				await DisposeItemAsync(disposable).ConfigureAwait(false);
		}
	}

	public void Add(Action disposeAction) {
		base.Add(new ActionDisposable(disposeAction));
	}

	public void Add(Func<Task> disposeTask) {
		base.Add(new TaskDisposable(disposeTask));
	}

	public static Disposables None { get; }

	private static ValueTask DisposeItemAsync(IDisposable disposable)
		=> disposable is IAsyncDisposable asyncDisposable ? asyncDisposable.DisposeAsync() : new ValueTask(Task.Run(disposable.Dispose));

}


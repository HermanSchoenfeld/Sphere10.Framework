// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Services;

public class BasicGenericEventAggregator : SynchronizedObject, IGenericEventAggregator {
	private readonly Dictionary<Type, List<Delegate>> _subscriptions = new();

	public void Subscribe<T>(Action<T> handler) {
		Guard.ArgumentNotNull(handler, nameof(handler));
		using (EnterWriteScope()) {
			if (!_subscriptions.TryGetValue(typeof(T), out var handlers)) {
				handlers = new List<Delegate>();
				_subscriptions.Add(typeof(T), handlers);
			}
			handlers.Add(handler);
		}
	}

	public void Unsubscribe<T>(Action<T> eventHandler) {
		Guard.ArgumentNotNull(eventHandler, nameof(eventHandler));
		using (EnterWriteScope()) {
			if (_subscriptions.TryGetValue(typeof(T), out var handlers))
				handlers.Remove(eventHandler);
		}
	}

	public Task PublishAsync<T>(T data) {
		Delegate[] subscribers;
		using (EnterReadScope())
			subscribers = _subscriptions.TryGetValue(typeof(T), out var handlers) ? handlers.ToArray() : Array.Empty<Delegate>();

		// Dispatch on the caller's context and permit handlers to change subscriptions.
		foreach (var subscriber in subscribers)
			((Action<T>)subscriber)(data);
		return Task.CompletedTask;
	}
}
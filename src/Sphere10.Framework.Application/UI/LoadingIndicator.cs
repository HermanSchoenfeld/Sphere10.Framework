// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework.Application;

/// <summary>The loading indicator's state: the messages shown, most recent last. Safe from any thread; a platform presents it by observing
/// <see cref="Changed"/> and marshalling to its UI thread.</summary>
public class LoadingIndicator : ILoadingIndicator {
	public event EventHandlerEx Changed;

	private readonly object _lock = new();
	private readonly List<Activity> _activities = new();

	public bool IsActive {
		get {
			lock (_lock)
				return _activities.Count > 0;
		}
	}

	public string Message {
		get {
			lock (_lock)
				return _activities.LastOrDefault()?.Message;
		}
	}

	public IScope Show(string message) {
		var activity = new Activity(message ?? string.Empty);
		lock (_lock)
			_activities.Add(activity);
		OnChanged();
		return Tools.Scope.ExecuteOnDispose(() => Hide(activity));
	}

	protected virtual void OnChanged() => Changed?.Invoke();

	// A scope disposed again finds its activity gone and changes nothing.
	private void Hide(Activity activity) {
		lock (_lock)
			if (!_activities.Remove(activity))
				return;
		OnChanged();
	}

	#region Inner Types

	// Identified by reference, so two scopes showing the same message end independently.
	private sealed class Activity {
		public Activity(string message) => Message = message;

		public string Message { get; }
	}

	#endregion
}

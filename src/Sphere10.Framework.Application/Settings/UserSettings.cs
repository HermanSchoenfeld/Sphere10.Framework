// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application;

/// <summary>Provides the current user's settings through the provider installed by Framework initialization.</summary>
public static class UserSettings {

	// Keep a call-scoped provider separate from the process default assigned by native Framework initialization.
	private const string _providerContext = "Sphere10.Framework.Application.UserSettings.Provider";

	private static ISettingsProvider _provider;

	/// <summary>The current call's user provider, falling back to the process provider configured by the native Framework module.</summary>
	public static ISettingsProvider Provider {
		get {
			Guard.Ensure(TryGetProvider(out var provider), "User settings provider has not been set");
			return provider;
		}
		set => _provider = value;
	}

	/// <summary>Loads existing settings or creates the declared settings type through the current provider.</summary>
	public static T Get<T>(object id = null)
		where T : SettingsObject, new() {
		return Provider.Get<T>(id);
	}

	/// <summary>Awaits the current provider's load or creation without introducing a synchronous execution adapter.</summary>
	public static Task<T> GetAsync<T>(object id = null, CancellationToken cancellationToken = default)
		where T : SettingsObject, new() {
		return Provider.GetAsync<T>(id, cancellationToken);
	}

	/// <summary>Tests for a stored settings record of the requested type and identity.</summary>
	public static bool Has<T>(object id = null)
		where T : SettingsObject, new() {
		return Provider.Has<T>(id);
	}

	/// <summary>Awaits the current provider's existence check.</summary>
	public static Task<bool> HasAsync<T>(object id = null, CancellationToken cancellationToken = default)
		where T : SettingsObject, new() {
		return Provider.HasAsync<T>(id, cancellationToken);
	}

	/// <summary>Clears stored user settings through the current provider.</summary>
	public static void Clear() => Provider.ClearSettings();

	/// <summary>Awaits clearing the current user's stored settings.</summary>
	public static Task ClearAsync(CancellationToken cancellationToken = default) => Provider.ClearSettingsAsync(cancellationToken);

	/// <summary>Temporarily binds a user provider to the current asynchronous call flow without changing the process default.</summary>
	/// <remarks>Use synchronous disposal so the previous provider is restored in the caller's context.</remarks>
	public static IDisposable UseProvider(ISettingsProvider provider) {
		Guard.ArgumentNotNull(provider, nameof(provider));

		// CallContext already isolates data across asynchronous flows; each circuit activity supplies its own existing provider.
		var previous = CallContext.LogicalGetData(_providerContext);
		CallContext.LogicalSetData(_providerContext, provider);
		return Tools.Scope.ExecuteOnDispose(static value => CallContext.LogicalSetData(_providerContext, value), previous);
	}

	/// <summary>Returns the ambient user provider or initialized process default without requiring either to exist.</summary>
	public static bool TryGetProvider(out ISettingsProvider provider) {
		provider = CallContext.LogicalGetData(_providerContext) as ISettingsProvider ?? _provider;
		return provider != null;
	}
}

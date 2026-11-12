// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;
using Newtonsoft.Json;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Stores typed settings in the current circuit's user or anonymous browser profile.</summary>
/// <remarks>Browser storage is asynchronous and becomes available when the circuit is interactive.</remarks>
public sealed class BlazorSettingsProvider : AsyncSettingsProviderBase, IAsyncDisposable {

	private readonly IServiceProvider _services;

	private readonly string _applicationKey;

	private IJSObjectReference _module;

	private Task _initialization;

	private string _ownerKey;

	private string _authenticatedSubject;

	private bool _disposed;

	/// <summary>Captures circuit services without accessing browser storage during composition or prerender.</summary>
	public BlazorSettingsProvider(IServiceProvider services, string applicationKey = null) {
		Guard.ArgumentNotNull(services, nameof(services));

		_services = services;
		_applicationKey = applicationKey ?? Tools.Runtime.GetEntryAssembly().GetName().Name;
	}

	/// <summary>Browser preferences retain the existing value-only serialization without machine-bound encryption.</summary>
	public override bool EncryptSettings => false;

	/// <inheritdoc />
	public override async Task<bool> ContainsSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default)
		=> await ReadAsync(GetSettingKey(settingsObjectType, id), cancellationToken) != null;

	/// <inheritdoc />
	public override async Task DeleteSettingAsync(SettingsObject settings, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(settings, nameof(settings));

		await EnsureInitializedAsync(cancellationToken);
		await EnsureSameUserAsync();
		cancellationToken.ThrowIfCancellationRequested();
		await _module.InvokeVoidAsync("Delete", GetStorageKey(GetSettingKey(settings.GetType(), settings.ID)));
		FireChanged();
	}

	/// <inheritdoc />
	public override async Task ClearSettingsAsync(CancellationToken cancellationToken = default) {
		await EnsureInitializedAsync(cancellationToken);
		await EnsureSameUserAsync();
		cancellationToken.ThrowIfCancellationRequested();
		await _module.InvokeVoidAsync("Clear", GetStorageKey(Uri.EscapeDataString(_applicationKey) + "/"));
		FireChanged();
	}

	/// <summary>Releases this circuit's imported module after any in-flight initialization finishes.</summary>
	public async ValueTask DisposeAsync() {
		if (_disposed)
			return;
		_disposed = true;

		if (_initialization != null)
			await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => await _initialization,
				error => error is JSDisconnectedException or OperationCanceledException);
		if (_module != null)
			await Tools.Exceptions.ExecuteCapturingExceptionAsync<JSDisconnectedException>(async () => await _module.DisposeAsync());
	}

	// Reads a provider key only after interactive initialization and verifies the circuit's original user.
	internal async Task<string> ReadAsync(string key, CancellationToken cancellationToken) {
		await EnsureInitializedAsync(cancellationToken);
		await EnsureSameUserAsync();
		return await _module.InvokeAsync<string>("Read", cancellationToken, GetStorageKey(key));
	}

	/// <inheritdoc />
	protected override async Task<SettingsObject> LoadInternalAsync(Type settingsObjectType, object id, CancellationToken cancellationToken) {
		var payload = await ReadAsync(GetSettingKey(settingsObjectType, id), cancellationToken);
		if (payload == null)
			throw new FileNotFoundException("The requested browser settings do not exist.");

		// Only the requested settings type is hydrated; persisted type names cannot select a CLR type.
		var settings = await Task.Run(() => {
			using var reader = new JsonTextReader(new StringReader(payload));
			return (SettingsObject)new SettingsObjectJsonSerializer().Deserialize(reader, settingsObjectType);
		}, cancellationToken);
		await EnsureSameUserAsync();
		Guard.Ensure(settings != null, "The stored preferences contain no settings value.");
		settings.ID = id;
		return settings;
	}

	/// <inheritdoc />
	protected override async Task<SettingsObject> LoadInternalAsync(SettingsObject defaults, CancellationToken cancellationToken) {
		var id = defaults.ID;
		var payload = await ReadAsync(GetSettingKey(defaults.GetType(), id), cancellationToken);
		if (payload == null)
			throw new FileNotFoundException("The requested browser settings do not exist.");

		await Task.Run(() => {
			using var reader = new JsonTextReader(new StringReader(payload));
			new SettingsObjectJsonSerializer().Populate(reader, defaults);
		}, cancellationToken);
		await EnsureSameUserAsync();
		return defaults;
	}

	/// <inheritdoc />
	protected override async Task SaveInternalAsync(SettingsObject settings, CancellationToken cancellationToken) {
		await EnsureInitializedAsync(cancellationToken);
		await EnsureSameUserAsync();

		// Serialize the declared settings value off the renderer; the caller awaits this operation before publishing OnChanged.
		var payload = await Task.Run(() => {
			using var writer = new StringWriter();
			new SettingsObjectJsonSerializer().Serialize(writer, settings);
			return writer.ToString();
		}, cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();
		await EnsureSameUserAsync();

		// Once a browser write starts, await its acknowledgement before publishing accepted values.
		await _module.InvokeVoidAsync("Write", GetStorageKey(GetSettingKey(settings.GetType(), settings.ID)), payload);
	}

	// Imports storage only in the live circuit; a failed initialization remains retryable on a later explicit save.
	private async Task EnsureInitializedAsync(CancellationToken cancellationToken) {
		ObjectDisposedException.ThrowIf(_disposed, this);

		var initialization = _initialization ??= InitializeAsync();
		try {
			await initialization.WaitAsync(cancellationToken);
		} catch {
			if (initialization.IsCompleted && ReferenceEquals(_initialization, initialization))
				_initialization = null;
			throw;
		}
	}

	// The application and settings ID supply record identity; this additional namespace belongs to the browser user.
	private async Task InitializeAsync() {
		var runtime = _services.GetRequiredService<IJSRuntime>();
		_module ??= await runtime.InvokeAsync<IJSObjectReference>("import", "./_content/Sphere10.Framework.Web.AspNetCore.Blazor/js/ApplicationSettings.js");
		_authenticatedSubject = await GetAuthenticatedSubjectAsync();
		_ownerKey = _authenticatedSubject == null
			? "browser:" + await _module.InvokeAsync<string>("GetAnonymousProfile")
			: "user:" + _authenticatedSubject;
	}

	// A principal change requires a new circuit/catalog rather than retargeting existing live preferences to another user.
	private async Task EnsureSameUserAsync() {
		Guard.Ensure(string.Equals(_authenticatedSubject, await GetAuthenticatedSubjectAsync(), StringComparison.Ordinal),
			"The signed-in user changed. Reload the application before accessing its preferences.");
	}

	// Prefer the stable authenticated subject; apps without authentication use the persisted browser-owned identity.
	private async Task<string> GetAuthenticatedSubjectAsync() {
		var authentication = _services.GetService<AuthenticationStateProvider>();
		if (authentication == null)
			return null;
		var user = (await authentication.GetAuthenticationStateAsync()).User;
		if (user.Identity?.IsAuthenticated != true)
			return null;
		var subject = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value ?? user.Identity.Name;
		Guard.Ensure(!string.IsNullOrWhiteSpace(subject), "Authenticated preferences require a stable user identifier.");
		return subject;
	}

	// Type and explicit identity form the same storage contract used by the existing native providers.
	private string GetSettingKey(Type settingsObjectType, object id) {
		Guard.Argument(typeof(SettingsObject).IsAssignableFrom(settingsObjectType), nameof(settingsObjectType), "Settings must derive from SettingsObject.");
		return Uri.EscapeDataString(_applicationKey) + "/" + Uri.EscapeDataString(settingsObjectType.FullName)
			+ "/" + Uri.EscapeDataString(id?.ToString() ?? string.Empty);
	}

	// Segments are escaped independently, so application keys cannot collide with another browser/user namespace.
	private string GetStorageKey(string profileKey) => "sphere10.settings:" + Uri.EscapeDataString(_ownerKey) + ":" + profileKey;
}

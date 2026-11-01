// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Persists detached declared preferences in the current application's user or anonymous browser profile.</summary>
/// <remarks>Register this store per circuit. Serialization runs on a worker; browser interop requires an interactive circuit.</remarks>
public sealed class BlazorApplicationSettingsStore : IApplicationSettingsStore, IAsyncDisposable {
	private readonly IServiceProvider _services;
	private IJSObjectReference _module;
	private Task _initialization;
	private string _ownerKey;
	private string _authenticatedSubject;
	private bool _disposed;

	/// <summary>Captures this circuit's services without reading storage during graph composition or prerender.</summary>
	public BlazorApplicationSettingsStore(IServiceProvider services) {
		Guard.ArgumentNotNull(services, nameof(services));
		_services = services;
	}

	/// <summary>Loads the known settings type and migration markers without activating its screen.</summary>
	public async Task<ApplicationSettingsStoredProfile> LoadAsync(string profileKey, object defaults, bool hasExplicitStorageId, object storageId, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNullOrEmpty(profileKey, nameof(profileKey));
		Guard.ArgumentNotNull(defaults, nameof(defaults));
		await EnsureInitializedAsync(cancellationToken);
		await EnsureSameUserAsync();
		var payload = await _module.InvokeAsync<string>("Read", cancellationToken, GetStorageKey(profileKey));
		if (payload == null)
			return null;

		// Only the declared value type is hydrated; persisted CLR type names and runtime graphs are never accepted.
		return await Task.Run(() => {
			var record = JObject.Parse(payload);
			var serializer = CreateValueSerializer();
			var values = record["values"];
			if (values == null || values.Type == JTokenType.Null)
				throw new JsonSerializationException("The stored preferences contain no settings value.");
			var restored = defaults;
			if (values is JObject && !defaults.GetType().IsValueType && defaults is not string) {
				using var reader = values.CreateReader();
				serializer.Populate(reader, restored);
			} else
				restored = values.ToObject(defaults.GetType(), serializer);
			return new ApplicationSettingsStoredProfile {
				Values = restored,
				SectionVersions = record["sectionVersions"]?.ToObject<Dictionary<string, int>>(serializer) ?? new Dictionary<string, int>()
			};
		}, cancellationToken);
	}

	/// <summary>Completes browser persistence before the catalog publishes the accepted revision.</summary>
	public async Task SaveAsync(string profileKey, ApplicationSettingsStoredProfile snapshot, bool hasExplicitStorageId, object storageId, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNullOrEmpty(profileKey, nameof(profileKey));
		Guard.ArgumentNotNull(snapshot, nameof(snapshot));
		Guard.ArgumentNotNull(snapshot.Values, nameof(snapshot));
		await EnsureInitializedAsync(cancellationToken);
		await EnsureSameUserAsync();

		// The catalog transfers an exclusively owned snapshot; no live screen or cached provider root is serialized here.
		var payload = await Task.Run(() => JObject.FromObject(new {
			values = snapshot.Values,
			sectionVersions = snapshot.SectionVersions
		}, CreateValueSerializer()).ToString(Formatting.None), cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();

		// Once the write starts, await its acknowledgement so a cancelled wait cannot hide a committed value from the catalog.
		await _module.InvokeVoidAsync("Write", GetStorageKey(profileKey), payload);
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

	// Application/profile identity is supplied by the catalog; this additional namespace belongs to the browser user.
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

	// Segments are escaped independently, so application keys cannot collide with another browser/user namespace.
	private string GetStorageKey(string profileKey) {
		return "sphere10.settings:" + Uri.EscapeDataString(_ownerKey) + ":" + profileKey;
	}

	// Ignore process-wide JSON defaults: stored preferences cannot request arbitrary CLR type activation.
	private static JsonSerializer CreateValueSerializer() {
		return JsonSerializer.Create(new JsonSerializerSettings {
			TypeNameHandling = TypeNameHandling.None,
			MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
			ContractResolver = new ValueContractResolver()
		});
	}

	// Reuse authoritative nested defaults, but replace explicitly supplied collections, including empty ones.
	private sealed class ValueContractResolver : DefaultContractResolver {
		protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization) {
			var property = base.CreateProperty(member, memberSerialization);
			if (property.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
				property.ObjectCreationHandling = ObjectCreationHandling.Replace;
			return property;
		}
	}
}

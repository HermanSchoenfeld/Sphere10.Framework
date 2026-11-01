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
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Persists detached application preferences on workers through one captured native settings provider.</summary>
/// <remarks>Explicit legacy IDs preserve the original type and ID and support value-only records. Migrated sections use the default profile record.</remarks>
public sealed class WinFormsApplicationSettingsStore : IApplicationSettingsStore {
	private readonly ISettingsProvider _provider;
	private readonly SettingsValueContractResolver _valueContractResolver = new();

	/// <summary>Captures the application-owned provider so later static provider changes cannot retarget pending writes.</summary>
	public WinFormsApplicationSettingsStore(ISettingsProvider provider) {
		Guard.ArgumentNotNull(provider, nameof(provider));
		_provider = provider;
	}

	// The captured provider also owns legacy grid migration reads in native screen bindings.
	internal ISettingsProvider Provider => _provider;

	/// <summary>Loads a detached known settings value without creating or saving a missing record.</summary>
	public Task<ApplicationSettingsStoredProfile> LoadAsync(string profileKey, object defaults, bool hasExplicitStorageId = false, object storageId = null, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNullOrEmpty(profileKey, nameof(profileKey));
		Guard.ArgumentNotNull(defaults, nameof(defaults));
		return Task.Run(() => ReadProfile(profileKey, defaults, hasExplicitStorageId, storageId, cancellationToken), cancellationToken);
	}

	/// <summary>Saves an owned snapshot before the catalog can publish its accepted revision.</summary>
	public Task SaveAsync(string profileKey, ApplicationSettingsStoredProfile snapshot, bool hasExplicitStorageId = false, object storageId = null, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNullOrEmpty(profileKey, nameof(profileKey));
		Guard.ArgumentNotNull(snapshot, nameof(snapshot));
		Guard.Argument(snapshot.Values != null, nameof(snapshot), "The snapshot must contain settings values.");
		return Task.Run(() => WriteProfile(profileKey, snapshot, hasExplicitStorageId, storageId, cancellationToken), cancellationToken);
	}

	// Provider records remain private; neither cached values nor temporary encryption reach the catalog's live model.
	private ApplicationSettingsStoredProfile ReadProfile(string profileKey, object defaults, bool hasExplicitStorageId, object storageId, CancellationToken cancellationToken) {
		Guard.ArgumentNotNull(defaults, nameof(defaults));
		var settingsType = defaults.GetType();
		cancellationToken.ThrowIfCancellationRequested();
		if (hasExplicitStorageId) {
			Guard.Argument(defaults is SettingsObject, nameof(defaults), "A legacy native record must retain its SettingsObject type.");
			if (!_provider.ContainsSetting(settingsType, storageId))
				return null;
			return new ApplicationSettingsStoredProfile {
				Values = CopyValues(_provider.LoadSetting(settingsType, storageId))
			};
		}

		var id = GetRecordId(profileKey);
		if (!_provider.ContainsSetting(typeof(WinFormsApplicationSettingsRecord), id))
			return null;
		var record = (WinFormsApplicationSettingsRecord)_provider.LoadSetting(typeof(WinFormsApplicationSettingsRecord), id);
		Guard.Ensure(record.Values != null, "The application settings record has no declared values.");
		var serializer = CreateValueSerializer();
		var stored = record.Values.DeepClone();
		if (_provider.EncryptSettings && stored is JObject members)
			DecryptStoredValues(members, settingsType, serializer);
		var values = defaults;
		using (var reader = stored.CreateReader()) {
			if (stored is JObject)
				serializer.Populate(reader, values);
			else
				values = serializer.Deserialize(reader, settingsType);
		}
		ClearProviderContext(values);
		return new ApplicationSettingsStoredProfile {
			Values = values,
			SectionVersions = new Dictionary<string, int>(record.SectionVersions ?? new Dictionary<string, int>(), StringComparer.Ordinal)
		};
	}

	// The catalog supplies an exclusive snapshot; only the legacy path needs a copy before attaching provider context.
	private void WriteProfile(string profileKey, ApplicationSettingsStoredProfile snapshot, bool hasExplicitStorageId, object storageId, CancellationToken cancellationToken) {
		var values = hasExplicitStorageId ? CopyValues(snapshot.Values) : snapshot.Values;
		cancellationToken.ThrowIfCancellationRequested();
		if (hasExplicitStorageId) {
			Guard.Ensure(snapshot.SectionVersions == null || snapshot.SectionVersions.Count == 0, "Legacy settings records cannot carry section migration markers; use profile-managed storage for migrated sections.");
			var legacy = Guard.ArgumentCast<SettingsObject>(values, nameof(snapshot));
			legacy.ID = storageId;
			legacy.Provider = _provider;
			_provider.SaveSetting(legacy);
			return;
		}

		// Provider encryption is shallow, so encrypt the detached typed value before placing it in the envelope.
		var encrypted = false;
		try {
			if (_provider.EncryptSettings) {
				Tools.Object.EncryptMembers(values);
				encrypted = true;
			}
			var record = new WinFormsApplicationSettingsRecord {
				ID = GetRecordId(profileKey),
				Provider = _provider,
				Values = JToken.FromObject(values, CreateValueSerializer()),
				SectionVersions = new Dictionary<string, int>(snapshot.SectionVersions ?? new Dictionary<string, int>(), StringComparer.Ordinal)
			};
			cancellationToken.ThrowIfCancellationRequested();
			_provider.SaveSetting(record);
		} finally {
			if (encrypted)
				Tools.Object.DecryptMembers(values);
		}
	}

	// Use the existing settings JSON contract without copying ignored provider context or preserving a cached root's ID.
	private object CopyValues(object values) {
		var copy = JToken.FromObject(values, CreateValueSerializer()).ToObject(values.GetType(), CreateValueSerializer());
		ClearProviderContext(copy);
		return copy;
	}

	// Provider state belongs only to the internal persisted record, never to the editor's ordinary value.
	private static void ClearProviderContext(object values) {
		if (values is SettingsObject settings) {
			settings.ID = null;
			settings.Provider = null;
		}
	}

	// Decode only the declared schema even when a host configured process-wide JSON type-name handling.
	private JsonSerializer CreateValueSerializer() => JsonSerializer.Create(new JsonSerializerSettings {
		TypeNameHandling = TypeNameHandling.None,
		MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
		ContractResolver = _valueContractResolver
	});

	// Only present, declared encrypted members contain stored ciphertext; absent factory defaults remain plaintext.
	private static void DecryptStoredValues(JObject values, Type settingsType, JsonSerializer serializer) {
		var contract = (JsonObjectContract)serializer.ContractResolver.ResolveContract(settingsType);
		foreach (var property in contract.Properties) {
			if (property.Ignored)
				continue;
			var encryption = property.AttributeProvider?.GetAttributes(typeof(EncryptedStringAttribute), true).OfType<EncryptedStringAttribute>().SingleOrDefault();
			var stored = values.Property(property.PropertyName, StringComparison.OrdinalIgnoreCase);
			if (encryption == null || stored == null)
				continue;
			var decrypted = encryption.Decrypt(stored.Value.Type == JTokenType.Null ? null : stored.Value.Value<string>());
			stored.Value = decrypted == null ? JValue.CreateNull() : new JValue(decrypted);
		}
	}

	// Nested objects reuse authoritative factory values; explicit collections, including empty ones, replace defaults.
	private sealed class SettingsValueContractResolver : DefaultContractResolver {
		protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization serialization) {
			var property = base.CreateProperty(member, serialization);
			if (property.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
				property.ObjectCreationHandling = ObjectCreationHandling.Replace;
			return property;
		}
	}

	// Fixed-length IDs avoid filename sanitization collisions and long registration/type names in file-backed providers.
	private static string GetRecordId(string profileKey) {
		return Hashers.Hash(CHF.SHA2_256, Encoding.UTF8.GetBytes(profileKey)).ToHexString(true);
	}
}

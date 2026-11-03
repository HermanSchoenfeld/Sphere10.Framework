// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application;

/// <summary>Serializes settings values while excluding transient state and runtime ownership.</summary>
public class SettingsObjectJsonSerializer : JsonSerializer {
	/// <summary>Uses declared types and retains nested defaults for omitted stored members.</summary>
	public SettingsObjectJsonSerializer() {
		TypeNameHandling = TypeNameHandling.None;
		MetadataPropertyHandling = MetadataPropertyHandling.Ignore;
		ContractResolver = new SettingsContractResolver();
	}

	/// <summary>Captures actual settings values, including values omitted only by persistence-specific serialization policies.</summary>
	public static string SerializeValues(SettingsObject settings) {
		Guard.ArgumentNotNull(settings, nameof(settings));
		using var writer = new StringWriter();
		var serializer = new SettingsObjectJsonSerializer();
		((DefaultContractResolver)serializer.ContractResolver).IgnoreShouldSerializeMembers = true;
		serializer.Serialize(writer, settings);
		return writer.ToString();
	}

	/// <summary>Restores values into an existing root while retaining its identity, provider and subscriptions.</summary>
	public static void PopulateValues(string values, SettingsObject settings) {
		Guard.ArgumentNotNull(values, nameof(values));
		Guard.ArgumentNotNull(settings, nameof(settings));
		var id = settings.ID;
		var provider = settings.Provider;
		var record = JObject.Parse(values);
		var serializer = new SettingsObjectJsonSerializer { ObjectCreationHandling = ObjectCreationHandling.Replace };
		var contract = (JsonObjectContract)serializer.ContractResolver.ResolveContract(settings.GetType());

		// Getter-only collections retain their identity while their saved membership is restored.
		foreach (var property in contract.Properties) {
			if (property.Ignored || !property.Readable || property.Writable
				|| !record.TryGetValue(property.PropertyName, StringComparison.OrdinalIgnoreCase, out _))
				continue;
			var value = property.ValueProvider.GetValue(settings);
			if (value is IList list && !list.IsReadOnly && !list.IsFixedSize) {
				list.Clear();
				property.ObjectCreationHandling = ObjectCreationHandling.Reuse;
			} else if (value is IDictionary dictionary && !dictionary.IsReadOnly && !dictionary.IsFixedSize) {
				dictionary.Clear();
				property.ObjectCreationHandling = ObjectCreationHandling.Reuse;
			}
		}

		// Even an incomplete payload cannot replace the owner's runtime record identity or provider.
		try {
			using var reader = record.CreateReader();
			serializer.Populate(reader, settings);
		} finally {
			settings.ID = id;
			settings.Provider = provider;
		}
	}

	#region Inner Types

	// Collections supplied by storage replace defaults, including explicitly empty collections.
	private sealed class SettingsContractResolver : DefaultContractResolver {
		// Filter runtime members without changing the existing population of getter-only collections.
		protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization) {
			var property = base.CreateProperty(member, memberSerialization);
			if (!ApplicationHelper.IsSettingsMember(member))
				property.Ignored = true;
			if (property.Writable && property.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
				property.ObjectCreationHandling = ObjectCreationHandling.Replace;
			return property;
		}
	}

	#endregion
}

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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;

/// <summary>Edits plain detached settings values using their component-model names, descriptions and read-only metadata.</summary>
public partial class BlazorApplicationSettingsValueEditor : ComponentBase {
	private readonly string _inputPrefix = "option-" + Guid.NewGuid().ToString("N");
	private readonly Dictionary<string, string> _dictionaryKeys = new();

	/// <summary>The detached object whose browsable value properties are edited.</summary>
	[Parameter] public object Value { get; set; }

	/// <summary>Receives completed edits, including invalid text that needs to remain visible for correction.</summary>
	[Parameter] public EventCallback Changed { get; set; }

	/// <summary>Disables editing for a containing read-only section.</summary>
	[Parameter] public bool ReadOnly { get; set; }

	/// <summary>Identifies nested editor input without using persistence identifiers.</summary>
	[Parameter] public string Prefix { get; set; } = string.Empty;

	/// <summary>Collects parse errors for the containing detached edit session.</summary>
	[Parameter] public IDictionary<string, string> Errors { get; set; } = new Dictionary<string, string>();

	/// <summary>Bounds recursive display of shared or cyclic values.</summary>
	[Parameter] public object[] Ancestors { get; set; } = Array.Empty<object>();

	// Persistence identity and provider ownership never appear as editable preferences.
	private PropertyDescriptor[] Properties {
		get {
			return Value == null ? Array.Empty<PropertyDescriptor>() : TypeDescriptor.GetProperties(Value).Cast<PropertyDescriptor>()
				.Where(property => property.IsBrowsable && !(Value is SettingsObject && property.Name is nameof(SettingsObject.ID) or nameof(SettingsObject.Provider)))
				.OrderBy(property => property.Category, StringComparer.CurrentCulture)
				.ThenBy(property => property.DisplayName, StringComparer.CurrentCulture).ToArray();
		}
	}

	// Parsing belongs to the editor; settings and product validation remain the edit session's responsibility.
	private async Task ChangeValueAsync(PropertyDescriptor property, object input) {
		var path = GetPath(property);
		try {
			var parsed = ParseValue(input, property.PropertyType);
			if (Equals(parsed, property.GetValue(Value)) && !Errors.ContainsKey(path))
				return;
			if (!Equals(parsed, property.GetValue(Value)))
				property.SetValue(Value, parsed);
			Errors.Remove(path);
		} catch (Exception error) when (error is ArgumentException or FormatException or OverflowException or NotSupportedException) {
			Errors[path] = error.Message;
		}
		await Changed.InvokeAsync();
	}

	// Collection edits replace array membership while retaining the detached element objects.
	private async Task ChangeItemAsync(PropertyDescriptor property, int index, object input) {
		var path = GetPath(property) + "." + index;
		try {
			var items = (IList)property.GetValue(Value);
			var parsed = ParseValue(input, GetElementType(property.PropertyType));
			if (Equals(items[index], parsed) && !Errors.ContainsKey(path))
				return;
			items[index] = parsed;
			Errors.Remove(path);
		} catch (Exception error) when (error is ArgumentException or FormatException or OverflowException or NotSupportedException) {
			Errors[path] = error.Message;
		}
		await Changed.InvokeAsync();
	}

	// A dictionary entry has its own input/error identity; editing one role cannot clear another role's invalid input.
	private async Task ChangeDictionaryItemAsync(PropertyDescriptor property, string key, object input) {
		var path = GetDictionaryPath(GetPath(property), key);
		try {
			var items = (IDictionary)property.GetValue(Value);
			var parsed = ParseValue(input, GetDictionaryValueType(property.PropertyType));
			if (Equals(items[key], parsed) && !Errors.ContainsKey(path))
				return;
			items[key] = parsed;
			Errors.Remove(path);
		} catch (Exception error) when (error is ArgumentException or FormatException or OverflowException or NotSupportedException) {
			Errors[path] = error.Message;
		}
		await Changed.InvokeAsync();
	}

	// Ordinary named collections can add values; declared grid-role membership remains schema-owned.
	private async Task AddDictionaryItemAsync(PropertyDescriptor property) {
		var path = GetPath(property);
		var key = GetDictionaryKey(path);
		var items = property.GetValue(Value) as IDictionary;
		if (string.IsNullOrWhiteSpace(key) || items?.Contains(key) == true) {
			Errors[path] = "Enter a nonempty key that is not already in this collection.";
			await Changed.InvokeAsync();
			return;
		}
		if (items == null) {
			items = (IDictionary)CreateValue(property.PropertyType);
			property.SetValue(Value, items);
		}
		items.Add(key, CreateValue(GetDictionaryValueType(property.PropertyType)));
		_dictionaryKeys.Remove(path);
		Errors.Remove(path);
		await Changed.InvokeAsync();
	}

	// Only the removed named item's pending input is abandoned.
	private async Task RemoveDictionaryItemAsync(PropertyDescriptor property, string key) {
		((IDictionary)property.GetValue(Value)).Remove(key);
		var path = GetDictionaryPath(GetPath(property), key);
		foreach (var errorKey in Errors.Keys.Where(errorKey => errorKey == path || errorKey.StartsWith(path + ".", StringComparison.Ordinal)).ToArray())
			Errors.Remove(errorKey);
		await Changed.InvokeAsync();
	}

	// New values remain in the draft until the dialog explicitly accepts them.
	private async Task CreateValueAsync(PropertyDescriptor property) {
		property.SetValue(Value, CreateValue(property.PropertyType));
		await Changed.InvokeAsync();
	}

	// Arrays are the normal settings collection shape; mutable IList schemas remain supported too.
	private async Task AddItemAsync(PropertyDescriptor property) {
		var itemType = GetElementType(property.PropertyType);
		var items = property.GetValue(Value) as IList;
		if (property.PropertyType.IsArray) {
			var replacement = Array.CreateInstance(itemType, (items?.Count ?? 0) + 1);
			items?.CopyTo(replacement, 0);
			replacement.SetValue(CreateValue(itemType), replacement.Length - 1);
			property.SetValue(Value, replacement);
		} else {
			if (items == null) {
				items = (IList)CreateValue(property.PropertyType);
				property.SetValue(Value, items);
			}
			items.Add(CreateValue(itemType));
		}
		await Changed.InvokeAsync();
	}

	// Removing a member also discards input errors belonging to the old collection positions.
	private async Task RemoveItemAsync(PropertyDescriptor property, int index) {
		var items = (IList)property.GetValue(Value);
		if (property.PropertyType.IsArray) {
			var replacement = Array.CreateInstance(GetElementType(property.PropertyType), items.Count - 1);
			for (var source = 0; source < items.Count; source++)
				if (source != index)
					replacement.SetValue(items[source], source < index ? source : source - 1);
			property.SetValue(Value, replacement);
		} else
			items.RemoveAt(index);

		var path = GetPath(property);
		foreach (var key in Errors.Keys.Where(key => key == path || key.StartsWith(path + ".", StringComparison.Ordinal)).ToArray())
			Errors.Remove(key);
		await Changed.InvokeAsync();
	}

	// Type converters preserve the declared scalar type instead of guessing from input text.
	private static object ParseValue(object input, Type type) {
		if (input is bool boolean && type == typeof(bool))
			return boolean;
		var text = Convert.ToString(input, CultureInfo.CurrentCulture) ?? string.Empty;
		if (text.Length == 0 && Nullable.GetUnderlyingType(type) != null)
			return null;
		return type == typeof(string) ? text : TypeDescriptor.GetConverter(type).ConvertFrom(null, CultureInfo.CurrentCulture, text);
	}

	// The same converter formats scalar values for both editable and read-only fields.
	private static string FormatValue(object value, Type type) {
		return value == null ? string.Empty : TypeDescriptor.GetConverter(type).ConvertToString(null, CultureInfo.CurrentCulture, value);
	}

	// Scalar converters include nullable primitives, enums, dates and other standard value types.
	private static bool IsScalar(Type type) {
		var converter = TypeDescriptor.GetConverter(type);
		return type == typeof(string) || converter.CanConvertFrom(typeof(string)) && converter.CanConvertTo(typeof(string));
	}

	// A schema fixes the set of known grid columns; only their nullable manual widths are editable.
	private static bool IsColumnSchema(Type type) {
		return GetElementType(type) == typeof(ApplicationGridColumnPreferences);
	}

	// Collection element type comes from the declared schema, including an empty array.
	private static Type GetElementType(Type type) {
		return type.IsArray ? type.GetElementType() : type.GetInterfaces().Append(type)
			.FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IList<>))?.GetGenericArguments()[0] ?? typeof(object);
	}

	// String keys are stable readable role identities; other dictionary schemas can supply a dedicated editor.
	private static Type GetDictionaryValueType(Type type) {
		var dictionary = type.GetInterfaces().Append(type).FirstOrDefault(candidate => candidate.IsGenericType
			&& candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>) && candidate.GetGenericArguments()[0] == typeof(string));
		return dictionary?.GetGenericArguments()[1];
	}

	// Enumerates actual value entries rather than framework collection implementation properties.
	private static DictionaryEntry[] GetDictionaryEntries(object value) {
		if (value is not IDictionary dictionary)
			return Array.Empty<DictionaryEntry>();
		var entries = new List<DictionaryEntry>();
		foreach (DictionaryEntry entry in dictionary)
			entries.Add(entry);
		return entries.ToArray();
	}

	// Role paths follow the catalog's migration marker syntax, separate from displayed captions.
	private static string GetDictionaryPath(string path, string key) {
		return path + "[" + key.Replace("\\", "\\\\").Replace("]", "\\]") + "]";
	}

	// New-key text is provisional editor state and never enters the settings payload until Add succeeds.
	private string GetDictionaryKey(string path) {
		return _dictionaryKeys.GetValueOrDefault(path, string.Empty);
	}

	// Correcting an attempted key clears its local validation message without creating a preference record.
	private void SetDictionaryKey(string path, string key) {
		_dictionaryKeys[path] = key;
		Errors.Remove(path);
	}

	// Readable grid-column titles make an unopened page's width entries recognizable.
	private static string GetItemTitle(object item, int index) {
		return item is ApplicationGridColumnPreferences column ? column.Title ?? column.Key : "Item " + (index + 1);
	}

	// Enum descriptions are presentation metadata; the stored value remains the declared enum.
	private static string GetEnumTitle(object value) {
		return value.GetType().GetField(value.ToString())?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? value.ToString();
	}

	// Only value schemas with an ordinary construction path offer Add/Set controls.
	private static bool CanCreate(Type type) {
		return type == typeof(string) || type.IsValueType || !type.IsAbstract && !type.IsInterface && type.GetConstructor(Type.EmptyTypes) != null;
	}

	// Constructors provide nested draft defaults; they never activate operational screens.
	private static object CreateValue(Type type) {
		return type == typeof(string) ? string.Empty : Tools.Object.Create(type);
	}

	// Paths are local editor coordinates, separate from stable persistence keys.
	private string GetPath(PropertyDescriptor property) {
		return Prefix.Length == 0 ? property.Name : Prefix + "." + property.Name;
	}

	// Each recursive component owns its input IDs, avoiding duplicate labels across options pages.
	private string GetInputId(string path) {
		return _inputPrefix + "-" + path;
	}
}

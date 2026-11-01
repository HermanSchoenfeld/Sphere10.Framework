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
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Sphere10.Framework.Application.UI;

// Capture only declared preference values; the general cloner also copies private provider and runtime fields.
internal sealed class ApplicationSettingsValueCloner : DeepObjectCloner {
	// No serializer runs while the caller is capturing a live UI-owned model.
	public static object Copy(object values) {
		Guard.ArgumentNotNull(values, nameof(values));
		return new ApplicationSettingsValueCloner().Clone(values);
	}

	// Reuse the existing identity map while explicitly constructing arrays and mutable collection elements.
	protected override object DeepClone(object source, IDictionary<Reference<object>, object> clones) {
		if (source == null)
			return null;
		var type = source.GetType();
		if (IsScalar(type))
			return source;
		Guard.Argument(!IsRuntimeType(type), nameof(source), "Settings snapshots contain values, not live services or UI owners.");
		var key = Reference.For(source);
		if (clones.TryGetValue(key, out var existing))
			return existing;

		if (source is Array array)
			return CopyArray(array, clones);
		if (source is IDictionary dictionary) {
			var copy = (IDictionary)ActivateObject(type, source);
			clones.Add(key, copy);
			foreach (DictionaryEntry item in dictionary)
				copy.Add(DeepClone(item.Key, clones), DeepClone(item.Value, clones));
			return copy;
		}
		if (source is IList list) {
			var copy = (IList)ActivateObject(type, source);
			clones.Add(key, copy);
			foreach (var item in list)
				copy.Add(DeepClone(item, clones));
			return copy;
		}

		var collection = type.GetInterfaces().FirstOrDefault(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(ICollection<>));
		if (source is IEnumerable items && collection != null) {
			var copy = ActivateObject(type, source);
			clones.Add(key, copy);
			var add = collection.GetMethod(nameof(ICollection<object>.Add));
			foreach (var item in items)
				add.Invoke(copy, new[] { DeepClone(item, clones) });
			return copy;
		}
		return base.DeepClone(source, clones);
	}

	// Preserve a dictionary/set comparer when its ordinary constructor accepts that declared comparer contract.
	protected override object ActivateObject(Type type, object sourceObjectHint) {
		if (type.IsValueType)
			return Activator.CreateInstance(type);
		var comparer = type.GetProperty("Comparer", BindingFlags.Instance | BindingFlags.Public);
		var constructor = comparer == null ? null : type.GetConstructor(new[] { comparer.PropertyType });
		if (constructor != null)
			return constructor.Invoke(new[] { comparer.GetValue(sourceObjectHint) });

		// Supplied ordinary values may require constructor arguments; their public values supply the clone's state.
		return type.GetConstructor(Type.EmptyTypes) == null
			? RuntimeHelpers.GetUninitializedObject(type)
			: base.ActivateObject(type, sourceObjectHint);
	}

	// Public serializable members describe preferences; private fields and ignored persistence context do not.
	protected override void DeepCopyMembers(object source, object dest, IDictionary<Reference<object>, object> clones) {
		foreach (var field in source.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
			if (!field.IsInitOnly && !IsIgnored(source, field, field.FieldType))
				field.SetValue(dest, DeepClone(field.GetValue(source), clones));
		foreach (var property in source.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
			if (property.GetIndexParameters().Length != 0 || property.GetMethod?.IsPublic != true || property.SetMethod?.IsPublic != true
				|| IsIgnored(source, property, property.PropertyType))
				continue;
			property.SetValue(dest, DeepClone(property.GetValue(source), clones));
		}
		if (dest is SettingsObject settings) {
			settings.ID = null;
			settings.Provider = null;
		}
	}

	// Array rank and lower bounds are part of the value shape, including arrays inside nested collections.
	private Array CopyArray(Array source, IDictionary<Reference<object>, object> clones) {
		var lengths = Enumerable.Range(0, source.Rank).Select(source.GetLength).ToArray();
		var lowerBounds = Enumerable.Range(0, source.Rank).Select(source.GetLowerBound).ToArray();
		var copy = source.Rank == 1 && lowerBounds[0] == 0
			? Array.CreateInstance(source.GetType().GetElementType(), lengths[0])
			: Array.CreateInstance(source.GetType().GetElementType(), lengths, lowerBounds);
		clones.Add(Reference.For((object)source), copy);
		var indices = (int[])lowerBounds.Clone();
		for (var item = 0; item < source.Length; item++) {
			copy.SetValue(DeepClone(source.GetValue(indices), clones), indices);
			for (var dimension = source.Rank - 1; dimension >= 0; dimension--) {
				indices[dimension]++;
				if (indices[dimension] <= source.GetUpperBound(dimension))
					break;
				indices[dimension] = lowerBounds[dimension];
			}
		}
		return copy;
	}

	// These framework value types are immutable and need no constructor/member traversal.
	internal static bool IsScalar(Type type) {
		return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime)
			|| type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(TimeSpan)
			|| type == typeof(Guid) || type == typeof(Uri) || type == typeof(Version)
			|| type.IsValueType && type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.All(field => IsScalar(field.FieldType));
	}

	// Ignored values never cause a provider or captured UI/service graph to be traversed.
	internal static bool IsIgnored(object owner, MemberInfo member, Type memberType) {
		return owner is SettingsObject && member.Name is nameof(SettingsObject.ID) or nameof(SettingsObject.Provider)
			|| IsRuntimeType(memberType) || member.IsDefined(typeof(JsonIgnoreAttribute)) || member.IsDefined(typeof(XmlIgnoreAttribute))
			|| member.IsDefined(typeof(NonSerializedAttribute));
	}

	// These contracts identify runtime ownership rather than serializable preference data.
	private static bool IsRuntimeType(Type type) {
		return typeof(Delegate).IsAssignableFrom(type) || typeof(IServiceProvider).IsAssignableFrom(type) || typeof(ISettingsProvider).IsAssignableFrom(type)
			|| typeof(IApplication).IsAssignableFrom(type) || typeof(IApplicationScreen).IsAssignableFrom(type) || typeof(IApplicationScreenHost).IsAssignableFrom(type);
	}
}

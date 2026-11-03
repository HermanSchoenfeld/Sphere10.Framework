// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework.Windows.Forms.Text.FixedLength;

/// <summary>
/// A dictionary with keys of type string and values of type IField
/// </summary>
public class FieldList : DictionaryDecorator<string, IField, Dictionary<string, IField>> {
	/// <summary>
	/// Initializes a new empty instance of the FieldList class
	/// </summary>
	public FieldList()
		: base(new Dictionary<string, IField>()) {
		// empty
	}

	/// <summary>
	/// Gets or sets the IField associated with the given string
	/// </summary>
	/// <param name="key">
	/// The string whose value to get or set.
	/// </param>
	public override IField this[string key] {
		get {
			TryGetValue(key, out var field);
			return field;
		}
		set {
			Guard.ArgumentNotNull(key, nameof(key));
			Guard.ArgumentNotNull(value, nameof(value));
			InternalDictionary[key] = value;
		}
	}

	/// <summary>
	/// Gets a collection containing the keys in this FieldList.
	/// </summary>
	public override ICollection<string> Keys => InternalDictionary.Keys;

	/// <summary>
	/// Gets a collection containing the values in this FieldList.
	/// </summary>
	public override ICollection<IField> Values => InternalDictionary.Values;

	/// <summary>
	/// Adds an element with the specified key and value to this FieldList.
	/// </summary>
	/// <param name="value">
	/// The IField value of the element to add.
	/// </param>
	public virtual void Add(IField value) {
		Guard.ArgumentNotNull(value, nameof(value));
		Add(value.Name, value);
	}

	/// <summary>Adds a named field while retaining the collection's rejection of null fields.</summary>
	public override void Add(string key, IField value) {
		Guard.ArgumentNotNull(key, nameof(key));
		Guard.ArgumentNotNull(value, nameof(value));
		InternalDictionary.Add(key, value);
	}

	/// <summary>Applies the same field validation when adding through the generic collection interface.</summary>
	public override void Add(KeyValuePair<string, IField> item) {
		Add(item.Key, item.Value);
	}

	/// <summary>
	/// Determines whether this FieldList contains a specific key.
	/// </summary>
	/// <returns>
	/// true if this FieldList contains an element with the specified key;
	/// otherwise, false.
	/// </returns>
	public override bool ContainsKey(string fieldName) {
		return InternalDictionary.ContainsKey(fieldName);
	}

	/// <summary>
	/// Determines whether this FieldList contains a specific key.
	/// </summary>
	/// <param name="fieldName">
	/// The string key to locate in this FieldList.
	/// </param>
	/// <returns>
	/// true if this FieldList contains an element with the specified key;
	/// otherwise, false.
	/// </returns>
	public virtual bool Contains(string fieldName) {
		return ContainsKey(fieldName);
	}

	/// <summary>
	/// Determines whether this FieldList contains a specific value.
	/// </summary>
	/// <param name="value">
	/// The IField value to locate in this FieldList.
	/// </param>
	/// <returns>
	/// true if this FieldList contains an element with the specified value;
	/// otherwise, false.
	/// </returns>
	public virtual bool ContainsValue(IField value) {
		foreach (var item in Values) {
			if (ReferenceEquals(item, value))
				return true;
		}
		return false;
	}

	/// <summary>
	/// Removes the element with the specified key from this FieldList.
	/// </summary>
	/// <param name="fieldName">
	/// The string key of the element to remove.
	/// </param>
	public override bool Remove(string fieldName) {
		return InternalDictionary.Remove(fieldName);
	}

	public IField[] GetSortedList() {
		// Resolve every declared position and reject gaps in the field sequence.
		var fields = new IField[Count];
		for (var index = 0; index < fields.Length; index++) {
			foreach (var field in Values) {
				if (field.Index == index) {
					fields[index] = field;
					break;
				}
			}
			if (fields[index] == null)
				throw new FieldNotDefinedException(index);
		}

		return fields;
	}
}

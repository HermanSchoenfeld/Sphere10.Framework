// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// A dictionary with keys of type Guid and values of type Control
/// </summary>
public class ControlsRepository : IReadOnlyDictionary<Guid, Control> {
	private readonly Control _parentControl;
	private readonly Dictionary<Guid, Control> _controls = new();

	/// <summary>
	/// Initializes a new empty instance of the ControlsRepository class
	/// </summary>
	public ControlsRepository(Control parentControl) => _parentControl = parentControl;

	/// <summary>
	/// Gets a collection containing the keys in this ControlsRepository.
	/// </summary>
	public virtual ICollection<Guid> Keys => _controls.Keys;

	/// <summary>
	/// Gets a collection containing the values in this ControlsRepository.
	/// </summary>
	public virtual ICollection<Control> Values => _controls.Values;

	/// <summary>Gets the number of registered controls.</summary>
	public int Count => _controls.Count;

	/// <summary>
	/// Gets or sets the Control associated with the given Guid
	/// </summary>
	/// <param name="key">
	/// The Guid whose value to get or set.
	/// </param>
	public virtual Control this[Guid key] {
		get => _controls.TryGetValue(key, out var control) ? control : null;
		/*			set
					{
						this.Dictionary[key] = value;
					}*/
	}

	// Read-only dictionary views retain the same live membership as the public typed collections.
	IEnumerable<Guid> IReadOnlyDictionary<Guid, Control>.Keys => Keys;

	IEnumerable<Control> IReadOnlyDictionary<Guid, Control>.Values => Values;

	/// <summary>
	/// Adds an element with the specified key and value to this ControlsRepository.
	/// </summary>
	/// <param name="key">
	/// The Guid key of the element to add.
	/// </param>
	/// <param name="value">
	/// The Control value of the element to add.
	/// </param>
	public virtual void Add(Guid key, Control value) {
		Guard.ArgumentNotNull(value, nameof(value));
		_controls.Add(key, value);
		_parentControl.Controls.Add(value);
	}

	/// <summary>
	/// Determines whether this ControlsRepository contains a specific value.
	/// </summary>
	/// <param name="value">
	/// The Control value to locate in this ControlsRepository.
	/// </param>
	/// <returns>
	/// true if this ControlsRepository contains an element with the specified value;
	/// otherwise, false.
	/// </returns>
	public virtual bool ContainsValue(Control value) {
		foreach (var item in _controls.Values) {
			if (item == value)
				return true;
		}
		return false;
	}

	/// <summary>
	/// Removes the element with the specified key from this ControlsRepository.
	/// </summary>
	/// <param name="key">
	/// The Guid key of the element to remove.
	/// </param>
	public virtual void Remove(Guid key) {
		if (_controls.TryGetValue(key, out var control)) {
			_parentControl.Controls.Remove(control);
			_controls.Remove(key);
		}
	}

	/// <summary>Clears registrations while leaving the borrowed controls attached to their parent.</summary>
	public virtual void Clear() => _controls.Clear();

	/// <summary>
	/// Determines whether this ControlsRepository contains a specific key.
	/// </summary>
	/// <param name="key">
	/// The Guid key to locate in this ControlsRepository.
	/// </param>
	/// <returns>
	/// true if this ControlsRepository contains an element with the specified key;
	/// otherwise, false.
	/// </returns>
	public virtual bool ContainsKey(Guid key) => _controls.ContainsKey(key);

	/// <summary>
	/// Determines whether this ControlsRepository contains a specific key.
	/// </summary>
	/// <param name="key">
	/// The Guid key to locate in this ControlsRepository.
	/// </param>
	/// <returns>
	/// true if this ControlsRepository contains an element with the specified key;
	/// otherwise, false.
	/// </returns>
	public virtual bool Contains(Guid key) => _controls.ContainsKey(key);

	/// <summary>Reads a registered control without changing its native parent or ownership.</summary>
	public bool TryGetValue(Guid key, out Control value) => _controls.TryGetValue(key, out value);

	/// <summary>Enumerates registrations without exposing a writable dictionary interface.</summary>
	public IEnumerator<KeyValuePair<Guid, Control>> GetEnumerator() => _controls.GetEnumerator();

	// Legacy enumeration remains available through the typed registration cursor.
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

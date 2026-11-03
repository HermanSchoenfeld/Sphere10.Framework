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

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// A collection of elements of type Cells.ICellVirtual
/// </summary>
public class CellCollection : IList<Cells.ICellVirtual>, IReadOnlyList<Cells.ICellVirtual> {
	private readonly List<Cells.ICellVirtual> _items = new();

	/// <summary>
	/// Initializes a new empty instance of the CellBaseCollection class.
	/// </summary>
	public CellCollection() {
		// empty
	}

	/// <summary>
	/// Initializes a new instance of the CellBaseCollection class, containing elements
	/// copied from an array.
	/// </summary>
	/// <param name="items">
	/// The array whose elements are to be added to the new CellBaseCollection.
	/// </param>
	public CellCollection(Cells.ICellVirtual[] items) {
		this.AddRange(items);
	}

	/// <summary>
	/// Initializes a new instance of the CellBaseCollection class, containing elements
	/// copied from another instance of CellBaseCollection
	/// </summary>
	/// <param name="items">
	/// The CellBaseCollection whose elements are to be added to the new CellBaseCollection.
	/// </param>
	public CellCollection(CellCollection items) {
		this.AddRange(items);
	}

	/// <summary>Gets the number of cells in the collection.</summary>
	public int Count => _items.Count;

	/// <summary>Gets or sets the allocated capacity without changing collection membership.</summary>
	public int Capacity {
		get => _items.Capacity;
		set => _items.Capacity = value;
	}

	/// <summary>Gets whether the collection rejects membership changes.</summary>
	public bool IsReadOnly => false;

	/// <summary>
	/// Gets or sets the Cells.ICellVirtual at the given index in this CellBaseCollection.
	/// </summary>
	public virtual Cells.ICellVirtual this[int index] {
		get {
			return _items[index];
		}
		set {
			Guard.ArgumentNotNull(value, nameof(value));
			_items[index] = value;
		}
	}

	/// <summary>
	/// Adds the elements of an array to the end of this CellBaseCollection.
	/// </summary>
	/// <param name="items">
	/// The array whose elements are to be added to the end of this CellBaseCollection.
	/// </param>
	public virtual void AddRange(Cells.ICellVirtual[] items) {
		foreach (var item in items)
			Add(item);
	}

	/// <summary>
	/// Adds the elements of another CellBaseCollection to the end of this CellBaseCollection.
	/// </summary>
	/// <param name="items">
	/// The CellBaseCollection whose elements are to be added to the end of this CellBaseCollection.
	/// </param>
	public virtual void AddRange(CellCollection items) {
		foreach (var item in items)
			Add(item);
	}

	/// <summary>
	/// Adds an instance of type Cells.ICellVirtual to the end of this CellBaseCollection.
	/// </summary>
	/// <param name="value">
	/// The Cells.ICellVirtual to be added to the end of this CellBaseCollection.
	/// </param>
	public virtual void Add(Cells.ICellVirtual value) {
		Guard.ArgumentNotNull(value, nameof(value));
		_items.Add(value);
	}

	/// <summary>
	/// Determines whether a specfic Cells.ICellVirtual value is in this CellBaseCollection.
	/// </summary>
	/// <param name="value">
	/// The Cells.ICellVirtual value to locate in this CellBaseCollection.
	/// </param>
	/// <returns>
	/// true if value is found in this CellBaseCollection;
	/// false otherwise.
	/// </returns>
	public virtual bool Contains(Cells.ICellVirtual value) {
		return _items.Contains(value);
	}

	/// <summary>
	/// Return the zero-based index of the first occurrence of a specific value
	/// in this CellBaseCollection
	/// </summary>
	/// <param name="value">
	/// The Cells.ICellVirtual value to locate in the CellBaseCollection.
	/// </param>
	/// <returns>
	/// The zero-based index of the first occurrence of the _ELEMENT value if found;
	/// -1 otherwise.
	/// </returns>
	public virtual int IndexOf(Cells.ICellVirtual value) {
		return _items.IndexOf(value);
	}

	/// <summary>
	/// Inserts an element into the CellBaseCollection at the specified index
	/// </summary>
	/// <param name="index">
	/// The index at which the Cells.ICellVirtual is to be inserted.
	/// </param>
	/// <param name="value">
	/// The Cells.ICellVirtual to insert.
	/// </param>
	public virtual void Insert(int index, Cells.ICellVirtual value) {
		Guard.ArgumentNotNull(value, nameof(value));
		_items.Insert(index, value);
	}

	/// <summary>
	/// Removes the first occurrence of a specific Cells.ICellVirtual from this CellBaseCollection.
	/// </summary>
	/// <param name="value">
	/// The Cells.ICellVirtual value to remove from this CellBaseCollection.
	/// </param>
	public virtual void Remove(Cells.ICellVirtual value) {
		Guard.ArgumentNotNull(value, nameof(value));
		if (!_items.Remove(value))
			throw new ArgumentException("The cell is not part of this collection.", nameof(value));
	}

	/// <summary>Removes the cell at its zero-based position.</summary>
	public virtual void RemoveAt(int index) {
		_items.RemoveAt(index);
	}

	/// <summary>Removes all cells without disposing their borrowed contents.</summary>
	public virtual void Clear() {
		_items.Clear();
	}

	/// <summary>Copies the current cells into the supplied typed array.</summary>
	public void CopyTo(Cells.ICellVirtual[] array, int arrayIndex) {
		_items.CopyTo(array, arrayIndex);
	}

	/// <summary>
	/// Returns an enumerator that can iterate through the elements of this CellBaseCollection.
	/// </summary>
	/// <returns>
	/// An object that implements System.Collections.IEnumerator.
	/// </returns>
	public virtual CellCollection.Enumerator GetEnumerator() {
		return new CellCollection.Enumerator(this);
	}

	// Generic collection removal reports absence while the original public method still rejects it.
	bool ICollection<Cells.ICellVirtual>.Remove(Cells.ICellVirtual item) {
		Guard.ArgumentNotNull(item, nameof(item));
		if (!Contains(item))
			return false;
		Remove(item);
		return true;
	}

	// Both enumeration interfaces use the same typed cursor and invalid-state checks.
	IEnumerator<Cells.ICellVirtual> IEnumerable<Cells.ICellVirtual>.GetEnumerator() {
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}

	/// <summary>
	/// Type-specific enumeration class, used by CellBaseCollection.GetEnumerator.
	/// </summary>
	public class Enumerator : IEnumerator<Cells.ICellVirtual> {
		private readonly IEnumerator<Cells.ICellVirtual> _wrapped;

		/// <summary>
		///
		/// </summary>
		/// <param name="collection"></param>
		public Enumerator(CellCollection collection) {
			_wrapped = collection._items.GetEnumerator();
		}

		/// <summary>
		///
		/// </summary>
		public Cells.ICellVirtual Current => (Cells.ICellVirtual)((IEnumerator)_wrapped).Current;

		/// <summary>
		///
		/// </summary>
		object System.Collections.IEnumerator.Current => Current;

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public bool MoveNext() {
			return _wrapped.MoveNext();
		}

		/// <summary>
		///
		/// </summary>
		public void Reset() {
			_wrapped.Reset();
		}

		/// <summary>Releases the wrapped cursor without disposing its cells or collection.</summary>
		public void Dispose() {
			_wrapped.Dispose();
		}
	}
}

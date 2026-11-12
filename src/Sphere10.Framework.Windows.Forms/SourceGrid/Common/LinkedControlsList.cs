// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// A dictionary with keys of type Control and values of type LinkedControlValue
/// </summary>
public class LinkedControlsList : IEnumerable<LinkedControlValue> {
	private Control _parent;
	private List<LinkedControlValue> _list = new List<LinkedControlValue>();

	public LinkedControlsList(Control parent) => _parent = parent;

	public void Clear() {
		foreach (var linkedControlValue in _list)
			RemoveFromParent(linkedControlValue);

		_list.Clear();
	}

	public void Add(LinkedControlValue linkedControl) {
		_list.Add(linkedControl);

		// Windows erases a new child window where it is created, so the control is placed over its cell first. Otherwise it flashes at the
		// grid's origin until the next repaint.
		(_parent as GridVirtual)?.ArrangeLinkedControl(linkedControl);
		_parent.Controls.Add(linkedControl.Control);
	}

	public LinkedControlValue GetByControl(Control control) {
		for (var index = 0; index < _list.Count; index++) {
			if (control == _list[index].Control)
				return _list[index];
		}

		return null;
	}

	#region IEnumerable<LinkedControlValue> Members

	public IEnumerator<LinkedControlValue> GetEnumerator() => _list.GetEnumerator();

	#endregion

	#region IEnumerable Members

	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _list.GetEnumerator();

	#endregion

	public void Remove(LinkedControlValue linkedControl) {
		_list.Remove(linkedControl);
		RemoveFromParent(linkedControl);
	}

	private void RemoveFromParent(LinkedControlValue linkedControl) => _parent.Controls.Remove(linkedControl.Control);
}

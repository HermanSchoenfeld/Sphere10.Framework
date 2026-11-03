// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Controllers;

/// <summary>
/// Summary description for BindProperty.
/// </summary>
public class BindProperty : ControllerBase {
	// Bind Property
	//proprietà per gestire il link di una cella ad una property
	private System.Reflection.PropertyInfo _linkPropertyInfo = null;
	private object _linkObject = null;

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="property">Property of the object that you want to lint to the cell. When the value of the cell changed this behavior call automatically this property.</param>
	/// <param name="linkObject">Instance of the object to link</param>
	public BindProperty(System.Reflection.PropertyInfo property, object linkObject) {
		BindValueAtProperty(property, linkObject);
	}

	/// <summary>
	/// OnValueChanged
	/// </summary>
	public override void OnValueChanged(CellContext sender, EventArgs e) {
		base.OnValueChanged(sender, e);

		//gestione eventuale link ad una property
		if (_linkPropertyInfo != null)
			_linkPropertyInfo.SetValue(_linkObject, sender.Cell.Model.ValueModel.GetValue(sender), null);
	}

	/// <summary>
	/// Bind the cell's value with the property p_Property of the object p_LinkObject
	/// when the cell's value change also the property change
	/// </summary>
	/// <param name="property">linked property</param>
	/// <param name="linkObject">Can be null to call static property</param>
	protected virtual void BindValueAtProperty(System.Reflection.PropertyInfo property, object linkObject) {
		_linkPropertyInfo = property;
		_linkObject = linkObject;
	}

	/// <summary>
	/// UnBind the cell with the property
	/// </summary>
	protected virtual void UnBindValueAtProperty() {
		_linkPropertyInfo = null;
		_linkObject = null;
	}
	//		/// <summary>
	//		/// Method that can be used to refresh the cell value
	//		/// </summary>
	//		/// <param name="sender"></param>
	//		/// <param name="e"></param>
	//		public void BindObject_ValueChanged(object sender, EventArgs e)
	//		{
	//			if (m_LinkPropertyInfo!=null)
	//			{
	//				object tmp = m_LinkPropertyInfo.GetValue(m_LinkObject,null);
	//				if (tmp!=Value)
	//					Value = tmp;
	//			}
	//		}
}

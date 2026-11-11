// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.ComponentModel;

/// <summary>
/// The ValueMapping class can be used to easily map a value to a string value or a display string for conversion
/// </summary>
public class ValueMapping {
	private System.Collections.IList _valueList;
	private Type _specialType = typeof(string);
	private System.Collections.IList _specialList;
	private System.Collections.IList _displayStringList;
	private bool _throwErrorIfNotFound = true;

	/// <summary>
	/// Constructor
	/// </summary>
	public ValueMapping() {
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="validator"></param>
	/// <param name="valueList">A list of valid value. If null an error occurred. The index must match the index of ValueList, ObjectList and DisplayStringList</param>
	/// <param name="displayStringList">A list of displayString. Can be null. The index must match the index of ValueList, ObjectList and DisplayStringList</param>
	/// <param name="specialList">A list of object that can be converted to value. Can be null. The index must match the index of ValueList, ObjectList and DisplayStringList</param>
	/// <param name="specialType">The type of object stored in the specialList collection.</param>
	public ValueMapping(IValidator validator, System.Collections.IList valueList, System.Collections.IList displayStringList, System.Collections.IList specialList, Type specialType) {
		ValueList = valueList;
		DisplayStringList = displayStringList;
		SpecialList = specialList;
		if (validator != null)
			BindValidator(validator);
	}

	/// <summary>
	/// A list of valid value. If null an error occurred. The index must match the index of ValueList and DisplayStringList
	/// </summary>
	public System.Collections.IList ValueList {
		get => _valueList;
		set => _valueList = value;
	}

	/// <summary>
	/// A list of object that can be converted to value. Can be null. The index must match the index of ValueList and DisplayStringList. Must be a list of object of the type specified in the SpecialType property.
	/// Usually this property can be used when performing special conversion of specific type. For example if you want to map an enum value or an id value to a string for a better user experience.
	/// </summary>
	public System.Collections.IList SpecialList {
		get => _specialList;
		set => _specialList = value;
	}

	/// <summary>
	/// Gets or sets the type used for converting an object to a value and a value to an object when populating the SpecialList property. Default is System.String.
	/// </summary>
	public Type SpecialType {
		get => _specialType;
		set => _specialType = value;
	}

	/// <summary>
	/// A list of displayString. Can be null. The index must match the index of ValueList and DisplayStringList
	/// </summary>
	public System.Collections.IList DisplayStringList {
		get => _displayStringList;
		set => _displayStringList = value;
	}

	/// <summary>
	/// Gets or sets, if throw an error when the value if not found in one of the collections.
	/// Default true.
	/// </summary>
	public bool ThrowErrorIfNotFound {
		get => _throwErrorIfNotFound;
		set => _throwErrorIfNotFound = value;
	}

	/// <summary>
	/// Bind the specified validator
	/// </summary>
	/// <param name="validator"></param>
	public void BindValidator(IValidator validator) {
		validator.ConvertingValueToDisplayString += new ConvertingObjectEventHandler(ValidatorConvertingValueToDisplayString);
		validator.ConvertingObjectToValue += new ConvertingObjectEventHandler(ValidatorConvertingObjectToValue);
		validator.ConvertingValueToObject += new ConvertingObjectEventHandler(ValidatorConvertingValueToObject);
	}

	/// <summary>
	/// Unbind the specified validator
	/// </summary>
	/// <param name="validator"></param>
	public void UnBindValidator(IValidator validator) {
		validator.ConvertingValueToDisplayString -= new ConvertingObjectEventHandler(ValidatorConvertingValueToDisplayString);
		validator.ConvertingObjectToValue -= new ConvertingObjectEventHandler(ValidatorConvertingObjectToValue);
		validator.ConvertingValueToObject -= new ConvertingObjectEventHandler(ValidatorConvertingValueToObject);
	}

	// Maps an accepted value to its configured display label.
	private void ValidatorConvertingValueToDisplayString(object sender, ConvertingObjectEventArgs args) {
		if (_displayStringList != null) {
			if (_valueList == null)
				throw new ApplicationException("ValueList cannot be null");

			var index = _valueList.IndexOf(args.Value);
			if (index >= 0) {
				args.Value = _displayStringList[index];
				args.ConvertingStatus = ConvertingStatus.Completed;
			} else {
				if (_throwErrorIfNotFound)
					args.ConvertingStatus = ConvertingStatus.Error;
			}
		}
	}

	// Maps a configured external value into the validator value set.
	private void ValidatorConvertingObjectToValue(object sender, ConvertingObjectEventArgs args) {
		if (_specialList != null && args.Value != null && args.Value.GetType() == SpecialType) {
			if (_valueList == null)
				throw new ApplicationException("ValueList cannot be null");

			//Verifico se fa parte della lista di valori
			var index = _valueList.IndexOf(args.Value);
			if (index >= 0) {
				args.Value = _valueList[index];
				args.ConvertingStatus = ConvertingStatus.Completed;
			} else {
				//Verifico se fa parte della lista di oggetti, in questo caso restituisco il valore corrispondente
				index = _specialList.IndexOf(args.Value);
				if (index >= 0) {
					args.Value = _valueList[index];
					args.ConvertingStatus = ConvertingStatus.Completed;
				} else if (_throwErrorIfNotFound)
					args.ConvertingStatus = ConvertingStatus.Error;
			}
		}
	}

	// Maps a validator value back to its configured external representation.
	private void ValidatorConvertingValueToObject(object sender, ConvertingObjectEventArgs args) {
		if (_specialList != null && args.DestinationType == SpecialType) {
			if (_valueList == null)
				throw new ApplicationException("ValueList cannot be null");

			//Verifico se il valore è presente nella list, in questo caso restituisco l'oggetto associato
			var index = _valueList.IndexOf(args.Value);
			if (index >= 0) {
				args.Value = _specialList[index];
				args.ConvertingStatus = ConvertingStatus.Completed;
			} else {
				if (_throwErrorIfNotFound)
					args.ConvertingStatus = ConvertingStatus.Error;
			}
		}
	}
}

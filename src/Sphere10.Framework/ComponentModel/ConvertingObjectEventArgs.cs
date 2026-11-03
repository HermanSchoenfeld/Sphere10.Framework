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

public class ConvertingObjectEventArgs : System.EventArgs {
	private object _value;
	private Type _destinationType;
	private ConvertingStatus _convertingStatus = ConvertingStatus.Converting;

	public ConvertingObjectEventArgs(object value, Type destinationType) {
		_value = value;
		_destinationType = destinationType;
	}

	public object Value {
		get {
			return _value;
		}
		set {
			_value = value;
		}
	}

	/// <summary>
	/// Destination type to convert the Value. Can be null if no destination type is required.
	/// </summary>
	public Type DestinationType {
		get {
			return _destinationType;
		}
	}

	public ConvertingStatus ConvertingStatus {
		get {
			return _convertingStatus;
		}
		set {
			_convertingStatus = value;
		}
	}
}

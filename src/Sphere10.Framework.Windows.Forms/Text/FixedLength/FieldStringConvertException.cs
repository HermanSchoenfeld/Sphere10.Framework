// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.Text.FixedLength;

[Serializable]
public class FieldStringConvertException : ApplicationException {
	public FieldStringConvertException(string name, object value, Exception innerException)
		: base("Failed to convert to string field " + name + " '" + FieldStringConvertException.ObjectToStringForError(value) + "' - " + innerException.Message, innerException) {
	}

	[Obsolete("Formatter-based serialization is obsolete.", DiagnosticId = "SYSLIB0051")]
	protected FieldStringConvertException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext streamingContext)
		: base(info, streamingContext) {
	}

	/// <summary>
	/// Returns a string used for error description for a specified object. Usually used when printing the object for the error message when there is a conversion error.
	/// </summary>
	/// <param name="value"></param>
	private static string ObjectToStringForError(object value) {
		try {
			if (value == null)
				return "<null>";
			return value.ToString();
		} catch (Exception) {
			return "<object>";
		}
	}
}

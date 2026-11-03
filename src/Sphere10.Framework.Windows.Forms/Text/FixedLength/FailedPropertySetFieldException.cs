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
public class FailedPropertySetFieldException : ApplicationException {
	public FailedPropertySetFieldException(string field, Exception innerException)
		: base("Failed to set property for field " + field + " - " + innerException.Message, innerException) {
	}

	[Obsolete("Formatter-based serialization is obsolete.", DiagnosticId = "SYSLIB0051")]
	protected FailedPropertySetFieldException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext streamingContext)
		: base(info, streamingContext) {
	}
}

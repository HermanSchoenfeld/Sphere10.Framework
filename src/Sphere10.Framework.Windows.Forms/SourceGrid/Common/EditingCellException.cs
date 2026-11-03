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
using System.Runtime.Serialization;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

[Serializable]
public class EditingCellException : SourceGridException {
	public EditingCellException(Exception innerException)
		:
											base(innerException.Message, innerException) {
	}

	[Obsolete("Formatter-based serialization is obsolete.", DiagnosticId = "SYSLIB0051")]
	protected EditingCellException(SerializationInfo info, StreamingContext streamingContext)
		:
											base(info, streamingContext) {
	}
}

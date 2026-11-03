// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Sphere10.Framework.ComponentModel;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Windows.Forms.SourceGrid.Selection;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

[Serializable]
public class EndEditingException : SourceGridException {
	public EndEditingException(Exception innerException)
		: base(innerException.Message, innerException) {
	}

	[Obsolete("Formatter-based serialization is obsolete.", DiagnosticId = "SYSLIB0051")]
	protected EndEditingException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext streamingContext)
		: base(info, streamingContext) {
	}
}

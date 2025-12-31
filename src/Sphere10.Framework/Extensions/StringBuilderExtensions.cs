// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Text;

namespace Sphere10.Framework;

public static class StringBuilderExtensions {

	static public void AppendLine(this StringBuilder sb, string text, params object[] formatParams) {
		if (formatParams != null && formatParams.Length > 0) {
			sb.AppendLine(text.FormatWith(formatParams));
		} else {
			sb.AppendLine(text);
		}
	}

	static public void Clear(this StringBuilder stringBuilder) {
		stringBuilder.Remove(0, stringBuilder.Length);
	}

}


// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.CompilerServices;

namespace Sphere10.Framework;

[InterpolatedStringHandler]
internal ref struct AgainstGuardMessageHandler {
	private DefaultInterpolatedStringHandler _handler;

	public AgainstGuardMessageHandler(int literalLength, int formattedCount, bool condition, out bool shouldAppend) {
		shouldAppend = condition;
		_handler = shouldAppend ? new DefaultInterpolatedStringHandler(literalLength, formattedCount) : default;
	}

	public void AppendLiteral(string value) => _handler.AppendLiteral(value);

	public void AppendFormatted<T>(T value) => _handler.AppendFormatted(value);

	public void AppendFormatted<T>(T value, string format) => _handler.AppendFormatted(value, format);

	public void AppendFormatted<T>(T value, int alignment) => _handler.AppendFormatted(value, alignment);

	public void AppendFormatted<T>(T value, int alignment, string format) => _handler.AppendFormatted(value, alignment, format);

	public void AppendFormatted(string value) => _handler.AppendFormatted(value);

	public void AppendFormatted(string value, int alignment = 0, string format = null) => _handler.AppendFormatted(value, alignment, format);

	public void AppendFormatted(object value, int alignment = 0, string format = null) => _handler.AppendFormatted(value, alignment, format);

	public void AppendFormatted(scoped ReadOnlySpan<char> value) => _handler.AppendFormatted(value);

	public void AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string format = null) => _handler.AppendFormatted(value, alignment, format);

	internal string GetFormattedText() => _handler.ToStringAndClear();
}

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Text;
using Sphere10.Framework;

namespace Sphere10.Framework.Windows.Files;

/// <summary>Application text-edit policy and bounded text/hex inspection of arbitrary stream bytes.</summary>
public static class AlternateDataStreamContent {
	private static readonly UTF8Encoding _utf8 = new(false, true);

	public const int MaximumEditBytes = 16 * 1024 * 1024;
	public const int MaximumPreviewBytes = 64 * 1024;

	public static bool TryDecode(byte[] bytes, out string text, out bool hasBom) {
		Guard.ArgumentNotNull(bytes, nameof(bytes));
		return TryDecode(bytes.AsSpan(), true, out text, out hasBom);
	}

	public static byte[] Encode(string text, bool hasBom) {
		var content = _utf8.GetBytes(text ?? string.Empty);
		var bytes = hasBom ? new byte[] { 0xEF, 0xBB, 0xBF }.Concat(content).ToArray() : content;
		Guard.Ensure(bytes.Length <= MaximumEditBytes, "Text editing is limited to 16 MiB of encoded UTF-8 data.");
		Guard.Ensure(TryDecode(bytes, out _, out _), "Text cannot contain binary control characters.");
		return bytes;
	}

	public static string Preview(byte[] bytes, long? totalLength = null) {
		Guard.ArgumentNotNull(bytes, nameof(bytes));
		var limit = Math.Min(bytes.Length, MaximumPreviewBytes);
		var total = totalLength ?? bytes.LongLength;
		var summary = $"{total:N0} bytes. {(limit < total ? "Preview is limited to the first 64 KiB." : "Complete stream shown.")}\r\n\r\n";
		if (TryDecode(bytes.AsSpan(0, limit), limit >= total, out var text, out var bom))
			return summary + $"UTF-8{(bom ? " with BOM" : string.Empty)}\r\n\r\n" + text;
		var output = new StringBuilder(summary).AppendLine("Binary / non-UTF-8: hexadecimal bytes").AppendLine();
		for (var offset = 0; offset < limit; offset += 16) {
			output.Append(offset.ToString("X8")).Append("  ");
			for (var index = offset; index < Math.Min(limit, offset + 16); index++)
				output.Append(bytes[index].ToString("X2")).Append(' ');
			output.AppendLine();
		}
		return output.ToString();
	}

	private static bool TryDecode(ReadOnlySpan<byte> bytes, bool complete, out string text, out bool hasBom) {
		hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
		try {
			var characters = new char[bytes.Length];
			var count = _utf8.GetDecoder().GetChars(bytes[(hasBom ? 3 : 0)..], characters, complete);
			text = new string(characters, 0, count);
			if (!text.Any(character => char.IsControl(character) && character is not '\r' and not '\n' and not '\t'))
				return true;
		} catch (DecoderFallbackException) {
		}
		text = null;
		return false;
	}
}

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.VisualRenderer;

/// <summary>A consistent render-scoped view of templates, tokens, and output assets.</summary>
public sealed class ThemeSession {

	private static readonly Regex _placeholder = new(@"\{([^{}]+)\}", RegexOptions.CultureInvariant);

	private readonly RenderEnvironment _environment;

	private readonly RenderMode _mode;

	internal ThemeSession(IReadOnlyDictionary<string, object> tokens, IReadOnlyList<RenderAsset> assets,
				RenderEnvironment environment, RenderMode mode, bool suppressFormatting) {
		this.Tokens = tokens;
		this.Assets = assets;
		_environment = environment;
		_mode = mode;
		this.SuppressFormatting = suppressFormatting;
	}

	public IReadOnlyDictionary<string, object> Tokens { get; }

	public IReadOnlyList<RenderAsset> Assets { get; }

	public bool SuppressFormatting { get; }

	public string GetTemplate(string widget, string extension = ".html") {
		ArgumentException.ThrowIfNullOrWhiteSpace(widget);
		if (widget.IndexOfAny(['/', '\\', ':', '\0']) >= 0 || widget is "." or "..")
			throw new ThemeException($"Invalid template name '{widget}'.");
		extension = extension.TrimStart('.');
		var folder = _mode == RenderMode.Editable ? "editable" : "readonly";
		var environment = _environment == RenderEnvironment.Offline ? "offline" : "online";
		// Keep the legacy candidate order independent of the overlay/inheritance order.
		string[] candidates = [
			$"{folder}/{widget}.{environment}.{extension}",
			$"{folder}/{widget}.{extension}",
			$"{widget}.{extension}",
			$"{widget}.{environment}.{extension}"
		];
		foreach (var candidate in candidates) {
			if (Tokens.TryGetValue("include://" + candidate, out var value))
				return Regex.Replace(Convert.ToString(value, CultureInfo.InvariantCulture) ?? "", @"^<!--.*?-->", "", RegexOptions.Singleline);
		}
		throw new ThemeException($"Template '{widget}' was not found. Tried: {string.Join(", ", candidates)}.");
	}

	/// <summary>Expands theme includes and formatted values; local render values are inserted literally.</summary>
	public string Expand(string template, IReadOnlyDictionary<string, object> localTokens = null) {
		ArgumentNullException.ThrowIfNull(template);
		var stack = new List<string>();
		var literals = new List<string>();
		var result = ExpandValue(template, 0);
		for (var index = literals.Count - 1; index >= 0; index--)
			result = result.Replace("\uE000" + index.ToString(CultureInfo.InvariantCulture) + "\uE001", literals[index], StringComparison.Ordinal);
		return result.Replace("\uE002", "{", StringComparison.Ordinal).Replace("\uE003", "}", StringComparison.Ordinal);

		string Literal(string value) {
			literals.Add(value);
			return "\uE000" + (literals.Count - 1).ToString(CultureInfo.InvariantCulture) + "\uE001";
		}

		string ExpandValue(string value, int depth) {
			if (depth > 64)
				throw new ThemeException("Theme expansion exceeded 64 nested tokens.");
			value = value.Replace("{{", "\uE002", StringComparison.Ordinal).Replace("}}", "\uE003", StringComparison.Ordinal);
			for (var pass = 0; pass < 64; pass++) {
				var changed = false;
				var next = _placeholder.Replace(value, match => {
					var expression = match.Groups[1].Value;
					// An inner local token may form part of an include/resource name.
					// Resolve its protected literal only in the key, never as source markup.
					for (var literalIndex = literals.Count - 1; literalIndex >= 0; literalIndex--)
						expression = expression.Replace("\uE000" + literalIndex.ToString(CultureInfo.InvariantCulture) + "\uE001", literals[literalIndex], StringComparison.Ordinal);
					var key = expression.Trim();
					string format = null;
					if (!key.StartsWith("include://", StringComparison.Ordinal) && !key.StartsWith("theme://", StringComparison.Ordinal)) {
						var colon = key.IndexOf(':');
						if (colon >= 0) { format = key[(colon + 1)..].Trim(); key = key[..colon].Trim(); }
					}
					if (localTokens?.TryGetValue(key, out var local) == true) {
						changed = true;
						return Literal(FormatValue(local, format));
					}
					if (!Tokens.TryGetValue(key, out var token)) {
						if (key.StartsWith("include://", StringComparison.Ordinal) || key.StartsWith("theme://", StringComparison.Ordinal))
							throw new ThemeException($"Theme token '{key}' was not found.");
						return match.Value;
					}
					if (stack.Contains(key, StringComparer.Ordinal))
						throw new ThemeException("Cyclic theme token/include: " + string.Join(" -> ", stack.Append(key)));
					changed = true;
					stack.Add(key);
					try {
						var text = FormatValue(token, format);
						return ExpandValue(text, depth + 1);
					} finally { stack.RemoveAt(stack.Count - 1); }
				});
				value = next;
				if (!changed)
					return value;
			}
			throw new ThemeException("Theme expansion exceeded 64 passes.");
		}

		static string FormatValue(object value, string format) => value switch {
			null => "",
			IFormattable formattable => formattable.ToString(format, CultureInfo.InvariantCulture) ?? "",
			_ => value.ToString() ?? ""
		};
	}
}

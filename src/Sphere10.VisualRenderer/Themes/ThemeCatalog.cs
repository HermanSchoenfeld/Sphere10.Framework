// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;

namespace Sphere10.VisualRenderer;

/// <summary>Reads optional theme files over embedded defaults, without creating any files.</summary>
public sealed class ThemeCatalog {

	private static readonly Lazy<IReadOnlyDictionary<string, byte[]>> _embedded = new(ReadEmbedded);

	private readonly string _directory;

	private readonly IReadOnlyDictionary<string, byte[]> _overrideSnapshot;

	private readonly ConcurrentDictionary<SessionKey, Lazy<ThemeSession>> _sessions = new();

	private readonly Lazy<ThemeCatalog> _embeddedSnapshot = new(() => new ThemeCatalog(new ReadOnlyDictionary<string, byte[]>(new Dictionary<string, byte[]>())));

	public ThemeCatalog(ThemeOptions options = null) {
		_directory = string.IsNullOrWhiteSpace(options?.ThemesDirectory) ? null : Path.GetFullPath(options.ThemesDirectory);
	}

	private ThemeCatalog(IReadOnlyDictionary<string, byte[]> overrides) {
		_overrideSnapshot = overrides;
	}

	/// <summary>Freezes override files so renders and themed fragments can share a consistent view.</summary>
	public ThemeCatalog CreateSnapshot() {
		if (_overrideSnapshot is not null)
			return this;
		if (_directory is null || !Directory.Exists(_directory))
			return _embeddedSnapshot.Value;
		var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
		try {
			if (_directory is not null && Directory.Exists(_directory)) {
				foreach (var fileValue in Directory.EnumerateFiles(_directory, "*", SearchOption.AllDirectories))
					files.Add(Path.GetRelativePath(_directory, fileValue).Replace('\\', '/'), File.ReadAllBytes(fileValue));
			}
		} catch (Exception error) when (error is IOException or UnauthorizedAccessException) {
			throw new ThemeException($"Unable to snapshot theme overrides: {error.Message}", error);
		}
		return files.Count == 0 ? _embeddedSnapshot.Value : new ThemeCatalog(new ReadOnlyDictionary<string, byte[]>(files));
	}

	public ThemeSession CreateSession(IReadOnlyList<string> themes, RenderEnvironment environment, RenderMode mode, string assetBaseUrl = null) {
		if (!Enum.IsDefined(environment))
			throw new ArgumentOutOfRangeException(nameof(environment));
		if (!Enum.IsDefined(mode))
			throw new ArgumentOutOfRangeException(nameof(mode));
		var selected = themes is { Count: > 0 } ? themes.Distinct(StringComparer.Ordinal).ToArray() : ["default"];
		foreach (var name in selected)
			ValidateThemeName(name);
		if (_overrideSnapshot is null)
			return _directory is null
				? _embeddedSnapshot.Value.CreateSession(selected, environment, mode, assetBaseUrl)
				: BuildSession(selected, environment, mode, assetBaseUrl);
		var key = new SessionKey(string.Join("\0", selected), environment, mode, assetBaseUrl ?? string.Empty);
		return _sessions.GetOrAdd(key, _ => new Lazy<ThemeSession>(() => BuildSession(selected, environment, mode, assetBaseUrl))).Value;
	}

	private ThemeSession BuildSession(IReadOnlyList<string> selected, RenderEnvironment environment, RenderMode mode, string assetBaseUrl) {
		var loaded = new Dictionary<string, Definition>(StringComparer.Ordinal);
		var visiting = new List<string>();
		var definitions = selected.Distinct(StringComparer.Ordinal).Select(Load).ToArray();
		var tokens = new Dictionary<string, object>(StringComparer.Ordinal);
		var effectiveFiles = new Dictionary<string, ThemeFile>(StringComparer.Ordinal);
		var owners = new Dictionary<string, Definition>(StringComparer.Ordinal);
		// Legacy theme layering inherits the first theme, then overlays only each later theme's own definitions.
		for (var index = 0; index < definitions.Length; index++)
			Apply(definitions[index], index == 0);

		// Bundle the full effective asset trees, including scripts' dynamically loaded modules/fonts.
		// The hash covers both paths and bytes, so independent documents cannot overwrite each other.
		var assets = effectiveFiles.Where(item => IsAsset(item.Key)).OrderBy(item => item.Key, StringComparer.Ordinal).ToArray();
		using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
		var sizeBytes = new byte[8];
		foreach (var asset in assets) {
			hasher.AppendData(Encoding.UTF8.GetBytes(asset.Key));
			hasher.AppendData([0]);
			BinaryPrimitives.WriteInt64LittleEndian(sizeBytes, asset.Value.Bytes.LongLength);
			hasher.AppendData(sizeBytes);
			hasher.AppendData(asset.Value.Bytes);
		}
		var bundle = "assets/" + Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant();
		var resultAssets = new List<RenderAsset>();
		var localAssetNeeded = environment == RenderEnvironment.Offline || assets.Any(item => string.IsNullOrWhiteSpace(owners[item.Key].OnlineUrl));
		if (localAssetNeeded) {
			foreach (var asset in assets)
				resultAssets.Add(new RenderAsset(bundle + "/" + asset.Key, ContentType(asset.Key), asset.Value.Bytes.ToArray()));
		}
		foreach (var (pathValue, fileValue) in effectiveFiles) {
			var assetPath = bundle + "/" + pathValue;
			var owner = owners[pathValue];
			// Online URLs are configured by the owning theme, independently of where its files were loaded.
			tokens["theme://" + pathValue] = environment == RenderEnvironment.Online && !string.IsNullOrWhiteSpace(owner.OnlineUrl)
				? owner.OnlineUrl.TrimEnd('/') + "/" + EscapePath(pathValue)
				: JoinUrl(assetBaseUrl, assetPath);
			if (IsText(pathValue))
				tokens["include://" + pathValue] = Encoding.UTF8.GetString(fileValue.Bytes).TrimStart('\uFEFF');
		}
		tokens["render_mode"] = mode == RenderMode.Editable ? "editable" : "readonly";
		return new ThemeSession(new ReadOnlyDictionary<string, object>(tokens), resultAssets.AsReadOnly(), environment, mode, definitions.Any(item => item.SuppressFormatting));

		void Apply(Definition definitionValue, bool includeBase) {
			if (includeBase && definitionValue.Base is not null)
				Apply(definitionValue.Base, true);
			foreach (var tokenValue in definitionValue.Tokens)
				tokens[tokenValue.Key] = environment == RenderEnvironment.Offline ? tokenValue.Value.Offline : tokenValue.Value.Online;
			foreach (var fileValue in definitionValue.Files) {
				effectiveFiles[fileValue.Key] = fileValue.Value;
				owners[fileValue.Key] = definitionValue;
			}
		}

		Definition Load(string name) {
			ValidateThemeName(name);
			if (loaded.TryGetValue(name, out var existing))
				return existing;
			if (visiting.Contains(name, StringComparer.Ordinal))
				throw new ThemeException("Cyclic theme inheritance: " + string.Join(" -> ", visiting.Append(name)));
			visiting.Add(name);
			try {
				var files = Snapshot(name);
				if (!files.TryGetValue(".config.json", out var config))
					throw new ThemeException($"Theme '{name}' has no .config.json in the override directory or renderer assembly.");
				using var json = JsonDocument.Parse(Encoding.UTF8.GetString(config.Bytes).TrimStart('\uFEFF'), new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
				var root = json.RootElement;
				if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("type", out var type) || type.GetString() != "html")
					throw new ThemeException($"Theme '{name}' must have a .config.json with type 'html'.");
				var baseName = root.TryGetProperty("base", out var baseNode) ? baseNode.GetString() : null;
				var baseTheme = string.IsNullOrWhiteSpace(baseName) ? null : Load(baseName);
				var online = root.TryGetProperty("online_url", out var onlineNode) ? onlineNode.GetString() : null;
				var traits = root.TryGetProperty("traits", out var traitNode) ? traitNode.ToString() : "";
				var themeTokens = new Dictionary<string, Token>(StringComparer.Ordinal);
				if (root.TryGetProperty("tokens", out var tokenNode)) {
					if (tokenNode.ValueKind != JsonValueKind.Object)
						throw new ThemeException($"Theme '{name}' tokens must be an object.");
					foreach (var tokenValue in tokenNode.EnumerateObject()) {
						if (tokenValue.Name.StartsWith("include://", StringComparison.Ordinal) || tokenValue.Name.StartsWith("theme://", StringComparison.Ordinal))
							throw new ThemeException($"Theme '{name}' token '{tokenValue.Name}' uses a reserved file-token prefix.");
						if (tokenValue.Value.ValueKind != JsonValueKind.Object)
							throw new ThemeException($"Theme '{name}' token '{tokenValue.Name}' must specify offline/online values.");
						themeTokens.Add(tokenValue.Name, new Token(Value(tokenValue.Value, "offline"), Value(tokenValue.Value, "online")));
					}
				}
				var definitionValue = new Definition(name, baseTheme, files, themeTokens,
					string.IsNullOrEmpty(online) ? baseTheme?.OnlineUrl : online,
					traits.Contains("suppress_formatting", StringComparison.OrdinalIgnoreCase) || baseTheme?.SuppressFormatting == true);
				loaded.Add(name, definitionValue);
				return definitionValue;
			} catch (ThemeException) { throw; } catch (Exception error) when (error is JsonException or InvalidOperationException or IOException or UnauthorizedAccessException or ArgumentException) {
				throw new ThemeException($"Unable to read theme '{name}': {error.Message}", error);
			} finally { visiting.RemoveAt(visiting.Count - 1); }
		}
	}

	private Dictionary<string, ThemeFile> Snapshot(string name) {
		var prefix = name + "/";
		var result = _embedded.Value.Where(item => item.Key.StartsWith(prefix, StringComparison.Ordinal))
			.ToDictionary(item => item.Key[prefix.Length..], item => new ThemeFile(item.Value), StringComparer.Ordinal);
		if (_overrideSnapshot is not null) {
			foreach (var fileValue in _overrideSnapshot.Where(item => item.Key.StartsWith(prefix, StringComparison.Ordinal)))
				result[fileValue.Key[prefix.Length..]] = new ThemeFile(fileValue.Value);
			return result;
		}
		if (_directory is null)
			return result;
		var folder = Path.Combine(_directory, name);
		if (!Directory.Exists(folder))
			return result;
		foreach (var fileValue in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)) {
			var relative = Path.GetRelativePath(folder, fileValue).Replace('\\', '/');
			// Read all selected files now. A session never follows a future mutable physical file.
			result[relative] = new ThemeFile(File.ReadAllBytes(fileValue));
		}
		return result;
	}

	private static IReadOnlyDictionary<string, byte[]> ReadEmbedded() {
		var provider = new ManifestEmbeddedFileProvider(typeof(ThemeCatalog).Assembly, "Themes");
		var result = new Dictionary<string, byte[]>(StringComparer.Ordinal);
		Read("");
		return new ReadOnlyDictionary<string, byte[]>(result);
		void Read(string directoryValue) {
			foreach (var entry in provider.GetDirectoryContents(string.IsNullOrEmpty(directoryValue) ? "/" : directoryValue)) {
				var pathValue = string.IsNullOrEmpty(directoryValue) ? entry.Name : directoryValue + "/" + entry.Name;
				if (entry.IsDirectory)
					Read(pathValue);
				else {
					using var stream = entry.CreateReadStream();
					using var buffer = new MemoryStream();
					stream.CopyTo(buffer);
					result[pathValue] = buffer.ToArray();
				}
			}
		}
	}

	private static object Value(JsonElement tokenValue, string property) {
		if (!tokenValue.TryGetProperty(property, out var value))
			return "";
		return value.ValueKind switch {
			JsonValueKind.String => value.GetString() ?? "",
			JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
			JsonValueKind.Number => value.GetDouble(),
			JsonValueKind.True => true,
			JsonValueKind.False => false,
			JsonValueKind.Null => "",
			_ => value.GetRawText()
		};
	}

	private static void ValidateThemeName(string name) {
		if (string.IsNullOrWhiteSpace(name) || name is "." or ".." || name.IndexOfAny(['/', '\\', ':', '\0']) >= 0)
			throw new ThemeException($"Invalid theme name '{name}'. A theme name must be a single directory name.");
	}

	private static bool IsAsset(string pathValue) => pathValue.StartsWith("resources/", StringComparison.Ordinal) ||
				(!pathValue.EndsWith(".html", StringComparison.OrdinalIgnoreCase) && !pathValue.EndsWith(".inc", StringComparison.OrdinalIgnoreCase) && pathValue != ".config.json");

	private static bool IsText(string pathValue) => Path.GetExtension(pathValue).ToLowerInvariant() is ".html" or ".inc" or ".css" or ".js" or ".json" or ".txt" or ".md" or ".svg";

	private static string JoinUrl(string root, string relative) => string.IsNullOrEmpty(root) ? EscapePath(relative) : root.TrimEnd('/') + "/" + EscapePath(relative);

	private static string EscapePath(string pathValue) => string.Join("/", pathValue.Split('/').Select(Uri.EscapeDataString));

	private static string ContentType(string pathValue) => Path.GetExtension(pathValue).ToLowerInvariant() switch {
		".css" => "text/css",
		".js" => "text/javascript",
		".html" => "text/html",
		".json" or ".map" => "application/json",
		".svg" => "image/svg+xml",
		".png" => "image/png",
		".jpg" or ".jpeg" => "image/jpeg",
		".gif" => "image/gif",
		".woff" => "font/woff",
		".woff2" => "font/woff2",
		".ttf" => "font/ttf",
		".ico" => "image/x-icon",
		".txt" or ".md" => "text/plain",
		_ => "application/octet-stream"
	};

	private sealed record SessionKey(string Themes, RenderEnvironment Environment, RenderMode Mode, string AssetBaseUrl);

	private sealed record ThemeFile(byte[] Bytes);

	private sealed record Token(object Offline, object Online);

	private sealed record Definition(string Name, Definition Base, Dictionary<string, ThemeFile> Files, Dictionary<string, Token> Tokens, string OnlineUrl, bool SuppressFormatting);
}

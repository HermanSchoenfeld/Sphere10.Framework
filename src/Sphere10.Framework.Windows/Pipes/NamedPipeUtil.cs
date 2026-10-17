// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>Lists the local named-pipe namespace without opening pipe instances or probing their endpoints.</summary>
public class NamedPipeUtil {
	public const string LocalNamespacePath = @"\\.\pipe\";

	public string[] GetNames() => GetNamesCore(null);

	public Task<string[]> GetNamesAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => GetNamesCore(cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	public string GetPath(string name) {
		Guard.ArgumentNotNullOrWhitespace(name, nameof(name));
		Guard.Argument(name.Length + LocalNamespacePath.Length <= 256 && name[0] != '\\' && name[0] != '/' && !name.Contains(':') && !name.Contains('/') && !name.Contains('\0'),
			nameof(name), "Specify a relative local named-pipe name, without an absolute path, drive prefix or NUL.");
		foreach (var segment in name.Split('\\'))
			Guard.Argument(segment.Length > 0 && segment != "." && segment != "..", nameof(name), "Pipe namespace segments cannot be empty or traverse another namespace.");
		return LocalNamespacePath + name;
	}

	private static string[] GetNamesCore(Action checkpoint) {
		checkpoint?.Invoke();
		var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var path in Directory.EnumerateFileSystemEntries(LocalNamespacePath)) {
			checkpoint?.Invoke();
			Guard.Ensure(path.StartsWith(LocalNamespacePath, StringComparison.OrdinalIgnoreCase), "Windows returned a different pipe namespace.");
			names.Add(path[LocalNamespacePath.Length..]);
		}
		checkpoint?.Invoke();
		var result = new string[names.Count];
		names.CopyTo(result);
		Array.Sort(result, StringComparer.OrdinalIgnoreCase);
		return result;
	}
}


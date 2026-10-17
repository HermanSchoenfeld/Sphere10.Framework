// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Text;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>The selected output and its independent filtering/formatting options. No output is opened by constructing this data object.</summary>
public sealed class LogOutputConfiguration {
	public LogOutputDestination Destination { get; set; } = LogOutputDestination.File;
	public string FilePath { get; set; } = string.Empty;
	public bool RollingEnabled { get; set; }
	public int MaxFiles { get; set; } = RollingFileLogger.DefaultMaxFiles;
	public int MaxFileSizeBytes { get; set; } = RollingFileLogger.DefaultMaxFileSizeB;
	public string EncodingName { get; set; } = "utf-8";
	public LogOptions Options { get; set; } = LogOptions.VerboseProfile;
	public bool TimestampEnabled { get; set; } = true;
	public bool RememberAfterClose { get; set; }

	public LogOutputConfiguration Clone() => (LogOutputConfiguration)MemberwiseClone();

	public string Describe() {
		var output = Destination switch {
			LogOutputDestination.File => $"File: {FilePath}{Environment.NewLine}" + (RollingEnabled
				? $"Rolling: {MaxFiles:N0} files including the active file; {MaxFileSizeBytes:N0} bytes per file"
				: "Append to the existing file"),
			LogOutputDestination.Application => "Application logger",
			LogOutputDestination.StandardOutput => "Standard output",
			LogOutputDestination.StandardError => "Standard error",
			_ => "Unknown output"
		};
		return $"{output}{Environment.NewLine}Levels: {Options}{Environment.NewLine}Timestamp: {(TimestampEnabled ? "On" : "Off")}"
			+ (Destination == LogOutputDestination.File ? $"{Environment.NewLine}Encoding: {EncodingName}" : string.Empty)
			+ $"{Environment.NewLine}Remember After Close: {(RememberAfterClose ? "Yes" : "No")}";
	}

	internal LogOutputConfiguration ValidateAndNormalize() {
		var normalized = ValidateOptionsAndNormalize();
		if (Destination != LogOutputDestination.File)
			return normalized;
		var directory = Path.GetDirectoryName(normalized.FilePath);
		if (!Directory.Exists(directory))
			throw new DirectoryNotFoundException($"The log directory does not exist: {directory}");
		Guard.Argument(!Directory.Exists(normalized.FilePath), nameof(FilePath), "Select a log file, not a directory.");
		return normalized;
	}

	internal LogOutputConfiguration ValidateOptionsAndNormalize() {
		Guard.Argument(Enum.IsDefined(Destination), nameof(Destination), "Select a supported logging output.");
		Guard.Argument((Options & ~LogOptions.VerboseProfile) == 0, nameof(Options), "Unknown logging options.");
		var normalized = Clone();
		if (Destination != LogOutputDestination.File)
			return normalized;
		Guard.ArgumentNotNullOrEmpty(FilePath, nameof(FilePath));
		normalized.FilePath = Path.GetFullPath(FilePath);
		if (RollingEnabled) {
			Guard.ArgumentInRange(MaxFiles, 1, 9999, nameof(MaxFiles));
			Guard.ArgumentInRange(MaxFileSizeBytes, 32, int.MaxValue, nameof(MaxFileSizeBytes));
		}
		_ = normalized.GetEncoding();
		return normalized;
	}

	internal Encoding GetEncoding() => EncodingName?.ToLowerInvariant() switch {
		"utf-8" => new UTF8Encoding(false),
		"utf-16" => Encoding.Unicode,
		"ascii" => Encoding.ASCII,
		_ => throw new ArgumentException("Select UTF-8, UTF-16 or ASCII encoding.", nameof(EncodingName))
	};
}

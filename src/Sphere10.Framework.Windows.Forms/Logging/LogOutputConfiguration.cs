// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>The selected output and its independent filtering/formatting options. No output is opened by constructing this data object.</summary>
public sealed class LogOutputConfiguration {
	/// <summary>The selected output destination.</summary>
	[DefaultValue(LogOutputDestination.File)]
	public LogOutputDestination Destination { get; set; } = LogOutputDestination.File;

	/// <summary>The selected file path for file output.</summary>
	[DefaultValue("")]
	public string FilePath { get; set; } = string.Empty;

	/// <summary>Whether file output rotates through bounded files.</summary>
	[DefaultValue(false)]
	public bool RollingEnabled { get; set; }

	/// <summary>The maximum number of rolling files.</summary>
	[DefaultValue(RollingFileLogger.DefaultMaxFiles)]
	public int MaxFiles { get; set; } = RollingFileLogger.DefaultMaxFiles;

	/// <summary>The maximum byte length of one rolling file.</summary>
	[DefaultValue(RollingFileLogger.DefaultMaxFileSizeB)]
	public int MaxFileSizeBytes { get; set; } = RollingFileLogger.DefaultMaxFileSizeB;

	/// <summary>The encoding used for file output.</summary>
	[DefaultValue("utf-8")]
	public string EncodingName { get; set; } = "utf-8";

	/// <summary>The message levels written to the selected output.</summary>
	[DefaultValue(LogOptions.VerboseProfile)]
	public LogOptions Options { get; set; } = LogOptions.VerboseProfile;

	/// <summary>Whether each written message includes its timestamp.</summary>
	[DefaultValue(true)]
	public bool TimestampEnabled { get; set; } = true;

	/// <summary>Whether this output is restored after the owning screen closes.</summary>
	[DefaultValue(false)]
	public bool RememberAfterClose { get; set; }

	/// <summary>Copies plain output options without creating an output resource.</summary>
	public LogOutputConfiguration Clone() {
		return (LogOutputConfiguration)MemberwiseClone();
	}

	/// <summary>Formats the configured output and filtering choices for display.</summary>
	public string Describe() {
		var output = Destination switch {
			LogOutputDestination.File => $"File: {FilePath}{Environment.NewLine}" + (RollingEnabled
				? $"Rolling: {MaxFiles:N0} files including the active file; {MaxFileSizeBytes:N0} bytes per file"
				: "Append to the existing file"),
			LogOutputDestination.Application => "Windows Application Log",
			LogOutputDestination.StandardOutput => "Standard output",
			LogOutputDestination.StandardError => "Standard error",
			LogOutputDestination.Screen => "Log output screen",
			_ => "Unknown output"
		};
		return $"{output}{Environment.NewLine}Levels: {Options}{Environment.NewLine}Timestamp: {(TimestampEnabled ? "On" : "Off")}"
			+ (Destination == LogOutputDestination.File ? $"{Environment.NewLine}Encoding: {EncodingName}" : string.Empty)
			+ $"{Environment.NewLine}Remember After Close: {(RememberAfterClose ? "Yes" : "No")}";
	}

	// Validate value choices and the file destination before an output can be opened.
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

	// Validate and normalize plain configuration values without opening the output.
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

	// Resolve the supported encoding for a configured file output.
	internal Encoding GetEncoding() {
		return EncodingName?.ToLowerInvariant() switch {
			"utf-8" => new UTF8Encoding(false),
			"utf-16" => Encoding.Unicode,
			"ascii" => Encoding.ASCII,
			_ => throw new ArgumentException("Select UTF-8, UTF-16 or ASCII encoding.", nameof(EncodingName))
		};
	}
}

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Sphere10.Framework;

/// <summary>
/// Logs to a file and archives it at the byte-size threshold. The file limit includes the active log.
/// Archive filenames include a sortable UTC timestamp, including fractional seconds to support rapid rolling.
/// </summary>
/// <example>
/// web.log
/// web_20210914120111.log
/// web_20211014110100.log
/// </example>
public class RollingFileLogger : LoggerBase, IDisposable {
	public const int DefaultMaxFiles = 10;
	public const int DefaultMaxFileSizeB = 1000000;
	private static readonly string[] _archiveTimestampFormats = { "yyyyMMddHHmmss", "yyyyMMddHHmmssfffffff" };
	private readonly string _directory;
	private readonly string _fileName;
	private FileAppendTextWriter _textWriter;
	private long _currentFileSizeBytes;
	private readonly int _effectiveMaxSize;
	private readonly int _newLineSizeBytes;
	private readonly int _preambleSizeBytes;
	private long _lastArchiveTimestampTicks;


	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="logFilePath">Path of log file.</param>
	/// <param name="maxFiles">Maximum number of files to retain, including the active log and its timestamped archives. Older archives are deleted.</param>
	/// <param name="maxFileSize">The maximum byte-size of a log-file before further logging is rolled-over into a new log-file.</param>
	/// <param name="encoding">Text-encoding for file. Default is ASCII.</param>
	public RollingFileLogger(string logFilePath, int maxFiles = DefaultMaxFiles, int maxFileSize = DefaultMaxFileSizeB, Encoding encoding = default) {
		encoding ??= Encoding.ASCII;
		Guard.ArgumentInRange(maxFiles, 1, 9999, nameof(maxFiles));
		Guard.ArgumentInRange(maxFileSize, 32, int.MaxValue, nameof(maxFileSize));
		Guard.ArgumentNotNullOrEmpty(logFilePath, nameof(logFilePath));
		_directory = Tools.FileSystem.GetParentDirectoryPath(logFilePath);
		_fileName = Path.GetFileName(logFilePath);
		LogFilePath = logFilePath;
		Encoding = encoding;
		MaxFiles = maxFiles;
		MaxFileSize = maxFileSize;
		_newLineSizeBytes = Encoding.GetByteCount(Environment.NewLine);
		_preambleSizeBytes = Encoding.GetPreamble().Length;
		_effectiveMaxSize = MaxFileSize - _newLineSizeBytes - _preambleSizeBytes;
		Guard.Argument(_effectiveMaxSize >= 0, nameof(encoding), "The encoding's preamble and newline must fit within the maximum file size.");
		Options = LogOptions.VerboseProfile;
		OpenLogFile(logFilePath, encoding);
	}


	/// <summary>
	/// It is necessary to have an encoding specified so that the number of bytes a message will require can be calculated to maintain
	/// a max file size.
	/// </summary>
	public Encoding Encoding { get; }

	/// <summary>
	/// Template for log file
	/// </summary>
	public string LogFilePath { get; }

	/// <summary>
	/// Maximum number of files to retain, including the active log and its timestamped archives. Unrelated files are preserved.
	/// </summary>
	/// <remarks>
	/// Maximum number is 9999.
	/// </remarks>
	public int MaxFiles { get; }

	/// <summary>
	/// The maximum byte-size of a log file before further logging is rolled-over into a new log file.
	/// </summary>
	public int MaxFileSize { get; }

	/// <inheritdoc />
	protected override void Log(LogLevel logLevel, string message) {
		Guard.ArgumentNotNull(message, nameof(message));

		var messageSize = Encoding.GetByteCount(message);
		if (messageSize > _effectiveMaxSize) {
			// Clip to a whole-character prefix using the actual encoding's byte count.
			message = ClipMessage(message);
			messageSize = Encoding.GetByteCount(message);
		}

		var lineSize = messageSize + _newLineSizeBytes;
		if (_currentFileSizeBytes + lineSize + (_currentFileSizeBytes == 0 ? _preambleSizeBytes : 0) > MaxFileSize)
			ArchiveLogFile();

		_textWriter.WriteLine(message);
		_textWriter.Flush();
		_currentFileSizeBytes += lineSize + (_currentFileSizeBytes == 0 ? _preambleSizeBytes : 0);
	}

	/// <summary>
	/// Flushes and releases the underlying file writer.
	/// </summary>
	public void Dispose() {
		_textWriter.Close();
	}

	private void OpenLogFile(string logFilePath, Encoding encoding) {
		_textWriter = new FileAppendTextWriter(logFilePath, encoding, true);
		_currentFileSizeBytes = File.Exists(logFilePath) ? Tools.FileSystem.GetFileSize(logFilePath) : 0;
		if (_currentFileSizeBytes > MaxFileSize)
			ArchiveLogFile();
	}

	private string ClipMessage(string message) {
		// Search without allocating an encoding-sized buffer or cutting a surrogate pair.
		var lower = 0;
		var upper = message.Length;
		while (lower < upper) {
			var candidate = lower + (upper - lower + 1) / 2;
			var length = WholeCharacterPrefixLength(message, candidate);
			if (Encoding.GetByteCount(message.AsSpan(0, length)) <= _effectiveMaxSize)
				lower = candidate;
			else
				upper = candidate - 1;
		}
		return message.Substring(0, WholeCharacterPrefixLength(message, lower));
	}

	private static int WholeCharacterPrefixLength(string message, int length)
		=> length > 0 && length < message.Length && char.IsHighSurrogate(message[length - 1]) && char.IsLowSurrogate(message[length]) ? length - 1 : length;

	private void ArchiveLogFile() {
		// Keep names unique and sortable even when several files roll in one clock tick.
		var fileExt = Path.GetExtension(_fileName);
		_lastArchiveTimestampTicks = Math.Max(DateTime.UtcNow.Ticks, _lastArchiveTimestampTicks + 1);
		string archiveFileName;
		do {
			var timestamp = new DateTime(_lastArchiveTimestampTicks++, DateTimeKind.Utc).ToString("yyyyMMddHHmmssfffffff", CultureInfo.InvariantCulture);
			archiveFileName = $"{Path.GetFileNameWithoutExtension(_fileName)}_{timestamp}{fileExt}";
		} while (File.Exists(Path.Combine(_directory, archiveFileName)));
		if (File.Exists(LogFilePath))
			Tools.FileSystem.RenameFile(LogFilePath, archiveFileName);

		// Recreate log as empty file
		Tools.FileSystem.CreateBlankFile(LogFilePath);
		_currentFileSizeBytes = 0;

		// Delete only recognized archives; similarly named user files are not owned by this logger.
		var numArchives = MaxFiles - 1; // take 1 since log file itself included in MaxFiles
		var archivesToDelete = Directory
			.GetFiles(
				_directory,
				$"{Path.GetFileNameWithoutExtension(_fileName)}_*{fileExt}",
				SearchOption.TopDirectoryOnly
			)
			.Where(IsArchiveFile)
			.OrderByDescending(x => x, StringComparer.Ordinal)
			.Skip(numArchives);
		archivesToDelete.ForEach(File.Delete);
	}

	private bool IsArchiveFile(string path) {
		var prefix = Path.GetFileNameWithoutExtension(_fileName) + "_";
		var name = Path.GetFileNameWithoutExtension(path);
		if (!name.StartsWith(prefix, StringComparison.Ordinal) || !string.Equals(Path.GetExtension(path), Path.GetExtension(_fileName), StringComparison.Ordinal))
			return false;
		return DateTime.TryParseExact(name.AsSpan(prefix.Length), _archiveTimestampFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
	}

}


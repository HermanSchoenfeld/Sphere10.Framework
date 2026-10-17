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
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests.Logging;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class RollingFileLoggerTests {
	[Test]
	public void RapidRolling_RetainsNewestFilesWithoutArchiveCollisions() {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var logPath = Path.Combine(directory, "events.log");
		var messages = Enumerable.Range(0, 100).Select(index => index.ToString("D3").PadRight(32 - Environment.NewLine.Length, 'x')).ToArray();
		using (var logger = new RollingFileLogger(logPath, maxFiles: 4, maxFileSize: 32))
			foreach (var message in messages)
				logger.Info(message);

		var archives = Directory.GetFiles(directory, "events_*.log").OrderBy(path => path, StringComparer.Ordinal).ToArray();
		Assert.That(archives, Has.Length.EqualTo(3));
		var retainedMessages = archives.Concat(new[] { logPath }).SelectMany(File.ReadAllLines).ToArray();
		Assert.That(retainedMessages, Is.EqualTo(messages.TakeLast(4)));
		Assert.That(Directory.GetFiles(directory).Select(path => new FileInfo(path).Length), Is.All.LessThanOrEqualTo(32));
	}

	[Test]
	public void Retention_RecognizesLegacyAndCurrentArchivesWhilePreservingUnrelatedFiles() {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var logPath = Path.Combine(directory, "events.log");
		var legacyArchive = Path.Combine(directory, "events_20000101000000.log");
		var currentArchive = Path.Combine(directory, "events_200101010000000000000.log");
		File.WriteAllText(legacyArchive, "legacy archive", Encoding.ASCII);
		File.WriteAllText(currentArchive, "fractional archive", Encoding.ASCII);
		var unrelatedNames = new[] {
			"events_notes.log", "events_20230230000100.log", "events_20240101000100_backup.log",
			"events_202401010001000000000_more.log", "events_202401010001001.log", "events_other_20240101000100.log"
		};
		var unrelatedPaths = unrelatedNames.Select(name => Path.Combine(directory, name)).ToArray();
		foreach (var path in unrelatedPaths)
			File.WriteAllText(path, "preserve " + Path.GetFileName(path), Encoding.ASCII);

		// Archive identity and ordering must not depend on the user's calendar or culture.
		var previousCulture = CultureInfo.CurrentCulture;
		using var restoreCulture = Tools.Scope.ExecuteOnDispose(() => CultureInfo.CurrentCulture = previousCulture);
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
		var messages = Enumerable.Range(0, 10).Select(index => index.ToString("D3", CultureInfo.InvariantCulture).PadRight(32 - Environment.NewLine.Length, 'x')).ToArray();
		using (var logger = new RollingFileLogger(logPath, maxFiles: 4, maxFileSize: 32))
			foreach (var message in messages)
				logger.Info(message);

		Assert.That(File.Exists(legacyArchive), Is.False);
		Assert.That(File.Exists(currentArchive), Is.False);
		foreach (var path in unrelatedPaths)
			Assert.That(File.ReadAllText(path), Is.EqualTo("preserve " + Path.GetFileName(path)));
		var ownedFiles = Directory.GetFiles(directory).Except(unrelatedPaths, StringComparer.Ordinal).ToArray();
		Assert.That(ownedFiles, Has.Length.EqualTo(4));
		var retainedMessages = ownedFiles.Where(path => path != logPath).OrderBy(path => path, StringComparer.Ordinal).Concat(new[] { logPath }).SelectMany(File.ReadAllLines);
		Assert.That(retainedMessages, Is.EqualTo(messages.TakeLast(4)));
	}

	[Test]
	public void Rolling_CountsEveryEncodedNewline() {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var logPath = Path.Combine(directory, "events.log");
		var messages = Enumerable.Range(0, 30).Select(index => index.ToString("D2").PadRight(5, 'x')).ToArray();
		using (var logger = new RollingFileLogger(logPath, maxFiles: 20, maxFileSize: 32))
			foreach (var message in messages)
				logger.Info(message);

		var files = Directory.GetFiles(directory, "events_*.log").OrderBy(path => path, StringComparer.Ordinal).Concat(new[] { logPath }).ToArray();
		Assert.That(files.Select(path => new FileInfo(path).Length), Is.All.LessThanOrEqualTo(32));
		Assert.That(files.SelectMany(File.ReadAllLines), Is.EqualTo(messages));
		Assert.That(files, Has.Length.EqualTo((int)Math.Ceiling(messages.Length / (double)(32 / (5 + Environment.NewLine.Length)))));
	}

	[TestCase("界", 3)]
	[TestCase("😀", 4)]
	[TestCase("é", 2)]
	public void OversizedUtf8Message_ClipsAtWholeCharacterByteBoundary(string character, int encodedCharacterSize) {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var logPath = Path.Combine(directory, "events.log");
		var encoding = new UTF8Encoding(false, true);
		var message = string.Concat(Enumerable.Repeat(character, 40));
		using (var logger = new RollingFileLogger(logPath, maxFileSize: 32, encoding: encoding))
			logger.Info(message);

		var expected = string.Concat(Enumerable.Repeat(character, (32 - encoding.GetByteCount(Environment.NewLine)) / encodedCharacterSize));
		Assert.That(File.ReadAllText(logPath, encoding), Is.EqualTo(expected + Environment.NewLine));
		Assert.That(new FileInfo(logPath).Length, Is.LessThanOrEqualTo(32));
	}

	[Test]
	public void Utf8Preamble_IsIncludedInEachNewFileSize() {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var logPath = Path.Combine(directory, "events.log");
		var encoding = new UTF8Encoding(true, true);
		var characterLimit = 32 - encoding.GetPreamble().Length - encoding.GetByteCount(Environment.NewLine);
		using (var logger = new RollingFileLogger(logPath, maxFiles: 3, maxFileSize: 32, encoding: encoding)) {
			logger.Info(new string('a', 40));
			logger.Info(new string('b', 40));
		}

		var archive = Directory.GetFiles(directory, "events_*.log").Single();
		Assert.That(new FileInfo(archive).Length, Is.EqualTo(32));
		Assert.That(new FileInfo(logPath).Length, Is.EqualTo(32));
		Assert.That(File.ReadAllText(archive, encoding), Is.EqualTo(new string('a', characterLimit) + Environment.NewLine));
		Assert.That(File.ReadAllText(logPath, encoding), Is.EqualTo(new string('b', characterLimit) + Environment.NewLine));
	}

	[Test]
	public void SingleFileRetention_DiscardsArchivesAndKeepsCurrentEntry() {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var logPath = Path.Combine(directory, "events.log");
		using (var logger = new RollingFileLogger(logPath, maxFiles: 1, maxFileSize: 32)) {
			logger.Info(new string('a', 32));
			logger.Info("latest");
		}

		Assert.That(Directory.GetFiles(directory), Is.EqualTo(new[] { logPath }));
		Assert.That(File.ReadAllText(logPath), Is.EqualTo("latest" + Environment.NewLine));
	}

	[Test]
	public void OpeningOversizedExistingFile_ArchivesBeforeAppending() {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var logPath = Path.Combine(directory, "events.log");
		File.WriteAllText(logPath, new string('a', 80), Encoding.ASCII);
		using (var logger = new RollingFileLogger(logPath, maxFileSize: 32))
			logger.Info("new entry");

		Assert.That(File.ReadAllText(Directory.GetFiles(directory, "events_*.log").Single()), Is.EqualTo(new string('a', 80)));
		Assert.That(File.ReadAllText(logPath), Is.EqualTo("new entry" + Environment.NewLine));
	}

	[Test]
	public void RollingSinks_KeepIndependentEncodings() {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var asciiPath = Path.Combine(directory, "ascii.log");
		var unicodePath = Path.Combine(directory, "unicode.log");
		var unicode = new UnicodeEncoding(false, false, true);
		using (var asciiLogger = new RollingFileLogger(asciiPath, maxFileSize: 32, encoding: Encoding.ASCII))
		using (var unicodeLogger = new RollingFileLogger(unicodePath, maxFileSize: 32, encoding: unicode)) {
			asciiLogger.Info("ascii");
			unicodeLogger.Info("界");
			asciiLogger.Info("after");
		}

		Assert.That(File.ReadAllBytes(asciiPath), Is.EqualTo(Encoding.ASCII.GetBytes("ascii" + Environment.NewLine + "after" + Environment.NewLine)));
		Assert.That(File.ReadAllBytes(unicodePath), Is.EqualTo(unicode.GetBytes("界" + Environment.NewLine)));
	}

	[Test]
	public async Task FileAppendWriters_KeepIndependentEncodingsAndImmutableUtf8Default() {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var asciiPath = Path.Combine(directory, "ascii.log");
		var unicodePath = Path.Combine(directory, "unicode.log");
		var defaultPath = Path.Combine(directory, "utf8.log");
		var unicode = new UnicodeEncoding(false, false, true);
		using (var asciiWriter = new FileAppendTextWriter(asciiPath, Encoding.ASCII, true))
		using (var unicodeWriter = new FileAppendTextWriter(unicodePath, unicode, true))
		using (var defaultWriter = new FileAppendTextWriter(defaultPath, true)) {
			asciiWriter.Write("ascii");
			await unicodeWriter.WriteAsync("界");
			await asciiWriter.WriteAsync("after");
			defaultWriter.Write("é");
			Assert.That(asciiWriter.Encoding, Is.SameAs(Encoding.ASCII));
			Assert.That(unicodeWriter.Encoding, Is.SameAs(unicode));
		}

		Assert.That(File.ReadAllBytes(asciiPath), Is.EqualTo(Encoding.ASCII.GetBytes("asciiafter")));
		Assert.That(File.ReadAllBytes(unicodePath), Is.EqualTo(unicode.GetBytes("界")));
		Assert.That(File.ReadAllBytes(defaultPath), Is.EqualTo(new UTF8Encoding(false, true).GetBytes("é")));
	}
}

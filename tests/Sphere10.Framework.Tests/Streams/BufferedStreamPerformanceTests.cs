// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

/// <summary>Measures file-backed read and Length costs without enforcing machine-dependent speed thresholds.</summary>
[TestFixture]
[Explicit("Runs the file-backed BufferedStream performance comparison and writes measurement artifacts.")]
[Category("Performance")]
[NonParallelizable]
public class BufferedStreamPerformanceTests {

	private sealed record Workload(string Name, int ReadSize, int Bytes, int Operations);

	private sealed record Sample(Workload Workload, StreamVariant Variant, int Round, int Order, long Ticks, long AllocatedBytes, long Result);

	private sealed record ActivationAllocation(Workload Workload, StreamVariant Variant, long AllocatedBytes);

	// Keep the ordinary FileStream baseline distinct from wrappers over an unbuffered FileStream.
	private const int DefaultFileBufferSize = 4096;

	private const int UnbufferedFileBufferSize = 1;

	private const int WrapperBufferSize = 64 * 1024;

	private const int FileSize = 64 * 1024 * 1024;

	private const int SmallReadSize = 256 * 1024;

	private const int LengthIterations = 250_000;

	private const int WarmupRounds = 2;

	private const int SampleRounds = 5;

	private readonly List<Sample> _samples = new();

	private readonly List<Sample> _warmups = new();

	private readonly List<ActivationAllocation> _activationAllocations = new();

	private byte[] _expected;

	private string _dataPath;

	private string _outputDirectory;

	private DateTimeOffset _startedAt;

	/// <summary>Compares complete sequential reads through identical file and wrapper configurations.</summary>
	[TestCase(4 * 1024)]
	[TestCase(64 * 1024)]
	[TestCase(1024 * 1024)]
	public void SequentialChunkReads(int readSize) => CompareVariants(new Workload("Chunk reads", readSize, FileSize, FileSize / readSize));

	/// <summary>Bounds small-read workloads so raw one-byte file operations remain practical to reproduce.</summary>
	[TestCase(1)]
	[TestCase(8)]
	[TestCase(64)]
	[TestCase(256)]
	public void SmallReads(int readSize) => CompareVariants(new Workload("Small reads", readSize, SmallReadSize, SmallReadSize / readSize));

	/// <summary>Compares repeated length queries after one untimed query initializes lazy caches.</summary>
	[Test]
	public void RepeatedLengthQueries() => CompareVariants(new Workload("Length queries", 0, FileSize, LengthIterations));

	/// <summary>Creates one stable file in the operating system's temporary directory before any timed work.</summary>
	[OneTimeSetUp]
	public void CreateTestData() {
		_startedAt = DateTimeOffset.UtcNow;
		_outputDirectory = TestContext.Parameters.Get("BufferedStreamPerformanceDirectory",
			Path.Combine(Path.GetTempPath(), "Sphere10.BufferedStream.Performance", Guid.NewGuid().ToString("N")));
		Directory.CreateDirectory(_outputDirectory);
		_dataPath = Path.Combine(Path.GetTempPath(), "Sphere10.BufferedStream.Performance-" + Guid.NewGuid().ToString("N") + ".bin");

		// A deterministic byte pattern lets every sample validate its whole output after timing.
		_expected = new byte[FileSize];
		for (var index = 0; index < _expected.Length; index++)
			_expected[index] = unchecked((byte)(index * 31 + (index >> 16)));
		Tools.FileSystem.WriteAllBytes(_dataPath, _expected);
	}

	/// <summary>Retains raw samples and environment details, then removes only this fixture's unique data file.</summary>
	[OneTimeTearDown]
	public void WriteResultsAndCleanup() {
		try {
			if (_samples.Count > 0)
				WriteResults();
		} finally {
			if (_dataPath != null)
				File.Delete(_dataPath);
		}
	}

	// Rotate the first configuration each round; setup, disposal and correctness checks stay outside the timer.
	private void CompareVariants(Workload workload) {
		var variants = Tools.Enums.GetValues<StreamVariant>().ToArray();
		var destination = workload.ReadSize == 0 ? Array.Empty<byte>() : new byte[workload.Bytes];
		var streams = new Dictionary<StreamVariant, Stream>();
		try {
			// Keep eager constructor buffers and lazy first-read buffers out of the measured rounds equally.
			foreach (var variant in variants) {
				var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
				var stream = CreateStream(variant);
				var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
				streams.Add(variant, stream);
				_activationAllocations.Add(new ActivationAllocation(workload, variant, allocated));
			}
			for (var round = -WarmupRounds; round < SampleRounds; round++) {
				for (var order = 0; order < variants.Length; order++) {
					var variant = variants[(order + round + WarmupRounds) % variants.Length];
					var stream = streams[variant];
					if (workload.ReadSize == 0)
						_ = stream.Length;
					else {
						stream.Position = 0;
						destination.AsSpan().Fill(0xC7);
					}

					var sample = Measure(stream, destination, workload, variant, round, order);
					if (workload.ReadSize == 0)
						Assert.That(sample.Result, Is.EqualTo((long)FileSize * workload.Operations), variant.ToString());
					else {
						Assert.That(sample.Result, Is.EqualTo(workload.Bytes), variant.ToString());
						Assert.That(destination.AsSpan().SequenceEqual(_expected.AsSpan(0, workload.Bytes)), Is.True, variant.ToString());
					}
					if (round >= 0)
						_samples.Add(sample);
					else
						_warmups.Add(sample);
				}
			}
		} finally {
			foreach (var stream in streams.Values)
				stream.Dispose();
		}

		// Timing is diagnostic evidence, rather than a correctness assertion about relative speed.
		foreach (var variant in variants) {
			var samples = _samples.Where(sample => sample.Workload == workload && sample.Variant == variant).ToArray();
			var milliseconds = ToMilliseconds(Median(samples.Select(sample => sample.Ticks)));
			TestContext.Progress.WriteLine($"{workload.Name}, {workload.ReadSize} B, {variant}: {milliseconds:F4} ms median; {Median(samples.Select(sample => sample.AllocatedBytes)):F0} allocated bytes.");
		}
	}

	// Each configuration has the same file access, share and sequential-scan options, without timed decorators.
	private Stream CreateStream(StreamVariant variant) {
		if (variant == StreamVariant.FileStream4096)
			return OpenFile(DefaultFileBufferSize);
		if (variant == StreamVariant.FileStream1)
			return OpenFile(UnbufferedFileBufferSize);

		var source = OpenFile(UnbufferedFileBufferSize);
		try {
			return variant switch {
				StreamVariant.BclBuffered64K => new System.IO.BufferedStream(source, WrapperBufferSize),
				StreamVariant.Sphere64K => new BufferedStream(source, WrapperBufferSize, leaveOpen: false),
				StreamVariant.Sphere64KNoLengthCache => new BufferedStream(source, WrapperBufferSize, leaveOpen: false, enableStreamLengthCaching: false),
				StreamVariant.Sphere64KReadBufferingDisabled => new BufferedStream(source, WrapperBufferSize, leaveOpen: false, enableReadBuffering: false),
				_ => throw new ArgumentOutOfRangeException(nameof(variant))
			};
		} catch {
			source.Dispose();
			throw;
		}
	}

	// Open synchronously so the timed methods compare the same synchronous operating-system file API.
	private FileStream OpenFile(int bufferSize) => new FileStream(_dataPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan);

	// Keep only the operation loop and lightweight counters between timestamps; consume results after timing.
	private static Sample Measure(Stream stream, byte[] destination, Workload workload, StreamVariant variant, int round, int order) {
		var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
		var startedAt = Stopwatch.GetTimestamp();
		long result = 0;
		if (workload.ReadSize == 0) {
			for (var index = 0; index < workload.Operations; index++)
				result += stream.Length;
		} else {
			var offset = 0;
			while (offset < destination.Length) {
				var read = stream.Read(destination, offset, Math.Min(workload.ReadSize, destination.Length - offset));
				if (read == 0)
					break;
				offset += read;
			}
			result = offset;
		}
		var elapsed = Stopwatch.GetTimestamp() - startedAt;
		var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
		return new Sample(workload, variant, round, order, elapsed, allocated, result);
	}

	// Publish the raw five-sample measurements alongside medians and exact conditions for later comparison.
	private void WriteResults() {
		var summaries = _samples.GroupBy(sample => new { sample.Workload, sample.Variant }).Select(group => {
			var milliseconds = ToMilliseconds(Median(group.Select(sample => sample.Ticks)));
			return new {
				group.Key.Workload,
				Stream = group.Key.Variant.ToString(),
				Samples = group.Count(),
				MedianMilliseconds = milliseconds,
				MedianAllocatedBytes = Median(group.Select(sample => sample.AllocatedBytes)),
				MebibytesPerSecond = group.Key.Workload.ReadSize == 0 ? (double?)null : group.Key.Workload.Bytes / (1024d * 1024) / (milliseconds / 1000),
				NanosecondsPerOperation = milliseconds * 1_000_000 / group.Key.Workload.Operations
			};
		}).ToArray();
		var report = new {
			StartedAtUtc = _startedAt,
			CompletedAtUtc = DateTimeOffset.UtcNow,
			Runtime = RuntimeInformation.FrameworkDescription,
			RuntimeVersion = Environment.Version.ToString(),
			OperatingSystem = RuntimeInformation.OSDescription,
			ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
			Cpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? RuntimeInformation.ProcessArchitecture.ToString(),
			LogicalProcessors = Environment.ProcessorCount,
			ServerGarbageCollection = GCSettings.IsServerGC,
			StopwatchFrequency = Stopwatch.Frequency,
			DataFile = _dataPath,
			FileSize,
			SmallReadSize,
			WarmupRounds,
			SampleRounds,
			Conditions = "Synchronous FileAccess.Read, FileShare.Read, FileOptions.SequentialScan; wrappers use bufferSize=1 FileStream. Warm OS page cache, no cache flush. One stream per configuration is reused across two warmups and five measured rounds, excluding both eager constructor and lazy first-read buffer setup. Creation, rewind, destination reset, disposal and full-byte validation are outside timing. Length is initialized outside timing. No forced GC or timed decorators.",
			Configurations = new {
				DefaultFileBufferSize,
				UnbufferedFileBufferSize,
				WrapperBufferSize,
				Sphere64K = "Read buffering and length caching enabled",
				Sphere64KNoLengthCache = "Read buffering enabled; length caching disabled",
				Sphere64KReadBufferingDisabled = "Read buffering disabled; independent length caching enabled"
			},
			ConstructorAllocations = _activationAllocations.Select(sample => new {
				sample.Workload,
				Stream = sample.Variant.ToString(),
				sample.AllocatedBytes
			}).ToArray(),
			WarmupAllocations = _warmups.Select(sample => new {
				sample.Workload,
				Stream = sample.Variant.ToString(),
				sample.Round,
				sample.AllocatedBytes
			}).ToArray(),
			Summaries = summaries,
			Samples = _samples.Select(sample => new {
				sample.Workload,
				Stream = sample.Variant.ToString(),
				sample.Round,
				sample.Order,
				sample.Ticks,
				Milliseconds = ToMilliseconds(sample.Ticks),
				sample.AllocatedBytes,
				sample.Result
			}).ToArray()
		};
		var jsonPath = Path.Combine(_outputDirectory, "buffered-stream-performance.json");
		using (var stream = File.Create(jsonPath))
			JsonSerializer.Serialize(stream, report, new JsonSerializerOptions { WriteIndented = true });

		var csvPath = Path.Combine(_outputDirectory, "buffered-stream-performance.csv");
		using (var writer = new StreamWriter(csvPath, false, new UTF8Encoding(false))) {
			WriteCsvRow(writer, new[] { "Workload", "Stream", "ReadSizeBytes", "Bytes", "Operations", "Round", "Order", "Milliseconds", "AllocatedBytes", "Result" });
			foreach (var sample in _samples)
				WriteCsvRow(writer, new[] {
					sample.Workload.Name, sample.Variant.ToString(), sample.Workload.ReadSize.ToString(CultureInfo.InvariantCulture),
					sample.Workload.Bytes.ToString(CultureInfo.InvariantCulture), sample.Workload.Operations.ToString(CultureInfo.InvariantCulture),
					sample.Round.ToString(CultureInfo.InvariantCulture), sample.Order.ToString(CultureInfo.InvariantCulture),
					ToMilliseconds(sample.Ticks).ToString("R", CultureInfo.InvariantCulture), sample.AllocatedBytes.ToString(CultureInfo.InvariantCulture),
					sample.Result.ToString(CultureInfo.InvariantCulture)
				});
		}
		TestContext.AddTestAttachment(jsonPath, "Raw samples, medians and measurement environment");
		TestContext.AddTestAttachment(csvPath, "Individual measured samples");
		TestContext.Progress.WriteLine($"Performance artifacts: {jsonPath}; {csvPath}");
	}

	// Use the existing shared CSV writer so diagnostic output follows Framework escaping conventions.
	private static void WriteCsvRow(TextWriter writer, string[] fields)
		=> Tools.Data.WriteCsvRow(writer, fields, (field, destination) => Tools.Data.WriteCsvField(destination, field), ",", Environment.NewLine);

	// Five measured values give a middle sample without making a distributional assumption.
	private static double Median(IEnumerable<long> values) {
		var ordered = values.OrderBy(value => value).ToArray();
		return ordered[ordered.Length / 2];
	}

	// Retain raw ticks in artifacts and convert only after the timed operation has finished.
	private static double ToMilliseconds(double ticks) => ticks * 1000 / Stopwatch.Frequency;

	#region Inner Types

	// Separate ordinary FileStream buffering, BCL buffering and each independently configured Sphere10 mode.
	private enum StreamVariant {

		FileStream4096,

		FileStream1,

		BclBuffered64K,

		Sphere64K,

		Sphere64KNoLengthCache,

		Sphere64KReadBufferingDisabled
	}

	#endregion
}

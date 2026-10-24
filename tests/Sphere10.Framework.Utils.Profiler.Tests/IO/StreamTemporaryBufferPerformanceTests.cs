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
using System.IO;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace Sphere10.Framework.Utils.Profiler.Tests;

[TestFixture]
[NonParallelizable]
public class StreamTemporaryBufferPerformanceTests {

	[TestCase("EndianCharWrite")]
	[TestCase("ProfilerRead")]
	[TestCase("ProfilerCaptureRead")]
	[Explicit("Serial allocation diagnostic; run the same test assembly with the saved before and after Framework binaries.")]
	[Category("Performance")]
	public void TemporaryBufferSamples(string workload) {
		const int iterations = 4096;
		const int readSize = 64;
		var samples = new List<object>();
		var source = new byte[readSize];
		for (var index = 0; index < source.Length; index++)
			source[index] = (byte)index;
		for (var sample = -1; sample < 5; sample++) {
			using var stream = new MemoryStream(iterations * 2 + 128);
			stream.Write(source, 0, source.Length);
			using var writer = new EndianBinaryWriter(EndianBitConverter.Little, new NonClosingStream(stream));
			using var profiler = new StreamProfiler(new NonClosingStream(stream), true);
			var destination = new byte[readSize];
			for (var index = 0; index < 32; index++) {
				if (workload == "EndianCharWrite")
					writer.Write('A');
				else {
					profiler.Position = 0;
					if (profiler.Read(destination, 0, destination.Length) != destination.Length)
						throw new InvalidOperationException("The prepared stream returned an incomplete read.");
				}
			}
			if (workload == "EndianCharWrite") {
				stream.SetLength(0);
				stream.Position = 0;
			}
			if (workload == "ProfilerCaptureRead")
				profiler.StartListening();
			var started = Stopwatch.GetTimestamp();
			var before = GC.GetAllocatedBytesForCurrentThread();
			for (var index = 0; index < iterations; index++) {
				if (workload == "EndianCharWrite")
					writer.Write('A');
				else {
					profiler.Position = 0;
					if (profiler.Read(destination, 0, destination.Length) != destination.Length)
						throw new InvalidOperationException("The prepared stream returned an incomplete read.");
				}
			}
			var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
			var elapsed = Stopwatch.GetElapsedTime(started);
			if (workload == "EndianCharWrite") {
				var bytes = stream.ToArray();
				Assert.That(bytes.Length, Is.EqualTo(iterations * 2));
				for (var index = 0; index < bytes.Length; index += 2) {
					Assert.That(bytes[index], Is.EqualTo((byte)'A'));
					Assert.That(bytes[index + 1], Is.Zero);
				}
			} else {
				Assert.That(destination, Is.EqualTo(source));
				if (workload == "ProfilerCaptureRead") {
					var capture = profiler.StopListening();
					Assert.That(capture.Length, Is.EqualTo(iterations * readSize));
					Assert.That(capture.AsSpan(0, readSize).SequenceEqual(source), Is.True);
					Assert.That(capture.AsSpan(capture.Length - readSize).SequenceEqual(source), Is.True);
				}
			}
			if (sample >= 0)
				samples.Add(new { Sample = sample, AllocatedBytes = allocatedBytes, ElapsedMilliseconds = elapsed.TotalMilliseconds });
		}
		var report = new {
			Workload = workload, Iterations = iterations, ReadSize = readSize,
			FrameworkAssembly = typeof(StreamProfiler).Assembly.Location,
			FrameworkMvid = typeof(StreamProfiler).Assembly.ManifestModule.ModuleVersionId,
			Conditions = "Owner-thread allocations; input, destination, output capacity and warm-up are outside measurement. Capture-list growth remains included; owned snapshot materialization and validation are outside. Samples are diagnostic and establish no timing threshold.",
			Samples = samples
		};
		WriteReport(workload, report);
	}

	[TestCase("StringSerialize", 128)]
	[TestCase("StringSerialize", 2048)]
	[TestCase("StringDeserialize", 128)]
	[TestCase("StringDeserialize", 2048)]
	[TestCase("PaddedSerialize", 8192)]
	[TestCase("PaddedDeserialize", 8192)]
	[TestCase("NullReferencePadding", 8192)]
	[TestCase("NullablePadding", 4)]
	[Explicit("Serial allocation diagnostic using existing serializer APIs against the saved before and after Framework binaries.")]
	[Category("Performance")]
	public void SerializerTemporaryBufferSamples(string workload, int byteSize) {
		const int iterations = 4096;
		var samples = new List<object>();
		for (var sample = -1; sample < 5; sample++) {
			using var stream = new MemoryStream(byteSize + 64);
			using var writer = new EndianBinaryWriter(EndianBitConverter.Little, new NonClosingStream(stream));
			using var reader = new EndianBinaryReader(EndianBitConverter.Little, new NonClosingStream(stream));
			using var context = SerializationContext.New;
			Action operation;
			Action verify;
			switch (workload) {
				case "StringSerialize":
				case "StringDeserialize": {
					var serializer = new StringSerializer(Encoding.UTF8, SizeDescriptorStrategy.UseUInt32);
					var text = new string('A', byteSize);
					serializer.Serialize(text, writer, context);
					var last = string.Empty;
					operation = workload == "StringSerialize"
						? () => { stream.Position = 0; serializer.Serialize(text, writer, context); }
						: () => { stream.Position = 0; last = serializer.Deserialize(reader, context); };
					verify = () => {
						if (workload == "StringSerialize") {
							Assert.That(stream.Length, Is.EqualTo(byteSize + 4));
							stream.Position = 0;
							Assert.That(serializer.Deserialize(reader, context), Is.EqualTo(text));
						} else
							Assert.That(last, Is.EqualTo(text));
					};
					break;
				}
				case "PaddedSerialize":
				case "PaddedDeserialize": {
					var serializer = PrimitiveSerializer<byte>.Instance.AsConstantSize(byteSize);
					serializer.Serialize(42, writer, context);
					byte last = 0;
					operation = workload == "PaddedSerialize"
						? () => { stream.Position = 0; serializer.Serialize(42, writer, context); }
						: () => { stream.Position = 0; last = serializer.Deserialize(reader, context); };
					verify = () => {
						Assert.That(stream.Position, Is.EqualTo(byteSize));
						if (workload == "PaddedSerialize") {
							var expected = new byte[byteSize];
							expected[0] = 42;
							Assert.That(stream.ToArray(), Is.EqualTo(expected));
						} else
							Assert.That(last, Is.EqualTo(42));
					};
					break;
				}
				case "NullReferencePadding": {
					var serializer = new ConstantSizeReferenceTypeSerializer<byte[]>(new ReferenceSerializer<byte[]>(new ConstantSizeByteArraySerializer(byteSize), ReferenceSerializerMode.SupportNull));
					using (var writeContext = SerializationContext.New)
						serializer.Serialize(null, writer, writeContext);
					byte[] last = null;
					operation = () => { stream.Position = 0; last = serializer.Deserialize(reader, context); };
					verify = () => { Assert.That(last, Is.Null); Assert.That(stream.Position, Is.EqualTo(byteSize + 1)); };
					break;
				}
				case "NullablePadding": {
					var serializer = new NullableSerializer<int>(PrimitiveSerializer<int>.Instance, preserveConstantSize: true);
					serializer.Serialize(null, writer, context);
					int? last = null;
					operation = () => { stream.Position = 0; last = serializer.Deserialize(reader, context); };
					verify = () => { Assert.That(last, Is.Null); Assert.That(stream.Position, Is.EqualTo(5)); };
					break;
				}
				default: throw new ArgumentOutOfRangeException(nameof(workload));
			}
			for (var index = 0; index < 32; index++)
				operation();
			var started = Stopwatch.GetTimestamp();
			var before = GC.GetAllocatedBytesForCurrentThread();
			for (var index = 0; index < iterations; index++)
				operation();
			var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
			var elapsed = Stopwatch.GetElapsedTime(started);
			verify();
			if (sample >= 0)
				samples.Add(new { Sample = sample, AllocatedBytes = allocatedBytes, ElapsedMilliseconds = elapsed.TotalMilliseconds });
		}
		WriteReport(workload + "-" + byteSize, new {
			Workload = workload, ByteSize = byteSize, Iterations = iterations,
			FrameworkAssembly = typeof(StringSerializer).Assembly.Location,
			FrameworkMvid = typeof(StringSerializer).Assembly.ManifestModule.ModuleVersionId,
			Conditions = "Owner-thread allocations; serializer/context/input/output capacity and warm-up prepared outside measurement. Returned strings and reference tracking remain included. Verification is outside measurement; these samples establish no timing threshold.",
			Samples = samples
		});
	}

	private static void WriteReport(string workload, object report) {
		var root = Path.GetFullPath(TestContext.Parameters.Get("StreamTemporaryBufferArtifactRoot", Path.Combine(TestContext.CurrentContext.TestDirectory, "Results", "StreamTemporaryBuffers")));
		Directory.CreateDirectory(root);
		var path = Path.Combine(root, workload + "-" + Guid.NewGuid().ToString("N") + ".json");
		var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
		File.WriteAllText(path, json);
		TestContext.AddTestAttachment(path);
		TestContext.WriteLine(json);
	}
}

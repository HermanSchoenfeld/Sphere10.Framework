// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Text;

namespace Sphere10.Framework;

public static class SeedGenerator {

	public static byte[] Generate(EntropyPolicy policy = EntropyPolicy.Default, CHF chf = Sphere10FrameworkDefaults.HashFunction) {
		var seed = new byte[Hashers.GetDigestSizeBytes(chf)];
		Generate(seed, policy, chf);
		return seed;
	}

	public static void Generate(Span<byte> buffer, EntropyPolicy policy = EntropyPolicy.Default, CHF chf = Sphere10FrameworkDefaults.HashFunction) {
		var bufferBuilder = new ByteArrayBuilder();
		var bitConverter = policy.HasFlag(EntropyPolicy.UseBigEndianNotLittle) ? (EndianBitConverter) EndianBitConverter.Big : EndianBitConverter.Little;
		if (policy.HasFlag(EntropyPolicy.UseGuid)) {
			bufferBuilder.Append(Guid.NewGuid().ToByteArray());
		}
		if (policy.HasFlag(EntropyPolicy.UseDateTime)) {
			bufferBuilder.Append(bitConverter.GetBytes(DateTime.UtcNow.Ticks));
		}
		if (policy.HasFlag(EntropyPolicy.UseEnvironment)) {
			bufferBuilder.Append(Encoding.UTF8.GetBytes(Environment.CurrentDirectory));
			bufferBuilder.Append(Encoding.UTF8.GetBytes(Environment.CommandLine));
			bufferBuilder.Append(bitConverter.GetBytes(Environment.ProcessorCount));
			bufferBuilder.Append(bitConverter.GetBytes(Environment.WorkingSet));
			bufferBuilder.Append(bitConverter.GetBytes(Environment.CurrentManagedThreadId));
			bufferBuilder.Append(bitConverter.GetBytes(Environment.ProcessId));
			bufferBuilder.Append(bitConverter.GetBytes(Environment.TickCount64));
			bufferBuilder.Append(Encoding.UTF8.GetBytes(Environment.MachineName));
			bufferBuilder.Append(Encoding.UTF8.GetBytes(Environment.UserName));
			bufferBuilder.Append(Encoding.UTF8.GetBytes(Environment.UserDomainName));
			bufferBuilder.Append(Encoding.UTF8.GetBytes(Environment.OSVersion.ToString()));
			bufferBuilder.Append(bitConverter.GetBytes(Environment.SystemPageSize));
			bufferBuilder.Append(bitConverter.GetBytes(GC.GetTotalMemory(false)));
			bufferBuilder.Append(bitConverter.GetBytes(GC.CollectionCount(0)));
			bufferBuilder.Append(Encoding.UTF8.GetBytes(Environment.Version.ToString()));
			bufferBuilder.Append(bitConverter.GetBytes(Environment.Is64BitProcess ? 1L : 0L));
			bufferBuilder.Append(bitConverter.GetBytes(Environment.Is64BitOperatingSystem ? 1L : 0L));
		}
		Hashers.Hash(chf, bufferBuilder.ToArray(), buffer);
	}
}
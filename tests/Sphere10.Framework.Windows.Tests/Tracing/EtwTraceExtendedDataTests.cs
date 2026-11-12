// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
public sealed class EtwTraceExtendedDataTests {
	[TestCase(4)]
	[TestCase(8)]
	public void DocumentedStackPayloadsPreserveMatchIdentityAndExactAddresses(int pointerSize) {
		var data = EndianBitConverter.Little.GetBytes(42UL).Concat(pointerSize == 4 ? EndianBitConverter.Little.GetBytes(0xFFEEDDCCU)
			: EndianBitConverter.Little.GetBytes(0xFFEEDDCCBBAA9988UL)).ToArray();
		var type = pointerSize == 4 ? WinAPI.ADVAPI32.EVENT_HEADER_EXT_TYPE_STACK_TRACE32 : WinAPI.ADVAPI32.EVENT_HEADER_EXT_TYPE_STACK_TRACE64;
		var decoded = EtwTraceExtendedDataReader.Decode(type, data);
		Assert.That(decoded.MatchId, Is.EqualTo(42));
		Assert.That(decoded.StackAddresses.Single(), Is.EqualTo(pointerSize == 4 ? 0xFFEEDDCCUL : 0xFFEEDDCCBBAA9988UL));
		Assert.That(decoded.Data, Is.EqualTo(data));
		Assert.That(EtwTraceExtendedDataReader.Decode(type, new byte[9]).StackAddresses, Is.Empty);
	}

	[Test]
	public void CallbackOwnedPointersAreCopiedAndExtendedMetadataBoundIsReported() {
		Assert.That(Marshal.SizeOf<WinAPI.ADVAPI32.EVENT_HEADER_EXTENDED_DATA_ITEM>(), Is.EqualTo(16));
		using var payload = new UnmanagedMemoryScope(4);
		Marshal.Copy(new byte[] { 1, 2, 3, 4 }, 0, payload.Item, 4);
		using var items = new UnmanagedMemoryScope(65 * 16);
		for (var index = 0; index < 65; index++)
			Marshal.StructureToPtr(new WinAPI.ADVAPI32.EVENT_HEADER_EXTENDED_DATA_ITEM { ExtType = 123, DataSize = 4, DataPtr = (ulong)payload.Item.ToInt64() }, IntPtr.Add(items.Item, index * 16), false);
		var detached = EtwTraceExtendedDataReader.Read(items.Item, 65, out var omitted);
		Assert.That(detached.Length, Is.EqualTo(64));
		Assert.That(omitted, Is.EqualTo(1));
		Marshal.WriteInt32(payload.Item, 0);
		Assert.That(detached.All(item => item.Data.SequenceEqual(new byte[] { 1, 2, 3, 4 })), Is.True);
	}
}

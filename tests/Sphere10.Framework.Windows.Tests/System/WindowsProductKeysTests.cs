// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
public class WindowsProductKeysTests {
	private const string KeyDigits = "BCDFGHJKMPQRTVWXY2346789";

	// Microsoft's published generic keys for client and KMS installation, which activate nothing on their own.
	[TestCase("VK7JG-NPHTM-C97JM-9MPGT-3V66T")]
	[TestCase("W269N-WFGWX-YVC9B-4J6C9-T83GX")]
	[TestCase("NPPR9-FWDCX-D2C8J-H872K-2YT43")]
	// Made-up keys whose N is late, where the commonly copied flag test fails. The N's position is one base-24 digit, so it is never the last character.
	[TestCase("H872K-2YT43-D2C8J-FWDCX-NPPR9")]
	[TestCase("2YT43-H872K-D2C8J-FWDCX-PPRN9")]
	public void DecodesTheKeyAWindowsDigitalProductIdHolds(string key)
		=> Assert.That(WindowsProductKeys.DecodeKey(Encode(key)), Is.EqualTo(key));

	[Test]
	public void AClearedOrTruncatedValueHoldsNoKey() {
		Assert.That(WindowsProductKeys.DecodeKey(new byte[164]), Is.Null, "A volume activation leaves the key bytes zero.");
		Assert.That(WindowsProductKeys.DecodeKey(new byte[60]), Is.Null);
		Assert.That(WindowsProductKeys.DecodeKey(null), Is.Null);
	}

	[Test]
	[Category("Integration")]
	public async Task ReadsThisComputersWindowsProduct() {
		var product = await Tools.Windows.Computer.GetProductInfoAsync();
		Assert.That(product.ProductName, Does.StartWith("Windows"));
		Assert.That(product.BuildNumber, Does.Match(@"^\d+(\.\d+)?$"));
		Assert.That(product.Is64BitOperatingSystem, Is.EqualTo(Environment.Is64BitOperatingSystem));
		foreach (var key in new[] { product.Installed?.Key, product.Original?.Key, product.FirmwareKey }.Where(key => key != null))
			Assert.That(key, Does.Match("^([" + KeyDigits + "N]{5}-){4}[" + KeyDigits + "N]{5}$"));
	}

	[Test]
	public void ReadingStopsWhenCancelledBeforeItStarts() {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		Assert.That(async () => await Tools.Windows.Computer.GetProductInfoAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
	}

	// The inverse of Windows' encoding: the N's position is the leading base-24 digit, the 15 key bytes are the number little-endian, and the
	// last byte's fourth bit marks a key with an N.
	private static byte[] Encode(string key) {
		var text = key.Replace("-", string.Empty);
		var position = text.IndexOf('N');
		var digits = new[] { position }.Concat(text.Remove(position, 1).Select(character => KeyDigits.IndexOf(character))).ToArray();
		var number = digits.Aggregate(BigInteger.Zero, (total, digit) => total * 24 + digit);
		var bytes = number.ToByteArray(isUnsigned: true);
		var value = new byte[164];
		Array.Copy(bytes, 0, value, 52, bytes.Length);
		value[66] |= 0x08;
		return value;
	}
}

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
public sealed class NetworkProtocolTests {
	[Test]
	public void DnsResponseDecodesCompressedNamesAddressAndTtlWithoutGuessingNativeTypes() {
		var bytes = DnsResponse();
		Assert.That(Tools.Network.TryDecodeDnsMessage(bytes, out var message), Is.True);
		Assert.That(message.TransactionId, Is.EqualTo(0x1234));
		Assert.That(message.IsResponse, Is.True);
		Assert.That(message.Questions.Single().Name, Is.EqualTo("example.com"));
		Assert.That(message.Records.Single().Value, Is.EqualTo("192.0.2.1"));
		Assert.That(message.Records.Single().TimeToLiveSeconds, Is.EqualTo(60));
		Assert.That(Tools.Network.TryDecodeDnsMessage(Convert.FromHexString("123401000001000000000000C00C00010001"), out _), Is.False, "A cyclic/self name pointer cannot be projected as a name.");
	}

	[Test]
	public void CompleteClientHelloProjectsObservedSniAlpnAndOfferedVersion() {
		Assert.That(Tools.Network.TryDecodeTlsClientHello(ClientHello(), out var hello), Is.True);
		Assert.That(hello.ServerNames, Is.EqualTo(new[] { "example.com" }));
		Assert.That(hello.ApplicationProtocols, Is.EqualTo(new[] { "h2" }));
		Assert.That(hello.SupportedVersions, Is.EqualTo(new ushort[] { 0x0304 }));
		Assert.That(hello.CipherSuiteCount, Is.EqualTo(1));
		Assert.That(hello.ExtensionTypes, Is.EqualTo(new ushort[] { 0, 16, 43 }));
		Assert.That(hello.LegacyClientVersion, Is.EqualTo(0x0303), "The legacy compatibility field must not be labelled as the negotiated TLS version.");
	}

	[Test]
	public void TruncatedOrMalformedWireInputsAreRejectedWithoutReadingPastBounds() {
		foreach (var bytes in new[] { DnsResponse(), ClientHello() })
			for (var length = 0; length < bytes.Length; length++) {
				var prefix = bytes.AsSpan(0, length).ToArray();
				Assert.That(() => { Tools.Network.TryDecodeDnsMessage(prefix, out _); Tools.Network.TryDecodeTlsClientHello(prefix, out _); }, Throws.Nothing);
			}
		for (var length = 0; length < 2048; length += 7) {
			var bytes = Enumerable.Range(0, length).Select(index => (byte)(index * 37 + length)).ToArray();
			Assert.That(() => { Tools.Network.TryDecodeDnsMessage(bytes, out _); Tools.Network.TryDecodeTlsClientHello(bytes, out _); }, Throws.Nothing);
		}
	}

	public static byte[] DnsResponse() => Convert.FromHexString("123481800001000100000000076578616D706C6503636F6D0000010001C00C000100010000003C0004C0000201");

	public static byte[] ClientHello() {
		static byte[] U16(int value) => EndianBitConverter.Big.GetBytes((ushort)value);
		static byte[] Extension(ushort type, byte[] value) => EndianBitConverter.Big.GetBytes(type).Concat(U16(value.Length)).Concat(value).ToArray();
		var name = Encoding.ASCII.GetBytes("example.com");
		var names = new byte[] { 0 }.Concat(U16(name.Length)).Concat(name).ToArray();
		var sni = Extension(0, U16(names.Length).Concat(names).ToArray());
		var alpn = Extension(16, new byte[] { 0, 3, 2, (byte)'h', (byte)'2' });
		var versions = Extension(43, new byte[] { 2, 3, 4 });
		var extensions = sni.Concat(alpn).Concat(versions).ToArray();
		var body = new List<byte> { 3, 3 };
		body.AddRange(new byte[32]); body.Add(0); body.AddRange(new byte[] { 0, 2, 0x13, 1, 1, 0 });
		body.AddRange(U16(extensions.Length)); body.AddRange(extensions);
		var handshake = new byte[] { 1, 0, (byte)(body.Count >> 8), (byte)body.Count }.Concat(body).ToArray();
		return new byte[] { 22, 3, 3 }.Concat(U16(handshake.Length)).Concat(handshake).ToArray();
	}
}

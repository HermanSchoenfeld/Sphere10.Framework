// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class WarningRegressionTests {
	[Test]
	public void LegacyPasswordDerivationIsCompatible() {
		var key = PBKDF2.DeriveKey("warning-test-password", Encoding.UTF8.GetBytes("salt1234"), 1000, 32);
		Assert.That(Convert.ToHexString(key), Is.EqualTo("809B544CAE95FCB09A6648D2E2FA4504D92140B009A33EE3DA9E221FE11CA17C"));
	}

	[TestCase(0)]
	[TestCase(-1)]
	public void PasswordDerivationRejectsNonPositiveKeyLength(int keyLength) {
		Assert.That(
			() => PBKDF2.DeriveKey("warning-test-password", Encoding.UTF8.GetBytes("salt1234"), 1000, keyLength),
			Throws.TypeOf<ArgumentOutOfRangeException>().With.Property(nameof(ArgumentException.ParamName)).EqualTo(nameof(keyLength))
		);
	}

	[Test]
	public void LegacyEncryptedTextRemainsReadable() {
		const string ciphertext = "EAAAAFYIr/BZVGlvOSBbo3fnf3QkXQ910a/W2eZT+cDxvpBiZq9ixWdv/BbqgyQWrSdlTQ==";
		Assert.That(Tools.Crypto.DecryptStringAES(ciphertext, "warning-test-password", "salt1234"), Is.EqualTo("Compatibility text: £ 世界"));
		var encrypted = Tools.Crypto.EncryptStringAES("New text: £ 世界", "warning-test-password", "salt1234");
		Assert.That(Tools.Crypto.DecryptStringAES(encrypted, "warning-test-password", "salt1234"), Is.EqualTo("New text: £ 世界"));
	}

	[Test]
	public void LegacyCompressedTextRemainsReadable() {
		var ciphertext = Convert.FromBase64String("ygDJxXW1cmWreV7ITx8FkGsPa9a90m9EwbI66WyOi8TLljTHi238IzZV34BfYCuOy1ZVBtE9KjyOYms8FrbSwQ==");
		Assert.That(Tools.Text.DecompressText(ciphertext, "warning-test-password"), Is.EqualTo("Compression compatibility"));
	}

	[TestCase(42, 99)]
	[TestCase(0, -1)]
	public void ChecksumSubstitutionUsesConstructorValues(int reserved, int substitution) {
		var checksummer = new WithSubstitutionChecksummer<int>(new ActionChecksum<int>(value => value), reserved, substitution);
		Assert.That(checksummer.ReservedChecksum, Is.EqualTo(reserved));
		Assert.That(checksummer.SubstitutionChecksum, Is.EqualTo(substitution));
		Assert.That(checksummer.CalculateChecksum(reserved), Is.EqualTo(substitution));
		Assert.That(checksummer.CalculateChecksum(1234), Is.EqualTo(1234));
	}

	[Test]
	public async Task ScopeDecoratorForwardsNotificationsAndUnsubscription() {
		var notifications = 0;
		var removedNotifications = 0;
		EventHandlerEx removedHandler = () => removedNotifications++;
		var inner = new ActionScope(() => { });
		var decorator = new ScopeDecorator<IScope>(inner);
		decorator.ScopeEnd += () => notifications++;
		decorator.ScopeEnd += removedHandler;
		decorator.ScopeEnd -= removedHandler;
		await decorator.DisposeAsync();
		Assert.That(notifications, Is.EqualTo(1));
		Assert.That(removedNotifications, Is.Zero);
	}

	[TestCase("https://example.test/path", "https://example.test/path?reserved%26key=a%2Bb%26c%3Dd%23e%2Ff%3Fg%20%E4%B8%96%E7%95%8C")]
	[TestCase("https://example.test/path?existing=1", "https://example.test/path?existing=1&reserved%26key=a%2Bb%26c%3Dd%23e%2Ff%3Fg%20%E4%B8%96%E7%95%8C")]
	public void QueryStringEscapesEachNameAndValue(string url, string expected) {
		var parameters = new NameValueCollection { ["reserved&key"] = "a+b&c=d#e/f?g 世界" };
		Assert.That(Tools.Url.AppendQueryStringToUrl(url, parameters), Is.EqualTo(expected));
	}

	[Test]
	public void QueryStringPreservesValuelessFlagsAndEmptyValues() {
		var parameters = new NameValueCollection { ["a&b"] = null, ["empty"] = string.Empty };
		Assert.That(Tools.Url.AppendQueryStringToUrl("https://example.test", parameters), Is.EqualTo("https://example.test?a%26b&empty="));
	}

	[Test]
	public void UrlSlugsAndTruncationPreserveUnicodePunctuation() {
		Assert.That(Tools.Url.ToUrlSlug("First—Second–Third"), Is.EqualTo("first-second-third"));
		Assert.That("abcdef".Truncate(3), Is.EqualTo("abc…"));
	}

	[Test]
	public void BinaryReaderAndWriterAcceptEmptySpans() {
		using var stream = new MemoryStream();
		using var writer = new EndianBinaryWriter(EndianBitConverter.Little, stream);
		writer.Write(ReadOnlySpan<byte>.Empty);
		writer.Write(ReadOnlySpan<char>.Empty);
		Assert.That(stream.Length, Is.Zero);
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, stream);
		Assert.That(reader.Read(Span<byte>.Empty), Is.Zero);
	}

	[Test]
	public void EndianConversionRejectsInsufficientSpanLength() {
		Assert.That(() => EndianBitConverter.Little.ToInt32(ReadOnlySpan<byte>.Empty), Throws.TypeOf<ArgumentOutOfRangeException>());
		Assert.That(EndianBitConverter.Little.ToInt32(new byte[] { 4, 3, 2, 1 }), Is.EqualTo(0x01020304));
	}
}

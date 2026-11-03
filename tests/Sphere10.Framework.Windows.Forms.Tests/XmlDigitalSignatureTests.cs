// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Security.Cryptography;
using System.Xml;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Verifies XML signatures using the existing Windows desktop runtime.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class XmlDigitalSignatureTests {

	[Test]
	public void SignedDocumentRoundTripsWithoutChangingTheSource() {
		XmlDigitalSignature.GenerateKeys(out var privateKey, out var publicKey);
		var source = new XmlDocument();
		source.LoadXml("<record><value>example</value></record>");

		var signed = XmlDigitalSignature.CreateSignedDocument(source, privateKey);

		Assert.That(XmlDigitalSignature.CheckSignature(signed, publicKey), Is.True);
		Assert.That(XmlDigitalSignature.RemoveSignature(signed).OuterXml, Is.EqualTo(source.OuterXml));
		Assert.That(source.GetElementsByTagName("Signature"), Is.Empty);
		Assert.That(XmlDigitalSignature.GetSignature(signed), Is.Not.Null);
	}

	[Test]
	public void ChangedContentFailsVerification() {
		XmlDigitalSignature.GenerateKeys(out var privateKey, out var publicKey);
		var source = new XmlDocument();
		source.LoadXml("<record><value>example</value></record>");
		var signed = XmlDigitalSignature.CreateSignedDocument(source, privateKey);
		signed.SelectSingleNode("/record/value").InnerText = "changed";

		Assert.That(XmlDigitalSignature.CheckSignature(signed, publicKey), Is.False);
	}

	[TestCase(0)]
	[TestCase(2)]
	public void SignatureLookupRequiresExactlyOneSignature(int count) {
		var source = new XmlDocument();
		source.LoadXml("<record />");
		for (var index = 0; index < count; index++)
			source.DocumentElement.AppendChild(source.CreateElement("Signature"));

		Assert.That(() => XmlDigitalSignature.GetSignature(source), Throws.TypeOf<CryptographicException>());
	}
}

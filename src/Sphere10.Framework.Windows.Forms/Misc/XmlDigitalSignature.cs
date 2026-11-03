// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Creates and verifies enveloped RSA XML signatures using XML-encoded keys.</summary>
public static class XmlDigitalSignature {
	/// <summary>
	/// Create a signed xml document. Add a signature alement to the specified document using the specified private key.
	/// </summary>
	/// <param name="xmlToSign"></param>
	/// <param name="privateKeyXml">Private+public key</param>
	/// <returns></returns>
	public static XmlDocument CreateSignedDocument(XmlDocument xmlToSign, string privateKeyXml) {
		// Sign the supplied content before constructing the output document.
		var signature = CreateSignature(xmlToSign, privateKeyXml);
		var document = new XmlDocument();
		document.AppendChild(document.ImportNode(xmlToSign.DocumentElement, true));

		//Append the signature tag to the root element
		document.DocumentElement.PrependChild(document.ImportNode(signature, true));

		return document;
	}

	/// <summary>
	/// Check the signature of the specified signed document (created with CreateSignedDocument) using the specified public key.
	/// </summary>
	/// <param name="signedDoc"></param>
	/// <param name="publicKeyXml">Public key</param>
	/// <returns></returns>
	public static bool CheckSignature(XmlDocument signedDoc, string publicKeyXml) {
		using var rsa = RSA.Create();
		rsa.FromXmlString(publicKeyXml);

		// Create a new SignedXml object and pass it
		// the XML document class.
		var signedXml = new SignedXml(signedDoc);

		// Load the first <signature> node.
		signedXml.LoadXml(GetSignature(signedDoc));

		// Check the signature and return the result.
		return signedXml.CheckSignature(rsa);
	}

	/// <summary>
	/// Create a signature xml element for the specified xml document and private key
	/// </summary>
	/// <param name="xmlToSign"></param>
	/// <param name="privateKeyXml">Private+public key</param>
	/// <returns></returns>
	public static XmlElement CreateSignature(XmlDocument xmlToSign, string privateKeyXml) {
		// Prepare the signing key and document context.
		using var rsa = RSA.Create();
		rsa.FromXmlString(privateKeyXml);
		var signedXml = new SignedXml(xmlToSign);
		signedXml.SigningKey = rsa;

		// Create a reference to be signed
		var reference = new System.Security.Cryptography.Xml.Reference("");

		// Set the canonicalization method for the document.
		signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigCanonicalizationUrl; // No comments.

		// Add an enveloped transformation to the reference.
		var envelopedTransform = new XmlDsigEnvelopedSignatureTransform(false);
		reference.AddTransform(envelopedTransform);
		signedXml.AddReference(reference);

		// Compute the signature after all references and transforms are configured.
		signedXml.ComputeSignature();
		return signedXml.GetXml();
	}

	/// <summary>
	/// Generate the keys (public and private)
	/// </summary>
	/// <param name="privateKeyXml">Public and private key</param>
	/// <param name="publicKeyXml">Public key</param>
	public static void GenerateKeys(out string privateKeyXml, out string publicKeyXml) {
		using var rsa = RSA.Create(1024);
		privateKeyXml = rsa.ToXmlString(true);
		publicKeyXml = rsa.ToXmlString(false);
	}

	/// <summary>
	/// Create a new XmlDocument from the specified signed document removing the signature element.
	/// </summary>
	/// <param name="signedDoc"></param>
	/// <returns></returns>
	public static XmlDocument RemoveSignature(XmlDocument signedDoc) {
		// Copy the signed content so removing its signature leaves the caller's document intact.
		var document = new XmlDocument();
		document.AppendChild(document.ImportNode(signedDoc.DocumentElement, true));

		var signature = GetSignature(document);
		signature.ParentNode.RemoveChild(signature);

		return document;
	}

	/// <summary>
	/// Extract the signature element from the specified signed document.
	/// </summary>
	/// <param name="signedDoc"></param>
	/// <returns></returns>
	public static XmlElement GetSignature(XmlDocument signedDoc) {
		// Find the "Signature" node and create a new
		// XmlNodeList object.
		var signatures = signedDoc.GetElementsByTagName("Signature");

		// Throw an exception if no signature was found.
		if (signatures.Count <= 0)
			throw new CryptographicException("Verification failed: No Signature was found in the document.");

		// Supports one signature for
		// the entire XML document.  Throw an exception
		// if more than one signature was found.
		if (signatures.Count >= 2)
			throw new CryptographicException("Verification failed: More than one signature was found for the document.");

		return (XmlElement)signatures[0];
	}
}

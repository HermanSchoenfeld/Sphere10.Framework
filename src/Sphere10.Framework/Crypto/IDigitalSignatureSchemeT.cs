// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework;

/// <summary>Defines typed key operations and their non-generic signature-scheme adapters.</summary>
public interface IDigitalSignatureScheme<TPrivateKey, TPublicKey> : IDigitalSignatureScheme
	where TPrivateKey : IPrivateKey
	where TPublicKey : IPublicKey {
	bool TryParsePublicKey(ReadOnlySpan<byte> bytes, out TPublicKey publicKey);

	bool TryParsePrivateKey(ReadOnlySpan<byte> bytes, out TPrivateKey privateKey);

	TPrivateKey GeneratePrivateKey(ReadOnlySpan<byte> seed);

	TPublicKey DerivePublicKey(TPrivateKey privateKey, ulong signerNonce);

	bool IsPublicKey(TPrivateKey privateKey, ReadOnlySpan<byte> publicKeyBytes);

	byte[] SignDigest(TPrivateKey privateKey, ReadOnlySpan<byte> messageDigest, ulong signerNonce);

	#region Base Interface Bridges

	bool IDigitalSignatureScheme.TryParsePublicKey(ReadOnlySpan<byte> bytes, out IPublicKey publicKey) {
		var result = TryParsePublicKey(bytes, out TPublicKey typedPublicKey);
		publicKey = typedPublicKey;
		return result;
	}

	bool IDigitalSignatureScheme.TryParsePrivateKey(ReadOnlySpan<byte> bytes, out IPrivateKey privateKey) {
		var result = TryParsePrivateKey(bytes, out TPrivateKey typedPrivateKey);
		privateKey = typedPrivateKey;
		return result;
	}

	IPrivateKey IDigitalSignatureScheme.CreatePrivateKey(ReadOnlySpan<byte> seed) => GeneratePrivateKey(seed);

	IPublicKey IDigitalSignatureScheme.DerivePublicKey(IPrivateKey privateKey, ulong signerNonce) => DerivePublicKey((TPrivateKey)privateKey, signerNonce);

	bool IDigitalSignatureScheme.IsPublicKey(IPrivateKey privateKey, ReadOnlySpan<byte> publicKeyBytes) => IsPublicKey((TPrivateKey)privateKey, publicKeyBytes);

	byte[] IDigitalSignatureScheme.SignDigest(IPrivateKey privateKey, ReadOnlySpan<byte> messageDigest, ulong signerNonce) => SignDigest((TPrivateKey)privateKey, messageDigest, signerNonce);

	#endregion

}

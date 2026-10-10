// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework;

public abstract class DigitalSignatureSchemeBase<TPrivateKey, TPublicKey> : IDigitalSignatureScheme<TPrivateKey, TPublicKey>
	where TPrivateKey : IPrivateKey
	where TPublicKey : IPublicKey {


	protected DigitalSignatureSchemeBase(CHF messageDigestCHF) {
		MessageDigestCHF = messageDigestCHF;
		Traits = DigitalSignatureSchemeTraits.None;
	}

	public CHF MessageDigestCHF { get; }

	public int MessageDigestLength => Hashers.GetDigestSizeBytes(MessageDigestCHF);

	public DigitalSignatureSchemeTraits Traits { get; protected set; }

	public abstract IIESAlgorithm IES { get; }
	
	

	public abstract bool TryParsePublicKey(ReadOnlySpan<byte> bytes, out TPublicKey publicKey);

	public abstract bool TryParsePrivateKey(ReadOnlySpan<byte> bytes, out TPrivateKey privateKey);

	public TPrivateKey GeneratePrivateKey()
		=> (TPrivateKey)((IDigitalSignatureScheme)this).GeneratePrivateKey();

	public abstract TPrivateKey GeneratePrivateKey(ReadOnlySpan<byte> seed);

	public abstract TPublicKey DerivePublicKey(TPrivateKey privateKey, ulong signerNonce);

	public abstract bool IsPublicKey(TPrivateKey privateKey, ReadOnlySpan<byte> publicKeyBytes);

	public virtual byte[] CalculateMessageDigest(ReadOnlySpan<byte> message)
		=> Hashers.Hash(MessageDigestCHF, message);

	public abstract byte[] SignDigest(TPrivateKey privateKey, ReadOnlySpan<byte> messageDigest, ulong signerNonce);

	public abstract bool VerifyDigest(ReadOnlySpan<byte> signature, ReadOnlySpan<byte> messageDigest, ReadOnlySpan<byte> publicKey);

}


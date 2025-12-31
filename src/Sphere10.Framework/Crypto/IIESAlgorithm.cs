// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework;

public interface IIESAlgorithm {
	byte[] Encrypt(ReadOnlySpan<byte> message, IPublicKey publicKey);

	bool TryDecrypt(ReadOnlySpan<byte> encryptedMessage, out byte[] decryptedMessage, IPrivateKey privateKey);
}


public static class IIESAlgorithmExtensions {
	public static byte[] Decrypt(this IIESAlgorithm iesAlgorithm, ReadOnlySpan<byte> encryptedMessage, IPrivateKey privateKey) {
		if (!iesAlgorithm.TryDecrypt(encryptedMessage, out var decryptedMessage, privateKey))
			throw new InvalidOperationException("Unable to decrypt message"); // TODO: add proper exception types
		return decryptedMessage;
	}
}


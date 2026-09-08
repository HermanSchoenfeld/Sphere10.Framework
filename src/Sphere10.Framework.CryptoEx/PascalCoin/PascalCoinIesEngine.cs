// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Utilities;
using Sphere10.Framework.CryptoEx.IES;

namespace Sphere10.Framework.CryptoEx.PascalCoin;

/// <summary>
/// Implements PascalCoin's OpenSSL ECIES encoding and reads historical managed encodings.
/// </summary>
/// <remarks>
/// The supplied block cipher must not add or remove padding. The legacy MAC authenticates the
/// ciphertext body, not the header; original-length recovery does not authenticate that length.
/// See <see href="ECIES-compatibility.md"/> beside this file for the known issues, fixes and migration requirements.
/// </remarks>
public class PascalCoinIesEngine : CustomIesEngine {
	private const int _secureHeadSize = 6;
	internal const int MaxPlaintextLength = 32000;

	public PascalCoinIesEngine(IBasicAgreement agree, IDerivationFunction kdf, IMac mac)
		: base(agree, kdf, mac) {
	}

	public PascalCoinIesEngine(IBasicAgreement agree, IDerivationFunction kdf, IMac mac, BufferedBlockCipher cipher)
		: base(agree, kdf, mac, cipher) {
		Guard.ArgumentNot(cipher is PaddedBufferedBlockCipher, nameof(cipher), "Use an unpadded cipher; the PascalCoin engine handles padding.");
	}

	public override byte[] ProcessBlock(byte[] @in, int inOff, int inLen) {
		Guard.ArgumentNotNull(@in, nameof(@in));
		Guard.Argument(inOff >= 0 && inLen >= 0 && inOff <= @in.Length - inLen, nameof(inOff), "The input range is outside the buffer.");
		Guard.Ensure(Cipher != null, "PascalCoin ECIES requires a block cipher.");

		if (ForEncryption) {
			Guard.ArgumentLTE(inLen, MaxPlaintextLength, nameof(inLen), "PascalCoin supports messages of at most 32,000 bytes.");
			if (KeyPairGenerator != null) {
				var ephemeralKeyPair = KeyPairGenerator.Generate();
				PrivParam = ephemeralKeyPair.GetKeyPair().Private;
				V = ephemeralKeyPair.GetEncodedPublicKey();
			}
		}

		var header = ForEncryption ? default : ReadHeader(@in, inOff, inLen);
		Agree.Init(PrivParam);
		if (!ForEncryption) {
			if (KeyParser != null) {
				// Bound the parser to the encoded key and verify its length before any point decoding.
				var fieldSize = Agree.GetFieldSize();
				var pointEncoding = @in[inOff + _secureHeadSize];
				var expectedKeyLength = pointEncoding switch {
					2 or 3 => 1 + fieldSize,
					4 or 6 or 7 => 1 + 2 * fieldSize,
					_ => 0
				};
				Guard.Ensure(header.KeyLength > 0 && header.KeyLength == expectedKeyLength, "Invalid ephemeral public-key encoding or length.");
				using var keyStream = new MemoryStream(@in, inOff + _secureHeadSize, header.KeyLength, false);
				try {
					PubParam = KeyParser.ReadKey(keyStream);
				} catch (IOException error) {
					throw new InvalidCipherTextException("Unable to recover ephemeral public key: " + error.Message, error);
				} catch (ArgumentException error) {
					throw new InvalidCipherTextException("Unable to recover ephemeral public key: " + error.Message, error);
				}
				Guard.Ensure(keyStream.Position == header.KeyLength, "The ephemeral public-key length does not match its encoding.");
				V = @in.AsSpan(inOff + _secureHeadSize, header.KeyLength).ToArray();
			} else {
				Guard.Ensure(header.KeyLength == V.Length, "The ephemeral public-key length does not match the initialized engine.");
			}
		}

		// Retain PascalCoin's fixed-width ECDH secret and hash-only SHA-512 derivation.
		var sharedValue = Agree.CalculateAgreement(PubParam);
		var sharedSecret = BigIntegers.AsUnsignedByteArray(Agree.GetFieldSize(), sharedValue);
		using var secretScope = Tools.Scope.ExecuteOnDispose(() => Arrays.Fill(sharedSecret, 0));
		Kdf.Init(new KdfParameters(sharedSecret, null));
		if (ForEncryption)
			return EncryptBlock(@in, inOff, inLen);

		var paddedPlaintext = DecryptBlock(@in, inOff + _secureHeadSize, inLen - _secureHeadSize);
		using var plaintextScope = Tools.Scope.ExecuteOnDispose(() => Arrays.Fill(paddedPlaintext, 0));
		Guard.Ensure(paddedPlaintext.Length == header.BodyLength, "The cipher must return complete blocks without removing padding.");
		for (var index = header.OriginalLength; index < paddedPlaintext.Length; index++)
			Guard.Ensure(paddedPlaintext[index] == 0, "Invalid PascalCoin zero padding.");

		// The header supplies the original length; scanning for zeros would remove genuine binary data.
		return paddedPlaintext.AsSpan(0, header.OriginalLength).ToArray();
	}

	protected override byte[] DecryptBlock(byte[] inEnc, int inOff, int inLen) {
		var macSize = Mac.GetMacSize();
		Guard.Ensure(inLen >= V.Length + macSize, "The input does not contain the complete ephemeral key and MAC.");
		SetupBlockCipherAndMacKeyBytes(out var cipherKey, out var macKey);
		using var keyScope = Tools.Scope.ExecuteOnDispose(() => {
			Arrays.Fill(cipherKey, 0);
			Arrays.Fill(macKey, 0);
		});

		var bodyOffset = inOff + V.Length + macSize;
		var bodyLength = inLen - V.Length - macSize;
		var expectedMac = inEnc.AsSpan(inOff + V.Length, macSize).ToArray();
		var computedMac = new byte[macSize];
		Mac.Init(new KeyParameter(macKey));
		Mac.BlockUpdate(inEnc, bodyOffset, bodyLength);
		Mac.DoFinal(computedMac, 0);
		if (!Arrays.FixedTimeEquals(expectedMac, computedMac))
			throw new InvalidCipherTextException("Invalid MAC");

		// Authenticate the entire actual body, including any historical managed extra block, before decrypting.
		ICipherParameters cipherParameters = new KeyParameter(cipherKey);
		if (Iv != null)
			cipherParameters = new ParametersWithIV(cipherParameters, Iv);
		Cipher.Init(false, cipherParameters);
		return Cipher.DoFinal(inEnc, bodyOffset, bodyLength);
	}

	protected override byte[] EncryptBlock(byte[] @in, int inOff, int inLen) {
		SetupBlockCipherAndMacKeyBytes(out var cipherKey, out var macKey);
		using var keyScope = Tools.Scope.ExecuteOnDispose(() => {
			Arrays.Fill(cipherKey, 0);
			Arrays.Fill(macKey, 0);
		});

		ICipherParameters cipherParameters = new KeyParameter(cipherKey);
		if (Iv != null)
			cipherParameters = new ParametersWithIV(cipherParameters, Iv);
		Cipher.Init(true, cipherParameters);

		// OpenSSL pads only a partial final block; aligned and empty messages get no extra block.
		var blockSize = Cipher.GetBlockSize();
		var paddingLength = (blockSize - inLen % blockSize) % blockSize;
		var paddedPlaintext = new byte[inLen + paddingLength];
		using var plaintextScope = Tools.Scope.ExecuteOnDispose(() => Arrays.Fill(paddedPlaintext, 0));
		@in.AsSpan(inOff, inLen).CopyTo(paddedPlaintext);
		var ciphertext = Cipher.DoFinal(paddedPlaintext);
		Guard.Ensure(ciphertext.Length == paddedPlaintext.Length, "The cipher must not add automatic padding.");
		Guard.Ensure(ciphertext.Length <= ushort.MaxValue && V.Length <= byte.MaxValue, "The encrypted message exceeds the PascalCoin header fields.");

		var authenticationTag = new byte[Mac.GetMacSize()];
		Guard.Ensure(authenticationTag.Length <= byte.MaxValue, "The MAC exceeds the PascalCoin header field.");
		Mac.Init(new KeyParameter(macKey));
		Mac.BlockUpdate(ciphertext, 0, ciphertext.Length);
		Mac.DoFinal(authenticationTag, 0);

		// Native PascalCoin uses little-endian UInt16 lengths in its six-byte wire header.
		var output = new byte[_secureHeadSize + V.Length + authenticationTag.Length + ciphertext.Length];
		output[0] = (byte)V.Length;
		output[1] = (byte)authenticationTag.Length;
		EndianBitConverter.Little.GetBytes((ushort)inLen).CopyTo(output, 2);
		EndianBitConverter.Little.GetBytes((ushort)ciphertext.Length).CopyTo(output, 4);
		V.CopyTo(output, _secureHeadSize);
		authenticationTag.CopyTo(output, _secureHeadSize + V.Length);
		ciphertext.CopyTo(output, _secureHeadSize + V.Length + authenticationTag.Length);
		return output;
	}

	private (int KeyLength, int OriginalLength, int BodyLength) ReadHeader(byte[] input, int offset, int length) {
		Guard.Ensure(length >= _secureHeadSize, "The PascalCoin header is incomplete.");
		var keyLength = input[offset];
		var macLength = input[offset + 1];
		Guard.Ensure(macLength == Mac.GetMacSize(), "Invalid PascalCoin MAC length.");
		Guard.Ensure(length >= _secureHeadSize + keyLength + macLength, "The ephemeral key or MAC is incomplete.");
		var bodyLength = length - _secureHeadSize - keyLength - macLength;
		var blockSize = Cipher.GetBlockSize();
		Guard.Ensure(bodyLength % blockSize == 0, "The ciphertext body must contain complete blocks.");

		var encodedOriginalLength = EndianBitConverter.Little.ToUInt16(input, offset + 2);
		var encodedBodyLength = EndianBitConverter.Little.ToUInt16(input, offset + 4);
		// Old managed writers allowed oversized messages and wrapped the UInt16 header fields.
		// Actual body length and at most one block of padding uniquely recover the original length.
		var paddingLength = (bodyLength - encodedOriginalLength) & ushort.MaxValue;
		Guard.Ensure(paddingLength <= blockSize && paddingLength <= bodyLength, "Invalid PascalCoin original length.");
		var originalLength = bodyLength - paddingLength;
		var nativeBodyLength = originalLength + (long)((blockSize - originalLength % blockSize) % blockSize);
		Guard.Ensure(encodedBodyLength == (nativeBodyLength & ushort.MaxValue), "The PascalCoin body length does not match its original length.");

		// paddingLength == blockSize identifies the historical extra block on aligned managed messages.
		// These consistency checks do not authenticate the header; callers need trusted outer authentication.
		return (keyLength, originalLength, bodyLength);
	}
}

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework;

public abstract class HashFunctionBase : IHashFunction {
	protected bool _inTransform;
	private byte[] _computedDigest;
	private bool _hasComputedDigest;

	public abstract int DigestSize { get; }

	public virtual void Initialize() {
		// GetResult must distinguish a reset from a previously computed "empty pre-image" digest.
		_hasComputedDigest = false;
	}

	public virtual void Compute(ReadOnlySpan<byte> input, Span<byte> output) {
		// Standard single transformation, sub-class can override for performance
		Guard.Ensure(!_inTransform, "Complete prior transformations before starting a new one");
		Transform(input);
		GetResult(output);
	}

	public virtual void Transform(ReadOnlySpan<byte> data) {
		if (!_inTransform) {
			Initialize();
			_inTransform = true;
		}
		// Super-class implementation will perform the hashing
	}

	protected abstract void Finalize(Span<byte> digest);

	public void GetResult(Span<byte> result) {
		if (_inTransform || !_hasComputedDigest) {
			// Keep storage across preimages; validity is tracked separately for the empty-preimage case.
			if (_computedDigest == null)
				_computedDigest = new byte[DigestSize];
			else {
				if (_computedDigest.Length != DigestSize) // _computedDigest was from previous pre-image, resize it
					Array.Resize(ref _computedDigest, DigestSize);
				if (!_hasComputedDigest)
					_computedDigest.AsSpan().Clear();
			}
			_inTransform = false;
			// As with the original allocated buffer, retain the partial digest if finalization throws.
			_hasComputedDigest = true;
			Finalize(_computedDigest);
		}
		_computedDigest.CopyTo(result);
	}

	public void Reset() {
		Initialize();
		_hasComputedDigest = false;
	}

	public abstract object Clone();

	public virtual void Dispose() {
	}


}


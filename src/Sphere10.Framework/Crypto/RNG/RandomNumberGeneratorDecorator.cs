using System;

namespace Sphere10.Framework.Maths;

public class RandomNumberGeneratorDecorator(IRandomNumberGenerator rng) : IRandomNumberGenerator {
	protected IRandomNumberGenerator Internal = rng;

	public virtual void NextBytes(Span<byte> output) => Internal.NextBytes(output);
}


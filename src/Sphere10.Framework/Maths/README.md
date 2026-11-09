# Maths

A Q31.32 fixed-point type, running statistics, numerical integration and arc length, fast approximations, pseudo-random generators and power-of-two partitions.

`Tools.Maths` holds epsilon helpers, Euclidean modulo and distance, hyperbolic functions, 2D rotation, weighted random choice through a thread-local `Random`, and index permutation enumeration. `Tools.MathPlus` integrates and differentiates any `IFunction` with adaptive Simpson's rule, and computes arc length under a Euclidean or Minkowski `Metric`. Its "safe" overloads detect discontinuities within a value range. The `*DerivativeFunction`, `FunctionDerivative` and `FunctionIntegral` classes wrap an `IFunction` into the integrands and derivatives those methods use.

`FixedPoint` is a 64-bit Q31.32 number with saturating arithmetic, `Fast*` unchecked variants, `Sqrt`, `Ln`, `Pow` and `Exp`, and implicit conversions to and from `long`, `float`, `double` and `decimal`. `Statistics` accumulates count, sum, mean, variances, geometric and harmonic means, and can keep a bounded history for `GetTrend`. `Mersenne32Algorithm` and `XorShift32` are deterministic generators. The Mersenne generator backs a random number generator in [Crypto](../Crypto/README.md), and both drive the RandomHash functions of the CryptoEx project. `Pow2` represents a count as a descending list of power-of-two exponents, which the Merkle tree code uses for its perfect subtrees, and `BloomFilterMath` sizes Bloom filters. `ErrorBandEqualityComparer`, `FunctionDerivative`, `FunctionIntegral` and `Mersenne32Algorithm` are in the `Sphere10.Framework.Maths` namespace, and the others are in `Sphere10.Framework` or `Tools`.

`FastLog` fills a lookup table of 2^23 floats, about 32 MB, in its static constructor, so its first use is slow and memory-heavy.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ArcLengthDerivativeFunction`](ArcLengthDerivativeFunction.cs) | class | `IFunction` evaluating `sqrt(1 + f'(x)^2)`, the Euclidean arc-length integrand of a function. |
| [`BloomFilterMath`](BloomFilterMath.cs) | static class | Estimates a Bloom filter's false-positive rate, or the length needed for a target rate, from item count and hash rounds. |
| [`ErrorBandEqualityComparer`](ErrorBandEqualityComparer.cs) | class | `IEqualityComparer<decimal>` that treats values within a tolerance as equal and refuses `GetHashCode`. |
| [`FastExp`](FastExp.cs) | static class | Table-adjusted approximation of `Math.Exp` for doubles. |
| [`FastLog`](FastLog.cs) | static class | Table-based `Log2`, `Log10`, `Ln` and arbitrary-base logarithms for floats. |
| [`FixedPoint`](FixedPoint.cs) | struct | Q31.32 fixed-point number with saturating and fast arithmetic, rounding, transcendental functions and numeric conversions. |
| [`FunctionDerivative`](FunctionDerivative.cs) | class | `IFunction` evaluating the numerical derivative of another function. |
| [`FunctionIntegral`](FunctionIntegral.cs) | class | Evaluates the definite integral of a function from a fixed lower bound to `x`. |
| [`IFunction`](IFunction.cs) | interface | Real function of one variable with `Eval(double)`. |
| [`Mersenne32Algorithm`](Mersenne32Algorithm.cs) | class | Seeded 32-bit Mersenne Twister producing integers and floats. |
| [`Metric`](Metric.cs) | enum | Euclidean, timelike Minkowski or spacelike Minkowski metric for arc-length calculations. |
| [`Pow2`](Pow2.cs) | static class | Splits a number into power-of-two exponents and adds or multiplies such partitions. |
| [`SchoenfeldFunctions`](SchoenfeldFunctions.cs) | static class | Base-10 digit count, first, last and n-th digit, and decimal shift functions. |
| [`SpacelikeMinkowskiDerivativeFunction`](SpacelikeMinkowskiDerivativeFunction.cs) | class | `IFunction` for the arc-length integrand under the spacelike Minkowski metric. |
| [`Statistics`](Statistics.cs) | class | Running sample statistics with optional bounded history for trends over the last N data. |
| [`TimelikeMinkowskiDerivativeFunction`](TimelikeMinkowskiDerivativeFunction.cs) | class | `IFunction` for the arc-length integrand under the timelike Minkowski metric. |
| [`Tools.MathPlus`](MathPlusTool.cs) | tools | Adaptive Simpson integration, numerical derivatives and arc length of an `IFunction`, with discontinuity-tolerant variants. |
| [`Tools.Maths`](MathTool.cs) | tools | Epsilon truncation and clipping, Euclidean `Mod`, distances, rotations, inverse hyperbolic functions, random gambles and index permutations. |
| [`XorShift32`](XorShift32.cs) | static class | Advances a 32-bit xorshift state passed by reference and returns the next value. |

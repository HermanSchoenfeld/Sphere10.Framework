// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class SeedGeneratorTests {

    [Test]
    public void Generate_DefaultPolicy_ReturnsNonZeroBytes() {
        var seed = SeedGenerator.Generate();
        ClassicAssert.IsTrue(seed.Length > 0, "Seed must have non-zero length");
        ClassicAssert.IsFalse(seed.All(b => b == 0), "Seed must not be all zeros");
    }

    [Test]
    public void Generate_DefaultPolicy_LengthMatchesDigestSize() {
        var chf = Sphere10FrameworkDefaults.HashFunction;
        var expected = Hashers.GetDigestSizeBytes(chf);
        var seed = SeedGenerator.Generate();
        ClassicAssert.AreEqual(expected, seed.Length);
    }

    [Test]
    public void Generate_SuccessiveCalls_ProduceDifferentSeeds() {
        var seed1 = SeedGenerator.Generate();
        var seed2 = SeedGenerator.Generate();
        ClassicAssert.IsFalse(seed1.SequenceEqual(seed2),
            "Two successive calls must produce different seeds");
    }

    [Test]
    [TestCase(EntropyPolicy.UseGuid)]
    [TestCase(EntropyPolicy.UseDateTime)]
    [TestCase(EntropyPolicy.UseEnvironment)]
    [TestCase(EntropyPolicy.UseGuid | EntropyPolicy.UseDateTime)]
    [TestCase(EntropyPolicy.UseGuid | EntropyPolicy.UseEnvironment)]
    [TestCase(EntropyPolicy.UseDateTime | EntropyPolicy.UseEnvironment)]
    [TestCase(EntropyPolicy.Default)]
    public void Generate_EachPolicy_ProducesNonZeroSeed(EntropyPolicy policy) {
        var seed = SeedGenerator.Generate(policy);
        ClassicAssert.IsTrue(seed.Length > 0);
        ClassicAssert.IsFalse(seed.All(b => b == 0),
            $"Seed for policy {policy} must not be all zeros");
    }

    [Test]
    public void Generate_BigEndianPolicy_ProducesNonZeroSeed() {
        var seed = SeedGenerator.Generate(EntropyPolicy.Default | EntropyPolicy.UseBigEndianNotLittle);
        ClassicAssert.IsFalse(seed.All(b => b == 0));
    }

    [Test]
    public void Generate_BigEndianVsLittleEndian_ProduceDifferentSeeds() {
        // UseGuid introduces randomness, so use only deterministic sources
        var policy = EntropyPolicy.UseEnvironment;
        var seedLE = SeedGenerator.Generate(policy);
        var seedBE = SeedGenerator.Generate(policy | EntropyPolicy.UseBigEndianNotLittle);
        ClassicAssert.IsFalse(seedLE.SequenceEqual(seedBE),
            "Big-endian and little-endian policies should produce different seeds");
    }

    [Test]
    [TestCase(CHF.SHA2_256)]
    [TestCase(CHF.SHA2_512)]
    [TestCase(CHF.Blake2b_256)]
    public void Generate_DifferentCHF_MatchesDigestLength(CHF chf) {
        var expected = Hashers.GetDigestSizeBytes(chf);
        var seed = SeedGenerator.Generate(EntropyPolicy.Default, chf);
        ClassicAssert.AreEqual(expected, seed.Length);
    }

    [Test]
    public void Generate_SpanOverload_FillsBuffer() {
        var buffer = new byte[32];
        SeedGenerator.Generate(buffer);
        ClassicAssert.IsFalse(buffer.All(b => b == 0),
            "Span overload must fill the buffer with non-zero seed data");
    }

    [Test]
    public void Generate_SpanOverload_SmallBuffer_ThrowsWhenTooSmall() {
        var buffer = new byte[8];
        Assert.Throws<InvalidOperationException>(() => SeedGenerator.Generate(buffer));
    }

    [Test]
    public void Generate_SpanOverload_LargeBuffer() {
        var buffer = new byte[128];
        SeedGenerator.Generate(buffer);
        ClassicAssert.IsFalse(buffer.All(b => b == 0));
    }

    [Test]
    public void Generate_GuidOnlyPolicy_SuccessiveCalls_Differ() {
        var seed1 = SeedGenerator.Generate(EntropyPolicy.UseGuid);
        var seed2 = SeedGenerator.Generate(EntropyPolicy.UseGuid);
        ClassicAssert.IsFalse(seed1.SequenceEqual(seed2),
            "GUID-based seeds must differ across calls");
    }

    [Test]
    public void Generate_EnvironmentOnlyPolicy_ProducesNonZero() {
        var seed = SeedGenerator.Generate(EntropyPolicy.UseEnvironment);
        ClassicAssert.IsFalse(seed.All(b => b == 0),
            "Environment-only policy must produce non-zero seed from process telemetry");
    }
}

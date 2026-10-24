// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Sphere10.Framework.NUnit;
using Sphere10.Framework.Tests;

namespace Sphere10.Framework.Utils.Profiler.Tests.StreamMappedDictionaries;

[TestFixture]
[NonParallelizable]
public class StreamMappedDictionaryWorkloadTests {
	public const int ClusterSize = 32;
	public const int HeavyMaxItems = 250;
	public const int HeavyIterations = 250;
	public const int OperationSeed = 31337;

	public static IEnumerable<TestCaseData> HeavyCases() => Cases("IntegrationTests_Heavy", HeavyMaxItems);

	public static IEnumerable<TestCaseData> SmokeCases() => Cases("IntegrationTests_Smoke", 8);

	[TestCaseSource(nameof(HeavyCases))]
	[Explicit("Runs the complete 250-iteration dictionary integration workload; select explicitly in a serial profiling session.")]
	[Category("Performance")]
	public void IntegrationTests_Heavy(string implementation, ClusteredStreamsPolicy policy) => RunWorkload(implementation, policy, HeavyMaxItems, HeavyIterations);

	[TestCaseSource(nameof(SmokeCases))]
	public void IntegrationTests_Smoke(string implementation, ClusteredStreamsPolicy policy) => RunWorkload(implementation, policy, 8, 3);

	[Test]
	public void HeavyRegistryContainsBothImplementationsAndAllThreePolicies() {
		var cases = HeavyCases().ToArray();
		Assert.That(cases, Has.Length.EqualTo(6));
		Assert.That(cases.Select(test => $"{test.Arguments[0]}/{test.Arguments[1]}"), Is.EquivalentTo(new[] {
			"Kvp/Debug", "Kvp/Default", "Kvp/Performance", "Clk/Debug", "Clk/Default", "Clk/Performance"
		}));
		Assert.That(ClusterSize, Is.EqualTo(32));
		Assert.That(HeavyMaxItems, Is.EqualTo(250));
		Assert.That(HeavyIterations, Is.EqualTo(250));
	}

	private static IEnumerable<TestCaseData> Cases(string method, int maxItems) {
		foreach (var implementation in new[] { "Kvp", "Clk" })
			foreach (var policy in new[] { ClusteredStreamsPolicy.Debug, ClusteredStreamsPolicy.Default, ClusteredStreamsPolicy.Performance })
				yield return new TestCaseData(implementation, policy).SetName($"{method}({implementation},MemoryStream,{policy},{maxItems})");
	}

	private static void RunWorkload(string implementation, ClusteredStreamsPolicy policy, int maxItems, int iterations) {
		// Match the core fixture's construction and reuse its exact payload sources and unchanged assertion/operation helper.
		using var stream = new MemoryStream();
		var keySerializer = new StringSerializer(Encoding.UTF8).AsReferenceSerializer();
		using IStreamMappedDictionary<string, TestObject> dictionary = implementation switch {
			"Kvp" => StreamMappedFactory.CreateDictionaryKvp(stream, ClusterSize, keySerializer, new TestObjectSerializer(),
				EqualityComparer<string>.Default, new TestObjectEqualityComparer(), null, policy, Endianness.LittleEndian, false),
			"Clk" => StreamMappedFactory.CreateDictionaryClk(stream, ClusterSize, keySerializer.AsConstantSize(256), new TestObjectSerializer(),
				EqualityComparer<string>.Default, new TestObjectEqualityComparer(), policy),
			_ => throw new ArgumentOutOfRangeException(nameof(implementation))
		};
		if (dictionary.RequiresLoad)
			dictionary.Load();
		TestContext.WriteLine($"{implementation}; MemoryStream; clusters={ClusterSize}; maxItems={maxItems}; iterations={iterations}; operation seed={OperationSeed}; policy={policy}.");
		TestContext.WriteLine("The canonical helper seeds operation/input generation; its removal shuffles retain the existing thread-local RNG behavior.");
		var keyGens = 0;
		var completedIterations = 0;
		AssertEx.DictionaryIntegrationTest(dictionary, maxItems,
			random => ($"{keyGens++}_{random.NextString(0, 100)}", new TestObject(random)),
			iterations: iterations,
			valueComparer: new TestObjectEqualityComparer(),
			endOfIterTest: () => {
				completedIterations++;
				if (completedIterations % 25 == 0)
					TestContext.Progress.WriteLine($"{TestContext.CurrentContext.Test.FullName}: {completedIterations}/{iterations} iterations completed.");
			});
		Assert.That(completedIterations, Is.EqualTo(iterations));
	}
}

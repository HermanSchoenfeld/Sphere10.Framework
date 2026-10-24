// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

namespace Sphere10.Framework.Utils.Profiler.Tests.StreamMappedDictionaries;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class RecordedStreamMappedDictionaryResultsTests {
	private static readonly XNamespace TrxNamespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

	public static IEnumerable<TestCaseData> SupportedCaptureIdentities() {
		foreach (var implementation in new[] { "Kvp", "Clk" })
			foreach (var policy in new[] { "Debug", "Default", "Performance" })
				foreach (var profilerFixture in new[] { false, true })
					yield return new TestCaseData(implementation, policy, profilerFixture);
	}

	[TestCaseSource(nameof(SupportedCaptureIdentities))]
	public void BothExactCaptureShapesPreserveActualResultDuration(string implementation, string policy, bool profilerFixture) {
		var testClass = profilerFixture ? typeof(StreamMappedDictionaryWorkloadTests).FullName : implementation == "Clk"
			? "Sphere10.Framework.Tests.StreamMappedDictionaryCLKTests" : "Sphere10.Framework.Tests.StreamMappedDictionaryTests";
		var testName = $"IntegrationTests_Heavy({(profilerFixture ? implementation + "," : "")}MemoryStream,{policy},250)";
		var document = CreateCapture(testClass, testName);
		Assert.That(RecordedStreamMappedDictionaryResults.ReadSuccessfulCase(document, implementation, policy, testClass, testName), Is.EqualTo(1.25));
	}

	[TestCase("unknown implementation")]
	[TestCase("unknown policy")]
	[TestCase("unrelated class")]
	[TestCase("wrong core implementation")]
	[TestCase("core name with profiler class")]
	[TestCase("profiler name with core class")]
	[TestCase("different item limit")]
	[TestCase("different storage")]
	[TestCase("wrong result class")]
	[TestCase("wrong result name")]
	[TestCase("wrong definition name")]
	[TestCase("missing definition")]
	[TestCase("duplicate result")]
	[TestCase("failed result")]
	[TestCase("skipped result")]
	[TestCase("zero duration")]
	public void MismatchedAndUnsuccessfulCapturesCannotSatisfyCoverage(string mutation) {
		var implementation = "Clk";
		var policy = "Default";
		var testClass = typeof(StreamMappedDictionaryWorkloadTests).FullName;
		var testName = "IntegrationTests_Heavy(Clk,MemoryStream,Default,250)";
		var document = CreateCapture(testClass, testName);
		var result = document.Descendants(TrxNamespace + "UnitTestResult").Single();
		var definition = document.Descendants(TrxNamespace + "TestMethod").Single();
		switch (mutation) {
			case "unknown implementation": implementation = "Other"; break;
			case "unknown policy": policy = "Other"; break;
			case "unrelated class": testClass = "Other.StreamMappedDictionaryWorkloadTests"; break;
			case "wrong core implementation":
				testClass = "Sphere10.Framework.Tests.StreamMappedDictionaryTests";
				testName = "IntegrationTests_Heavy(MemoryStream,Default,250)";
				break;
			case "core name with profiler class": testName = "IntegrationTests_Heavy(MemoryStream,Default,250)"; break;
			case "profiler name with core class": testClass = "Sphere10.Framework.Tests.StreamMappedDictionaryCLKTests"; break;
			case "different item limit": testName = "IntegrationTests_Heavy(Clk,MemoryStream,Default,249)"; break;
			case "different storage": testName = "IntegrationTests_Heavy(Clk,FileStream,Default,250)"; break;
			case "wrong result class": definition.SetAttributeValue("className", "Other.WorkloadTests"); break;
			case "wrong result name": result.SetAttributeValue("testName", "Other"); break;
			case "wrong definition name": definition.SetAttributeValue("name", "Other"); break;
			case "missing definition": definition.Parent.Remove(); break;
			case "duplicate result": result.Parent.Add(new XElement(result)); break;
			case "failed result": result.SetAttributeValue("outcome", "Failed"); break;
			case "skipped result": result.SetAttributeValue("outcome", "NotExecuted"); break;
			case "zero duration": result.SetAttributeValue("duration", "00:00:00"); break;
			default: throw new ArgumentOutOfRangeException(nameof(mutation));
		}
		Assert.That(() => RecordedStreamMappedDictionaryResults.ReadSuccessfulCase(document, implementation, policy, testClass, testName),
			Throws.InstanceOf<AssertionException>());
	}

	private static XDocument CreateCapture(string testClass, string testName) => new(
		new XElement(TrxNamespace + "TestRun",
			new XElement(TrxNamespace + "Results",
				new XElement(TrxNamespace + "UnitTestResult", new XAttribute("testId", "case-1"), new XAttribute("testName", testName),
					new XAttribute("outcome", "Passed"), new XAttribute("duration", "00:00:01.2500000"))),
			new XElement(TrxNamespace + "TestDefinitions",
				new XElement(TrxNamespace + "UnitTest", new XAttribute("id", "case-1"),
					new XElement(TrxNamespace + "TestMethod", new XAttribute("className", testClass), new XAttribute("name", testName))))));
}

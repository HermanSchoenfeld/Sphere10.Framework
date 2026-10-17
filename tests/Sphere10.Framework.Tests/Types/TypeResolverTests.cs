// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class TypeResolverTests {
	[TestCase(1)]
	[TestCase(5)]
	public void AssemblyIndexPreservesSharedShortNamesAndUniqueFullNames(int matchingTypes) {
		var names = Enumerable.Range(0, matchingTypes).Select(index => $"Namespace{index}.SharedName").ToArray();
		var assembly = CreateAssembly(names);
		var index = new TypeResolver.AssemblyInfo(assembly);
		Assert.That(index.TypesByShortName.Count, Is.EqualTo(1));
		Assert.That(index.TypesByShortName["SharedName"], Is.EquivalentTo(assembly.GetTypes()), "A bucket must grow when several namespaces use the same short name.");
		Assert.That(index.TypesByLongName.Count, Is.EqualTo(matchingTypes));
		foreach (var name in names)
			Assert.That(index.TypesByLongName[name], Is.SameAs(assembly.GetType(name)));
		Assert.That(index.TypesByShortName["MissingName"], Is.Empty);
	}

	[Test]
	public void ResolverRejectsAmbiguousShortNameAndResolvesEachFullName() {
		var names = new[] { "First.SharedName", "Second.SharedName", "Third.SharedName" };
		var assembly = CreateAssembly(names);
		Assert.That(() => TypeResolver.ResolveTypeInAssembly(assembly, "SharedName"), Throws.TypeOf<SoftwareException>().With.Message.Contains("multiple types"));
		foreach (var name in names)
			Assert.That(TypeResolver.ResolveTypeInAssembly(assembly, name), Is.SameAs(assembly.GetType(name)));
	}

	[Test]
	public void SparseAssemblyIndexAllocationRemainsProportionalToItsTypeCount() {
		var names = Enumerable.Range(0, 100).Select(index => $"Generated.Type{index:D3}").ToArray();
		var assembly = CreateAssembly(names);
		// Warm the index path before measuring; assembly/type generation belongs outside this allocation budget.
		_ = new TypeResolver.AssemblyInfo(CreateAssembly("Warmup.Seed"));
		var before = GC.GetAllocatedBytesForCurrentThread();
		var index = new TypeResolver.AssemblyInfo(assembly);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert.That(index.TypesByShortName.Count, Is.EqualTo(100));
		Assert.That(index.TypesByLongName.Count, Is.EqualTo(100));
		Assert.That(allocated, Is.LessThan(512 * 1024), "Indexing 100 sparse names must not reserve 4096 Type slots per name (over 3 MB on x64).");
	}

	private static Assembly CreateAssembly(params string[] names) {
		var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"TypeResolverTests.{Guid.NewGuid():N}"), AssemblyBuilderAccess.RunAndCollect);
		var module = assembly.DefineDynamicModule("Types");
		foreach (var name in names)
			module.DefineType(name, TypeAttributes.Public).CreateType();
		return assembly;
	}
}

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Category("Architecture")]
[Parallelizable(ParallelScope.Children)]
public class ApplicationDependencyTests {
	[TestCase("Microsoft.AspNetCore")]
	[TestCase("Microsoft.JSInterop")]
	[TestCase("Microsoft.Maui")]
	[TestCase("System.Windows.Forms")]
	[TestCase("System.Drawing.Common")]
	[TestCase("PresentationCore")]
	[TestCase("PresentationFramework")]
	[TestCase("WindowsBase")]
	[TestCase("Sphere10.Framework.Windows")]
	[TestCase("Sphere10.Framework.Web")]
	[TestCase("Sphere10.Framework.Drawing")]
	public void ApplicationDependencyClosureDoesNotReferenceUiAssemblies(string forbiddenAssembly) {
		var visited = new HashSet<string>(StringComparer.Ordinal);
		var pending = new Stack<Assembly>();
		var violations = new List<string>();
		pending.Push(typeof(Sphere10Framework).Assembly);
		while (pending.TryPop(out var assembly)) {
			if (!visited.Add(assembly.GetName().Name))
				continue;
			foreach (var reference in assembly.GetReferencedAssemblies()) {
				if (reference.Name == forbiddenAssembly || reference.Name.StartsWith(forbiddenAssembly + ".", StringComparison.Ordinal))
					violations.Add($"{assembly.GetName().Name} -> {reference.Name}");
				if (reference.Name.StartsWith("Sphere10.", StringComparison.Ordinal))
					pending.Push(Assembly.Load(reference));
			}
		}

		Assert.That(violations, Is.Empty, "Application and its framework dependencies must remain independent of UI assemblies.");
	}
}

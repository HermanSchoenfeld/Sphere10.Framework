// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Utils.WinFormsTester;
using Sphere10.Framework.Windows.Forms.DataVisualization;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Derives coverage from the libraries' exported native types, rather than a second manually maintained list.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ControlCatalogTests {
	[Test]
	public void EveryExportedControlAndFormHasOneNamedExample() {
		var assemblies = typeof(ControlCatalogSamples).Assembly.GetReferencedAssemblies()
			.Where(name => name.Name.StartsWith("Sphere10.Framework.Windows.Forms", StringComparison.Ordinal))
			.Select(Assembly.Load)
			.Append(typeof(Chart).Assembly)
			.Distinct()
			.ToArray();
		var exported = assemblies
			.SelectMany(assembly => assembly.GetExportedTypes())
			.Where(type => typeof(Control).IsAssignableFrom(type) || typeof(ToolStripItem).IsAssignableFrom(type))
			.ToArray();
		var examples = ControlCatalogSamples.CreateEntries();
		Assert.That(examples.Where(entry => typeof(Control).IsAssignableFrom(entry.ControlType) || typeof(ToolStripItem).IsAssignableFrom(entry.ControlType)).Select(entry => entry.ControlType), Is.EquivalentTo(exported));
		Assert.That(examples.Select(entry => entry.ControlType).Distinct().Count(), Is.EqualTo(examples.Length));
		foreach (var entry in examples) {
			Assert.That(entry.ClassName, Is.EqualTo(entry.ControlType.FullName));
			Assert.That(entry.Instructions, Is.Not.Empty, entry.ClassName);
			Assert.That(entry.CreateExample != null || entry.ShowWindow != null, Is.True, entry.ClassName);
		}
		var inventory = exported.OrderBy(type => type.FullName, StringComparer.Ordinal).Select(type => {
			var example = examples.Single(entry => entry.ControlType == type);
			return new {
				ClassName = type.FullName,
				Assembly = type.Assembly.GetName().Name,
				BaseClass = type.BaseType?.FullName,
				type.IsAbstract,
				type.IsGenericTypeDefinition,
				IsControl = typeof(Control).IsAssignableFrom(type),
				IsToolStripItem = typeof(ToolStripItem).IsAssignableFrom(type),
				example.Category,
				example.OpensWindow,
				example.Instructions
			};
		}).ToArray();
		var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "control-catalog-inventory.json");
		File.WriteAllText(path, JsonSerializer.Serialize(inventory, new JsonSerializerOptions { WriteIndented = true }));
		TestContext.Progress.WriteLine($"Catalog coverage: {inventory.Count(type => type.IsControl)} controls, {inventory.Count(type => type.IsToolStripItem)} tool-strip items, {examples.Length} total examples. Inventory: {path}");
	}

	[Test]
	public void CatalogConstructionCreatesNoSampleAndNeedsNoApplicationServices() {
		using var catalog = new ControlCatalogScreen();
		Assert.That(catalog.Controls.Find("catalogPreview", true).Single().Controls, Is.Empty);
		Assert.That(catalog.Controls.Find("catalogClasses", true).Cast<ListBox>().Single().Items, Is.Empty);
		Assert.That(catalog.IsHandleCreated, Is.False);
	}

	[Test]
	public void SampleImagesAreReleasedWithTheirActualControls() {
		foreach (var type in new[] { typeof(ImageNavigator), typeof(PictureBoxEx) }) {
			var entry = ControlCatalogSamples.CreateEntries().Single(example => example.ControlType == type);
			var sample = entry.CreateExample();
			var image = sample is ImageNavigator navigator ? navigator.Images[0] : ((PictureBox)sample).Image;
			Assert.That(image.Width, Is.GreaterThan(0));
			sample.Dispose();
			Assert.That(() => {
				_ = image.Width;
			}, Throws.TypeOf<ArgumentException>(), type.FullName);
		}
	}

	// Runtime samples are checked by the real application-host traversal, after their services and dispatcher exist.
	internal static bool ContainsRepresentedType(Control sample, Type expected) {
		return FindRepresentedObject(sample, expected) != null;
	}

	internal static object FindRepresentedObject(Control sample, Type expected) {
		return Subjects(sample).FirstOrDefault(subject => Represents(expected, subject.GetType()));
	}

	private static IEnumerable<object> Subjects(Control control) {
		yield return control;
		if (control.Tag is Component component)
			yield return component;
		if (control is ToolStrip strip)
			foreach (var item in strip.Items.Cast<ToolStripItem>())
				yield return item;
		foreach (var subject in control.Controls.Cast<Control>().SelectMany(Subjects))
			yield return subject;
	}

	private static bool Represents(Type expected, Type actual) {
		if (!expected.IsGenericTypeDefinition)
			return expected.IsAssignableFrom(actual);
		for (var type = actual; type != null; type = type.BaseType)
			if (type.IsGenericType && type.GetGenericTypeDefinition() == expected)
				return true;
		return false;
	}
}


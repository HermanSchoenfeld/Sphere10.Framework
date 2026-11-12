// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>
/// Forms and controls stay editable in the Visual Studio WinForms designer: each loads as the root of a design surface with no framework
/// running, and its designer file has the generated shape (components container, Dispose, generated-code region) and assigns only
/// components to fields in InitializeComponent, which is all the designer can keep as fields. Add a type here when changing its surface.
/// </summary>
[TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
public class DesignerCompatibilityTests {
	private static readonly Type[] DesignedTypes = [
		typeof(ImageAttachmentSelectorControl), typeof(ProductProductReportBugDialog), typeof(ProductProductRequestFeatureDialog),
		typeof(ProductProductSendCommentsDialog), typeof(ProductAboutBox), typeof(ApplicationBar), typeof(BlockMainForm),
		typeof(Utils.WinFormsTester.BlockSelectorStylesScreen), typeof(Utils.WinFormsTester.ExplorerBarDesignerScreen),
		typeof(Utils.WinFormsTester.ExplorerBarThemeExportForm)
	];

	[TestCaseSource(nameof(DesignedTypes))]
	public void LoadsAsTheRootOfADesignSurface(Type type) {
		Assert.That(Sphere10Framework.Instance.IsStarted, Is.False, "A design surface has no running framework.");
		using var surface = new DesignSurface();
		surface.BeginLoad(type);
		Assert.That(surface.LoadErrors.Cast<object>().Select(error => error.ToString()), Is.Empty);
		Assert.That(surface.View, Is.Not.Null);
		Assert.That(((IDesignerHost)surface.GetService(typeof(IDesignerHost))).RootComponent, Is.InstanceOf(type));
	}

	[TestCaseSource(nameof(DesignedTypes))]
	public void TheDesignerFileHasTheGeneratedShape(Type type) {
		var source = File.ReadAllText(DesignerFile(type));
		Assert.That(source, Does.Match(@"private System\.ComponentModel\.IContainer components = null;"));
		Assert.That(source, Does.Match(@"protected override void Dispose\(bool disposing\)"));
		Assert.That(source, Does.Match(@"#region (Windows Form|Component) Designer generated code"));
		var start = source.IndexOf("private void InitializeComponent()", StringComparison.Ordinal);
		Assert.That(start, Is.GreaterThanOrEqualTo(0));
		var body = source[start..source.IndexOf("#endregion", start, StringComparison.Ordinal)];
		foreach (Match assignment in Regex.Matches(body, @"^\s*(?:this\.)?(?<field>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*new\s", RegexOptions.Multiline)) {
			var field = type.GetField(assignment.Groups["field"].Value, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
			// The designer's components container is the one non-component it assigns to a field.
			if (field == null || field.Name == "components")
				continue; // a local the designer declared
			Assert.That(typeof(IComponent).IsAssignableFrom(field.FieldType), Is.True, $"{type.Name}.{field.Name} is a {field.FieldType.Name}; InitializeComponent may assign only components to fields.");
		}
	}

	// The designer file beside the type's source in Sphere10.Framework.Windows.Forms or the WinForms tester, found from this file's own
	// path, since test binaries can be built outside the checkout. Older designer files are named *.designer.cs.
	private static string DesignerFile(Type type, [CallerFilePath] string thisFile = "") {
		var root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", ".."));
		var projects = new[] { Path.Combine(root, "src", "Sphere10.Framework.Windows.Forms"), Path.Combine(root, "utils", "Sphere10.Framework.Utils.WinFormsTester") };
		var matches = projects.SelectMany(project => Directory.GetFiles(project, "*.cs", SearchOption.AllDirectories))
			.Where(path => string.Equals(Path.GetFileName(path), type.Name + ".Designer.cs", StringComparison.OrdinalIgnoreCase)).ToArray();
		Assert.That(matches, Has.Length.EqualTo(1), $"{type.Name}.Designer.cs");
		return matches[0];
	}
}

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using FormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ClipboardRuntimeTests {
	[TestCase("System.Reflection.Metadata")]
	[TestCase("System.Collections.Immutable")]
	[TestCase("System.Formats.Nrbf")]
	public void DrawingRuntimeSerializationDependenciesCanBeLoaded(string dependencyName) {
		var coreName = typeof(Bitmap).Assembly.GetReferencedAssemblies().Single(assembly => assembly.Name == "System.Private.Windows.Core");
		var core = Assembly.Load(coreName);
		var dependency = core.GetReferencedAssemblies().Single(assembly => assembly.Name == dependencyName);
		var resolved = Assembly.Load(dependency);
		Assert.That(resolved.GetName().Version, Is.GreaterThanOrEqualTo(dependency.Version));
	}

	[Test]
	public void UnicodeTextRoundTripsWithTheResolvedDrawingRuntime() => RunWithMessageLoop(() => {
		TestContext.WriteLine($"Drawing runtime: {typeof(Bitmap).Assembly.FullName}");
		using var bitmap = new Bitmap(2, 2);
		var previous = Clipboard.GetDataObject();
		try {
			var text = $"Process clipboard regression {Guid.NewGuid():N}\r\nGreek Ω; Han 漢; emoji 😀";
			Clipboard.SetText(text);
			Assert.That(Clipboard.GetText(TextDataFormat.UnicodeText), Is.EqualTo(text));
		} finally {
			if (previous == null)
				Clipboard.Clear();
			else
				Clipboard.SetDataObject(previous, true);
		}
	});

	private static void RunWithMessageLoop(Action test) {
		Exception failure = null;
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		owner.Shown += (_, _) => {
			try {
				test();
			} catch (Exception exception) {
				failure = exception;
			} finally {
				owner.Close();
			}
		};
		FormsApplication.Run(owner);
		if (failure != null)
			ExceptionDispatchInfo.Capture(failure).Throw();
	}
}
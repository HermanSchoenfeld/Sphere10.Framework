// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Threading;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture, NonParallelizable]
public class ShellLinkTests {
	[Test, Apartment(ApartmentState.STA)]
	public void UnicodeShortcutRoundTripsAndResolvesThroughGeneratedComInterfaces() {
		var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ShellLink-" + Guid.NewGuid().ToString("N"))).FullName;
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var path = Path.Combine(directory, "測試 shortcut.lnk");
		var target = Path.Combine(directory, "target.txt");
		File.WriteAllText(target, "Shortcut target");
		using (var link = new ShellLink()) {
			link.Target = target;
			link.Description = "Unicode description 測試";
			link.Arguments = "--argument Ω";
			link.WorkingDirectory = directory;
			link.DisplayMode = ShellLink.LinkDisplayMode.Minimized;
			link.Save(path);
		}
		using var reopened = new ShellLink();
		reopened.LoadWithoutResolve(path);
		Assert.That(reopened.RawTarget, Is.EqualTo(target).IgnoreCase);
		Assert.That(reopened.Description, Is.EqualTo("Unicode description 測試"));
		Assert.That(reopened.Arguments, Is.EqualTo("--argument Ω"));
		Assert.That(reopened.WorkingDirectory, Is.EqualTo(directory));
		Assert.That(reopened.DisplayMode, Is.EqualTo(ShellLink.LinkDisplayMode.Minimized));
		reopened.Open(path);
		Assert.That(reopened.Target, Is.EqualTo(target).IgnoreCase);
	}
}

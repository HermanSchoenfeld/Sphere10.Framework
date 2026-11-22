// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>
/// The update notice's content and layout for each channel, prepared as the notice prepares itself before it is shown but without
/// showing it: a release-server update shows its release notes and download link, and an update without them, such as the Windows
/// Store's, gives them no space. No window is shown.
/// </summary>
[TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
public class ProductUpdateDialogLayoutTests {

	[Test]
	public void AnUpdateWithoutReleaseNotesOrDownloadLinkGivesThemNoSpace() {
		using var release = PreparedNotice(new ReleaseServerProductUpdate {
			Version = ApplicationVersion.Parse("2.0.0.0"),
			DownloadUrl = "https://updates.example.com/files/setup.exe",
			ReleaseNotes = "Faster widgets."
		});
		using var store = PreparedNotice(new ProductUpdate { Version = ApplicationVersion.Parse("2.0.0.0"), IsMandatory = true });

		// Both notices name the product and the two versions.
		foreach (var notice in new[] { release, store }) {
			Assert.That(Find<Label>(notice, "_messageLabel").Text, Is.EqualTo("A new version of Example Product is available."));
			Assert.That(Find<Label>(notice, "_currentVersionLabel").Text, Is.EqualTo("1.0.0.0"));
			Assert.That(Find<Label>(notice, "_newVersionLabel").Text, Is.EqualTo("2.0.0.0"));
		}

		// The release-server notice places its notes and link below the versions.
		var versionsBottom = Find<Label>(release, "_newVersionLabel").Bottom;
		var notes = Find<GroupBox>(release, "_releaseNotesGroupBox");
		var link = Find<LinkLabel>(release, "_downloadLinkLabel");
		Assert.That(Find<TextBox>(release, "_releaseNotesTextBox").Text, Is.EqualTo("Faster widgets."));
		Assert.That(link.Text, Is.EqualTo("https://updates.example.com/files/setup.exe"));
		Assert.That(notes.Top, Is.GreaterThan(versionsBottom));
		Assert.That(link.Top, Is.GreaterThan(notes.Bottom));
		Assert.That(Find<ProgressBar>(release, "_progressBar").Top, Is.GreaterThan(link.Bottom));

		// The Store's notice has neither, so its progress follows the versions and the notice is shorter by their height.
		var storeProgress = Find<ProgressBar>(store, "_progressBar");
		Assert.That(storeProgress.Top - Find<Label>(store, "_newVersionLabel").Bottom, Is.EqualTo(Find<ProgressBar>(release, "_progressBar").Top - link.Bottom), "No empty box or link row is left.");
		Assert.That(release.ClientSize.Height - store.ClientSize.Height, Is.EqualTo(Find<ProgressBar>(release, "_progressBar").Top - storeProgress.Top));
		Assert.That(release.ClientSize.Height, Is.GreaterThan(store.ClientSize.Height + notes.Height));
		Assert.That(Find<Button>(store, "_downloadAndInstallButton").Text, Is.EqualTo("&Download And Install"), "Both channels offer the usual actions.");
		Assert.That(Find<Button>(store, "_laterButton").Text, Is.EqualTo("&Later"));
	}

	private static PreparedDialog PreparedNotice(ProductUpdate update) {
		var dialog = new PreparedDialog {
			Updater = new ReleaseServerProductUpdater(new NoOpProductUpdateService()),
			AvailableUpdate = update,
			Product = new ProductInformation { ProductName = "Example Product", ProductVersion = ApplicationVersion.Parse("1.0.0.0") }
		};
		dialog.Prepare();
		return dialog;
	}

	private static T Find<T>(Form dialog, string name)
		where T : Control
		=> (T)dialog.Controls.Find(name, true).Single();

	#region Inner Types

	/// <summary>Runs the notice's load preparation, which sets its content and lays it out, without showing it.</summary>
	private sealed class PreparedDialog : ProductUpdateDialog {
		public void Prepare() => OnLoad(EventArgs.Empty);
	}

	#endregion
}

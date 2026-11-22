// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>The main window shows its loading indicator in the status bar, and the screen host shows it while a screen opens or closes.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class MainFormLoadingIndicatorTests {
	private bool _startedFramework;

	[OneTimeSetUp]
	public void StartFramework() {
		_startedFramework = !Sphere10Framework.Instance.IsStarted;
		if (_startedFramework) Sphere10Framework.Instance.Build().Start();
	}

	[OneTimeTearDown]
	public void EndFramework() {
		if (_startedFramework) Sphere10Framework.Instance.EndFramework();
	}

	[Test]
	public void TheStatusBarShowsTheIndicatorAndItsMessageWhileItIsOn() {
		using var form = CreateForm(out var block);
		using var ownedBlock = block;
		var statusStrip = form.Controls.OfType<StatusStrip>().Single();
		var circle = (LoadingCircleToolStripMenuItem)statusStrip.Items["_loadingCircle"];
		var message = statusStrip.Items["_loadingLabel"];
		Assert.That(circle.Available || message.Available, Is.False, "The indicator is hidden while the application is idle.");
		using (form.LoadingIndicator.Show("Reading Services…")) {
			Assert.Multiple(() => {
				Assert.That(circle.Available, Is.True);
				Assert.That(circle.LoadingCircleControl.Active, Is.True, "The circle turns while the indicator is on.");
				Assert.That(message.Available, Is.True);
				Assert.That(message.Text, Is.EqualTo("Reading Services…"));
			});
		}
		Assert.Multiple(() => {
			Assert.That(circle.Available, Is.False);
			Assert.That(circle.LoadingCircleControl.Active, Is.False);
			Assert.That(message.Available, Is.False);
		});
	}

	[Test]
	public async Task ChangesMadeOnAWorkerArePresentedOnTheUIThread() {
		using var form = CreateForm(out var block);
		using var ownedBlock = block;
		var message = form.Controls.OfType<StatusStrip>().Single().Items["_loadingLabel"];
		var shown = await Task.Run(() => form.LoadingIndicator.Show("Saving…"));
		for (var waited = 0; message.Text != "Saving…" && waited < 5000; waited += 10) {
			System.Windows.Forms.Application.DoEvents();
			await Task.Delay(10);
		}
		Assert.That(message.Text, Is.EqualTo("Saving…"));
		shown.Dispose();
	}

	[Test]
	public async Task TheScreenHostShowsTheIndicatorWhileAScreenOpensAndCloses() {
		using var form = CreateForm(out var block);
		using var ownedBlock = block;
		var messages = new List<string>();
		form.LoadingIndicator.Changed += () => messages.Add(form.LoadingIndicator.Message);
		var screen = await form.ScreenHost.ActivateScreenAsync(block, typeof(OtherScreen));
		Assert.That(messages, Is.EqualTo(new[] { "Opening Other…", null }));
		messages.Clear();
		Assert.That(await form.ScreenHost.CloseScreenAsync(screen), Is.True);
		Assert.That(messages, Is.EqualTo(new[] { "Closing Other…", null }));
		Assert.That(form.LoadingIndicator.IsActive, Is.False);
	}

	private static BlockMainForm CreateForm(out ApplicationBlock block) {
		var form = new BlockMainForm { ScreenMode = ScreenMode.MultiView, StartPosition = FormStartPosition.Manual, Location = new Point(-19000, -19000), Size = new Size(800, 600), ShowInTaskbar = false };
		block = new ApplicationBlockBuilder()
			.WithName("Loading")
			.AddScreen<StartScreen>(screen => screen.WithTitle("Start").WithActivationMode(ScreenActivationMode.SingleInstance))
			.AddScreen<OtherScreen>(screen => screen.WithTitle("Other").WithActivationMode(ScreenActivationMode.SingleInstance))
			.WithDefaultScreen<StartScreen>()
			.Build();
		form.RegisterBlock(block);
		// A window handle lets a worker's change be posted to the form; showing it is not needed, and would run the application's load events.
		_ = form.Handle;
		return form;
	}

	#region Inner Types

	public sealed class StartScreen : ApplicationScreen { }

	public sealed class OtherScreen : ApplicationScreen { }

	#endregion
}

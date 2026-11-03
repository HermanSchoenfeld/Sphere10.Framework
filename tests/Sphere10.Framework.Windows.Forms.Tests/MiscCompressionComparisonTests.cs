// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Utils.WinFormsTester;
using FormsUI = global::Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Exercises the utility comparison through its native button, cancellation and actual screen lifetime.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class MiscCompressionComparisonTests {

	[Test]
	public void ComparisonFinishesFourVerifiedSamplesAndCanRunAgain() {
		RunWithMessageLoop(async owner => {
			using var screen = new MiscTestScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			Find<TabControl>(screen, "_tools").SelectedIndex = 1;
			await Task.Yield();
			var button = Find<Button>(screen, "_compressTestButton");
			var output = Find<TextBox>(screen, "_outputTextBox");

			button.PerformClick();
			Assert.That(button.Enabled, Is.True, "The running comparison must remain cancellable.");
			Assert.That(button.Text, Is.EqualTo("Cancel comparison"));
			await WaitUntilAsync(() => CompressionCancellation(screen) == null);
			var results = output.Lines.Where(line => line.StartsWith("Original: ", StringComparison.Ordinal)).ToArray();
			Assert.That(results, Has.Length.EqualTo(4));
			Assert.That(results.All(line => line.Contains("Round trip: True", StringComparison.Ordinal)), Is.True, output.Text);
			Assert.That(button.Text, Is.EqualTo("Compression comparison"));
			Assert.That(button.Enabled, Is.True);

			// Completion restores the original action rather than leaving the button disabled.
			button.PerformClick();
			await WaitUntilAsync(() => CompressionCancellation(screen) == null);
			Assert.That(output.Lines.Count(line => line.StartsWith("Original: ", StringComparison.Ordinal)), Is.EqualTo(8));
			Assert.That(output.Text, Does.Not.Contain("failed").IgnoreCase);
		});
	}

	[Test]
	public void CancelStopsTheCurrentComparisonAndAllowsARetry() {
		RunWithMessageLoop(async owner => {
			using var screen = new MiscTestScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			Find<TabControl>(screen, "_tools").SelectedIndex = 1;
			await Task.Yield();
			var button = Find<Button>(screen, "_compressTestButton");
			var output = Find<TextBox>(screen, "_outputTextBox");

			// Both clicks occur in one dispatcher callback, before completion can restore the action.
			button.PerformClick();
			var cancellation = CompressionCancellation(screen);
			Assert.That(cancellation, Is.Not.Null);
			Assert.That(button.Enabled, Is.True);
			button.PerformClick();
			Assert.That(cancellation.IsCancellationRequested, Is.True);
			Assert.That(CompressionCancellation(screen), Is.SameAs(cancellation), "Cancel must not start a competing worker.");
			await WaitUntilAsync(() => CompressionCancellation(screen) == null);
			Assert.That(output.Text, Does.Contain("cancel").IgnoreCase);
			Assert.That(button.Text, Is.EqualTo("Compression comparison"));

			output.Clear();
			button.PerformClick();
			await WaitUntilAsync(() => CompressionCancellation(screen) == null);
			Assert.That(output.Lines.Count(line => line.StartsWith("Original: ", StringComparison.Ordinal)), Is.EqualTo(4));
			Assert.That(output.Text, Does.Not.Contain("failed").IgnoreCase);
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ClosingOrDisposingAnActiveScreenCancelsWithoutWaitingForItsWorker(bool closeThroughHost) {
		RunWithMessageLoop(async owner => {
			using var host = new FormsUI.ApplicationScreenHost { Dock = DockStyle.Fill, ScreenMode = ScreenMode.MultiView };
			using var block = new ApplicationBlockBuilder()
				.WithName("Compression comparison")
				.AddScreen<MiscTestScreen>("Utility comparison")
				.Build();
			owner.Controls.Add(host);
			var screen = (MiscTestScreen)host.ActivateScreen(block, typeof(MiscTestScreen));
			Find<TabControl>(screen, "_tools").SelectedIndex = 1;
			await Task.Yield();

			Find<Button>(screen, "_compressTestButton").PerformClick();
			var cancellation = CompressionCancellation(screen);
			Assert.That(cancellation, Is.Not.Null);
			Assert.That(((FormsUI.IApplicationScreen)screen).CanDeactivate(), Is.True);
			Assert.That(cancellation.IsCancellationRequested, Is.False, "Preflight must not tear down a still-running screen.");

			// Keep the dispatcher alive after destruction to observe the actual worker's cleanup.
			if (closeThroughHost)
				Assert.That(host.CloseScreen(screen), Is.True);
			else
				screen.Dispose();
			Assert.That(screen.IsDisposed, Is.True);
			Assert.That(cancellation.IsCancellationRequested, Is.True);
			await WaitUntilAsync(() => CompressionCancellation(screen) == null);
		});
	}

	// Named controls exercise the real Designer surface without adding production testing accessors.
	private static T Find<T>(Control owner, string name)
		where T : Control {
		return (T)owner.Controls.Find(name, true).Single();
	}

	// Join the owned operation after disposal when the button can no longer report its completion.
	private static CancellationTokenSource CompressionCancellation(MiscTestScreen screen) {
		return (CancellationTokenSource)typeof(MiscTestScreen)
			.GetField("_compressionCancellation", BindingFlags.Instance | BindingFlags.NonPublic)
			.GetValue(screen);
	}

	// Await semantic completion while continuing to dispatch native close and worker continuations.
	private static async Task WaitUntilAsync(Func<bool> predicate) {
		var elapsed = Stopwatch.StartNew();
		while (!predicate() && elapsed.Elapsed < TimeSpan.FromSeconds(8))
			await Task.Delay(10);
		Assert.That(predicate(), Is.True, "The compression comparison did not finish or cancel.");
	}

	// Each scenario owns its real native dispatcher and restores the NUnit synchronization context.
	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form {
			ShowInTaskbar = false,
			StartPosition = FormStartPosition.Manual,
			Location = new Point(-20000, -20000),
			ClientSize = new Size(1100, 800)
		};
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The compression comparison native test timed out.");
			System.Windows.Forms.Application.ExitThread();
		};
		owner.Shown += async (_, _) => {
			try {
				await test(owner);
				completed = true;
			} catch (Exception error) {
				failure = error;
			} finally {
				watchdog.Stop();
				owner.Close();
			}
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True, "The native test must finish before its dispatcher closes.");
	}
}

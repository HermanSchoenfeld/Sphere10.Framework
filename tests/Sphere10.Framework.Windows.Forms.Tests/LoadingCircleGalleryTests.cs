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
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Utils.WinFormsTester;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Exercises the loading gallery's live bindings, cancellable examples and screen lifetime.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class LoadingCircleGalleryTests {

	[Test]
	public void GalleryRendersAllStylesAndKeepsItsActionsVisibleWhenResized() {
		RunWithMessageLoop(async owner => {
			using var screen = new LoadingCircleTestScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();

			var circles = new[] {
				Find<LoadingCircle>(screen, "loadingSpokes"),
				Find<LoadingCircle>(screen, "loadingDots"),
				Find<LoadingCircle>(screen, "loadingArc"),
				Find<LoadingCircle>(screen, "loadingPulse")
			};
			Assert.That(circles.Select(circle => circle.AnimationStyle), Is.EqualTo(Enum.GetValues<LoadingCircleAnimationStyle>()));
			Assert.That(circles.All(circle => circle.Visible && circle.Active), Is.True);
			Assert.That(circles.Select(circle => circle.Color.ToArgb()).Distinct().Count(), Is.EqualTo(4));
			foreach (var circle in circles) {
				using var image = new Bitmap(circle.Width, circle.Height);
				circle.DrawToBitmap(image, circle.ClientRectangle);
				Assert.That(CountColouredPixels(image), Is.GreaterThan(20), circle.AnimationStyle + " must visibly render in its gallery card.");
			}
			Assert.That(Find<DataGridView>(screen, "loadingSampleGrid").Rows.Count, Is.EqualTo(4));
			SaveScreenshot(screen, "loading-gallery-normal.png");

			owner.ClientSize = new Size(820, 670);
			await Task.Yield();
			SaveScreenshot(screen, "loading-gallery-resized.png");
			foreach (var name in new[] { "pauseLoadingPreview", "resetLoadingPreview", "runLoadingOverlay", "runLoadingInline", "cancelLoadingOperation" }) {
				var button = Find<Button>(screen, name);
				var bounds = screen.RectangleToClient(button.RectangleToScreen(button.ClientRectangle));
				Assert.That(screen.ClientRectangle.Contains(bounds), Is.True, name + " must remain on the resized screen.");
				Assert.That(button.Parent.ClientRectangle.Contains(button.Bounds), Is.True, name + $" must remain inside its actual command row: {button.Bounds} in {button.Parent.ClientRectangle}.");
			}
		});
	}

	[Test]
	public void NativeAppearanceChoicesUpdateThePreviewAndPauseResetControls() {
		RunWithMessageLoop(async owner => {
			using var screen = new LoadingCircleTestScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();

			Find<ComboBox>(screen, "loadingStyle").SelectedIndex = 2;
			Find<ComboBox>(screen, "loadingColour").SelectedIndex = 3;
			Find<NumericUpDown>(screen, "loadingInterval").Value = 150;
			Find<NumericUpDown>(screen, "loadingRadius").Value = 34;
			Find<NumericUpDown>(screen, "loadingThickness").Value = 6;
			var preview = Find<LoadingCircle>(screen, "loadingPreview");
			Assert.That(preview.AnimationStyle, Is.EqualTo(LoadingCircleAnimationStyle.Arc));
			Assert.That(preview.Color, Is.EqualTo(Color.FromArgb(211, 125, 28)));
			Assert.That(preview.RotationSpeed, Is.EqualTo(150));
			Assert.That(preview.OuterCircleRadius, Is.EqualTo(34));
			Assert.That(preview.SpokeThickness, Is.EqualTo(6));
			Assert.That(Find<Label>(screen, "loadingPreviewSummary").Text, Does.Contain("Arc").And.Contain("150 ms").And.Contain("68 px"));

			var pause = Find<Button>(screen, "pauseLoadingPreview");
			pause.PerformClick();
			Assert.That(preview.Active, Is.False);
			Assert.That(pause.Text, Is.EqualTo("Resume preview"));
			Assert.That(Find<LoadingCircle>(screen, "loadingSpokes").Active, Is.True);
			pause.PerformClick();
			Assert.That(preview.Active, Is.True);
			Find<Button>(screen, "resetLoadingPreview").PerformClick();
			Assert.That(preview.AnimationStyle, Is.EqualTo(LoadingCircleAnimationStyle.Spokes));
			Assert.That(preview.RotationSpeed, Is.EqualTo(80));
			Assert.That(preview.OuterCircleRadius, Is.EqualTo(26));
			Assert.That(preview.SpokeThickness, Is.EqualTo(4));
		});
	}

	[TestCase(true)]
	[TestCase(false)]
	public void AnimationExamplesCompleteAndReleaseTheirActualIndicators(bool overlay) {
		RunWithMessageLoop(async owner => {
			using var screen = new LoadingCircleTestScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			var workspace = Find<Panel>(screen, "loadingWorkArea");
			var grid = Find<DataGridView>(screen, "loadingSampleGrid");
			var inline = Find<LoadingCircle>(screen, "loadingInlineIndicator");
			var originalChildren = workspace.Controls.Count;
			Find<Button>(screen, overlay ? "runLoadingOverlay" : "runLoadingInline").PerformClick();
			var owned = workspace.Controls.OfType<LoadingCircle>().SingleOrDefault();
			Assert.That(Find<Button>(screen, "cancelLoadingOperation").Enabled, Is.True);
			Assert.That(Find<Button>(screen, "runLoadingOverlay").Enabled, Is.False);
			Assert.That(grid.Rows.Cast<DataGridViewRow>().Select(row => row.Cells[2].Value), Is.All.EqualTo("Working"));
			if (overlay) {
				Assert.That(owned, Is.Not.Null);
				Assert.That(owned.Active, Is.True);
				Assert.That(grid.Enabled, Is.False);
			} else {
				Assert.That(inline.Visible && inline.Active, Is.True);
				Assert.That(grid.Enabled, Is.True);
			}

			await AwaitIdle(screen);
			Assert.That(grid.Rows.Cast<DataGridViewRow>().Select(row => row.Cells[2].Value), Is.All.EqualTo("Completed"));
			Assert.That(grid.Rows.Cast<DataGridViewRow>().Select(row => row.Cells[3].Value), Is.All.Not.EqualTo("—"));
			Assert.That(workspace.Controls.Count, Is.EqualTo(originalChildren));
			Assert.That(grid.Enabled, Is.True);
			Assert.That(inline.Active || inline.Visible, Is.False);
			Assert.That(inline.IsDisposed, Is.False, "The design-surface indicator is borrowed by the scope.");
			if (overlay)
				Assert.That(owned.IsDisposed, Is.True);
			Assert.That(Find<Label>(screen, "loadingOperationStatus").Text, Does.StartWith("Completed."));
		});
	}

	[TestCase(true)]
	[TestCase(false)]
	public void CancellationRestoresTheWorkspaceWithoutCompletingRows(bool overlay) {
		RunWithMessageLoop(async owner => {
			using var screen = new LoadingCircleTestScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			Find<Button>(screen, overlay ? "runLoadingOverlay" : "runLoadingInline").PerformClick();
			Find<Button>(screen, "cancelLoadingOperation").PerformClick();
			await AwaitIdle(screen);

			var grid = Find<DataGridView>(screen, "loadingSampleGrid");
			Assert.That(grid.Enabled, Is.True);
			Assert.That(grid.Rows.Cast<DataGridViewRow>().Select(row => row.Cells[2].Value), Is.All.EqualTo("Cancelled"));
			Assert.That(grid.Rows.Cast<DataGridViewRow>().Select(row => row.Cells[3].Value), Is.All.EqualTo("—"));
			Assert.That(Find<Label>(screen, "loadingOperationStatus").Text, Does.StartWith("Cancelled."));
			Assert.That(Find<Panel>(screen, "loadingWorkArea").Controls.OfType<LoadingCircle>(), Is.Empty);
		});
	}

	[TestCase(true)]
	[TestCase(false)]
	public void DisposingAnActiveExampleCancelsItsContinuationAndDisposesTheIndicators(bool overlay) {
		RunWithMessageLoop(async owner => {
			var screen = new LoadingCircleTestScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			Find<Button>(screen, overlay ? "runLoadingOverlay" : "runLoadingInline").PerformClick();
			var workspace = Find<Panel>(screen, "loadingWorkArea");
			var inline = Find<LoadingCircle>(screen, "loadingInlineIndicator");
			var owned = workspace.Controls.OfType<LoadingCircle>().SingleOrDefault();
			var operation = typeof(LoadingCircleTestScreen).GetField("_operationCancellation", BindingFlags.Instance | BindingFlags.NonPublic);
			var cancellation = (CancellationTokenSource)operation.GetValue(screen);
			Assert.That(cancellation, Is.Not.Null);
			screen.Dispose();
			Assert.That(cancellation.IsCancellationRequested, Is.True, "Screen destruction must cancel the pending operation before its ordinary delay completes.");

			// Join the actual async finally block while its owning dispatcher remains alive.
			var timeout = Stopwatch.StartNew();
			while (operation.GetValue(screen) != null && timeout.Elapsed < TimeSpan.FromSeconds(5))
				await Task.Delay(10);
			Assert.That(operation.GetValue(screen), Is.Null, "Disposal must leave no pending demonstration continuation.");
			Assert.That(screen.IsDisposed, Is.True);
			Assert.That(inline.IsDisposed, Is.True);
			if (overlay)
				Assert.That(owned.IsDisposed, Is.True);
		});
	}

	// Await the real operation's public native availability state on its owning dispatcher.
	private static async Task AwaitIdle(Control screen) {
		var timeout = Stopwatch.StartNew();
		while (Find<Button>(screen, "cancelLoadingOperation").Enabled && timeout.Elapsed < TimeSpan.FromSeconds(5))
			await Task.Delay(15);
		Assert.That(Find<Button>(screen, "cancelLoadingOperation").Enabled, Is.False);
		Assert.That(Find<Button>(screen, "runLoadingOverlay").Enabled, Is.True);
	}

	// Read concrete design-surface controls rather than creating production test-only accessors.
	private static T Find<T>(Control parent, string name)
		where T : Control {
		return (T)parent.Controls.Find(name, true).Single();
	}

	// Exclude neutral text and borders when checking that a coloured animation was actually drawn.
	private static int CountColouredPixels(Bitmap image) {
		var count = 0;
		for (var row = 0; row < image.Height; row++) {
			for (var column = 0; column < image.Width; column++) {
				var pixel = image.GetPixel(column, row);
				if (Math.Max(pixel.R, Math.Max(pixel.G, pixel.B)) - Math.Min(pixel.R, Math.Min(pixel.G, pixel.B)) > 24)
					count++;
			}
		}
		return count;
	}

	// Retain reviewable normal and compact views alongside the C-backed test outputs.
	private static void SaveScreenshot(Control screen, string name) {
		using var image = new Bitmap(screen.Width, screen.Height);
		screen.DrawToBitmap(image, screen.ClientRectangle);
		var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, name);
		image.Save(path, ImageFormat.Png);
		TestContext.AddTestAttachment(path);
	}

	// Native tests run in one owned dispatcher and restore the caller's synchronization context.
	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previousContext = SynchronizationContext.Current;
		using var restoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		using var context = new WindowsFormsSynchronizationContext();
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(1100, 820) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The loading gallery test did not complete.");
			System.Windows.Forms.Application.ExitThread();
		};
		owner.Shown += async (_, _) => {
			try {
				await test(owner);
				completed = true;
			} catch (Exception error) {
				failure = error;
			} finally {
				owner.Close();
			}
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}
}

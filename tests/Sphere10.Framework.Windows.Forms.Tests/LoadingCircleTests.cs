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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class LoadingCircleTests {
	[TestCase(LoadingCircleAnimationStyle.Spokes)]
	[TestCase(LoadingCircleAnimationStyle.Dots)]
	[TestCase(LoadingCircleAnimationStyle.Arc)]
	[TestCase(LoadingCircleAnimationStyle.Pulse)]
	public void EveryStyleDrawsItsConfiguredColourAndPreservesAlpha(LoadingCircleAnimationStyle style) {
		using var owner = new Form { BackColor = Color.White };
		// An explicit opaque native surface makes compositing measurable without an uncreated parent's transparency.
		using var circle = new LoadingCircle { AnimationStyle = style, Size = new Size(64, 64), BackColor = Color.White, Color = Color.Crimson };
		owner.Controls.Add(circle);
		using var opaque = Draw(circle);
		circle.Color = Color.FromArgb(96, Color.Crimson);
		using var translucent = Draw(circle);
		circle.Color = Color.Transparent;
		using var transparent = Draw(circle);
		Assert.Multiple(() => {
			Assert.That(CountColouredPixels(opaque), Is.GreaterThan(20), "The style must render a visible coloured indicator.");
			Assert.That(MinimumGreen(translucent), Is.GreaterThan(MinimumGreen(opaque) + 70), "Drawing must retain the caller's alpha rather than replacing it.");
			Assert.That(CountColouredPixels(transparent), Is.Zero, "A fully transparent foreground leaves the normal parent background intact.");
		});
	}

	[TestCase(LoadingCircleAnimationStyle.Spokes)]
	[TestCase(LoadingCircleAnimationStyle.Dots)]
	[TestCase(LoadingCircleAnimationStyle.Arc)]
	[TestCase(LoadingCircleAnimationStyle.Pulse)]
	public void EveryStyleFitsSmallAndNonSquareClientAreas(LoadingCircleAnimationStyle style) {
		using var circle = new LoadingCircle { AnimationStyle = style, Size = new Size(8, 19), BackColor = Color.White, Color = Color.MidnightBlue };
		circle.SetCircleAppearance(12, 8, 20, 32);
		using var image = Draw(circle);
		Assert.That(CountColouredPixels(image), Is.GreaterThan(0));
		Assert.That(image.GetPixel(0, 0).ToArgb(), Is.EqualTo(Color.White.ToArgb()), "Oversized logical geometry must fit inside the client rather than clip against its corner.");
		circle.Size = Size.Empty;
		Assert.That(() => circle.Refresh(), Throws.Nothing);
	}

	[TestCase(LoadingCircle.StylePresets.MacOSX, 12, 2, 5, 11)]
	[TestCase(LoadingCircle.StylePresets.Firefox, 9, 4, 6, 7)]
	[TestCase(LoadingCircle.StylePresets.IE7, 24, 4, 8, 9)]
	[TestCase(LoadingCircle.StylePresets.Custom, 10, 4, 8, 10)]
	public void ExistingPresetsRetainGeometryAndIndependentStyleAndColour(LoadingCircle.StylePresets preset, int spokes, int thickness, int inner, int outer) {
		using var circle = new LoadingCircle { AnimationStyle = LoadingCircleAnimationStyle.Arc, Color = Color.Teal, StylePreset = preset };
		Assert.Multiple(() => {
			Assert.That(circle.NumberSpoke, Is.EqualTo(spokes));
			Assert.That(circle.SpokeThickness, Is.EqualTo(thickness));
			Assert.That(circle.InnerCircleRadius, Is.EqualTo(inner));
			Assert.That(circle.OuterCircleRadius, Is.EqualTo(outer));
			Assert.That(circle.AnimationStyle, Is.EqualTo(LoadingCircleAnimationStyle.Arc));
			Assert.That(circle.Color, Is.EqualTo(Color.Teal));
		});
	}

	[Test]
	public void InvalidGeometryIsRejectedBeforeChangingACompleteAppearance() {
		using var circle = new LoadingCircle();
		Assert.Multiple(() => {
			Assert.That(() => circle.NumberSpoke = 0, Throws.InstanceOf<ArgumentException>());
			Assert.That(() => circle.SpokeThickness = 0, Throws.InstanceOf<ArgumentException>());
			Assert.That(() => circle.InnerCircleRadius = -1, Throws.InstanceOf<ArgumentException>());
			Assert.That(() => circle.OuterCircleRadius = 0, Throws.InstanceOf<ArgumentException>());
			Assert.That(() => circle.SetCircleAppearance(12, 2, 4, 0), Throws.InstanceOf<ArgumentException>());
			Assert.That(circle.NumberSpoke, Is.EqualTo(10));
			Assert.That(circle.SpokeThickness, Is.EqualTo(4));
			Assert.That(circle.InnerCircleRadius, Is.EqualTo(8));
			Assert.That(circle.OuterCircleRadius, Is.EqualTo(10));
		});
	}

	[Test]
	public void TransparentPaintingDoesNotRenderEveryOverlappingSiblingIntoABitmap() {
		using var owner = new Form { BackColor = Color.Beige };
		using var sibling = new PaintCounter { Bounds = new Rectangle(0, 0, 80, 80) };
		using var circle = new LoadingCircle { Bounds = sibling.Bounds };
		owner.Controls.Add(sibling);
		owner.Controls.Add(circle);
		circle.BringToFront();
		sibling.PaintCount = 0;
		using var image = Draw(circle);
		Assert.That(sibling.PaintCount, Is.Zero, "Native transparency repaints the parent background, not sibling controls through recursive bitmap snapshots.");
	}

	[Test]
	public void StoppingRestoresOnlyChangedControlsAndKeepsNestedExclusionsUsable() {
		using var owner = new Form();
		using var branch = new Panel();
		using var enabled = new TextBox();
		using var disabled = new TextBox { Enabled = false };
		using var excluded = new Button();
		using var hideStop = new Button { Visible = false };
		using var circle = new LoadingCircle { HideStopControl = hideStop };
		owner.Controls.Add(branch);
		branch.Controls.AddRange(new Control[] { enabled, disabled, excluded, hideStop, circle });

		circle.StartAnimating(owner, excluded);
		Assert.Multiple(() => {
			Assert.That(enabled.Enabled, Is.False);
			Assert.That(disabled.Enabled, Is.False);
			Assert.That(branch.Enabled, Is.True, "An excluded descendant must retain its enabled ancestor path.");
			Assert.That(excluded.Enabled, Is.True);
			Assert.That(hideStop.Enabled, Is.True);
			Assert.That(circle.Enabled, Is.True);
		});

		circle.StopAnimating();
		Assert.Multiple(() => {
			Assert.That(enabled.Enabled, Is.True);
			Assert.That(disabled.Enabled, Is.False, "An input disabled before the operation must remain disabled.");
			Assert.That(excluded.Enabled, Is.True);
			Assert.That(circle.Active, Is.False);
		});
	}

	[Test]
	public void DisposingTheIndicatorRestoresBorrowedControlsWithoutDisposingThem() {
		using var owner = new Panel();
		using var input = new TextBox();
		var circle = new LoadingCircle();
		owner.Controls.AddRange(new Control[] { input, circle });
		circle.StartAnimating(owner);
		Assert.That(input.Enabled, Is.False);
		circle.Dispose();
		circle.Dispose();
		Assert.Multiple(() => {
			Assert.That(input.Enabled, Is.True);
			Assert.That(input.IsDisposed, Is.False);
			Assert.That(owner.IsDisposed, Is.False);
			Assert.That(circle.Active, Is.False);
		});
	}

	[Test]
	public void OwnedOverlayScopeRemovesAndDisposesItsIndicatorWhileKeepingPriorInputState() {
		using var owner = new Panel { Size = new Size(240, 120) };
		using var disabled = new TextBox { Enabled = false };
		owner.Controls.Add(disabled);
		LoadingCircle overlay;
		using (LoadingCircle.EnterAnimationScope(owner, disableControls: true)) {
			overlay = owner.Controls.OfType<LoadingCircle>().Single();
			Assert.That(overlay.Active, Is.True);
			Assert.That(overlay.Size.Width, Is.EqualTo(overlay.Size.Height));
		}
		Assert.Multiple(() => {
			Assert.That(overlay.IsDisposed, Is.True);
			Assert.That(owner.Controls.Contains(overlay), Is.False);
			Assert.That(owner.IsDisposed, Is.False);
			Assert.That(disabled.Enabled, Is.False);
		});
	}

	[Test]
	public void FailedOverlayEntryDisposesTheOwnedIndicatorAndRestoresBorrowedInputs() {
		using var owner = new Panel { Size = new Size(240, 120) };
		using var input = new TextBox();
		owner.Controls.Add(input);
		LoadingCircle overlay = null;
		owner.ControlAdded += (_, e) => {
			if (e.Control is LoadingCircle circle)
				overlay = circle;
		};
		input.EnabledChanged += (_, _) => {
			if (!input.Enabled)
				throw new InvalidOperationException("The consumer rejected the disabled state.");
		};

		Assert.That(() => LoadingCircle.EnterAnimationScope(owner, disableControls: true), Throws.TypeOf<InvalidOperationException>());
		Assert.Multiple(() => {
			Assert.That(overlay, Is.Not.Null);
			Assert.That(overlay.IsDisposed, Is.True, "A failed entry cannot leave a timer or native overlay without a returned scope.");
			Assert.That(owner.Controls.OfType<LoadingCircle>(), Is.Empty);
			Assert.That(input.Enabled, Is.True);
			Assert.That(input.IsDisposed, Is.False);
			Assert.That(owner.IsDisposed, Is.False);
		});
	}

	[Test]
	public void OverlayExitRestoresEveryBorrowedInputAndDisposesBeforePropagatingTheFirstError() {
		using var owner = new Panel { Size = new Size(240, 120) };
		using var first = new TextBox();
		using var second = new TextBox();
		using var last = new TextBox();
		owner.Controls.AddRange(new Control[] { first, second, last });
		var scope = LoadingCircle.EnterAnimationScope(owner, disableControls: true);
		var overlay = owner.Controls.OfType<LoadingCircle>().Single();
		var firstError = new InvalidOperationException("The first input rejected restoration.");
		first.EnabledChanged += (_, _) => {
			if (first.Enabled)
				throw firstError;
		};
		second.EnabledChanged += (_, _) => {
			if (second.Enabled)
				throw new InvalidOperationException("A later input also rejected restoration.");
		};

		Tools.Exceptions.ExecuteCapturingException(scope.Dispose, out var error);
		Assert.Multiple(() => {
			Assert.That(error, Is.SameAs(firstError), "Every input is attempted, while the first consumer exception retains its identity.");
			Assert.That(error?.StackTrace, Does.Contain(nameof(OverlayExitRestoresEveryBorrowedInputAndDisposesBeforePropagatingTheFirstError)));
			Assert.That(first.Enabled, Is.True);
			Assert.That(second.Enabled, Is.True);
			Assert.That(last.Enabled, Is.True, "An earlier event-handler exception cannot strand later inputs disabled.");
			Assert.That(overlay.IsDisposed, Is.True);
			Assert.That(owner.Controls.OfType<LoadingCircle>(), Is.Empty);
			Assert.That(first.IsDisposed || second.IsDisposed || last.IsDisposed || owner.IsDisposed, Is.False);
		});
	}

	[Test]
	public void BorrowedIndicatorDisposesItsNativeBaseEvenWhenInputRestorationThrows() {
		using var owner = new Panel();
		using var first = new TextBox();
		using var later = new TextBox();
		using var companion = new Button { Visible = false };
		var circle = new LoadingCircle { HideStopControl = companion };
		owner.Controls.AddRange(new Control[] { first, later, companion, circle });
		circle.StartAnimating(owner);
		Assert.That(companion.Visible, Is.True);
		_ = circle.Handle;
		var expected = new InvalidOperationException("The borrowed input rejected restoration.");
		first.EnabledChanged += (_, _) => {
			if (first.Enabled)
				throw expected;
		};

		Tools.Exceptions.ExecuteCapturingException(circle.Dispose, out var error);
		Assert.Multiple(() => {
			Assert.That(error, Is.SameAs(expected));
			Assert.That(first.Enabled, Is.True);
			Assert.That(later.Enabled, Is.True);
			Assert.That(companion.Visible, Is.False, "Companion visibility is cleaned up after a restoration error.");
			Assert.That(circle.Active, Is.False);
			Assert.That(circle.IsDisposed, Is.True);
			Assert.That(circle.IsHandleCreated, Is.False, "The native base must still destroy its handle.");
			Assert.That(owner.Controls.Contains(circle), Is.False);
			Assert.That(first.IsDisposed || later.IsDisposed || companion.IsDisposed || owner.IsDisposed, Is.False);
		});
	}

	[Test]
	public void AnimationScopesTolerateNativeOwnerDisposalAndDoNotDisposeBorrowedIndicators() {
		var owner = new Panel { Size = new Size(120, 80) };
		var overlayScope = LoadingCircle.EnterAnimationScope(owner, disableControls: true);
		owner.Dispose();
		Assert.That(() => overlayScope.Dispose(), Throws.Nothing);

		using var borrowed = new LoadingCircle();
		using (borrowed.BeginAnimationScope()) {
			Assert.That(borrowed.Active, Is.True);
		}
		Assert.That(borrowed.IsDisposed, Is.False);
		var borrowedScope = borrowed.BeginAnimationScope();
		borrowed.Dispose();
		Assert.That(() => borrowedScope.Dispose(), Throws.Nothing);
	}

	[Test]
	public void ToolStripHostUsesBothPreferredDimensionsAndOwnsItsIndicator() {
		using var host = new LoadingCircleToolStripMenuItem();
		var circle = host.LoadingCircleControl;
		circle.SetCircleAppearance(12, 4, 12, 20);
		var preferred = host.GetPreferredSize(new Size(200, 1));
		Assert.That(preferred.Width, Is.EqualTo(preferred.Height));
		Assert.That(preferred.Width, Is.GreaterThanOrEqualTo(44));
		host.Dispose();
		Assert.That(circle.IsDisposed, Is.True);
	}

	[Test]
	public void NativeTimerPausesWhileHiddenAndStopsAfterDisposal() {
		RunWithMessageLoop(async owner => {
			using var circle = new LoadingCircle { RotationSpeed = 15, Active = true };
			Assert.That(circle.IsHandleCreated, Is.False, "Requesting animation must not create a handle.");
			owner.Controls.Add(circle);
			var invalidations = 0;
			circle.Invalidated += (_, _) => invalidations++;
			await WaitUntilAsync(() => invalidations >= 2);

			circle.Visible = false;
			await Task.Delay(40);
			var hiddenCount = invalidations;
			await Task.Delay(100);
			Assert.That(invalidations, Is.EqualTo(hiddenCount), "A hidden indicator must not retain a running animation timer.");
			Assert.That(circle.Active, Is.True, "Visibility pauses the requested animation without changing it.");

			circle.Visible = true;
			var shownCount = invalidations;
			await WaitUntilAsync(() => invalidations >= shownCount + 2);
			circle.Dispose();
			var disposedCount = invalidations;
			await Task.Delay(100);
			Assert.That(invalidations, Is.EqualTo(disposedCount), "Disposal must release native timer delivery.");
		});
	}

	// Render the actual custom-painted native surface, including its normal parent background.
	private static Bitmap Draw(LoadingCircle circle) {
		var image = new Bitmap(circle.Width, circle.Height);
		circle.DrawToBitmap(image, circle.ClientRectangle);
		return image;
	}

	// Non-grey pixels distinguish the configured coloured stroke from the neutral background.
	private static int CountColouredPixels(Bitmap image) {
		var count = 0;
		for (var y = 0; y < image.Height; y++) {
			for (var x = 0; x < image.Width; x++) {
				var color = image.GetPixel(x, y);
				if (Math.Max(color.R, Math.Max(color.G, color.B)) - Math.Min(color.R, Math.Min(color.G, color.B)) > 12)
					count++;
			}
		}
		return count;
	}

	// The red foreground's strongest channel difference reveals whether caller-supplied alpha survived.
	private static int MinimumGreen(Bitmap image) {
		var minimum = 255;
		for (var y = 0; y < image.Height; y++) {
			for (var x = 0; x < image.Width; x++)
				minimum = Math.Min(minimum, image.GetPixel(x, y).G);
		}
		return minimum;
	}

	// Positive timer assertions wait for observable delivery rather than assuming a fixed scheduling delay.
	private static async Task WaitUntilAsync(Func<bool> condition) {
		var stopwatch = Stopwatch.StartNew();
		while (!condition() && stopwatch.Elapsed < TimeSpan.FromSeconds(3))
			await Task.Delay(10);
		Assert.That(condition(), Is.True, "The owning native message loop did not deliver the expected animation frames.");
	}

	// An actual Forms dispatcher keeps timer callbacks and awaited lifecycle operations on their owning thread.
	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 10000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The LoadingCircle lifecycle test timed out.");
			owner.Close();
		};
		owner.Shown += async (_, _) => {
			try {
				await test(owner);
			} catch (Exception error) {
				failure = error;
			} finally {
				owner.Close();
			}
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}

	// A real sibling distinguishes normal native transparency from the removed recursive bitmap workaround.
	private sealed class PaintCounter : Control {
		public int PaintCount;

		protected override void OnPaint(PaintEventArgs e) {
			PaintCount++;
			base.OnPaint(e);
		}
	}
}

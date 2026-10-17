// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ControlRenderingScopeTests {
	[TestCase(false, false)]
	[TestCase(true, false)]
	[TestCase(false, true)]
	[TestCase(true, true)]
	public void ReparentedTreeReceivesOneCompletePaintWhenItsOriginalScopeEnds(bool repaintImmediately, bool disposeOriginalRoot) {
		using var original = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-19000, -19000) };
		using var destination = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-18000, -18000) };
		using var moved = new CountingPanel { Dock = DockStyle.Fill, Padding = new Padding(7) };
		using var child = new CountingPanel { Dock = DockStyle.Fill };
		original.Controls.Add(moved);
		moved.Controls.Add(child);
		original.Show();
		destination.Show();
		System.Windows.Forms.Application.DoEvents();
		using (original.EnterRenderingScope(repaintImmediately)) {
			destination.Controls.Add(moved);
			if (disposeOriginalRoot)
				original.Dispose();
			// Observe the final repaint rather than incidental exposure queued by native reparenting.
			WinAPI.USER32.ValidateRect(new HandleRef(moved, moved.Handle), IntPtr.Zero);
			WinAPI.USER32.ValidateRect(new HandleRef(child, child.Handle), IntPtr.Zero);
			moved.ResetCounts();
			child.ResetCounts();
		}
		Assert.That(moved.PaintCount, Is.EqualTo(repaintImmediately ? 1 : 0), "The released subtree follows the requested immediate/deferred paint policy even after leaving its original root.");
		System.Windows.Forms.Application.DoEvents();
		Assert.Multiple(() => {
			Assert.That(moved.PaintCount, Is.EqualTo(1), "The reparented root must receive its completed native paint exactly once.");
			Assert.That(child.PaintCount, Is.EqualTo(1), "The final invalidation must include descendants in the new window.");
			Assert.That(moved.LastUpdateRegion, Is.EqualTo(moved.ClientRectangle));
			Assert.That(child.LastUpdateRegion, Is.EqualTo(child.ClientRectangle));
			Assert.That(WinAPI.USER32.IsWindowVisible(moved.Handle), Is.True);
			Assert.That(WinAPI.USER32.IsWindowVisible(child.Handle), Is.True);
		});
	}

	[Test]
	public void ReparentedTreeRemainsSuppressedUntilItsNewOwnersScopeEnds() {
		using var original = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-19000, -19000) };
		using var destination = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-18000, -18000) };
		using var moved = new CountingPanel { Dock = DockStyle.Fill, Padding = new Padding(7) };
		using var child = new CountingPanel { Dock = DockStyle.Fill };
		original.Controls.Add(moved);
		moved.Controls.Add(child);
		original.Show();
		destination.Show();
		System.Windows.Forms.Application.DoEvents();
		using var oldOwner = original.EnterRenderingScope();
		using (destination.EnterRenderingScope()) {
			destination.Controls.Add(moved);
			moved.ResetCounts();
			child.ResetCounts();
			oldOwner.Dispose();
			System.Windows.Forms.Application.DoEvents();
			Assert.That(moved.EnableRedrawCount, Is.Zero, "The new owner still holds redraw suppression.");
			Assert.That(moved.PaintCount, Is.Zero);
			Assert.That(child.PaintCount, Is.Zero);
		}
		System.Windows.Forms.Application.DoEvents();
		Assert.Multiple(() => {
			Assert.That(moved.EnableRedrawCount, Is.EqualTo(1));
			Assert.That(child.EnableRedrawCount, Is.EqualTo(1));
			Assert.That(moved.PaintCount, Is.EqualTo(1));
			Assert.That(child.PaintCount, Is.EqualTo(1));
		});
	}

	[Test]
	public void DeferredPaintingCompletesLayoutAndVisibilityBeforeTheNextNativePaintCycle() {
		using var form = new Form();
		using var parent = new CountingPanel { Dock = DockStyle.Fill };
		using var child = new Button { Dock = DockStyle.Fill };
		using var hidden = new TextBox();
		form.Controls.Add(parent);
		parent.Controls.Add(child);
		parent.Controls.Add(hidden);
		form.Show();
		System.Windows.Forms.Application.DoEvents();
		parent.ResetCounts();
		using (parent.EnterRenderingScope(repaintImmediately: false)) {
			parent.Padding = new Padding(13);
			hidden.Hide();
		}
		Assert.Multiple(() => {
			Assert.That(parent.EnableRedrawCount, Is.EqualTo(1));
			Assert.That(parent.LayoutCount, Is.EqualTo(1));
			Assert.That(child.Bounds, Is.EqualTo(new Rectangle(13, 13, parent.ClientSize.Width - 26, parent.ClientSize.Height - 26)));
			Assert.That(hidden.Visible, Is.False);
			Assert.That(WinAPI.USER32.IsWindowVisible(hidden.Handle), Is.False);
			Assert.That(parent.PaintCount, Is.Zero, "Restoring drawing does not force a paint inside the operation that released the scope.");
		});
		System.Windows.Forms.Application.DoEvents();
		Assert.That(parent.PaintCount, Is.EqualTo(1), "The normal native paint cycle renders the final state once.");
	}

	[Test]
	public void NestedScopesSuppressRealWindowPaintingAndFlushChildLayoutOnce() {
		using var form = new Form();
		using var parent = new CountingPanel { Dock = DockStyle.Fill, Padding = new Padding(3) };
		using var child = new CountingPanel { Dock = DockStyle.Fill };
		using var fill = new Button { Dock = DockStyle.Fill };
		form.Controls.Add(parent);
		parent.Controls.Add(child);
		child.Controls.Add(fill);
		form.Show();
		System.Windows.Forms.Application.DoEvents();
		parent.ResetCounts();
		child.ResetCounts();
		using (parent.EnterRenderingScope()) {
			using (parent.EnterRenderingScope()) {
				using (child.EnterRenderingScope()) {
					child.Padding = new Padding(11);
					parent.Invalidate(true);
					System.Windows.Forms.Application.DoEvents();
					Assert.That(parent.PaintCount, Is.Zero);
					Assert.That(child.LayoutCount, Is.Zero);
					Assert.That(parent.DisableRedrawCount, Is.EqualTo(1));
					Assert.That(child.DisableRedrawCount, Is.EqualTo(1));
				}
			}
			Assert.That(parent.EnableRedrawCount, Is.Zero);
			Assert.That(child.LayoutCount, Is.Zero);
		}
		Assert.That(parent.EnableRedrawCount, Is.EqualTo(1));
		Assert.That(child.EnableRedrawCount, Is.EqualTo(1));
		Assert.That(child.LayoutCount, Is.EqualTo(1));
		Assert.That(fill.Bounds, Is.EqualTo(new Rectangle(11, 11, child.ClientSize.Width - 22, child.ClientSize.Height - 22)));
		Assert.That(parent.PaintCount, Is.EqualTo(1), "The outer scope performs one final native paint.");
	}

	[Test]
	public void FailedEntryRestoresPreviouslySuppressedControlsAndCanBeRetried() {
		using var form = new Form();
		using var parent = new CountingPanel { Dock = DockStyle.Fill, Padding = new Padding(3) };
		using var child = new FailingLayoutPanel { Dock = DockStyle.Fill };
		form.Controls.Add(parent);
		parent.Controls.Add(child);
		form.Show();
		System.Windows.Forms.Application.DoEvents();
		parent.ResetCounts();
		child.ResetCounts();
		child.FailOnNextEnumeration();
		var failure = Assert.Throws<InvalidOperationException>(() => parent.EnterRenderingScope());
		Assert.That(failure, Is.SameAs(child.Failure));
		Assert.That(parent.DisableRedrawCount, Is.EqualTo(1));
		Assert.That(parent.EnableRedrawCount, Is.EqualTo(1));
		Assert.That(child.DisableRedrawCount, Is.EqualTo(1));
		Assert.That(child.EnableRedrawCount, Is.EqualTo(1));
		using (parent.EnterRenderingScope())
			child.Padding = new Padding(7);
		Assert.That(parent.DisableRedrawCount, Is.EqualTo(2));
		Assert.That(parent.EnableRedrawCount, Is.EqualTo(2));
		Assert.That(child.DisableRedrawCount, Is.EqualTo(2));
		Assert.That(child.EnableRedrawCount, Is.EqualTo(2));
		Assert.That(child.LayoutCount, Is.EqualTo(1), "The failed acquisition must not leave the child layout suspended.");
	}

	[Test]
	public void FailedLayoutResumptionRestoresEveryWindowAndPreservesTheOriginalFailure() {
		using var form = new Form();
		using var parent = new CountingPanel { Dock = DockStyle.Fill, Padding = new Padding(3) };
		using var failing = new FailingLayoutPanel { Dock = DockStyle.Top };
		using var sibling = new CountingPanel { Dock = DockStyle.Fill };
		form.Controls.Add(parent);
		parent.Controls.Add(failing);
		parent.Controls.Add(sibling);
		form.Show();
		System.Windows.Forms.Application.DoEvents();
		parent.ResetCounts();
		failing.ResetCounts();
		sibling.ResetCounts();
		var rendering = parent.EnterRenderingScope();
		failing.Padding = new Padding(5);
		sibling.Padding = new Padding(7);
		failing.FailOnNextLayout();
		var failure = Assert.Throws<InvalidOperationException>(rendering.Dispose);
		Assert.That(failure, Is.SameAs(failing.Failure));
		Assert.That(parent.EnableRedrawCount, Is.EqualTo(1));
		Assert.That(failing.EnableRedrawCount, Is.EqualTo(1));
		Assert.That(sibling.EnableRedrawCount, Is.EqualTo(1));
		Assert.That(sibling.LayoutCount, Is.EqualTo(1));
		using (parent.EnterRenderingScope())
			failing.Padding = new Padding(9);
		Assert.That(failing.LayoutCount, Is.EqualTo(1), "The recovery pass must balance the failed native layout resumption.");
		Assert.That(parent.EnableRedrawCount, Is.EqualTo(2));
	}

	[Test]
	public void ScopeRestoresDrawingAfterAnExceptionAndCanBeReentered() {
		using var panel = new CountingPanel();
		panel.CreateControl();
		panel.ResetCounts();
		Assert.That(() => {
			using var rendering = panel.EnterRenderingScope();
			panel.Padding = new Padding(4);
			throw new InvalidOperationException("Aborted update");
		}, Throws.InvalidOperationException);
		Assert.That(panel.DisableRedrawCount, Is.EqualTo(1));
		Assert.That(panel.EnableRedrawCount, Is.EqualTo(1));
		using (panel.EnterRenderingScope())
			panel.Padding = new Padding(6);
		Assert.That(panel.DisableRedrawCount, Is.EqualTo(2));
		Assert.That(panel.EnableRedrawCount, Is.EqualTo(2));
	}

	[Test]
	public void ScopeDoesNotForceHandlesAndSuppressesHandlesCreatedDuringTheUpdate() {
		using var panel = new CountingPanel();
		using (panel.EnterRenderingScope()) {
			Assert.That(panel.IsHandleCreated, Is.False);
			panel.CreateControl();
			Assert.That(panel.DisableRedrawCount, Is.EqualTo(1));
			Assert.That(panel.EnableRedrawCount, Is.Zero);
		}
		Assert.That(panel.EnableRedrawCount, Is.EqualTo(1));
	}

	[Test]
	public void AddedAndNewlyVisibleChildrenJoinTheSuppressionScope() {
		using var form = new Form();
		using var parent = new CountingPanel { Dock = DockStyle.Fill };
		using var hidden = new CountingPanel { Visible = false };
		form.Controls.Add(parent);
		parent.Controls.Add(hidden);
		form.Show();
		_ = hidden.Handle;
		hidden.ResetCounts();
		using var added = new CountingPanel();
		using (parent.EnterRenderingScope()) {
			parent.Controls.Add(added);
			hidden.Visible = true;
			Assert.That(added.DisableRedrawCount, Is.EqualTo(1));
			Assert.That(hidden.DisableRedrawCount, Is.EqualTo(1));
			Assert.That(added.EnableRedrawCount, Is.Zero);
		}
		Assert.That(added.EnableRedrawCount, Is.EqualTo(1));
		Assert.That(hidden.EnableRedrawCount, Is.EqualTo(1));
		Assert.That(hidden.Visible, Is.True);
	}

	[Test]
	public void HiddenAndDisposedControlsAreNotResurrectedDuringRestoration() {
		using var form = new Form();
		using var parent = new CountingPanel { Dock = DockStyle.Fill };
		using var hidden = new CountingPanel();
		using var removed = new CountingPanel();
		form.Controls.Add(parent);
		parent.Controls.Add(hidden);
		parent.Controls.Add(removed);
		form.Show();
		using (parent.EnterRenderingScope()) {
			hidden.Visible = false;
			removed.Dispose();
		}
		Assert.That(hidden.Visible, Is.False);
		Assert.That(removed.IsDisposed, Is.True);
		Assert.That(parent.EnableRedrawCount, Is.EqualTo(1));
	}

	[Test]
	public void AsynchronousDisposalCompletesOnTheUiThread() {
		using var panel = new CountingPanel();
		panel.CreateControl();
		panel.ResetCounts();
		var scope = panel.EnterRenderingScope();
		var disposal = scope.DisposeAsync();
		Assert.That(disposal.IsCompletedSuccessfully, Is.True);
		Assert.That(panel.EnableRedrawCount, Is.EqualTo(1));
	}

	[TestCase(false, false)]
	[TestCase(true, false)]
	[TestCase(false, true)]
	[TestCase(true, true)]
	public void RetainedParentRestoresItsChildsOwnNativeVisibility(bool hideChild, bool declaredDefault) {
		using var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-19000, -19000) };
		using var parent = new Panel { Dock = DockStyle.Fill };
		using Control child = declaredDefault ? new DefaultHiddenComboBox() : new ComboBox();
		child.Bounds = new Rectangle(10, 10, 180, 24);
		form.Controls.Add(parent);
		parent.Controls.Add(child);
		form.Show();
		System.Windows.Forms.Application.DoEvents();
		Assert.That(Sphere10.Framework.Windows.WinAPI.USER32.IsWindowVisible(child.Handle), Is.True);
		using (parent.EnterRenderingScope()) {
			parent.Hide();
			if (hideChild)
				child.Hide();
		}
		parent.Show();
		System.Windows.Forms.Application.DoEvents();
		Assert.That(child.Visible, Is.EqualTo(!hideChild));
		Assert.That(Sphere10.Framework.Windows.WinAPI.USER32.IsWindowVisible(child.Handle), Is.EqualTo(!hideChild), "Showing a retained parent must restore requested native visibility without resurrecting explicitly hidden children.");
	}

	private sealed class DefaultHiddenComboBox : ComboBox {
		[DefaultValue(false)]
		public new bool Visible {
			get => base.Visible;
			set => base.Visible = value;
		}
	}

	private class CountingPanel : Panel {
		public int DisableRedrawCount { get; private set; }
		public int EnableRedrawCount { get; private set; }
		public int LayoutCount { get; private set; }
		public int PaintCount { get; private set; }
		public Rectangle LastUpdateRegion { get; private set; }

		public void ResetCounts() {
			DisableRedrawCount = EnableRedrawCount = LayoutCount = PaintCount = 0;
			LastUpdateRegion = Rectangle.Empty;
		}

		protected override void WndProc(ref Message message) {
			// BeginPaint clips a fully offscreen HWND to an empty visible area; its native update region is still complete.
			if (message.Msg == 0x000F && WinAPI.USER32.GetUpdateRect(new HandleRef(this, Handle), out var update, false))
				LastUpdateRegion = Rectangle.FromLTRB(update.Left, update.Top, update.Right, update.Bottom);
			if (message.Msg == 0x000B) {
				if (message.WParam == IntPtr.Zero)
					DisableRedrawCount++;
				else
					EnableRedrawCount++;
			}
			base.WndProc(ref message);
		}

		protected override void OnLayout(LayoutEventArgs args) {
			LayoutCount++;
			base.OnLayout(args);
		}

		protected override void OnPaint(PaintEventArgs args) {
			PaintCount++;
			base.OnPaint(args);
		}
	}

	private sealed class FailingLayoutPanel : CountingPanel {
		private bool _failEnumeration;
		private bool _failLayout;

		public InvalidOperationException Failure { get; } = new("Controlled control failure.");

		public void FailOnNextEnumeration() => _failEnumeration = true;

		public void FailOnNextLayout() => _failLayout = true;

		protected override ControlCollection CreateControlsInstance() => new FailingControlCollection(this);

		protected override void OnLayout(LayoutEventArgs args) {
			if (_failLayout) {
				_failLayout = false;
				throw Failure;
			}
			base.OnLayout(args);
		}

		private sealed class FailingControlCollection : ControlCollection {
			private readonly FailingLayoutPanel _owner;

			public FailingControlCollection(FailingLayoutPanel owner)
				: base(owner) => _owner = owner;

			public override IEnumerator GetEnumerator() {
				if (_owner._failEnumeration) {
					_owner._failEnumeration = false;
					throw _owner.Failure;
				}
				return base.GetEnumerator();
			}
		}
	}
}

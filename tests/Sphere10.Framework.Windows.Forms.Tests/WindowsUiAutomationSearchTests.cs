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
using PropertyId = Sphere10.Framework.Windows.WindowsUiAutomationComObject.PropertyId;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class WindowsUiAutomationSearchTests {
	[Test]
	public void DescendantSearchFindsADeeplyNestedPictureAndAFilteredWalkerReachesItsPane() {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		var screen = Tools.Windows.Display.GetBounds(Screen.PrimaryScreen.DeviceName);
		using var owner = new Form {
			Text = "Automation search fixture", FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false,
			StartPosition = FormStartPosition.Manual, Bounds = new Rectangle(screen.Left + 120, screen.Top + 120, 420, 320)
		};
		var pane = new Panel { Bounds = new Rectangle(20, 20, 300, 220), AccessibleName = "Fixture pane" };
		owner.Controls.Add(pane);
		// The picture sits far deeper than its window's first levels; a search does not pay for that depth.
		Control parent = pane;
		for (var level = 0; level < 8; level++) {
			var inner = new Panel { Dock = DockStyle.Fill };
			parent.Controls.Add(inner);
			parent = inner;
		}
		var picture = new PictureBox {
			Bounds = new Rectangle(30, 30, 160, 100), BackColor = Color.CornflowerBlue, AccessibleRole = AccessibleRole.Graphic, AccessibleName = "Fixture picture"
		};
		parent.Controls.Add(picture);
		owner.Show();
		owner.Update();
		var expected = picture.RectangleToScreen(picture.ClientRectangle);
		var paneBounds = pane.RectangleToScreen(pane.ClientRectangle);
		var handle = owner.Handle;

		// The client runs on an MTA worker, as the media helper's does, while this thread serves the window's provider.
		var query = Task.Run(() => {
			using var workerDpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
			using var automation = Tools.Windows.COM.CreateUiAutomation(TimeSpan.FromSeconds(5));
			foreach (var property in new[] { PropertyId.RuntimeId, PropertyId.ControlTypeId, PropertyId.BoundingRectangle })
				automation.AddProperty(property);
			using var root = automation.ElementFromHandle(handle);
			using var image = automation.CreateCondition(PropertyId.ControlTypeId, WinAPI.UIAUTOMATION.UIA_ImageControlTypeId);
			using var paneCondition = automation.CreateCondition(PropertyId.ControlTypeId, WinAPI.UIAUTOMATION.UIA_PaneControlTypeId);
			using var either = automation.CreateEitherCondition(image, paneCondition);
			using var walker = automation.CreateWalker(paneCondition);
			var status = automation.FindDescendants(root, image, out var images);
			var eitherStatus = automation.FindDescendants(root, either, out var both);
			try {
				WindowsUiAutomationComObject.Element container = null;
				var parentStatus = images.Length == 0 ? -1 : automation.GetParent(walker, images[0], out container);
				using (container)
					return new SearchOutcome(status, images.Select(ReadBounds).ToArray(), eitherStatus, both.Length, parentStatus, container == null ? Rectangle.Empty : ReadBounds(container));
			} finally {
				foreach (var element in images.Concat(both))
					element.Dispose();
			}
		});
		var watch = Stopwatch.StartNew();
		while (!query.IsCompleted && watch.Elapsed < TimeSpan.FromSeconds(20)) {
			System.Windows.Forms.Application.DoEvents();
			Thread.Sleep(5);
		}
		Assert.That(query.IsCompleted, Is.True, "The search must finish while the window's own thread serves it.");
		var outcome = query.GetAwaiter().GetResult();

		Assert.That(outcome.Status, Is.GreaterThanOrEqualTo(0));
		Assert.That(outcome.Images, Is.EqualTo(new[] { expected }), "One request returns the nested picture with its physical bounds.");
		Assert.That(outcome.EitherStatus, Is.GreaterThanOrEqualTo(0));
		Assert.That(outcome.EitherCount, Is.GreaterThan(outcome.Images.Length), "A combined condition also returns the panes.");
		Assert.That(outcome.ParentStatus, Is.GreaterThanOrEqualTo(0));
		Assert.That(outcome.Container.IsEmpty, Is.False, "A filtered walker stops at the nearest pane above the picture.");
		Assert.That(paneBounds.Contains(outcome.Container), Is.True);
		Assert.That(outcome.Container.Contains(expected), Is.True);
	}

	[Test]
	public void RequestTimeoutMustBePositiveAndBounded() {
		Assert.That(() => Tools.Windows.COM.CreateUiAutomation(TimeSpan.Zero), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => Tools.Windows.COM.CreateUiAutomation(TimeSpan.FromMinutes(2)), Throws.InstanceOf<ArgumentException>());
	}

	private static Rectangle ReadBounds(WindowsUiAutomationComObject.Element element)
		=> element.GetCachedProperty(PropertyId.BoundingRectangle, out var value, out _) >= 0 && value is double[] { Length: 4 } box
			? new Rectangle((int)box[0], (int)box[1], (int)box[2], (int)box[3]) : Rectangle.Empty;

	#region Inner Types
	private sealed record SearchOutcome(int Status, Rectangle[] Images, int EitherStatus, int EitherCount, int ParentStatus, Rectangle Container);
	#endregion
}

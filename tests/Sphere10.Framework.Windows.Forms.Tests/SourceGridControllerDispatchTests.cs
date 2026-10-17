// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Controllers;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class SourceGridControllerDispatchTests {

	[TestCase("MouseDown")]
	[TestCase("MouseMove")]
	[TestCase("Click")]
	[TestCase("KeyDown")]
	[TestCase("EditStarting")]
	public void CallbacksCanReplaceControllersWithoutSkippingTheAdmittedEvent(string eventName) {
		var container = new ControllerContainer();
		var calls = new List<string>();
		var added = new CallbackController(() => calls.Add("Added"));
		var removed = new CallbackController(() => calls.Add("Removed"));
		var changed = false;
		var first = new CallbackController(() => {
			calls.Add("First");
			if (changed)
				return;
			changed = true;
			container.RemoveController(removed);
			container.AddController(added);
		});
		container.AddController(first);
		container.AddController(removed);
		Dispatch(container, eventName);
		Assert.That(calls, Is.EqualTo(new[] { "First", "Removed" }));
		calls.Clear();
		Dispatch(container, eventName);
		Assert.That(calls, Is.EqualTo(new[] { "First", "Added" }));
	}

	[Test]
	public void NestedDispatchUsesNewMembershipWithoutChangingTheOuterMouseEvent() {
		var container = new ControllerContainer();
		var calls = new List<string>();
		var removed = new CallbackController(() => calls.Add("Removed"));
		var added = new CallbackController(() => calls.Add("Added"));
		var changed = false;
		container.AddController(new CallbackController(() => {
			calls.Add("First");
			if (changed)
				return;
			changed = true;
			container.RemoveController(removed);
			container.AddController(added);
			Dispatch(container, "MouseDown");
		}));
		container.AddController(removed);
		Dispatch(container, "MouseDown");
		Assert.That(calls, Is.EqualTo(new[] { "First", "First", "Added", "Removed" }));
	}

	[Test]
	public void CellDispatcherSurvivesCellControllerChangesDuringTheMouseCallback() {
		using var grid = new Grid();
		grid.Redim(1, 1);
		var cell = new SourceGrid.Cells.Cell("Value");
		grid[0, 0] = cell;
		var calls = new List<string>();
		var removed = new CallbackController(() => calls.Add("Removed"));
		cell.AddController(new CallbackController(() => {
			calls.Add("First");
			cell.RemoveController(removed);
			cell.AddController(new CallbackController(() => calls.Add("Added")));
		}));
		cell.AddController(removed);
		var dispatcher = new ControllerContainer();
		dispatcher.AddController(CellEventDispatcher.Default);
		dispatcher.OnMouseDown(new CellContext(grid, new Position(0, 0)), new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
		Assert.That(calls, Is.EqualTo(new[] { "First", "Removed" }));
	}

	[Test]
	public void EmptyDispatchAndFocusVetoRetainTheirContracts() {
		var container = new ControllerContainer();
		Assert.That(() => Dispatch(container, "MouseDown"), Throws.Nothing);
		Assert.That(container.CanReceiveFocus(default, EventArgs.Empty), Is.True);
		var calls = new List<string>();
		container.AddController(new CallbackController(() => calls.Add("Veto"), false));
		container.AddController(new CallbackController(() => calls.Add("Later")));
		Assert.That(container.CanReceiveFocus(default, EventArgs.Empty), Is.False);
		Assert.That(calls, Is.EqualTo(new[] { "Veto" }));
	}

	private static void Dispatch(ControllerContainer container, string eventName) {
		switch (eventName) {
			case "MouseDown": container.OnMouseDown(default, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0)); break;
			case "MouseMove": container.OnMouseMove(default, new MouseEventArgs(MouseButtons.None, 0, 0, 0, 0)); break;
			case "Click": container.OnClick(default, EventArgs.Empty); break;
			case "KeyDown": container.OnKeyDown(default, new KeyEventArgs(Keys.Enter)); break;
			case "EditStarting": container.OnEditStarting(default, new CancelEventArgs()); break;
			default: throw new ArgumentOutOfRangeException(nameof(eventName));
		}
	}

	private sealed class CallbackController : ControllerBase {
		private readonly Action _callback;
		private readonly bool _canFocus;
		public CallbackController(Action callback, bool canFocus = true) { _callback = callback; _canFocus = canFocus; }
		public override void OnMouseDown(CellContext sender, MouseEventArgs e) => _callback();
		public override void OnMouseMove(CellContext sender, MouseEventArgs e) => _callback();
		public override void OnClick(CellContext sender, EventArgs e) => _callback();
		public override void OnKeyDown(CellContext sender, KeyEventArgs e) => _callback();
		public override void OnEditStarting(CellContext sender, CancelEventArgs e) => _callback();
		public override bool CanReceiveFocus(CellContext sender, EventArgs e) { _callback(); return _canFocus; }
	}
}

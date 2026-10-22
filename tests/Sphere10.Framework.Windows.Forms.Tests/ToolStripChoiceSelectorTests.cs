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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ToolStripChoiceSelectorTests {
	[Test]
	public void SelectionRetainsExactModelsAndCopiedMembershipWhileReplacingChoices() {
		using var selector = new ToolStripChoiceSelector { ItemTextProvider = item => ((Choice)item).Name };
		var first = new Choice(1, "First");
		var second = new Choice(1, "Second");
		selector.SetItems(new object[] { first, second });
		selector.SelectedItem = second;
		Assert.That(selector.SelectedIndex, Is.EqualTo(1), "Reference identity wins over equal values.");
		var copy = selector.Items;
		copy[1] = first;
		Assert.That(selector.SelectedItem, Is.SameAs(second));
		var notifications = 0;
		selector.SelectedIndexChanged += (_, _) => notifications++;
		selector.SetItems(new object[] { second, first });
		Assert.That(selector.SelectedIndex, Is.Zero);
		Assert.That(selector.SelectedItem, Is.SameAs(second));
		Assert.That(selector.Text, Is.EqualTo("Second"));
		Assert.That(notifications, Is.EqualTo(1));
		Assert.That(selector.DropDownItems[0].Tag, Is.SameAs(second));
		Assert.That(selector.DropDownItems.Cast<ToolStripMenuItem>().Select(item => item.Checked), Is.EqualTo(new[] { true, false }));
	}

	[TestCase(9F)]
	[TestCase(13.5F)]
	public void FocusedNativeMenuSupportsKeyboardSelectionWithoutAnEditableField(float points) => RunWithMessageLoop(async owner => {
		using var font = new Font("Segoe UI", points);
		using var toolbar = new ToolStrip { Dock = DockStyle.Top, Font = font };
		using var menu = new KeyboardMenu();
		var first = new Choice(1, "Balanced");
		var second = new Choice(2, "Power Saver");
		var selector = new ToolStripChoiceSelector { Name = "choice", DropDown = menu };
		selector.SetItems(new object[] { first, second });
		selector.SelectedItem = first;
		toolbar.Items.Add(selector);
		owner.Controls.Add(toolbar);
		await owner.InvokeAsync(() => { });
		Assert.That(toolbar.Focus(), Is.True);
		selector.Select();
		Assert.That(selector.Selected, Is.True);
		Assert.That(toolbar.Controls.OfType<ComboBox>(), Is.Empty);
		selector.ShowDropDown();
		menu.Items[0].Select();
		Assert.That(menu.Navigate(Keys.Down), Is.True);
		Assert.That(menu.Items[1].Selected, Is.True);
		Assert.That(menu.Navigate(Keys.Enter), Is.True);
		Assert.That(selector.SelectedItem, Is.SameAs(second));
		Assert.That(selector.Text, Is.EqualTo("Power Saver"));
		Assert.That(selector.DropDownItems.Cast<ToolStripMenuItem>().Select(item => item.Checked), Is.EqualTo(new[] { false, true }));
		Assert.That(selector.Font.SizeInPoints, Is.EqualTo(points));
		Assert.That(selector.Height, Is.GreaterThanOrEqualTo(selector.Font.Height));
	});

	[Test]
	public void ClickAndClearUpdateChecksAndDisposalReleasesBorrowedChoiceReferences() {
		var selector = new ToolStripChoiceSelector { EmptyText = "Choose Plan" };
		using var first = new Choice(1, "Balanced");
		using var second = new Choice(2, "Power Saver");
		selector.SetItems(new object[] { first, second });
		var option = (ToolStripMenuItem)selector.DropDownItems[1];
		option.PerformClick();
		Assert.That(selector.SelectedItem, Is.SameAs(second));
		Assert.That(option.Checked, Is.True);
		selector.SelectedIndex = -1;
		Assert.That(selector.Text, Is.EqualTo("Choose Plan"));
		Assert.That(selector.DropDownItems.Cast<ToolStripMenuItem>().Any(item => item.Checked), Is.False);
		selector.Dispose();
		Assert.That(selector.Items, Is.Empty);
		Assert.That(selector.SelectedItem, Is.Null);
		Assert.That(option.IsDisposed, Is.True);
		Assert.That(option.Tag, Is.Null);
		Assert.That(first.Disposed || second.Disposed, Is.False, "Choices belong to their caller.");
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(600, 150) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("Choice selector test timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			failure = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => test(owner));
			completed = true;
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}

	private sealed class KeyboardMenu : ToolStripDropDownMenu {
		public bool Navigate(Keys key) => ProcessDialogKey(key);
	}

	private sealed class Choice : IDisposable {
		public Choice(int id, string name) { Id = id; Name = name; }
		public int Id { get; }
		public string Name { get; }
		public bool Disposed { get; private set; }
		public override string ToString() => Name;
		public override bool Equals(object other) => other is Choice choice && Id == choice.Id;
		public override int GetHashCode() => Id;
		public void Dispose() => Disposed = true;
	}
}

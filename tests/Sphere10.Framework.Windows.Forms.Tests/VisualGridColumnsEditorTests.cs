// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Exercises the native collection editor used by screen-owned grid column settings.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class VisualGridColumnsEditorTests {

	[TestCase(false)]
	[TestCase(true)]
	public void NativeOrderingAndColumnEditsPublishOnlyOnAcceptance(bool accept) {
		using var owner = new Form {
			ShowInTaskbar = false,
			StartPosition = FormStartPosition.Manual,
			Location = new Point(-20000, -20000),
			ClientSize = new Size(700, 500)
		};
		owner.Show();
		var original = new[] {
			new ApplicationGridColumnPreferences { Key = "first", Title = "First" },
			new ApplicationGridColumnPreferences { Key = "second", Title = "Second" }
		};
		var property = TypeDescriptor.GetProperties(typeof(VisualGridSettings))[nameof(VisualGridSettings.Columns)];
		var editor = (UITypeEditor)property.GetEditor(typeof(UITypeEditor));
		Assert.That(editor, Is.TypeOf<VisualGridColumnsEditor>());
		Exception failure = null;
		var service = new CollectionEditorService(owner, property, new VisualGridSettings { Columns = original }, form => {
			var members = Find<ListBox>(form, "listbox");
			Assert.That(members.Items.Count, Is.EqualTo(2));
			Assert.That(Find<Button>(form, "addButton").Visible, Is.False);
			Assert.That(Find<Button>(form, "removeButton").Enabled, Is.False);
			typeof(Control).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)
				.Invoke(members, new object[] { new KeyEventArgs(Keys.Insert) });
			Assert.That(members.Items.Count, Is.EqualTo(2), "Insert cannot change the screen's column schema.");

			// Reorder through the native command before editing the selected column's values.
			members.ClearSelected();
			members.SelectedIndex = 1;
			Find<Button>(form, "upButton").PerformClick();
			var properties = Find<PropertyGrid>(form, "propertyBrowser");
			CommitText(properties, "Width", "184");
			CommitText(properties, "ExpandsToFit", "True");
			Assert.That(original[1].Width, Is.Null, "The settings owner is not changed while this editor is open.");
			Assert.That(original.Select(column => column.Key), Is.EqualTo(new[] { "first", "second" }));
			Find<Button>(form, accept ? "okButton" : "cancelButton").PerformClick();
		}, error => failure = error);
		var result = (ApplicationGridColumnPreferences[])editor.EditValue(service, service, original);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(original[1].Width, Is.Null);
		Assert.That(original[1].ExpandsToFit, Is.Null);
		if (accept) {
			Assert.That(result, Is.Not.SameAs(original));
			Assert.That(result.Select(column => column.Key), Is.EqualTo(new[] { "second", "first" }));
			Assert.That(result[0].Width, Is.EqualTo(184));
			Assert.That(result[0].ExpandsToFit, Is.True);
		} else
			Assert.That(result, Is.SameAs(original));
	}

	// Commit through the native property editor so nullable width and stretch use their normal converters.
	private static void CommitText(PropertyGrid properties, string property, string text) {
		var root = properties.SelectedGridItem;
		while (root.Parent != null)
			root = root.Parent;
		var item = root.GridItems.Cast<GridItem>().SelectMany(entry => entry.GridItemType == GridItemType.Category
			? entry.GridItems.Cast<GridItem>() : new[] { entry }).Single(entry => entry.PropertyDescriptor?.Name == property);
		properties.SelectedGridItem = item;
		var view = properties.Controls.Cast<Control>().Single(control => control.GetType().Name == "PropertyGridView");
		Assert.That(view.GetType().GetMethod("CommitText", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, new object[] { text }), Is.True);
	}

	// Native collection editor controls have stable designer names.
	private static T Find<T>(Control owner, string name)
		where T : Control {
		return (T)owner.Controls.Find(name, true).Single();
	}

	#region Inner Types

	// Supply the real modal editor service while driving only this test's owned native window.
	private sealed class CollectionEditorService : ITypeDescriptorContext, IWindowsFormsEditorService {
		private readonly Form _owner;
		private readonly Action<Form> _edit;
		private readonly Action<Exception> _fail;

		public CollectionEditorService(Form owner, PropertyDescriptor propertyDescriptor, VisualGridSettings settings, Action<Form> edit, Action<Exception> fail) {
			_owner = owner;
			PropertyDescriptor = propertyDescriptor;
			Instance = settings;
			_edit = edit;
			_fail = fail;
		}

		public IContainer Container => null;

		public object Instance { get; }

		public PropertyDescriptor PropertyDescriptor { get; }

		public bool OnComponentChanging() => true;

		public void OnComponentChanged() {
		}

		public object GetService(Type serviceType) => serviceType == typeof(IWindowsFormsEditorService) ? this : null;

		public DialogResult ShowDialog(Form dialog) {
			dialog.StartPosition = FormStartPosition.Manual;
			dialog.Location = _owner.Location;
			dialog.Shown += (_, _) => {
				try {
					_edit(dialog);
				} catch (Exception error) {
					_fail(error);
					dialog.DialogResult = DialogResult.Cancel;
				}
			};
			using var watchdog = new System.Windows.Forms.Timer { Interval = 10000 };
			watchdog.Tick += (_, _) => {
				_fail(new AssertionException("The native column editor did not finish."));
				dialog.DialogResult = DialogResult.Cancel;
			};
			watchdog.Start();
			return dialog.ShowDialog(_owner);
		}

		public void DropDownControl(Control control) => throw new NotSupportedException("The columns editor uses a modal collection form.");

		public void CloseDropDown() {
		}
	}

	#endregion
}

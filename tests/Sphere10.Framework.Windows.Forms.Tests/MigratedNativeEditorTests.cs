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
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.ComponentModel;
using SourceGrid = Sphere10.Framework.Windows.Forms.SourceGrid;
using Tools;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Checks shared native editors and borrowed resources after extraction from the grid bundle.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class MigratedNativeEditorTests {

	[TestCase(FontStyle.Bold)]
	[TestCase(FontStyle.Italic)]
	[TestCase(FontStyle.Underline)]
	public void RichTextConversionRetainsTextAndRequestedFontStyle(FontStyle style) {
		var value = RichTextConversion.StringToRichText("Styled value", style);
		using var editor = new RichTextBox { Rtf = value.Rtf };
		editor.SelectAll();
		using var selectedFont = editor.SelectionFont;

		Assert.That(RichTextConversion.RichTextToString(value), Is.EqualTo("Styled value"));
		Assert.That(selectedFont.Style.HasFlag(style), Is.True);
	}

	[Test]
	public void InvalidRichTextRetainsTheEmptyTextFallback() {
		var value = new Sphere10.Framework.Windows.Forms.RichText("This is not RTF");

		Assert.That(RichTextConversion.RichTextToString(value), Is.Empty);
	}

	[Test]
	public void TextEditorKeepsPlaceholderAndOrdinaryInputAndSupportsTypedValidation() {
		using var validator = new TypeConverterValidator(typeof(int));
		using var editor = new TextProbe { PlaceHolderText = "Enter a number", Text = "ordinary text" };
		Assert.That(editor.Value, Is.EqualTo("ordinary text"));
		Assert.That(editor.PlaceHolderText, Is.EqualTo("Enter a number"));

		editor.Validator = validator;
		editor.Value = 42;
		Assert.That(editor.Text, Is.EqualTo("42"));
		Assert.That(editor.Value, Is.EqualTo(42));

		editor.Text = "not a number";
		Assert.That(editor.IsValidValue(out _), Is.False);
		Assert.That(() => editor.Value, Throws.TypeOf<ArgumentOutOfRangeException>());
		Assert.That(editor.ValidateInput().Cancel, Is.True);

		editor.Validator = null;
		Assert.That(editor.Value, Is.EqualTo("not a number"));
		Assert.That(editor.PlaceHolderText, Is.EqualTo("Enter a number"));
	}

	[Test]
	public void DefaultTextEditorDoesNotAllocatePlaceholderComponentsOrCreateAHandle() {
		using var editor = new TextProbe();
		Assert.That(editor.PlaceHolderText, Is.Empty);
		editor.PlaceHolderText = string.Empty;
		editor.RecreateNativeHandle();

		Assert.That(editor.IsHandleCreated, Is.False);
		Assert.That(GetPlaceholderComponent(editor), Is.Null);
		Assert.That(typeof(TextBoxEx).GetField("_components", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(editor), Is.Null);
	}

	[Test]
	public void PlaceholderCueSurvivesHandleRecreationAndCanBeCleared() {
		RunWithMessageLoop(owner => {
			using var editor = new TextProbe { Parent = owner };
			editor.PlaceHolderText = "Enter a value";
			Assert.That(editor.IsHandleCreated, Is.True);
			Assert.That(ReadNativeCueBanner(editor), Is.EqualTo("Enter a value"));
			editor.RecreateNativeHandle();
			Assert.That(editor.PlaceHolderText, Is.EqualTo("Enter a value"));
			Assert.That(ReadNativeCueBanner(editor), Is.EqualTo("Enter a value"));

			editor.PlaceHolderText = string.Empty;
			Assert.That(editor.PlaceHolderText, Is.Empty);
			Assert.That(ReadNativeCueBanner(editor), Is.Empty);
			editor.RecreateNativeHandle();
			Assert.That(ReadNativeCueBanner(editor), Is.Empty);
		});
	}

	[Test]
	public void ExplicitNullPlaceholderRetainsItsValueAndClearsTheNativeCue() {
		RunWithMessageLoop(owner => {
			using var editor = new TextProbe { Parent = owner };
			editor.PlaceHolderText = null;
			Assert.That(editor.PlaceHolderText, Is.Null);
			Assert.That(ReadNativeCueBanner(editor), Is.Empty);
			editor.PlaceHolderText = "Temporary cue";
			Assert.That(ReadNativeCueBanner(editor), Is.EqualTo("Temporary cue"));
			editor.PlaceHolderText = null;
			editor.RecreateNativeHandle();

			Assert.That(editor.PlaceHolderText, Is.Null);
			Assert.That(ReadNativeCueBanner(editor), Is.Empty);
		});
	}

	[Test]
	public void TextEditorDisposesItsActivatedPlaceholderComponent() {
		using var editor = new TextProbe { PlaceHolderText = "Owned cue" };
		var component = GetPlaceholderComponent(editor);
		var disposed = false;
		component.Disposed += (_, _) => disposed = true;

		editor.Dispose();

		Assert.That(disposed, Is.True);
		Assert.That(editor.IsHandleCreated, Is.False);
	}

	[Test]
	public void ComboEditorUsesOrderedStandardValuesAndRetainsOrdinaryItemsWithoutValidation() {
		using var validator = new TypeConverterValidator(typeof(DayOfWeek)) {
			AllowStringConversion = false,
			StandardValues = new[] { DayOfWeek.Friday, DayOfWeek.Monday },
			StandardValuesExclusive = true
		};
		using var editor = new ComboBoxEx { PlaceHolderText = "Choose a day" };
		editor.Items.Add("ordinary item");
		editor.SelectedIndex = 0;
		Assert.That(editor.Value, Is.EqualTo("ordinary item"));

		editor.Validator = validator;
		Assert.That(editor.Items.Cast<object>(), Is.EqualTo(new object[] { DayOfWeek.Friday, DayOfWeek.Monday }));
		Assert.That(editor.DropDownStyle, Is.EqualTo(ComboBoxStyle.DropDownList));
		editor.SelectedItem = DayOfWeek.Monday;
		Assert.That(editor.Value, Is.EqualTo(DayOfWeek.Monday));
		Assert.That(editor.PlaceHolderText, Is.EqualTo("Choose a day"));
	}

	[TestCase("Text")]
	[TestCase("Combo")]
	[TestCase("Masked")]
	[TestCase("RichText")]
	[TestCase("Button")]
	public void ReplacingClearingAndDisposingAnEditorDetachesItsBorrowedValidator(string kind) {
		using var first = new TypeConverterValidator(typeof(int));
		using var second = new TypeConverterValidator(typeof(int));
		var secondDisposed = false;
		second.Disposed += (_, _) => secondDisposed = true;
		var probe = CreateProbe(kind);
		using var control = (Control)probe;

		probe.Validator = first;
		var attached = probe.RuleChanges;
		first.NullString = "first null";
		Assert.That(probe.RuleChanges, Is.EqualTo(attached + 1));

		probe.Validator = second;
		var replaced = probe.RuleChanges;
		first.NullString = "old validator";
		Assert.That(probe.RuleChanges, Is.EqualTo(replaced));
		second.NullString = "second null";
		Assert.That(probe.RuleChanges, Is.EqualTo(replaced + 1));

		probe.Validator = null;
		var cleared = probe.RuleChanges;
		second.NullString = "cleared validator";
		Assert.That(probe.RuleChanges, Is.EqualTo(cleared));

		probe.Validator = second;
		control.Dispose();
		var disposed = probe.RuleChanges;
		second.NullString = "disposed editor";
		Assert.That(probe.RuleChanges, Is.EqualTo(disposed));
		Assert.That(secondDisposed, Is.False, "The editor borrows its validator.");
	}

	[Test]
	public void ImageNavigatorLoadsItsArrowResourcesAndBoundsNavigationWithoutOwningImages() {
		using var first = new Bitmap(12, 12);
		using var second = new Bitmap(16, 16);
		using var navigator = new ImageNavigator { Images = new Image[] { first, second } };
		Assert.That(navigator.Controls.OfType<Button>().All(button => button.Image != null), Is.True);
		Assert.That(navigator.CurrentImageIndex, Is.Zero);
		navigator.NextImage();
		navigator.NextImage();
		Assert.That(navigator.CurrentImageIndex, Is.EqualTo(1));
		navigator.PreviousImage();
		navigator.PreviousImage();
		Assert.That(navigator.CurrentImageIndex, Is.Zero);

		navigator.Dispose();
		Assert.That(first.Width, Is.EqualTo(12));
		Assert.That(second.Width, Is.EqualTo(16));
	}

	[Test]
	public void ClosingTheNativeEditorDropdownReleasesItsBorrowedControl() {
		RunWithMessageLoop(owner => {
			using var parent = new Button { Parent = owner, Location = new Point(20, 20) };
			using var content = new Panel { Size = new Size(160, 80) };
			using var dropdown = new DropDownForm(content, parent, owner);
			dropdown.DropDownOpen += (_, _) => owner.BeginInvoke(new Action(dropdown.CloseDropDown));
			dropdown.ShowDropDown();

			Assert.That(dropdown.Visible, Is.False);
			Assert.That(content.Parent, Is.Null);
			dropdown.Dispose();
			Assert.That(content.IsDisposed, Is.False, "The UI type editor retains ownership of its dropdown content.");
		});
	}

	[Test]
	public void ListCompositeDetachesAnActiveBorrowedEditorWithoutDisposingOrCommittingIt() {
		RunWithMessageLoop(owner => {
			using var borrowed = new SourceGrid.Cells.Editors.TextBox(typeof(string));
			var row = new BorrowedListRow { Name = "Original" };
			using var composite = new SourceGrid.ListEditor { Dock = DockStyle.Fill, ItemType = typeof(BorrowedListRow) };
			composite.Editors = new SourceGrid.Cells.Editors.EditorBase[] { borrowed };
			composite.List = new ArrayList { row };
			composite.LoadList();
			owner.Controls.Add(composite);
			var grid = composite.Grid;
			var position = new SourceGrid.Position(1, 0);
			Assert.That(grid.Selection.Focus(position, true), Is.True);
			var context = new SourceGrid.CellContext(grid, position);
			context.StartEdit();
			Assert.That(context.IsEditing(), Is.True);
			borrowed.Control.Text = "Uncommitted";

			composite.Dispose();
			Assert.That(borrowed.Control.IsDisposed, Is.False);
			Assert.That(borrowed.Control.Parent, Is.Null);
			Assert.That(borrowed.Grid, Is.Null);
			Assert.That(borrowed.IsEditing, Is.False);
			Assert.That(row.Name, Is.EqualTo("Original"));
			borrowed.Control.Text = "Still usable";
			Assert.That(borrowed.Control.Text, Is.EqualTo("Still usable"));
		});
	}

	[TestCase(ContentAlignment.TopLeft, HorizontalAlignment.Left, TextFormatFlags.Top | TextFormatFlags.Left)]
	[TestCase(ContentAlignment.TopCenter, HorizontalAlignment.Center, TextFormatFlags.Top | TextFormatFlags.HorizontalCenter)]
	[TestCase(ContentAlignment.TopRight, HorizontalAlignment.Right, TextFormatFlags.Top | TextFormatFlags.Right)]
	[TestCase(ContentAlignment.MiddleLeft, HorizontalAlignment.Left, TextFormatFlags.VerticalCenter | TextFormatFlags.Left)]
	[TestCase(ContentAlignment.MiddleCenter, HorizontalAlignment.Center, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter)]
	[TestCase(ContentAlignment.MiddleRight, HorizontalAlignment.Right, TextFormatFlags.VerticalCenter | TextFormatFlags.Right)]
	[TestCase(ContentAlignment.BottomLeft, HorizontalAlignment.Left, TextFormatFlags.Bottom | TextFormatFlags.Left)]
	[TestCase(ContentAlignment.BottomCenter, HorizontalAlignment.Center, TextFormatFlags.Bottom | TextFormatFlags.HorizontalCenter)]
	[TestCase(ContentAlignment.BottomRight, HorizontalAlignment.Right, TextFormatFlags.Bottom | TextFormatFlags.Right)]
	public void NativeAlignmentUsesTheStandardDrawingAlignment(ContentAlignment alignment, HorizontalAlignment horizontal, TextFormatFlags flags) {
		Assert.That(Tools.WinForms.ContentToHorizontalAlignment(alignment), Is.EqualTo(horizontal));
		Assert.That(Tools.WinForms.ContentAlignmentToTextFormatFlags(alignment), Is.EqualTo(flags));
	}

	// Observe the optional component owner without activating the public property.
	private static PlaceHolderTextExtender GetPlaceholderComponent(TextBoxEx editor)
		=> (PlaceHolderTextExtender)typeof(TextBoxEx).GetField("_placeHolderTextExtender", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(editor);

	// Read the actual Windows cue banner rather than only checking the stored property.
	private static string ReadNativeCueBanner(TextBoxEx editor) {
		const int capacity = 256;
		var buffer = Marshal.AllocHGlobal(capacity * sizeof(char));
		using var releaseBuffer = Tools.Scope.ExecuteOnDispose(() => Marshal.FreeHGlobal(buffer));
		Marshal.WriteInt16(buffer, 0);
		WinAPI.USER32.SendMessage(editor.Handle, WinAPI.WindowMessageFlags.EM_GETCUEBANNER, buffer, (IntPtr)capacity);
		return Marshal.PtrToStringUni(buffer);
	}

	// Build the relevant real native subclass while sharing only the observation contract.
	private static IValidatorProbe CreateProbe(string kind) {
		return kind switch {
			"Text" => new TextProbe(),
			"Combo" => new ComboProbe(),
			"Masked" => new MaskedProbe(),
			"RichText" => new RichTextProbe(),
			"Button" => new ButtonProbe(),
			_ => throw new ArgumentOutOfRangeException(nameof(kind))
		};
	}

	// Run native controls and editor-service dropdowns inside their actual owning UI dispatcher.
	private static void RunWithMessageLoop(Action<Form> test) {
		Exception failure = null;
		var completed = false;
		var previousContext = SynchronizationContext.Current;
		using var restoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		using var context = new WindowsFormsSynchronizationContext();
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 10000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The native dropdown did not complete.");
			foreach (var dropdown in System.Windows.Forms.Application.OpenForms.OfType<DropDownForm>().ToArray())
				dropdown.CloseDropDown();
			System.Windows.Forms.Application.ExitThread();
		};
		owner.Shown += (_, _) => {
			try {
				test(owner);
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

	private interface IValidatorProbe {
		IValidator Validator { get; set; }
		int RuleChanges { get; }
	}

	private sealed class BorrowedListRow {
		public string Name { get; set; }
	}

	private sealed class TextProbe : TextBoxEx, IValidatorProbe {
		public int RuleChanges { get; private set; }

		// Exercise the real native recreation path without adding production API.
		public void RecreateNativeHandle() => RecreateHandle();

		public CancelEventArgs ValidateInput() {
			var result = new CancelEventArgs();
			OnValidating(result);
			return result;
		}

		protected override void ApplyValidatorRules() {
			RuleChanges++;
			base.ApplyValidatorRules();
		}
	}

	private sealed class ComboProbe : ComboBoxEx, IValidatorProbe {
		public int RuleChanges { get; private set; }

		protected override void ApplyValidatorRules() {
			RuleChanges++;
			base.ApplyValidatorRules();
		}
	}

	private sealed class MaskedProbe : MaskedTextBoxEx, IValidatorProbe {
		public int RuleChanges { get; private set; }

		protected override void ApplyValidatorRules() {
			RuleChanges++;
			base.ApplyValidatorRules();
		}
	}

	private sealed class RichTextProbe : RichTextBoxEx, IValidatorProbe {
		public int RuleChanges { get; private set; }

		protected override void ApplyValidatorRules() {
			RuleChanges++;
			base.ApplyValidatorRules();
		}
	}

	private sealed class ButtonProbe : ValidatedTextBoxButton, IValidatorProbe {
		public int RuleChanges { get; private set; }

		protected override void ApplyValidatorRules() {
			RuleChanges++;
			base.ApplyValidatorRules();
		}
	}
}

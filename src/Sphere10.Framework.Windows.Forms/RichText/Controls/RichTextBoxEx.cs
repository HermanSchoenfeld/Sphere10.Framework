// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// A RichTextBox that allows to set the type of value to edit,
/// then you can use the Value property to read and write the specific type.
/// Furthermore, it is possible to format single characters.
/// </summary>
public class RichTextBoxEx : System.Windows.Forms.RichTextBox {
	private Sphere10.Framework.ComponentModel.IValidator _validator = null;
	private int _updating = 0;
	private int _oldEventMask = 0;

	/// <summary>
	/// Gets or sets the underline style to apply to the
	/// current selection or insertion point.
	/// </summary>
	/// <remarks>
	/// Underline styles can be set to any value of the
	/// <see cref="UnderlineStyle"/> enumeration.
	/// </remarks>
	public UnderlineStyle SelectionUnderlineStyle {
		get {
			var characterFormat = Tools.Windows.Win32.CreateCharacterFormat();

			// Get the underline style.
			Tools.Windows.Win32.SendMessage(new HandleRef(this, Handle), WinAPI.RICHEDIT.EM_GETCHARFORMAT, WinAPI.RICHEDIT.SCF_SELECTION, ref characterFormat);

			// Default to no underline.
			if ((characterFormat.dwMask & WinAPI.RICHEDIT.CFM_UNDERLINETYPE) == 0)
				return UnderlineStyle.None;

			var type = (byte)(characterFormat.bUnderlineType & 0x0F);

			return (UnderlineStyle)type;
		}

		set {
			// Ensure we don't alter the color by accident.
			var color = SelectionUnderlineColor;

			// Ensure we don't show it if it shouldn't be shown.
			if (value == UnderlineStyle.None)
				color = UnderlineColor.Black;

			var characterFormat = Tools.Windows.Win32.CreateCharacterFormat();
			characterFormat.dwMask = WinAPI.RICHEDIT.CFM_UNDERLINETYPE;
			characterFormat.bUnderlineType = (byte)((byte)value | (byte)color);

			// Set the underline type.
			Tools.Windows.Win32.SendMessage(new HandleRef(this, Handle), WinAPI.RICHEDIT.EM_SETCHARFORMAT, WinAPI.RICHEDIT.SCF_SELECTION, ref characterFormat);
		}
	}

	/// <summary>
	/// Gets or sets the underline color to apply to the
	/// current selection or insertion point.
	/// </summary>
	/// <remarks>
	/// Underline colors can be set to any value of the
	/// <see cref="UnderlineColor"/> enumeration.
	/// </remarks>
	public UnderlineColor SelectionUnderlineColor {
		get {
			var characterFormat = Tools.Windows.Win32.CreateCharacterFormat();

			// Get the underline color.
			Tools.Windows.Win32.SendMessage(new HandleRef(this, Handle), WinAPI.RICHEDIT.EM_GETCHARFORMAT, WinAPI.RICHEDIT.SCF_SELECTION, ref characterFormat);

			// Default to black.
			if ((characterFormat.dwMask & WinAPI.RICHEDIT.CFM_UNDERLINETYPE) == 0)
				return UnderlineColor.Black;

			var style = (byte)(characterFormat.bUnderlineType & 0xF0);

			return (UnderlineColor)style;
		}

		set {
			// Ensure we don't alter the style.
			var style = SelectionUnderlineStyle;

			// Ensure we don't show it if it shouldn't be shown.
			if (style == UnderlineStyle.None)
				value = UnderlineColor.Black;

			var characterFormat = Tools.Windows.Win32.CreateCharacterFormat();
			characterFormat.dwMask = WinAPI.RICHEDIT.CFM_UNDERLINETYPE;
			characterFormat.bUnderlineType = (byte)((byte)style | (byte)value);

			// Set the underline color.
			Tools.Windows.Win32.SendMessage(new HandleRef(this, Handle), WinAPI.RICHEDIT.EM_SETCHARFORMAT, WinAPI.RICHEDIT.SCF_SELECTION, ref characterFormat);
		}
	}

	/// <summary>
	/// Set the effect of the selected text
	/// </summary>
	public EffectType SelectionEffect {
		set {
			switch (value) {
				case EffectType.Subscript:
					SetSelectionSub();
					break;

				case EffectType.Superscript:
					SetSelectionSuper();
					break;

				default:
					SetSelectionNormal();
					break;
			}
		}
	}

	/// <summary>
	/// Gets or sets the Validator class useded to validate the value
	/// and convert the text when using the Value property.
	/// You can use the ApplyValidatorRules method to apply the settings of
	/// the Validator directly to the ComboBox, for example the list of values.
	/// </summary>
	[DefaultValue(null)]
	public Sphere10.Framework.ComponentModel.IValidator Validator {
		get => _validator;
		set {
			if (_validator != value) {
				if (_validator != null)
					_validator.Changed -= ValidatorChanged;

				_validator = value;
				if (_validator != null)
					_validator.Changed += ValidatorChanged;
				ApplyValidatorRules();
			}
		}
	}

	/// <summary>
	/// Gets or sets the typed value for the control, using the Validator class.
	/// If the Validator is null the Text property is used.
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Sphere10.Framework.Windows.Forms.RichText Value {
		get {
			Sphere10.Framework.Windows.Forms.RichText convertedValue;
			if (IsValidValue(out convertedValue))
				return convertedValue;
			throw new ArgumentOutOfRangeException("Text");
		}
		set {
			if (Validator != null) {
				var richText = (Sphere10.Framework.Windows.Forms.RichText)
					Validator.ValueToObject(value, typeof(Sphere10.Framework.Windows.Forms.RichText));

				if (richText == null)
					Text = string.Empty;
				else
					Rtf = richText.Rtf;
			} else {
				if (value == null)
					Text = string.Empty;
				else
					Text = value.ToString();
			}
		}
	}

	/// <summary>
	/// Returns true when the control is performing some
	/// internal updates
	/// </summary>
	public bool InternalUpdating => (_updating != 0);

	/// <summary>
	/// Maintains performance while updating.
	/// </summary>
	/// <remarks>
	/// <para>
	/// It is recommended to call this method before doing
	/// any major updates that you do not wish the user to
	/// see. Remember to call EndUpdate when you are finished
	/// with the update. Nested calls are supported.
	/// </para>
	/// <para>
	/// Calling this method will prevent redrawing. It will
	/// also setup the event mask of the underlying richedit
	/// control so that no events are sent.
	/// </para>
	/// </remarks>
	public void BeginUpdate() {
		// Deal with nested calls.
		++_updating;

		if (_updating > 1)
			return;

		// Prevent the control from raising any events.
		_oldEventMask = Tools.Windows.Win32.SendMessage(new HandleRef(this, Handle), WinAPI.RICHEDIT.EM_SETEVENTMASK, 0, 0);

		// Prevent the control from redrawing itself.
		Tools.Windows.Win32.SendMessage(new HandleRef(this, Handle), WinAPI.RICHEDIT.WM_SETREDRAW, 0, 0);
	}

	/// <summary>
	/// Resumes drawing and event handling.
	/// </summary>
	/// <remarks>
	/// This method should be called every time a call is made
	/// made to BeginUpdate. It resets the event mask to it's
	/// original value and enables redrawing of the control.
	/// </remarks>
	public void EndUpdate() {
		// Deal with nested calls.
		--_updating;

		if (_updating > 0)
			return;

		// Allow the control to redraw itself.
		Tools.Windows.Win32.SendMessage(new HandleRef(this, Handle), WinAPI.RICHEDIT.WM_SETREDRAW, 1, 0);

		// Allow the control to raise event messages.
		Tools.Windows.Win32.SendMessage(new HandleRef(this, Handle), WinAPI.RICHEDIT.EM_SETEVENTMASK, 0, _oldEventMask);

		// is needed, as otherwise last action has not been performed
		Refresh();
	}

	/// <summary>
	/// Append a text with the specified fontstyle to the rich text box
	/// </summary>
	/// <param name="text">plain text to append</param>
	/// <param name="fontStyle">the fontstyle for the appended text</param>
	public void AppendText(string text, FontStyle fontStyle) {
		var startPosition = this.Text.Length;
		this.AppendText(text);
		this.Select(startPosition, text.Length);
		// The native selection copies the format; this temporary managed font remains locally owned.
		using var selectionFont = new Font(this.Font, fontStyle);
		this.SelectionFont = selectionFont;
	}

	/// <summary>
	/// Remove all formatting, optionally with tabs and line breaks
	/// </summary>
	/// <param name="withWhitespaces">true with tabs and line breaks</param>
	public void RemoveFormats(bool withWhitespaces) {
		var rtfText = Rtf;
		Text = string.Empty;

		if (withWhitespaces)
			Text = RichTextConversion.RichTextToStringStripWhitespaces(new RichText(rtfText));
		else
			Text = RichTextConversion.RichTextToString(new RichText(rtfText));
	}

	/// <summary>
	/// Measure the content of the text box
	/// </summary>
	/// <returns></returns>
	public System.Drawing.SizeF MeasureTextBoxContent(Font font) {
		// The caller retains its font; the control owns only this short-lived measurement surface and format.
		using var graphics = CreateGraphics();
		using var format = new StringFormat();
		int charactersFitted;
		int linesFilled;
		return graphics.MeasureString(Text, font, new SizeF(this.Width, 400.0F), format, out charactersFitted, out linesFilled);
	}

	/// <summary>
	/// Check if the selected value is valid based on the
	/// current validator and returns the value.
	/// </summary>
	/// <param name="convertedValue"></param>
	/// <returns></returns>
	public bool IsValidValue(out Sphere10.Framework.Windows.Forms.RichText convertedValue) {
		if (Validator != null) {
			object convertedRichTextValue = null;
			var success = false;
			if (Validator.IsValidObject(new RichText(this.Rtf), out convertedRichTextValue))
				success = true;

			convertedValue = convertedRichTextValue as Sphere10.Framework.Windows.Forms.RichText;
			return success;
		}
		convertedValue = new RichText(this.Rtf);
		return true;
	}

	/// <summary>
	/// Apply the current Validator rules. This method is automatically
	/// fired when the Validator changed.
	/// </summary>
	protected virtual void ApplyValidatorRules() {
	}

	// Generic validation methods
	protected override void OnValidating(CancelEventArgs args) {
		base.OnValidating(args);

		Sphere10.Framework.Windows.Forms.RichText convertedValue;
		if (IsValidValue(out convertedValue) == false)
			args.Cancel = true;
	}

	/// <summary>Detaches events from the borrowed validator before disposing the native editor.</summary>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			if (_validator != null) {
				_validator.Changed -= ValidatorChanged;
				_validator = null;
			}
		}
		base.Dispose(disposing);
	}

	// Win32 API
	// Applies the selected character format to the native editor.
	protected void SetCharFormatMessage(ref WinAPI.RICHEDIT.CHARFORMAT characterFormat)
		=> Tools.Windows.Win32.SendMessage(new HandleRef(this, Handle), WinAPI.RICHEDIT.EM_SETCHARFORMAT, WinAPI.RICHEDIT.SCF_SELECTION, ref characterFormat);

	/// <summary>
	/// Set the selection to superscript
	/// </summary>
	private void SetSelectionSuper() {
		var characterFormat = Tools.Windows.Win32.CreateCharacterFormat();
		characterFormat.dwMask = WinAPI.RICHEDIT.CFM_SUPERSCRIPT;
		characterFormat.dwEffects |= WinAPI.RICHEDIT.CFE_SUPERSCRIPT;
		SetCharFormatMessage(ref characterFormat);
	}

	/// <summary>
	/// Set the selection to subscript
	/// </summary>
	private void SetSelectionSub() {
		var characterFormat = Tools.Windows.Win32.CreateCharacterFormat();
		characterFormat.dwMask = WinAPI.RICHEDIT.CFM_SUBSCRIPT;
		characterFormat.dwEffects |= WinAPI.RICHEDIT.CFE_SUBSCRIPT;
		SetCharFormatMessage(ref characterFormat);
	}

	/// <summary>
	/// Set the selection to normal
	/// </summary>
	private void SetSelectionNormal() {
		var characterFormat = Tools.Windows.Win32.CreateCharacterFormat();
		characterFormat.dwMask = WinAPI.RICHEDIT.CFM_SUBSCRIPT | WinAPI.RICHEDIT.CFM_SUPERSCRIPT;
		characterFormat.dwEffects &= ~WinAPI.RICHEDIT.CFE_SUPERSCRIPT;
		characterFormat.dwEffects &= ~WinAPI.RICHEDIT.CFE_SUBSCRIPT;
		SetCharFormatMessage(ref characterFormat);
	}

	void ValidatorChanged(object sender, EventArgs args) => ApplyValidatorRules();
}

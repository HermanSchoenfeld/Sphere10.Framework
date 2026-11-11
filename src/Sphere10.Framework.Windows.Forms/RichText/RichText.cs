// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// Class which contains a rich text string.
/// Used to distinguish between string and rich text
/// as normally rich text is also a string.
/// </summary>
[Serializable]
public class RichText : IComparable, IComparable<RichText> {
	// Members
	/// <summary>
	/// RichText
	/// </summary>
	private string _rtf = string.Empty;

	// Constructors
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="rtf"></param>
	public RichText(string rtf) => _rtf = rtf;

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public RichText(RichText other) => Rtf = other.Rtf;

	public string Rtf {
		get => _rtf;
		set => _rtf = value;
	}

	// IComparable Members
	/// <summary>
	/// Compare this instance with a specified RichText object.
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public int CompareTo(object obj) {
		var rtf = obj as RichText;
		return CompareTo(rtf);
	}

	// IComparable<RichText> Members
	/// <summary>
	/// Compare this instance with a specified RichText.
	/// </summary>
	/// <returns></returns>
	public int CompareTo(RichText other) {
		// convert rich text first to plain text and compare that string
		var text = RichTextConversion.RichTextToString(this);
		var otherText = RichTextConversion.RichTextToString(other);
		return text.CompareTo(otherText);
	}

	// Overrides
	/// <summary>
	/// Return richtext as string
	/// </summary>
	/// <returns></returns>
	public override string ToString() => Rtf;
}

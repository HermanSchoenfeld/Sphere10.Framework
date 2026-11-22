// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Marks a string property, or each string of a string collection property, as a file or folder path the user chooses.</summary>
/// <remarks>The entity and settings editors (<see cref="VisualEntityEditor"/>, <see cref="ApplicationSettingsEditor"/>) give a marked row the
/// browse button of a <see cref="PathSelectorControl"/> configured from this attribute, through <see cref="PathSelectorEditor"/>; the path can
/// still be typed into the row. A marked collection keeps its own editor for adding and removing entries, and each entry gets the browse button.</remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
public sealed class PathSelectorAttribute : Attribute {

	public PathSelectorAttribute(PathSelectionMode mode) {
		Mode = mode;
	}

	/// <summary>The dialog the browse button opens, as <see cref="PathSelectorControl.Mode"/>.</summary>
	public PathSelectionMode Mode { get; }

	/// <summary>Whether the dialog requires an existing path, as <see cref="PathSelectorControl.ForcePathExists"/>.</summary>
	public bool ForcePathExists { get; set; }

	/// <summary>Whether the save dialog asks before an existing file is chosen, as <see cref="PathSelectorControl.OverwritePrompt"/>.</summary>
	public bool OverwritePrompt { get; set; } = true;

	/// <summary>The file dialogs' file type filter, such as "Text Files|*.txt|All Files|*.*", or null for every file.</summary>
	public string Filter { get; set; }

	/// <summary>The caption of the dialog, or null for the dialog's own caption.</summary>
	public string DialogTitle { get; set; }
}

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Modal;

/// <summary>
/// Modal options
/// </summary>
public class ModalOptions {
	/// <summary>
	/// Modal size.
	/// </summary>
	public ModalSize Size { get; init; } = ModalSize.Medium;

	/// <summary>
	/// What a click outside the modal, or the Escape key, does. When not set, the modal's own default applies,
	/// whereby a wizard asks for confirmation and every other modal closes.
	/// </summary>
	public ModalBackdropDismissal? BackdropDismissal { get; init; }
}


/// <summary>
/// Modal size
/// </summary>
public enum ModalSize {
	Small,
	Medium,
	Large
}


/// <summary>
/// What a click outside a modal, or the Escape key, does. The modal's close button always closes it.
/// </summary>
public enum ModalBackdropDismissal {
	/// <summary>Closes the modal as its close button does.</summary>
	Close,

	/// <summary>Asks "Are you sure you want to close?" and closes the modal as its close button does on Yes.</summary>
	Confirm,

	/// <summary>Leaves the modal open.</summary>
	Ignore
}

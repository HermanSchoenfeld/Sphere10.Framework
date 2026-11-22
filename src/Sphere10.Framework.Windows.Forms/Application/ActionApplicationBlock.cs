// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A native block whose availability is decided by a delegate supplied at construction.</summary>
/// <remarks>
/// <see cref="ApplicationBlockBuilder"/> builds this block for <c>AvailableWhen</c>. The application asks once, with its own provider, before it
/// registers the block. Availability is presentation only; the application's services still authorize every operation behind the block.
/// </remarks>
public class ActionApplicationBlock : ApplicationBlock {
	private readonly Func<IServiceProvider, bool> _availability;

	/// <summary>Creates an empty block decided by <paramref name="availability"/>.</summary>
	public ActionApplicationBlock(Func<IServiceProvider, bool> availability)
		: this(availability, string.Empty, false, false, null, null, null, null) {
	}

	/// <summary>Creates a block with native presentation, decided by <paramref name="availability"/>.</summary>
	public ActionApplicationBlock(Func<IServiceProvider, bool> availability, string title, bool showInToolStrip, bool showInMenuStrip, Image image32x32, Image image8x8,
		string helpFile, ApplicationMenu[] menus)
		: base(title, showInToolStrip, showInMenuStrip, image32x32, image8x8, helpFile, ValidateAvailability(availability, menus)) {
		_availability = availability;
	}

	/// <inheritdoc />
	protected override bool DetermineAvailability(IServiceProvider services) => _availability(services);

	// Rejects a missing delegate before the base constructor takes ownership of the menus.
	private static ApplicationMenu[] ValidateAvailability(Func<IServiceProvider, bool> availability, ApplicationMenu[] menus) {
		Guard.ArgumentNotNull(availability, nameof(availability));
		return menus;
	}
}

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>A web block whose availability is decided by a delegate supplied at construction.</summary>
/// <remarks>
/// Each circuit asks once, with its own scoped services, before it copies the block. Availability is presentation only; the application's
/// services still authorize every operation behind the block. <see cref="ApplicationBlockBuilder"/> configures the same decision through
/// <c>AvailableWhen</c>.
/// </remarks>
public class ActionApplicationBlock : ApplicationBlock {
	private readonly Func<IServiceProvider, bool> _availability;

	/// <summary>Creates a block decided by <paramref name="availability"/>; its metadata is set through the inherited initializers.</summary>
	public ActionApplicationBlock(Func<IServiceProvider, bool> availability) {
		Guard.ArgumentNotNull(availability, nameof(availability));
		_availability = availability;
	}

	/// <inheritdoc />
	protected override bool DetermineAvailability(IServiceProvider services) => _availability(services);
}

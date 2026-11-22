// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework;

/// <summary>
/// Computes an identifier of the machine the process runs on, so that a service can recognise the same machine across
/// checks, sessions and restarts. Resolve it from the service provider; platform libraries supply the implementation.
/// </summary>
/// <remarks>
/// The identifier is upper-case hexadecimal and the same on every call for an unchanged machine. Core registers no
/// implementation, so a host without a platform implementation has none to resolve.
/// </remarks>
public interface IMachineIdentifierGenerator {

	/// <summary>Returns the machine's identifier as upper-case hexadecimal; never null.</summary>
	/// <exception cref="InvalidOperationException">No identity source of the machine yielded a value.</exception>
	string Generate();

}

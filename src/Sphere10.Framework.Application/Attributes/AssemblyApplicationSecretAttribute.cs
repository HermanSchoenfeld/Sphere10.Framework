// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Application;

/// <summary>
/// The application's one secret. The Application module uses it to encrypt local data (encrypted settings members), and a
/// licensing server that holds the same value can use it for its own purposes, such as opening sealed usage reports.
/// </summary>
[ComVisible(true)]
[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
public sealed class AssemblyApplicationSecretAttribute : Attribute {
	public AssemblyApplicationSecretAttribute(string secret) => Secret = secret;

	public string Secret { get; set; }
}


// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.


using System;
using Sphere10.Framework.Windows;

namespace PackageConsumer.Windows;

// The non-UI Windows packages, which also target .NET 8 and 9.
public static class PackageUsage {
	public static Type WindowsApi => typeof(WinAPI);
}

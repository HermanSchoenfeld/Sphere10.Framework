// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Windows;
using Sphere10.Framework;

namespace Tools;

public static partial class Windows {

	/// <summary>Exposes existing driver-package queries and registered path resolution through Tools.Windows.Drivers.</summary>
	public static class Drivers {
		private static readonly DriverManager _drivers = new();

		/// <summary>Reads installed driver packages on the existing worker query.</summary>
		public static Task<WindowsDriverPackageInfo[]> GetDriverPackagesAsync(CancellationToken cancellationToken = default, TimeSpan? timeout = null)
			=> _drivers.GetDriverPackagesAsync(cancellationToken, timeout);

		/// <summary>Reads installed driver packages synchronously on the calling worker.</summary>
		public static WindowsDriverPackageInfo[] GetDriverPackages(TimeSpan? timeout = null) => _drivers.GetDriverPackages(timeout);

		/// <summary>Resolves an INF name using the existing local Windows-directory validation.</summary>
		public static string ResolveInfPath(string infName, string windowsDirectory = null) => DriverManager.ResolveInfPath(infName, windowsDirectory);

		/// <summary>Resolves a registered driver image using the existing absolute local .sys-file contract.</summary>
		public static string ResolveBinaryPath(string binaryPath, string windowsDirectory = null) => DriverManager.ResolveBinaryPath(binaryPath, windowsDirectory);
	}
}

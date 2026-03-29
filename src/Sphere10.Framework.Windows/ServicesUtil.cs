// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using System.ServiceProcess;

namespace Sphere10.Framework.Windows;

/// <summary>
/// Provides utility methods for querying Windows services.
/// </summary>
public class ServicesUtil {

	public bool IsServiceInstalled(string serviceName)
		=> TryGetServiceController(serviceName, out _);

	public bool TryGetServiceController(string serviceName, out ServiceController serviceController) {
		serviceController = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == serviceName);
		return serviceController != null;
	}

}

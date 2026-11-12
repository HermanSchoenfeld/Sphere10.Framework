// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Application;

public static class AutoRunServicesExtensions {
	/// <summary>
	/// Registers or removes the application's automatic start so it matches <paramref name="enabled"/>, for example a
	/// "Start with Windows" option, and touches the registration only when it differs. Returns whether it changed.
	/// </summary>
	public static bool SetAutoRunEnabled(this IAutoRunServices services, AutoRunType type, string applicationName, string executable, bool enabled, string arguments = null) {
		Guard.ArgumentNotNull(services, nameof(services));
		Guard.ArgumentNotNullOrWhitespace(applicationName, nameof(applicationName));
		Guard.ArgumentNotNullOrWhitespace(executable, nameof(executable));
		var registered = services.DoesAutoRun(type, applicationName, executable);
		if (registered == enabled)
			return false;
		if (enabled)
			services.SetAutoRun(type, applicationName, executable, arguments);
		else
			services.RemoveAutoRun(type, applicationName, executable);
		return true;
	}
}

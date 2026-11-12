// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework.Windows;

/// <summary>Exchanges IPMI messages through the Windows Microsoft_IPMI provider without runtime COM activation.</summary>
public static class WindowsIpmi {

	public static string[] GetInstancePaths(bool activeOnly = false) {
		using var query = Tools.Windows.COM.CreateManagementQuery(TimeSpan.FromMilliseconds(int.MaxValue), @"root\WMI");
		var paths = new List<string>();
		foreach (var instance in query.Read("SELECT * FROM Microsoft_IPMI" + (activeOnly ? " WHERE Active='True'" : string.Empty))) {
			using (instance)
				paths.Add(instance.Path);
		}
		return paths.ToArray();
	}

	/// <summary>Runs one provider command on the calling thread; callers own admission and authorization for hardware writes.</summary>
	public static byte[] RequestResponse(string instancePath, byte networkFunction, byte command, byte[] requestData, byte lun = 0, byte responderAddress = 0x20) {
		Guard.ArgumentNotNull(instancePath, nameof(instancePath));
		Guard.ArgumentNotNull(requestData, nameof(requestData));
		var timeout = TimeSpan.FromMilliseconds(int.MaxValue);
		// Each operation owns its connection so a hardware poll can safely move between worker threads.
		using var query = Tools.Windows.COM.CreateManagementQuery(timeout, @"root\WMI");
		using var instance = query.GetObject(instancePath);
		using var input = instance.GetMethodParameters("RequestResponse");
		input["NetworkFunction"] = networkFunction;
		input["Lun"] = lun;
		input["ResponderAddress"] = responderAddress;
		input["Command"] = command;
		input["RequestDataSize"] = requestData.Length;
		input["RequestData"] = requestData;
		using var output = instance.InvokeMethod("RequestResponse", input, timeout);
		return (byte[])output["ResponseData"];
	}
}

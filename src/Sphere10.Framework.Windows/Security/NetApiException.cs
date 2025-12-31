// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows;

public class NetApiException : WindowsException {

	public NetApiException(WinAPI.NETAPI32.NET_API_STATUS status, string errMsg, params object[] formatArgs) : base((int)status, errMsg, formatArgs) {
	}
}


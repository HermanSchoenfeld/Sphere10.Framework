// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Polyminer
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.DApp.Node.RPC;

public class RpcServerConfig {

	public bool IsLocal { get; set; }
	public int Port { get; set; }
	public int MaxListeners { get; set; }
}


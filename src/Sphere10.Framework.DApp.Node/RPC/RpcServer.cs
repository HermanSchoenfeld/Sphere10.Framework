// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Poylminer
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Communications.RPC;

namespace Sphere10.Framework.DApp.Node.RPC;

//Generic json rpc server singleton for spot calls (Pulse mode)
public class RpcServer {
	static private JsonRpcServer _instance = null;

	static public void SetLogger(ILogger l) => _instance.SetLogger(l);

	static public void Start(bool isLocal, int port, int maxListeners) {
		//Start server()
		_instance = new JsonRpcServer(new TcpEndPointListener(isLocal, port, maxListeners), JsonRpcConfig.Default);
		_instance.Start();
	}

	static public void Stop() {
		_instance.Stop();
		_instance = null;
	}
}


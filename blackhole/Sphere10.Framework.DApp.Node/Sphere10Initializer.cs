// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application;

namespace Sphere10.Framework.DApp.Node;

public interface ISphere10FrameworkA {
}


public class Sphere10Initializer : ApplicationInitializerBase {

	public override void Initialize() {
		SystemLog.RegisterLogger(new TimestampLogger(new ConsoleLogger()));

		//NOTE: Until Sphere10.FrameworkInitializer gets to properly reference CryptoEx module, we init it here.
		//Sphere10.Framework.CryptoEx.Sphere10FrameworkIntegration.Initialize();
		//SystemLog.RegisterLogger(new TimestampLogger(new DebugLogger()));


		//TODO: fetch server's init values from some global config module
		//RpcServer.Start(true, 27000, 32);
	}
}


// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading.Tasks;
using Sphere10.Framework.Communications;

namespace Sphere10.Framework.DApp.Core.Runtime;

public interface IHost {
	event EventHandlerEx<AnonymousPipe> NodeStarted;
	event EventHandlerEx NodeEnded;
	HostStatus Status { get; }
	IApplicationPaths Paths { get; }

	Task DeployHAP(string newHapPath);

	Task Run();

	Task RequestShutdown();
}


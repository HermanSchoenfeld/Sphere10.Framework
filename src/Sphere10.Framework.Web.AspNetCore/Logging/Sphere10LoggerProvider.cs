// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Microsoft.Extensions.Logging;
using Sphere10.Framework;
using SphereLogger = Sphere10.Framework.ILogger;

namespace Sphere10.Framework.Web.AspNetCore;

public class Sphere10LoggerProvider : ILoggerProvider {

	public Sphere10LoggerProvider(SphereLogger logger) {
		Logger = logger;
	}

	protected SphereLogger Logger { get; }

	public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
		=> new MicrosoftExtensionsLoggerAdapter(Logger);

	public void Dispose() {
	}

}


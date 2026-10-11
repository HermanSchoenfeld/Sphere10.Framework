// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Utils.WinFormsTester.Lifecycle;

/// <summary>Shows startup progress for this plugin's demonstrations.</summary>
public sealed class DemoWorkspaceInitializer : AsyncApplicationInitializerBase {
	private readonly IApplication _application;

	public DemoWorkspaceInitializer(IApplication application) => _application = application;

	public override async Task InitializeAsync(CancellationToken cancellationToken = default) {
		_application.Logger.Info("Workspace plugin: preparing screen hosting...");
		await Task.Delay(250, cancellationToken);
		_application.Logger.Info("Workspace plugin: screen hosting is ready.");
	}
}

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

/// <summary>Demonstrates plugin finalization before framework services are disposed.</summary>
public sealed class DemoPluginFinalizer : AsyncApplicationFinalizerBase {
	private readonly IApplication _application;

	public DemoPluginFinalizer(IApplication application) => _application = application;

	public override async Task FinalizeAsync(CancellationToken cancellationToken = default) {
		_application.Logger.Info("Workspace plugin: finalizing demonstration components...");
		await Task.Delay(500, cancellationToken);
		_application.Logger.Info("Workspace plugin: demonstration components are finalized.");
	}
}

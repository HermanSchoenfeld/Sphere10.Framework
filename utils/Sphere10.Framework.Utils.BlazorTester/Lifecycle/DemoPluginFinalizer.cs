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
using Sphere10.Framework.Utils.BlazorTester.Application;

namespace Sphere10.Framework.Utils.BlazorTester.Lifecycle;

/// <summary>Demonstrates plugin shutdown when the current application exits or its circuit closes.</summary>
public sealed class DemoPluginFinalizer : AsyncApplicationFinalizerBase {
	private readonly IApplication _application;
	private readonly WorkspaceStatus _status;
	private readonly string _pluginName;

	public DemoPluginFinalizer(IApplication application, WorkspaceStatus status, string pluginName) {
		Guard.ArgumentNotNull(application, nameof(application));
		Guard.ArgumentNotNull(status, nameof(status));
		Guard.Argument(!string.IsNullOrWhiteSpace(pluginName), nameof(pluginName), "A plugin name is required.");
		_application = application;
		_status = status;
		_pluginName = pluginName;
	}

	public override async Task FinalizeAsync(CancellationToken cancellationToken = default) {
		_application.Logger.Info($"{_pluginName}: finishing demonstration components...");
		await Task.Delay(50, cancellationToken);
		_status.RecordPluginFinalized(_pluginName);
		_application.Logger.Info($"{_pluginName}: demonstration components are finalized.");
	}
}

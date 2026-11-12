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

/// <summary>Shows shutdown progress for this plugin's demonstrations.</summary>
public sealed class DemoComponentFinalizer : AsyncApplicationFinalizerBase {
	private readonly IApplication _application;
	private readonly DemoLogOutputLifetime _outputs;

	public DemoComponentFinalizer(IApplication application, DemoLogOutputLifetime outputs) {
		_application = application;
		_outputs = outputs;
	}

	public override async Task FinalizeAsync(CancellationToken cancellationToken = default) {
		_application.Logger.Info("Component plugin: finalizing controls and collections...");
		await _outputs.CompleteAsync();
		await Task.Delay(250, cancellationToken);
		_application.Logger.Info("Component plugin: controls and collections are finalized.");
	}
}

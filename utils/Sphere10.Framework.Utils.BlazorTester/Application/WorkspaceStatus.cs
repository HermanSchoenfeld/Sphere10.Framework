// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework.Utils.BlazorTester.Application;

public class WorkspaceStatus {
	public event EventHandlerEx Changed;

	private readonly List<string> _initializedPlugins = new();
	private readonly List<string> _finalizedPlugins = new();

	public int ActionCount { get; private set; }

	public bool ActivateBlockOnSelection { get; private set; }

	public string[] InitializedPlugins => _initializedPlugins.ToArray();

	public string[] FinalizedPlugins => _finalizedPlugins.ToArray();

	public void RecordPluginInitialized(string pluginName) {
		Guard.Argument(!string.IsNullOrWhiteSpace(pluginName), nameof(pluginName), "A plugin name is required.");
		_initializedPlugins.Add(pluginName);
		Changed?.Invoke();
	}

	public void RecordPluginFinalized(string pluginName) {
		Guard.Argument(!string.IsNullOrWhiteSpace(pluginName), nameof(pluginName), "A plugin name is required.");
		_finalizedPlugins.Add(pluginName);
		Changed?.Invoke();
	}

	public void SetBlockActivation(bool activate) {
		if (ActivateBlockOnSelection == activate)
			return;
		ActivateBlockOnSelection = activate;
		Changed?.Invoke();
	}

	public void Increment() {
		ActionCount++;
		Changed?.Invoke();
	}
}

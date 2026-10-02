// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Application.UI;

/// <summary>A running UI application's plugins, registered blocks, active selection and application-level commands.</summary>
/// <remarks>Platform adapters preserve the host's thread affinity and own only their event subscriptions, not screens or registered definitions.</remarks>
public interface IApplication : IApplicationCommandProvider, IDisposable, IAsyncDisposable {
	event EventHandlerEx Changed;

	IApplicationPlugin[] Plugins { get; }

	IApplicationPlugin ActivePlugin { get; }

	IApplicationBlock[] Blocks { get; }

	/// <summary>The block selected for navigation; browsing it need not activate one of its screens.</summary>
	IApplicationBlock ActiveBlock { get; }

	IApplicationScreen ActiveScreen { get; }

	bool HasUnsavedChanges { get; }
}

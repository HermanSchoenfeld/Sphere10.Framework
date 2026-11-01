// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Windows.Forms;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>The owning application, block, host and services available when a registered native screen is created.</summary>
public sealed class WinFormsApplicationScreenActivationContext {
	// Captures the current native host context without creating or owning services.
	internal WinFormsApplicationScreenActivationContext(IWinFormsApplicationBlock block, WinFormsApplicationScreenHost host,
		ApplicationScreenDefinition definition, IServiceProvider services, Form owner) {
		Block = block;
		Host = host;
		Definition = definition;
		Services = services;
		Owner = owner;
	}

	/// <summary>The application currently owning the registered block, or null for a standalone host.</summary>
	public IWinFormsApplication Application {
		get {
			return Block.Parent ?? (Owner as BlockMainForm)?.Application as IWinFormsApplication;
		}
	}

	/// <summary>The registration's owning block.</summary>
	public IWinFormsApplicationBlock Block { get; }

	/// <summary>The UI-thread host requesting the new screen.</summary>
	public WinFormsApplicationScreenHost Host { get; }

	/// <summary>The exact canonical registration being activated.</summary>
	public ApplicationScreenDefinition Definition { get; }

	/// <summary>The application's provider, or null when a standalone host has no application services.</summary>
	public IServiceProvider Services { get; }

	/// <summary>The form containing the host, when it is attached to a form.</summary>
	public Form Owner { get; }
}

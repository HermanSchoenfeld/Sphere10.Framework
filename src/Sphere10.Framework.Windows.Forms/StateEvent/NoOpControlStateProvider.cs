// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A design-time observer that acquires no native state or resources.</summary>
public sealed class NoOpControlStateProvider : IControlStateEventProvider {
	/// <summary>Accepts subscriptions without publishing operational or design-time changes.</summary>
	public event EventHandlerEx StateChanged {
		add {
		}
		remove {
		}
	}

	/// <summary>The general control contract accepted by this no-op observer.</summary>
	public Type ControlType {
		get {
			return typeof(Control);
		}
	}

	/// <summary>Releases no resources because none were acquired.</summary>
	public void Clear() {
		// do nothing
	}

	/// <summary>Accepts a borrowed design-time control without creating its handle.</summary>
	public void SetControl(Control control) {
		// do nothing
	}
}

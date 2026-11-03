// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Hosts an owned LoadingCircle in a native tool strip or menu.</summary>
[ToolStripItemDesignerAvailability(ToolStripItemDesignerAvailability.All)]
public class LoadingCircleToolStripMenuItem : ToolStripControlHost {
	/// <summary>Creates the indicator owned and disposed by this native control host.</summary>
	public LoadingCircleToolStripMenuItem()
		: base(new LoadingCircle()) {
	}

	/// <summary>The hosted indicator, including its editable geometry, style and colour.</summary>
	[RefreshProperties(RefreshProperties.All)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
	public LoadingCircle LoadingCircleControl => (LoadingCircle)Control;

	/// <summary>Uses the indicator's complete square preferred size for native tool-strip layout.</summary>
	public override Size GetPreferredSize(Size constrainingSize) {
		return LoadingCircleControl.GetPreferredSize(constrainingSize);
	}

	/// <summary>Retains the native host's standard control-event subscription behavior.</summary>
	protected override void OnSubscribeControlEvents(Control control) {
		base.OnSubscribeControlEvents(control);
	}

	/// <summary>Retains the native host's standard control-event cleanup behavior.</summary>
	protected override void OnUnsubscribeControlEvents(Control control) {
		base.OnUnsubscribeControlEvents(control);
	}
}

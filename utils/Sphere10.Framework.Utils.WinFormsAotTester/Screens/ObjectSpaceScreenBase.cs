// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

// The inherited declaration preserves ObjectSpaceScreen.ScreenSettings, the nested type identifying its legacy provider record.
public class ObjectSpaceScreenBase : ApplicationScreen {
	/// <summary>The typed preferences inherited by the concrete designer screen.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override ObjectSpaceScreen.ObjectSpaceScreenSettings ScreenSettings => (ObjectSpaceScreen.ObjectSpaceScreenSettings)base.ScreenSettings;
}

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Security.Permissions;
using System.Windows.Forms;
using System.Windows.Forms.Design;


namespace Sphere10.Framework.Windows.Forms;

// Keeps unsupported native ComboBox properties out of the custom dropdown designer.
internal class CustomComboBoxDesigner : ParentControlDesigner {
	// ParentControlDesigner Overrides
	protected override void PreFilterProperties(IDictionary properties) {
		base.PreFilterProperties(properties);
		properties.Remove("DropDownStyle");
		properties.Remove("Items");
		properties.Remove("ItemHeight");
		properties.Remove("MaxDropDownItems");
		properties.Remove("DisplayMember");
		properties.Remove("ValueMember");
		properties.Remove("DropDownWidth");
		properties.Remove("DropDownHeight");
		properties.Remove("IntegralHeight");
		properties.Remove("Sorted");
	}
}

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms.Application;

public class DefaultApplicationIconProvider : IApplicationIconProvider {
	public Icon ApplicationIcon {
		get {
			var icon = Sphere10Framework.Instance.GetAppIcon();
			if (icon != null)
				return icon;

			var serviceProvider = Sphere10Framework.Instance.ServiceProvider;
			if (serviceProvider != null) {
				var mainForm = serviceProvider.GetService(typeof(IMainForm)) as Form;
				if (mainForm != null && mainForm.Icon != null)
					return mainForm.Icon;
			}

			return null;
		}
	}
}

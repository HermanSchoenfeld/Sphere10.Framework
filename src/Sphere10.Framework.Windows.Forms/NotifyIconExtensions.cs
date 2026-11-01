// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Reflection;
using System.Windows.Forms;

namespace Sphere10.Framework;

public static class NotifyIconExtensions {

	public static void EnableContextMenuOnLeftClick(this NotifyIcon notifyIcon) {
		notifyIcon.MouseUp += (sender, args) => {
			if (args.Button == MouseButtons.Left) {
				MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
				mi.Invoke(notifyIcon, null);
			}
		};
	}
}


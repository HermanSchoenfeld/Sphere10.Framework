// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.Windows.Forms;

public class CHMHelpProvider : IHelpServices {
	private static readonly object SyncObject;
	private string _chmFile;

	static CHMHelpProvider() {
		SyncObject = new object();
	}

	public CHMHelpProvider(IUserInterfaceServices userInterfaceServices, IProductInformationProvider productInformationProvider) {

		UserInterfaceServices = userInterfaceServices;
		ProductInformationProvider = productInformationProvider;
		CHMFile = null;


	}

	public IUserInterfaceServices UserInterfaceServices { get; private set; }

	public IProductInformationProvider ProductInformationProvider { get; private set; }

	public string CHMFile {
		get {
			if (_chmFile == null) {
				lock (SyncObject) {
					if (_chmFile == null) {
						var chmQuery = ProductInformationProvider.ProductInformation.HelpResources.Where(hr => hr.Item1 == HelpType.CHM);
						if (!chmQuery.Any()) {
							throw new SoftwareException("No default CHM help file is defined");
						}
						CHMFile = chmQuery.First().Item2;
					}
				}
			}
			return _chmFile;
		}
		private set {
			if (value == null) {
				_chmFile = null;
			} else {
				_chmFile = StringFormatter.FormatEx(value);
				if (!File.Exists(_chmFile)) {
					throw new SoftwareException("File does not exist '{0}'", _chmFile);
				}
			}
		}
	}

	public void ShowContextHelp(IHelpableObject helpableObject) {
		Guard.ArgumentNotNull(helpableObject, nameof(helpableObject));
		var contextControl = helpableObject as Control;
		var owner = contextControl != null && !contextControl.IsDisposed ? contextControl : UserInterfaceServices.PrimaryUIController as Control;
		ShowHelp(owner?.FindForm() ?? owner, File.Exists(helpableObject.FileName) ? helpableObject.FileName : CHMFile, helpableObject.HelpTopicID.Value);
	}

	public void ShowHelp() {
		var owner = UserInterfaceServices.PrimaryUIController as Control;
		ShowHelp(owner?.FindForm() ?? owner, CHMFile, null);
	}

	protected virtual void ShowHelp(Control owner, string file, int? topicId) {
		// HTML Help inherits the creation thread's awareness. Keep every native rectangle in physical desktop pixels.
		var caller = owner != null && !owner.IsDisposed && !owner.Disposing ? owner.Handle : IntPtr.Zero;
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		var monitor = caller != IntPtr.Zero ? WinAPI.USER32.MonitorFromWindow(caller, 2) : IntPtr.Zero;
		var information = new WinAPI.USER32.MONITORINFO { Size = Marshal.SizeOf<WinAPI.USER32.MONITORINFO>() };
		var ownerBounds = default(WinAPI.RECT);
		var hasMonitor = monitor != IntPtr.Zero && WinAPI.USER32.GetMonitorInfo(monitor, ref information) && WinAPI.USER32.GetWindowRect(caller, out ownerBounds);
		// Use the exact window returned by HTML Help; never search unrelated desktop windows by caption or class.
		var command = topicId.HasValue ? WinAPI.HHCTRL.HH_HELP_CONTEXT : WinAPI.HHCTRL.HH_DISPLAY_TOPIC;
		var data = topicId.HasValue ? new UIntPtr(unchecked((uint)topicId.Value)) : UIntPtr.Zero;
		var window = WinAPI.HHCTRL.HtmlHelp(caller, file, command, data);
		Guard.Ensure(window != IntPtr.Zero, $"The help file could not be opened: '{file}'.");
		if (!hasMonitor || WinAPI.USER32.MonitorFromWindow(window, 2) == monitor)
			return;

		// HTML Help remembers its own monitor. Move its returned window before completing the help request.
		var maximized = WinAPI.USER32.IsZoomed(window);
		if (maximized || WinAPI.USER32.IsIconic(window))
			WinAPI.USER32.ShowWindow(window, WinAPI.USER32.ShowWindowCommands.Restore);
		if (!WinAPI.USER32.GetWindowRect(window, out var currentBounds))
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "The help window bounds could not be read.");
		var bounds = GetHelpWindowBounds(information.WorkingArea.ToRectangle(), ownerBounds.ToRectangle(), currentBounds.ToRectangle().Size);
		if (!WinAPI.USER32.SetWindowPos(window, IntPtr.Zero, bounds.X, bounds.Y, bounds.Width, bounds.Height, WinAPI.USER32.SetWindowPosFlags.SWP_NOZORDER))
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "The help window could not be placed on its owner's monitor.");
		if (maximized)
			WinAPI.USER32.ShowWindow(window, WinAPI.USER32.ShowWindowCommands.ShowMaximized);
	}

	protected static Rectangle GetHelpWindowBounds(Rectangle workingArea, Rectangle ownerBounds, Size helpSize) {
		var width = Math.Min(helpSize.Width > 0 ? helpSize.Width : 1000, workingArea.Width);
		var height = Math.Min(helpSize.Height > 0 ? helpSize.Height : 700, workingArea.Height);
		var x = Math.Clamp(ownerBounds.Left + (ownerBounds.Width - width) / 2, workingArea.Left, workingArea.Right - width);
		var y = Math.Clamp(ownerBounds.Top + (ownerBounds.Height - height) / 2, workingArea.Top, workingArea.Bottom - height);
		return new Rectangle(x, y, width, height);
	}


}


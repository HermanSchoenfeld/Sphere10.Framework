// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

public static partial class WinAPI {
	public static class POWRPROF {
		[DllImport("powrprof.dll", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.U1)]
		public static extern bool SetSuspendState([MarshalAs(UnmanagedType.U1)] bool hibernate, [MarshalAs(UnmanagedType.U1)] bool force, [MarshalAs(UnmanagedType.U1)] bool disableWakeEvents);

		[DllImport("powrprof.dll", ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.U1)]
		public static extern bool IsPwrSuspendAllowed();

		[DllImport("powrprof.dll", ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.U1)]
		public static extern bool IsPwrHibernateAllowed();

		[DllImport("powrprof.dll", ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.U1)]
		public static extern bool IsPwrShutdownAllowed();
	}
}


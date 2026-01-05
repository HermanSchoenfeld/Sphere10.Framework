// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using Foundation;
using Sphere10.Framework;

namespace Sphere10.Framework.iOS {

	public static class AssemblyExtensions {
	
		public static NSData FromResource(this Assembly assembly, string name) {
			if (name == null)
				throw new ArgumentNullException("name");

			using (var stream = assembly.GetManifestResourceStream(name)) {
				if (stream == null) {
					throw new SoftwareException("No embedded resource called '{0}' was found in assembly '{1}", name, assembly.FullName);
				}

				var buffer = Marshal.AllocHGlobal((int)stream.Length);
				try {
					if (buffer == IntPtr.Zero)
						return null;

					var copyBuffer = new byte[Math.Min(1024, (int)stream.Length)];
					int n;
					var target = buffer;
					while ((n = stream.Read(copyBuffer, 0, copyBuffer.Length)) != 0) {
						Marshal.Copy(copyBuffer, 0, target, n);
						target = (IntPtr)((int)target + n);
					}
					return (NSData)(NSData.FromBytes(buffer, (uint)stream.Length));
				} finally {
					if (buffer != IntPtr.Zero)
						Marshal.FreeHGlobal(buffer);
				}
			}
		}

	}
}


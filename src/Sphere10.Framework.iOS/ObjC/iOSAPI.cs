// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Foundation;
using ObjCRuntime;


namespace Sphere10.Framework.iOS {
    public static class iOSAPI {
        [DllImport(Constants.ObjectiveCLibrary)]
        public static extern void objc_setAssociatedObject(IntPtr @object, IntPtr key, IntPtr value, AssociationPolicy policy);

        [DllImport(Constants.ObjectiveCLibrary)]
        public static extern IntPtr objc_getAssociatedObject(IntPtr @object, IntPtr key);

        [DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
        public static extern void void_objc_msgSend_int(IntPtr deviceHandle, IntPtr setterHandle, nint val);

        [DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
        public static extern void void_objc_msgSend_float(IntPtr deviceHandle, IntPtr setterHandle, nfloat val);

        [DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
        public static extern IntPtr intptr_objc_msgSend(IntPtr tokenHandle, IntPtr selectorHandle);
    }
}



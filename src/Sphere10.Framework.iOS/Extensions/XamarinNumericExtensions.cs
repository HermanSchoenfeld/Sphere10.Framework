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

namespace Sphere10.Framework.iOS {
    public static class XamarinNumericExtensions {

        public static nfloat ClipTo(this nfloat value, nfloat min, nfloat max) {
            if (value < min) {
                return min;
            } else if (value > max) {
                return max;
            }
            return value;
        }


        public static nint ClipTo(this nint value, nint min, nint max) {
            if (value < min) {
                return min;
            } else if (value > max) {
                return max;
            }
            return value;
        }

        public static nuint ClipTo(this nuint value, nuint min, nuint max) {
            if (value < min) {
                return min;
            } else if (value > max) {
                return max;
            }
            return value;
        }


        public static void Repeat(this nint times, Action action) {
            for (var i = 0; i < times; i++) {
                action();
            }
        }

    }
}


// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using CoreGraphics;

namespace Sphere10.Framework.iOS {

    public static class SizeExtensions {

        public static CGSize ToCGSize(this Size size) {
            return new CGSize(size.Height, size.Height);
        }

    }
}


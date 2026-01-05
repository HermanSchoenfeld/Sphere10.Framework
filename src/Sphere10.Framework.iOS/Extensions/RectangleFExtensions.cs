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
    public static class RectangleFFExtensions {
        public static CGRect ToCGRect(this RectangleF rectangle) {
            return new CGRect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        }

    }
}


// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using CoreGraphics;

namespace Sphere10.Framework.iOS {

    public static class PointExtensions {
        public static CGPoint ToCGPoint(this Point point) {
            return new CGPoint(point.X, point.Y);
        }
    }
}


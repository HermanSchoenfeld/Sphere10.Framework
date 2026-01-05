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


namespace Sphere10.Framework {

    public static class CGPointExtensions {

        public static Point ToPoint(this CGPoint point) {
            return new Point((int)point.X, (int)point.Y);
        }

        public static PointF ToPointF(this CGPoint point) {
            return new PointF((float)point.X, (float)point.Y);
        }

        public static float DistanceTo(this CGPoint source, CGPoint dest) {
            return (float)Math.Sqrt(Math.Pow(dest.X - source.X, 2.0) + Math.Pow(dest.Y - source.Y, 2.0));
        }

        public static CGPoint Subtract(this CGPoint orgPoint, CGPoint point) {
            var x = orgPoint.X - point.X;
            var y = orgPoint.Y - point.Y;
            return new CGPoint(x, y);
        }
        public static CGPoint Add(this CGPoint orgPoint, CGPoint point) {
            var x = orgPoint.X + point.X;
            var y = orgPoint.Y + point.Y;
            return new CGPoint(x, y);
        }

        public static CGPoint Add(this CGPoint orgPoint, CGSize size) {
            var x = orgPoint.X + size.Width;
            var y = orgPoint.Y + size.Height;
            return new CGPoint(x, y);
        }
    }
}


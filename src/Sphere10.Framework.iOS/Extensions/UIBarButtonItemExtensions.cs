// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using CoreGraphics;
using UIKit;

namespace Sphere10.Framework.iOS {
    public static class UIBarButtonItemExtensions {


        public static void Hide(this UIBarButtonItem button) {
            button.Enabled = false;
            button.TintColor = UIColor.Clear;
        }

        public static void Show(this UIBarButtonItem button) {
            button.Enabled = true;
            button.TintColor = null;
        }

    }
}



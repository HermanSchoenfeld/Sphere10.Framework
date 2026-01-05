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
using Sphere10.Framework;
using System.Linq;

namespace Sphere10.Framework.iOS {
    public static class UINavigationItemExtensions {


        public static void AddLeftItem(this UINavigationItem navigationItem, UIBarButtonItem item, bool skipIfAlreadyExists = true) {
            if (navigationItem.LeftBarButtonItem == null)
                navigationItem.LeftBarButtonItem = item;
            else
                navigationItem.LeftBarButtonItems =
                    skipIfAlreadyExists ?
                    navigationItem.LeftBarButtonItems.Union(item).ToArray() :  
                    Tools.Array.ConcatArrays(navigationItem.LeftBarButtonItems, item);
        }


    }
}



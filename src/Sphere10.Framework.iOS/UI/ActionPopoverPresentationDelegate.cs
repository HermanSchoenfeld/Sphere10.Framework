// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using UIKit;

namespace Sphere10.Framework.iOS {
    public class ActionPopoverPresentationDelegate : UIPopoverPresentationControllerDelegate {
        private readonly Action _didDismissPopoverAction;


        public ActionPopoverPresentationDelegate(Action didDismissPopoverAction = null) {
            _didDismissPopoverAction = didDismissPopoverAction ?? (() => Tools.Lambda.NoOp());
        }


        public override void DidDismissPopover(UIPopoverPresentationController popoverPresentationController) {
            _didDismissPopoverAction();
        }
    }
}



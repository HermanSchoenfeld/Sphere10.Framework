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

using Foundation;
using UIKit;

namespace Sphere10.Framework.iOS {
    public enum AssociationPolicy {
        ASSIGN = 0,
        RETAIN_NONATOMIC = 1,
        COPY_NONATOMIC = 3,
        RETAIN = 01401,
        COPY = 01403,
    }
}


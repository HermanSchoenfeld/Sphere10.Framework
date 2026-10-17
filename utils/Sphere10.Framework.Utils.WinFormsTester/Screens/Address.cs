// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application.UI;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Windows.Forms.Crud;

namespace Sphere10.Framework.Utils.WinFormsTester;


public class Address {
	public string Street { get; set; }
	public string City { get; set; }
	public string PostCode { get; set; }
	public string State { get; set; }
}

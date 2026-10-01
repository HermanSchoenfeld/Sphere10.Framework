// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Logic;

public interface IApplicationMenu {
	string Id => Text;
	string Icon { get; }
	string Text { get; }
	IEnumerable<IApplicationMenuItem> Items { get; }
}

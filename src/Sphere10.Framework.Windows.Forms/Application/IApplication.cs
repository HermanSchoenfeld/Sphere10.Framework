// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>The native application aggregate over the existing block manager and screen host.</summary>
public interface IApplication : SharedUI.IApplication<IApplicationPlugin, IApplicationScreenHost>,
	IApplicationCommandProvider<IApplicationMenu, IApplicationMenuItem> {
	IBlockManager BlockManager { get; }

	new IApplicationBlock[] Blocks { get; }

	#region Base Interface Bridges

	SharedUI.IApplicationBlock[] SharedUI.IApplication.Blocks => Blocks;

	#endregion

}

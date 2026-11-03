// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>The native application aggregate over the existing block manager and screen host.</summary>
public interface IWinFormsApplication : IApplication<IWinFormsApplicationPlugin, IWinFormsApplicationScreenHost>,
	IApplicationCommandProvider<IWinFormsApplicationMenu, IWinFormsApplicationMenuItem> {
	IBlockManager BlockManager { get; }

	new IWinFormsApplicationBlock[] Blocks { get; }

	#region Base Interface Bridges

	IApplicationBlock[] IApplication.Blocks => Blocks;

	#endregion

}

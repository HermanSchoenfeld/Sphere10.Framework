// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms;

[Flags]
public enum FormCloseAction {
	Nothing = 0x00000000,
	Close = 0x00000001,
	Hide = 0x00000010,
	Minimize = 0x00000100,
	HideMinimize = 0x00000110,
}


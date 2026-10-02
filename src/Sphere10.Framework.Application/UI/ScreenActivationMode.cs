// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Application.UI;

public enum ScreenActivationMode {
	SingleInstance = 0,
	MultiInstance = 1,

	/// <summary>One automatically opened instance whose tab cannot be closed during its block lifetime.</summary>
	PermanentSingleton = 2
}

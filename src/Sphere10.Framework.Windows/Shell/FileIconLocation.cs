// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Shell;

/// <summary>A local executable, DLL or icon file and its zero-based icon index, or negative resource identifier.</summary>
public readonly record struct FileIconLocation(string FilePath, int IconIndex = 0);

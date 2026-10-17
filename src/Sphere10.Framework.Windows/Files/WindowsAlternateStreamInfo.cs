// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Files;

/// <summary>A named $DATA stream. The unnamed primary file contents are excluded.</summary>
public sealed record WindowsAlternateStreamInfo(string FilePath, string Name, long Length);

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.VisualRenderer;

/// <summary>An output resource. RelativePath is relative to the configured asset base URL.</summary>
public sealed record RenderAsset(string RelativePath, string ContentType, ReadOnlyMemory<byte> Content);

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows;

/// <summary>A driver-enumerated mode for a GDI source; these are not a panel's complete EDID timing list.</summary>
public sealed record WindowsDisplayMode(uint Width, uint Height, uint BitsPerPixel, uint RefreshRateHz, bool Interlaced);

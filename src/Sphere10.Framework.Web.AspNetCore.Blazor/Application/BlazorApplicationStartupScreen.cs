// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>A version-independent identity resolved only against the current circuit's registered screen definitions.</summary>
public sealed record BlazorApplicationStartupScreen(string PluginName, string BlockId, string MenuItemId, string ScreenTypeName, string ScreenAssemblyName);

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>Reports an operation's title, current task, progress counts and percentage from zero to one hundred.</summary>
public delegate void ProgressHandler(string title, string message, int? itemsToComplete,  int? totalItemsToComplete, decimal percentComplete);


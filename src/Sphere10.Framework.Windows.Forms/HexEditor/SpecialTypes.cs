// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

/// <summary>One named interpretation of the bytes at the caret, formatted for display.</summary>
public sealed record HexEditorInspectionEntry(string Name, string Value);

/// <summary>A resolved pointer position inside a hex editor view.</summary>
public sealed record HexEditorHitTestResult(HexEditorPane Pane, long Offset, int Nibble);

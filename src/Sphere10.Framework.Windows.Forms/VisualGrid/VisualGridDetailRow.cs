// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Captures a displayed node, its stable key, child-query key and expansion eligibility for binding.</summary>
internal sealed record VisualGridDetailRow(object Entity, object Identity, object QueryIdentity, bool Expandable);

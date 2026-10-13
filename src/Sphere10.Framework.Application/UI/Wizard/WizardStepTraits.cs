// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Application.UI;

[Flags]
public enum WizardStepTraits {
	None = 0,

	/// <summary>The step supplies a successor during navigation and must display Next even before that successor is registered.</summary>
	AssumeHasNext = 1
}

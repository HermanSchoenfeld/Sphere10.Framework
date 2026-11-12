// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

/// <summary>How a <see cref="WinFormsWizardBase{T}"/> sizes its dialog to its steps.</summary>
public enum WizardStepSizing {
	/// <summary>One size for every step: the largest step's size, measured when the wizard starts. The default.</summary>
	LargestStep,

	/// <summary>Each step at its own size; the dialog resizes about its centre as the steps change.</summary>
	EachStep
}

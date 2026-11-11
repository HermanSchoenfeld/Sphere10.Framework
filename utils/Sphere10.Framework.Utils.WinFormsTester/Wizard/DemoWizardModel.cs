// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Utils.WinFormsTester.Wizard;

/// <summary>Retains the current wizard session's validated input without writing application settings.</summary>
public class DemoWizardModel {
	/// <summary>The trimmed name collected on the first page.</summary>
	public string Name {
		get; set;
	}

	/// <summary>The age collected on the second page; null denotes an unanswered field.</summary>
	public int? Age {
		get; set;
	}

	/// <summary>Creates an independent unanswered session for each wizard invocation.</summary>
	public static DemoWizardModel Default => new DemoWizardModel { Name = string.Empty };
}

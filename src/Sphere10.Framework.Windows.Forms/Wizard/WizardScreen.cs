// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Forms;

public class WizardScreen<T> : UserControlEx, IWizardScreen<T> {
	public IWizard<T> Wizard { get; internal set; }

	public T Model => Wizard.Model;

	public virtual async Task Initialize() {
	}

	public virtual async Task OnPresent() {
		CopyModelToUI();
	}

	public virtual async Task OnPrevious() {
	}

	public virtual async Task OnNext() {
	}

	public virtual async Task<Result> Validate() {
		return Result.Default;
	}

}


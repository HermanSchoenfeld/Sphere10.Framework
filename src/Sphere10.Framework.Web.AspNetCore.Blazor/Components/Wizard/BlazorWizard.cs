// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Components.Wizard;

/// <summary>Adapts the portable wizard engine to Blazor step component types.</summary>
public class BlazorWizard<TModel> : Wizard<TModel, Type>, IBlazorWizard<TModel> {
	public BlazorWizard(string title, IEnumerable<Type> steps, TModel modal,
		Func<TModel, Task<Result<bool>>> onFinish = null, Func<TModel, Task<Result<bool>>> onCancel = null
	)
		: base(title, modal, steps, onFinish, onCancel) {
		Guard.ArgumentNotNull(modal, nameof(modal));
	}

	public Result<bool> Next() => MoveNext();

	public Result<bool> Previous() => MovePrevious();
}

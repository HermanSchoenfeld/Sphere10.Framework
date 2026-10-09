// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application.UI;

/// <summary>Portable fluent wizard configuration, with platform-specific step types and product interfaces.</summary>
public interface IWizardBuilder<TSelf, TModel, TStep, out TWizard> : IApplicationUIBuilder<TWizard> {
	TSelf NewWizard(string title);

	TSelf WithTitle(string title);

	TSelf WithModel(TModel model);

	TSelf WithCancellation(bool isCancellable);

	TSelf AddStep(TStep step);

	TSelf OnFinished(Func<TModel, Task<Result<bool>>> onFinished);

	TSelf OnCancelled(Func<TModel, Task<Result<bool>>> onCancelled);

	TSelf OnFinished(Func<TModel, Task<Result>> onFinished);

	TSelf OnCancelled(Func<TModel, Result> onCancelled);
}

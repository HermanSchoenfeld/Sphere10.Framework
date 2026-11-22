// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Process finalizer that stops the <see cref="ProductUpdatePresenter"/>, so no notice is posted to a closing application.</summary>
public class ProductUpdatePresenterFinalizer : SyncApplicationFinalizerBase {

	public ProductUpdatePresenterFinalizer(ProductUpdatePresenter presenter) {
		Guard.ArgumentNotNull(presenter, nameof(presenter));
		Presenter = presenter;
	}

	protected ProductUpdatePresenter Presenter { get; }

#pragma warning disable CS0465 // Implements the existing task contract, not a CLR finalizer.
	public override void Finalize() => Presenter.Stop();
#pragma warning restore CS0465
}

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

/// <summary>What became of a request to <see cref="ProductUpdatePresenter"/> to present an update.</summary>
public enum ProductUpdateNoticeResult {

	/// <summary>No notice was shown, because another notice was open or the application was exiting. The update can be offered again.</summary>
	NotShown,

	/// <summary>The user saw the notice and did not install the update.</summary>
	Declined,

	/// <summary>
	/// The user accepted the update and the updater took it over: the release server's installer was started and the application was
	/// asked to exit, or a channel such as the Windows Store installed it and restarts the application itself.
	/// </summary>
	Installing
}

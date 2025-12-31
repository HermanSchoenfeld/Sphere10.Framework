// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

public interface IApplicationDialog {

	DialogResult ShowDialog();

	DialogResult ShowDialog(IWin32Window parent);

	FormStartPosition StartPosition { get; set; }

	bool Visible { get; set; }

	void Refresh();

}


// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Forms;

public interface ICrudGrid {
	Type EntityEditorDisplay { get; }
	string GridTitle { get; }
	DataSourceCapabilities Capabilities { get; set; }
	IEnumerable<ICrudGridColumn> GridBindings { get; }

	Task SetDataSource<TEntity>(IDataSource<TEntity> dataSource);
}


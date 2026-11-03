// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

//
//	public class CellArrayEventArgs : EventArgs
//	{
//		private Cell[] m_Cell;
//		public CellArrayEventArgs(Cell[] p_Cell)
//		{
//			m_Cell = p_Cell;
//		}
//
//		public Cell[] Cells
//		{
//			get{return m_Cell;}
//			set{m_Cell = value;}
//		}
//
//	}

public class RowInfoEventArgs : EventArgs {
	public RowInfoEventArgs(RowInfo rowInfo) {
		Row = rowInfo;
	}

	public RowInfo Row { get; }
}

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>
/// A data source that can serve its tree as an outline: one list in tree order, where each row knows its depth and whether the rows beneath it
/// are shown. A grid draws the depth as indentation and an expander, and changes what is shown through this interface.
/// </summary>
/// <remarks>
/// The rows are the source's own rows, as read. Expansion is kept by node key, so it survives a refresh that reads new row objects; a change takes
/// effect on the next read. One table shows a whole tree this way, however many nodes it has, where nested child tables need a control per node.
/// </remarks>
public interface IHierarchyOutline {

	/// <summary>Whether reads currently serve the outline, rather than another presentation of the same rows.</summary>
	bool IsOutline { get; }

	/// <summary>How many levels lie above the row in the last read: zero for a top-level row.</summary>
	int GetOutlineDepth(object row);

	/// <summary>Whether the row has rows beneath it to show or hide.</summary>
	bool HasOutlineChildren(object row);

	/// <summary>Whether the rows beneath the row are shown.</summary>
	bool IsOutlineExpanded(object row);

	/// <summary>Shows or hides the rows beneath the row from the next read.</summary>
	void SetOutlineExpanded(object row, bool expanded);

	/// <summary>Shows every row from the next read, including rows not yet read.</summary>
	void ExpandAll();

	/// <summary>Shows only the top-level rows from the next read, including rows not yet read.</summary>
	void CollapseAll();
}

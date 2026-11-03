// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Diagnostics;

namespace Sphere10.Framework.Maths.Compiler;

public class UnaryOperatorTree : SyntaxTree {
	public UnaryOperatorTree(Token token)
		: base(token) {
	}

	public SyntaxTree Operand { get; set; }

	public Operator Operator { get; set; }

	public override string ToString() {
		Debug.Assert(Operand != null);
		return string.Format("{0}({1})", Operator, Operand.ToString());
	}
}


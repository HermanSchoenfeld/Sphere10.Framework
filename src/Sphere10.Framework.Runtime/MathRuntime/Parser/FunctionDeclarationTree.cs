// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Text;

namespace Sphere10.Framework.Maths.Compiler;

public class FunctionDeclarationTree : SyntaxTree {
	public FunctionDeclarationTree() {
	}

	public FunctionDeclarationTree(Token token)
		: base(token) {
	}

	public SyntaxTree Expression { get; set; }

	public List<Token> Parameters { get; set; }

	public override string ToString() {
		StringBuilder paramsText = new StringBuilder();
		for (int i = 0; i < Parameters.Count; i++) {
			if (i > 0) paramsText.Append(",");
			paramsText.Append(Parameters[i].Value);
		}
		return string.Format("FunctionDeclaration({0},Params({1}),{2})", Token.Value, paramsText.ToString(), Expression.ToString());
	}
}


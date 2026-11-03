// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Maths.Compiler;

public class DefaultMathContext : IMathContext {
	public DefaultMathContext() {
		ParentContext = null;
		Variables = new DefaultVariableContext(this);
		Functions = new DefaultFunctionContext(this);
	}

	public IMathContext ParentContext { get; set; }

	public IVariableContext Variables { get; }

	public IFunctionContext Functions { get; }

	public IFunction GenerateFunction(string expression) {
		return GenerateFunction(expression, "x");
	}

	public IFunction GenerateFunction(string expression, string parameterName) {
		BasicFunctionGenerator functionGenerator = new BasicFunctionGenerator(this);
		functionGenerator.FunctionParameterName = parameterName;
		return functionGenerator.GenerateFunctionFromExpression(expression);
	}

}


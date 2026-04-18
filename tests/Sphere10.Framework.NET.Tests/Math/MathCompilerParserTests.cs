// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using NUnit.Framework;
using System.IO;
using Sphere10.Framework.Maths.Compiler;
namespace Sphere10.Framework.UnitTests;

[TestFixture]
public class MathCompilerParserTests {

	/// <summary>
	/// -x-y-z = gives tree:
	///                     -
	///                 -       z
	///             -x      y
	/// </summary>
	[Test]
	public void TestTreeStructure() {
		string exp = "-x-y-z";
		Parser parser = new Parser(new Scanner(new StringReader(exp)));
		SyntaxTree tree = parser.ParseExpression();
		// (-x) - y - z
		Assert.That(tree, Is.InstanceOf(typeof(BinaryOperatorTree)));
		Assert.That(((BinaryOperatorTree)tree).Operator, Is.EqualTo(Operator.Subtraction));
		Assert.That(((BinaryOperatorTree)tree).LeftHandSide, Is.InstanceOf(typeof(BinaryOperatorTree)));
		Assert.That(((BinaryOperatorTree)((BinaryOperatorTree)tree).LeftHandSide).Operator, Is.EqualTo(Operator.Subtraction));
		Assert.That(((BinaryOperatorTree)((BinaryOperatorTree)tree).LeftHandSide).LeftHandSide, Is.InstanceOf(typeof(UnaryOperatorTree)));
		Assert.That(((BinaryOperatorTree)((BinaryOperatorTree)tree).LeftHandSide).RightHandSide, Is.InstanceOf(typeof(FactorTree)));
		Assert.That(((UnaryOperatorTree)((BinaryOperatorTree)((BinaryOperatorTree)tree).LeftHandSide).LeftHandSide).Operator, Is.EqualTo(Operator.UnaryMinus));
		;
		Assert.That(((UnaryOperatorTree)((BinaryOperatorTree)((BinaryOperatorTree)tree).LeftHandSide).LeftHandSide).Operand, Is.InstanceOf(typeof(FactorTree)));
		;
		Assert.That(((BinaryOperatorTree)tree).RightHandSide, Is.InstanceOf(typeof(FactorTree)));
	}

	[Test]
	public void TestUnaryAndPower() {
		string exp = "-x^2.5E-1";
		Parser parser = new Parser(new Scanner(new StringReader(exp)));
		SyntaxTree tree = parser.ParseExpression();
		Assert.That(tree.ToString(), Is.EqualTo("UnaryMinus(Power(Identifier(x),Scalar(2.5E-1)))"));
	}

	[Test]
	public void TestUnaryAndMultiplication() {
		string exp = "-x*-y";
		Parser parser = new Parser(new Scanner(new StringReader(exp)));
		SyntaxTree tree = parser.ParseExpression();
		Assert.That(tree.ToString(), Is.EqualTo("Multiplication(UnaryMinus(Identifier(x)),UnaryMinus(Identifier(y)))"));
	}

	[Test]
	public void TestUnaryAndNegativePower() {
		string exp = "-x^-y";
		Parser parser = new Parser(new Scanner(new StringReader(exp)));
		SyntaxTree tree = parser.ParseExpression();
		Assert.That(tree.ToString(), Is.EqualTo("UnaryMinus(Power(Identifier(x),UnaryMinus(Identifier(y))))"));
	}

	[Test]
	public void TestUnaryAndNegativePowerTower() {
		string exp = "-x^-y^-z";
		Parser parser = new Parser(new Scanner(new StringReader(exp)));
		SyntaxTree tree = parser.ParseExpression();
		Assert.That(tree.ToString(), Is.EqualTo("UnaryMinus(Power(Identifier(x),UnaryMinus(Power(Identifier(y),UnaryMinus(Identifier(z))))))"));
	}

	[Test]
	public void TestMultiplicationDivision() {
		string exp = "x*y/z";
		Parser parser = new Parser(new Scanner(new StringReader(exp)));
		SyntaxTree tree = parser.ParseExpression();
		Assert.That(tree.ToString(), Is.EqualTo("Multiplication(Identifier(x),Division(Identifier(y),Identifier(z)))"));
	}

	[Test]
	public void TestDivisionDivision() {
		string exp = "x/y/z";
		Parser parser = new Parser(new Scanner(new StringReader(exp)));
		SyntaxTree tree = parser.ParseExpression();
		Assert.That(tree.ToString(), Is.EqualTo("Division(Division(Identifier(x),Identifier(y)),Identifier(z))"));
	}

	[Test]
	public void TestDivisionMultiplication() {
		string exp = "x/y*z";
		Parser parser = new Parser(new Scanner(new StringReader(exp)));
		SyntaxTree tree = parser.ParseExpression();
		Assert.That(tree.ToString(), Is.EqualTo("Multiplication(Division(Identifier(x),Identifier(y)),Identifier(z))"));
	}

	[Test]
	public void TestAdditionSub() {
		string exp = "x+y-z";
		Parser parser = new Parser(new Scanner(new StringReader(exp)));
		SyntaxTree tree = parser.ParseExpression();
		Assert.That(tree.ToString(), Is.EqualTo("Subtraction(Addition(Identifier(x),Identifier(y)),Identifier(z))"));
	}

}


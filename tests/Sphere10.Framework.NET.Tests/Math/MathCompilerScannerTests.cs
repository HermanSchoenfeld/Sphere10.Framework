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
public class MathCompilerScannerTests {

	[Test]
	public void TestComplexTokenSequence() {
		string text = "ID1E-1E-3++";
		Scanner scanner = new Scanner(new StringReader(text));
		Assert.That(scanner.GetNextToken().TokenType, Is.EqualTo(TokenType.Identifier));
		Assert.That(scanner.GetNextToken().TokenType, Is.EqualTo(TokenType.Minus));
		Assert.That(scanner.GetNextToken().TokenType, Is.EqualTo(TokenType.Scalar));
		Assert.That(scanner.GetNextToken().TokenType, Is.EqualTo(TokenType.Plus));
		Assert.That(scanner.GetNextToken().TokenType, Is.EqualTo(TokenType.Plus));
	}


	[Test]
	public void TestNumber() {
		string text = "5";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.Scalar));
		Assert.That(token.Value, Is.EqualTo(5.ToString()));
	}

	[Test]
	public void TestDecimal() {
		string text = "123.456";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.Scalar));
		Assert.That(token.Value, Is.EqualTo((123.456).ToString()));
	}

	[Test]
	public void TestReal() {
		string text = "123.456E12";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.Scalar));
		Assert.That(double.Parse(token.Value), Is.EqualTo(double.Parse(text)));
	}

	[Test]
	public void TestRealWithSign() {
		string text = "   123.456E-12   e";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.Scalar));
		Assert.That(double.Parse(token.Value), Is.EqualTo(double.Parse("123.456E-12")));
	}

	[Test]
	public void TestIdentifier() {
		string text = "abc123   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.Identifier));
		Assert.That(token.Value, Is.EqualTo("abc123"));

	}

	[Test]
	public void TestPlus() {
		string text = " +   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.Plus));
	}

	[Test]
	public void TestMinus() {
		string text = " -   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.Minus));
	}

	[Test]
	public void TestMultiply() {
		string text = " *   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.Multiply));

	}

	[Test]
	public void TestDivide() {
		string text = " /   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.Divide));

	}

	[Test]
	public void TestPower() {
		string text = " ^   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.Power));

	}

	[Test]
	public void TestAssignment() {
		string text = " =   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.Assignment));
	}

	[Test]
	public void TestEquality() {
		string text = " ==   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.Equality));
	}

	[Test]
	public void TestAnd() {
		string text = " &&   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.And));
	}

	[Test]
	public void TestOr() {
		string text = " ||   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.Or));
	}

	[Test]
	public void TestOpenBracket() {
		string text = " [   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.OpenBracket));
	}

	[Test]
	public void TestCloseBracket() {
		string text = " ]   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.CloseBracket));
	}


	[Test]
	public void TestOpenParenthesis() {
		string text = " (   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.OpenParenthesis));
	}

	[Test]
	public void TestCloseParenthesis() {
		string text = " )   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.CloseParenthesis));
	}

	[Test]
	public void TestBeginBracket() {
		string text = " {   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.BeginBracket));
	}

	[Test]
	public void TestEndBracket() {
		string text = " }   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.EndBracket));
	}

	[Test]
	public void TestComma() {
		string text = " ,   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.Comma));
	}

	[Test]
	public void TestDot() {
		string text = " .   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.Dot));
	}

	[Test]
	public void TestLet() {
		string text = " leT   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.Let));
	}

	[Test]
	public void TestIf() {
		string text = " If   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.If));
	}

	[Test]
	public void TestThen() {
		string text = " tHeN   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.Then));
	}


	[Test]
	public void TestElse() {
		string text = " else   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.Else));
	}


	[Test]
	public void TestSemiColon() {
		string text = " ;   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.SemiColon));
	}


	[Test]
	public void TestNot() {
		string text = " !   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.Not));
	}


	[Test]
	public void TestInequality() {
		string text = " !=   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.Inequality));
	}


	[Test]
	public void TestModulus() {
		string text = " %   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.Modulus));
	}

	[Test]
	public void TestLessThan() {
		string text = " <   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.LessThan));
	}

	[Test]
	public void TesLessThanEqualTo() {
		string text = " <=   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.LessThanEqualTo));
	}

	[Test]
	public void TestGreaterThan() {
		string text = " >   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.GreaterThan));
	}


	[Test]
	public void TestGreaterThanEqualTo() {
		string text = " >=   ";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.GreaterThanEqualTo));
	}


	[Test]
	public void TestEndOfCode() {
		string text = "";
		Scanner scanner = new Scanner(new StringReader(text));
		Token token = scanner.GetNextToken();
		Assert.That(token, Is.Not.Null);
		Assert.That(token.TokenType, Is.EqualTo(TokenType.EndOfCode));
	}
}


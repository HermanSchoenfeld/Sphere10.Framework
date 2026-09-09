// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;

namespace Sphere10.VisualRenderer;

/// <summary>Code languages with their Prism syntax-highlighting identifiers.</summary>
public enum CodeLanguage {
	[Description("text")]
	Text,

	[Description("abap")]
	Abap,

	[Description("agda")]
	Agda,

	[Description("arduino")]
	Ardunio,

	[Description("armasm")]
	Assembly,

	[Description("bash")]
	Bash,

	[Description("basic")]
	Basic,

	[Description("bnf")]
	Bnf,

	[Description("c")]
	C,

	[Description("csharp")]
	CSharp,

	[Description("cpp")]
	CPlusPlus,

	[Description("clojure")]
	Clojure,

	[Description("coffeescript")]
	Coffeescript,

	[Description("coq")]
	Coq,

	[Description("css")]
	Css,

	[Description("dart")]
	Dart,

	[Description("dhall")]
	Dhall,

	[Description("diff")]
	Diff,

	[Description("docker")]
	Docker,

	[Description("ebnf")]
	Ebnf,

	[Description("elixir")]
	Elixer,

	[Description("elm")]
	Elm,

	[Description("erlang")]
	Erlang,

	[Description("fsharp")]
	FSharp,

	[Description("flow")]
	Flow,

	[Description("fortran")]
	Fortran,

	[Description("gherkin")]
	Gherkin,

	[Description("glsl")]
	Glsl,

	[Description("go")]
	Go,

	[Description("graphql")]
	Graphql,

	[Description("groovy")]
	Groovy,

	[Description("haskell")]
	Haskell,

	[Description("html")]
	Html,

	[Description("idris")]
	Idris,

	[Description("java")]
	Java,

	[Description("javascript")]
	Javascript,

	[Description("json")]
	Json,

	[Description("julia")]
	Julia,

	[Description("kotlin")]
	Kotlin,

	[Description("latex")]
	Latex,

	[Description("less")]
	Less,

	[Description("lisp")]
	Lisp,

	[Description("typescript")]
	LiveScript,

	[Description("llvm")]
	Llvm,

	[Description("llvm")]
	LlvmIr,

	[Description("lua")]
	Lua,

	[Description("makefile")]
	Makefile,

	[Description("markdown")]
	Markdown,

	[Description("markup-templating")]
	Markup,

	[Description("matlab")]
	Matlab,

	[Description("mermaid")]
	Mermaid,

	[Description("nix")]
	Nix,

	[Description("objc")]
	ObjectiveC,

	[Description("ocaml")]
	Ocaml,

	[Description("pascal")]
	Pascal,

	[Description("perl")]
	Perl,

	[Description("php")]
	Php,

	[Description("powershell")]
	Powershell,

	[Description("prolog")]
	Prolog,

	[Description("protobuf")]
	Protobuf,

	[Description("python")]
	Python,

	[Description("r")]
	R,

	[Description("racket")]
	Racket,

	[Description("reason")]
	Reason,

	[Description("ruby")]
	Ruby,

	[Description("rust")]
	Rust,

	[Description("sass")]
	Sass,

	[Description("scala")]
	Scala,

	[Description("scheme")]
	Scheme,

	[Description("scss")]
	Scss,

	[Description("shell-session")]
	Shell,

	[Description("solidity")]
	Solidity,

	[Description("sql")]
	Sql,

	[Description("swift")]
	Swift,

	[Description("toml")]
	Toml,

	[Description("typescript")]
	Typescript,

	[Description("vbnet")]
	VbNet,

	[Description("verilog")]
	Verilog,

	[Description("vhdl")]
	Vhdl,

	[Description("vb")]
	VisualBasic,

	[Description("wasm")]
	Webassembly,

	[Description("xml")]
	Xml,

	[Description("yaml")]
	Yaml,

	[Description("arduino")]
	Arduino,

	[Description("elixir")]
	Elixir
}

// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Sphere10.Framework.Generators;

/// <summary>
/// Incremental source generator that implements partial properties in classes marked with
/// <c>[AutoDirty]</c>. For each <c>partial</c> property whose declaring class (or any ancestor)
/// carries the attribute, the generator emits an implementing declaration that:
/// <list type="bullet">
///   <item>stores the value in a private backing field,</item>
///   <item>short-circuits when the new value equals the current one, and</item>
///   <item>sets the dirty-flag property (named in the attribute constructor) to <c>true</c> on change.</item>
/// </list>
/// </summary>
[Generator]
public class AutoDirtyGenerator : IIncrementalGenerator {

	private const string AttributeFullName = "Sphere10.Framework.Generators.AutoDirtyAttribute";

	public void Initialize(IncrementalGeneratorInitializationContext context) {
		// 1. Inject the [AutoDirty] attribute so consumers don't need a compile-time dependency on this assembly.
		context.RegisterPostInitializationOutput(static ctx => {
			ctx.AddSource("AutoDirtyAttribute.g.cs", SourceText.From(AttributeSource, Encoding.UTF8));
		});

		// 2. Collect every partial property whose containing class hierarchy has [AutoDirty].
		var propertyInfos = context.SyntaxProvider
			.CreateSyntaxProvider(
				predicate: static (node, _) => node is PropertyDeclarationSyntax pds
					&& pds.Modifiers.Any(SyntaxKind.PartialKeyword)
					&& pds.Parent is ClassDeclarationSyntax or RecordDeclarationSyntax,
				transform: static (ctx, _) => ExtractPropertyInfo(ctx))
			.Where(static info => info is not null);

		// 3. Group by containing class so we emit one file per class.
		var grouped = propertyInfos
			.Collect()
			.SelectMany(static (items, _) => {
				var dict = new Dictionary<string, List<PropertyInfo?>>();
				foreach (var item in items) {
					if (item is null) continue;
					if (!dict.TryGetValue(item.ClassFullName, out var list)) {
						list = new List<PropertyInfo?>();
						dict[item.ClassFullName] = list;
					}
					list.Add(item);
				}
				return dict.Select(kvp => new ClassGroup(kvp.Key, kvp.Value.ToImmutableArray()));
			});

		// 4. Emit source for each class.
		context.RegisterSourceOutput(grouped, static (spc, group) => {
			var source = GenerateClassSource(group);
			spc.AddSource($"{SanitizeFileName(group.ClassFullName)}_AutoDirty.g.cs",
				SourceText.From(source, Encoding.UTF8));
		});
	}

	// ── Extraction ───────────────────────────────────────────────────────

	private static PropertyInfo? ExtractPropertyInfo(GeneratorSyntaxContext ctx) {
		var propDecl = (PropertyDeclarationSyntax)ctx.Node;
		var propSymbol = ctx.SemanticModel.GetDeclaredSymbol(propDecl);
		if (propSymbol is null)
			return null;

		// Walk up the class hierarchy looking for [AutoDirty].
		var dirtyPropName = FindAutoDirtyAttribute(propSymbol.ContainingType);
		if (dirtyPropName is null)
			return null;

		// Skip the dirty-flag property itself to avoid infinite recursion.
		if (propSymbol.Name == dirtyPropName)
			return null;

		// Only consider instance properties with both getter and setter.
		if (propSymbol.IsStatic || propSymbol.GetMethod is null || propSymbol.SetMethod is null)
			return null;

		// Only process declaring declarations (not implementing ones).
		if (propDecl.AccessorList is null ||
			propDecl.AccessorList.Accessors.Any(a => a.Body is not null || a.ExpressionBody is not null))
			return null;

		var getter = propDecl.AccessorList.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
		var setter = propDecl.AccessorList.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration) || a.IsKind(SyntaxKind.InitAccessorDeclaration));
		if (getter is null || setter is null)
			return null;

		var containingType = propSymbol.ContainingType;
		var ns = containingType.ContainingNamespace?.IsGlobalNamespace == true
			? null
			: containingType.ContainingNamespace?.ToDisplayString();

		// The implementing declaration repeats the declaring modifiers and accessors, such as internal, virtual or init.
		return new PropertyInfo(
			ns,
			GetTypeDeclarations(containingType),
			containingType.ToDisplayString(),
			propSymbol.Name,
			propSymbol.Type.ToDisplayString(),
			dirtyPropName,
			propDecl.Modifiers.ToString(),
			AccessorPrefix(getter),
			AccessorPrefix(setter) + setter.Keyword.Text
		);
	}

	/// <summary>
	/// Returns the partial declaration headers from the outermost containing type to the given type.
	/// </summary>
	private static ImmutableArray<string> GetTypeDeclarations(INamedTypeSymbol type) {
		var declarations = new List<string>();
		for (var current = type; current is not null; current = current.ContainingType) {
			var keyword = current switch {
				{ IsRecord: true, TypeKind: TypeKind.Struct } => "record struct",
				{ IsRecord: true } => "record",
				{ TypeKind: TypeKind.Struct } => "struct",
				{ TypeKind: TypeKind.Interface } => "interface",
				_ => "class"
			};
			var typeParameters = current.TypeParameters.Length > 0
				? $"<{string.Join(", ", current.TypeParameters.Select(parameter => parameter.Name))}>"
				: string.Empty;
			declarations.Insert(0, $"partial {keyword} {current.Name}{typeParameters}");
		}
		return declarations.ToImmutableArray();
	}

	/// <summary>
	/// Walks up the class hierarchy looking for <c>[AutoDirty("PropertyName")]</c>.
	/// Returns the dirty-flag property name, or <c>null</c> if the attribute is not found.
	/// </summary>
	private static string? FindAutoDirtyAttribute(INamedTypeSymbol? type) {
		while (type is not null) {
			foreach (var attr in type.GetAttributes()) {
				if (attr.AttributeClass?.ToDisplayString() == AttributeFullName &&
					attr.ConstructorArguments.Length > 0 &&
					attr.ConstructorArguments[0].Value is string name)
					return name;
			}
			type = type.BaseType;
		}
		return null;
	}

	// ── Code Generation ──────────────────────────────────────────────────

	private static string GenerateClassSource(ClassGroup group) {
		var first = group.Properties[0]!;
		var sb = new StringBuilder();
		sb.AppendLine("// <auto-generated/>");
		sb.AppendLine("#nullable enable annotations");
		sb.AppendLine();

		if (first.Namespace is not null) {
			sb.AppendLine($"namespace {first.Namespace};");
			sb.AppendLine();
		}

		// Reopen every containing type, so nested and generic types receive their implementations.
		var indent = "";
		foreach (var declaration in first.TypeDeclarations) {
			sb.AppendLine($"{indent}{declaration} {{");
			indent += "\t";
		}

		foreach (var prop in group.Properties) {
			if (prop is null) continue;
			var backingField = $"_autoDirty_{char.ToLowerInvariant(prop.PropertyName[0])}{prop.PropertyName.Substring(1)}";

			sb.AppendLine($"{indent}private {prop.PropertyType} {backingField};");
			sb.AppendLine();

			sb.AppendLine($"{indent}{prop.Modifiers} {prop.PropertyType} {prop.PropertyName} {{");
			sb.AppendLine($"{indent}\t{prop.GetterPrefix}get => {backingField};");
			sb.AppendLine($"{indent}\t{prop.SetterDeclaration} {{");
			sb.AppendLine($"{indent}\t\tif (!System.Collections.Generic.EqualityComparer<{prop.PropertyType}>.Default.Equals({backingField}, value)) {{");
			sb.AppendLine($"{indent}\t\t\t{backingField} = value;");
			sb.AppendLine($"{indent}\t\t\t{prop.DirtyPropertyName} = true;");
			sb.AppendLine($"{indent}\t\t}}");
			sb.AppendLine($"{indent}\t}}");
			sb.AppendLine($"{indent}}}");
			sb.AppendLine();
		}

		for (var level = first.TypeDeclarations.Length - 1; level >= 0; level--)
			sb.AppendLine($"{new string('\t', level)}}}");
		return sb.ToString();
	}

	// ── Helpers ──────────────────────────────────────────────────────────

	// Returns an accessor's own modifiers, such as "private ", or an empty string.
	private static string AccessorPrefix(AccessorDeclarationSyntax accessor) =>
		accessor.Modifiers.Count > 0 ? accessor.Modifiers.ToString() + " " : string.Empty;

	private static string SanitizeFileName(string name) =>
		name.Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_').Replace(' ', '_');

	// ── Attribute Source ─────────────────────────────────────────────────

	private const string AttributeSource = @"// <auto-generated/>
using System;

namespace Sphere10.Framework.Generators;

/// <summary>
/// Marks a class for automatic dirty-flag tracking. The source generator will implement
/// every <c>partial</c> property in the class (and its subclasses) so that setting a new
/// value automatically sets the specified boolean property to <c>true</c>.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [AutoDirty(nameof(IsDirty))]
/// public partial class MyEntity {
///     public bool IsDirty { get; set; }
///     public partial string Name { get; set; }   // ← implemented by generator
///     public partial int    Age  { get; set; }    // ← implemented by generator
/// }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
internal sealed class AutoDirtyAttribute : Attribute {
    /// <summary>
    /// Initializes a new instance of the <see cref=""AutoDirtyAttribute""/> class.
    /// </summary>
    /// <param name=""dirtyPropertyName"">
    /// The name of the boolean property that should be set to <c>true</c> when any
    /// tracked property changes. Use <c>nameof()</c> for compile-time safety.
    /// </param>
    public AutoDirtyAttribute(string dirtyPropertyName) {
        DirtyPropertyName = dirtyPropertyName;
    }

    /// <summary>Gets the name of the boolean dirty-flag property.</summary>
    public string DirtyPropertyName { get; }
}
";

	// ── Data Types ───────────────────────────────────────────────────────

	private sealed class PropertyInfo {
		public PropertyInfo(
			string? ns,
			ImmutableArray<string> typeDeclarations,
			string classFullName,
			string propertyName,
			string propertyType,
			string dirtyPropertyName,
			string modifiers,
			string getterPrefix,
			string setterDeclaration) {
			Namespace = ns;
			TypeDeclarations = typeDeclarations;
			ClassFullName = classFullName;
			PropertyName = propertyName;
			PropertyType = propertyType;
			DirtyPropertyName = dirtyPropertyName;
			Modifiers = modifiers;
			GetterPrefix = getterPrefix;
			SetterDeclaration = setterDeclaration;
		}

		public string? Namespace { get; }
		public ImmutableArray<string> TypeDeclarations { get; }
		public string ClassFullName { get; }
		public string PropertyName { get; }
		public string PropertyType { get; }
		public string DirtyPropertyName { get; }
		public string Modifiers { get; }
		public string GetterPrefix { get; }
		public string SetterDeclaration { get; }
	}

	private sealed class ClassGroup {
		public ClassGroup(string classFullName, ImmutableArray<PropertyInfo?> properties) {
			ClassFullName = classFullName;
			Properties = properties;
		}

		public string ClassFullName { get; }
		public ImmutableArray<PropertyInfo?> Properties { get; }
	}
}

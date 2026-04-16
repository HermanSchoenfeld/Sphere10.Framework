using System;

namespace Sphere10.Framework;

/// <summary>
/// Flags controlling which reference serialization modes are supported by a <see cref="ReferenceSerializer{TItem}"/>.
/// Each flag enables a distinct serialization path for reference-type values.
/// </summary>
[Flags]
public enum ReferenceSerializerMode {

	/// <summary>Allows null values to be serialized (discriminator byte = 0).</summary>
	SupportNull = 1 << 0,

	/// <summary>Allows context references — back-references to objects already seen in the same serialization context.</summary>
	SupportContextReferences = 1 << 1,

	/// <summary>Allows external references — pointers to objects stored outside the current serialization stream (e.g. dimension objects in an ObjectSpace).</summary>
	SupportExternalReferences = 1 << 2,

	/// <summary>No special reference handling.</summary>
	None = 0,

	/// <summary>Default mode: null, context references, and external references are all supported.</summary>
	Default = SupportNull | SupportContextReferences | SupportExternalReferences,
}


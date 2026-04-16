using System;

namespace Sphere10.Framework;

/// <summary>
/// Flags controlling which reference serialization modes are supported by a <see cref="ReferenceSerializer{TItem}"/>.
/// Each flag enables a distinct serialization path for reference-type values.
/// </summary>
[Flags]
public enum ReferenceSerializerMode {

	// Allows null values to be serialized (discriminator byte = 0)
	SupportNull = 1 << 0,

	// Allows context references — back-references to objects already seen in the same serialization context
	SupportContextReferences = 1 << 1,

	// Allows external references — pointers to objects stored outside the current serialization stream (e.g. dimension objects in an ObjectSpace)
	SupportExternalReferences = 1 << 2,

	// No special reference handling
	None = 0,

	// Default mode: null, context references, and external references are all supported
	Default = SupportNull | SupportContextReferences | SupportExternalReferences,
}


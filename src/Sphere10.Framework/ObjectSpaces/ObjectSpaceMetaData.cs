// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.ObjectSpaces;

/// <summary>ObjectSpace construction settings not already supplied by the clustered header.</summary>
public sealed class ObjectSpaceMetaData {
	/// <summary>The digest algorithm used by this ObjectSpace's Merkle attachments.</summary>
	public CHF HashFunction { get; set; }

	/// <summary>The shared serializer registrations used by all dimension ObjectStreams.</summary>
	public SerializerFactory SerializerFactory { get; set; }
}

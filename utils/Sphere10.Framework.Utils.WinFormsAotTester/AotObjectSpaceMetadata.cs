// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics.CodeAnalysis;
using Sphere10.Framework.ObjectSpaces;
using Account = Sphere10.Framework.Utils.WinFormsTester.ObjectSpaceScreen.Account;
using Identity = Sphere10.Framework.Utils.WinFormsTester.ObjectSpaceScreen.Identity;
using Preserve = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace Sphere10.Framework.Utils.WinFormsTester;

internal static class AotObjectSpaceMetadata {
	// Retain this demonstration's finite schema and the existing factory implementation.
	// Store metadata remains authoritative when reopening; no replacement serializers or type codes are registered.
	[DynamicDependency(Preserve.PublicParameterlessConstructor | Preserve.PublicProperties, typeof(Account))]
	[DynamicDependency(Preserve.PublicParameterlessConstructor | Preserve.PublicProperties, typeof(Identity))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(ObjectSpaceDimensionBuilder<Account>))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(ObjectSpaceDimensionBuilder<Identity>))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(ObjectStream<Account>))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(ObjectStream<Identity>))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(ObjectSpace.Dimension<Account>))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(ObjectSpace.Dimension<Identity>))]
	[DynamicDependency(Preserve.PublicConstructors | Preserve.PublicProperties, typeof(global::StreamMappedRecyclableList<Account>))]
	[DynamicDependency(Preserve.PublicConstructors | Preserve.PublicProperties, typeof(global::StreamMappedRecyclableList<Identity>))]
	[DynamicDependency(Preserve.NonPublicConstructors | Preserve.NonPublicMethods | Preserve.PublicProperties, typeof(CompositeSerializer<Account>))]
	[DynamicDependency(Preserve.NonPublicConstructors | Preserve.NonPublicMethods | Preserve.PublicProperties, typeof(CompositeSerializer<Identity>))]
	[DynamicDependency(Preserve.PublicConstructors | Preserve.PublicProperties, typeof(PolymorphicSerializer<Account>))]
	[DynamicDependency(Preserve.PublicConstructors | Preserve.PublicProperties, typeof(PolymorphicSerializer<Identity>))]
	[DynamicDependency(Preserve.PublicConstructors | Preserve.PublicProperties, typeof(ReferenceSerializer<Account>))]
	[DynamicDependency(Preserve.PublicConstructors | Preserve.PublicProperties, typeof(ReferenceSerializer<Identity>))]
	[DynamicDependency(Preserve.PublicConstructors | Preserve.PublicProperties, typeof(ReferenceSerializer<string>))]
	[DynamicDependency(Preserve.PublicConstructors | Preserve.PublicProperties, typeof(ReferenceSerializer<byte[]>))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(EnumSerializer<DSS>))]
	[DynamicDependency(Preserve.PublicConstructors, typeof(NullableSerializer<DSS>))]
	public static void Retain() {
		// Metadata alone does not generate these closed value-type instantiations.
		// Retain factory delegates without invoking them or changing serializer registrations.
		GC.KeepAlive((Func<EnumSerializer<DSS>>)(() => new EnumSerializer<DSS>()));
		GC.KeepAlive((Func<IItemSerializer<DSS>, NullableSerializer<DSS>>)(serializer => new NullableSerializer<DSS>(serializer)));
	}
}

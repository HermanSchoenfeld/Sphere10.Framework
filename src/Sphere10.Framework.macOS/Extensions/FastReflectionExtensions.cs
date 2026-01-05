// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Sphere10.Framework.FastReflection;


namespace Sphere10.Framework
{
	public static class FastReflectionExtensions
	{
		public static object FastInvoke(this MethodInfo methodInfo, object instance, params object[] parameters)
		{
			return FastReflectionCaches.MethodInvokerCache.Get(methodInfo).Invoke(instance, parameters);
		}
		
		public static void FastSetValue(this PropertyInfo propertyInfo, object instance, object value)
		{
			FastReflectionCaches.PropertyAccessorCache.Get(propertyInfo).SetValue(instance, value);
		}
		
		public static object FastGetValue(this PropertyInfo propertyInfo, object instance)
		{
			return FastReflectionCaches.PropertyAccessorCache.Get(propertyInfo).GetValue(instance);
		}
		
		public static object FastGetValue(this FieldInfo fieldInfo, object instance)
		{
			return FastReflectionCaches.FieldAccessorCache.Get(fieldInfo).GetValue(instance);
		}
		
		public static object FastInvoke(this ConstructorInfo constructorInfo, params object[] parameters)
		{
			return FastReflectionCaches.ConstructorInvokerCache.Get(constructorInfo).Invoke(parameters);
		}
	}
}


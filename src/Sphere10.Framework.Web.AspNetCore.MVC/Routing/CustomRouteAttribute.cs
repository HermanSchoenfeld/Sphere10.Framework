// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Sphere10.Framework.Web.AspNetCore.MVC;

/// <summary>
/// Set up a custom route for a controller. This attribute can be used
/// multiple times on the same class. It is NOT inherited. Register the routes with
/// <see cref="RegisterCustomRoutesFromAssembly"/>, which maps each attribute as a conventional controller route.
///
/// <example>
///     [CustomRoute("admin/content/{action}")]
///     [CustomRoute("account/register", DefaultAction = "Register")]
///     [CustomRoute("account/plan/{planType}", DefaultAction = "ChangePlan")]
///     [CustomRoute("account/plan/trial", DefaultAction = "SelectPlan", Defaults = "planType=Trial")]
/// </example>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class CustomRouteAttribute : Attribute {
	private string _defaults;

	/// <summary>
	/// The default action for this route
	/// </summary>
	public string DefaultAction { get; set; }


	/// <summary>
	/// A key1=value1&amp;key2=value2 key/value pair string of defaults for this route
	/// </summary>
	public string Defaults {
		get {
			// concatenate _defaults and DefaultAction into a key=value string
			var toReturn = _defaults ?? string.Empty;
			if (!string.IsNullOrEmpty(DefaultAction)) {
				toReturn += (toReturn.Length > 0 ? "&" : string.Empty) + "action=" + DefaultAction;
			}
			return toReturn;
		}
		set => _defaults = value;
	}


	public string Url { get; private set; }

	/// <summary>
	/// Determines the priority of this route.
	/// Routes with a higher (larger value) priority are selected before those with a lower priority.
	/// The default priority is zero.
	/// </summary>
	public int Priority { get; set; }

	/// <summary>
	/// Sets up a custom route for a <seealso cref="Controller"/> derived class
	/// </summary>
	/// <param name="url">The route url</param>
	public CustomRouteAttribute(string url) {
		if ((!String.IsNullOrEmpty(url)) && (url.ToLower().Contains("{controller}")))
			throw new Exception("A custom route contains '{controller}'. Please revise the route.");
		Url = url;
	}


	public static void RegisterCustomRoutesFromAssembly(IEndpointRouteBuilder endpoints, Assembly assembly) {
		Guard.ArgumentNotNull(endpoints, nameof(endpoints));
		Guard.ArgumentNotNull(assembly, nameof(assembly));
		assembly.GetDerivedTypes<Controller>().ForEach(type => ProcessType(endpoints, type));
	}

	public static void ProcessType(IEndpointRouteBuilder endpoints, Type type) {
		Guard.ArgumentNotNull(endpoints, nameof(endpoints));
		Guard.ArgumentNotNull(type, nameof(type));
		var index = 0;
		foreach (var customRouteAttribute in type.GetCustomAttributesOfType<CustomRouteAttribute>().OrderByDescending(x => x.Priority))
			ProcessCustomRouteAttribute(endpoints, type, customRouteAttribute, $"{type.FullName}[{index++}]");
	}

	public static void ProcessCustomRouteAttribute(IEndpointRouteBuilder endpoints, Type sourceType, CustomRouteAttribute attribute, string routeName) {
		Guard.ArgumentNotNull(endpoints, nameof(endpoints));
		Guard.ArgumentNotNull(sourceType, nameof(sourceType));
		Guard.ArgumentNotNull(attribute, nameof(attribute));
		Guard.ArgumentNotNullOrWhitespace(routeName, nameof(routeName));
		var defaults = new RouteValueDictionary();
		var defaultDict = StringExtensions.ParseQueryString(attribute.Defaults);
		foreach (string key in defaultDict.Keys) {
			defaults.Add(key, defaultDict[key]);
		}
		defaults.Add("controller", sourceType.Name.Substring(0, sourceType.Name.LastIndexOf("Controller")));
		endpoints.MapControllerRoute(routeName, attribute.Url, defaults);
	}

}


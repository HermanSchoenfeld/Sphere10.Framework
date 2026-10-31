using System;
using Sphere10.Framework.Mapping;

namespace Sphere10.Framework.ObjectSpaces;

/// <summary>
/// Encapsulates how change tracking is performed on objects, optionally mapping to a boolean property for automatic dirty flagging.
/// </summary>
internal class ObjectChangeTracker {

	/// <summary>Creates a tracker that neither detects nor changes dirty state.</summary>
	public ObjectChangeTracker()  {
		// The default tracker leaves objects unchanged and reports no dirty state.
		HasChanged = _ => false;;
		SetChanged = (_, _) => { };
		SupportsChangeTracking = false;
	}

	/// <summary>Creates reconstructible dirty tracking from a mapped boolean property.</summary>
	public ObjectChangeTracker(Member member) 
		: this(Guard.EnsureCast<PropertyMember>(member, $"Must be a {nameof(PropertyMember)}")) {
	}

	/// <summary>Creates dirty tracking and retains the readable, writable boolean property's identity.</summary>
	public ObjectChangeTracker(PropertyMember member) {
		// A persisted member-based tracker must be able to read and clear the same boolean property.
		Guard.ArgumentNotNull(member, nameof(member));
		Guard.Ensure(member.CanRead, "Member must have public getter");
		Guard.Ensure(member.CanWrite, "Member must have public setter");
		Guard.Ensure(member.PropertyType == typeof(bool), "Member must be of type bool");

		// Retain member identity alongside the delegates so metadata can reconstruct this tracker.
		HasChanged = obj => (bool)member.GetValue(obj);
		SetChanged = (obj, val) => member.SetValue(obj, val);
		SupportsChangeTracking = true;
		Member = member;
	}

	/// <summary>Creates delegate-backed tracking for supplied schemas; metadata cannot reconstruct these callbacks.</summary>
	public ObjectChangeTracker(Func<object, bool> hasChanged, Action<object, bool> setChanged) {
		// Delegate-backed tracking remains available for supplied schemas without a persisted member name.
		Guard.ArgumentNotNull(hasChanged, nameof(hasChanged));
		Guard.ArgumentNotNull(setChanged, nameof(setChanged));
		HasChanged = hasChanged;
		SetChanged = setChanged;
		SupportsChangeTracking = true;
	}

	public bool SupportsChangeTracking { get; }

	/// <summary>The mapped dirty property, or null for default and delegate-backed trackers.</summary>
	public PropertyMember Member { get; }

	public Func<object, bool> HasChanged { get; }

	public Action<object, bool> SetChanged { get; }

	public static ObjectChangeTracker Default { get; } = new ();

}


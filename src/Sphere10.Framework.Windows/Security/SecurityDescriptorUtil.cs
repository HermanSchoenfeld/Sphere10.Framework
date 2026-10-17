// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Sphere10.Framework.Windows.Security;

/// <summary>Reads and translates exact SID identities without changing the descriptor's other security data.</summary>
public class SecurityDescriptorUtil {
	public SecurityDescriptorSidReference[] GetSidReferences(byte[] descriptor, AccessControlSections sections = AccessControlSections.All) {
		Guard.ArgumentNotNull(descriptor, nameof(descriptor));
		var security = new RawSecurityDescriptor(descriptor, 0);
		var references = new Dictionary<SecurityIdentifier, AccessControlSections>();
		void Add(SecurityIdentifier sid, AccessControlSections section) {
			if (sid != null)
				references[sid] = references.GetValueOrDefault(sid) | section;
		}
		if (sections.HasFlag(AccessControlSections.Owner))
			Add(security.Owner, AccessControlSections.Owner);
		if (sections.HasFlag(AccessControlSections.Group))
			Add(security.Group, AccessControlSections.Group);
		void AddAcl(RawAcl acl, AccessControlSections section) {
			if (acl != null && sections.HasFlag(section))
				foreach (GenericAce ace in acl)
					if (ace is KnownAce known)
						Add(known.SecurityIdentifier, section);
		}
		AddAcl(security.DiscretionaryAcl, AccessControlSections.Access);
		AddAcl(security.SystemAcl, AccessControlSections.Audit);
		return references.Select(item => new SecurityDescriptorSidReference(item.Key, item.Value)).ToArray();
	}

	/// <summary>Translates every original SID at most once. A null translation retains its original SID.</summary>
	public byte[] TranslateSecurityDescriptor(byte[] descriptor, Func<SecurityIdentifier, SecurityIdentifier> translate)
		=> TranslateSecurityDescriptor(descriptor, translate, AccessControlSections.All, out _);

	/// <summary>Returns the original bytes when unchanged. Owner, group, ACL order, masks, inheritance, object GUIDs and opaque ACE data are preserved.</summary>
	public byte[] TranslateSecurityDescriptor(byte[] descriptor, Func<SecurityIdentifier, SecurityIdentifier> translate,
		AccessControlSections sections, out AccessControlSections changedSections) {
		Guard.ArgumentNotNull(descriptor, nameof(descriptor));
		Guard.ArgumentNotNull(translate, nameof(translate));
		var security = new RawSecurityDescriptor(descriptor, 0);
		var translations = new Dictionary<SecurityIdentifier, SecurityIdentifier>();
		var changed = AccessControlSections.None;
		SecurityIdentifier Translate(SecurityIdentifier original, AccessControlSections section) {
			if (original == null || !sections.HasFlag(section))
				return original;
			if (!translations.TryGetValue(original, out var replacement)) {
				replacement = translate(original) ?? original;
				translations.Add(original, replacement);
			}
			if (!original.Equals(replacement))
				changed |= section;
			return replacement;
		}
		security.Owner = Translate(security.Owner, AccessControlSections.Owner);
		security.Group = Translate(security.Group, AccessControlSections.Group);
		void TranslateAcl(RawAcl acl, AccessControlSections section) {
			if (acl != null && sections.HasFlag(section)) {
				for (var index = 0; index < acl.Count; index++) {
					if (acl[index] is KnownAce known) {
						known.SecurityIdentifier = Translate(known.SecurityIdentifier, section);
						acl[index] = known;
					}
				}
			}
		}
		TranslateAcl(security.DiscretionaryAcl, AccessControlSections.Access);
		TranslateAcl(security.SystemAcl, AccessControlSections.Audit);
		changedSections = changed;
		if (changed == AccessControlSections.None)
			return descriptor;
		var result = new byte[security.BinaryLength];
		security.GetBinaryForm(result, 0);
		return result;
	}
}

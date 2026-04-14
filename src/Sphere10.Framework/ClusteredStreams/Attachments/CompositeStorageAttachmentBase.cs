// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Sphere10.Framework;

/// <summary>
/// An <see cref="IClusteredStreamsAttachment"/> that composes multiple child attachments into a single logical unit.
/// Each child is individually registered with the <see cref="ClusteredStreams"/> and owns its own reserved streams.
/// This composite itself declares <see cref="StreamCount"/> of 0 to avoid double-counting.
/// </summary>
public class CompositeStorageAttachmentBase : IClusteredStreamsAttachment {

	protected CompositeStorageAttachmentBase(ClusteredStreams streams, string attachmentID, params IClusteredStreamsAttachment[] children) {
		Guard.ArgumentNotNull(streams, nameof(streams));
		Guard.ArgumentNotNull(attachmentID, nameof(attachmentID));
		Guard.ArgumentNotNull(children, nameof(children));
		Guard.Argument(children.Length > 0, nameof(children), "At least one child attachment is required");
		AttachmentID = attachmentID;
		Children = children;

		// Register each child with the owning ClusteredStreams instance
		foreach (var Child in children)
			streams.RegisterAttachment(Child);
	}

	public string AttachmentID { get; }

	public int StreamCount => 0;

	public ClusteredStreams Streams => Children.FirstOrDefault()?.Streams;

	public bool IsAttached => Children.All(child => child.IsAttached);

	protected IClusteredStreamsAttachment[] Children { get; }

	public virtual void Attach() {
		foreach (var Child in Children)
			Child.Attach();
	}

	public virtual void Detach() {
		foreach (var Child in Children)
			Child.Detach();
	}

	public virtual void Flush() {
		foreach (var Child in Children)
			Child.Flush();
	}

	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected void CheckAttached() {
		if (!IsAttached)
			throw new InvalidOperationException("Composite attachment is not attached");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected void CheckNotAttached() {
		if (IsAttached)
			throw new InvalidOperationException("Composite attachment is already attached");
	}
}



// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows.BITS;

#region Notification Event Arguments

public class JobNotificationEventArgs : System.EventArgs {
}


public class JobErrorNotificationEventArgs : JobNotificationEventArgs {
	internal JobErrorNotificationEventArgs(BitsError error)
		: base() {
		Error = error;
	}

	public BitsError Error { get; }
}


public class NotificationEventArgs : JobNotificationEventArgs {
	internal NotificationEventArgs(BitsJob job) {
		Job = job;
	}

	public BitsJob Job { get; }
}


public class ErrorNotificationEventArgs : NotificationEventArgs {
	internal ErrorNotificationEventArgs(BitsJob job, BitsError error)
		: base(job) {
		Error = error;
	}

	public BitsError Error { get; }
}


public class BitsInterfaceNotificationEventArgs : NotificationEventArgs {
	private COMException exception;

	internal BitsInterfaceNotificationEventArgs(BitsJob job, COMException exception, string description)
		: base(job) {
		Description = description;
		this.exception = exception;
	}

	public string Message => this.exception.Message;

	public string Description { get; }

	public int HResult => this.exception.ErrorCode;
}

#endregion


internal class BitsNotification : IBackgroundCopyCallback {

	#region IBackgroundCopyCallback Members

	private EventHandler<NotificationEventArgs> onJobModified;
	private EventHandler<NotificationEventArgs> onJobTransfered;
	private EventHandler<ErrorNotificationEventArgs> onJobErrored;
	private BitsManager manager;

	internal BitsNotification(BitsManager manager) {
		this.manager = manager;
		;
	}

	public void JobTransferred(IBackgroundCopyJob pJob) {
		BitsJob job;
		if (null != this.onJobTransfered) {
			Guid guid;
			pJob.GetId(out guid);
			if (manager.Jobs.ContainsKey(guid)) {
				job = manager.Jobs[guid];
			} else {
				job = new BitsJob(manager, pJob);
			}
			this.onJobTransfered(this, new NotificationEventArgs(job));
		}
	}

	public void JobError(IBackgroundCopyJob pJob, IBackgroundCopyError pError) {
		BitsJob job;
		if (null != this.onJobErrored) {
			Guid guid;
			pJob.GetId(out guid);
			if (manager.Jobs.ContainsKey(guid)) {
				job = manager.Jobs[guid];
			} else {
				job = new BitsJob(manager, pJob);
			}
			this.onJobErrored(this, new ErrorNotificationEventArgs(job, new BitsError(job, pError)));
		}
	}

	public void JobModification(IBackgroundCopyJob pJob, uint dwReserved) {
		BitsJob job;
		if (null != this.onJobModified) {
			Guid guid;
			pJob.GetId(out guid);
			if (manager.Jobs.ContainsKey(guid)) {
				job = manager.Jobs[guid];
			} else {
				job = new BitsJob(manager, pJob);
			}
			this.onJobModified(this, new NotificationEventArgs(job));
		}
	}

	public event EventHandler<NotificationEventArgs> OnJobModifiedEvent {
		add { this.onJobModified += value; }
		remove { this.onJobModified -= value; }
	}

	public event EventHandler<NotificationEventArgs> OnJobTransferredEvent {
		add { this.onJobTransfered += value; }
		remove { this.onJobTransfered -= value; }
	}

	public event EventHandler<ErrorNotificationEventArgs> OnJobErrorEvent {
		add { this.onJobErrored += value; }
		remove { this.onJobErrored -= value; }
	}

	#endregion

}


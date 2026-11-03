// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms.DataVisualization;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Observes native chart selection and view changes while leaving data, painting and hover operational.</summary>
public sealed class ChartControlStateEventProvider : ControlStateEventProviderBase<Chart> {
	private bool _mouseChangePending;

	/// <summary>Attaches only completed selection and axis-view events.</summary>
	protected override void RegisterStateChangedListener(Chart control, EventHandlerEx eventHandler) {
		control.SelectionRangeChanged += SelectionRangeChanged;
		control.AxisViewChanged += AxisViewChanged;
		control.MouseUp += MouseUp;
		control.MouseCaptureChanged += MouseCaptureChanged;
	}

	/// <summary>Removes the selection and view listeners without owning the chart.</summary>
	protected override void DeregisterStateChangedListener(Chart control, EventHandlerEx eventHandler) {
		control.SelectionRangeChanged -= SelectionRangeChanged;
		control.AxisViewChanged -= AxisViewChanged;
		control.MouseUp -= MouseUp;
		control.MouseCaptureChanged -= MouseCaptureChanged;
		_mouseChangePending = false;
	}

	// Hover and incoming samples do not arrive through this completed range event.
	private void SelectionRangeChanged(object sender, CursorEventArgs args) {
		PublishChange();
	}

	// The native engine has already accepted the new viewport.
	private void AxisViewChanged(object sender, ViewEventArgs args) {
		PublishChange();
	}

	// A zoom gesture changes both axis view and selection; publish its final state once.
	private void PublishChange() {
		if (Control?.Capture == true)
			_mouseChangePending = true;
		else
			NotifyStateChanged();
	}

	// Native mouse-up processing has completed both range and viewport updates before this event.
	private void MouseUp(object sender, MouseEventArgs args) {
		PublishPendingMouseChange();
	}

	// Losing capture also ends an interrupted gesture without retaining a late notification.
	private void MouseCaptureChanged(object sender, EventArgs args) {
		if (Control?.Capture != true)
			PublishPendingMouseChange();
	}

	// Clear pending state before notifying to preserve reentrant event ordering.
	private void PublishPendingMouseChange() {
		if (!_mouseChangePending)
			return;

		_mouseChangePending = false;
		NotifyStateChanged();
	}
}

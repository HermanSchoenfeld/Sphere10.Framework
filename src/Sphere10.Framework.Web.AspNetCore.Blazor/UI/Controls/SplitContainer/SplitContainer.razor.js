// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

// One behavior instance owns the splitter's pointer and keyboard handling for the lifetime of the rendered container.
// The position follows the pointer locally and is reported to .NET once, when the drag or key press completes.
export function Initialize(element, receiver) {
	const splitter = element.querySelector(":scope > .sphere10-splitter");
	let drag = null;
	let disposed = false;

	const Horizontal = () => element.dataset.splitOrientation === "horizontal";
	const Variable = name => parseFloat(getComputedStyle(element).getPropertyValue(name)) || 0;
	const Current = () => Variable("--sphere10-split-position");

	// Keeps both panels at or above their minimum sizes while the container has a measurable extent.
	const Clamp = position => {
		const rect = element.getBoundingClientRect();
		const extent = Horizontal() ? rect.height : rect.width;
		const usable = extent - Variable("--sphere10-splitter-width");
		if (usable <= 0)
			return Math.min(100, Math.max(0, position));
		const minimum = Variable("--sphere10-split-panel1-minimum") / usable * 100;
		const maximum = 100 - Variable("--sphere10-split-panel2-minimum") / usable * 100;
		return Math.min(Math.max(position, Math.min(minimum, maximum)), Math.max(minimum, maximum));
	};
	const Apply = position => element.style.setProperty("--sphere10-split-position", String(position));
	const Report = position => receiver.invokeMethodAsync("SetSplitterPositionAsync", Math.round(position * 100) / 100).catch(() => {});

	// Converts the pointer's offset within the container to the first panel's share.
	const PositionAt = event => {
		const rect = element.getBoundingClientRect();
		const usable = (Horizontal() ? rect.height : rect.width) - Variable("--sphere10-splitter-width");
		const offset = Horizontal() ? event.clientY - rect.top : event.clientX - rect.left;
		return usable > 0 ? Clamp(offset / usable * 100) : Current();
	};
	const OnPointerDown = event => {
		if (event.button !== 0)
			return;
		event.preventDefault();
		drag = { pointerId: event.pointerId, position: Current() };
		splitter.setPointerCapture(event.pointerId);
		element.classList.add("sphere10-split-dragging");
	};
	const OnPointerMove = event => {
		if (!drag || event.pointerId !== drag.pointerId)
			return;
		drag.position = PositionAt(event);
		Apply(drag.position);
	};
	const StopDrag = event => {
		if (!drag || event.pointerId !== drag.pointerId)
			return;
		if (splitter.hasPointerCapture(drag.pointerId))
			splitter.releasePointerCapture(drag.pointerId);
		element.classList.remove("sphere10-split-dragging");
		const position = drag.position;
		drag = null;
		Report(position);
	};

	// Arrow keys move by 2 percent (10 with Shift); Home and End move to the minimum sizes.
	const OnKeyDown = event => {
		const step = event.shiftKey ? 10 : 2;
		const decrease = Horizontal() ? "ArrowUp" : "ArrowLeft";
		const increase = Horizontal() ? "ArrowDown" : "ArrowRight";
		let position;
		if (event.key === decrease) position = Current() - step;
		else if (event.key === increase) position = Current() + step;
		else if (event.key === "Home") position = 0;
		else if (event.key === "End") position = 100;
		else return;
		event.preventDefault();
		position = Clamp(position);
		Apply(position);
		Report(position);
	};
	const Dispose = () => {
		if (disposed)
			return;
		disposed = true;
		if (drag && splitter.hasPointerCapture(drag.pointerId))
			splitter.releasePointerCapture(drag.pointerId);
		drag = null;
		splitter.removeEventListener("pointerdown", OnPointerDown);
		splitter.removeEventListener("pointermove", OnPointerMove);
		splitter.removeEventListener("pointerup", StopDrag);
		splitter.removeEventListener("pointercancel", StopDrag);
		splitter.removeEventListener("keydown", OnKeyDown);
	};
	splitter.addEventListener("pointerdown", OnPointerDown);
	splitter.addEventListener("pointermove", OnPointerMove);
	splitter.addEventListener("pointerup", StopDrag);
	splitter.addEventListener("pointercancel", StopDrag);
	splitter.addEventListener("keydown", OnKeyDown);
	return { Dispose };
}

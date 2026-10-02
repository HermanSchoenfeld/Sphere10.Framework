// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

const popups = new WeakMap();
const activePopups = new WeakMap();

export function Focus(trigger) {
	if (trigger?.isConnected && !trigger.disabled)
		trigger.focus({ preventScroll: true });
}

// Each opening owns its listeners and generation so stale dismissals cannot close a newer picker.
export function Show(panel, trigger, receiver, generation) {
	if (!panel?.isConnected || !trigger?.isConnected)
		return;
	const existing = popups.get(panel);
	if (existing?.generation === generation)
		return;
	existing?.Cleanup(false);
	let disposed = false;
	let frame = 0;
	const gap = 4;
	const margin = 8;
	const SetStyle = (property, value) => {
		if (panel.style[property] !== value)
			panel.style[property] = value;
	};
	const Position = () => {
		frame = 0;
		if (disposed || !panel.isConnected)
			return;
		const anchor = trigger.getBoundingClientRect();
		// Viewport rectangles include inherited CSS zoom; positioned lengths use the panel's unzoomed CSS units.
		const cssWidth = parseFloat(getComputedStyle(panel).width);
		const scale = cssWidth > 0 ? panel.getBoundingClientRect().width / cssWidth : 1;
		SetStyle("maxWidth", Math.max(0, (window.innerWidth - margin * 2) / scale) + "px");
		const below = Math.max(0, window.innerHeight - anchor.bottom - gap - margin);
		const above = Math.max(0, anchor.top - gap - margin);
		const desiredHeight = (panel.scrollHeight + panel.offsetHeight - panel.clientHeight) * scale;
		const placeBelow = below >= desiredHeight || below >= above;
		SetStyle("maxHeight", Math.max(0, Math.min(window.innerHeight - margin * 2, placeBelow ? below : above) / scale) + "px");
		const bounds = panel.getBoundingClientRect();
		const left = Math.max(margin, Math.min(anchor.left, window.innerWidth - bounds.width - margin));
		const top = placeBelow ? anchor.bottom + gap : anchor.top - bounds.height - gap;
		SetStyle("left", left / scale + "px");
		SetStyle("top", Math.max(margin, Math.min(top, window.innerHeight - bounds.height - margin)) / scale + "px");
	};
	const Schedule = () => {
		if (!frame && !disposed)
			frame = requestAnimationFrame(Position);
	};
	const Cleanup = restoreFocus => {
		if (disposed)
			return;
		disposed = true;
		cancelAnimationFrame(frame);
		window.removeEventListener("resize", Schedule);
		document.removeEventListener("scroll", Schedule, true);
		document.removeEventListener("pointerdown", OnOutsidePointerDown, true);
		panel.removeEventListener("keydown", OnKeyDown);
		panel.removeEventListener("toggle", OnToggle);
		resizeObserver.disconnect();
		removalObserver.disconnect();
		popups.delete(panel);
		if (activePopups.get(trigger) === state)
			activePopups.delete(trigger);
		if (restoreFocus)
			Focus(trigger);
	};
	const Dismiss = () => {
		if (disposed)
			return;
		Cleanup(false);
		if (panel.matches(":popover-open"))
			panel.hidePopover();
		receiver.invokeMethodAsync("DismissAsync", generation).then(() => {
			if (!activePopups.has(trigger))
				Focus(trigger);
		}).catch(() => {});
	};
	const OnOutsidePointerDown = event => {
		if (!panel.contains(event.target) && !trigger.contains(event.target))
			Dismiss();
	};
	const OnKeyDown = event => {
		if (event.key === "Escape") {
			event.preventDefault();
			event.stopPropagation();
			Dismiss();
		}
	};
	const OnToggle = event => {
		if (event.newState === "closed") {
			if (panel.isConnected)
				Dismiss();
			else
				Cleanup(false);
		}
	};
	const resizeObserver = new ResizeObserver(Schedule);
	const removalObserver = new MutationObserver(() => {
		if (!panel.isConnected || !trigger.isConnected)
			Cleanup(false);
	});
	const state = { Cleanup, generation };
	popups.set(panel, state);
	activePopups.set(trigger, state);
	panel.addEventListener("toggle", OnToggle);
	panel.addEventListener("keydown", OnKeyDown);
	document.addEventListener("pointerdown", OnOutsidePointerDown, true);
	document.addEventListener("scroll", Schedule, true);
	window.addEventListener("resize", Schedule);
	resizeObserver.observe(panel);
	removalObserver.observe(document.body, { childList: true, subtree: true });
	if (!panel.matches(":popover-open"))
		panel.showPopover();
	Position();
	(panel.querySelector("input:not(:disabled), button:not(:disabled), [tabindex='0']") ?? panel).focus({ preventScroll: true });
}

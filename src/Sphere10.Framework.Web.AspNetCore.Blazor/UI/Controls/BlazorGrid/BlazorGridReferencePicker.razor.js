// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

const popups = new WeakMap();
const activePopups = new WeakMap();

export function focus(trigger) {
	if (trigger?.isConnected && !trigger.disabled)
		trigger.focus({ preventScroll: true });
}

export function show(panel, trigger, receiver, generation) {
	if (!panel?.isConnected || !trigger?.isConnected)
		return;
	const existing = popups.get(panel);
	if (existing?.generation === generation)
		return;
	existing?.cleanup(false);
	let disposed = false;
	let frame = 0;
	const gap = 4;
	const margin = 8;
	const setStyle = (property, value) => {
		if (panel.style[property] !== value)
			panel.style[property] = value;
	};
	const position = () => {
		frame = 0;
		if (disposed || !panel.isConnected)
			return;
		const anchor = trigger.getBoundingClientRect();
		// Viewport rectangles include inherited CSS zoom; positioned lengths use the panel's unzoomed CSS units.
		const cssWidth = parseFloat(getComputedStyle(panel).width);
		const scale = cssWidth > 0 ? panel.getBoundingClientRect().width / cssWidth : 1;
		setStyle("maxWidth", Math.max(0, (window.innerWidth - margin * 2) / scale) + "px");
		const below = Math.max(0, window.innerHeight - anchor.bottom - gap - margin);
		const above = Math.max(0, anchor.top - gap - margin);
		const desiredHeight = (panel.scrollHeight + panel.offsetHeight - panel.clientHeight) * scale;
		const placeBelow = below >= desiredHeight || below >= above;
		setStyle("maxHeight", Math.max(0, Math.min(window.innerHeight - margin * 2, placeBelow ? below : above) / scale) + "px");
		const bounds = panel.getBoundingClientRect();
		const left = Math.max(margin, Math.min(anchor.left, window.innerWidth - bounds.width - margin));
		const top = placeBelow ? anchor.bottom + gap : anchor.top - bounds.height - gap;
		setStyle("left", left / scale + "px");
		setStyle("top", Math.max(margin, Math.min(top, window.innerHeight - bounds.height - margin)) / scale + "px");
	};
	const schedule = () => {
		if (!frame && !disposed)
			frame = requestAnimationFrame(position);
	};
	const cleanup = restoreFocus => {
		if (disposed)
			return;
		disposed = true;
		cancelAnimationFrame(frame);
		window.removeEventListener("resize", schedule);
		document.removeEventListener("scroll", schedule, true);
		document.removeEventListener("pointerdown", outside, true);
		panel.removeEventListener("keydown", keyDown);
		panel.removeEventListener("toggle", toggled);
		resizeObserver.disconnect();
		removalObserver.disconnect();
		popups.delete(panel);
		if (activePopups.get(trigger) === state)
			activePopups.delete(trigger);
		if (restoreFocus)
			focus(trigger);
	};
	const dismiss = () => {
		if (disposed)
			return;
		cleanup(false);
		if (panel.matches(":popover-open"))
			panel.hidePopover();
		receiver.invokeMethodAsync("DismissAsync", generation).then(() => {
			if (!activePopups.has(trigger))
				focus(trigger);
		}).catch(() => {});
	};
	const outside = event => {
		if (!panel.contains(event.target) && !trigger.contains(event.target))
			dismiss();
	};
	const keyDown = event => {
		if (event.key === "Escape") {
			event.preventDefault();
			event.stopPropagation();
			dismiss();
		}
	};
	const toggled = event => {
		if (event.newState === "closed") {
			if (panel.isConnected)
				dismiss();
			else
				cleanup(false);
		}
	};
	const resizeObserver = new ResizeObserver(schedule);
	const removalObserver = new MutationObserver(() => {
		if (!panel.isConnected || !trigger.isConnected)
			cleanup(false);
	});
	const state = { cleanup, generation };
	popups.set(panel, state);
	activePopups.set(trigger, state);
	panel.addEventListener("toggle", toggled);
	panel.addEventListener("keydown", keyDown);
	document.addEventListener("pointerdown", outside, true);
	document.addEventListener("scroll", schedule, true);
	window.addEventListener("resize", schedule);
	resizeObserver.observe(panel);
	removalObserver.observe(document.body, { childList: true, subtree: true });
	if (!panel.matches(":popover-open"))
		panel.showPopover();
	position();
	(panel.querySelector("input:not(:disabled), button:not(:disabled), [tabindex='0']") ?? panel).focus({ preventScroll: true });
}

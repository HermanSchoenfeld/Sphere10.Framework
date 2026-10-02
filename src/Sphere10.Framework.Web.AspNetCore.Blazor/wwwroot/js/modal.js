// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.
const dialogs = new Map();

// Keep focus inside the active dialog and route dismissal through its Blazor close button.
export function Show(element) {
	if (dialogs.has(element)) return;
	const previousFocus = document.activeElement;
	const backdrop = document.createElement("div");
	backdrop.className = "modal-backdrop fade show";
	document.body.appendChild(backdrop);
	const Close = () => element.querySelector("button.close")?.click();
	const OnClick = event => { if (event.target === element) Close(); };
	const OnKeyDown = event => {
		if (event.key === "Escape") Close();
		if (event.key !== "Tab") return;
		const controls = [...element.querySelectorAll('button:not([disabled]), a[href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex="0"]')].filter(control => control.getClientRects().length);
		const first = controls[0] ?? element;
		const last = controls.at(-1) ?? element;
		if (event.shiftKey && (document.activeElement === first || document.activeElement === element)) {
			event.preventDefault();
			last.focus();
		} else if (!event.shiftKey && (document.activeElement === last || !controls.length)) {
			event.preventDefault();
			first.focus();
		}
	};
	const observer = new MutationObserver(() => { if (!element.isConnected) Hide(element); });
	observer.observe(document.body, { childList: true, subtree: true });
	dialogs.set(element, { backdrop, observer, previousFocus, OnClick, OnKeyDown });
	element.addEventListener("click", OnClick);
	element.addEventListener("keydown", OnKeyDown);
	element.style.display = "block";
	element.classList.add("show");
	element.removeAttribute("aria-hidden");
	element.setAttribute("aria-modal", "true");
	document.body.classList.add("modal-open");
	(element.querySelector("[autofocus]") ?? element).focus();
}

// Release handlers and restore focus, including when navigation removes a dialog from the DOM.
export function Hide(element) {
	const state = dialogs.get(element);
	if (!state) return;
	state.observer.disconnect();
	state.backdrop.remove();
	element.removeEventListener("click", state.OnClick);
	element.removeEventListener("keydown", state.OnKeyDown);
	element.classList.remove("show");
	element.style.display = "none";
	element.setAttribute("aria-hidden", "true");
	element.removeAttribute("aria-modal");
	dialogs.delete(element);
	if (!dialogs.size) document.body.classList.remove("modal-open");
	if (state.previousFocus?.isConnected) state.previousFocus.focus();
}

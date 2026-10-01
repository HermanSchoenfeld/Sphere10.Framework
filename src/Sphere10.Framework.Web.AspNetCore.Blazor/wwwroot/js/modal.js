// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.
const dialogs = new Map();

export function show(element) {
	if (dialogs.has(element)) return;
	const previousFocus = document.activeElement;
	const backdrop = document.createElement("div");
	backdrop.className = "modal-backdrop fade show";
	document.body.appendChild(backdrop);
	const close = () => element.querySelector("button.close")?.click();
	const click = event => { if (event.target === element) close(); };
	const keydown = event => {
		if (event.key === "Escape") close();
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
	const observer = new MutationObserver(() => { if (!element.isConnected) hide(element); });
	observer.observe(document.body, { childList: true, subtree: true });
	dialogs.set(element, { backdrop, observer, previousFocus, click, keydown });
	element.addEventListener("click", click);
	element.addEventListener("keydown", keydown);
	element.style.display = "block";
	element.classList.add("show");
	element.removeAttribute("aria-hidden");
	element.setAttribute("aria-modal", "true");
	document.body.classList.add("modal-open");
	(element.querySelector("[autofocus]") ?? element).focus();
}

export function hide(element) {
	const state = dialogs.get(element);
	if (!state) return;
	state.observer.disconnect();
	state.backdrop.remove();
	element.removeEventListener("click", state.click);
	element.removeEventListener("keydown", state.keydown);
	element.classList.remove("show");
	element.style.display = "none";
	element.setAttribute("aria-hidden", "true");
	element.removeAttribute("aria-modal");
	dialogs.delete(element);
	if (!dialogs.size) document.body.classList.remove("modal-open");
	if (state.previousFocus?.isConnected) state.previousFocus.focus();
}

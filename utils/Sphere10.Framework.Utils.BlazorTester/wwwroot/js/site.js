// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

let previousModalFocus;
window.showModal = () => {
	const modal = document.getElementById("modal");
	if (!modal) return;
	previousModalFocus = document.activeElement;
	modal.style.display = "block";
	modal.classList.add("show");
	modal.setAttribute("aria-modal", "true");
	modal.removeAttribute("aria-hidden");
	document.body.classList.add("modal-open");
	if (!document.getElementById("sphere10-modal-backdrop")) {
		const backdrop = document.createElement("div");
		backdrop.id = "sphere10-modal-backdrop";
		backdrop.className = "modal-backdrop show";
		document.body.appendChild(backdrop);
	}
	modal.querySelector("button, input, textarea, select")?.focus();
};
window.hideModal = () => {
	const modal = document.getElementById("modal");
	if (modal) {
		modal.style.display = "none";
		modal.classList.remove("show");
		modal.removeAttribute("aria-modal");
		modal.setAttribute("aria-hidden", "true");
	}
	document.getElementById("sphere10-modal-backdrop")?.remove();
	document.body.classList.remove("modal-open");
	previousModalFocus?.focus();
};
window.addDropdownHover = () => {};
window.initializeToolTips = () => {};
window.initializeSearchDropdowns = () => {};
window.dispatchContentLoadedEvent = () => {};
window.clipboardCopy = { copyText: text => navigator.clipboard.writeText(text) };
document.addEventListener("click", event => {
	const toggle = event.target.closest('[data-toggle="dropdown"], [data-toggle="collapse"]');
	if (!toggle) return;
	event.preventDefault();
	if (toggle.dataset.toggle === "dropdown") {
		const menu = toggle.parentElement.querySelector(".dropdown-menu");
		menu?.classList.toggle("show");
		toggle.setAttribute("aria-expanded", menu?.classList.contains("show") ? "true" : "false");
	} else {
		const target = toggle.dataset.target || toggle.getAttribute("href");
		if (target?.startsWith("#") && target.length > 1) {
			const panel = document.getElementById(target.slice(1));
			panel?.classList.toggle("show");
			toggle.setAttribute("aria-expanded", panel?.classList.contains("show") ? "true" : "false");
		}
	}
});

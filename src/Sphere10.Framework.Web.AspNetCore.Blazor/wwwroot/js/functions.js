// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Distributed under the MIT software license. See LICENSE.

window.dispatchContentLoadedEvent = () => document.dispatchEvent(new Event("sphere10:contentloaded"));

window.showModal = () => {
	const modal = document.getElementById("modal");
	if (!modal)
		return;
	modal.style.display = "block";
	modal.classList.add("show");
	modal.removeAttribute("aria-hidden");
	modal.setAttribute("aria-modal", "true");
	document.body.classList.add("modal-open");
};

window.hideModal = () => {
	const modal = document.getElementById("modal");
	if (!modal)
		return;
	modal.style.display = "none";
	modal.classList.remove("show");
	modal.setAttribute("aria-hidden", "true");
	modal.removeAttribute("aria-modal");
	document.body.classList.remove("modal-open");
};

// Optional compatibility hook for applications that supply the DataTables plugin.
window.initDataTableById = (id, options) => {
	if (!window.jQuery?.fn?.DataTable)
		throw new Error("initDataTableById requires the optional DataTables plugin.");
	return window.jQuery(document.getElementById(id)).DataTable(options);
};

window.clipboardCopy = { copyText: text => navigator.clipboard.writeText(text) };

document.addEventListener("input", event => {
	if (!event.target.matches(".search-input"))
		return;
	const results = event.target.closest(".input-group")?.querySelector(".search-input-results");
	results?.classList.toggle("show", event.target.value.length > 0);
});

document.addEventListener("focusout", event => {
	if (event.target.matches(".search-input"))
		event.target.closest(".input-group")?.querySelector(".search-input-results")?.classList.remove("show");
});


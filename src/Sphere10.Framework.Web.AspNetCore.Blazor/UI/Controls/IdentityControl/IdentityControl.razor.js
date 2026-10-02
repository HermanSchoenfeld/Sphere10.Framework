// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

const instances = new WeakMap();

export function Initialize(element) {
	Dispose(element);
	const HandleKeyDown = event => {
		if (!(event.target instanceof Element) || event.altKey || event.ctrlKey || event.metaKey)
			return;
		const button = event.target.closest("button");
		if (!button || button.closest(".sphere10-identity") !== element)
			return;
		const triggerKey = button.classList.contains("sphere10-identity-trigger")
			&& ["ArrowDown", "ArrowUp", "Escape"].includes(event.key);
		const menuKey = button.matches('[role="menuitem"]')
			&& ["ArrowDown", "ArrowUp", "Home", "End", "Escape"].includes(event.key);
		if (triggerKey || menuKey)
			event.preventDefault();
	};
	element.addEventListener("keydown", HandleKeyDown);
	instances.set(element, HandleKeyDown);
}

export function Dispose(element) {
	const listener = instances.get(element);
	if (listener) {
		element.removeEventListener("keydown", listener);
		instances.delete(element);
	}
}

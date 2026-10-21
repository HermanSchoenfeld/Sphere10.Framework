// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Distributed under the MIT software license, see the accompanying LICENSE.

const Prefix = "sphere10.startup-screen.";

export function ReadStartupScreen(key) {
	const value = localStorage.getItem(Prefix + key);
	if (!value)
		return null;
	try {
		const identity = JSON.parse(value);
		return identity && typeof identity === "object" && typeof identity.blockId === "string"
			&& typeof identity.menuItemId === "string" && typeof identity.screenTypeName === "string"
			&& typeof identity.screenAssemblyName === "string" ? identity : null;
	} catch {
		return null;
	}
}

export function WriteStartupScreen(key, identity) {
	localStorage.setItem(Prefix + key, JSON.stringify(identity));
}

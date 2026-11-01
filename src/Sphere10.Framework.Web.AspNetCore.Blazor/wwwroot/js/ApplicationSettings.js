// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Distributed under the MIT software license, see the accompanying LICENSE.

// A browser identity is independent of a temporary circuit and is shared only by this origin's anonymous profile.
export function GetAnonymousProfile() {
	const key = "sphere10.settings.browser-profile";
	let identity = localStorage.getItem(key);
	if (!identity) {
		identity = crypto.randomUUID();
		localStorage.setItem(key, identity);
	}
	return identity;
}

// Missing values use declared defaults; malformed values and unavailable storage remain observable to the caller.
export function Read(key) {
	return localStorage.getItem(key);
}

// A successful return is the browser acceptance boundary used by the catalog's ordered save gate.
export function Write(key, value) {
	localStorage.setItem(key, value);
}

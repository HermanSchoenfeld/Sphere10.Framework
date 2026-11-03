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

// A successful return confirms the browser write before SettingsObject.SaveAsync publishes its change notification.
export function Write(key, value) {
	localStorage.setItem(key, value);
}

// Delete only the requested typed settings record.
export function Delete(key) {
	localStorage.removeItem(key);
}

// Clear this application's namespace without affecting another application's preferences or the browser identity.
export function Clear(prefix) {
	for (const key of Object.keys(localStorage)) {
		if (key.startsWith(prefix))
			localStorage.removeItem(key);
	}
}

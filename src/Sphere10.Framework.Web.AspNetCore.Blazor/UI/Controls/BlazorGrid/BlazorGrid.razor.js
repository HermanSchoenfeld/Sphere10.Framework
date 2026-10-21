// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

// One behavior instance owns column geometry and observers for the lifetime of this rendered grid.
export function Initialize(element, receiver) {
	const viewport = element.querySelector(".sphere10-grid-viewport");
	const table = element.querySelector("table");
	let autoPageSize = false;
	let lastPageSize = 0;
	let capacityCeiling = 9999;
	let viewportWidth = 0;
	let viewportHeight = 0;
	let pageIdentity = "";
	let layoutPending = true;
	let frame = 0;
	let disposed = false;
	let drag = null;
	let resizedWidths = new WeakMap();
	const naturalWidths = new WeakMap();
	let settingsKey = null;
	let settings = null;
	let storedWidths = {};
	const Configuration = column => column.dataset.gridWidth + ":" + column.dataset.gridExpands + ":" + element.dataset.gridAutoSize;
	const WriteSettings = () => {
		if (!settingsKey || !settings) return;
		try {
			localStorage.setItem("sphere10-grid:" + settingsKey, JSON.stringify({ ...settings, columnWidths: storedWidths }));
		} catch {
			// Private browsing, storage policy or quota must not disable a usable grid.
		}
	};

	const SetStyle = (target, property, value) => {
		if (target.style[property] !== value)
			target.style[property] = value;
	};
	const MeasureNaturalWidths = columns => {
		if (element.dataset.gridAutoSize !== "true" || element.getAttribute("aria-busy") === "true"
			|| element.querySelector(".sphere10-grid-edit-bar") || !table.tBodies[0]?.querySelector("tr[aria-selected]"))
			return;
		if (columns.every(column => naturalWidths.get(column)?.configuration === Configuration(column)))
			return;

		// Measure a detached presentation copy once; never resize the live rows while evaluating their natural widths.
		const probe = table.cloneNode(true);
		probe.removeAttribute("id");
		for (const identified of probe.querySelectorAll("[id]")) identified.removeAttribute("id");
		Object.assign(probe.style, { position: "absolute", visibility: "hidden", pointerEvents: "none", width: "max-content", minWidth: "0", tableLayout: "auto" });
		for (const column of probe.querySelectorAll("col")) column.style.width = "auto";
		for (const cell of probe.querySelectorAll("th, td")) Object.assign(cell.style, { whiteSpace: "nowrap", overflowWrap: "normal" });
		viewport.append(probe);
		try {
			const scale = table.getBoundingClientRect().width / (parseFloat(getComputedStyle(table).width) || 1);
			const headers = probe.tHead.rows[0].cells;
			for (let index = 0; index < columns.length; index++) {
				if (naturalWidths.get(columns[index])?.configuration === Configuration(columns[index])) continue;
				naturalWidths.set(columns[index], { width: Math.ceil(headers[index].getBoundingClientRect().width / (scale || 1)), configuration: Configuration(columns[index]) });
			}
		} finally {
			probe.remove();
		}
	};
	const LayoutColumns = () => {
		layoutPending = false;
		if (viewportWidth <= 0)
			return;
		const columns = Array.from(table.querySelector("colgroup").children);
		if (!columns.length)
			return;
		MeasureNaturalWidths(columns);
		const widths = columns.map(column => {
			const configuredWidth = Number(column.dataset.gridWidth);
			const resized = resizedWidths.get(column);
			if (resized && resized.configuration !== Configuration(column))
				resizedWidths.delete(column);
			if (!resizedWidths.has(column) && Number.isFinite(storedWidths[column.dataset.gridKey]))
				resizedWidths.set(column, { width: storedWidths[column.dataset.gridKey], configuration: Configuration(column) });
			const natural = naturalWidths.get(column);
			return resizedWidths.get(column)?.width ?? Math.max(configuredWidth, natural?.configuration === Configuration(column) ? natural.width : 0);
		});
		const stretches = columns.map(column => column.dataset.gridExpands === "true" && !resizedWidths.has(column));
		const stretchCount = stretches.filter(Boolean).length;
		const minimumWidth = widths.reduce((total, width) => total + width, 0);
		// clientWidth rounds fractional content widths up, which can repeatedly introduce a horizontal scrollbar at browser zoom.
		const tableWidth = stretchCount ? Math.max(minimumWidth, Math.floor(viewportWidth)) : minimumWidth;
		const extraWidth = stretchCount ? (tableWidth - minimumWidth) / stretchCount : 0;
		// Table-column percentage calculations are not interoperable; allocate spare pixels explicitly.
		for (let index = 0; index < columns.length; index++)
			SetStyle(columns[index], "width", (widths[index] + (stretches[index] ? extraWidth : 0)) + "px");
		SetStyle(table, "minWidth", minimumWidth + "px");
		SetStyle(table, "width", tableWidth + "px");
	};
	const Measure = () => {
		frame = 0;
		if (disposed || !element.isConnected)
			return;
		if (layoutPending)
			LayoutColumns();
		if (!autoPageSize || element.getAttribute("aria-busy") === "true" || element.querySelector(".sphere10-grid-edit-bar"))
			return;

		const rows = Array.from(table?.tBodies[0]?.rows ?? []);
		if (!rows.length || rows[0].querySelector(".sphere10-grid-empty"))
			return;
		const scale = viewport.getBoundingClientRect().height / (viewport.offsetHeight || 1);
		const available = viewport.clientHeight * scale - table.tHead.getBoundingClientRect().height - (table.caption?.getBoundingClientRect().height ?? 0);
		const heights = rows.map(row => row.getBoundingClientRect().height);
		if (heights.some(height => height <= 0) || available <= 0)
			return;
		// Count complete ordered rows, then estimate only the rows not yet fetched. An observed overflow bounds
		// further estimates for this viewport/page so a large next row cannot cause a shrink/grow request loop.
		let used = 0;
		let capacity = 0;
		for (const height of heights) {
			if (used + height > available + .5) break;
			used += height;
			capacity++;
		}
		if (capacity < heights.length)
			capacityCeiling = Math.min(capacityCeiling, Math.max(1, capacity));
		else
			capacity += Math.floor(Math.max(0, available - used) / heights[heights.length - 1]);
		const pageSize = Math.min(capacityCeiling, Math.max(1, capacity));
		if (pageSize !== lastPageSize) {
			lastPageSize = pageSize;
			receiver.invokeMethodAsync("SetViewportPageSizeAsync", pageSize).catch(() => {});
		}
	};
	const Schedule = () => {
		if (!frame && !disposed)
			frame = requestAnimationFrame(Measure);
	};
	const StopDrag = event => {
		if (event && drag && event.pointerId !== drag.pointerId)
			return;
		if (drag?.grip.hasPointerCapture(drag.pointerId))
			drag.grip.releasePointerCapture(drag.pointerId);
		if (drag) {
			const column = drag.columns[drag.index];
			if (column.dataset.gridKey && resizedWidths.has(column)) {
				storedWidths[column.dataset.gridKey] = resizedWidths.get(column).width;
				WriteSettings();
			}
		}
		drag = null;
	};
	const OnPointerDown = event => {
		const grip = event.target.closest(".sphere10-grid-resizer");
		if (!grip || event.button !== 0 || grip.closest("table") !== table)
			return;
		event.preventDefault();
		event.stopPropagation();
		const header = grip.closest("th");
		const columns = Array.from(table.querySelector("colgroup").children);
		const scale = table.getBoundingClientRect().width / parseFloat(getComputedStyle(table).width);
		const widths = Array.from(table.tHead.rows[0].cells, cell => cell.getBoundingClientRect().width / scale);
		drag = { grip, columns, widths, scale, index: header.cellIndex, pointerId: event.pointerId, startX: event.clientX };
		grip.setPointerCapture(event.pointerId);
	};
	const OnPointerMove = event => {
		if (!drag || event.pointerId !== drag.pointerId)
			return;
		const width = Math.max(40, drag.widths[drag.index] + (event.clientX - drag.startX) / drag.scale);
		// Only the dragged column receives an explicit override; other preferred columns still share spare width.
		const column = drag.columns[drag.index];
		resizedWidths.set(column, { width, configuration: Configuration(column) });
		layoutPending = true;
		capacityCeiling = 9999;
		Schedule();
	};
	const Dispose = () => {
		if (disposed)
			return;
		disposed = true;
		StopDrag();
		cancelAnimationFrame(frame);
		resizeObserver.disconnect();
		removalObserver.disconnect();
		element.removeEventListener("pointerdown", OnPointerDown);
		element.removeEventListener("pointermove", OnPointerMove);
		element.removeEventListener("pointerup", StopDrag);
		element.removeEventListener("pointercancel", StopDrag);
	};
	const resizeObserver = new ResizeObserver(entries => {
		let measurementPending = false;
		for (const entry of entries) {
			if (entry.target === viewport && viewportWidth !== entry.contentRect.width) {
				viewportWidth = entry.contentRect.width;
				capacityCeiling = 9999;
				layoutPending = true;
			}
			if (entry.target === viewport && viewportHeight !== entry.contentRect.height) {
				viewportHeight = entry.contentRect.height;
				capacityCeiling = 9999;
			}
			// Table height changes can affect page capacity; they must not feed column widths back into layout.
			measurementPending ||= layoutPending || autoPageSize;
		}
		if (measurementPending)
			Schedule();
	});
	resizeObserver.observe(viewport);
	resizeObserver.observe(table);
	const removalObserver = new MutationObserver(() => {
		if (!element.isConnected)
			Dispose();
	});
	removalObserver.observe(document.body, { childList: true, subtree: true });
	element.addEventListener("pointerdown", OnPointerDown);
	element.addEventListener("pointermove", OnPointerMove);
	element.addEventListener("pointerup", StopDrag);
	element.addEventListener("pointercancel", StopDrag);

	// Blazor calls Update after rendering; geometry changes are coalesced into the next animation frame.
	return {
		ReadSettings(key, defaults) {
			settingsKey = key;
			settings = defaults;
			storedWidths = {};
			resizedWidths = new WeakMap();
			if (!key) return null;
			try {
				const parsed = JSON.parse(localStorage.getItem("sphere10-grid:" + key));
				if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) return null;
				// Accept old or partial preference records; the controller validates query values too.
				settings = {
					pageMode: [0, 1, 2].includes(parsed.pageMode) ? parsed.pageMode : 0,
					pageSize: Number.isInteger(parsed.pageSize) ? Math.min(9999, Math.max(1, parsed.pageSize)) : 10,
					pageNumber: Number.isInteger(parsed.pageNumber) ? Math.min(2147483647, Math.max(0, parsed.pageNumber)) : 0,
					sortColumnName: typeof parsed.sortColumnName === "string" ? parsed.sortColumnName : null,
					sortDirection: [0, 1, 2].includes(parsed.sortDirection) ? parsed.sortDirection : 0
				};
				if (parsed.columnWidths && typeof parsed.columnWidths === "object")
					for (const [name, width] of Object.entries(parsed.columnWidths))
						if (Number.isFinite(width) && width >= 40 && width <= 100000) storedWidths[name] = width;
				return { ...settings, columnWidths: storedWidths };
			} catch {
				return null;
			}
		},
		WriteSettings(key, value) {
			if (!key) return;
			if (settingsKey !== key) storedWidths = {};
			settingsKey = key;
			settings = value;
			WriteSettings();
		},
		Update(enabled) {
			if (enabled && !autoPageSize)
				lastPageSize = 0;
			autoPageSize = enabled;
			if (pageIdentity !== element.dataset.gridPageIdentity) {
				pageIdentity = element.dataset.gridPageIdentity;
				capacityCeiling = 9999;
				lastPageSize = 0;
			}
			// Rendering may replace column definitions or restore their declarative inline widths.
			layoutPending = true;
			Schedule();
		},
		Dispose
	};
}

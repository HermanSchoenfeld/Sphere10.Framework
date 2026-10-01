// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

export function initialize(element, receiver) {
	const viewport = element.querySelector(".sphere10-grid-viewport");
	const table = element.querySelector("table");
	let autoPageSize = false;
	let lastPageSize = 0;
	let maximumRowHeight = 0;
	let viewportWidth = 0;
	let layoutPending = true;
	let frame = 0;
	let disposed = false;
	let drag = null;
	const resizedWidths = new WeakMap();

	const setStyle = (target, property, value) => {
		if (target.style[property] !== value)
			target.style[property] = value;
	};
	const layoutColumns = () => {
		layoutPending = false;
		if (viewportWidth <= 0)
			return;
		const columns = Array.from(table.querySelector("colgroup").children);
		if (!columns.length)
			return;
		const widths = columns.map(column => {
			const configuredWidth = Number(column.dataset.gridWidth);
			const resized = resizedWidths.get(column);
			if (resized && resized.configuration !== column.dataset.gridWidth + ":" + column.dataset.gridExpands)
				resizedWidths.delete(column);
			return resizedWidths.get(column)?.width ?? configuredWidth;
		});
		const stretches = columns.map(column => column.dataset.gridExpands === "true" && !resizedWidths.has(column));
		const stretchCount = stretches.filter(Boolean).length;
		const minimumWidth = widths.reduce((total, width) => total + width, 0);
		// clientWidth rounds fractional content widths up, which can repeatedly introduce a horizontal scrollbar at browser zoom.
		const tableWidth = stretchCount ? Math.max(minimumWidth, Math.floor(viewportWidth)) : minimumWidth;
		const extraWidth = stretchCount ? (tableWidth - minimumWidth) / stretchCount : 0;
		// Table-column percentage calculations are not interoperable; allocate spare pixels explicitly.
		for (let index = 0; index < columns.length; index++)
			setStyle(columns[index], "width", (widths[index] + (stretches[index] ? extraWidth : 0)) + "px");
		setStyle(table, "minWidth", minimumWidth + "px");
		setStyle(table, "width", tableWidth + "px");
	};
	const measure = () => {
		frame = 0;
		if (disposed || !element.isConnected)
			return;
		if (layoutPending)
			layoutColumns();
		if (!autoPageSize || element.getAttribute("aria-busy") === "true" || element.querySelector(".sphere10-grid-edit-bar"))
			return;

		const row = table?.tBodies[0]?.rows[0];
		if (!row || row.querySelector(".sphere10-grid-empty"))
			return;
		// Retain the tallest measured row across page-size changes to avoid oscillating between capacities.
		maximumRowHeight = Math.max(maximumRowHeight, ...Array.from(table.tBodies[0].rows, item => item.getBoundingClientRect().height));
		const rowHeight = maximumRowHeight;
		const available = viewport.clientHeight - table.tHead.getBoundingClientRect().height;
		if (rowHeight <= 0 || available <= 0)
			return;
		const pageSize = Math.min(9999, Math.max(1, Math.floor(available / rowHeight)));
		if (pageSize !== lastPageSize) {
			lastPageSize = pageSize;
			receiver.invokeMethodAsync("SetViewportPageSizeAsync", pageSize).catch(() => {});
		}
	};
	const schedule = () => {
		if (!frame && !disposed)
			frame = requestAnimationFrame(measure);
	};
	const stopDrag = event => {
		if (event && drag && event.pointerId !== drag.pointerId)
			return;
		if (drag?.grip.hasPointerCapture(drag.pointerId))
			drag.grip.releasePointerCapture(drag.pointerId);
		drag = null;
	};
	const pointerDown = event => {
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
	const pointerMove = event => {
		if (!drag || event.pointerId !== drag.pointerId)
			return;
		const width = Math.max(40, drag.widths[drag.index] + (event.clientX - drag.startX) / drag.scale);
		// Preserve user-resized widths independently from source data and neighbouring grids.
		for (let index = 0; index < drag.columns.length; index++) {
			const column = drag.columns[index];
			resizedWidths.set(column, {
				width: index === drag.index ? width : drag.widths[index],
				configuration: column.dataset.gridWidth + ":" + column.dataset.gridExpands
			});
		}
		layoutPending = true;
		maximumRowHeight = 0;
		schedule();
	};
	const dispose = () => {
		if (disposed)
			return;
		disposed = true;
		stopDrag();
		cancelAnimationFrame(frame);
		resizeObserver.disconnect();
		removalObserver.disconnect();
		element.removeEventListener("pointerdown", pointerDown);
		element.removeEventListener("pointermove", pointerMove);
		element.removeEventListener("pointerup", stopDrag);
		element.removeEventListener("pointercancel", stopDrag);
	};
	const resizeObserver = new ResizeObserver(entries => {
		let measurementPending = false;
		for (const entry of entries) {
			if (entry.target === viewport && viewportWidth !== entry.contentRect.width) {
				viewportWidth = entry.contentRect.width;
				maximumRowHeight = 0;
				layoutPending = true;
			}
			// Table height changes can affect page capacity; they must not feed column widths back into layout.
			measurementPending ||= layoutPending || autoPageSize;
		}
		if (measurementPending)
			schedule();
	});
	resizeObserver.observe(viewport);
	resizeObserver.observe(table);
	const removalObserver = new MutationObserver(() => {
		if (!element.isConnected)
			dispose();
	});
	removalObserver.observe(document.body, { childList: true, subtree: true });
	element.addEventListener("pointerdown", pointerDown);
	element.addEventListener("pointermove", pointerMove);
	element.addEventListener("pointerup", stopDrag);
	element.addEventListener("pointercancel", stopDrag);

	return {
		update(enabled) {
			if (enabled && !autoPageSize)
				lastPageSize = 0;
			autoPageSize = enabled;
			// Rendering may replace column definitions or restore their declarative inline widths.
			layoutPending = true;
			schedule();
		}
	};
}

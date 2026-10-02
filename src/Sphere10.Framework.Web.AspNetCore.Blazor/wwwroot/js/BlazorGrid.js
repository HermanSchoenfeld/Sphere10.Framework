// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Distributed under the MIT software license. See LICENSE.

// Compatibility resizing for plain tables; the CRUD grid uses its isolated component module.
window.ResizableColumnTable = function (id) {
	const table = document.getElementById(id);
	if (!table || !table.tHead || !table.tHead.rows.length)
		return;
	for (const cell of table.tHead.rows[0].cells) {
		if (cell.querySelector(".sphere10-column-resizer"))
			continue;
		cell.style.position = "relative";
		const grip = document.createElement("span");
		grip.className = "sphere10-column-resizer";
		Object.assign(grip.style, { position: "absolute", right: "0", top: "0", bottom: "0", width: "6px", cursor: "col-resize", touchAction: "none" });
		cell.appendChild(grip);
		grip.addEventListener("pointerdown", event => {
			event.preventDefault();
			const startX = event.clientX;
			const startWidth = cell.getBoundingClientRect().width;
			grip.setPointerCapture(event.pointerId);
			const OnPointerMove = moveEvent => {
				const width = Math.max(20, startWidth + moveEvent.clientX - startX);
				for (const row of table.rows) {
					if (row.cells[cell.cellIndex])
						row.cells[cell.cellIndex].style.width = width + "px";
				}
			};
			const FinishResize = () => {
				grip.removeEventListener("pointermove", OnPointerMove);
				grip.removeEventListener("pointerup", FinishResize);
				grip.removeEventListener("pointercancel", FinishResize);
			};
			grip.addEventListener("pointermove", OnPointerMove);
			grip.addEventListener("pointerup", FinishResize);
			grip.addEventListener("pointercancel", FinishResize);
		});
	}
};

window.ConsoleWrite = data => console.log(data);
window.AlertWrite = data => alert(data);


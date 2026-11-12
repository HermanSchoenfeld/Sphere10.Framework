//////////////////////////
// Initialization
/////////////////////////

// Initialize native tabs
InitializeTabs();

// Initialize code block copy controls
InitializeCodeBlocks();

// Apply feather icons
feather.replace();

// Apply some cosmetic fixes
$(document).ready(function () {
    // remove empty block children
    $(".ln-block-children:not(:has(*))").remove();
    $(".ln-callout-children:not(:has(*))").remove();
});

// Render database frames (ensure async loaded script is loaded first)
$(document).ready(function () {
    // Initialize the datatables (after lazy loaded datatables script loads)
    var maxRetries = 50; // Maximum number of polling attempts
    var interval = 100; // Polling interval in milliseconds

    function SetDataTables(retries) {
        if (typeof $.fn.DataTable !== 'undefined') {
            // DataTables is loaded, initialize it
            $('.ln-database').DataTable();
        } else if (retries > 0) {
            // DataTables is not loaded, check again after a delay
            setTimeout(function () {
                SetDataTables(retries - 1);
            }, interval);
        } else {
            // Max retries reached, log an error or handle as needed
            console.warn('DataTables script did not load in time.');
        }
    }
    // Start polling with the maximum number of retries
    SetDataTables(maxRetries);
});


// TOC initialization
$(document).ready(function () {
    let heading = $("main .ln-page-content").find(":header").not("[data-toc-skip]");
    for (let i = 0; i < heading.length; i++) {
        const head = heading[i];
        const jhead = $(head);
        var id = jhead.attr('id')
        if (id === "") {
            const URL = head.innerText.replace(/ /g, '_');
            jhead.attr('id', URL);
        }
        let header_level = parseInt(head.tagName.slice(-1));
        let spacing = "";
        spacing = `style="padding-left: ` + (header_level - 1) * 2 + `rem;"`
        const this_head = `<li class="py-1 ln-behaviour-hover-highlight" style="transform: rotate(0);"><a ${spacing} class="text-secondary stretched-link" href="#${id}">${head.innerText}</a></li>`
        $('.ln-toc').append(this_head)
    }
});

////////////////////////////////////
// Global Functions
////////////////////////////////////

////////////////////////////////////
// Code block clipboard
////////////////////////////////////

function InitializeCodeBlocks() {
    function initialize() {
        document.querySelectorAll('[data-ln-code-block]').forEach(block => {
            const button = block.querySelector('[data-ln-code-copy]');
            const code = block.querySelector('pre > code');
            const status = block.querySelector('.ln-code-status');
            if (!button || !code || !status || button.dataset.lnCopyReady) return;
            button.dataset.lnCopyReady = 'true';
            button.hidden = false;
            const label = button.querySelector('span');
            let resetTimer;
            button.addEventListener('click', async () => {
                clearTimeout(resetTimer);
                button.disabled = true;
                status.textContent = '';
                try {
                    // textContent preserves whitespace and excludes the header and Prism markup.
                    if (navigator.clipboard && window.isSecureContext) {
                        await navigator.clipboard.writeText(code.textContent);
                    } else {
                        // Support exported files and HTTP hosts without the Clipboard API.
                        const selection = window.getSelection();
                        const previousRanges = [];
                        for (let index = 0; index < selection.rangeCount; index++)
                            previousRanges.push(selection.getRangeAt(index).cloneRange());
                        const range = document.createRange();
                        range.selectNodeContents(code);
                        try {
                            selection.removeAllRanges();
                            selection.addRange(range);
                            if (!document.execCommand('copy')) throw new Error('Clipboard unavailable');
                        } finally {
                            selection.removeAllRanges();
                            previousRanges.forEach(previous => selection.addRange(previous));
                        }
                    }
                    label.textContent = 'Copied';
                    status.textContent = 'Code copied to clipboard.';
                } catch {
                    label.textContent = 'Copy failed';
                    status.textContent = 'Copy failed. Select the code and copy it manually.';
                } finally {
                    button.disabled = false;
                    resetTimer = setTimeout(() => { label.textContent = 'Copy'; }, 2000);
                }
            });
        });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initialize, { once: true });
    else initialize();
}

// Used to search articles on website, just redirects to google
function DoSearch(event) {
    if (event.type === "keypress" && event.key !== "Enter") {
        return; // Do nothing if the key is not Enter
    }

    event.preventDefault(); // Prevent form submission or default button behavior

    const parentElement = event.target.closest('.input-group'); // Get parent .input-group
    const inputElement = parentElement.querySelector('input[type="search"]'); // Find the input inside the parent
    const query = inputElement.value; // Get the value of the input

    if (query.trim() !== "") {
        const domainAndPath = window.location.host + window.location.pathname;
        const googleSearchUrl = `https://www.google.com/search?q=${encodeURIComponent(query)} site:${domainAndPath}`;
        window.location.href = googleSearchUrl; // Redirect to Google search
    }
}

////////////////////////////////////
// Native Notion tabs
////////////////////////////////////

/* Sphere10.VisualRenderer native tabs. MIT license. */
function InitializeTabs() {
    "use strict";
    if (window.localNotionTabs) return;
    const controllers = new WeakMap();
    let nextId = 0;
    function uniqueId() {
        let id;
        do { id = "ln-tab-button-" + (++nextId); } while (document.getElementById(id));
        return id;
    }
    function initialize(group) {
        if (controllers.has(group)) return;
        const panels = Array.from(group.children).filter(child => child.hasAttribute("data-ln-tab"));
        if (!panels.length) return;
        const labels = panels.map(panel => panel.querySelector(":scope > [data-ln-tab-label]"));
        if (labels.some(label => !label)) return;
        const list = document.createElement("div");
        list.className = "ln-tab-list";
        list.setAttribute("role", "tablist");
        list.setAttribute("aria-label", group.getAttribute("aria-label") || "Content tabs");
        const buttons = panels.map((panel, index) => {
            const button = document.createElement("button");
            button.type = "button";
            button.id = uniqueId();
            button.setAttribute("role", "tab");
            button.setAttribute("aria-controls", panel.id);
            const label = labels[index].cloneNode(true);
            // Labels retain rich formatting, but a tab button cannot contain another control.
            label.querySelectorAll("a, button, input, select, textarea, [tabindex]").forEach(element => {
                element.replaceWith(...element.childNodes);
            });
            label.querySelectorAll("[id]").forEach(element => element.removeAttribute("id"));
            button.append(...label.childNodes);
            if (!button.textContent.trim() && !button.querySelector("img[alt]"))
                button.setAttribute("aria-label", "Tab " + (index + 1));
            panel.setAttribute("role", "tabpanel");
            panel.setAttribute("aria-labelledby", button.id);
            panel.tabIndex = 0;
            button.addEventListener("click", () => activate(index));
            button.addEventListener("keydown", event => {
                let target;
                const rtl = getComputedStyle(list).direction === "rtl";
                if (event.key === "ArrowRight") target = index + (rtl ? -1 : 1);
                else if (event.key === "ArrowLeft") target = index + (rtl ? 1 : -1);
                else if (event.key === "Home") target = 0;
                else if (event.key === "End") target = panels.length - 1;
                else return;
                event.preventDefault();
                target = (target + panels.length) % panels.length;
                activate(target);
                buttons[target].focus();
            });
            list.append(button);
            return button;
        });
        function activate(index) {
            panels.forEach((panel, item) => {
                panel.hidden = item !== index;
                buttons[item].setAttribute("aria-selected", String(item === index));
                buttons[item].tabIndex = item === index ? 0 : -1;
            });
            // Existing image/diagram/layout consumers can remeasure a newly visible panel.
            window.dispatchEvent(new Event("resize"));
        }
        controllers.set(group, { panels, activate });
        group.prepend(list);
        group.classList.add("ln-tabs-enhanced");
        labels.forEach(label => { label.hidden = true; });
        activate(0);
    }
    function revealHash() {
        let id;
        try { id = decodeURIComponent(location.hash.slice(1)); } catch { return; }
        const target = id && document.getElementById(id);
        if (!target) return;
        let panel = target.closest("[data-ln-tab]");
        let revealed = false;
        while (panel) {
            const group = panel.parentElement;
            const controller = controllers.get(group);
            if (controller) {
                controller.activate(controller.panels.indexOf(panel));
                revealed = true;
            }
            panel = group.closest("[data-ln-tab]");
        }
        if (revealed) requestAnimationFrame(() => target.scrollIntoView());
    }
    function initializeAll() {
        document.querySelectorAll("[data-ln-tabs]").forEach(initialize);
        revealHash();
    }
    window.localNotionTabs = { initialize: initializeAll };
    window.addEventListener("hashchange", revealHash);
    // Clicking the current fragment again does not raise hashchange.
    document.addEventListener("click", event => {
        const link = event.target.closest && event.target.closest("a[href]");
        if (!link) return;
        const url = new URL(link.href, location.href);
        if (url.origin === location.origin && url.pathname === location.pathname && url.search === location.search && url.hash === location.hash)
            requestAnimationFrame(revealHash);
    });
    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", initializeAll, { once: true });
    else initializeAll();
}

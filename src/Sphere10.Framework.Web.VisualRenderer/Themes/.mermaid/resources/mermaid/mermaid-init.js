// Copyright (c) Herman Schoenfeld. Distributed under the MIT license; see LICENSE.
(function () {
    "use strict";

    if (window.Sphere10Mermaid) {
        window.Sphere10Mermaid.render(document);
        return;
    }

    let nextDiagramId = 0;
    let initialized = false;

    function initialize() {
        if (initialized)
            return;
        if (!window.mermaid)
            throw new Error("The Mermaid runtime could not be loaded.");
        window.mermaid.initialize({
            startOnLoad: false,
            securityLevel: "strict",
            suppressErrorRendering: true,
            htmlLabels: false,
            flowchart: { htmlLabels: false },
            secure: [
                "secure", "securityLevel", "startOnLoad", "maxTextSize", "maxEdges",
                "suppressErrorRendering", "htmlLabels", "flowchart", "dompurifyConfig",
                "themeCSS", "themeVariables", "fontFamily", "altFontFamily"
            ]
        });
        initialized = true;
    }

    async function renderDiagram(figure) {
        if (figure.dataset.lnMermaidState !== "pending")
            return;
        figure.dataset.lnMermaidState = "rendering";
        const source = figure.querySelector(".ln-mermaid-source");
        const code = figure.querySelector(".ln-mermaid-code code, .ln-mermaid-source code").textContent;
        const display = figure.querySelector(".ln-mermaid-diagram");
        const errorMessage = figure.querySelector(".ln-mermaid-error");
        try {
            initialize();
            let diagramId;
            do {
                diagramId = "ln-mermaid-svg-" + (++nextDiagramId);
            } while (document.getElementById(diagramId));
            const result = await window.mermaid.render(diagramId, code);
            display.innerHTML = result.svg;
            if (source) source.open = false;
            errorMessage.hidden = true;
            figure.dataset.lnMermaidState = "rendered";
        } catch (error) {
            display.replaceChildren();
            if (source) source.open = true;
            errorMessage.textContent = source
                ? "This Mermaid diagram could not be rendered. Check the diagram source below."
                : "This Mermaid diagram could not be rendered.";
            errorMessage.hidden = false;
            figure.dataset.lnMermaidState = "error";
        }
    }

    function render(root) {
        root = root || document;
        const figures = Array.from(root.querySelectorAll("[data-ln-mermaid]"));
        if (root.matches && root.matches("[data-ln-mermaid]"))
            figures.unshift(root);
        return Promise.all(figures.map(renderDiagram));
    }

    window.Sphere10Mermaid = { render: render };
    if (document.readyState === "loading")
        document.addEventListener("DOMContentLoaded", function () { render(document); }, { once: true });
    else
        render(document);
})();

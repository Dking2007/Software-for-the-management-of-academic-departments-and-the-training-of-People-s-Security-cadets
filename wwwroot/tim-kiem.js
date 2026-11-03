document.addEventListener("input", event => {
    const input = event.target;
    if (!input.matches("input[data-tim-kiem]")) return;
    event.stopImmediatePropagation();
    clearTimeout(input.choTimKiem);
    input.choTimKiem = setTimeout(() => {
        if (input.isConnected) input.dispatchEvent(new Event("change", { bubbles: true }));
    }, 300);
}, true);

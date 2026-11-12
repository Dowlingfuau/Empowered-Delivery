export function show(dialog) {
    dialog.addEventListener("cancel", event => event.preventDefault());
    dialog.showModal();
}

export function allowPaint() {
    return new Promise(resolve => {
        let finished = false;
        const finish = () => {
            if (finished) return;
            finished = true;
            clearTimeout(timeout);
            resolve();
        };
        const timeout = setTimeout(finish, 100);
        requestAnimationFrame(() => requestAnimationFrame(finish));
    });
}

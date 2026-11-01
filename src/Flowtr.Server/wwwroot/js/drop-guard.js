// A file dropped beside a drop zone must not replace Flowtr: the browser would open the file, and an open dialog would be lost.
// Only a file input takes a drop.
for (const type of ['dragover', 'drop']) {
    window.addEventListener(type, event => {
        if (!(event.target instanceof HTMLInputElement && event.target.type === 'file')) {
            event.preventDefault();
            event.dataTransfer.dropEffect = 'none';
        }
    });
}

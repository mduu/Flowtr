// The account pages are static: a form posts and the server answers with a new page. After a pause, the database
// needs up to a minute to wake up (story #011). Meanwhile the button shows that Flowtr works.
// The passkey button has its own script: the browser first asks for the passkey.
document.addEventListener('submit', event => {
    const button = event.submitter;
    if (!button || button.name === '__passkeySubmit' || event.defaultPrevented) {
        return;
    }

    button.classList.add('submitting');
    button.setAttribute('aria-busy', 'true');
});

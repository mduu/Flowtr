// Starts Blazor with the culture of Flowtr (configuration "Culture", in <html lang>), not with the language of the browser:
// a decimal comma (for example de-DE) must not break the input of amounts.
Blazor.start({
    webAssembly: {
        applicationCulture: document.documentElement.lang || 'de-CH',
    },
});

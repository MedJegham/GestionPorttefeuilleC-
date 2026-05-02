window.themeInterop = {
    init: function () {
        var t = localStorage.getItem('gp-theme');
        if (!t) {
            t = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
        }
        document.documentElement.setAttribute('data-bs-theme', t);
        return t;
    },
    toggle: function () {
        var cur = document.documentElement.getAttribute('data-bs-theme') || 'light';
        var next = cur === 'dark' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-bs-theme', next);
        localStorage.setItem('gp-theme', next);
        return next;
    },
    current: function () {
        return document.documentElement.getAttribute('data-bs-theme') || 'light';
    }
};

// Small browser helpers used from Blazor via JS interop. Loaded in <head> so the theme is applied before first paint.
(function () {
    const THEME_KEY = 'dt.theme';
    const colors = { light: '#f7f6f3', dark: '#141413' };
    const media = window.matchMedia('(prefers-color-scheme: dark)');

    function stored() {
        try { return localStorage.getItem(THEME_KEY) || 'system'; } catch { return 'system'; }
    }

    function resolved(theme) {
        return theme === 'system' ? (media.matches ? 'dark' : 'light') : theme;
    }

    function apply() {
        const value = resolved(stored());
        document.documentElement.dataset.theme = value;
        const meta = document.querySelector('meta[name="theme-color"]');
        if (meta) meta.setAttribute('content', colors[value]);
    }

    apply();
    media.addEventListener('change', apply);

    window.dt = {
        getTheme: stored,
        resolvedTheme: () => resolved(stored()),
        setTheme: (theme) => {
            try {
                if (theme === 'system') localStorage.removeItem(THEME_KEY);
                else localStorage.setItem(THEME_KEY, theme);
            } catch { /* storage unavailable: theme applies until reload */ }
            apply();
        },
        // Local calendar date as yyyy-MM-dd (WASM's own clock may not know the browser time zone).
        today: () => {
            const d = new Date();
            const pad = (n) => String(n).padStart(2, '0');
            return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
        },
        download: (filename, content, mime) => {
            const url = URL.createObjectURL(new Blob([content], { type: mime }));
            const a = document.createElement('a');
            a.href = url;
            a.download = filename;
            document.body.appendChild(a);
            a.click();
            a.remove();
            setTimeout(() => URL.revokeObjectURL(url), 1000);
        },
        print: () => window.print(),
    };
})();

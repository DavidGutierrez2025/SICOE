// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// --- Dark Mode Logic ---
// --- Dark Mode Logic ---
const darkModeToggle = document.getElementById('darkModeToggleSidebar');
const themeToggleTop = document.getElementById('theme-toggle'); // Keep top toggle if it exists
const storedTheme = localStorage.getItem('theme');
const prefersDarkScheme = window.matchMedia('(prefers-color-scheme: dark)');

// Function to apply theme
function applyTheme(theme) {
    if (theme === 'dark') {
        document.documentElement.setAttribute('data-theme', 'dark');
        if (darkModeToggle) {
            darkModeToggle.innerHTML = '<i class="fas fa-sun"></i> Modo Claro';
        }
        if (themeToggleTop) themeToggleTop.innerHTML = '<i class="bi bi-sun-fill"></i>';
    } else {
        document.documentElement.removeAttribute('data-theme');
        if (darkModeToggle) {
            darkModeToggle.innerHTML = '<i class="fas fa-moon"></i> Modo Oscuro';
        }
        if (themeToggleTop) themeToggleTop.innerHTML = '<i class="bi bi-moon-fill"></i>';
    }
}

// Initial check
if (storedTheme) {
    applyTheme(storedTheme);
} else if (prefersDarkScheme.matches) {
    applyTheme('dark');
}

// Toggle Event Listener (Sidebar)
if (darkModeToggle) {
    darkModeToggle.addEventListener('click', (e) => {
        e.preventDefault(); // Prevent default anchor behavior
        const currentTheme = document.documentElement.getAttribute('data-theme');
        let newTheme = 'light';

        if (currentTheme !== 'dark') {
            newTheme = 'dark';
        }

        applyTheme(newTheme);
        localStorage.setItem('theme', newTheme);
    });
}

// Toggle Event Listener (Top Bar - Optional backup)
if (themeToggleTop) {
    themeToggleTop.addEventListener('click', (e) => {
        e.preventDefault();
        const currentTheme = document.documentElement.getAttribute('data-theme');
        let newTheme = 'light';
        if (currentTheme !== 'dark') newTheme = 'dark';
        applyTheme(newTheme);
        localStorage.setItem('theme', newTheme);
    });
}

// Display User RFC if logged in
const userRfc = sessionStorage.getItem('userRfc');
const userDisplayElement = document.getElementById('userDisplayRfc');

if (userRfc && userDisplayElement) {
    // Remove the icon insertion since _Layout already has an icon next to this span
    userDisplayElement.textContent = userRfc;
}

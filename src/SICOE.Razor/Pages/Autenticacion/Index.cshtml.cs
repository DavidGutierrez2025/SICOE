using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SICOE.Razor.Pages.Autenticacion;

/// <summary>
/// Página de autenticación con FIEL
/// </summary>
public class IndexModel : PageModel
{
    public void OnGet()
    {
        // Página de autenticación - no requiere lógica especial en el servidor
        // Toda la lógica de autenticación se maneja en el cliente (JavaScript)
    }
}


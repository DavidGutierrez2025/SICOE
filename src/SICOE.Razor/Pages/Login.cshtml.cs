using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SICOE.Razor.Pages;

/// <summary>
/// Página de Login con e.firma (FIEL)
/// Esta es la primera pantalla que ve el usuario
/// </summary>
public class LoginModel : PageModel
{
    public void OnGet()
    {
        // Página de login - no requiere lógica especial en el servidor
        // Toda la lógica de autenticación se maneja en el cliente (JavaScript)
    }
}


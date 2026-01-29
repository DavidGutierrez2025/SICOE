# Subir SICOE a GitHub

Objetivo: subir el código a **https://github.com/DavidGutierrez2025/SICOE** en la rama **development**.

---

## Por qué falla "Permission denied" en .git

Al ejecutar `git add`, `git config` o `git push` desde el **terminal integrado de Cursor** (o con el script desde ahí), Git puede devolver:

- `error: could not lock config file .git/config: Permission denied`
- `fatal: Unable to create '.git/index.lock': Permission denied`

**Causas típicas:**

1. **Cursor / Visual Studio** usan Git (status, diff, etc.) y pueden **bloquear** archivos en `.git` mientras la carpeta está abierta.
2. El **terminal integrado** de Cursor corre en un **sandbox**: no escribe en `.git` (config, index.lock) y no tiene acceso a la red para `git push`. Por eso los comandos fallan al ejecutarlos desde el IDE.

**Solución:** ejecutar Git (o el script de push) en **PowerShell externo**, con Cursor/VS **cerrados** o sin tener el proyecto abierto en el IDE.

---

## Opción 1: Script automático (recomendado)

1. **Cierra** Cursor y Visual Studio.
2. Abre **PowerShell** (Win + X → “Windows PowerShell” o “Terminal”).
3. Ve a la carpeta del proyecto y ejecuta el script **con tu token**:

```powershell
cd C:\Sistemas\GEDINET\SICOE\SICOE
.\push-sicoe-automatico.ps1 -Token "tu_personal_access_token"
```

4. Si aparece un aviso de política de ejecución:

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

Luego vuelve a ejecutar el script.

El script configura usuario, rama `development`, remoto y hace `add` → `commit` → `push`.

---

## Opción 2: Comandos manuales

En **PowerShell externo** (Cursor/VS cerrados):

```powershell
cd C:\Sistemas\GEDINET\SICOE\SICOE

git config user.email "david.gutierrez73@gmail.com"
git config user.name "DavidGutierrez2025"
git remote set-url origin https://github.com/DavidGutierrez2025/SICOE.git

git checkout -b development
git add .
git status
git commit -m "Actualización SICOE: integración SAT, descargas, UI."
git push -u origin development
```

Cuando pida credenciales:

- **Username:** `DavidGutierrez2025`
- **Password:** tu **Personal Access Token** (no la contraseña de GitHub).

---

## Qué se sube

- **Incluido:** `src/`, `scripts/`, `SICOE.sln`, `.gitignore`, `README_SICOE.md`, etc.
- **Excluido por .gitignore:** `context/`, `Docs/`, `gedinet/`, `logs/`, `keys/`, `bin/`, `obj/`, etc.

---

## Token de GitHub

1. https://github.com/settings/tokens  
2. “Generate new token (classic)”  
3. Marca el scope **repo**  
4. Copia el token y úsalo como contraseña al hacer `git push` o en el script con `-Token "..."`.

---

## Resumen

| Paso | Acción |
|------|--------|
| 1 | Cerrar Cursor / Visual Studio |
| 2 | Abrir PowerShell externo |
| 3 | `cd C:\Sistemas\GEDINET\SICOE\SICOE` |
| 4 | `.\push-sicoe-automatico.ps1 -Token "tu_token"` **o** los comandos manuales de arriba |
| 5 | Si pide credenciales: usuario `DavidGutierrez2025`, contraseña = token |

Repositorio: **https://github.com/DavidGutierrez2025/SICOE** (rama **development**).

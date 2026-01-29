# Comandos Rápidos para Configurar Git y Subir a Development

## 🚀 Configuración Rápida

Ejecuta estos comandos en PowerShell desde la raíz del proyecto:

### 1. Configurar Email y Nombre (si no están configurados globalmente)

```powershell
git config user.email "david.gutierrez73@gmail.com"
git config user.name "david.gutierrezc"
```

### 2. Verificar Configuración

```powershell
git config --get user.email
git config --get user.name
```

### 3. Crear/Cambiar a Rama Development

```powershell
# Si estás en main, renombrar a development
git branch -M development

# O si no hay commits aún, crear la rama
git checkout -b development
```

### 4. Agregar Archivos y Crear Commit

```powershell
git add .
git commit -m "Initial commit: SICOE - Sistema de Integración CFDI con SAT"
```

### 5. Configurar Remoto

```powershell
git remote add origin https://github.com/DavidGutierrez2025/SICOE.git
```

O si ya existe, actualizarlo:

```powershell
git remote set-url origin https://github.com/DavidGutierrez2025/SICOE.git
```

### 6. Verificar Remoto

```powershell
git remote -v
```

### 7. Subir a Development

```powershell
git push -u origin development
```

---

## 📋 Script automático (con token, sin preguntar)

Para configurar todo y hacer **push automático** usando tu token (sin teclearlo cada vez):

```powershell
.\push-sicoe-automatico.ps1 -Token "github_pat_xxxx"
```

Sustituye `github_pat_xxxx` por tu Personal Access Token. El script:

- Configura email y usuario Git
- Crea/cambia a la rama `development`
- Configura el remoto con el token
- Hace `git add`, `commit` y `push`

El token se guarda en `.git/config` (local, no se sube al repo). Los `git push` siguientes serán automáticos.

**Alternativa con variable de entorno:**

```powershell
$env:GITHUB_TOKEN = "github_pat_xxxx"
.\push-sicoe-automatico.ps1
```

---

## 📋 Script interactivo (sin token)

```powershell
.\configurar-git-development.ps1
```

Te pedirá confirmar y, al hacer push, Git pedirá usuario y token la primera vez.

---

## 🔐 Autenticación con GitHub

Cuando hagas `git push`, GitHub te pedirá credenciales:

1. **Username**: `DavidGutierrez2025` (o tu usuario de GitHub)
2. **Password**: Usa un **Personal Access Token** (no tu contraseña)

### Crear Personal Access Token:

1. Ve a: https://github.com/settings/tokens
2. Click en **"Generate new token (classic)"**
3. Configura:
   - **Note**: `SICOE - Desarrollo Local`
   - **Expiration**: Elige una duración
   - **Scopes**: Marca `repo` (acceso completo)
4. Click en **"Generate token"**
5. **Copia el token** (solo se muestra una vez)
6. Úsalo como contraseña cuando Git te la pida

---

## ✅ Verificación Final

Después del push, verifica en GitHub:

- Ve a: https://github.com/DavidGutierrez2025/SICOE
- Deberías ver la rama `development` con todos los archivos
- El commit inicial debería estar visible

---

## 🆘 Solución de Problemas

### Error: "Permission denied"
- Cierra Visual Studio, GitHub Desktop u otros programas que usen Git
- Intenta de nuevo

### Error: "Repository not found"
- Verifica que el repositorio exista en: https://github.com/DavidGutierrez2025/SICOE
- Verifica que tengas permisos de escritura

### Error: "Branch 'development' does not exist"
- Asegúrate de haber creado la rama: `git checkout -b development`
- O renombra main: `git branch -M development`

### Error: "Could not lock config file"
- Cierra programas que usen Git
- Intenta usar `git config --local` en lugar de `--global`

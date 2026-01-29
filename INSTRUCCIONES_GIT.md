# Instrucciones para Configurar Git y Subir SICOE a GitHub

## 📋 Resumen

Este documento te guía para:
1. Configurar Git con tu email `david.gutierrez73@gmail.com`
2. Crear el repositorio **SICOE** en GitHub
3. Subir el código a la rama **development**

---

## 🔧 Paso 1: Configurar Git Localmente

### Opción A: Usar el Script Automatizado (Recomendado)

Ejecuta el script desde PowerShell en la raíz del proyecto:

```powershell
.\configurar-git-development.ps1
```

El script hará automáticamente:
- ✅ Configurar email: `david.gutierrez73@gmail.com`
- ✅ Crear/cambiar a la rama `development`
- ✅ Agregar archivos al staging
- ✅ Crear commit inicial
- ✅ Configurar el remoto
- ✅ Hacer push a `development`

### Opción B: Comandos Manuales

Si prefieres hacerlo manualmente:

```powershell
# 1. Configurar email
git config user.email "david.gutierrez73@gmail.com"
git config user.name "david.gutierrezc"

# 2. Verificar configuración
git config --get user.email
git config --get user.name

# 3. Crear/cambiar a rama development
git checkout -b development
# O si ya estás en main:
git branch -M development

# 4. Agregar archivos
git add .

# 5. Crear commit inicial
git commit -m "Initial commit: SICOE - Sistema de Integración CFDI con SAT"

# 6. Configurar remoto (después de crear el repo en GitHub)
git remote add origin https://github.com/david.gutierrezc/SICOE.git

# 7. Subir a development
git push -u origin development
```

---

## 🌐 Paso 2: Crear el Repositorio en GitHub

### 2.1. Crear el Repositorio

1. Ve a [GitHub](https://github.com) e inicia sesión con tu cuenta
2. Haz clic en el botón **"+"** (arriba derecha) → **"New repository"**
3. Configura el repositorio:
   - **Repository name**: `SICOE`
   - **Description**: `Sistema de Integración CFDI con SAT`
   - **Visibility**: 
     - ✅ **Private** (recomendado para proyectos internos)
     - O **Public** (si es código abierto)
   - **NO marques** "Initialize this repository with a README"
   - **NO agregues** .gitignore ni licencia (ya los tenemos)
4. Haz clic en **"Create repository"**

### 2.2. Copiar la URL del Repositorio

GitHub te mostrará la URL del repositorio. Debería ser algo como:
```
https://github.com/david.gutierrezc/SICOE.git
```

**Copia esta URL** - la necesitarás en el siguiente paso.

---

## 🚀 Paso 3: Conectar y Subir el Código

### 3.1. Si usaste el script automatizado

El script te pedirá la URL del repositorio. Pega la URL que copiaste en el paso anterior.

### 3.2. Si hiciste la configuración manual

```powershell
# Configurar el remoto
git remote add origin https://github.com/david.gutierrezc/SICOE.git

# Verificar que se configuró correctamente
git remote -v

# Subir a la rama development
git push -u origin development
```

---

## 🔐 Paso 4: Autenticación con GitHub

GitHub requiere autenticación para hacer push. Tienes dos opciones:

### Opción A: Personal Access Token (PAT) - Recomendado

1. Ve a GitHub → **Settings** → **Developer settings** → **Personal access tokens** → **Tokens (classic)**
2. Haz clic en **"Generate new token (classic)"**
3. Configura el token:
   - **Note**: `SICOE - Desarrollo Local`
   - **Expiration**: Elige una duración (90 días, 1 año, etc.)
   - **Scopes**: Marca `repo` (acceso completo a repositorios)
4. Haz clic en **"Generate token"**
5. **Copia el token** (solo se muestra una vez)

Cuando hagas `git push`, Git te pedirá:
- **Username**: `david.gutierrezc` (o tu usuario de GitHub)
- **Password**: Pega el **Personal Access Token** (no tu contraseña)

### Opción B: SSH Keys

Si prefieres usar SSH:

```powershell
# Generar clave SSH (si no tienes una)
ssh-keygen -t ed25519 -C "david.gutierrez73@gmail.com"

# Copiar la clave pública
cat ~/.ssh/id_ed25519.pub

# Agregar la clave en GitHub:
# Settings → SSH and GPG keys → New SSH key
```

Luego usa la URL SSH:
```powershell
git remote set-url origin git@github.com:david.gutierrezc/SICOE.git
```

---

## ✅ Verificación Final

Después de hacer push, verifica que todo esté correcto:

```powershell
# Verificar rama actual
git branch

# Verificar remoto
git remote -v

# Verificar último commit
git log --oneline -1

# Verificar estado
git status
```

En GitHub, deberías ver:
- ✅ El código en la rama `development`
- ✅ Todos los archivos del proyecto (excepto los excluidos por .gitignore)
- ✅ El commit inicial con el mensaje "Initial commit: SICOE..."

---

## 📝 Estructura del Repositorio Remoto

El repositorio en GitHub tendrá esta estructura:

```
SICOE/
├── .gitignore
├── README_SICOE.md
├── DOCUMENTO_TECNICO_SICOE.md
├── SICOE.sln
├── scripts/
│   ├── CreateDatabase_SICOE.sql
│   ├── DropDatabase_SICOE.sql
│   └── ...
└── src/
    ├── SICOE.API/
    ├── SICOE.Application/
    ├── SICOE.Domain/
    ├── SICOE.Infrastructure/
    └── SICOE.Razor/
```

**NO se subirán:**
- ❌ `context/` y `Docs/` (excluidos por .gitignore)
- ❌ `logs/`, `keys/`, archivos de base de datos
- ❌ Archivos compilados (`bin/`, `obj/`)

---

## 🔄 Comandos Útiles para el Futuro

```powershell
# Ver estado
git status

# Agregar cambios
git add .

# Crear commit
git commit -m "Descripción de los cambios"

# Subir cambios a development
git push origin development

# Ver historial
git log --oneline --graph -10

# Cambiar de rama
git checkout development

# Ver diferencias
git diff
```

---

## 🆘 Solución de Problemas

### Error: "Permission denied"
- Verifica que tengas permisos de escritura en el repositorio
- Asegúrate de estar autenticado correctamente (PAT o SSH)

### Error: "Repository not found"
- Verifica que el repositorio `SICOE` exista en tu cuenta de GitHub
- Verifica que la URL del remoto sea correcta

### Error: "Branch 'development' does not exist"
- Asegúrate de haber creado la rama: `git checkout -b development`
- O renombra main: `git branch -M development`

### Error: "Could not lock config file"
- Cierra cualquier programa que esté usando Git (Visual Studio, GitHub Desktop, etc.)
- Intenta de nuevo

---

## 📞 Soporte

Si tienes problemas, verifica:
1. Que el repositorio `SICOE` exista en GitHub
2. Que tengas permisos de escritura
3. Que las credenciales estén configuradas correctamente
4. Que la rama `development` exista localmente

¡Listo para comenzar! 🚀

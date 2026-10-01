# 📄 ValidaciónPDF

Sistema de gestión y validación de archivos PDF con auditoría, control de usuarios y flujo de revisión documental.

---

## 📋 ¿Qué hace el sistema?

Permite a un equipo de trabajo revisar, renombrar, rotar y validar documentos PDF de manera controlada, registrando automáticamente cada acción en un archivo Excel de auditoría.

---

## ✅ Funcionalidades

### 🔐 Control de acceso
- **Pantalla de login** al iniciar la aplicación
- Usuarios con nombre completo, nombre de usuario y PIN de 4 dígitos
- Usuario por defecto: `admin` / PIN `0000`
- Cierre de sesión desde la pantalla principal (botón rojo, esquina derecha)
- Al cerrar sesión, regresa al login para que otro usuario pueda entrar

### 👥 Administración (solo SuperAdmin)
Accesible desde el menú **⚙ Opciones de Admin** en la pantalla de login, protegido con PIN maestro:

| Opción | Qué hace |
|---|---|
| Administrar Usuarios | Agregar y eliminar usuarios del sistema |
| Catálogo de Años | Configurar los años válidos para la validación de nombres |
| Ver Auditoría (Excel) | Abre directamente el archivo `auditoria.xlsx` |

### 📁 Selección de carpeta
- Botón **Seleccionar carpeta** para elegir la carpeta con los PDFs a revisar
- Al seleccionar, se crea automáticamente la carpeta `{nombre_carpeta}_revisados` donde se enviarán los documentos validados
- El sistema carga y muestra los PDFs en orden

### ✏️ Renombrado con doble captura
El renombrado es seguro: requiere escribir el nombre **dos veces** para confirmar.

1. Seleccionar **Regla 1** o **Regla 2**
2. Escribir el nombre en el campo de texto (se valida el formato en tiempo real)
3. Clic en **Renombrar** → el campo se limpia y pide confirmación
4. Escribir el mismo nombre de nuevo
5. Clic en **Renombrar** → si coinciden, se renombra el archivo

Si los nombres no coinciden, se reinicia el proceso.

### 📐 Reglas de validación de nombres

| Regla | Formato | Ejemplo |
|---|---|---|
| Regla 1 | `DD_AAAA_NNNNNN` | `01_2024_123456` |
| Regla 2 | `00_00_00_TEXTO_00_TEXTO_AAAA_0000_00` | Formato extendido con texto |

- El **año** es validado contra el catálogo de años configurado
- Si el año no está en el catálogo, no permite renombrar

### ➡️ Siguiente / Flujo de validación
Al hacer clic en **Siguiente** después de haber renombrado un archivo:
- El sistema pregunta: *"¿El documento ha sido validado correctamente?"*
- **Sí** → el archivo se mueve a la carpeta `_revisados` y se registra en auditoría
- **No** → solo avanza al siguiente PDF, el archivo permanece en su lugar

Si **no** se renombró el archivo, avanza directamente sin preguntar.

### 🔄 Rotación de páginas
- Campo numérico para seleccionar el número de página
- Botón **↺** → rota 90° a la izquierda
- Botón **↻** → rota 90° a la derecha
- La rotación se guarda permanentemente en el PDF
- **No se registra en auditoría cada vez que se rota** — solo se anota cuando el documento es validado y enviado a revisados (con la nota "incluye rotación de páginas")

### 📋 Otras acciones
- **Duplicar**: copia el PDF con un nuevo nombre (captura simple, sin doble confirmación)
- **Eliminar**: elimina el PDF actual con confirmación previa

---

## 📊 Auditoría en Excel

El sistema genera automáticamente el archivo:
```
data/auditoria.xlsx
```

### Organización
- **Una hoja por año** (ej: `2025`, `2026`, `2027`...)
- Todos los usuarios y todas las sesiones del año se acumulan en la misma hoja

### Columnas registradas

| Columna | Descripción |
|---|---|
| Fecha y Hora | Momento exacto de la acción |
| Usuario | Nombre de login |
| Nombre Completo | Nombre real del trabajador |
| Acción | Renombrar, Duplicar, Eliminar, Validado |
| Archivo Original | Nombre del archivo antes de la acción |
| Archivo Nuevo | Nombre asignado (si aplica) |
| Detalles | Información adicional (ej: "incluye rotación de páginas") |

---

## 🗂️ Estructura de archivos

```
validacionPdf/
├── validacionPdf.sln          ← Solución de Visual Studio
└── validacionPdf/
    ├── Form1.vb               ← Formulario principal
    ├── Form1.Designer.vb
    ├── FormLogin.vb           ← Pantalla de login
    ├── FormLogin.Designer.vb
    ├── FormUsuarios.vb        ← Gestión de usuarios
    ├── FormUsuarios.Designer.vb
    ├── FormCatalogoAnios.vb   ← Catálogo de años válidos
    ├── FormCatalogoAnios.Designer.vb
    ├── ModuloUsuarios.vb      ← Lógica de usuarios y sesiones
    ├── ModuloCatalogo.vb      ← Lógica del catálogo de años
    ├── ModuloAuditoria.vb     ← Lógica de auditoría Excel
    ├── My Project/
    │   └── ApplicationEvents.vb  ← Muestra el login al iniciar
    └── bin/Debug/
        └── data/              ← Generada automáticamente al ejecutar
            ├── usuarios.json
            ├── catalogo_anios.json
            └── auditoria.xlsx
```

---

## ⚙️ Dependencias (NuGet)

| Paquete | Versión | Uso |
|---|---|---|
| `iText` | 9.5.0 | Rotación de páginas PDF |
| `ClosedXML` | 0.102.3 | Generación del Excel de auditoría |
| `Newtonsoft.Json` | — | Persistencia de configuración local |
| `Microsoft.Web.WebView2` | — | Visualizador de PDFs embebido |

---

## 🚀 Instalación y compilación

### Requisitos
- Windows 10/11
- Visual Studio 2022
- .NET Framework 4.7.2

### Pasos

1. Abrir `validacionPdf.sln` con Visual Studio 2022
2. Instalar ClosedXML desde la **Consola del Administrador de Paquetes**:
   ```
   Install-Package ClosedXML
   ```
3. Compilar con **Ctrl + Shift + B**
4. Ejecutar con **F5**

---

## 👤 Credenciales por defecto

| Campo | Valor |
|---|---|
| Usuario | `admin` |
| PIN | `0000` |
| PIN Maestro (SuperAdmin) | `0000` |

> ⚠️ Se recomienda cambiar el PIN maestro después de la primera instalación. Esto se hace editando directamente el archivo `data/usuarios.json`.

---

## 📌 Notas importantes

- Toda la información es **100% local**, no requiere conexión a internet ni servidor
- Los archivos de configuración (`usuarios.json`, `catalogo_anios.json`) se crean automáticamente en la primera ejecución
- El archivo `auditoria.xlsx` se abre mientras el programa está corriendo, pero se recomienda abrirlo en modo **Solo lectura** para evitar conflictos
- Si se borra `auditoria.xlsx`, el sistema lo vuelve a crear vacío automáticamente

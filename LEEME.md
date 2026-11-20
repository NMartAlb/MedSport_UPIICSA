# Plataforma de Servicios Estudiantiles: código completo

Todo el código de los módulos: inicio de sesión por código de correo, actividades, inscripciones, documentos,
pase de lista, servicio médico (nuevo ingreso, valoración, expediente, primeros auxilios, urgencias, campañas),
reportes, administración y bitácora. Los datos viven **en memoria** (se borran al reiniciar la API) hasta que
tu compañero conecte PostgreSQL (ver `docs/CONTRATO-BASE-DE-DATOS.md`).

> Este código se escribió sin poder compilarlo. Lo primero es hacer `dotnet build` y llevar los errores que salgan.

## 1. Poner el código en tu repositorio

1. Cierra Visual Studio.
2. En tu repo, **borra los archivos .cs** de `src/Dominio`, `src/Aplicacion`, `src/Infraestructura` y `src/Api`
   (no borres los `.csproj`, ni `Properties`, ni `bin`/`obj`). Así no quedan archivos duplicados del primer módulo.
3. Copia de este zip, respetando las carpetas: `src/`, `tests/Pruebas/`, `docs/`, `docker-compose.yml`,
   `nginx.conf`, `.env.example` y `LEEME.md`.
   - `src/Api/appsettings.json` y `Program.cs` reemplazan a los que tienes.
   - En `tests/Pruebas` borra antes el `UnitTest1.cs` que crea la plantilla.
4. Agrega a tu `.gitignore` las líneas de `.gitignore.agregar`.

## 2. Paquetes y referencias (una sola vez, en la raíz del repo)

```
dotnet add src/Api package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/Infraestructura package System.IdentityModel.Tokens.Jwt
dotnet add tests/Pruebas reference src/Dominio src/Aplicacion src/Infraestructura
dotnet build
```

## 3. Ejecutarlo

1. Buzón de correo falso, para ver los códigos de verificación:
   `docker run -d --name mailpit -p 8025:8025 -p 1025:1025 axllent/mailpit`  → bandeja en http://localhost:8025
2. Ejecuta la API (F5 en Visual Studio). Anota el puerto https; si no es **7016**, cámbialo en `src/Web/js/config.js`.
3. Abre la carpeta `src/Web` en VS Code, clic derecho en `index.html` → **Open with Live Server** (puerto 5500).

## 4. Cuentas de prueba

Nadie tiene contraseña al inicio. Para cada cuenta: «Crear o restablecer mi contraseña» → correo → código (está en Mailpit) → contraseña.

| Correo | Rol |
|---|---|
| admin@ipn.mx | Administrador |
| coach@ipn.mx | Coach (ya tiene Básquetbol, Taekwondo y Teatro) |
| medico@ipn.mx | Personal médico |
| directivo@ipn.mx | Directivo |
| cualquier otro @ipn.mx (por ejemplo tunombre@alumno.ipn.mx) | Alumno (se registra solo) |

Recorrido sugerido: alumno se inscribe → sube documentos → coach los aprueba o rechaza → el alumno ve la notificación;
el coach pasa lista; el alumno inicia su nuevo ingreso médico → el médico revisa, carga valoración y expediente → el directivo ve reportes.

## 5. Pruebas automáticas

`dotnet test` (valida el correo @ipn.mx, el estado de los trámites, el hash de contraseñas, el cupo y los rangos de reportes).

## 6. Todo junto con Docker (web en NGINX + API + buzón)

1. Copia `.env.example` a `.env` y llena los dos secretos.
2. `docker compose up --build` → plataforma en http://localhost:8080 y buzón en http://localhost:8025

## 7. Seguridad implementada

- **Red:** NGINX como única puerta; API sin puerto público; CORS limitado; límite de intentos en `/api/auth`; encabezados de seguridad y CSP.
- **Aplicación:** validación de entradas; sin SQL en el código; errores sin detalles internos; archivos solo PDF/JPG/PNG (extensión y contenido) de máximo 5 MB, cifrados (AES-GCM) y con nombre aleatorio.
- **Usuario:** código por correo (6 dígitos, 10 min, 5 intentos); contraseña con PBKDF2; bloqueo tras 5 fallos; token con expiración.
- **Acceso:** roles en cada endpoint, además de verificar que cada quien solo toque lo suyo; bitácora de accesos al expediente.

## 8. Pendientes y decisiones por tomar

- Conectar PostgreSQL (tu compañero) y cambiar los `...EnMemoria` en `Configuracion/RegistroDeServicios.cs`.
- El inicio de sesión es verificación propia por código. El Art. 9 pide autenticación institucional (SSO de Microsoft): confirmar con la UDI si se acepta.
- Lista definitiva de documentos por trámite (hoy son 4 de prueba, en `TipoDocumentoRepositorioEnMemoria`).
- SMTP institucional real, certificado HTTPS (UDI), pruebas de vulnerabilidad firmadas, manuales y aviso de privacidad.
- Probar que los PDF se abren en una pestaña nueva con la política de NGINX; si el navegador los bloquea, hay que ajustar `object-src` en `nginx.conf`.

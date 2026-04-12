# DuoCareAPI

DuoCareAPI es una API REST desarrollada en **.NET 8** diseñada para gestionar usuarios, citas y registros médicos dentro de un entorno seguro. Incluye autenticación basada en JWT, control de roles, auditoría básica, envío de correos electrónicos, versionado de API, rate limiting y bloqueo de cuentas.

---

## **Características principales**
- **Autenticación segura**: Basada en JWT con roles (`Administrator`, `User`).
- **Gestión de citas**: Crear, aceptar, rechazar y completar citas entre usuarios.
- **Gestión de registros médicos**: Crear y consultar registros médicos.
- **Gestión de usuarios**: Buscar usuarios por email de forma segura.
- **Auditoría básica**: Seguimiento de creación de citas (`CreatedAt`, `CreatedBy`).
- **Seguridad avanzada**: CORS, Rate Limiting, bloqueo de cuentas tras intentos fallidos.
- **Logging profesional**: Registro de eventos, errores y auditoría en archivo.
- **Versionado de API**: Permite mantener varias versiones sin romper compatibilidad.

---

## **Tecnologías utilizadas**

### **.NET MAUI**
Framework multiplataforma para la aplicación móvil, permitiendo desarrollar para Android e iOS desde una única base de código con acceso a geolocalización, sensores y almacenamiento seguro.

### **C# y .NET 8**
Lenguaje principal del backend. Utilizado para controladores, servicios, autenticación JWT, Identity, validaciones, BackgroundService y lógica de negocio.

### **Visual Studio 2022**
IDE profesional utilizado para desarrollar, depurar y mantener el proyecto.

### **Git y GitHub**
Control de versiones, ramas, commits y despliegue continuo. Repositorio oficial del proyecto.

### **GitHub Copilot**
Asistente de IA utilizado **como apoyo**, no como autor del código.  
Se ha empleado para:
- detectar errores
- auditar fragmentos complejos
- sugerir refactorizaciones
- acelerar tareas repetitivas

El desarrollo principal ha sido realizado manualmente.

### **Azure (App Service + Azure SQL)**
Plataforma cloud para desplegar la API y la base de datos con alta disponibilidad, escalabilidad y seguridad.

### **SQL Server / Azure SQL**
Base de datos relacional con integridad ACID, seguridad avanzada y compatibilidad con Entity Framework Core.

### **ASP.NET Core Identity**
Gestión de usuarios, roles, contraseñas, bloqueo por intentos fallidos y confirmación de email.

### **JWT (JSON Web Tokens)**
Autenticación sin estado para la API.

### **CORS**
Control de orígenes permitidos para proteger la API.

### **ILogger + Logging en archivo**
Sistema de registro profesional que almacena logs en:


Incluye logs de información, advertencias y errores críticos.

### **BackgroundService**
Servicio en segundo plano para cancelar citas automáticamente.

### **Rate Limiting**
Limitación de solicitudes por usuario/IP.  
Ejemplo: máximo 5 intentos de login por minuto.

### **API Versioning**
Permite mantener varias versiones de la API sin romper compatibilidad.

---

## **Requisitos**
- **SDK de .NET 8**
- **SQL Server / Azure SQL**
- **SendGrid API Key**
- Visual Studio 2022
- NuGet Package Manager

---

## **Configuración**

1. Clonar el Repositorio

git clone https://github.com/jlolumi-cell/DuoCareAPI.git
cd DuoCareAPI


2. Configurar appsettings.json

json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=TU_SERVIDOR;Database=DuoCareDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "TU_CLAVE_SECRETA",
    "Issuer": "DuoCareApp",
    "Audience": "DuoCareApp",
    "DurationInMinutes": 60
  },
  "Cors": {
    "AllowedOrigins": ["https://example.com", "https://otro-origen.com"]
  },
  "Admin": {
    "Email": "admin@duocare.com",
    "Password": "CONTRASEÑA_SEGURA"
  },
  "SendGrid": {
    "ApiKey": "TU_API_KEY",
    "FromEmail": "tucorreo@dominio.com",
    "FromName": "DuoCare"
  }
}


3. Restaurar paquetes

dotnet restore


4. Aplicar migraciones

dotnet ef database update


5. Ejecutar la aplicación

dotnet run


---

## **Créditos**

Este proyecto ha sido desarrollado por:
  
- Myriam Rodríguez
- Lucía Jiménez
- Jose Javier García

Con el apoyo y la guía del profesorado de 1º y 2º de Desarrollo de Aplicaciones Multiplataforma del  
"IES “Valle Inclán”

A todos ellos, gracias por su dedicación, esfuerzo y acompañamiento durante el proceso formativo.

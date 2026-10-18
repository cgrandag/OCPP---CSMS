# OCPP - Plataforma de Gestión para Cargadores EV (OCPP 1.6-J)

Plataforma integral de gestión, monitoreo y control de estaciones de recarga de vehículos eléctricos basada en el protocolo estándar **OCPP 1.6-J** (Open Charge Point Protocol sobre WebSockets). Desarrollada con **.NET 8** bajo los principios de Arquitectura Limpia (*Clean Architecture*).

---

## 🚀 Características Principales

- **Servidor WebSocket OCPP 1.6-J**: Comunicación bidireccional en tiempo real con estaciones de carga (BootNotification, Heartbeat, StatusNotification, StartTransaction, StopTransaction, MeterValues).
- **Control Remoto de Estaciones**: Envío de comandos directos como `RemoteStartTransaction`, `RemoteStopTransaction`, `Reset` (Soft/Hard) y `UnlockConnector`.
- **Smart Charging**: Algoritmo de desconexión inteligente y protección contra sobrecarga de baterías según umbrales de estado de carga (SoC).
- **Asistente Técnico**: Asistente integrado con la API de Google Gemini para diagnóstico en lenguaje natural y asistencia técnica sobre el estado de la red de cargadores.
- **Dashboard Web en Tiempo Real**: 
  - Métricas de consumo energético (kWh) y cobro financiero.
  - Geolocalización de cargadores en mapa interactivo.
  - Transmisión en vivo de eventos y estado mediante Server-Sent Events (SSE).
  - Consola de logs OCPP en tiempo real vía WebSockets por cargador.
- **Gestión de Tarifas y Facturación**: Configuración de esquemas de cobro basados en energía (kWh), tiempo (minutos) o cargos fijos de conexión.
- **Administración de Usuarios y Roles**: Control de acceso granular para Super Administradores, Operadores (CPO), Soporte y Clientes finales con autorización por tags RFID/NFC.

---

## 🛠️ Stack Tecnológico

| Capa | Tecnologías |
|---|---|
| **Backend** | .NET 8, C#, ASP.NET Core Minimal APIs, WebSockets, Serilog |
| **Persistencia** | PostgreSQL con TimescaleDB (para métricas de series temporales), Entity Framework Core 8 |
| **Caché & Mensajería** | Redis (estado en memoria), RabbitMQ (eventos asíncronos de recarga) |
| **IA & LLM** | Google Gemini API (`gemini-2.5-flash`) |
| **Frontend** | HTML5, CSS3 Glassmorphism, JavaScript Moderno (Vanilla ES6+), Chart.js, Leaflet.js |
| **Testing** | xUnit, Moq |

---

## 📋 Requisitos Previos

Asegúrate de tener instalados los siguientes componentes en tu máquina:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (con Docker Compose activo)
- Herramienta de Entity Framework Core para .NET CLI:
  ```powershell
  dotnet tool install --global dotnet-ef
  ```

---

## ⚙️ Configuración e Instalación

### 1. Clonar el Repositorio
```bash
git clone <URL_DEL_REPOSITORIO>
cd OcppGateway
```

### 2. Configurar la API Key de Gemini
Por motivos de seguridad, la clave de acceso a la API de Gemini fue excluida del repositorio. Debes configurarla antes de iniciar la aplicación.

Abre el archivo de configuración `OcppGateway/appsettings.json` y reemplaza el valor del campo `ApiKey`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "PostgresDb": "Host=localhost;Database=ocppdb;Username=postgres;Password=mysecretpassword",
    "Redis": "localhost:6379,abortConnect=false"
  },
  "RabbitMq": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  },
  "Gemini": {
    "ApiKey": "TU_API_KEY_DE_GEMINI_AQUI"
  }
}
```

> **Nota:** Puedes obtener una API Key gratuita desde [Google AI Studio](https://aistudio.google.com/).

---

### 3. Levantar Infraestructura con Docker

Inicia los contenedores de TimescaleDB/PostgreSQL, Redis y RabbitMQ ejecutando desde la carpeta `OcppGateway`:

```powershell
cd OcppGateway
docker-compose up -d
```

Verifica que los tres servicios estén en estado *Up*:
```powershell
docker ps
```

---

### 4. Aplicar Migraciones a la Base de Datos

Aplica el esquema de datos y las migraciones existentes a PostgreSQL:

```powershell
dotnet ef database update --project ..\OcppGateway.Infrastructure --startup-project .
```

---

### 5. Compilar y Ejecutar la Solución

Desde la carpeta `OcppGateway`:

```powershell
dotnet run --launch-profile http
```

La aplicación se iniciará en `http://localhost:5168`.

---

## 🌐 Acceso a la Plataforma

- **Panel de Control (Dashboard Web)**: Abre en el navegador `http://localhost:5168`
- **Documentación de la API (Swagger UI)**: `http://localhost:5168/swagger`

### Credenciales de Acceso por Defecto
El sistema auto-inicializa un usuario administrador en la primera ejecución:
- **Usuario:** `admin`
- **Contraseña:** `admin`

*(Una vez dentro, puedes actualizar las credenciales desde la sección Administración).*

---

## 🔌 Simular una Estación de Carga (Simulador OCPP)

El repositorio incluye un script en PowerShell para emular el comportamiento de un cargador físico conectado por WebSocket al gateway:

```powershell
# Ejecutar simulador con un identificador de cargador
powershell -ExecutionPolicy Bypass -File .\simulate_charger.ps1 "CP-SIMULATED-01"
```

El simulador realizará:
1. Conexión WebSocket al gateway en `ws://localhost:5168/CP-SIMULATED-01`.
2. Envío de `BootNotification` inicial y reporte de estado `Available`.
3. Envío periódico de `Heartbeat` (cada 60 segundos).
4. Respuesta interactiva a comandos remotos (`RemoteStartTransaction`, `RemoteStopTransaction`, `Reset`).
5. Generación de telemetría y métricas de consumo `MeterValues` mientras está cargando.

---

## 🧪 Ejecución de Pruebas Unitarias

Para ejecutar el conjunto de pruebas automatizadas del proyecto:

```powershell
dotnet test .\OcppGateway.sln
```

---

## 📁 Estructura del Proyecto

```text
OcppGateway/
├── OcppGateway.sln                        # Solución principal de .NET
├── OcppGateway/                           # Capa de Presentación / Web API
│   ├── Endpoints/                         # Minimal API Endpoints (Auth, Assets, Commands, etc.)
│   ├── Extensions/                        # Inyección de dependencias y middlewares
│   ├── wwwroot/                           # Frontend Web (HTML, CSS, JS)
│   ├── appsettings.json                   # Configuración y conexión de servicios
│   ├── docker-compose.yml                 # Servicios de infraestructura (DB, Cache, Broker)
│   └── simulate_charger.ps1               # Simulador de cargador OCPP en PowerShell
├── OcppGateway.Application/               # Capa de Aplicación
│   ├── AI/                                # Interfaces y base de conocimiento
│   ├── Dtos/                              # Objetos de transferencia de datos
│   ├── Messaging/                         # Contratos de eventos
│   └── Services/                          # Lógica de negocio (Conexión, Smart Charging)
├── OcppGateway.Domain/                    # Capa de Dominio
│   ├── Entities/                          # Entidades centrales (Charger, Transaction, MeterValue, etc.)
│   └── Interfaces/                        # Contratos de repositorios y control
├── OcppGateway.Infrastructure/            # Capa de Infraestructura
│   ├── Adapters/                          # Adaptadores (WebSocket, Gemini API)
│   ├── Messaging/                         # Productor de mensajes RabbitMQ
│   ├── Migrations/                        # Migraciones de base de datos EF Core
│   ├── Persistence/                       # Contexto de base de datos y repositorios Postgres
│   └── Workers/                           # Hosted Services de monitoreo continuo
└── OcppGateway.Tests/                     # Suite de pruebas unitarias (xUnit, Moq)
```

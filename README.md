# TaskFlow - Project Management Platform

A full-stack project management application with Kanban board functionality, built with **Angular 18** and **.NET 8**.

## 🚀 Tech Stack

### Backend
- ASP.NET Core 8 Web API
- Clean Architecture (Domain, Application, Infrastructure, API)
- Entity Framework Core 8 (Code First)
- ASP.NET Core Identity + JWT Authentication
- SignalR (Real-time updates)
- SQL Server (LocalDB)

### Frontend
- Angular 18 (Standalone Components)
- Angular Signals (Reactive State Management)
- Angular CDK (Drag & Drop)
- SCSS with modern dark theme
- Lazy-loaded routes with Guards & Interceptors

## 📁 Project Structure

```
TaskFlow/
├── src/                        # Backend .NET Solution
│   ├── TaskFlow.Domain/        # Entities, Enums, Interfaces
│   ├── TaskFlow.Application/   # DTOs, Service Interfaces
│   ├── TaskFlow.Infrastructure/# EF Core, Identity, Services
│   └── TaskFlow.API/           # Controllers, Program.cs
├── client/                     # Frontend Angular App
│   └── src/app/
│       ├── models/             # TypeScript interfaces
│       ├── services/           # API services
│       ├── guards/             # Route guards
│       ├── interceptors/       # HTTP interceptors
│       └── pages/              # Page components
└── TaskFlow.sln
```

## 🛠 Getting Started

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- PgAdmin 4 Progest

### Backend
```bash
cd TaskFlow
dotnet restore
dotnet ef database update --project src/TaskFlow.Infrastructure --startup-project src/TaskFlow.API
dotnet run --project src/TaskFlow.API
```

### Frontend
```bash
cd TaskFlow/client
npm install
ng serve
```

Open http://localhost:4200

## ✨ Features
- 🔐 JWT Authentication (Register, Login, Refresh Token)
- 📋 Kanban Board with Drag & Drop
- 📊 Project Management (CRUD)
- 🔄 Real-time updates with SignalR
- 🎨 Modern dark glassmorphism UI
- 📱 Responsive design

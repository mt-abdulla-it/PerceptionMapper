<div align="center">

<img src="https://img.shields.io/badge/Platform-Windows-0078D4?style=for-the-badge&logo=windows&logoColor=white"/>
<img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white"/>
<img src="https://img.shields.io/badge/WPF-Desktop-68217A?style=for-the-badge&logo=visualstudio&logoColor=white"/>
<img src="https://img.shields.io/badge/SQLite-Database-003B57?style=for-the-badge&logo=sqlite&logoColor=white"/>
<img src="https://img.shields.io/badge/License-MIT-22C55E?style=for-the-badge"/>

# 🧠 Perception Mapper
### Communication Practice Platform

**An offline-first desktop application for emotional tone detection, bias analysis, and scenario-based communication coaching — supporting English, Tamil, and Sinhala.**

[Getting Started](#-quick-start) · [Features](#-features) · [API Docs](#-api-endpoints) · [Testing](#-testing--evaluation)

</div>

---

## 📖 Overview

Perception Mapper is a **Windows desktop application** that helps users improve their communication skills through real-time feedback. Practice spoken or written messages across professional scenarios, receive an AI-powered communication score, detect bias patterns, and export detailed session reports — all without an internet connection.

---

## ✨ Features

| Feature | Description |
|---|---|
| 🎯 **Scenario-based practice** | Job interview, conflict resolution, customer support, and more |
| 🎤 **Voice input** | Speak your message — transcribed and analyzed in real time |
| 📊 **Communication score** | 0–100 score based on clarity, tone, grammar, and professionalism |
| 🔍 **Bias detection** | Flags Gender, Age, Ability, and Race/Ethnicity bias with High/Medium severity |
| 🗣️ **Filler word detection** | Detects *um, uh, like, you know* — affects score and coaching |
| 💬 **Live coach feed** | Moment-level coaching during voice sessions |
| 🔄 **Target tone rephrase** | Pick Assertive / Empathetic / Formal before analysis for tailored rewrites |
| 📈 **Practice dashboard** | Session count, average score, best score, latest score |
| 📄 **Export report** | Saves `.txt` report to `Documents\PerceptionMapper\Reports` |
| 🌐 **Multi-language** | English, Tamil (தமிழ்), and Sinhala (සිංහල) voice support |
| 📡 **Offline banner** | Detects API downtime — local analysis continues uninterrupted |

---

## 🏗️ Solution Structure

```
PerceptionMapper/
├── src/
│   ├── PerceptionMapper.Domain/          # Entities, enums
│   ├── PerceptionMapper.Application/     # Tone, bias, feedback, scoring logic
│   ├── PerceptionMapper.Infrastructure/  # SQLite + EF Core, auth, repositories
│   ├── PerceptionMapper.Api/             # ASP.NET Core Web API + Swagger
│   ├── PerceptionMapper.Desktop/         # WPF desktop UI
│   └── PerceptionMapper.ML/              # Optional ONNX speech emotion model
├── tests/
│   ├── PerceptionMapper.Tests/           # xUnit unit tests
│   ├── PerceptionMapper.EvalTool/        # Tone + bias accuracy evaluation
│   ├── tone_eval_samples.json
│   └── bias_eval_samples.json
└── database/
    ├── Schema.sql                        # SQLite schema
    └── Schema-SqlServer.sql              # SQL Server reference schema
```

---

## ⚡ Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Windows 10/11
- Visual Studio 2022 *(recommended)* or VS Code

### Run

```powershell
# Clone and build
git clone https://github.com/your-username/PerceptionMapper.git
cd PerceptionMapper
dotnet build

# Terminal 1 — start the API
dotnet run --project src\PerceptionMapper.Api

# Terminal 2 — start the desktop app
dotnet run --project src\PerceptionMapper.Desktop
```

**Login credentials:** `demo` / `Demo@123`

> **Visual Studio tip:** Configure multiple startup projects → select both **Api** and **Desktop**, then press F5.

---

## ⚙️ Configuration

Edit `src/PerceptionMapper.Desktop/appsettings.json` to point the desktop app at your API:

```json
{
  "Api": {
    "BaseUrl": "http://localhost:5050"
  }
}
```

Swagger UI is available at **[http://localhost:5050/swagger](http://localhost:5050/swagger)** when the API is running.

---

## 🗄️ Database

Perception Mapper uses **SQLite** — no installation required. The database file `perceptionmapper.db` is created automatically when the API starts for the first time.

| File | Purpose |
|---|---|
| `database/Schema.sql` | SQLite schema (primary) |
| `database/Schema-SqlServer.sql` | SQL Server schema *(SSMS reference only)* |

---

## 📡 API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/health` | Health check — used by the offline banner |
| `POST` | `/api/auth/login` | Authenticate and receive a session token |
| `POST` | `/api/analysis/analyze` | Analyze message with `scenario`, `durationSeconds`, `rephraseTargetTone` |
| `GET` | `/api/analysis/practice-sessions/{userId}` | Retrieve a user's practice session history |

Full interactive documentation: **Swagger UI** at `/swagger`.

---

## 🧪 Testing & Evaluation

```powershell
# Run unit tests
dotnet test src\PerceptionMapper.Tests

# Run tone + bias accuracy evaluation (console output)
dotnet run --project src\PerceptionMapper.EvalTool

# Strict mode — exits with code 2 if any sample fails
dotnet run --project src\PerceptionMapper.EvalTool -- --strict
```

Evaluation samples are located in `tests/tone_eval_samples.json` and `tests/bias_eval_samples.json`.

---

## 🎬 Demo Script *(~3 minutes)*

1. Start **Api** + **Desktop** → log in with `demo`
2. Select **Job Interview** scenario → click **Speak now** → review score, tone, and voice timeline
3. Paste a sentence with bias language → observe **category** and **severity** flags
4. Click **Export report** → open the saved file from Documents
5. Run `dotnet test` and `EvalTool` → show accuracy output in the terminal

---

## 🛠️ Built With

- [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/) — REST API backend
- [WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/) — Windows desktop UI
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) — ORM + SQLite
- [xUnit](https://xunit.net/) — unit testing
- [Swashbuckle / Swagger](https://swagger.io/) — API documentation
- [ONNX Runtime](https://onnxruntime.ai/) *(optional)* — speech emotion model

---

## 👥 Authors

| Name | Role |
|---|---|
| **Suwedha** | Software Idea Planner — Concept & Planning |
| **Abdullah** | Software Developer — Development & Execution |

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

<div align="center">
  <sub>Built with ❤️ at Mars Tech Campus</sub>
</div>
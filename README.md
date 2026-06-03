# Tour Planner

Fullstack web application for planning tours (walking, cycling, driving),
picking routes interactively on a map and keeping a journal of completed
tours through structured tour logs. Built for **SWEN 2** at FH Technikum
Wien.

- **Frontend:** Angular 21 (signals, SSR), Tailwind CSS 4, Leaflet, Lucide
- **Backend:** ASP.NET Core (.NET 10) Web API
- **External APIs:** Nominatim (geocoding), OSRM (routing)
- **Architecture:** MVVM, reactive signals, singleton services
- **Patterns:** MVVM · Singleton · Observer · Facade · **Adapter** · Strategy

---

## Features

- **Tours:** create / edit / delete tours; pick *from* and *to* on an
  interactive Leaflet map; auto-computed distance & duration per transport
  mode (walking / cycling / driving); filter by transport and status.
- **Tour logs:** add / edit / delete logs per tour; distance and duration
  inherited from the tour; difficulty + 1–5 star rating + free-text comment.
- **Global search:** scope-aware navbar search filters tours (name / from /
  to) on the Tours page and logs (tour name / comment / difficulty /
  date) on the Logs page; live match-count chip and clear button.
- **Dashboard:** planned-tours widget, 5 most recent logs, aggregate stats.
- **Auth & profile:** login / register / logout, editable profile.
- **Responsive UI:** off-canvas sidebar drawer on mobile, fluid modal grids,
  Tailwind breakpoints throughout.

---

## Prerequisites

| Tool | Version |
|---|---|
| [Node.js](https://nodejs.org/) | ≥ 20 LTS |
| npm | ≥ 10 (ships with Node) |
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 |

No database setup is required — the current build uses in-memory state on
the frontend.

---

## Run the application

The project consists of two independent apps. Start them in **two
terminals**.

### 1) Backend (ASP.NET Core)

```powershell
cd backend/TourPlannerAPI
dotnet restore
dotnet run
```

The API starts on the URLs printed in the console (typically
`http://localhost:5000` / `https://localhost:7000`). The OpenAPI document
is exposed at `/openapi/v1.json` in development.

### 2) Frontend (Angular)

```powershell
cd frontend/TourPlannerWeb
npm install
npx ng serve

```

Open <http://localhost:4200>. Live-reload is enabled.

Side Note: Auth is not implemented. Login only needs '@'-symbol for email and any char for password!


> Tip: the responsive layout is best tested with the browser device
> toolbar (Ctrl+Shift+M in Chrome/Edge) at 375 × 667, 768 × 1024 and
> 1280 × 800.

---

## Build for production

```powershell
# frontend
cd frontend/TourPlannerWeb
npx ng build

# backend
cd backend/TourPlannerAPI
dotnet publish -c Release
```

The frontend output lands in `frontend/TourPlannerWeb/dist/TourPlannerWeb/`.

---

## Tooling

- **Jira** — sprint planning and SCRUM-prefixed branch names
  (`feature/SCRUM-10_Tour_Management`, `feature/SCRUM-11_Tour_Log_Management`,
  `feature/SCRUM-26_Map`, `feature/SCRUM-33_Search_Improvement` …)
- **Figma** — UI wireframes (link inside the protocol PDF)
- **GitHub** — version control & pull-request workflow

## License

[MIT](LICENSE) © 2026 Erion Polisi

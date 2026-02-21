# 🚚 FleetFlow: Enterprise Fleet Management System

FleetFlow is a comprehensive, full-stack fleet management solution designed to handle the complexities of logistics operations. It features robust Role-Based Access Control (RBAC), real-time trip dispatching, maintenance scheduling, financial analytics, and driver safety tracking.

![FleetFlow Demo](https://via.placeholder.com/1000x500?text=FleetFlow+Dashboard+Screenshot)

## ✨ Core Features

*   **🔒 Granular Role-Based Access Control (RBAC):** Four distinct user roles (Manager, Dispatcher, Safety Officer, Analyst) with strictly segregated capabilities enforced at both the UI and API levels.
*   **📊 Interactive Dashboard:** Real-time KPIs including active fleet size, utilization rates, pending cargo, and total revenue.
*   **🚛 Vehicle Registry & Lifecycle Management:** Complete CRUD operations for vehicles with status tracking (Available, OnTrip, InShop, Retired).
*   **🗺️ Trip Dispatching:** End-to-end trip lifecycle management (Draft ➔ Dispatched ➔ Completed ➔ Cancelled) with automatic odometer updates and validation checks.
*   **🔧 Preventative Maintenance Logs:** Track service history. Scheduling maintenance automatically moves vehicles to "In Shop" to prevent accidental dispatching.
*   **⛽ Expense & Fuel Tracking:** Detailed ledger for tracking operational costs, automatically rolled up into per-vehicle ROI calculations.
*   **👨‍✈️ Driver Safety Profiles:** Track driver compliance, license expiry, and auto-derived safety scores based on trip completion rates.
*   **📈 Financial Analytics & Reporting:** Deep insights with Recharts visualizations, CSV data dumps, and elegantly formatted PDF Executive Reports generated via QuestPDF.
*   **🕵️‍♂️ Audit Trail:** System-wide transparency logging every critical action (Create, Update, Delete, Status Change) with timestamps and user details.

## 🛠️ Technology Stack

### Frontend
*   **Framework:** React 19 (via Vite)
*   **Styling:** Custom CSS (Vanilla, CSS Variables, Responsive Grid/Flexbox)
*   **Routing:** React Router DOM v7
*   **Icons & Components:** Lucide React, React Hot Toast
*   **Data Visualization:** Recharts

### Backend
*   **Framework:** ASP.NET Core (.NET 10)
*   **Database & ORM:** SQL Server via Entity Framework Core (EF Core)
*   **Authentication:** JWT (JSON Web Tokens) with BCrypt Password Hashing
*   **Document Generation:** QuestPDF (for premium PDF analytics reports)
*   **Architecture:** Clean RESTful API design with separation of Controllers, Models, and DTOs.

## 🚀 Getting Started

### Prerequisites
*   [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
*   [Node.js](https://nodejs.org/) (v18 or higher)
*   [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express or Developer edition)

### Backend Setup (C# .NET)

1. Navigate to the backend directory:
   ```bash
   cd backend
   ```

2. Restore NuGet dependencies:
   ```bash
   dotnet restore
   ```

3. Update the database connection string (if using a specific SQL Server instance). By default, it uses `Server=(localdb)\\mssqllocaldb`. Check `appsettings.json`.

4. Apply database migrations to create the schema and seed default users:
   ```bash
   dotnet ef database update
   ```

5. Run the server:
   ```bash
   dotnet run
   ```
   *The backend will typically start on `http://localhost:5000`.*

### Frontend Setup (React/Vite)

1. Open a new terminal and navigate to the frontend directory:
   ```bash
   cd frontend
   ```

2. Install NPM dependencies:
   ```bash
   npm install
   ```

3. Start the development server:
   ```bash
   npm run dev
   ```
   *The frontend will start on the port specified by Vite (usually `http://localhost:5173`).*

## ⚠️ Troubleshooting

**Build Error: `MSB3021` / `MSB3027` (The file is locked by: 'FleetFlow.API')**
If you encounter this error when running `dotnet run` or `dotnet build`, it means a previous instance of the backend server is still running in the background and locking the `.exe` or `.dll` files.
To resolve this, forcefully close the running process:
*   **Windows (PowerShell):** `Stop-Process -Name "FleetFlow.API" -Force -ErrorAction SilentlyContinue`
*   **Mac/Linux:** `pkill -f FleetFlow.API`

## 🔑 Default Seed Credentials

Upon your first database migration, the following mock users are generated so you can explore the RBAC features:

| Role | Email | Password | Permissions Summary |
| :--- | :--- | :--- | :--- |
| **Manager** | `admin@fleet.com` | `Admin123!` | Full system access. Create/Edit/Delete across all modules. Manage users. |
| **Dispatcher** | `dispatcher@fleet.com` | `Dispatch123!` | Manage Trips and Expenses. Read-only access to Vehicles and Drivers. |
| **Safety Officer** | `safety@fleet.com` | `Safety123!` | Full control over Driver Profiles. Read-only access to Vehicles. |
| **Analyst** | `analyst@fleet.com` | `Analyst123!` | Read operations and full access to Analytics/PDF Exports. |

## 📁 Project Structure

```text
FleetFlow/
├── backend/                  # ASP.NET Core Web API
│   ├── Controllers/          # API Route endpoints
│   ├── Data/                 # EF Core DbContext & Seed Data
│   ├── DTOs/                 # Data Transfer Objects
│   ├── Models/               # Entity Models
│   ├── Program.cs            # App configuration and middleware
│   └── appsettings.json      # Configs (ConnectionStrings, JWT Secret)
│
└── frontend/                 # React SPA
    ├── public/               # Static assets
    ├── src/
    │   ├── components/       # Reusable UI (Sidebar, Layout, ProtectedRoute)
    │   ├── context/          # React Context (AuthContext)
    │   ├── pages/            # View components (Dashboard, Registry, trips, etc.)
    │   ├── services/         # Axios interceptors (api.js)
    │   ├── App.jsx           # App routing
    │   └── index.css         # Global stylesheet
    └── package.json          # Frontend dependencies
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License.

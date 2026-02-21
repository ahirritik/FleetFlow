namespace FleetFlow.API.DTOs;

// ── Auth ──
public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, string FullName, string Role);

// ── User Management ──
public record UserDto(int Id, string FullName, string Email, string Role, DateTime CreatedAt);
public record UserCreateDto(string FullName, string Email, string Password, string Role);
public record UserUpdateDto(string FullName, string Email, string Role);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);

// ── Vehicle ──
public record VehicleDto(int Id, string Name, string Model, string LicensePlate, string Type,
    double MaxCapacity, double Odometer, string Status, string Region, double AcquisitionCost, DateTime CreatedAt);
public record VehicleCreateDto(string Name, string Model, string LicensePlate, string Type,
    double MaxCapacity, double Odometer, string Region, double AcquisitionCost);
public record VehicleUpdateDto(string Name, string Model, string LicensePlate, string Type,
    double MaxCapacity, string Region, double AcquisitionCost);

// ── Driver ──
public record DriverDto(int Id, string FullName, string LicenseNumber, DateTime LicenseExpiry,
    string LicenseCategory, string Phone, string Status, double SafetyScore, int TripCount, int CompletedTrips, DateTime CreatedAt);
public record DriverCreateDto(string FullName, string LicenseNumber, DateTime LicenseExpiry,
    string LicenseCategory, string Phone);
public record DriverUpdateDto(string FullName, string LicenseNumber, DateTime LicenseExpiry,
    string LicenseCategory, string Phone);

// ── Trip ──
public record TripDto(int Id, int VehicleId, string? VehicleName, string? VehiclePlate,
    int DriverId, string? DriverName, string Origin, string Destination, double CargoWeight,
    string CargoDescription, string Status, double StartOdometer, double EndOdometer,
    DateTime CreatedAt, DateTime? DispatchedAt, DateTime? CompletedAt);
public record TripCreateDto(int VehicleId, int DriverId, string Origin, string Destination,
    double CargoWeight, string CargoDescription);
public record TripCompleteDto(double EndOdometer);

// ── Maintenance ──
public record MaintenanceLogDto(int Id, int VehicleId, string? VehicleName, string ServiceType,
    string Description, double Cost, DateTime Date, bool IsCompleted);
public record MaintenanceCreateDto(int VehicleId, string ServiceType, string Description, double Cost, DateTime Date);

// ── Expense ──
public record ExpenseDto(int Id, int VehicleId, string? VehicleName, int? TripId,
    string Category, double? Liters, double Cost, DateTime Date, string Notes);
public record ExpenseCreateDto(int VehicleId, int? TripId, string Category, double? Liters, double Cost, DateTime Date, string Notes);

// ── Analytics ──
public record DashboardDto(int ActiveFleet, int MaintenanceAlerts, double UtilizationRate, int PendingCargo,
    int TotalVehicles, int TotalDrivers, int TotalTrips, double TotalRevenue);
public record VehicleCostDto(int VehicleId, string VehicleName, string LicensePlate,
    double FuelCost, double MaintenanceCost, double TotalCost, double Odometer, double CostPerKm, double AcquisitionCost, double Roi,
    double TotalLiters, double FuelEfficiency);

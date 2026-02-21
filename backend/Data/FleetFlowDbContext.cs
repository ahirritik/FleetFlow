using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using FleetFlow.API.Models;

namespace FleetFlow.API.Data;

public class FleetFlowDbContext : DbContext
{
    public FleetFlowDbContext(DbContextOptions<FleetFlowDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<MaintenanceLog> MaintenanceLogs => Set<MaintenanceLog>();
    public DbSet<Expense> Expenses => Set<Expense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unique constraints
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Vehicle>().HasIndex(v => v.LicensePlate).IsUnique();
        modelBuilder.Entity<Driver>().HasIndex(d => d.LicenseNumber).IsUnique();

        // Relationships
        modelBuilder.Entity<Trip>()
            .HasOne(t => t.Vehicle)
            .WithMany(v => v.Trips)
            .HasForeignKey(t => t.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Trip>()
            .HasOne(t => t.Driver)
            .WithMany(d => d.Trips)
            .HasForeignKey(t => t.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MaintenanceLog>()
            .HasOne(m => m.Vehicle)
            .WithMany(v => v.MaintenanceLogs)
            .HasForeignKey(m => m.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Expense>()
            .HasOne(e => e.Vehicle)
            .WithMany(v => v.Expenses)
            .HasForeignKey(e => e.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Expense>()
            .HasOne(e => e.Trip)
            .WithMany(t => t.Expenses)
            .HasForeignKey(e => e.TripId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed Data
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, FullName = "Admin Manager", Email = "admin@fleet.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"), Role = "Manager", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new User { Id = 2, FullName = "Sarah Dispatcher", Email = "dispatcher@fleet.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Dispatch123!"), Role = "Dispatcher", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new User { Id = 3, FullName = "Officer Mike", Email = "safety@fleet.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Safety123!"), Role = "SafetyOfficer", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new User { Id = 4, FullName = "Emma Analyst", Email = "analyst@fleet.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Analyst123!"), Role = "Analyst", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<Vehicle>().HasData(
            new Vehicle { Id = 1, Name = "Truck-01", Model = "Volvo FH16", LicensePlate = "TRK-1001", Type = "Truck", MaxCapacity = 20000, Odometer = 45200, Status = "Available", Region = "North", AcquisitionCost = 85000, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Vehicle { Id = 2, Name = "Truck-02", Model = "Scania R500", LicensePlate = "TRK-1002", Type = "Truck", MaxCapacity = 18000, Odometer = 32100, Status = "Available", Region = "North", AcquisitionCost = 78000, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Vehicle { Id = 3, Name = "Van-01", Model = "Mercedes Sprinter", LicensePlate = "VAN-2001", Type = "Van", MaxCapacity = 3500, Odometer = 21000, Status = "Available", Region = "East", AcquisitionCost = 42000, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Vehicle { Id = 4, Name = "Van-02", Model = "Ford Transit", LicensePlate = "VAN-2002", Type = "Van", MaxCapacity = 2800, Odometer = 15600, Status = "Available", Region = "West", AcquisitionCost = 35000, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Vehicle { Id = 5, Name = "Bike-01", Model = "Honda CB500X", LicensePlate = "BKE-3001", Type = "Bike", MaxCapacity = 50, Odometer = 8200, Status = "Available", Region = "South", AcquisitionCost = 7500, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<Driver>().HasData(
            new Driver { Id = 1, FullName = "Alex Johnson", LicenseNumber = "DL-10001", LicenseExpiry = new DateTime(2027, 6, 15, 0, 0, 0, DateTimeKind.Utc), LicenseCategory = "Truck", Phone = "555-0101", Status = "OnDuty", SafetyScore = 95, TripCount = 120, CompletedTrips = 118, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Driver { Id = 2, FullName = "Maria Garcia", LicenseNumber = "DL-10002", LicenseExpiry = new DateTime(2027, 3, 22, 0, 0, 0, DateTimeKind.Utc), LicenseCategory = "Van", Phone = "555-0102", Status = "OnDuty", SafetyScore = 98, TripCount = 85, CompletedTrips = 85, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Driver { Id = 3, FullName = "James Wilson", LicenseNumber = "DL-10003", LicenseExpiry = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc), LicenseCategory = "Truck", Phone = "555-0103", Status = "OnDuty", SafetyScore = 88, TripCount = 60, CompletedTrips = 57, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}

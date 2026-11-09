using System;
using System.Collections.Generic;
using StockPortal.Models;

namespace StockPortal.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalAssets { get; set; }
        public int InServiceAssets { get; set; }
        public int InStockAssets { get; set; }
        public int UnderMaintenanceAssets { get; set; }
        public decimal TotalInventoryValue { get; set; }

        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int PendingTickets { get; set; }
        public int DelayedTickets { get; set; }
        public int ClosedTickets { get; set; }

        public List<Ticket> UrgentTickets { get; set; } = new();
        public List<Ticket> RecentTickets { get; set; } = new();
        public List<Asset> OverduePeriodicAssets { get; set; } = new();
        public List<Asset> UpcomingPeriodicAssets { get; set; } = new();
        public List<Asset> OverdueCalibrationAssets { get; set; } = new();
        public List<Asset> UpcomingCalibrationAssets { get; set; } = new();
        public List<AssetMaintenance> RecentMaintenance { get; set; } = new();

        // Maintenance & Teams Analytics Tab Properties
        public string ActiveTab { get; set; } = "overview"; // "overview" or "analytics"
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string SelectedTeam { get; set; } = "ALL";
        public string DatePreset { get; set; } = "30days";
        public int? SelectedTechUserId { get; set; }
        public bool CanFilterTeam { get; set; } = true;
        public string? UserMaintenanceTeam { get; set; }
        public List<User> AvailableTechnicians { get; set; } = new();

        public int FilteredTotalTickets { get; set; }
        public int FilteredClosedTickets { get; set; }
        public int FilteredActiveTickets { get; set; }
        public double FilteredResolutionRate { get; set; }
        public double FilteredAverageResolutionHours { get; set; }
        public int FilteredMaintenanceLogsCount { get; set; }

        public List<TechnicianWorkloadStat> TechnicianStats { get; set; } = new();
        public List<TeamAnalyticsStat> TeamStats { get; set; } = new();
        public List<CategoryAnalyticsStat> CategoryStats { get; set; } = new();
        public List<ClosedTicketRecordItem> FilteredClosedTicketsLog { get; set; } = new();
    }

    public class TechnicianWorkloadStat
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string? FullNameAr { get; set; }
        public string DisplayFullName => (System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("ar") && !string.IsNullOrWhiteSpace(FullNameAr)) ? FullNameAr : FullName;
        public string? DomainUser { get; set; }
        public string MaintenanceTeam { get; set; } = "";
        public string RoleDisplay { get; set; } = "";
        public int ClosedTicketsCount { get; set; }
        public int ActiveTicketsCount { get; set; }
        public int TotalHandledTickets { get; set; }
        public double AverageResolutionHours { get; set; }
        public int MaintenanceWorkOrdersCount { get; set; }
    }

    public class TeamAnalyticsStat
    {
        public string TeamCode { get; set; } = "";
        public string TeamName { get; set; } = "";
        public int TotalTickets { get; set; }
        public int ClosedTickets { get; set; }
        public int ActiveTickets { get; set; }
        public int UrgentTickets { get; set; }
        public double ResolutionRate { get; set; }
        public double AverageResolutionHours { get; set; }
    }

    public class CategoryAnalyticsStat
    {
        public string Category { get; set; } = "";
        public string DisplayCategory { get; set; } = "";
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class ClosedTicketRecordItem
    {
        public int TicketId { get; set; }
        public string? Title { get; set; }
        public string? TargetTeam { get; set; }
        public string? Type { get; set; }
        public int? Priority { get; set; }
        public DateTime? OpenedDT { get; set; }
        public DateTime? ClosedDT { get; set; }
        public double TurnaroundHours { get; set; }
        public string? RequesterName { get; set; }
        public string? EquipmentTag { get; set; }
        public string? EquipmentBrand { get; set; }
        public int? AssetId { get; set; }
        public string? ResolutionNotes { get; set; }
        public List<User> ResolvedByUsers { get; set; } = new();
    }

    public class TechnicianReportViewModel
    {
        public User Technician { get; set; } = null!;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string DatePreset { get; set; } = "30days";

        public int ClosedTicketsCount { get; set; }
        public int ActiveTicketsCount { get; set; }
        public int TotalHandledTickets { get; set; }
        public double AverageResolutionHours { get; set; }
        public int WorkOrdersCount { get; set; }

        public List<ClosedTicketRecordItem> ResolvedTickets { get; set; } = new();
        public List<Ticket> ActiveTickets { get; set; } = new();
        public List<AssetMaintenance> MaintenanceLogs { get; set; } = new();
        public List<CategoryAnalyticsStat> CategoryBreakdown { get; set; } = new();
    }

    public class TicketDetailsViewModel
    {
        public Ticket Ticket { get; set; } = null!;
        public List<TicketStatus> Statuses { get; set; } = new();
        public List<User> Technicians { get; set; } = new();
        public List<User> DepartmentEmployees { get; set; } = new();
        public List<Supplier> Suppliers { get; set; } = new();

        // Reserve Stock & Spare Parts
        public List<SparePart> AvailableSpareParts { get; set; } = new();
        public List<SparePartTransaction> UsedSpareParts { get; set; } = new();

        // Forms inside Details view
        public string? NewMessage { get; set; }
        public int? NewTechUserId { get; set; }
        public int? NewStatusId { get; set; }

        // Quick Maintenance Log
        public string? MaintenanceDescription { get; set; }
        public string? MaintenancePartsReplaced { get; set; }
        public int? MaintenanceSupplierId { get; set; }
    }

    public class AssetDetailsViewModel
    {
        public Asset Asset { get; set; } = null!;
        public List<User> AvailableUsers { get; set; } = new();
        public List<Room> AvailableRooms { get; set; } = new();
        public List<CustodyType> AvailableCustodyTypes { get; set; } = new();

        // Transfer Custody Form
        public int NewUserId { get; set; }
        public int NewRoomId { get; set; }
        public int NewCustodyTypeId { get; set; }
        public string? TransferNotes { get; set; }

        public List<CalibrationForm> AvailableCalibrationProtocols { get; set; } = new();
        public List<ChecklistForm> AvailableChecklists { get; set; } = new();
        public List<SparePart> AvailableSpareParts { get; set; } = new();
        public List<User> AvailableTechnicians { get; set; } = new();
    }

    public class MaintenanceScheduleViewModel
    {
        public List<Asset> OverdueAssets { get; set; } = new();
        public List<Asset> DueWithin30DaysAssets { get; set; } = new();
        public List<Asset> UpToDateAssets { get; set; } = new();
        public List<Asset> OverdueCalibrationAssets { get; set; } = new();
        public List<Asset> UpcomingCalibrationAssets { get; set; } = new();
        public List<Asset> ValidCalibrationAssets { get; set; } = new();
        public List<AssetMaintenance> RecentLogs { get; set; } = new();
        public List<AssetMaintenance> ActiveWorkOrders { get; set; } = new();
    }

    public class AutoTicketViewModel
    {
        public string DetectedPcName { get; set; } = string.Empty;
        public string ClientIp { get; set; } = string.Empty;
        public bool IsAssetFound { get; set; }
        public Asset? MatchedAsset { get; set; }

        // Form Fields
        public int? SelectedAssetId { get; set; }
        public int? SelectedRoomId { get; set; }
        public int? SelectedUserId { get; set; }
        public string Category { get; set; } = "Hardware";
        public int Priority { get; set; } = 2; // Medium
        public string? UserNotes { get; set; }

        // Dropdown data
        public List<Asset> AvailableAssets { get; set; } = new();
        public List<Room> AvailableRooms { get; set; } = new();
        public List<User> AvailableUsers { get; set; } = new();
    }

    public class AssetDropdownItem
    {
        public int Id { get; set; }
        public string Display { get; set; } = string.Empty;
        public string MaintenanceTeam { get; set; } = string.Empty;
    }

    public class SparePartsIndexViewModel
    {
        public List<SparePart> Items { get; set; } = new();
        public string? CurrentDepartment { get; set; }
        public string? SelectedCategory { get; set; }
        public string? Search { get; set; }
        public bool OnlyLowStock { get; set; }
        public int TotalItems { get; set; }
        public int TotalQuantity { get; set; }
        public int LowStockCount { get; set; }
        public int DualNatureCount { get; set; }
        public List<string> AvailableDepartments { get; set; } = new();
        public List<User> Users { get; set; } = new();
        public List<Room> Rooms { get; set; } = new();
        public List<SparePartForm> AvailableForms { get; set; } = new();
        public List<Asset> AvailableAssets { get; set; } = new();
    }

    public class SparePartTransactionsViewModel
    {
        public List<SparePartTransaction> Transactions { get; set; } = new();
        public SparePart? FilterPart { get; set; }
        public int? TicketId { get; set; }
        public string? Department { get; set; }
    }
}

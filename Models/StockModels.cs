using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using StockPortal.Services;

namespace StockPortal.Models
{
    public static class SystemRoles
    {
        public const string Admin = "ADMIN";
        public const string Maintenance = "MAINTENANCE";
        public const string EndUser = "END_USER";

        public static readonly string[] All = { Admin, Maintenance, EndUser };
    }

    public static class MaintenanceTeams
    {
        public const string IT = "IT";
        public const string Biomedical = "BIOMEDICAL";
        public const string General = "GENERAL";

        public static readonly string[] All = { IT, Biomedical, General };
    }

    [Table("custody_type")]
    public class CustodyType
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("type")]
        [StringLength(255)]
        public string? Type { get; set; }

        [Column("type_ar")]
        [StringLength(255)]
        public string? TypeAr { get; set; }

        [NotMapped]
        public string DisplayType => (CultureInfo.CurrentUICulture.Name.StartsWith("ar") && !string.IsNullOrWhiteSpace(TypeAr)) ? TypeAr : (Type ?? "");

        public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
    }

    [Table("ticket_status")]
    public class TicketStatus
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("status")]
        [StringLength(255)]
        public string? Status { get; set; }

        [Column("status_ar")]
        [StringLength(255)]
        public string? StatusAr { get; set; }

        [NotMapped]
        public string DisplayStatus => (CultureInfo.CurrentUICulture.Name.StartsWith("ar") && !string.IsNullOrWhiteSpace(StatusAr)) ? StatusAr : (Status ?? "");

        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }

    [Table("departments")]
    public class Department
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [StringLength(255)]
        [Display(Name = "Department Name")]
        public string? Name { get; set; }

        [Column("name_ar")]
        [StringLength(255)]
        [Display(Name = "Department Name (Arabic)")]
        public string? NameAr { get; set; }

        [NotMapped]
        public string DisplayName => (CultureInfo.CurrentUICulture.Name.StartsWith("ar") && !string.IsNullOrWhiteSpace(NameAr)) ? NameAr : (Name ?? "");

        [Column("headUserID")]
        public int? HeadUserId { get; set; }

        [ForeignKey("HeadUserId")]
        public virtual User? HeadUser { get; set; }

        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }

    [Table("rooms")]
    public class Room
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("roomNo")]
        [Display(Name = "Room Number")]
        public int? RoomNo { get; set; }

        [Column("floor")]
        [Display(Name = "Floor")]
        public int? Floor { get; set; }

        [Column("room_name_ar")]
        [StringLength(255)]
        [Display(Name = "Room Name (Arabic)")]
        public string? RoomNameAr { get; set; }

        [NotMapped]
        public string DisplayName
        {
            get
            {
                if (CultureInfo.CurrentUICulture.Name.StartsWith("ar"))
                {
                    var prefix = !string.IsNullOrWhiteSpace(RoomNameAr) ? $"{RoomNameAr} - " : "";
                    return $"{prefix}غرفة {RoomNo ?? 0} (الطابق {Floor ?? 0})";
                }
                return $"Room {RoomNo ?? 0} (Floor {Floor ?? 0})";
            }
        }

        public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
    }

    [Table("suppliers")]
    public class Supplier
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [StringLength(255)]
        [Display(Name = "Supplier Name")]
        public string? Name { get; set; }

        [Column("name_ar")]
        [StringLength(255)]
        [Display(Name = "Supplier Name (Arabic)")]
        public string? NameAr { get; set; }

        [NotMapped]
        public string DisplayName => (CultureInfo.CurrentUICulture.Name.StartsWith("ar") && !string.IsNullOrWhiteSpace(NameAr)) ? NameAr : (Name ?? "");

        [Column("isActive")]
        public bool? IsActive { get; set; } = true;

        [Column("type")]
        [Display(Name = "Supplier Type")]
        public int? Type { get; set; } // 1: Items, 2: Maintenance, 3: Both

        [Column("departments")]
        [Display(Name = "Supported Departments (JSON)")]
        public string? Departments { get; set; } = "[]";

        [NotMapped]
        public string TypeName => Type switch
        {
            1 => "Equipment & Items",
            2 => "Maintenance Provider",
            3 => "Full Service (Items & Maintenance)",
            _ => "General Vendor"
        };

        [NotMapped]
        public string DisplayTypeName => CultureInfo.CurrentUICulture.Name.StartsWith("ar")
            ? Type switch
            {
                1 => "الأجهزة والمعدات",
                2 => "مزود خدمات صيانة",
                3 => "خدمة متكاملة (أجهزة وصيانة)",
                _ => "مورد عام"
            }
            : TypeName;

        [NotMapped]
        public List<string> DepartmentList
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Departments)) return new List<string>();
                try
                {
                    return JsonSerializer.Deserialize<List<string>>(Departments, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
            set
            {
                Departments = JsonSerializer.Serialize(value ?? new List<string>());
            }
        }

        public bool SupportsDepartment(string? department)
        {
            if (string.IsNullOrWhiteSpace(department)) return true;
            var list = DepartmentList;
            if (!list.Any()) return true; // Common/unrestricted if not configured
            return list.Any(d => string.Equals(d, department, StringComparison.OrdinalIgnoreCase) || 
                                 string.Equals(d, "ALL", StringComparison.OrdinalIgnoreCase) || 
                                 string.Equals(d, "COMMON", StringComparison.OrdinalIgnoreCase));
        }

        public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
    }

    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("fullName")]
        [StringLength(255)]
        [Display(Name = "Full Name")]
        public string? FullName { get; set; }

        [Column("full_name_ar")]
        [StringLength(255)]
        [Display(Name = "Full Name (Arabic)")]
        public string? FullNameAr { get; set; }

        [NotMapped]
        public string DisplayFullName => (CultureInfo.CurrentUICulture.Name.StartsWith("ar") && !string.IsNullOrWhiteSpace(FullNameAr)) ? FullNameAr : (FullName ?? "");

        [Column("departmentID")]
        public int? DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        [Column("domainUser")]
        [StringLength(255)]
        [Display(Name = "Username / Domain")]
        public string? DomainUser { get; set; }

        [Column("role")]
        [StringLength(255)]
        [Display(Name = "Role / Title")]
        public string? Role { get; set; } = SystemRoles.EndUser;

        [Column("role_ar")]
        [StringLength(255)]
        [Display(Name = "Role / Title (Arabic)")]
        public string? RoleAr { get; set; }

        [Column("maintenance_team")]
        [StringLength(50)]
        [Display(Name = "Maintenance Team")]
        public string? MaintenanceTeam { get; set; }

        [Column("isActive")]
        public bool? IsActive { get; set; } = true;

        [NotMapped]
        public string RoleDisplay
        {
            get
            {
                bool isAr = CultureInfo.CurrentUICulture.Name.StartsWith("ar");
                if (isAr && !string.IsNullOrWhiteSpace(RoleAr))
                {
                    return RoleAr;
                }
                if (string.Equals(Role, SystemRoles.Maintenance, StringComparison.OrdinalIgnoreCase) ||
                    (Role != null && Role.IndexOf("MAINTENANCE", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    var team = !string.IsNullOrWhiteSpace(MaintenanceTeam) ? MaintenanceTeam.ToUpperInvariant() : "GENERAL";
                    return isAr ? $"صيانة ({team})" : $"MAINTENANCE ({team})";
                }
                return Role ?? SystemRoles.EndUser;
            }
        }

        [NotMapped]
        public string DisplayRole => RoleDisplay;

        public bool BelongsToResponsibleDepartment(string? targetTeam)
        {
            if (string.IsNullOrWhiteSpace(targetTeam)) return true;
            var normTeam = targetTeam.Trim().ToUpperInvariant();

            // 1. Direct match on MaintenanceTeam: specialized technicians belong strictly to their designated maintenance team
            if (!string.IsNullOrWhiteSpace(MaintenanceTeam))
            {
                return string.Equals(MaintenanceTeam.Trim(), normTeam, StringComparison.OrdinalIgnoreCase);
            }

            // 2. Administrators have cross-department oversight
            if (!string.IsNullOrWhiteSpace(Role) && (string.Equals(Role.Trim(), SystemRoles.Admin, StringComparison.OrdinalIgnoreCase) || Role.Contains("Admin", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // 3. User's Department matches the maintenance target team (only when user has no explicit MaintenanceTeam assigned)
            if (Department != null && !string.IsNullOrWhiteSpace(Department.Name))
            {
                var dName = Department.Name;
                if (normTeam == MaintenanceTeams.IT && (dName.Contains("IT", StringComparison.OrdinalIgnoreCase) || dName.Contains("Information", StringComparison.OrdinalIgnoreCase) || dName.Contains("Technology", StringComparison.OrdinalIgnoreCase)))
                    return true;
                if (normTeam == MaintenanceTeams.Biomedical && (dName.Contains("Biomedical", StringComparison.OrdinalIgnoreCase) || dName.Contains("Medical", StringComparison.OrdinalIgnoreCase)))
                    return true;
                if (normTeam == MaintenanceTeams.General && (dName.Contains("Facility", StringComparison.OrdinalIgnoreCase) || dName.Contains("Engineering", StringComparison.OrdinalIgnoreCase) || dName.Contains("Maintenance", StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            // 4. Role title explicitly specifies the team/discipline
            if (!string.IsNullOrWhiteSpace(Role))
            {
                if (normTeam == MaintenanceTeams.IT && (Role.Contains("IT", StringComparison.OrdinalIgnoreCase) || Role.Contains("SysAdmin", StringComparison.OrdinalIgnoreCase) || Role.Contains("Support", StringComparison.OrdinalIgnoreCase)))
                    return true;
                if (normTeam == MaintenanceTeams.Biomedical && Role.Contains("Biomedical", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (normTeam == MaintenanceTeams.General && (Role.Contains("Facility", StringComparison.OrdinalIgnoreCase) || Role.Contains("General", StringComparison.OrdinalIgnoreCase) || Role.Contains("Maintenance", StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            return false;
        }

        public virtual ICollection<Asset> CustodyAssets { get; set; } = new List<Asset>();
        public virtual ICollection<Ticket> OpenedTickets { get; set; } = new List<Ticket>();
    }

    [Table("assets")]
    public class Asset
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("categoryID")]
        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        [Display(Name = "Category")]
        public virtual AssetCategory? Category { get; set; }

        [Column("price", TypeName = "decimal(10,2)")]
        [Display(Name = "Purchase Price")]
        [DataType(DataType.Currency)]
        public decimal? Price { get; set; }

        [Column("hospitalNo")]
        [StringLength(255)]
        [Display(Name = "Asset Tag / Hospital No")]
        public string? HospitalNo { get; set; }

        [Column("serialNo")]
        [StringLength(255)]
        [Display(Name = "Serial Number")]
        public string? SerialNo { get; set; }

        [Column("brand")]
        [StringLength(255)]
        [Display(Name = "Brand / Manufacturer")]
        public string? Brand { get; set; }

        [Column("modelNo")]
        [Display(Name = "Model / Version")]
        public int? ModelNo { get; set; }

        [Column("supplierID")]
        public int? SupplierId { get; set; }

        [ForeignKey("SupplierId")]
        public virtual Supplier? Supplier { get; set; }

        [Column("status")]
        [StringLength(255)]
        [Display(Name = "Operational Status")]
        public string? Status { get; set; } = "In Service";

        [Column("status_ar")]
        [StringLength(255)]
        public string? StatusAr { get; set; }

        [NotMapped]
        public string DisplayStatus => (CultureInfo.CurrentUICulture.Name.StartsWith("ar") && !string.IsNullOrWhiteSpace(StatusAr))
            ? StatusAr
            : (CultureInfo.CurrentUICulture.Name.StartsWith("ar") ? LocHelper.TranslateStatus(Status) : (Status ?? "In Service"));

        [Column("userID")]
        public int? UserId { get; set; }

        [NotMapped]
        public int? CustodianId
        {
            get => UserId;
            set => UserId = value;
        }

        [ForeignKey("UserId")]
        [Display(Name = "Assigned Custodian")]
        public virtual User? User { get; set; }

        [NotMapped]
        public virtual User? Custodian
        {
            get => User;
            set => User = value;
        }

        [Column("maintenance_team")]
        [StringLength(50)]
        [Display(Name = "Maintenance Team")]
        public string MaintenanceTeam { get; set; } = MaintenanceTeams.IT;

        [Column("roomID")]
        public int? RoomId { get; set; }

        [ForeignKey("RoomId")]
        [Display(Name = "Location / Room")]
        public virtual Room? Room { get; set; }

        [Column("custodyType")]
        public int? CustodyTypeId { get; set; }

        [ForeignKey("CustodyTypeId")]
        public virtual CustodyType? CustodyType { get; set; }

        [Column("type")]
        [StringLength(255)]
        [Display(Name = "Equipment Type")]
        public string? Type { get; set; } // Laptop, Monitor, Medical Device, Server, Printer, etc.

        [Column("equipment_type_ar")]
        [StringLength(255)]
        public string? EquipmentTypeAr { get; set; }

        [NotMapped]
        public string DisplayType => (CultureInfo.CurrentUICulture.Name.StartsWith("ar") && !string.IsNullOrWhiteSpace(EquipmentTypeAr))
            ? EquipmentTypeAr
            : (CultureInfo.CurrentUICulture.Name.StartsWith("ar") ? LocHelper.TranslateType(Type) : (Type ?? ""));

        [Column("warrantyTill", TypeName = "date")]
        [Display(Name = "Warranty Expiration")]
        [DataType(DataType.Date)]
        public DateTime? WarrantyTill { get; set; }

        [Column("specs")]
        [Display(Name = "Technical Specifications")]
        public string? Specs { get; set; }

        [Column("buyingDate")]
        [Display(Name = "Purchase Date")]
        public DateTime? BuyingDate { get; set; }

        [Column("needsPeriodic")]
        [Display(Name = "Requires Periodic Maintenance")]
        public bool? NeedsPeriodic { get; set; } = false;

        [Column("periodicInterval")]
        [Display(Name = "Interval (Days)")]
        public int? PeriodicInterval { get; set; }

        [Column("lastPeriodic", TypeName = "date")]
        [Display(Name = "Last Periodic Check")]
        [DataType(DataType.Date)]
        public DateTime? LastPeriodic { get; set; }

        [Column("nextPeriodic", TypeName = "date")]
        [Display(Name = "Next Periodic Due")]
        [DataType(DataType.Date)]
        public DateTime? NextPeriodic { get; set; }

        [Column("needsCalibration")]
        [Display(Name = "Requires Periodic Calibration")]
        public bool? NeedsCalibration { get; set; } = false;

        [Column("calibrationInterval")]
        [Display(Name = "Calibration Interval (Days)")]
        public int? CalibrationInterval { get; set; }

        [Column("lastCalibration", TypeName = "date")]
        [Display(Name = "Last Calibration Date")]
        [DataType(DataType.Date)]
        public DateTime? LastCalibration { get; set; }

        [Column("nextCalibration", TypeName = "date")]
        [Display(Name = "Next Calibration Due Date")]
        [DataType(DataType.Date)]
        public DateTime? NextCalibration { get; set; }

        [Column("calibrationCertNo")]
        [StringLength(255)]
        [Display(Name = "Calibration Certificate / Report No")]
        public string? CalibrationCertNo { get; set; }

        [Column("calibratedBy")]
        [StringLength(255)]
        [Display(Name = "Calibrated By / Calibration Agency")]
        public string? CalibratedBy { get; set; }

        [Column("calibrationNotes")]
        [StringLength(1024)]
        [Display(Name = "Calibration Notes / Standards")]
        public string? CalibrationNotes { get; set; }

        [NotMapped]
        public bool IsCalibrationOverdue =>
            NeedsCalibration == true && NextCalibration.HasValue && NextCalibration.Value < DateTime.Now.Date;

        [NotMapped]
        public bool IsCalibrationDueSoon =>
            NeedsCalibration == true && NextCalibration.HasValue &&
            NextCalibration.Value >= DateTime.Now.Date &&
            NextCalibration.Value <= DateTime.Now.Date.AddDays(30);

        [NotMapped]
        public string CalibrationStatus => NeedsCalibration != true ? "Not Required" :
            IsCalibrationOverdue ? "Overdue" :
            IsCalibrationDueSoon ? "Due Soon" :
            NextCalibration.HasValue ? "Calibrated & Valid" : "Pending Calibration";

        [Column("installed_softwares")]
        [Display(Name = "Installed Software Packages")]
        public string? InstalledSoftwares { get; set; } = "[]";

        [NotMapped]
        public List<string> InstalledSoftwaresList
        {
            get
            {
                if (string.IsNullOrWhiteSpace(InstalledSoftwares)) return new List<string>();
                try
                {
                    return JsonSerializer.Deserialize<List<string>>(InstalledSoftwares) ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
            set
            {
                InstalledSoftwares = JsonSerializer.Serialize(value ?? new List<string>());
            }
        }

        [Column("has_vendor_maintenance")]
        [Display(Name = "Free Vendor Maintenance (After Sale)")]
        public bool HasVendorMaintenance { get; set; } = false;

        [Column("vendor_maintenance_start", TypeName = "date")]
        [Display(Name = "Vendor Maintenance Start Date")]
        [DataType(DataType.Date)]
        public DateTime? VendorMaintenanceStartDate { get; set; }

        [Column("vendor_maintenance_end", TypeName = "date")]
        [Display(Name = "Vendor Maintenance End Date")]
        [DataType(DataType.Date)]
        public DateTime? VendorMaintenanceEndDate { get; set; }

        [Column("vendor_maintenance_count")]
        [Display(Name = "Number of Free Maintenances")]
        public int? VendorMaintenanceCount { get; set; }

        [Column("vendor_maintenance_completed")]
        [Display(Name = "Completed Vendor Maintenances")]
        public int VendorMaintenanceCompletedCount { get; set; } = 0;

        [Column("vendor_maintenance_visits")]
        [Display(Name = "Recorded Vendor Visits (JSON)")]
        public string? VendorMaintenanceVisits { get; set; } = "[]";

        [NotMapped]
        public List<VendorVisitItem> VendorVisitsList
        {
            get
            {
                if (string.IsNullOrWhiteSpace(VendorMaintenanceVisits)) return new List<VendorVisitItem>();
                try
                {
                    return JsonSerializer.Deserialize<List<VendorVisitItem>>(VendorMaintenanceVisits,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<VendorVisitItem>();
                }
                catch
                {
                    return new List<VendorVisitItem>();
                }
            }
            set
            {
                VendorMaintenanceVisits = JsonSerializer.Serialize(value ?? new List<VendorVisitItem>());
            }
        }

        [Column("last_calibration_data")]
        [Display(Name = "Last Calibration Protocol Data (JSON)")]
        public string? LastCalibrationData { get; set; }

        [Column("last_checklist_date")]
        [Display(Name = "Last Checklist Date")]
        public DateTime? LastChecklistDate { get; set; }

        [Column("last_checklist_by")]
        [StringLength(255)]
        [Display(Name = "Last Checklist Completed By")]
        public string? LastChecklistBy { get; set; }

        public virtual ICollection<AssetCompatibleSparePart> CompatibleSpareParts { get; set; } = new List<AssetCompatibleSparePart>();
        public virtual ICollection<AssetHistory> History { get; set; } = new List<AssetHistory>();
        public virtual ICollection<AssetMaintenance> MaintenanceRecords { get; set; } = new List<AssetMaintenance>();
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }

    [Table("asset_history")]
    public class AssetHistory
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("assetID")]
        public int AssetId { get; set; }

        [ForeignKey("AssetId")]
        public virtual Asset? Asset { get; set; }

        [Column("userID")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Column("roomID")]
        public int RoomId { get; set; }

        [ForeignKey("RoomId")]
        public virtual Room? Room { get; set; }

        [Column("CustodyType")]
        public int CustodyTypeId { get; set; }

        [ForeignKey("CustodyTypeId")]
        public virtual CustodyType? CustodyType { get; set; }

        [Column("assignedOn")]
        public DateTime AssignedOn { get; set; } = DateTime.Now;

        [Column("returnedOn")]
        public DateTime? ReturnedOn { get; set; }

        [Column("notes")]
        [StringLength(255)]
        public string? Notes { get; set; }

        [Column("timeStamp")]
        public DateTime TimeStamp { get; set; } = DateTime.Now;
    }

    [Table("tickets")]
    public class Ticket
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("assetID")]
        public int? AssetId { get; set; }

        [ForeignKey("AssetId")]
        public virtual Asset? Asset { get; set; }

        [Column("openedBy")]
        public int? OpenedBy { get; set; }

        [ForeignKey("OpenedBy")]
        public virtual User? OpenedByUser { get; set; }

        [Column("onBehalfOf")]
        public int? OnBehalfOf { get; set; }

        [ForeignKey("OnBehalfOf")]
        public virtual User? OnBehalfOfUser { get; set; }

        [Column("type")]
        [StringLength(255)]
        [Display(Name = "Issue Category")]
        public string? Type { get; set; } = "Hardware"; // Hardware / Software / Network / Maintenance

        [Column("status")]
        public int? StatusId { get; set; } = 1; // 1: opened, 2: pending, 3: delayed, 4: canceled, 5: closed

        [ForeignKey("StatusId")]
        public virtual TicketStatus? Status { get; set; }

        [Column("priority")]
        [Display(Name = "Priority Level")]
        public int? Priority { get; set; } = 2; // 1: Low, 2: Medium, 3: High, 4: Urgent

        [Column("openedDT")]
        [Display(Name = "Date Opened")]
        public DateTime? OpenedDT { get; set; } = DateTime.Now;

        [Column("closedDT")]
        [Display(Name = "Date Closed")]
        public DateTime? ClosedDT { get; set; }

        [Column("target_team")]
        [StringLength(50)]
        [Display(Name = "Target Maintenance Team")]
        public string? TargetTeam { get; set; }

        [Column("resolution_details")]
        [StringLength(1024)]
        [Display(Name = "Resolution Details")]
        public string? ResolutionDetails { get; set; }

        [Column("title_ar")]
        [StringLength(255)]
        public string? TitleAr { get; set; }

        [Column("affected_software")]
        [StringLength(255)]
        [Display(Name = "Affected Software / Application")]
        public string? AffectedSoftware { get; set; }

        [Column("pc_name")]
        [StringLength(255)]
        [Display(Name = "Client PC Name")]
        public string? PcName { get; set; }

        [Column("ip_address")]
        [StringLength(100)]
        [Display(Name = "Client IP Address")]
        public string? IpAddress { get; set; }

        [Column("mac_address")]
        [StringLength(100)]
        [Display(Name = "Client MAC Address")]
        public string? MacAddress { get; set; }

        [Column("closed_by_users")]
        [Display(Name = "Technicians Who Resolved / Closed")]
        public string? ClosedByUsers { get; set; } = "[]";

        [NotMapped]
        public List<int> ClosedByUserIds
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ClosedByUsers)) return new List<int>();
                try
                {
                    return JsonSerializer.Deserialize<List<int>>(ClosedByUsers) ?? new List<int>();
                }
                catch
                {
                    return new List<int>();
                }
            }
            set
            {
                ClosedByUsers = JsonSerializer.Serialize(value ?? new List<int>());
            }
        }

        [NotMapped]
        public int? CreatedBy => OpenedBy;

        public virtual TicketHandler? Handler { get; set; }
        public virtual ICollection<TicketMsg> Messages { get; set; } = new List<TicketMsg>();
        public virtual ICollection<AssetMaintenance> MaintenanceLogs { get; set; } = new List<AssetMaintenance>();
        public virtual ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();

        [NotMapped]
        public string PriorityName => Priority switch
        {
            1 => "Low",
            2 => "Medium",
            3 => "High",
            4 => "Urgent",
            _ => "Medium"
        };

        [NotMapped]
        public string DisplayPriority => CultureInfo.CurrentUICulture.Name.StartsWith("ar")
            ? Priority switch { 1 => "منخفضة", 2 => "متوسطة", 3 => "مرتفعة", 4 => "طارئة جداً", _ => "متوسطة" }
            : PriorityName;

        [NotMapped]
        public string DisplayType => CultureInfo.CurrentUICulture.Name.StartsWith("ar")
            ? LocHelper.TranslateTicketType(Type)
            : (Type ?? "Hardware");

        [NotMapped]
        public string DisplayTargetTeam => CultureInfo.CurrentUICulture.Name.StartsWith("ar")
            ? LocHelper.TranslateTargetTeam(TargetTeam)
            : (TargetTeam ?? "IT");
    }

    [Table("ticket_handler")]
    public class TicketHandler
    {
        [Key]
        [Column("ticketID")]
        public int TicketId { get; set; }

        [ForeignKey("TicketId")]
        public virtual Ticket? Ticket { get; set; }

        [Column("techUserID")]
        public int? TechUserId { get; set; }

        [ForeignKey("TechUserId")]
        public virtual User? TechUser { get; set; }

        [Column("assignedDT")]
        public DateTime? AssignedDT { get; set; } = DateTime.Now;
    }

    [Table("ticket_msg")]
    public class TicketMsg
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("ticketID")]
        public int? TicketId { get; set; }

        [ForeignKey("TicketId")]
        public virtual Ticket? Ticket { get; set; }

        [Column("userID")]
        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Column("msg")]
        [StringLength(1023)]
        public string? Msg { get; set; }

        [Column("timeStamp")]
        public DateTime? TimeStamp { get; set; } = DateTime.Now;

        [Column("ip_address")]
        [StringLength(100)]
        public string? IpAddress { get; set; }

        [Column("mac_address")]
        [StringLength(100)]
        public string? MacAddress { get; set; }

        public virtual ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    }

    [Table("asset_maintenance")]
    public class AssetMaintenance
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("assetID")]
        public int? AssetId { get; set; }

        [ForeignKey("AssetId")]
        public virtual Asset? Asset { get; set; }

        [Column("ticketID")]
        public int? TicketId { get; set; }

        [ForeignKey("TicketId")]
        public virtual Ticket? Ticket { get; set; }

        [Column("maintenanceType")]
        [Display(Name = "Maintenance Type")]
        public int? MaintenanceType { get; set; } // 1: Corrective / Repair, 2: Periodic / Preventive

        [Column("userID")]
        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? Technician { get; set; }

        [Column("supplierID")]
        public int? SupplierId { get; set; }

        [ForeignKey("SupplierId")]
        public virtual Supplier? Supplier { get; set; }

        [Column("description")]
        [StringLength(1024)]
        public string? Description { get; set; }

        [Column("partsReplaced")]
        public string? PartsReplaced { get; set; }

        [Column("timStamp")]
        public DateTime? TimStamp { get; set; } = DateTime.Now;

        [Column("workOrderStatus")]
        [StringLength(50)]
        [Display(Name = "Work Order Status")]
        public string? WorkOrderStatus { get; set; } = "Completed"; // "Under Maintenance" or "Completed"

        [Column("sentDate")]
        [Display(Name = "Sent to Maintenance")]
        public DateTime? SentDate { get; set; }

        [Column("returnDate")]
        [Display(Name = "Returned from Maintenance")]
        public DateTime? ReturnDate { get; set; }

        [Column("actionTaken")]
        [StringLength(1024)]
        [Display(Name = "Work Summary / Actions Taken")]
        public string? ActionTaken { get; set; }

        [Column("cost")]
        [DataType(DataType.Currency)]
        [Display(Name = "Cost ($)")]
        public decimal? Cost { get; set; }

        [Column("previousUserId")]
        public int? PreviousUserId { get; set; }

        [ForeignKey("PreviousUserId")]
        public virtual User? PreviousUser { get; set; }

        [Column("previousRoomId")]
        public int? PreviousRoomId { get; set; }

        [ForeignKey("PreviousRoomId")]
        public virtual Room? PreviousRoom { get; set; }

        [NotMapped]
        public string TypeName => MaintenanceType switch
        {
            2 => "Preventive / Periodic",
            3 => "Supplier Warranty Repair",
            4 => "External Overhaul / Calibration",
            _ => "Breakdown / Repair"
        };

        [NotMapped]
        public string DisplayTypeName => CultureInfo.CurrentUICulture.Name.StartsWith("ar")
            ? MaintenanceType switch
            {
                2 => "صيانة وقائية / دورية",
                3 => "إصلاح تحت ضمان المورد",
                4 => "معايرة وفحص خارجي",
                _ => "إصلاح أعطال طارئة"
            }
            : TypeName;

        [NotMapped]
        public bool IsUnderMaintenance => string.Equals(WorkOrderStatus, "Under Maintenance", StringComparison.OrdinalIgnoreCase);

        [NotMapped]
        public int DaysInMaintenance => SentDate.HasValue ? Math.Max(0, (int)(DateTime.Now.Date - SentDate.Value.Date).TotalDays) : 0;

        [NotMapped]
        public List<ReplacedPartItem> ParsedParts
        {
            get
            {
                if (string.IsNullOrWhiteSpace(PartsReplaced)) return new List<ReplacedPartItem>();
                try
                {
                    return JsonSerializer.Deserialize<List<ReplacedPartItem>>(PartsReplaced, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ReplacedPartItem>();
                }
                catch
                {
                    return new List<ReplacedPartItem> { new ReplacedPartItem { PartName = PartsReplaced } };
                }
            }
        }
    }

    public class ReplacedPartItem
    {
        public string? PartName { get; set; }
        public string? PartNumber { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal? Cost { get; set; }
        public bool UnderWarranty { get; set; }
        public string? SerialNo { get; set; }
        public string? Notes { get; set; }
    }

    public static class TicketStatusConstants
    {
        public const int Opened = 1;
        public const int Pending = 2;
        public const int Delayed = 3;
        public const int Canceled = 4;
        public const int Closed = 5;
        public const int Maintenance = 6;
    }

    [Table("device_forms")]
    public class DeviceForm
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("name")]
        [Required]
        [StringLength(255)]
        [Display(Name = "Device Type / Template Name")]
        public string Name { get; set; } = string.Empty;

        [Column("department")]
        [Required]
        [StringLength(50)]
        [Display(Name = "Maintenance Department")]
        public string Department { get; set; } = MaintenanceTeams.IT;

        [Column("form")]
        [Required]
        [Display(Name = "Form Field Schema (JSON)")]
        public string Form { get; set; } = "[]";

        [Column("has_software")]
        [Display(Name = "Softwares Installable on this Asset")]
        public bool HasSoftware { get; set; } = false;

        [Column("installable_softwares")]
        [Display(Name = "Installable Softwares (JSON)")]
        public string? InstallableSoftwares { get; set; } = "[]";

        [NotMapped]
        public List<string> InstallableSoftwaresList
        {
            get
            {
                if (string.IsNullOrWhiteSpace(InstallableSoftwares)) return new List<string>();
                try
                {
                    return JsonSerializer.Deserialize<List<string>>(InstallableSoftwares,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
            set
            {
                InstallableSoftwares = JsonSerializer.Serialize(value ?? new List<string>());
            }
        }

        [NotMapped]
        public List<FormFieldDefinition> Fields
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Form)) return new List<FormFieldDefinition>();
                try
                {
                    return JsonSerializer.Deserialize<List<FormFieldDefinition>>(Form, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<FormFieldDefinition>();
                }
                catch
                {
                    return new List<FormFieldDefinition>();
                }
            }
            set
            {
                Form = JsonSerializer.Serialize(value);
            }
        }
    }

    public class FormFieldDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = "text"; // "text", "number", "select", "textarea"
        public List<string> Options { get; set; } = new List<string>();
        public bool Required { get; set; } = false;
        public string? Placeholder { get; set; }
    }

    [Table("notifications")]
    public class Notification
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("userID")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Column("ticketID")]
        public int? TicketId { get; set; }

        [ForeignKey("TicketId")]
        public virtual Ticket? Ticket { get; set; }

        [Column("title")]
        [StringLength(255)]
        [Required]
        public string Title { get; set; } = string.Empty;

        [Column("message")]
        [StringLength(1024)]
        [Required]
        public string Message { get; set; } = string.Empty;

        [Column("type")]
        [StringLength(50)]
        public string Type { get; set; } = "TicketUpdate"; // TicketOpened, MessageAdded, StatusChanged

        [Column("linkUrl")]
        [StringLength(255)]
        public string? LinkUrl { get; set; }

        [Column("isRead")]
        public bool IsRead { get; set; } = false;

        [Column("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    [Table("software_catalog")]
    public class SoftwareCatalogItem
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("name")]
        [StringLength(255)]
        [Display(Name = "Software Name")]
        public string Name { get; set; } = string.Empty;

        [Column("category")]
        [StringLength(100)]
        [Display(Name = "Category")]
        public string? Category { get; set; } = "General"; // Clinical, Imaging, Laboratory, Office, Security, System

        [Column("vendor")]
        [StringLength(255)]
        [Display(Name = "Vendor / Developer")]
        public string? Vendor { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }

    [Table("ticket_attachments")]
    public class TicketAttachment
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("ticketID")]
        public int TicketId { get; set; }

        [ForeignKey("TicketId")]
        public virtual Ticket? Ticket { get; set; }

        [Column("messageID")]
        public int? MessageId { get; set; }

        [ForeignKey("MessageId")]
        public virtual TicketMsg? Message { get; set; }

        [Column("fileName")]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Column("filePath")]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Column("fileType")]
        [StringLength(50)]
        public string? FileType { get; set; }

        [Column("fileSize")]
        public long? FileSize { get; set; }

        [Column("uploadedDT")]
        public DateTime UploadedDT { get; set; } = DateTime.Now;

        [Column("uploadedBy")]
        public int? UploadedBy { get; set; }

        [ForeignKey("UploadedBy")]
        public virtual User? UploadedByUser { get; set; }
    }

    [Table("spare_parts")]
    public class SparePart
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("name")]
        [Required]
        [StringLength(255)]
        [Display(Name = "Part / Item Name")]
        public string Name { get; set; } = string.Empty;

        [Column("name_ar")]
        [StringLength(255)]
        [Display(Name = "Part / Item Name (Arabic)")]
        public string? NameAr { get; set; }

        [Column("part_number")]
        [StringLength(100)]
        [Display(Name = "Part Number / SKU / Code")]
        public string? PartNumber { get; set; }

        [Column("department")]
        [Required]
        [StringLength(50)]
        [Display(Name = "Maintenance Department")]
        public string Department { get; set; } = MaintenanceTeams.IT; // IT, BIOMEDICAL, GENERAL

        [Column("category")]
        [StringLength(50)]
        [Display(Name = "Stock Category")]
        public string Category { get; set; } = "Spare Part"; // "Spare Part", "Consumable", "Periodic Maintenance Reserve"

        [Column("quantity")]
        [Display(Name = "Quantity in Stock")]
        public int Quantity { get; set; } = 0;

        [Column("min_reserve_quantity")]
        [Display(Name = "Minimum Reserve Level")]
        public int MinReserveQuantity { get; set; } = 2;

        [Column("unit")]
        [StringLength(50)]
        [Display(Name = "Unit of Measure")]
        public string? Unit { get; set; } = "Piece"; // Piece, Box, Meter, Liter, Roll, etc.

        [Column("unit_cost", TypeName = "decimal(18,2)")]
        [Display(Name = "Unit Cost")]
        public decimal? UnitCost { get; set; }

        [Column("storage_location")]
        [StringLength(255)]
        [Display(Name = "Shelf / Storage Location")]
        public string? StorageLocation { get; set; }

        [Column("can_function_as_asset")]
        [Display(Name = "Can Function as Independent Asset")]
        public bool CanFunctionAsAsset { get; set; } = false; // e.g. SSD (spare part or backup external disk), RAM, Monitor, etc.

        [Column("compatible_equipment")]
        [StringLength(500)]
        [Display(Name = "Compatible Devices / Equipment")]
        public string? CompatibleEquipment { get; set; }

        [Column("notes")]
        [StringLength(1024)]
        public string? Notes { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string DisplayName => (CultureInfo.CurrentUICulture.Name.StartsWith("ar") && !string.IsNullOrWhiteSpace(NameAr)) ? NameAr : Name;

        [NotMapped]
        public bool IsLowStock => Quantity <= MinReserveQuantity;

        [NotMapped]
        public string DisplayCategory => CultureInfo.CurrentUICulture.Name.StartsWith("ar")
            ? Category switch
            {
                "Spare Part" => "قطعة غيار",
                "Consumable" => "مستهلكات",
                "Periodic Maintenance Reserve" => "مخزون صيانة دورية",
                _ => Category
            }
            : Category;

        [Column("form_id")]
        public int? FormId { get; set; }

        [ForeignKey("FormId")]
        public virtual SparePartForm? SparePartForm { get; set; }

        [Column("specs")]
        [Display(Name = "Technical Specifications (JSON)")]
        public string? Specs { get; set; } = "{}";

        [NotMapped]
        public Dictionary<string, string> SpecsDictionary
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Specs)) return new Dictionary<string, string>();
                try
                {
                    return JsonSerializer.Deserialize<Dictionary<string, string>>(Specs,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new Dictionary<string, string>();
                }
                catch
                {
                    return new Dictionary<string, string>();
                }
            }
            set
            {
                Specs = JsonSerializer.Serialize(value ?? new Dictionary<string, string>());
            }
        }

        public virtual ICollection<AssetCompatibleSparePart> CompatibleAssets { get; set; } = new List<AssetCompatibleSparePart>();
        public virtual ICollection<SparePartTransaction> Transactions { get; set; } = new List<SparePartTransaction>();
    }

    [Table("spare_part_transactions")]
    public class SparePartTransaction
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("spare_part_id")]
        public int SparePartId { get; set; }

        [ForeignKey("SparePartId")]
        public virtual SparePart? SparePart { get; set; }

        [Column("ticket_id")]
        public int? TicketId { get; set; }

        [ForeignKey("TicketId")]
        public virtual Ticket? Ticket { get; set; }

        [Column("asset_id")]
        public int? AssetId { get; set; }

        [ForeignKey("AssetId")]
        public virtual Asset? Asset { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Column("transaction_type")]
        [StringLength(50)]
        [Display(Name = "Transaction Type")]
        public string TransactionType { get; set; } = "Usage in Ticket"; // "Usage in Ticket", "Replenishment", "Commissioned as Asset", "Returned to Reserve", "Inventory Adjustment"

        [Column("quantity_changed")]
        [Display(Name = "Quantity Changed")]
        public int QuantityChanged { get; set; }

        [Column("quantity_after")]
        [Display(Name = "Stock After")]
        public int QuantityAfter { get; set; }

        [Column("notes")]
        [StringLength(1024)]
        public string? Notes { get; set; }

        [Column("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [NotMapped]
        public string DisplayTransactionType => CultureInfo.CurrentUICulture.Name.StartsWith("ar")
            ? TransactionType switch
            {
                "Usage in Ticket" => "استخدام في تذكرة",
                "Replenishment" => "توريد وإعادة تزويد",
                "Commissioned as Asset" => "تحويل إلى أصل مستقل",
                "Returned to Reserve" => "استرجاع إلى مخزون الاحتياط",
                "Inventory Adjustment" => "تسوية جردية",
                _ => TransactionType
            }
            : TransactionType;
    }

    [Table("asset_categories")]
    public class AssetCategory
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("name")]
        [Required]
        [StringLength(255)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [Column("name_ar")]
        [StringLength(255)]
        [Display(Name = "Category Name (Arabic)")]
        public string? NameAr { get; set; }

        [Column("department")]
        [Required]
        [StringLength(50)]
        [Display(Name = "Department")]
        public string Department { get; set; } = MaintenanceTeams.IT;

        [Column("description")]
        [StringLength(1024)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string DisplayName => (CultureInfo.CurrentUICulture.Name.StartsWith("ar") && !string.IsNullOrWhiteSpace(NameAr)) ? NameAr : Name;

        public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
        public virtual ICollection<ChecklistForm> Checklists { get; set; } = new List<ChecklistForm>();
        public virtual ICollection<CalibrationForm> CalibrationForms { get; set; } = new List<CalibrationForm>();
        public virtual ICollection<IssueCategory> IssueCategories { get; set; } = new List<IssueCategory>();
    }

    [Table("issue_categories")]
    public class IssueCategory
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("name")]
        [Required]
        [StringLength(255)]
        [Display(Name = "Issue Name")]
        public string Name { get; set; } = string.Empty;

        [Column("name_ar")]
        [StringLength(255)]
        [Display(Name = "Issue Name (Arabic)")]
        public string? NameAr { get; set; }

        [Column("department")]
        [Required]
        [StringLength(50)]
        [Display(Name = "Department")]
        public string Department { get; set; } = MaintenanceTeams.IT;

        [Column("description")]
        [StringLength(1024)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string DisplayName => (CultureInfo.CurrentUICulture.Name.StartsWith("ar") && !string.IsNullOrWhiteSpace(NameAr)) ? NameAr : Name;

        public virtual ICollection<AssetCategory> AssetCategories { get; set; } = new List<AssetCategory>();
    }

    [Table("spare_part_forms")]
    public class SparePartForm
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("name")]
        [Required]
        [StringLength(255)]
        [Display(Name = "Spare Part Form Name")]
        public string Name { get; set; } = string.Empty;

        [Column("department")]
        [Required]
        [StringLength(50)]
        [Display(Name = "Maintenance Department")]
        public string Department { get; set; } = MaintenanceTeams.IT;

        [Column("form")]
        [Required]
        [Display(Name = "Form Field Schema (JSON)")]
        public string Form { get; set; } = "[]";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public List<FormFieldDefinition> Fields
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Form)) return new List<FormFieldDefinition>();
                try
                {
                    return JsonSerializer.Deserialize<List<FormFieldDefinition>>(Form,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<FormFieldDefinition>();
                }
                catch
                {
                    return new List<FormFieldDefinition>();
                }
            }
            set
            {
                Form = JsonSerializer.Serialize(value ?? new List<FormFieldDefinition>());
            }
        }

        public virtual ICollection<SparePart> SpareParts { get; set; } = new List<SparePart>();
    }

    [Table("asset_compatible_spare_parts")]
    public class AssetCompatibleSparePart
    {
        [Column("asset_id")]
        public int AssetId { get; set; }

        [ForeignKey("AssetId")]
        public virtual Asset? Asset { get; set; }

        [Column("spare_part_id")]
        public int SparePartId { get; set; }

        [ForeignKey("SparePartId")]
        public virtual SparePart? SparePart { get; set; }

        [Column("assigned_at")]
        public DateTime AssignedAt { get; set; } = DateTime.Now;
    }

    [Table("calibration_forms")]
    public class CalibrationForm
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("name")]
        [Required]
        [StringLength(255)]
        [Display(Name = "Calibration Protocol Name")]
        public string Name { get; set; } = string.Empty;

        [Column("department")]
        [Required]
        [StringLength(50)]
        [Display(Name = "Department")]
        public string Department { get; set; } = MaintenanceTeams.Biomedical;

        [Column("category_id")]
        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual AssetCategory? Category { get; set; }

        [Column("form")]
        [Required]
        [Display(Name = "Protocol Schema (JSON)")]
        public string Form { get; set; } = "[]";

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public List<FormFieldDefinition> Fields
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Form)) return new List<FormFieldDefinition>();
                try
                {
                    return JsonSerializer.Deserialize<List<FormFieldDefinition>>(Form,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<FormFieldDefinition>();
                }
                catch
                {
                    return new List<FormFieldDefinition>();
                }
            }
            set
            {
                Form = JsonSerializer.Serialize(value ?? new List<FormFieldDefinition>());
            }
        }
    }

    [Table("checklist_forms")]
    public class ChecklistForm
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("title")]
        [Required]
        [StringLength(255)]
        [Display(Name = "Checklist Form Title")]
        public string Title { get; set; } = string.Empty;

        [Column("department")]
        [Required]
        [StringLength(50)]
        [Display(Name = "Department")]
        public string Department { get; set; } = MaintenanceTeams.IT;

        [Column("category_id")]
        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual AssetCategory? Category { get; set; }

        [Column("checklist_type")]
        [StringLength(50)]
        [Display(Name = "Checklist Scope")]
        public string ChecklistType { get; set; } = "Periodic Maintenance"; // "Calibration", "Periodic Maintenance", "Safety Inspection"

        [Column("items_json")]
        [Display(Name = "Checklist Action Items (JSON)")]
        public string ItemsJson { get; set; } = "[]";

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public List<string> Items
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ItemsJson)) return new List<string>();
                try
                {
                    return JsonSerializer.Deserialize<List<string>>(ItemsJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
            set
            {
                ItemsJson = JsonSerializer.Serialize(value ?? new List<string>());
            }
        }
    }

    public class VendorVisitItem
    {
        public int VisitNumber { get; set; }
        public DateTime VisitDate { get; set; } = DateTime.Now;
        public string? EngineerName { get; set; }
        public string? Notes { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.Now;
    }
}

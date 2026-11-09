using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using StockPortal.Models;

namespace StockPortal.Data
{
    public static class DbInitializer
    {
        public static void Initialize(StockDbContext context)
        {
            context.Database.EnsureCreated();

            // Ensure Arabic columns exist before any LINQ queries on entities execute
            EnsureArabicSchema(context);

            // Ensure notifications table exists
            try
            {
                var isSqlite = context.Database.ProviderName?.Contains("Sqlite") == true;
                if (isSqlite)
                {
                    context.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS notifications (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            userID INTEGER NOT NULL,
                            ticketID INTEGER NULL,
                            title TEXT NOT NULL,
                            message TEXT NOT NULL,
                            type TEXT NOT NULL,
                            linkUrl TEXT NULL,
                            isRead INTEGER NOT NULL DEFAULT 0,
                            createdAt TEXT NOT NULL
                        );");
                }
                else
                {
                    context.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS `notifications` (
                            `id` int NOT NULL AUTO_INCREMENT,
                            `userID` int NOT NULL,
                            `ticketID` int NULL,
                            `title` varchar(255) NOT NULL,
                            `message` varchar(1024) NOT NULL,
                            `type` varchar(50) NOT NULL,
                            `linkUrl` varchar(255) NULL,
                            `isRead` tinyint(1) NOT NULL DEFAULT 0,
                            `createdAt` datetime NOT NULL,
                            PRIMARY KEY (`id`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
                }
            }
            catch {}

            // Ensure Ticket Statuses
            if (!context.TicketStatuses.Any())
            {
                context.TicketStatuses.AddRange(
                    new TicketStatus { Id = 1, Status = "opened" },
                    new TicketStatus { Id = 2, Status = "pending" },
                    new TicketStatus { Id = 3, Status = "delayed" },
                    new TicketStatus { Id = 4, Status = "canceled" },
                    new TicketStatus { Id = 5, Status = "closed" },
                    new TicketStatus { Id = 6, Status = "maintenance" }
                );
                context.SaveChanges();
            }
            else if (!context.TicketStatuses.Any(s => s.Status != null && s.Status.ToLower() == "maintenance"))
            {
                context.TicketStatuses.Add(new TicketStatus { Id = 6, Status = "maintenance" });
                context.SaveChanges();
            }

            // Ensure Custody Types
            if (!context.CustodyTypes.Any())
            {
                context.CustodyTypes.AddRange(
                    new CustodyType { Id = 1, Type = "Personal" },
                    new CustodyType { Id = 2, Type = "Shared" },
                    new CustodyType { Id = 3, Type = "Supplier / External Maintenance" }
                );
                context.SaveChanges();
            }
            else if (!context.CustodyTypes.Any(c => c.Id == 3))
            {
                context.CustodyTypes.Add(new CustodyType { Id = 3, Type = "Supplier / External Maintenance" });
                context.SaveChanges();
            }
            else
            {
                var cust3 = context.CustodyTypes.Find(3);
                if (cust3 != null && string.IsNullOrEmpty(cust3.Type))
                {
                    cust3.Type = "Supplier / External Maintenance";
                    context.SaveChanges();
                }
            }

            // Ensure departments column in suppliers table
            try
            {
                var isSqlite = context.Database.ProviderName?.Contains("Sqlite") == true;
                if (isSqlite)
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE suppliers ADD COLUMN departments TEXT NULL;");
                }
                else
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE suppliers ADD COLUMN IF NOT EXISTS departments LONGTEXT NULL;");
                }
            }
            catch {}

            // Seed Departments if empty
            if (!context.Departments.Any())
            {
                var depts = new[]
                {
                    new Department { Name = "Information Technology" },
                    new Department { Name = "Radiology & Imaging" },
                    new Department { Name = "Cardiology" },
                    new Department { Name = "Biomedical Engineering" },
                    new Department { Name = "Hospital Administration" }
                };
                context.Departments.AddRange(depts);
                context.SaveChanges();
            }

            // Seed Rooms if empty
            if (!context.Rooms.Any())
            {
                var rooms = new[]
                {
                    new Room { RoomNo = 101, Floor = 1 },
                    new Room { RoomNo = 102, Floor = 1 },
                    new Room { RoomNo = 105, Floor = 1 }, // Server Room
                    new Room { RoomNo = 201, Floor = 2 }, // ICU
                    new Room { RoomNo = 301, Floor = 3 }  // Radiology
                };
                context.Rooms.AddRange(rooms);
                context.SaveChanges();
            }

            // Seed Suppliers if empty
            if (!context.Suppliers.Any())
            {
                var suppliers = new[]
                {
                    new Supplier { Name = "Dell Technologies", IsActive = true, Type = 3, Departments = "[\"IT\"]" },
                    new Supplier { Name = "Philips Healthcare", IsActive = true, Type = 3, Departments = "[\"BIOMEDICAL\"]" },
                    new Supplier { Name = "HP Enterprise", IsActive = true, Type = 1, Departments = "[\"IT\"]" },
                    new Supplier { Name = "Cisco Systems", IsActive = true, Type = 3, Departments = "[\"IT\"]" },
                    new Supplier { Name = "GE Healthcare", IsActive = true, Type = 3, Departments = "[\"BIOMEDICAL\"]" },
                    new Supplier { Name = "Carrier & Facility Engineering", IsActive = true, Type = 3, Departments = "[\"GENERAL\"]" }
                };
                context.Suppliers.AddRange(suppliers);
                context.SaveChanges();
            }

            // Seed Users if empty
            string currentPcUser = string.IsNullOrWhiteSpace(Environment.UserName) ? "Abbas" : Environment.UserName;

            if (!context.Users.Any())
            {
                var itDept = context.Departments.FirstOrDefault(d => d.Name == "Information Technology");
                var radDept = context.Departments.FirstOrDefault(d => d.Name == "Radiology & Imaging");
                var cardDept = context.Departments.FirstOrDefault(d => d.Name == "Cardiology");
                var bioDept = context.Departments.FirstOrDefault(d => d.Name == "Biomedical Engineering");

                var users = new[]
                {
                    new User { FullName = "Abbas Al-Amiri", DomainUser = currentPcUser, Role = SystemRoles.Admin, DepartmentId = itDept?.Id, IsActive = true },
                    new User { FullName = "Ahmed Mansour", DomainUser = "a.mansour", Role = "Field Support Technician", DepartmentId = itDept?.Id, IsActive = true },
                    new User { FullName = "Dr. Sarah Jenkins", DomainUser = "s.jenkins", Role = "Lead Cardiologist", DepartmentId = cardDept?.Id, IsActive = true },
                    new User { FullName = "Dr. Elena Rostova", DomainUser = "e.rostova", Role = "Radiology Consultant", DepartmentId = radDept?.Id, IsActive = true },
                    new User { FullName = "Tariq Ziad", DomainUser = "t.ziad", Role = "Biomedical Equipment Engineer", DepartmentId = bioDept?.Id, IsActive = true },
                    new User { FullName = "Former Staff", DomainUser = "inactive.user", Role = "Inactive Account", DepartmentId = itDept?.Id, IsActive = false }
                };
                context.Users.AddRange(users);
                context.SaveChanges();

                if (itDept != null && users.Length > 0)
                {
                    itDept.HeadUserId = users[0].Id;
                    context.SaveChanges();
                }
            }

            // Seed Assets if empty
            if (!context.Assets.Any())
            {
                var users = context.Users.ToList();
                var rooms = context.Rooms.ToList();
                var suppliers = context.Suppliers.ToList();
                var custodyTypes = context.CustodyTypes.ToList();

                var abbas = users.FirstOrDefault(u => u.DomainUser == "a.amiri") ?? users.First();
                var sarah = users.FirstOrDefault(u => u.DomainUser == "s.jenkins") ?? users.First();
                var elena = users.FirstOrDefault(u => u.DomainUser == "e.rostova") ?? users.First();

                var srvRoom = rooms.FirstOrDefault(r => r.RoomNo == 105) ?? rooms[0];
                var clinicRoom = rooms.FirstOrDefault(r => r.RoomNo == 101) ?? rooms[0];
                var radRoom = rooms.FirstOrDefault(r => r.RoomNo == 301) ?? rooms[0];

                var dell = suppliers.FirstOrDefault(s => s.Name != null && s.Name.Contains("Dell")) ?? suppliers[0];
                var philips = suppliers.FirstOrDefault(s => s.Name != null && s.Name.Contains("Philips")) ?? suppliers[0];
                var hp = suppliers.FirstOrDefault(s => s.Name != null && s.Name.Contains("HP")) ?? suppliers[0];
                var cisco = suppliers.FirstOrDefault(s => s.Name != null && s.Name.Contains("Cisco")) ?? suppliers[0];

                var personal = custodyTypes.FirstOrDefault(c => c.Type == "Personal")?.Id ?? 1;
                var shared = custodyTypes.FirstOrDefault(c => c.Type == "Shared")?.Id ?? 2;

                var assetList = new[]
                {
                    new Asset
                    {
                        HospitalNo = "HOSP-SRV-001",
                        SerialNo = "SN-DELL-88219-R750",
                        Brand = "Dell EMC",
                        ModelNo = 750,
                        Price = 14500.00m,
                        Type = "Rack Server",
                        Status = "In Service",
                        SupplierId = dell.Id,
                        RoomId = srvRoom.Id,
                        UserId = abbas.Id,
                        CustodyTypeId = shared,
                        Specs = "{\"CPU\": \"2x Intel Xeon Gold 6330\", \"RAM\": \"128GB DDR4 ECC\", \"Storage\": \"8x 1.92TB SAS SSD RAID 10\"}",
                        BuyingDate = DateTime.Now.AddMonths(-14),
                        WarrantyTill = DateTime.Now.AddMonths(22),
                        NeedsPeriodic = true,
                        PeriodicInterval = 90,
                        LastPeriodic = DateTime.Now.AddDays(-75),
                        NextPeriodic = DateTime.Now.AddDays(15),
                        MaintenanceTeam = MaintenanceTeams.IT
                    },
                    new Asset
                    {
                        HospitalNo = "HOSP-US-004",
                        SerialNo = "SN-PH-99231-EPIQ7",
                        Brand = "Philips Healthcare",
                        ModelNo = 7,
                        Price = 48000.00m,
                        Type = "Ultrasound Scanner",
                        Status = "In Service",
                        SupplierId = philips.Id,
                        RoomId = radRoom.Id,
                        UserId = elena.Id,
                        CustodyTypeId = shared,
                        Specs = "{\"Model\": \"EPIQ 7 Diagnostic Ultrasound\", \"Probes\": \"Curved array, Linear, Phased array cardiac\", \"Screen\": \"21.5-inch HD Display\"}",
                        BuyingDate = DateTime.Now.AddMonths(-18),
                        WarrantyTill = DateTime.Now.AddMonths(18),
                        NeedsPeriodic = true,
                        PeriodicInterval = 180,
                        LastPeriodic = DateTime.Now.AddDays(-190), // Overdue!
                        NextPeriodic = DateTime.Now.AddDays(-10),
                        MaintenanceTeam = MaintenanceTeams.Biomedical
                    },
                    new Asset
                    {
                        HospitalNo = "HOSP-LAP-012",
                        SerialNo = "SN-HP-44102-G8",
                        Brand = "HP",
                        ModelNo = 850,
                        Price = 1250.00m,
                        Type = "Doctor Laptop",
                        Status = "Under Maintenance",
                        SupplierId = hp.Id,
                        RoomId = clinicRoom.Id,
                        UserId = sarah.Id,
                        CustodyTypeId = personal,
                        Specs = "{\"Model\": \"EliteBook 850 G8\", \"CPU\": \"Intel Core i7-1165G7\", \"RAM\": \"16GB\", \"SSD\": \"512GB NVMe\"}",
                        BuyingDate = DateTime.Now.AddMonths(-8),
                        WarrantyTill = DateTime.Now.AddMonths(28),
                        NeedsPeriodic = false,
                        MaintenanceTeam = MaintenanceTeams.IT
                    },
                    new Asset
                    {
                        HospitalNo = "HOSP-NET-003",
                        SerialNo = "SN-CS-77119-9300",
                        Brand = "Cisco",
                        ModelNo = 9300,
                        Price = 6200.00m,
                        Type = "Core Network Switch",
                        Status = "In Service",
                        SupplierId = cisco.Id,
                        RoomId = srvRoom.Id,
                        UserId = abbas.Id,
                        CustodyTypeId = shared,
                        Specs = "{\"Ports\": \"48x Gigabit PoE+\", \"Uplinks\": \"4x 10G SFP+\", \"Stacking\": \"StackWise-480\"}",
                        BuyingDate = DateTime.Now.AddMonths(-20),
                        WarrantyTill = DateTime.Now.AddMonths(40),
                        NeedsPeriodic = true,
                        PeriodicInterval = 180,
                        LastPeriodic = DateTime.Now.AddDays(-60),
                        NextPeriodic = DateTime.Now.AddDays(120),
                        MaintenanceTeam = MaintenanceTeams.IT
                    },
                    new Asset
                    {
                        HospitalNo = "HOSP-BIO-001",
                        SerialNo = "SN-GE-CARESCAPE-881",
                        Brand = "GE Healthcare",
                        ModelNo = 800,
                        Price = 12500.00m,
                        Type = "Patient Monitor",
                        Status = "In Service",
                        SupplierId = philips.Id,
                        RoomId = clinicRoom.Id,
                        UserId = sarah.Id,
                        CustodyTypeId = shared,
                        Specs = "{\"Model\": \"Carescape B850 Monitor\", \"Parameters\": \"ECG, SpO2, NIBP, Temp\"}",
                        BuyingDate = DateTime.Now.AddMonths(-12),
                        WarrantyTill = DateTime.Now.AddMonths(12),
                        NeedsPeriodic = true,
                        PeriodicInterval = 180,
                        LastPeriodic = DateTime.Now.AddDays(-60),
                        NextPeriodic = DateTime.Now.AddDays(120),
                        MaintenanceTeam = MaintenanceTeams.Biomedical
                    },
                    new Asset
                    {
                        HospitalNo = "HOSP-GEN-001",
                        SerialNo = "SN-SCH-GALAXY-300",
                        Brand = "Schneider Electric",
                        ModelNo = 300,
                        Price = 28500.00m,
                        Type = "Facility UPS",
                        Status = "In Service",
                        SupplierId = dell.Id,
                        RoomId = srvRoom.Id,
                        UserId = abbas.Id,
                        CustodyTypeId = shared,
                        Specs = "{\"Capacity\": \"40kVA / 36kW\", \"Battery Bank\": \"32x 12V 100Ah\"}",
                        BuyingDate = DateTime.Now.AddMonths(-16),
                        WarrantyTill = DateTime.Now.AddMonths(20),
                        NeedsPeriodic = true,
                        PeriodicInterval = 90,
                        LastPeriodic = DateTime.Now.AddDays(-30),
                        NextPeriodic = DateTime.Now.AddDays(60),
                        MaintenanceTeam = MaintenanceTeams.General
                    }
                };

                context.Assets.AddRange(assetList);
                context.SaveChanges();

                // Add Asset History records
                foreach (var a in assetList)
                {
                    context.AssetHistories.Add(new AssetHistory
                    {
                        AssetId = a.Id,
                        UserId = a.UserId ?? abbas.Id,
                        RoomId = a.RoomId ?? srvRoom.Id,
                        CustodyTypeId = a.CustodyTypeId ?? shared,
                        AssignedOn = a.BuyingDate ?? DateTime.Now.AddMonths(-6),
                        Notes = "Initial commissioning and asset custody tagging"
                    });
                }
                context.SaveChanges();
            }

            // Seed Tickets if empty
            if (!context.Tickets.Any())
            {
                var assets = context.Assets.ToList();
                var users = context.Users.ToList();

                var laptop = assets.FirstOrDefault(a => a.HospitalNo == "HOSP-LAP-012") ?? assets.First();
                var ultrasound = assets.FirstOrDefault(a => a.HospitalNo == "HOSP-US-004") ?? assets.First();
                var server = assets.FirstOrDefault(a => a.HospitalNo == "HOSP-SRV-001") ?? assets.First();

                var sarah = users.FirstOrDefault(u => u.DomainUser == "s.jenkins") ?? users.First();
                var elena = users.FirstOrDefault(u => u.DomainUser == "e.rostova") ?? users.First();
                var ahmed = users.FirstOrDefault(u => u.DomainUser == "a.mansour") ?? users.First();
                var abbas = users.FirstOrDefault(u => u.DomainUser == "a.amiri") ?? users.First();

                var ticket1 = new Ticket
                {
                    AssetId = laptop.Id,
                    OpenedBy = sarah.Id,
                    OnBehalfOf = sarah.Id,
                    Type = "Hardware",
                    StatusId = 2, // pending
                    Priority = 3, // High
                    OpenedDT = DateTime.Now.AddHours(-18)
                };

                var ticket2 = new Ticket
                {
                    AssetId = ultrasound.Id,
                    OpenedBy = elena.Id,
                    OnBehalfOf = elena.Id,
                    Type = "Maintenance",
                    StatusId = 1, // opened
                    Priority = 4, // Urgent
                    OpenedDT = DateTime.Now.AddHours(-4)
                };

                var ticket3 = new Ticket
                {
                    AssetId = server.Id,
                    OpenedBy = abbas.Id,
                    OnBehalfOf = abbas.Id,
                    Type = "Hardware",
                    StatusId = 5, // closed
                    Priority = 2, // Medium
                    OpenedDT = DateTime.Now.AddDays(-10),
                    ClosedDT = DateTime.Now.AddDays(-8)
                };

                context.Tickets.AddRange(ticket1, ticket2, ticket3);
                context.SaveChanges();

                // Assign handlers
                context.TicketHandlers.Add(new TicketHandler
                {
                    TicketId = ticket1.Id,
                    TechUserId = ahmed.Id,
                    AssignedDT = DateTime.Now.AddHours(-16)
                });

                // Add discussion messages
                context.TicketMessages.AddRange(
                    new TicketMsg
                    {
                        TicketId = ticket1.Id,
                        UserId = sarah.Id,
                        Msg = "Laptop displays blue screen (CRITICAL_PROCESS_DIED) shortly after booting into Windows.",
                        TimeStamp = DateTime.Now.AddHours(-18)
                    },
                    new TicketMsg
                    {
                        TicketId = ticket1.Id,
                        UserId = ahmed.Id,
                        Msg = "Hardware diagnostics run. NVMe SSD shows bad SMART sectors. Ordering replacement drive under warranty.",
                        TimeStamp = DateTime.Now.AddHours(-15)
                    },
                    new TicketMsg
                    {
                        TicketId = ticket2.Id,
                        UserId = elena.Id,
                        Msg = "Periodic preventive calibration is overdue by 10 days. Image noise detected on linear probe.",
                        TimeStamp = DateTime.Now.AddHours(-4)
                    }
                );

                // Add maintenance record for closed ticket
                context.AssetMaintenances.Add(new AssetMaintenance
                {
                    AssetId = server.Id,
                    TicketId = ticket3.Id,
                    MaintenanceType = 1, // Breakdown
                    UserId = ahmed.Id,
                    Description = "Replaced faulty hot-swap power supply unit PSU2 in Dell R750 server.",
                    PartsReplaced = "{\"Part\": \"Dell 1400W Platinum Hot-Plug Redundant PSU\", \"PartNumber\": \"DP/N 0XXV21\"}",
                    TimStamp = DateTime.Now.AddDays(-8)
                });

                context.SaveChanges();
            }

            // Ensure Universal Device Forms exist
            if (!context.DeviceForms.Any())
            {
                var templates = new[]
                {
                    // --- IT MAINTENANCE TEMPLATES ---
                    new DeviceForm
                    {
                        Name = "Laptop",
                        Department = MaintenanceTeams.IT,
                        HasSoftware = true,
                        InstallableSoftwares = @"[""Windows 11 Pro"",""Microsoft Office 365"",""Hospital HIS / EHR Client"",""PACS Imaging Viewer"",""Antivirus Agent"",""Google Chrome""]",
                        Form = @"[
                            {""name"":""Processor"",""label"":""Processor / CPU"",""type"":""text"",""required"":true,""placeholder"":""e.g. Intel Core i7-1165G7""},
                            {""name"":""RAM"",""label"":""Installed Memory (RAM)"",""type"":""select"",""options"":[""8 GB"",""16 GB"",""32 GB"",""64 GB""],""required"":true},
                            {""name"":""Storage"",""label"":""Primary Storage (SSD)"",""type"":""text"",""required"":true,""placeholder"":""e.g. 512GB NVMe SSD""},
                            {""name"":""OS"",""label"":""Operating System"",""type"":""select"",""options"":[""Windows 11 Pro"",""Windows 10 Pro"",""Ubuntu Linux"",""macOS""],""required"":true},
                            {""name"":""ScreenSize"",""label"":""Display Size"",""type"":""text"",""required"":false,""placeholder"":""e.g. 15.6 inch FHD""}
                        ]"
                    },
                    new DeviceForm
                    {
                        Name = "Desktop Workstation",
                        Department = MaintenanceTeams.IT,
                        HasSoftware = true,
                        InstallableSoftwares = @"[""Windows 11 Pro"",""Microsoft Office 365"",""Hospital HIS / EHR Client"",""PACS Imaging Viewer"",""Antivirus Agent"",""Google Chrome""]",
                        Form = @"[
                            {""name"":""FormFactor"",""label"":""Chassis / Form Factor"",""type"":""select"",""options"":[""Small Form Factor (SFF)"",""Mini Tower"",""All-in-One (AIO)"",""Micro / Tiny PC""],""required"":true},
                            {""name"":""CPU"",""label"":""Processor (CPU)"",""type"":""text"",""required"":true,""placeholder"":""e.g. Intel Core i5-12500 / Ryzen 5 5600G""},
                            {""name"":""RAM"",""label"":""Installed RAM"",""type"":""select"",""options"":[""8 GB"",""16 GB"",""32 GB"",""64 GB""],""required"":true},
                            {""name"":""Storage"",""label"":""Primary Drive"",""type"":""text"",""required"":true,""placeholder"":""e.g. 256GB SSD + 1TB HDD""},
                            {""name"":""GPU"",""label"":""Graphics Card"",""type"":""text"",""required"":false,""placeholder"":""e.g. Integrated Intel UHD / NVIDIA T600""},
                            {""name"":""OS"",""label"":""Operating System"",""type"":""select"",""options"":[""Windows 11 Pro"",""Windows 10 Pro""],""required"":true}
                        ]"
                    },
                    new DeviceForm
                    {
                        Name = "Core Switch",
                        Department = MaintenanceTeams.IT,
                        Form = @"[
                            {""name"":""Ports"",""label"":""Ethernet Port Count"",""type"":""select"",""options"":[""24-Port Gigabit"",""48-Port Gigabit"",""24-Port Multi-Gig"",""48-Port Multi-Gig""],""required"":true},
                            {""name"":""PoE"",""label"":""Power Over Ethernet"",""type"":""select"",""options"":[""PoE+ (30W/port)"",""PoE++ (60W/port)"",""Non-PoE""],""required"":true},
                            {""name"":""Uplinks"",""label"":""Uplink SFP Ports"",""type"":""text"",""required"":true,""placeholder"":""e.g. 4x 10G SFP+""},
                            {""name"":""ManagementIP"",""label"":""Default Management IP"",""type"":""text"",""required"":false,""placeholder"":""e.g. 10.10.1.5/24""}
                        ]"
                    },
                    new DeviceForm
                    {
                        Name = "Network Printer",
                        Department = MaintenanceTeams.IT,
                        Form = @"[
                            {""name"":""Tech"",""label"":""Print Technology"",""type"":""select"",""options"":[""Laser Monochrome"",""Laser Color"",""Thermal Barcode / Wristband"",""Multi-Function Copier (MFP)""],""required"":true},
                            {""name"":""Resolution"",""label"":""Max Resolution"",""type"":""text"",""required"":false,""placeholder"":""e.g. 1200 x 1200 dpi""},
                            {""name"":""Duplex"",""label"":""Automatic Duplex (2-sided)"",""type"":""select"",""options"":[""Yes"",""No""],""required"":true},
                            {""name"":""TrayCapacity"",""label"":""Paper Tray Capacity"",""type"":""text"",""required"":false,""placeholder"":""e.g. 2x 500-sheet trays""}
                        ]"
                    },
                    new DeviceForm
                    {
                        Name = "Rack Server",
                        Department = MaintenanceTeams.IT,
                        Form = @"[
                            {""name"":""Chassis"",""label"":""Rack Units (U)"",""type"":""select"",""options"":[""1U Rack"",""2U Rack"",""4U Rack"",""Blade Enclosure""],""required"":true},
                            {""name"":""CPU"",""label"":""Processors Installed"",""type"":""text"",""required"":true,""placeholder"":""e.g. 2x Intel Xeon Silver 4314""},
                            {""name"":""RAM"",""label"":""Installed ECC Memory"",""type"":""select"",""options"":[""64 GB DDR4"",""128 GB DDR4"",""256 GB DDR4"",""512 GB DDR4""],""required"":true},
                            {""name"":""RAID"",""label"":""Storage / RAID Setup"",""type"":""text"",""required"":true,""placeholder"":""e.g. 6x 1.92TB SAS SSD RAID 6""},
                            {""name"":""PSU"",""label"":""Power Supply Units"",""type"":""text"",""required"":false,""placeholder"":""e.g. Dual Redundant 800W Platinum""}
                        ]"
                    },

                    // --- BIOMEDICAL ENGINEERING TEMPLATES ---
                    new DeviceForm
                    {
                        Name = "Patient Monitor",
                        Department = MaintenanceTeams.Biomedical,
                        Form = @"[
                            {""name"":""Screen"",""label"":""Display Screen"",""type"":""text"",""required"":true,""placeholder"":""e.g. 19-inch High Resolution Medical Touchscreen""},
                            {""name"":""Parameters"",""label"":""Monitored Parameters"",""type"":""text"",""required"":true,""placeholder"":""e.g. ECG, SpO2, NIBP, Invasive Pressure, Temp, EtCO2""},
                            {""name"":""LeadCount"",""label"":""ECG Lead Option"",""type"":""select"",""options"":[""3-Lead"",""5-Lead"",""12-Lead Diagnostic""],""required"":true},
                            {""name"":""Battery"",""label"":""Battery Autonomy"",""type"":""text"",""required"":false,""placeholder"":""e.g. 4 Hours Lithium-ion""},
                            {""name"":""Telemetry"",""label"":""Central Telemetry Link"",""type"":""select"",""options"":[""Wired Ethernet"",""Wireless 5GHz Wi-Fi"",""Dual Link""],""required"":true}
                        ]"
                    },
                    new DeviceForm
                    {
                        Name = "ICU Ventilator",
                        Department = MaintenanceTeams.Biomedical,
                        Form = @"[
                            {""name"":""VentModes"",""label"":""Ventilation Modes"",""type"":""text"",""required"":true,""placeholder"":""e.g. VC-CMV, PC-BIPAP, PSV, APRV, CPAP/NIV""},
                            {""name"":""PatientRange"",""label"":""Patient Demographic"",""type"":""select"",""options"":[""Adult & Pediatric"",""Neonatal Specialized"",""Universal (Neonatal to Adult)""],""required"":true},
                            {""name"":""GasSupply"",""label"":""Gas Supply Requirements"",""type"":""text"",""required"":true,""placeholder"":""e.g. Pipeline O2 & Medical Air (2.8 - 6 bar)""},
                            {""name"":""InternalBattery"",""label"":""Battery Backup Time"",""type"":""text"",""required"":false,""placeholder"":""e.g. 180 Minutes Internal Li-Ion""}
                        ]"
                    },
                    new DeviceForm
                    {
                        Name = "Defibrillator",
                        Department = MaintenanceTeams.Biomedical,
                        Form = @"[
                            {""name"":""Waveform"",""label"":""Defibrillation Waveform"",""type"":""select"",""options"":[""Biphasic Truncated Exponential"",""Rectilinear Biphasic""],""required"":true},
                            {""name"":""Pacing"",""label"":""External Transcutaneous Pacing"",""type"":""select"",""options"":[""Yes - Demand and Fixed"",""No""],""required"":true},
                            {""name"":""CPRFeedback"",""label"":""Real-time CPR Sensor Feedback"",""type"":""select"",""options"":[""Equipped"",""Not Equipped""],""required"":true},
                            {""name"":""BatteryType"",""label"":""Rechargeable Battery Chemistry"",""type"":""text"",""required"":false,""placeholder"":""e.g. Smart Rechargeable Lithium-ion""}
                        ]"
                    },
                    new DeviceForm
                    {
                        Name = "Ultrasound Scanner",
                        Department = MaintenanceTeams.Biomedical,
                        Form = @"[
                            {""name"":""Probes"",""label"":""Included Transducer Probes"",""type"":""text"",""required"":true,""placeholder"":""e.g. Convex Abdominal, Linear Vascular, Phased Cardiac""},
                            {""name"":""ProbePorts"",""label"":""Active Transducer Ports"",""type"":""select"",""options"":[""2 Active Ports"",""3 Active Ports"",""4 Active Ports""],""required"":true},
                            {""name"":""Doppler"",""label"":""Doppler Capabilities"",""type"":""text"",""required"":false,""placeholder"":""e.g. Color Doppler, Pulsed Wave, Continuous Wave, Power Doppler""},
                            {""name"":""Display"",""label"":""Monitor Specification"",""type"":""text"",""required"":false,""placeholder"":""e.g. 21.5-inch OLED Articulating Arm""}
                        ]"
                    },
                    new DeviceForm
                    {
                        Name = "Mobile X-Ray",
                        Department = MaintenanceTeams.Biomedical,
                        Form = @"[
                            {""name"":""GeneratorOutput"",""label"":""Generator Output (kW)"",""type"":""select"",""options"":[""15 kW High Frequency"",""30 kW High Frequency"",""35 kW High Frequency"",""50 kW Ultra High Frequency""],""required"":true},
                            {""name"":""Detector"",""label"":""Flat Panel Detector (FPD)"",""type"":""text"",""required"":true,""placeholder"":""e.g. 14x17 inch Wireless Cesium Iodide (CsI)""},
                            {""name"":""DriveSystem"",""label"":""Motorized Travel Assist"",""type"":""select"",""options"":[""Dual Motorized Drive"",""Manual Push""],""required"":true}
                        ]"
                    },

                    // --- GENERAL FACILITY MAINTENANCE TEMPLATES ---
                    new DeviceForm
                    {
                        Name = "Facility UPS",
                        Department = MaintenanceTeams.General,
                        Form = @"[
                            {""name"":""Capacity"",""label"":""Apparent / Active Power"",""type"":""select"",""options"":[""10 kVA / 9 kW"",""20 kVA / 18 kW"",""40 kVA / 36 kW"",""80 kVA / 72 kW"",""120 kVA / 108 kW""],""required"":true},
                            {""name"":""BatteryBank"",""label"":""Battery Bank Specification"",""type"":""text"",""required"":true,""placeholder"":""e.g. 32x 12V 100Ah VRLA Batteries""},
                            {""name"":""Autonomy"",""label"":""Full Load Autonomy Time"",""type"":""text"",""required"":false,""placeholder"":""e.g. 45 Minutes Critical Hospital Load""},
                            {""name"":""InputVoltage"",""label"":""Nominal Input / Output"",""type"":""select"",""options"":[""380/400V 3-Phase 4-Wire"",""220/230V Single Phase""],""required"":true}
                        ]"
                    },
                    new DeviceForm
                    {
                        Name = "HVAC Chiller",
                        Department = MaintenanceTeams.General,
                        Form = @"[
                            {""name"":""CoolingTons"",""label"":""Cooling Capacity (TR / kW)"",""type"":""select"",""options"":[""25 TR (88 kW)"",""50 TR (175 kW)"",""100 TR (350 kW)"",""200 TR (700 kW)""],""required"":true},
                            {""name"":""Refrigerant"",""label"":""Refrigerant Gas"",""type"":""select"",""options"":[""R-410A Eco"",""R-134a"",""R-32"",""R-407C""],""required"":true},
                            {""name"":""CompressorCount"",""label"":""Compressor Circuit Count"",""type"":""select"",""options"":[""Single Scroll"",""Dual Scroll"",""Dual Screw Hermetic""],""required"":true},
                            {""name"":""PowerFeed"",""label"":""Main Electrical Supply"",""type"":""text"",""required"":false,""placeholder"":""e.g. 380V / 3Ph / 50Hz, 125A Breaker""}
                        ]"
                    },
                    new DeviceForm
                    {
                        Name = "Emergency Generator",
                        Department = MaintenanceTeams.General,
                        Form = @"[
                            {""name"":""PrimeRating"",""label"":""Prime Power Rating (kVA)"",""type"":""select"",""options"":[""150 kVA"",""250 kVA"",""500 kVA"",""750 kVA"",""1000 kVA""],""required"":true},
                            {""name"":""EngineMake"",""label"":""Diesel Engine Brand / Model"",""type"":""text"",""required"":true,""placeholder"":""e.g. Perkins 2506A / Cummins QSK23""},
                            {""name"":""ATS"",""label"":""Automatic Transfer Switch"",""type"":""text"",""required"":false,""placeholder"":""e.g. ASCO 7000 Series 4-Pole 800A""},
                            {""name"":""FuelTank"",""label"":""Base Fuel Tank Capacity"",""type"":""text"",""required"":false,""placeholder"":""e.g. 1000 Liters (12 hours runtime)""}
                        ]"
                    },
                    new DeviceForm
                    {
                        Name = "Medical Gas Manifold",
                        Department = MaintenanceTeams.General,
                        Form = @"[
                            {""name"":""GasService"",""label"":""Medical Gas Type"",""type"":""select"",""options"":[""Medical Oxygen (O2)"",""Medical Air 4-Bar"",""Surgical Air 7-Bar"",""Nitrous Oxide (N2O)"",""Medical Vacuum""],""required"":true},
                            {""name"":""BankSize"",""label"":""Cylinder Bank Configuration"",""type"":""text"",""required"":true,""placeholder"":""e.g. 2x 10-Cylinder Dual Bank Auto-Changeover""},
                            {""name"":""SupplyPressure"",""label"":""Regulated Distribution Pressure"",""type"":""text"",""required"":true,""placeholder"":""e.g. 4.2 bar (60 psig)""},
                            {""name"":""ReserveSupply"",""label"":""Emergency Reserve Bank"",""type"":""select"",""options"":[""Third Emergency Bank Attached"",""Standalone High-Pressure Pack""],""required"":false}
                        ]"
                    },
                    new DeviceForm
                    {
                        Name = "Cleanroom HEPA Filter",
                        Department = MaintenanceTeams.General,
                        Form = @"[
                            {""name"":""Efficiency"",""label"":""Filter Rating / Efficiency"",""type"":""select"",""options"":[""H14 HEPA 99.995%"",""U15 ULPA 99.9995%"",""H13 HEPA 99.95%""],""required"":true},
                            {""name"":""FlowRate"",""label"":""Rated Airflow Volume"",""type"":""text"",""required"":true,""placeholder"":""e.g. 1200 CFM (2000 m3/h)""},
                            {""name"":""PressureGauge"",""label"":""Magnehelic Differential Gauge"",""type"":""select"",""options"":[""Installed (0-500 Pa)"",""Digital Sensor to BMS""],""required"":true},
                            {""name"":""LocationServed"",""label"":""Hospital Critical Area"",""type"":""text"",""required"":false,""placeholder"":""e.g. Operating Theatre 1 / Bone Marrow Transplant Room""}
                        ]"
                    }
                };

                context.DeviceForms.AddRange(templates);
                context.SaveChanges();
            }

            // Seed Arabic localization values in database
            EnsureArabicSeedData(context);

            // Seed / Update Calibration data for products (Task 2)
            EnsureCalibrationSeedData(context);

            // Seed / Ensure Software Catalog and Ticket Attachments
            EnsureSoftwareAndAttachmentData(context);

            // Seed Reserve Stock, Consumables & Spare Parts (Task 5)
            EnsureSparePartsSeedData(context);

            // Seed Categories, Spare Part Forms, Calibration Protocols & Checklists (Tasks 1, 2, 5, 6, 7, 8)
            EnsureNewFeaturesSeedData(context);
        }

        private static void EnsureArabicSchema(StockDbContext context)
        {
            var isSqlite = context.Database.ProviderName?.Contains("Sqlite") == true;
            try
            {
                if (isSqlite)
                {
                    TryExecuteSql(context, "ALTER TABLE departments ADD COLUMN name_ar TEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE ticket_status ADD COLUMN status_ar TEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE custody_type ADD COLUMN type_ar TEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE suppliers ADD COLUMN name_ar TEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE users ADD COLUMN full_name_ar TEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE users ADD COLUMN role_ar TEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE rooms ADD COLUMN room_name_ar TEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE assets ADD COLUMN equipment_type_ar TEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE assets ADD COLUMN status_ar TEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE tickets ADD COLUMN title_ar TEXT NULL;");

                    // Calibration Schema (Task 2)
                    TryExecuteSql(context, "ALTER TABLE assets ADD COLUMN needsCalibration INTEGER NULL DEFAULT 0;");
                    TryExecuteSql(context, "ALTER TABLE assets ADD COLUMN calibrationInterval INTEGER NULL;");
                    TryExecuteSql(context, "ALTER TABLE assets ADD COLUMN lastCalibration TEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE assets ADD COLUMN nextCalibration TEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE assets ADD COLUMN calibrationCertNo TEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE assets ADD COLUMN calibratedBy TEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE assets ADD COLUMN calibrationNotes TEXT NULL;");

                    // Workstation Software & Ticket Telemetry Schema
                    TryExecuteSql(context, "ALTER TABLE assets ADD COLUMN installed_softwares TEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE tickets ADD COLUMN affected_software TEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE tickets ADD COLUMN pc_name TEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE tickets ADD COLUMN closed_by_users TEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE ticket_attachments ADD COLUMN messageID INTEGER NULL;");

                    // Device Forms Installable Softwares Schema
                    TryExecuteSql(context, "ALTER TABLE device_forms ADD COLUMN has_software INTEGER NOT NULL DEFAULT 0;");
                    TryExecuteSql(context, "ALTER TABLE device_forms ADD COLUMN installable_softwares TEXT NULL;");
                    TryExecuteSql(context, @"UPDATE device_forms SET has_software = 1, installable_softwares = '[""Windows 11 Pro"",""Microsoft Office 365"",""Hospital HIS / EHR Client"",""PACS Imaging Viewer"",""Antivirus Agent"",""Google Chrome""]' WHERE (name = 'Laptop' OR name = 'Desktop Workstation') AND (has_software = 0 OR installable_softwares IS NULL OR installable_softwares = '[]');");

                    TryExecuteSql(context, @"
                        CREATE TABLE IF NOT EXISTS software_catalog (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            name TEXT NOT NULL,
                            category TEXT NULL,
                            vendor TEXT NULL,
                            is_active INTEGER NOT NULL DEFAULT 1
                        );");

                    TryExecuteSql(context, @"
                        CREATE TABLE IF NOT EXISTS ticket_attachments (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ticketID INTEGER NOT NULL,
                            messageID INTEGER NULL,
                            fileName TEXT NOT NULL,
                            filePath TEXT NOT NULL,
                            fileType TEXT NULL,
                            fileSize INTEGER NULL,
                            uploadedDT TEXT NOT NULL,
                            uploadedBy INTEGER NULL
                        );");

                    TryExecuteSql(context, @"
                        CREATE TABLE IF NOT EXISTS spare_parts (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            name TEXT NOT NULL,
                            name_ar TEXT NULL,
                            part_number TEXT NULL,
                            department TEXT NOT NULL,
                            category TEXT NULL,
                            quantity INTEGER NOT NULL DEFAULT 0,
                            min_reserve_quantity INTEGER NOT NULL DEFAULT 2,
                            unit TEXT NULL DEFAULT 'Piece',
                            unit_cost REAL NULL,
                            storage_location TEXT NULL,
                            can_function_as_asset INTEGER NOT NULL DEFAULT 0,
                            compatible_equipment TEXT NULL,
                            notes TEXT NULL,
                            is_active INTEGER NOT NULL DEFAULT 1,
                            created_at TEXT NOT NULL,
                            updated_at TEXT NULL
                        );");

                    TryExecuteSql(context, @"
                        CREATE TABLE IF NOT EXISTS spare_part_transactions (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            spare_part_id INTEGER NOT NULL,
                            ticket_id INTEGER NULL,
                            asset_id INTEGER NULL,
                            user_id INTEGER NULL,
                            transaction_type TEXT NOT NULL,
                            quantity_changed INTEGER NOT NULL,
                            quantity_after INTEGER NOT NULL,
                            notes TEXT NULL,
                            timestamp TEXT NOT NULL
                        );");

                    // Task 1, 2, 5, 6, 7, 8 Schema Additions for SQLite
                    TryExecuteSql(context, "ALTER TABLE assets ADD COLUMN categoryID INTEGER NULL;");
                    TryExecuteSql(context, "ALTER TABLE assets ADD COLUMN has_vendor_maintenance INTEGER NOT NULL DEFAULT 0;");
                    TryExecuteSql(context, "ALTER TABLE assets ADD COLUMN vendor_maintenance_start TEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE assets ADD COLUMN vendor_maintenance_end TEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE assets ADD COLUMN vendor_maintenance_count INTEGER NULL;");
                    TryExecuteSql(context, "ALTER TABLE assets ADD COLUMN vendor_maintenance_completed INTEGER NOT NULL DEFAULT 0;");
                    TryExecuteSql(context, "ALTER TABLE assets ADD COLUMN vendor_maintenance_visits TEXT NULL DEFAULT '[]';");
                    TryExecuteSql(context, "ALTER TABLE assets ADD COLUMN last_calibration_data TEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE assets ADD COLUMN last_checklist_date TEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE assets ADD COLUMN last_checklist_by TEXT NULL;");

                    TryExecuteSql(context, "ALTER TABLE spare_parts ADD COLUMN form_id INTEGER NULL;");
                    TryExecuteSql(context, "ALTER TABLE spare_parts ADD COLUMN specs TEXT NULL DEFAULT '{}';");

                    TryExecuteSql(context, @"
                        CREATE TABLE IF NOT EXISTS asset_categories (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            name TEXT NOT NULL,
                            name_ar TEXT NULL,
                            department TEXT NOT NULL,
                            description TEXT NULL,
                            is_active INTEGER NOT NULL DEFAULT 1,
                            created_at TEXT NOT NULL
                        );");

                    TryExecuteSql(context, @"
                        CREATE TABLE IF NOT EXISTS spare_part_forms (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            name TEXT NOT NULL,
                            department TEXT NOT NULL,
                            form TEXT NOT NULL,
                            created_at TEXT NOT NULL
                        );");

                    TryExecuteSql(context, @"
                        CREATE TABLE IF NOT EXISTS asset_compatible_spare_parts (
                            asset_id INTEGER NOT NULL,
                            spare_part_id INTEGER NOT NULL,
                            assigned_at TEXT NOT NULL,
                            PRIMARY KEY (asset_id, spare_part_id)
                        );");

                    TryExecuteSql(context, @"
                        CREATE TABLE IF NOT EXISTS calibration_forms (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            name TEXT NOT NULL,
                            department TEXT NOT NULL,
                            category_id INTEGER NULL,
                            form TEXT NOT NULL,
                            is_active INTEGER NOT NULL DEFAULT 1,
                            created_at TEXT NOT NULL
                        );");

                    TryExecuteSql(context, @"
                        CREATE TABLE IF NOT EXISTS checklist_forms (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            title TEXT NOT NULL,
                            department TEXT NOT NULL,
                            category_id INTEGER NULL,
                            checklist_type TEXT NOT NULL,
                            items_json TEXT NOT NULL,
                            is_active INTEGER NOT NULL DEFAULT 1,
                            created_at TEXT NOT NULL
                        );");
                }
                else
                {
                    TryExecuteSql(context, "ALTER TABLE `departments` ADD COLUMN IF NOT EXISTS `name_ar` varchar(255) NULL;");
                    TryExecuteSql(context, "ALTER TABLE `ticket_status` ADD COLUMN IF NOT EXISTS `status_ar` varchar(255) NULL;");
                    TryExecuteSql(context, "ALTER TABLE `custody_type` ADD COLUMN IF NOT EXISTS `type_ar` varchar(255) NULL;");
                    TryExecuteSql(context, "ALTER TABLE `suppliers` ADD COLUMN IF NOT EXISTS `name_ar` varchar(255) NULL;");
                    TryExecuteSql(context, "ALTER TABLE `users` ADD COLUMN IF NOT EXISTS `full_name_ar` varchar(255) NULL;");
                    TryExecuteSql(context, "ALTER TABLE `users` ADD COLUMN IF NOT EXISTS `role_ar` varchar(255) NULL;");
                    TryExecuteSql(context, "ALTER TABLE `rooms` ADD COLUMN IF NOT EXISTS `room_name_ar` varchar(255) NULL;");
                    TryExecuteSql(context, "ALTER TABLE `assets` ADD COLUMN IF NOT EXISTS `equipment_type_ar` varchar(255) NULL;");
                    TryExecuteSql(context, "ALTER TABLE `assets` ADD COLUMN IF NOT EXISTS `status_ar` varchar(255) NULL;");
                    TryExecuteSql(context, "ALTER TABLE `tickets` ADD COLUMN IF NOT EXISTS `title_ar` varchar(255) NULL;");

                    // Calibration Schema (Task 2)
                    TryExecuteSql(context, "ALTER TABLE `assets` ADD COLUMN IF NOT EXISTS `needsCalibration` tinyint(1) NULL DEFAULT 0;");
                    TryExecuteSql(context, "ALTER TABLE `assets` ADD COLUMN IF NOT EXISTS `calibrationInterval` int NULL;");
                    TryExecuteSql(context, "ALTER TABLE `assets` ADD COLUMN IF NOT EXISTS `lastCalibration` date NULL;");
                    TryExecuteSql(context, "ALTER TABLE `assets` ADD COLUMN IF NOT EXISTS `nextCalibration` date NULL;");
                    TryExecuteSql(context, "ALTER TABLE `assets` ADD COLUMN IF NOT EXISTS `calibrationCertNo` varchar(255) NULL;");
                    TryExecuteSql(context, "ALTER TABLE `assets` ADD COLUMN IF NOT EXISTS `calibratedBy` varchar(255) NULL;");
                    TryExecuteSql(context, "ALTER TABLE `assets` ADD COLUMN IF NOT EXISTS `calibrationNotes` varchar(1024) NULL;");

                    // Workstation Software & Ticket Telemetry Schema
                    TryExecuteSql(context, "ALTER TABLE `assets` ADD COLUMN IF NOT EXISTS `installed_softwares` LONGTEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE `tickets` ADD COLUMN IF NOT EXISTS `affected_software` varchar(255) NULL;");
                    TryExecuteSql(context, "ALTER TABLE `tickets` ADD COLUMN IF NOT EXISTS `pc_name` varchar(255) NULL;");
                    TryExecuteSql(context, "ALTER TABLE `tickets` ADD COLUMN IF NOT EXISTS `closed_by_users` LONGTEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE `ticket_attachments` ADD COLUMN IF NOT EXISTS `messageID` int NULL;");

                    // Device Forms Installable Softwares Schema
                    TryExecuteSql(context, "ALTER TABLE `device_forms` ADD COLUMN IF NOT EXISTS `has_software` tinyint(1) NOT NULL DEFAULT 0;");
                    TryExecuteSql(context, "ALTER TABLE `device_forms` ADD COLUMN IF NOT EXISTS `installable_softwares` LONGTEXT NULL;");
                    TryExecuteSql(context, @"UPDATE `device_forms` SET `has_software` = 1, `installable_softwares` = '[""Windows 11 Pro"",""Microsoft Office 365"",""Hospital HIS / EHR Client"",""PACS Imaging Viewer"",""Antivirus Agent"",""Google Chrome""]' WHERE (`name` = 'Laptop' OR `name` = 'Desktop Workstation') AND (`has_software` = 0 OR `installable_softwares` IS NULL OR `installable_softwares` = '[]');");

                    TryExecuteSql(context, @"
                        CREATE TABLE IF NOT EXISTS `software_catalog` (
                            `id` int NOT NULL AUTO_INCREMENT,
                            `name` varchar(255) NOT NULL,
                            `category` varchar(100) NULL,
                            `vendor` varchar(255) NULL,
                            `is_active` tinyint(1) NOT NULL DEFAULT 1,
                            PRIMARY KEY (`id`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                    TryExecuteSql(context, @"
                        CREATE TABLE IF NOT EXISTS `ticket_attachments` (
                            `id` int NOT NULL AUTO_INCREMENT,
                            `ticketID` int NOT NULL,
                            `messageID` int NULL,
                            `fileName` varchar(255) NOT NULL,
                            `filePath` varchar(500) NOT NULL,
                            `fileType` varchar(50) NULL,
                            `fileSize` bigint NULL,
                            `uploadedDT` datetime NOT NULL,
                            `uploadedBy` int NULL,
                            PRIMARY KEY (`id`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                    TryExecuteSql(context, @"
                        CREATE TABLE IF NOT EXISTS `spare_parts` (
                            `id` int NOT NULL AUTO_INCREMENT,
                            `name` varchar(255) NOT NULL,
                            `name_ar` varchar(255) NULL,
                            `part_number` varchar(100) NULL,
                            `department` varchar(50) NOT NULL,
                            `category` varchar(50) NULL,
                            `quantity` int NOT NULL DEFAULT 0,
                            `min_reserve_quantity` int NOT NULL DEFAULT 2,
                            `unit` varchar(50) NULL DEFAULT 'Piece',
                            `unit_cost` decimal(18,2) NULL,
                            `storage_location` varchar(255) NULL,
                            `can_function_as_asset` tinyint(1) NOT NULL DEFAULT 0,
                            `compatible_equipment` varchar(500) NULL,
                            `notes` varchar(1024) NULL,
                            `is_active` tinyint(1) NOT NULL DEFAULT 1,
                            `created_at` datetime NOT NULL,
                            `updated_at` datetime NULL,
                            PRIMARY KEY (`id`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                    TryExecuteSql(context, @"
                        CREATE TABLE IF NOT EXISTS `spare_part_transactions` (
                            `id` int NOT NULL AUTO_INCREMENT,
                            `spare_part_id` int NOT NULL,
                            `ticket_id` int NULL,
                            `asset_id` int NULL,
                            `user_id` int NULL,
                            `transaction_type` varchar(50) NOT NULL,
                            `quantity_changed` int NOT NULL,
                            `quantity_after` int NOT NULL,
                            `notes` varchar(1024) NULL,
                            `timestamp` datetime NOT NULL,
                            PRIMARY KEY (`id`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                    // Task 1, 2, 5, 6, 7, 8 Schema Additions for MySQL
                    TryExecuteSql(context, "ALTER TABLE `assets` ADD COLUMN IF NOT EXISTS `categoryID` int NULL;");
                    TryExecuteSql(context, "ALTER TABLE `assets` ADD COLUMN IF NOT EXISTS `has_vendor_maintenance` tinyint(1) NOT NULL DEFAULT 0;");
                    TryExecuteSql(context, "ALTER TABLE `assets` ADD COLUMN IF NOT EXISTS `vendor_maintenance_start` date NULL;");
                    TryExecuteSql(context, "ALTER TABLE `assets` ADD COLUMN IF NOT EXISTS `vendor_maintenance_end` date NULL;");
                    TryExecuteSql(context, "ALTER TABLE `assets` ADD COLUMN IF NOT EXISTS `vendor_maintenance_count` int NULL;");
                    TryExecuteSql(context, "ALTER TABLE `assets` ADD COLUMN IF NOT EXISTS `vendor_maintenance_completed` int NOT NULL DEFAULT 0;");
                    TryExecuteSql(context, "ALTER TABLE `assets` ADD COLUMN IF NOT EXISTS `vendor_maintenance_visits` LONGTEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE `assets` ADD COLUMN IF NOT EXISTS `last_calibration_data` LONGTEXT NULL;");
                    TryExecuteSql(context, "ALTER TABLE `assets` ADD COLUMN IF NOT EXISTS `last_checklist_date` datetime NULL;");
                    TryExecuteSql(context, "ALTER TABLE `assets` ADD COLUMN IF NOT EXISTS `last_checklist_by` varchar(255) NULL;");

                    TryExecuteSql(context, "ALTER TABLE `spare_parts` ADD COLUMN IF NOT EXISTS `form_id` int NULL;");
                    TryExecuteSql(context, "ALTER TABLE `spare_parts` ADD COLUMN IF NOT EXISTS `specs` LONGTEXT NULL;");

                    TryExecuteSql(context, @"
                        CREATE TABLE IF NOT EXISTS `asset_categories` (
                            `id` int NOT NULL AUTO_INCREMENT,
                            `name` varchar(255) NOT NULL,
                            `name_ar` varchar(255) NULL,
                            `department` varchar(50) NOT NULL,
                            `description` varchar(1024) NULL,
                            `is_active` tinyint(1) NOT NULL DEFAULT 1,
                            `created_at` datetime NOT NULL,
                            PRIMARY KEY (`id`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                    TryExecuteSql(context, @"
                        CREATE TABLE IF NOT EXISTS `spare_part_forms` (
                            `id` int NOT NULL AUTO_INCREMENT,
                            `name` varchar(255) NOT NULL,
                            `department` varchar(50) NOT NULL,
                            `form` LONGTEXT NOT NULL,
                            `created_at` datetime NOT NULL,
                            PRIMARY KEY (`id`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                    TryExecuteSql(context, @"
                        CREATE TABLE IF NOT EXISTS `asset_compatible_spare_parts` (
                            `asset_id` int NOT NULL,
                            `spare_part_id` int NOT NULL,
                            `assigned_at` datetime NOT NULL,
                            PRIMARY KEY (`asset_id`, `spare_part_id`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                    TryExecuteSql(context, @"
                        CREATE TABLE IF NOT EXISTS `calibration_forms` (
                            `id` int NOT NULL AUTO_INCREMENT,
                            `name` varchar(255) NOT NULL,
                            `department` varchar(50) NOT NULL,
                            `category_id` int NULL,
                            `form` LONGTEXT NOT NULL,
                            `is_active` tinyint(1) NOT NULL DEFAULT 1,
                            `created_at` datetime NOT NULL,
                            PRIMARY KEY (`id`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                    TryExecuteSql(context, @"
                        CREATE TABLE IF NOT EXISTS `checklist_forms` (
                            `id` int NOT NULL AUTO_INCREMENT,
                            `title` varchar(255) NOT NULL,
                            `department` varchar(50) NOT NULL,
                            `category_id` int NULL,
                            `checklist_type` varchar(50) NOT NULL,
                            `items_json` LONGTEXT NOT NULL,
                            `is_active` tinyint(1) NOT NULL DEFAULT 1,
                            `created_at` datetime NOT NULL,
                            PRIMARY KEY (`id`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
                }
            }
            catch {}
        }

        private static void EnsureArabicSeedData(StockDbContext context)
        {

            // Populate Arabic names for ticket_status
            try
            {
                var tsMap = new System.Collections.Generic.Dictionary<int, string>
                {
                    [1] = "مفتوحة",
                    [2] = "قيد الإجراء",
                    [3] = "مؤجلة",
                    [4] = "ملغاة",
                    [5] = "مغلقة",
                    [6] = "في الصيانة"
                };
                foreach (var ts in context.TicketStatuses.ToList())
                {
                    if (string.IsNullOrEmpty(ts.StatusAr) && tsMap.ContainsKey(ts.Id))
                    {
                        ts.StatusAr = tsMap[ts.Id];
                    }
                }
                context.SaveChanges();
            }
            catch {}

            // Populate Arabic names for custody_type
            try
            {
                var ctMap = new System.Collections.Generic.Dictionary<int, string>
                {
                    [1] = "عهدة شخصية",
                    [2] = "عهدة مشتركة",
                    [3] = "صيانة خارجية / المورد"
                };
                foreach (var ct in context.CustodyTypes.ToList())
                {
                    if (string.IsNullOrEmpty(ct.TypeAr) && ctMap.ContainsKey(ct.Id))
                    {
                        ct.TypeAr = ctMap[ct.Id];
                    }
                }
                context.SaveChanges();
            }
            catch {}

            // Populate Arabic names for departments
            try
            {
                var deptMap = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
                {
                    ["Information Technology"] = "تقنية المعلومات",
                    ["Radiology & Imaging"] = "الأشعة والتصوير الطبي",
                    ["Cardiology"] = "أمراض وجراحة القلب",
                    ["Biomedical Engineering"] = "الهندسة الطبية الحيوية",
                    ["Hospital Administration"] = "إدارة المستشفى العامة"
                };
                foreach (var d in context.Departments.ToList())
                {
                    if (string.IsNullOrEmpty(d.NameAr) && d.Name != null && deptMap.ContainsKey(d.Name))
                    {
                        d.NameAr = deptMap[d.Name];
                    }
                }
                context.SaveChanges();
            }
            catch {}

            // Populate Arabic names for suppliers
            try
            {
                var suppMap = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
                {
                    ["Dell Technologies"] = "ديل للتقنية (Dell)",
                    ["Philips Healthcare"] = "فيليبس للرعاية الصحية (Philips)",
                    ["HP Enterprise"] = "إتش بي إنتربرايز (HPE)",
                    ["Cisco Systems"] = "سيسكو سيستمز (Cisco)",
                    ["GE Healthcare"] = "جنرال إلكتريك للرعاية الصحية (GE)",
                    ["Carrier & Facility Engineering"] = "كارير لهندسة المرافق والتبريد"
                };
                foreach (var s in context.Suppliers.ToList())
                {
                    if (string.IsNullOrEmpty(s.NameAr) && s.Name != null && suppMap.ContainsKey(s.Name))
                    {
                        s.NameAr = suppMap[s.Name];
                    }
                }
                context.SaveChanges();
            }
            catch {}

            // Populate Arabic names for rooms
            try
            {
                var roomMap = new System.Collections.Generic.Dictionary<int, string>
                {
                    [101] = "عيادة الاستقبال الفحص 1",
                    [102] = "عيادة الفحص العام 2",
                    [105] = "غرفة الخوادم الرئيسية (Data Center)",
                    [201] = "وحدة العناية المركزة (ICU)",
                    [301] = "مختبر الأشعة المقطعية (CT Scan)"
                };
                foreach (var r in context.Rooms.ToList())
                {
                    if (string.IsNullOrEmpty(r.RoomNameAr) && r.RoomNo.HasValue && roomMap.ContainsKey(r.RoomNo.Value))
                    {
                        r.RoomNameAr = roomMap[r.RoomNo.Value];
                    }
                }
                context.SaveChanges();
            }
            catch {}

            // Populate Arabic names for users
            try
            {
                var userMap = new System.Collections.Generic.Dictionary<string, (string NameAr, string RoleAr)>(System.StringComparer.OrdinalIgnoreCase)
                {
                    ["Abbas Al-Amiri"] = ("عباس العامري", "مدير تقنية المعلومات ومسؤول النظم الرئيسي"),
                    ["Ahmed Mansour"] = ("أحمد منصور", "فني دعم ميداني وصيانة"),
                    ["Dr. Sarah Jenkins"] = ("د. سارة جنكينز", "استشاري أول أمراض القلب"),
                    ["Dr. Elena Rostova"] = ("د. إيلينا روستوفا", "استشاري الأشعة والتصوير الطبي"),
                    ["Tariq Ziad"] = ("طارق زياد", "مهندس أجهزة طبية حيوية"),
                    ["Former Staff"] = ("موظف سابق", "حساب معطل")
                };
                foreach (var u in context.Users.ToList())
                {
                    if (u.FullName != null && userMap.ContainsKey(u.FullName))
                    {
                        var info = userMap[u.FullName];
                        if (string.IsNullOrEmpty(u.FullNameAr)) u.FullNameAr = info.NameAr;
                        if (string.IsNullOrEmpty(u.RoleAr)) u.RoleAr = info.RoleAr;
                    }
                }
                context.SaveChanges();
            }
            catch {}
        }

        private static void EnsureCalibrationSeedData(StockDbContext context)
        {
            try
            {
                var assets = context.Assets.ToList();
                foreach (var asset in assets)
                {
                    var isBiomedical = asset.MaintenanceTeam == MaintenanceTeams.Biomedical ||
                                       (asset.Type != null && (asset.Type.Contains("Ultrasound", StringComparison.OrdinalIgnoreCase) ||
                                                              asset.Type.Contains("Monitor", StringComparison.OrdinalIgnoreCase) ||
                                                              asset.Type.Contains("Defibrillator", StringComparison.OrdinalIgnoreCase) ||
                                                              asset.Type.Contains("ECG", StringComparison.OrdinalIgnoreCase) ||
                                                              asset.Type.Contains("Ventilator", StringComparison.OrdinalIgnoreCase)));

                    if (isBiomedical && (asset.NeedsCalibration == null || asset.NeedsCalibration == false))
                    {
                        asset.NeedsCalibration = true;
                        if (asset.Type?.Contains("Ultrasound", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            asset.CalibrationInterval = 365;
                            asset.LastCalibration = DateTime.Now.AddDays(-340);
                            asset.NextCalibration = DateTime.Now.AddDays(25); // Due soon (25 days left)
                            asset.CalibrationCertNo = "CAL-2025-US-991";
                            asset.CalibratedBy = "Philips Precision Calibration Lab";
                            asset.CalibrationNotes = "AIUM ultrasound acoustic output and frequency verification within ±1.5% tolerance standard.";
                        }
                        else
                        {
                            // Patient Monitor or other biomedical device
                            asset.CalibrationInterval = 180;
                            asset.LastCalibration = DateTime.Now.AddDays(-200);
                            asset.NextCalibration = DateTime.Now.AddDays(-20); // Overdue by 20 days!
                            asset.CalibrationCertNo = "CAL-2025-MON-442";
                            asset.CalibratedBy = "National Biomedical Metrology Authority";
                            asset.CalibrationNotes = "ECG gain accuracy 10mm/mV, NIBP transducer calibrated against mercury manometer NIST standard.";
                        }
                    }
                    else if (!isBiomedical && asset.NeedsCalibration == null)
                    {
                        // IT & General hardware does not need calibration
                        asset.NeedsCalibration = false;
                    }
                }
                context.SaveChanges();
            }
            catch {}
        }

        private static void EnsureSoftwareAndAttachmentData(StockDbContext context)
        {
            try
            {
                // Ensure upload directory exists
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "tickets");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // Seed Software Catalog
                if (!context.SoftwareCatalog.Any())
                {
                    var softwares = new[]
                    {
                        new SoftwareCatalogItem { Name = "HIS - Hospital Information System (EHR)", Category = "Hospital Systems", Vendor = "Epic / Cerner / In-House" },
                        new SoftwareCatalogItem { Name = "PACS - Radiology DICOM Viewer", Category = "Clinical Imaging", Vendor = "Philips / GE Healthcare" },
                        new SoftwareCatalogItem { Name = "LIS - Laboratory Information System", Category = "Laboratory", Vendor = "Abbott / Roche" },
                        new SoftwareCatalogItem { Name = "Clinical Pharmacy Dispensing", Category = "Pharmacy", Vendor = "Pyxis / Omnicell" },
                        new SoftwareCatalogItem { Name = "Microsoft Office 365 (Word, Excel, Outlook)", Category = "Office Productivity", Vendor = "Microsoft Corporation" },
                        new SoftwareCatalogItem { Name = "Pulse Secure / GlobalProtect VPN", Category = "Network & Security", Vendor = "Palo Alto / Ivanti" },
                        new SoftwareCatalogItem { Name = "Telemetry & Patient Monitoring Suite", Category = "Clinical", Vendor = "Philips IntelliVue" },
                        new SoftwareCatalogItem { Name = "SAP ERP / Finance & Supply", Category = "Enterprise ERP", Vendor = "SAP" },
                        new SoftwareCatalogItem { Name = "Zebra Barcode Label Designer & Print", Category = "Utility", Vendor = "Zebra Technologies" },
                        new SoftwareCatalogItem { Name = "Antivirus & Endpoint Security (EDR)", Category = "Security", Vendor = "CrowdStrike / Defender" },
                        new SoftwareCatalogItem { Name = "Google Chrome Enterprise", Category = "Browser", Vendor = "Google LLC" },
                        new SoftwareCatalogItem { Name = "Adobe Acrobat Reader DC", Category = "Document", Vendor = "Adobe Inc." },
                        new SoftwareCatalogItem { Name = "Zoom & Teams Telehealth Suite", Category = "Communication", Vendor = "Zoom / Microsoft" },
                        new SoftwareCatalogItem { Name = "IntelliSpace Critical Care & Anesthesia (ICCA)", Category = "Clinical", Vendor = "Philips Healthcare" }
                    };
                    context.SoftwareCatalog.AddRange(softwares);
                    context.SaveChanges();
                }

                // Seed installed softwares on existing IT workstations if empty
                var itAssets = context.Assets.Where(a => a.MaintenanceTeam == MaintenanceTeams.IT && (a.InstalledSoftwares == null || a.InstalledSoftwares == "" || a.InstalledSoftwares == "[]")).ToList();
                if (itAssets.Any())
                {
                    var defaultSoftwares = System.Text.Json.JsonSerializer.Serialize(new[]
                    {
                        "HIS - Hospital Information System (EHR)",
                        "Microsoft Office 365 (Word, Excel, Outlook)",
                        "PACS - Radiology DICOM Viewer",
                        "Google Chrome Enterprise"
                    });
                    foreach (var asset in itAssets)
                    {
                        asset.InstalledSoftwares = defaultSoftwares;
                    }
                    context.SaveChanges();
                }
            }
            catch {}
        }

        private static void EnsureSparePartsSeedData(StockDbContext context)
        {
            try
            {
                if (!context.SpareParts.Any())
                {
                    var parts = new[]
                    {
                        // IT Reserve Stock
                        new SparePart
                        {
                            Name = "500GB NVMe M.2 SSD",
                            NameAr = "قرص تخزين فائق السرعة 500 جيجابايت NVMe M.2",
                            PartNumber = "SSD-NVME-500GB",
                            Department = MaintenanceTeams.IT,
                            Category = "Spare Part",
                            Quantity = 8,
                            MinReserveQuantity = 3,
                            Unit = "Piece",
                            UnitCost = 45.00m,
                            StorageLocation = "IT Store - Shelf B2",
                            CanFunctionAsAsset = true, // Dual nature: internal PC spare or commissioned as external backup SSD
                            CompatibleEquipment = "Dell OptiPlex Desktops, HP Workstations, Hospital Laptops",
                            Notes = "Dual-purpose: Can be used as replacement internal drive or mounted in enclosure as external backup drive."
                        },
                        new SparePart
                        {
                            Name = "1TB SATA 2.5\" SSD",
                            NameAr = "قرص تخزين صلب 1 تيرابايت SATA 2.5 بوصة",
                            PartNumber = "SSD-SATA-1TB",
                            Department = MaintenanceTeams.IT,
                            Category = "Spare Part",
                            Quantity = 5,
                            MinReserveQuantity = 2,
                            Unit = "Piece",
                            UnitCost = 68.00m,
                            StorageLocation = "IT Store - Shelf B3",
                            CanFunctionAsAsset = true, // Dual nature: replacement or standalone backup disk
                            CompatibleEquipment = "All Server & Desktop SATA bays, USB 3.0 external enclosures",
                            Notes = "High reliability 3D NAND SSD for workstation refresh or emergency backup storage."
                        },
                        new SparePart
                        {
                            Name = "16GB DDR4 3200MHz RAM",
                            NameAr = "ذاكرة وصول عشوائي 16 جيجابايت DDR4",
                            PartNumber = "RAM-DDR4-16GB",
                            Department = MaintenanceTeams.IT,
                            Category = "Spare Part",
                            Quantity = 12,
                            MinReserveQuantity = 4,
                            Unit = "Piece",
                            UnitCost = 35.00m,
                            StorageLocation = "IT Store - Shelf B1",
                            CanFunctionAsAsset = false,
                            CompatibleEquipment = "Dell & HP Clinic Workstations",
                            Notes = "Replacement memory module for diagnostic workstations."
                        },
                        new SparePart
                        {
                            Name = "Cat6 UTP Patch Cable 3M",
                            NameAr = "سلك شبكة Cat6 بطول 3 أمتار عالي الجودة",
                            PartNumber = "CAB-CAT6-3M",
                            Department = MaintenanceTeams.IT,
                            Category = "Consumable",
                            Quantity = 45,
                            MinReserveQuantity = 15,
                            Unit = "Piece",
                            UnitCost = 3.50m,
                            StorageLocation = "IT Store - Bin C1",
                            CanFunctionAsAsset = false,
                            CompatibleEquipment = "All Network Data Points, IP Phones, Wall Jacks",
                            Notes = "Molded strain relief snagless Gigabit patch leads."
                        },
                        new SparePart
                        {
                            Name = "Thermal Paste Syringe 4g",
                            NameAr = "معجون تبريد حراري للمعالجات 4 جرام",
                            PartNumber = "THM-PST-4G",
                            Department = MaintenanceTeams.IT,
                            Category = "Periodic Maintenance Reserve",
                            Quantity = 6,
                            MinReserveQuantity = 2,
                            Unit = "Piece",
                            UnitCost = 7.50m,
                            StorageLocation = "IT Store - Shelf A1",
                            CanFunctionAsAsset = false,
                            CompatibleEquipment = "CPU Heatsinks, Server Processors, GPU Coolers",
                            Notes = "Used during semi-annual periodic workstation cleanups."
                        },
                        new SparePart
                        {
                            Name = "24-inch FHD IPS Monitor",
                            NameAr = "شاشة عرض 24 بوصة FHD IPS متوافقة",
                            PartNumber = "MON-24-DELL",
                            Department = MaintenanceTeams.IT,
                            Category = "Spare Part",
                            Quantity = 4,
                            MinReserveQuantity = 2,
                            Unit = "Piece",
                            UnitCost = 135.00m,
                            StorageLocation = "IT Store - Rack D",
                            CanFunctionAsAsset = true, // Can replace broken monitor or be commissioned as independent asset
                            CompatibleEquipment = "All Reception Desks, Nurse Stations, Doctor Clinics",
                            Notes = "Dual-purpose: replacement panel or new workstation commission."
                        },

                        // BIOMEDICAL Reserve Stock
                        new SparePart
                        {
                            Name = "Patient Monitor 5-Lead ECG Trunk Cable",
                            NameAr = "كابل تخطيط القلب الرئيسي 5 أطراف لأجهزة المراقبة",
                            PartNumber = "BIO-ECG-CAB5",
                            Department = MaintenanceTeams.Biomedical,
                            Category = "Spare Part",
                            Quantity = 7,
                            MinReserveQuantity = 3,
                            Unit = "Piece",
                            UnitCost = 115.00m,
                            StorageLocation = "Biomedical Lab - Drawer 1",
                            CanFunctionAsAsset = false,
                            CompatibleEquipment = "Philips IntelliVue MX450/MX700, Mindray BeneView",
                            Notes = "Defibrillator-protected trunk cable."
                        },
                        new SparePart
                        {
                            Name = "Defibrillator Rechargeable Lithium Battery",
                            NameAr = "بطارية ليثيوم قابلة للشحن لجهاز الصدمات الكهربائية",
                            PartNumber = "BIO-BATT-DFB",
                            Department = MaintenanceTeams.Biomedical,
                            Category = "Periodic Maintenance Reserve",
                            Quantity = 3,
                            MinReserveQuantity = 2,
                            Unit = "Piece",
                            UnitCost = 280.00m,
                            StorageLocation = "Biomedical Lab - Cabinet Safe",
                            CanFunctionAsAsset = false,
                            CompatibleEquipment = "Zoll R Series, Philips HeartStart XL+",
                            Notes = "Scheduled annual replacement battery packs."
                        },
                        new SparePart
                        {
                            Name = "Adult Reusable SpO2 Sensor Probe",
                            NameAr = "مجس قياس نسبة الأكسجين بالدم قابل لإعادة الاستخدام",
                            PartNumber = "BIO-SPO2-ADL",
                            Department = MaintenanceTeams.Biomedical,
                            Category = "Consumable",
                            Quantity = 18,
                            MinReserveQuantity = 6,
                            Unit = "Piece",
                            UnitCost = 42.00m,
                            StorageLocation = "Biomedical Lab - Drawer 2",
                            CanFunctionAsAsset = false,
                            CompatibleEquipment = "ICU Multi-parameter Monitors, Pulse Oximeters",
                            Notes = "Durable silicone finger clip sensor."
                        },
                        new SparePart
                        {
                            Name = "Medical Diagnostic Tablet Display 10\"",
                            NameAr = "جهاز لوحي طبي محمول 10 بوصة للمعاينة والتشخيص",
                            PartNumber = "BIO-TAB-MED10",
                            Department = MaintenanceTeams.Biomedical,
                            Category = "Spare Part",
                            Quantity = 3,
                            MinReserveQuantity = 1,
                            Unit = "Piece",
                            UnitCost = 690.00m,
                            StorageLocation = "Biomedical Lab - Safe B",
                            CanFunctionAsAsset = true, // Can replace broken display or be commissioned as standalone asset
                            CompatibleEquipment = "Mobile Ultrasound Consoles, Portable Telemetry Stations",
                            Notes = "Dual-purpose: Can replace modular tablet screen or be commissioned as portable clinical asset."
                        },
                        new SparePart
                        {
                            Name = "Ventilator Expiratory Filter & Water Trap",
                            NameAr = "فلتر زفير ومصيدة مياه لجهاز التنفس الاصطناعي",
                            PartNumber = "BIO-VENT-FLT",
                            Department = MaintenanceTeams.Biomedical,
                            Category = "Consumable",
                            Quantity = 25,
                            MinReserveQuantity = 10,
                            Unit = "Piece",
                            UnitCost = 24.00m,
                            StorageLocation = "Biomedical Lab - Shelf C",
                            CanFunctionAsAsset = false,
                            CompatibleEquipment = "Hamilton-G5, Dräger Evita Infinity V500",
                            Notes = "Autoclavable bacterial/viral filter set."
                        },

                        // GENERAL / FACILITIES Reserve Stock
                        new SparePart
                        {
                            Name = "HVAC Antimicrobial Air Filter 24x24x2",
                            NameAr = "فلتر هواء مكيفات مركزي مضاد للميكروبات 24x24x2",
                            PartNumber = "FAC-HVAC-FLT24",
                            Department = MaintenanceTeams.General,
                            Category = "Periodic Maintenance Reserve",
                            Quantity = 32,
                            MinReserveQuantity = 12,
                            Unit = "Piece",
                            UnitCost = 16.50m,
                            StorageLocation = "Facilities Store - Bay 4",
                            CanFunctionAsAsset = false,
                            CompatibleEquipment = "Hospital Central AHU Systems & Clinic HVAC",
                            Notes = "Quarterly periodic maintenance replacement filters."
                        },
                        new SparePart
                        {
                            Name = "T8 LED Tube Light 40W 120cm",
                            NameAr = "أنبوب إضاءة ليد T8 موفر للطاقة 40 واط 120 سم",
                            PartNumber = "FAC-LED-T8-40W",
                            Department = MaintenanceTeams.General,
                            Category = "Consumable",
                            Quantity = 50,
                            MinReserveQuantity = 20,
                            Unit = "Piece",
                            UnitCost = 4.25m,
                            StorageLocation = "Facilities Store - Bay 2",
                            CanFunctionAsAsset = false,
                            CompatibleEquipment = "Wards, Corridors, Nursing Stations Fixtures",
                            Notes = "Daylight 6500K energy-efficient tubes."
                        },
                        new SparePart
                        {
                            Name = "Commercial Water Pump Mechanical Seal",
                            NameAr = "مانع تسرب ميكانيكي لمضخات المياه المركزية",
                            PartNumber = "FAC-PMP-SEAL",
                            Department = MaintenanceTeams.General,
                            Category = "Spare Part",
                            Quantity = 4,
                            MinReserveQuantity = 2,
                            Unit = "Piece",
                            UnitCost = 88.00m,
                            StorageLocation = "Facilities Workshop - Shelf M1",
                            CanFunctionAsAsset = false,
                            CompatibleEquipment = "Grundfos Hydro MPC Booster Pumps",
                            Notes = "Silicon Carbide shaft seal for domestic booster pumps."
                        },
                        new SparePart
                        {
                            Name = "Portable Industrial Dehumidifier 50L/D",
                            NameAr = "جهاز إزالة رطوبة وتجفيف صناعي متنقل 50 لتر/يوم",
                            PartNumber = "FAC-AIR-DEHUM50",
                            Department = MaintenanceTeams.General,
                            Category = "Spare Part",
                            Quantity = 2,
                            MinReserveQuantity = 1,
                            Unit = "Piece",
                            UnitCost = 410.00m,
                            StorageLocation = "Facilities Workshop - Ground",
                            CanFunctionAsAsset = true, // Can be used as facility equipment or commissioned as asset
                            CompatibleEquipment = "Clean Rooms, Server Rooms, Operating Suites",
                            Notes = "Dual-purpose: Can be deployed for emergency maintenance or commissioned as hospital asset."
                        }
                    };

                    context.SpareParts.AddRange(parts);
                    context.SaveChanges();

                    // Create initial replenishment transactions for audit trail
                    var initialAdmin = context.Users.FirstOrDefault(u => u.Role == "Admin" || u.Role == "IT Manager & Lead SysAdmin") ?? context.Users.FirstOrDefault();
                    foreach (var part in parts)
                    {
                        context.SparePartTransactions.Add(new SparePartTransaction
                        {
                            SparePartId = part.Id,
                            UserId = initialAdmin?.Id,
                            TransactionType = "Replenishment",
                            QuantityChanged = part.Quantity,
                            QuantityAfter = part.Quantity,
                            Notes = $"Initial reserve stock configuration for {part.Department} maintenance department.",
                            Timestamp = DateTime.Now
                        });
                    }
                    context.SaveChanges();
                }
            }
            catch {}
        }

        private static void EnsureNewFeaturesSeedData(StockDbContext context)
        {
            try
            {
                // Ensure Admin user (Abbas / domain admin) has role 'ADMIN' so Admin can manage users and see everything
                string currentPcUser = string.IsNullOrWhiteSpace(Environment.UserName) ? "Abbas" : Environment.UserName;
                TryExecuteSql(context, "UPDATE users SET role = 'ADMIN' WHERE role LIKE '%Admin%' OR role LIKE '%Manager%' OR role LIKE '%SysAdmin%' OR fullName LIKE '%Abbas%';");
                var adminUser = context.Users.FirstOrDefault(u => u.DomainUser == currentPcUser || (u.FullName != null && u.FullName.Contains("Abbas")));
                if (adminUser != null && adminUser.Role != SystemRoles.Admin)
                {
                    adminUser.Role = SystemRoles.Admin;
                    adminUser.IsActive = true;
                    context.SaveChanges();
                }

                // 1. Seed Asset Categories (Task 6)
                if (!context.AssetCategories.Any())
                {
                    var categories = new System.Collections.Generic.List<AssetCategory>
                    {
                        // IT Categories
                        new AssetCategory { Name = "PC", NameAr = "حاسوب مكتبي", Department = MaintenanceTeams.IT, Description = "Desktop workstations, micro-PCs, and towers", CreatedAt = DateTime.Now },
                        new AssetCategory { Name = "Laptop", NameAr = "حاسوب محمول", Department = MaintenanceTeams.IT, Description = "Notebooks, Ultrabooks, and mobile workstations", CreatedAt = DateTime.Now },
                        new AssetCategory { Name = "Printer", NameAr = "طابعة", Department = MaintenanceTeams.IT, Description = "Laser, thermal, and multifunction network printers", CreatedAt = DateTime.Now },
                        new AssetCategory { Name = "Scanner", NameAr = "ماسح ضوئي", Department = MaintenanceTeams.IT, Description = "Document scanners, flatbed and barcode scanners", CreatedAt = DateTime.Now },
                        new AssetCategory { Name = "PC Peripheral", NameAr = "ملحقات الحاسوب (ماوس ولوحة مفاتيح وشاشات)", Department = MaintenanceTeams.IT, Description = "Keyboards, mice, monitors, headsets, webcams, docking stations", CreatedAt = DateTime.Now },
                        new AssetCategory { Name = "Server", NameAr = "خادم شبكة", Department = MaintenanceTeams.IT, Description = "Rack-mount, tower and virtualization servers", CreatedAt = DateTime.Now },
                        new AssetCategory { Name = "Network Switch", NameAr = "موزع شبكة / سويتش", Department = MaintenanceTeams.IT, Description = "Core, distribution and PoE access edge switches", CreatedAt = DateTime.Now },

                        // Biomedical Categories
                        new AssetCategory { Name = "Infusion Pump", NameAr = "مضخة تسريب محاليل", Department = MaintenanceTeams.Biomedical, Description = "Volumetric infusion and syringe drug delivery pumps", CreatedAt = DateTime.Now },
                        new AssetCategory { Name = "Patient Monitor", NameAr = "شاشة مراقبة علامات حيوية", Department = MaintenanceTeams.Biomedical, Description = "Multi-parameter physiological bedside and transport monitors", CreatedAt = DateTime.Now },
                        new AssetCategory { Name = "ECG Machine", NameAr = "جهاز تخطيط قلب كهربائي", Department = MaintenanceTeams.Biomedical, Description = "12-lead diagnostic resting electrocardiographs", CreatedAt = DateTime.Now },
                        new AssetCategory { Name = "Defibrillator", NameAr = "جهاز صدمات القلب الكهربائي", Department = MaintenanceTeams.Biomedical, Description = "Manual, AED, and pacing biphasic defibrillators", CreatedAt = DateTime.Now },
                        new AssetCategory { Name = "Ultrasound", NameAr = "جهاز تصوير بالموجات فوق الصوتية", Department = MaintenanceTeams.Biomedical, Description = "Diagnostic ultrasound imaging systems and consoles", CreatedAt = DateTime.Now },
                        new AssetCategory { Name = "Ventilator", NameAr = "جهاز تنفس اصطناعي", Department = MaintenanceTeams.Biomedical, Description = "Intensive care and transport mechanical ventilators", CreatedAt = DateTime.Now },

                        // General Maintenance Categories
                        new AssetCategory { Name = "HVAC / Air Conditioner", NameAr = "أنظمة التكييف والتهوية", Department = MaintenanceTeams.General, Description = "Central chillers, AHUs, split AC units and fan coils", CreatedAt = DateTime.Now },
                        new AssetCategory { Name = "Electric Generator", NameAr = "مولد كهربائي احتياطي", Department = MaintenanceTeams.General, Description = "Emergency standby diesel generators and ATS units", CreatedAt = DateTime.Now },
                        new AssetCategory { Name = "Medical Refrigerator", NameAr = "ثلاجة طبية وحفظ أدوية", Department = MaintenanceTeams.General, Description = "Blood bank, laboratory and pharmaceutical cold storage", CreatedAt = DateTime.Now },
                        new AssetCategory { Name = "Elevator", NameAr = "مصعد كهربائي", Department = MaintenanceTeams.General, Description = "Patient bed elevators and service traction lifts", CreatedAt = DateTime.Now },
                        new AssetCategory { Name = "Water Pump", NameAr = "مضخة مياه", Department = MaintenanceTeams.General, Description = "Sanitary water booster and circulation pumps", CreatedAt = DateTime.Now }
                    };

                    context.AssetCategories.AddRange(categories);
                    context.SaveChanges();
                }

                // Associate existing assets with matching categories if not assigned
                var allCategories = context.AssetCategories.ToList();
                var unassignedAssets = context.Assets.Where(a => a.CategoryId == null).ToList();
                foreach (var asset in unassignedAssets)
                {
                    var cat = allCategories.FirstOrDefault(c =>
                        (!string.IsNullOrEmpty(asset.Type) && (c.Name.Equals(asset.Type, StringComparison.OrdinalIgnoreCase) || asset.Type.Contains(c.Name, StringComparison.OrdinalIgnoreCase))) ||
                        (!string.IsNullOrEmpty(asset.Brand) && c.Name.Contains(asset.Brand, StringComparison.OrdinalIgnoreCase)));
                    if (cat != null)
                    {
                        asset.CategoryId = cat.Id;
                    }
                    else
                    {
                        // Fall back to first category of asset's department
                        var deptCat = allCategories.FirstOrDefault(c => c.Department == asset.MaintenanceTeam);
                        if (deptCat != null) asset.CategoryId = deptCat.Id;
                    }
                }
                context.SaveChanges();

                // 2. Seed Spare Part Forms (Task 1)
                if (!context.SparePartForms.Any())
                {
                    var spForms = new System.Collections.Generic.List<SparePartForm>
                    {
                        new SparePartForm
                        {
                            Name = "RAM / Memory Module",
                            Department = MaintenanceTeams.IT,
                            Form = @"[
                                {""name"":""Gen"",""label"":""Memory Generation"",""type"":""select"",""options"":[""DDR4"",""DDR5"",""DDR3""],""required"":true},
                                {""name"":""Capacity"",""label"":""Module Capacity"",""type"":""select"",""options"":[""8 GB"",""16 GB"",""32 GB"",""64 GB""],""required"":true},
                                {""name"":""Speed"",""label"":""Frequency Speed"",""type"":""text"",""required"":false,""placeholder"":""e.g. 3200 MHz / 4800 MT/s""},
                                {""name"":""FormFactor"",""label"":""Form Factor"",""type"":""select"",""options"":[""UDIMM (Desktop)"",""SO-DIMM (Laptop)"",""RDIMM ECC (Server)""],""required"":true}
                            ]",
                            CreatedAt = DateTime.Now
                        },
                        new SparePartForm
                        {
                            Name = "Storage Drive / SSD",
                            Department = MaintenanceTeams.IT,
                            Form = @"[
                                {""name"":""DriveType"",""label"":""Storage Architecture"",""type"":""select"",""options"":[""NVMe M.2 PCIe 4.0"",""SATA III 2.5-inch"",""SAS Enterprise 12Gbps""],""required"":true},
                                {""name"":""Capacity"",""label"":""Storage Size"",""type"":""select"",""options"":[""256 GB"",""512 GB"",""1 TB"",""2 TB"",""4 TB""],""required"":true},
                                {""name"":""Endurance"",""label"":""Endurance (TBW)"",""type"":""text"",""required"":false,""placeholder"":""e.g. 600 TBW""}
                            ]",
                            CreatedAt = DateTime.Now
                        },
                        new SparePartForm
                        {
                            Name = "Biomedical Sensor & Probe",
                            Department = MaintenanceTeams.Biomedical,
                            Form = @"[
                                {""name"":""SensorType"",""label"":""Sensor Application"",""type"":""select"",""options"":[""SpO2 Finger Sensor"",""ECG 5-Lead Trunk Cable"",""NIBP Blood Pressure Cuff"",""Skin Temperature Probe""],""required"":true},
                                {""name"":""ConnectorPin"",""label"":""Connector Configuration"",""type"":""text"",""required"":true,""placeholder"":""e.g. 14-Pin Oximax / Sub-D 15""},
                                {""name"":""Sterilization"",""label"":""Sterilization Compatibility"",""type"":""select"",""options"":[""Autoclavable"",""Alcohol Wipe Disinfectable"",""Disposable Single-Use""],""required"":true}
                            ]",
                            CreatedAt = DateTime.Now
                        },
                        new SparePartForm
                        {
                            Name = "Medical Device Battery Pack",
                            Department = MaintenanceTeams.Biomedical,
                            Form = @"[
                                {""name"":""Chemistry"",""label"":""Battery Chemistry"",""type"":""select"",""options"":[""Lithium-ion Rechargeable"",""NiMH High-Drain"",""Sealed Lead-Acid (SLA)""],""required"":true},
                                {""name"":""Voltage"",""label"":""Rated Voltage"",""type"":""text"",""required"":true,""placeholder"":""e.g. 11.1 V / 14.8 V""},
                                {""name"":""Capacity"",""label"":""Capacity (mAh / Wh)"",""type"":""text"",""required"":true,""placeholder"":""e.g. 4400 mAh (48 Wh)""}
                            ]",
                            CreatedAt = DateTime.Now
                        },
                        new SparePartForm
                        {
                            Name = "Electric Motor & Fan Assembly",
                            Department = MaintenanceTeams.General,
                            Form = @"[
                                {""name"":""MotorHP"",""label"":""Power Output (HP / kW)"",""type"":""text"",""required"":true,""placeholder"":""e.g. 1.5 HP (1.1 kW)""},
                                {""name"":""Voltage"",""label"":""Operating Voltage"",""type"":""select"",""options"":[""220V Single-Phase"",""380V 3-Phase 50Hz"",""Dual Voltage 220/380V""],""required"":true},
                                {""name"":""RPM"",""label"":""Rated Speed (RPM)"",""type"":""text"",""required"":false,""placeholder"":""e.g. 1450 RPM""}
                            ]",
                            CreatedAt = DateTime.Now
                        }
                    };

                    context.SparePartForms.AddRange(spForms);
                    context.SaveChanges();
                }

                // 3. Seed Calibration Forms (Task 7)
                if (!context.CalibrationForms.Any())
                {
                    var calForms = new System.Collections.Generic.List<CalibrationForm>
                    {
                        new CalibrationForm
                        {
                            Name = "Infusion Pump Flow Rate & Occlusion Protocol",
                            Department = MaintenanceTeams.Biomedical,
                            Form = @"[
                                {""name"":""TestFlowRate"",""label"":""Test Flow Rate Setpoint (ml/h)"",""type"":""select"",""options"":[""20 ml/h"",""100 ml/h"",""500 ml/h""],""required"":true},
                                {""name"":""MeasuredFlowRate"",""label"":""Measured Delivered Rate (ml/h)"",""type"":""text"",""required"":true,""placeholder"":""e.g. 99.4 ml/h""},
                                {""name"":""AccuracyDeviation"",""label"":""Flow Accuracy Deviation (%)"",""type"":""text"",""required"":true,""placeholder"":""e.g. -0.6% (Tolerance: +/- 5%)""},
                                {""name"":""OcclusionPressure"",""label"":""Downstream Occlusion Trip (mmHg)"",""type"":""text"",""required"":true,""placeholder"":""e.g. 620 mmHg (Nominal: 600 +/- 100)""},
                                {""name"":""AirInLineAlarm"",""label"":""Air-in-Line Bubble Detection"",""type"":""select"",""options"":[""Pass - Alarm within 50 uL"",""Fail - Did Not Alarm""],""required"":true},
                                {""name"":""StandardEquipment"",""label"":""Calibration Analyzer Used"",""type"":""text"",""required"":true,""placeholder"":""e.g. Fluke IDA-5 Infusion Device Analyzer (S/N FLK-9021)""},
                                {""name"":""OverallStatus"",""label"":""Protocol Outcome"",""type"":""select"",""options"":[""Certified & Compliant"",""Conditional Pass"",""Failed - Requires Service""],""required"":true}
                            ]",
                            CreatedAt = DateTime.Now
                        },
                        new CalibrationForm
                        {
                            Name = "Patient Monitor NIBP & SpO2 Verification Protocol",
                            Department = MaintenanceTeams.Biomedical,
                            Form = @"[
                                {""name"":""TargetNIBP"",""label"":""Simulator NIBP Target (Sys/Dia)"",""type"":""select"",""options"":[""120 / 80 mmHg (Mean 93)"",""80 / 50 mmHg (Mean 60)"",""200 / 150 mmHg (Mean 167)""],""required"":true},
                                {""name"":""MeasuredNIBP"",""label"":""Measured NIBP (Sys/Dia)"",""type"":""text"",""required"":true,""placeholder"":""e.g. 121 / 81 mmHg""},
                                {""name"":""LeakRate"",""label"":""Pneumatic Leak Rate (mmHg/min)"",""type"":""text"",""required"":true,""placeholder"":""e.g. 1.2 mmHg/min (Limit: < 6.0)""},
                                {""name"":""SpO2Simulated"",""label"":""SpO2 Simulator Setting"",""type"":""select"",""options"":[""98% SpO2 / 60 BPM"",""85% SpO2 / 80 BPM"",""70% SpO2 / 100 BPM""],""required"":true},
                                {""name"":""SpO2Measured"",""label"":""Monitor SpO2 Indication (%)"",""type"":""text"",""required"":true,""placeholder"":""e.g. 98%""},
                                {""name"":""SafetyLeakage"",""label"":""Enclosure Leakage Current (uA)"",""type"":""text"",""required"":true,""placeholder"":""e.g. 35 uA (IEC 62353 Limit: < 100 uA)""},
                                {""name"":""StandardEquipment"",""label"":""Metrology Standard Instrument"",""type"":""text"",""required"":true,""placeholder"":""e.g. Rigel 288+ / ProSim 8 Vital Signs Simulator""},
                                {""name"":""OverallStatus"",""label"":""Protocol Result"",""type"":""select"",""options"":[""Certified & Compliant"",""Calibrated with Adjustment"",""Out of Tolerance - Failed""],""required"":true}
                            ]",
                            CreatedAt = DateTime.Now
                        },
                        new CalibrationForm
                        {
                            Name = "Defibrillator Energy Discharge & Synchronization Protocol",
                            Department = MaintenanceTeams.Biomedical,
                            Form = @"[
                                {""name"":""EnergySelected"",""label"":""Selected Discharge Energy (Joules)"",""type"":""select"",""options"":[""50 Joules"",""100 Joules"",""150 Joules"",""200 Joules""],""required"":true},
                                {""name"":""EnergyDelivered"",""label"":""Delivered Energy into 50 Ohm (J)"",""type"":""text"",""required"":true,""placeholder"":""e.g. 198.5 Joules (Nominal 200 +/- 15%)""},
                                {""name"":""ChargeTime"",""label"":""Charge Time to Maximum (Seconds)"",""type"":""text"",""required"":true,""placeholder"":""e.g. 4.2 Seconds (Limit: < 7.0 s)""},
                                {""name"":""SyncDelay"",""label"":""Cardioversion Sync Delay (ms)"",""type"":""text"",""required"":true,""placeholder"":""e.g. 28 ms (Limit: < 60 ms)""},
                                {""name"":""StandardEquipment"",""label"":""Defibrillator Analyzer Used"",""type"":""text"",""required"":true,""placeholder"":""e.g. Fluke Impulse 7000DP Defibrillator Analyzer""},
                                {""name"":""OverallStatus"",""label"":""Defibrillator Certification"",""type"":""select"",""options"":[""Certified for Emergency Clinical Use"",""Failed Discharge Test""],""required"":true}
                            ]",
                            CreatedAt = DateTime.Now
                        },
                        new CalibrationForm
                        {
                            Name = "Pressure & Temperature Sensor Protocol",
                            Department = MaintenanceTeams.General,
                            Form = @"[
                                {""name"":""Range"",""label"":""Pressure Range"",""type"":""text"",""required"":true,""placeholder"":""e.g. 0 to 10 Bar""},
                                {""name"":""Reference"",""label"":""Reference Standard Value"",""type"":""text"",""required"":true,""placeholder"":""e.g. 5.00 Bar""},
                                {""name"":""Measured"",""label"":""Sensor Indicated Value"",""type"":""text"",""required"":true,""placeholder"":""e.g. 5.03 Bar""},
                                {""name"":""OverallStatus"",""label"":""Verification Status"",""type"":""select"",""options"":[""Certified Compliant"",""Recalibrated"",""Failed""],""required"":true}
                            ]",
                            CreatedAt = DateTime.Now
                        }
                    };

                    context.CalibrationForms.AddRange(calForms);
                    context.SaveChanges();
                }

                // 4. Seed Checklist Forms (Task 8)
                if (!context.ChecklistForms.Any())
                {
                    var pcCat = context.AssetCategories.FirstOrDefault(c => c.Name == "PC");
                    var printerCat = context.AssetCategories.FirstOrDefault(c => c.Name == "Printer");
                    var pumpCat = context.AssetCategories.FirstOrDefault(c => c.Name == "Infusion Pump");
                    var monitorCat = context.AssetCategories.FirstOrDefault(c => c.Name == "Patient Monitor");
                    var acCat = context.AssetCategories.FirstOrDefault(c => c.Name == "HVAC / Air Conditioner");

                    var checkLists = new System.Collections.Generic.List<ChecklistForm>
                    {
                        new ChecklistForm
                        {
                            Title = "Quarterly Workstation & PC Preventive Health Checklist",
                            Department = MaintenanceTeams.IT,
                            CategoryId = pcCat?.Id,
                            ChecklistType = "Periodic Maintenance",
                            ItemsJson = @"[""Inspect power cord, surge protector and surge ground indicator"",""Open chassis and clean dust accumulation from CPU heatsink and cooling fans"",""Inspect motherboard electrolytic capacitors for swelling or leakage"",""Execute SMART diagnostic drive health analysis on SSD/HDD"",""Verify hospital antivirus definitions and OS critical security patch compliance"",""Test network LAN port, patch cord clip, and speed negotiation (1000 Mbps)"",""Sanitize keyboard, mouse and chassis surfaces""]",
                            CreatedAt = DateTime.Now
                        },
                        new ChecklistForm
                        {
                            Title = "Network Printer Periodic Servicing & Quality Checklist",
                            Department = MaintenanceTeams.IT,
                            CategoryId = printerCat?.Id,
                            ChecklistType = "Periodic Maintenance",
                            ItemsJson = @"[""Clean pickup rollers, separation pads and paper feed paths"",""Inspect fuser roller and transfer belt for scratches or wear"",""Empty or replace toner waste collector box"",""Print diagnostic test pattern and verify color alignment"",""Verify network Ethernet cable connection and static IP accessibility"",""Check paper cassette tray guide alignment and dampness""]",
                            CreatedAt = DateTime.Now
                        },
                        new ChecklistForm
                        {
                            Title = "Infusion Pump Semi-Annual Safety & Function Checklist",
                            Department = MaintenanceTeams.Biomedical,
                            CategoryId = pumpCat?.Id,
                            ChecklistType = "Calibration",
                            ItemsJson = @"[""Visual inspection of housing, door latch, hinges, and IV pole clamp"",""Check AC power plug, hospital grade cord integrity and battery charging LED"",""Verify drop sensor lens cleanliness and anti-free-flow clip mechanism"",""Execute self-diagnostic memory and display segment test"",""Perform downstream occlusion pressure sensor trigger verification"",""Test battery autonomy by running on internal battery for 15 minutes"",""Verify clinical flow rate accuracy using certified graduated burette / analyzer"",""Affix updated electrical safety and calibration inspection sticker""]",
                            CreatedAt = DateTime.Now
                        },
                        new ChecklistForm
                        {
                            Title = "Bedside Patient Monitor Periodic Safety & Maintenance Checklist",
                            Department = MaintenanceTeams.Biomedical,
                            CategoryId = monitorCat?.Id,
                            ChecklistType = "Periodic Maintenance",
                            ItemsJson = @"[""Inspect chassis, display touchscreen, and articulated wall mount arm"",""Clean NIBP pneumatic internal air filter and inspect hose connectors"",""Check SpO2 finger probe cable, optical emitter and detector condition"",""Inspect ECG 5-lead trunk cable and individual snaps for pin damage"",""Verify acoustic alarm audio volume and visual top alarm flasher"",""Test internal battery backup by disconnecting mains power"",""Perform electrical safety leakage test conforming to IEC 62353"",""Verify network central station telemetry signal stability""]",
                            CreatedAt = DateTime.Now
                        },
                        new ChecklistForm
                        {
                            Title = "Central HVAC & AHU Preventive Maintenance Checklist",
                            Department = MaintenanceTeams.General,
                            CategoryId = acCat?.Id,
                            ChecklistType = "Periodic Maintenance",
                            ItemsJson = @"[""Wash or replace primary pre-filters and inspect secondary HEPA filters"",""Clean evaporator cooling coils and treat condensate drain pan with biocide"",""Inspect fan blower belts, tension, and pulleys for cracks and slippage"",""Check motor bearings lubrication and listen for abnormal bearing vibration"",""Measure operating current (Amps) on all electrical compressor phases"",""Check refrigerant suction and discharge pressure readings"",""Verify room thermostat calibration and air duct dampers operation""]",
                            CreatedAt = DateTime.Now
                        }
                    };

                    context.ChecklistForms.AddRange(checkLists);
                    context.SaveChanges();
                }

                // 5. Seed some initial asset-sparepart compatibility for demonstration (Task 2)
                if (!context.AssetCompatibleSpareParts.Any())
                {
                    var pcAsset = context.Assets.FirstOrDefault(a => a.Type == "Desktop Workstation" || a.Type == "Laptop" || (a.Category != null && a.Category.Name == "PC"));
                    var ramPart = context.SpareParts.FirstOrDefault(p => p.Name.Contains("RAM") || p.Name.Contains("Memory") || p.Name.Contains("DDR"));
                    var ssdPart = context.SpareParts.FirstOrDefault(p => p.Name.Contains("SSD") || p.Name.Contains("Storage"));
                    if (pcAsset != null)
                    {
                        if (ramPart != null)
                        {
                            context.AssetCompatibleSpareParts.Add(new AssetCompatibleSparePart { AssetId = pcAsset.Id, SparePartId = ramPart.Id });
                        }
                        if (ssdPart != null)
                        {
                            context.AssetCompatibleSpareParts.Add(new AssetCompatibleSparePart { AssetId = pcAsset.Id, SparePartId = ssdPart.Id });
                        }
                    }

                    var monitorAsset = context.Assets.FirstOrDefault(a => a.Type == "Patient Monitor" || (a.Category != null && a.Category.Name == "Patient Monitor"));
                    var spo2Part = context.SpareParts.FirstOrDefault(p => p.Name.Contains("SpO2") || p.Name.Contains("Sensor") || p.Name.Contains("Lead"));
                    if (monitorAsset != null && spo2Part != null)
                    {
                        context.AssetCompatibleSpareParts.Add(new AssetCompatibleSparePart { AssetId = monitorAsset.Id, SparePartId = spo2Part.Id });
                    }

                    context.SaveChanges();
                }
            }
            catch {}
        }

        private static void TryExecuteSql(StockDbContext context, string sql)
        {
            try { context.Database.ExecuteSqlRaw(sql); } catch {}
        }
    }
}

using Microsoft.EntityFrameworkCore;
using StockPortal.Models;

namespace StockPortal.Data
{
    public class StockDbContext : DbContext
    {
        public StockDbContext(DbContextOptions<StockDbContext> options) : base(options)
        {
        }

        public DbSet<Asset> Assets { get; set; } = null!;
        public DbSet<AssetHistory> AssetHistories { get; set; } = null!;
        public DbSet<AssetMaintenance> AssetMaintenances { get; set; } = null!;
        public DbSet<CustodyType> CustodyTypes { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Room> Rooms { get; set; } = null!;
        public DbSet<Supplier> Suppliers { get; set; } = null!;
        public DbSet<Ticket> Tickets { get; set; } = null!;
        public DbSet<TicketHandler> TicketHandlers { get; set; } = null!;
        public DbSet<TicketMsg> TicketMessages { get; set; } = null!;
        public DbSet<TicketStatus> TicketStatuses { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<DeviceForm> DeviceForms { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<SoftwareCatalogItem> SoftwareCatalog { get; set; } = null!;
        public DbSet<TicketAttachment> TicketAttachments { get; set; } = null!;
        public DbSet<SparePart> SpareParts { get; set; } = null!;
        public DbSet<SparePartTransaction> SparePartTransactions { get; set; } = null!;
        public DbSet<AssetCategory> AssetCategories { get; set; } = null!;
        public DbSet<SparePartForm> SparePartForms { get; set; } = null!;
        public DbSet<AssetCompatibleSparePart> AssetCompatibleSpareParts { get; set; } = null!;
        public DbSet<CalibrationForm> CalibrationForms { get; set; } = null!;
        public DbSet<ChecklistForm> ChecklistForms { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TicketHandler 1-to-1 with Ticket
            modelBuilder.Entity<TicketHandler>(entity =>
            {
                entity.HasKey(e => e.TicketId);

                entity.HasOne(e => e.Ticket)
                    .WithOne(t => t.Handler)
                    .HasForeignKey<TicketHandler>(e => e.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.TechUser)
                    .WithMany()
                    .HasForeignKey(e => e.TechUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Department HeadUser relationship
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasOne(d => d.HeadUser)
                    .WithMany()
                    .HasForeignKey(d => d.HeadUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(d => d.Users)
                    .WithOne(u => u.Department)
                    .HasForeignKey(u => u.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Asset relationships
            modelBuilder.Entity<Asset>(entity =>
            {
                entity.HasOne(a => a.User)
                    .WithMany(u => u.CustodyAssets)
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(a => a.Room)
                    .WithMany(r => r.Assets)
                    .HasForeignKey(a => a.RoomId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(a => a.Supplier)
                    .WithMany(s => s.Assets)
                    .HasForeignKey(a => a.SupplierId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(a => a.CustodyType)
                    .WithMany(c => c.Assets)
                    .HasForeignKey(a => a.CustodyTypeId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Ticket relationships
            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.HasOne(t => t.Asset)
                    .WithMany(a => a.Tickets)
                    .HasForeignKey(t => t.AssetId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(t => t.OpenedByUser)
                    .WithMany(u => u.OpenedTickets)
                    .HasForeignKey(t => t.OpenedBy)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(t => t.OnBehalfOfUser)
                    .WithMany()
                    .HasForeignKey(t => t.OnBehalfOf)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(t => t.Status)
                    .WithMany(s => s.Tickets)
                    .HasForeignKey(t => t.StatusId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // AssetHistory
            modelBuilder.Entity<AssetHistory>(entity =>
            {
                entity.HasOne(h => h.Asset)
                    .WithMany(a => a.History)
                    .HasForeignKey(h => h.AssetId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(h => h.User)
                    .WithMany()
                    .HasForeignKey(h => h.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(h => h.Room)
                    .WithMany()
                    .HasForeignKey(h => h.RoomId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(h => h.CustodyType)
                    .WithMany()
                    .HasForeignKey(h => h.CustodyTypeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // TicketMsg
            modelBuilder.Entity<TicketMsg>(entity =>
            {
                entity.HasOne(m => m.Ticket)
                    .WithMany(t => t.Messages)
                    .HasForeignKey(m => m.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.User)
                    .WithMany()
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // AssetMaintenance
            modelBuilder.Entity<AssetMaintenance>(entity =>
            {
                entity.HasOne(m => m.Asset)
                    .WithMany(a => a.MaintenanceRecords)
                    .HasForeignKey(m => m.AssetId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Ticket)
                    .WithMany(t => t.MaintenanceLogs)
                    .HasForeignKey(m => m.TicketId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(m => m.Technician)
                    .WithMany()
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(m => m.Supplier)
                    .WithMany()
                    .HasForeignKey(m => m.SupplierId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Notification relationships
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasOne(n => n.User)
                    .WithMany()
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(n => n.Ticket)
                    .WithMany()
                    .HasForeignKey(n => n.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // TicketAttachment relationships
            modelBuilder.Entity<TicketAttachment>(entity =>
            {
                entity.HasOne(a => a.Ticket)
                    .WithMany(t => t.Attachments)
                    .HasForeignKey(a => a.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Message)
                    .WithMany(m => m.Attachments)
                    .HasForeignKey(a => a.MessageId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(a => a.UploadedByUser)
                    .WithMany()
                    .HasForeignKey(a => a.UploadedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // SparePart & SparePartTransaction relationships
            modelBuilder.Entity<SparePartTransaction>(entity =>
            {
                entity.HasOne(t => t.SparePart)
                    .WithMany(p => p.Transactions)
                    .HasForeignKey(t => t.SparePartId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(t => t.Ticket)
                    .WithMany()
                    .HasForeignKey(t => t.TicketId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(t => t.Asset)
                    .WithMany()
                    .HasForeignKey(t => t.AssetId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(t => t.User)
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // AssetCompatibleSparePart many-to-many
            modelBuilder.Entity<AssetCompatibleSparePart>(entity =>
            {
                entity.HasKey(e => new { e.AssetId, e.SparePartId });

                entity.HasOne(e => e.Asset)
                    .WithMany(a => a.CompatibleSpareParts)
                    .HasForeignKey(e => e.AssetId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.SparePart)
                    .WithMany(p => p.CompatibleAssets)
                    .HasForeignKey(e => e.SparePartId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Asset & Category
            modelBuilder.Entity<Asset>(entity =>
            {
                entity.HasOne(a => a.Category)
                    .WithMany(c => c.Assets)
                    .HasForeignKey(a => a.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // SparePart & SparePartForm
            modelBuilder.Entity<SparePart>(entity =>
            {
                entity.HasOne(p => p.SparePartForm)
                    .WithMany(f => f.SpareParts)
                    .HasForeignKey(p => p.FormId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // CalibrationForm & Category
            modelBuilder.Entity<CalibrationForm>(entity =>
            {
                entity.HasOne(f => f.Category)
                    .WithMany(c => c.CalibrationForms)
                    .HasForeignKey(f => f.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ChecklistForm & Category
            modelBuilder.Entity<ChecklistForm>(entity =>
            {
                entity.HasOne(f => f.Category)
                    .WithMany(c => c.Checklists)
                    .HasForeignKey(f => f.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}

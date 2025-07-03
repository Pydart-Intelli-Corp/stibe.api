using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using stibe.api.Models.Entities;
using stibe.api.Models.Entities.PartnersEntity;
using stibe.api.Models.Entities.PartnersEntity.ServicesEntity;
using stibe.api.Models.Entities.PartnersEntity.StaffEntity;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace stibe.api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // Existing DbSet properties
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Salon> Salons { get; set; } = null!;
        public DbSet<Service> Services { get; set; } = null!;
        public DbSet<Booking> Bookings { get; set; } = null!;
        public DbSet<Staff> Staff { get; set; } = null!;
        public DbSet<StaffWorkSession> StaffWorkSessions { get; set; } = null!;
        public DbSet<StaffSpecialization> StaffSpecializations { get; set; } = null!;

        // New DbSet properties for service management enhancements
        public DbSet<ServiceCategory> ServiceCategories { get; set; } = null!;
        public DbSet<ServiceOffer> ServiceOffers { get; set; } = null!;
        public DbSet<ServiceOfferItem> ServiceOfferItems { get; set; } = null!;
        public DbSet<ServiceAvailability> ServiceAvailabilities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Existing configurations
            ConfigureUserEntity(modelBuilder);
            ConfigureSalonEntity(modelBuilder);
            ConfigureServiceEntity(modelBuilder);
            ConfigureBookingEntity(modelBuilder);
            ConfigureStaffEntity(modelBuilder);
            ConfigureStaffSpecializationEntity(modelBuilder);
            ConfigureStaffWorkSessionEntity(modelBuilder);

            // New configurations for service management
            ConfigureServiceCategoryEntity(modelBuilder);
            ConfigureServiceOfferEntity(modelBuilder);
            ConfigureServiceOfferItemEntity(modelBuilder);
            ConfigureServiceAvailabilityEntity(modelBuilder);
        }

        // Add missing configuration methods
        private void ConfigureUserEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(u => u.StaffProfile)
                .WithOne(s => s.User)
                .HasForeignKey<Staff>(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        }

        private void ConfigureSalonEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Salon>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<Salon>()
                .HasOne(s => s.Owner)
                .WithMany()
                .HasForeignKey(s => s.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for location-based searches
            modelBuilder.Entity<Salon>()
                .HasIndex(s => new { s.Latitude, s.Longitude })
                .HasFilter("Latitude IS NOT NULL AND Longitude IS NOT NULL");

            // Index for salon status
            modelBuilder.Entity<Salon>()
                .HasIndex(s => s.IsActive);
        }

        private void ConfigureStaffEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Staff>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<Staff>()
                .HasOne(s => s.Salon)
                .WithMany()
                .HasForeignKey(s => s.SalonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Staff>()
                .HasMany(s => s.Specializations)
                .WithOne(ss => ss.Staff)
                .HasForeignKey(ss => ss.StaffId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private void ConfigureBookingEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Booking>()
                .HasKey(b => b.Id);

            // Create indexes for common queries
            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.BookingDate);

            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.SalonId, b.BookingDate });

            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.CustomerId, b.Status });

            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.AssignedStaffId, b.Status });
        }

        private void ConfigureStaffSpecializationEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StaffSpecialization>()
                .HasKey(ss => ss.Id);

            // Create a unique index to prevent duplicates
            modelBuilder.Entity<StaffSpecialization>()
                .HasIndex(ss => new { ss.StaffId, ss.ServiceId })
                .IsUnique();

            modelBuilder.Entity<StaffSpecialization>()
                .HasOne(ss => ss.Service)
                .WithMany()
                .HasForeignKey(ss => ss.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private void ConfigureStaffWorkSessionEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StaffWorkSession>()
                .HasKey(sws => sws.Id);

            // Create index for date-based queries
            modelBuilder.Entity<StaffWorkSession>()
                .HasIndex(sws => new { sws.StaffId, sws.WorkDate });

            // Ensure only one work session per staff per day
            modelBuilder.Entity<StaffWorkSession>()
                .HasIndex(sws => new { sws.StaffId, sws.WorkDate })
                .IsUnique();
        }

        // New configuration methods
        private void ConfigureServiceCategoryEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ServiceCategory>()
                .HasKey(sc => sc.Id);

            modelBuilder.Entity<ServiceCategory>()
                .HasOne(sc => sc.Salon)
                .WithMany()
                .HasForeignKey(sc => sc.SalonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for salon categories lookup
            modelBuilder.Entity<ServiceCategory>()
                .HasIndex(sc => new { sc.SalonId, sc.IsActive });

            // Unique constraint for category name per salon
            modelBuilder.Entity<ServiceCategory>()
                .HasIndex(sc => new { sc.SalonId, sc.Name })
                .IsUnique()
                .HasFilter("IsDeleted = 0");
        }

        private void ConfigureServiceOfferEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ServiceOffer>()
                .HasKey(so => so.Id);

            modelBuilder.Entity<ServiceOffer>()
                .HasOne(so => so.Salon)
                .WithMany()
                .HasForeignKey(so => so.SalonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for faster querying of active offers
            modelBuilder.Entity<ServiceOffer>()
                .HasIndex(so => new { so.SalonId, so.IsActive });

            modelBuilder.Entity<ServiceOffer>()
                .HasIndex(so => new { so.StartDate, so.EndDate });
        }

        private void ConfigureServiceOfferItemEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ServiceOfferItem>()
                .HasKey(soi => soi.Id);

            // Prevent duplicate offer-service combinations
            modelBuilder.Entity<ServiceOfferItem>()
                .HasIndex(soi => new { soi.ServiceId, soi.OfferID })
                .IsUnique();

            modelBuilder.Entity<ServiceOfferItem>()
                .HasOne(soi => soi.Service)
                .WithMany(s => s.OfferItems)
                .HasForeignKey(soi => soi.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ServiceOfferItem>()
                .HasOne(soi => soi.Offer)
                .WithMany(so => so.ServiceOfferItems)
                .HasForeignKey(soi => soi.OfferID)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private void ConfigureServiceAvailabilityEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ServiceAvailability>()
                .HasKey(sa => sa.Id);

            modelBuilder.Entity<ServiceAvailability>()
                .HasOne(sa => sa.Service)
                .WithMany(s => s.Availabilities)
                .HasForeignKey(sa => sa.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for faster lookup of availability by day
            modelBuilder.Entity<ServiceAvailability>()
                .HasIndex(sa => new { sa.ServiceId, sa.DayOfWeek });
        }

        // Update ConfigureServiceEntity to include new relationships
        private void ConfigureServiceEntity(ModelBuilder modelBuilder)
        {
            // Existing service configuration
            modelBuilder.Entity<Service>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<Service>()
                .HasOne(s => s.Salon)
                .WithMany(s => s.Services)
                .HasForeignKey(s => s.SalonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add new relationship to category
            modelBuilder.Entity<Service>()
                .HasOne(s => s.Category)
                .WithMany(c => c.Services)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Index for finding active services in a salon
            modelBuilder.Entity<Service>()
                .HasIndex(s => new { s.SalonId, s.IsActive });

            // Index for category-based searches
            modelBuilder.Entity<Service>()
                .HasIndex(s => s.CategoryId);
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity && (
                    e.State == EntityState.Added ||
                    e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                var entity = (BaseEntity)entityEntry.Entity;

                if (entityEntry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }

                entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}

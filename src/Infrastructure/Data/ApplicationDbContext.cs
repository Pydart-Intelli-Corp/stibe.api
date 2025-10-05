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
        public DbSet<Shop> Shops { get; set; } = null!;
        public DbSet<Service> Services { get; set; } = null!;
        public DbSet<Booking> Bookings { get; set; } = null!;
        public DbSet<Staff> Staff { get; set; } = null!;
        public DbSet<StaffWorkSession> StaffWorkSessions { get; set; } = null!;
        public DbSet<StaffSpecialization> StaffSpecializations { get; set; } = null!;

        // OTP Management
        public DbSet<OtpEntity> OtpEntities { get; set; } = null!;

        // Payment Management
        public DbSet<Payment> Payments { get; set; } = null!;

        // Coupon Management
        public DbSet<CouponUsage> CouponUsages { get; set; } = null!;
        public DbSet<UserCouponUsage> UserCouponUsages { get; set; } = null!;

        // New DbSet properties for service management enhancements
        public DbSet<ServiceCategory> ServiceCategories { get; set; } = null!;
        public DbSet<ServiceOffer> ServiceOffers { get; set; } = null!;
        public DbSet<ServiceOfferItem> ServiceOfferItems { get; set; } = null!;
        public DbSet<ServiceAvailability> ServiceAvailabilities { get; set; } = null!;

        // KYC Management
        public DbSet<KycVerification> KycVerifications { get; set; } = null!;
        public DbSet<KycAuditLog> KycAuditLogs { get; set; } = null!;

        // Service Suggestions Management
        public DbSet<ServiceNameSuggestion> ServiceNameSuggestions { get; set; } = null!;
        public DbSet<ServiceDescriptionTemplate> ServiceDescriptionTemplates { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Existing configurations
            ConfigureUserEntity(modelBuilder);
            ConfigureShopEntity(modelBuilder);
            ConfigureServiceEntity(modelBuilder);
            ConfigureBookingEntity(modelBuilder);
            ConfigureStaffEntity(modelBuilder);
            ConfigureStaffSpecializationEntity(modelBuilder);
            ConfigureStaffWorkSessionEntity(modelBuilder);

            // OTP Management configuration
            ConfigureOtpEntity(modelBuilder);

            // Payment Management configuration
            ConfigurePaymentEntity(modelBuilder);

            // Coupon Management configuration
            ConfigureCouponUsageEntity(modelBuilder);
            ConfigureUserCouponUsageEntity(modelBuilder);

            // New configurations for service management
            ConfigureServiceCategoryEntity(modelBuilder);
            ConfigureServiceOfferEntity(modelBuilder);
            ConfigureServiceOfferItemEntity(modelBuilder);
            ConfigureServiceAvailabilityEntity(modelBuilder);

            // Service Suggestions configuration
            ConfigureServiceSuggestionEntities(modelBuilder);

            // KYC configuration
            ConfigureKycVerificationEntity(modelBuilder);
        }

        private void ConfigureOtpEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OtpEntity>()
                .HasKey(o => o.Id);

            // Index for email and purpose lookups
            modelBuilder.Entity<OtpEntity>()
                .HasIndex(o => new { o.Email, o.Purpose });

            // Index for cleanup operations
            modelBuilder.Entity<OtpEntity>()
                .HasIndex(o => o.ExpiresAt);

            // Index for rate limiting checks
            modelBuilder.Entity<OtpEntity>()
                .HasIndex(o => new { o.Email, o.Purpose, o.CreatedAt });

            // Unique constraint to prevent multiple active OTPs
            modelBuilder.Entity<OtpEntity>()
                .HasIndex(o => new { o.Email, o.Purpose, o.IsUsed })
                .HasFilter("IsUsed = 0 AND ExpiresAt > CURRENT_TIMESTAMP");
        }

        private void ConfigurePaymentEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>()
                .HasKey(p => p.Id);

            // Unique index for PaymentId
            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.PaymentId)
                .IsUnique();

            // Index for user payments lookup
            modelBuilder.Entity<Payment>()
                .HasIndex(p => new { p.UserId, p.Status });

            // Index for payment status and purpose (changed from PaymentType to Purpose)
            modelBuilder.Entity<Payment>()
                .HasIndex(p => new { p.Status, p.Purpose });

            // Index for cleanup of expired payments
            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.ExpiresAt);

            // Index for Razorpay order ID lookups
            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.RazorpayOrderId)
                .HasFilter("RazorpayOrderId IS NOT NULL");

            // Index for Razorpay payment ID lookups
            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.RazorpayPaymentId)
                .HasFilter("RazorpayPaymentId IS NOT NULL");
        }

        private void ConfigureCouponUsageEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CouponUsage>()
                .HasKey(cu => cu.Id);

            // Index for coupon code lookups
            modelBuilder.Entity<CouponUsage>()
                .HasIndex(cu => cu.CouponCode);

            // Index for user coupon usage lookups
            modelBuilder.Entity<CouponUsage>()
                .HasIndex(cu => new { cu.UserId, cu.CouponCode, cu.Purpose });

            // Index for payment reference lookups
            modelBuilder.Entity<CouponUsage>()
                .HasIndex(cu => cu.PaymentId)
                .HasFilter("PaymentId IS NOT NULL");

            // Index for status and date queries
            modelBuilder.Entity<CouponUsage>()
                .HasIndex(cu => new { cu.Status, cu.AppliedAt });

            // Index for soft delete queries
            modelBuilder.Entity<CouponUsage>()
                .HasIndex(cu => cu.IsDeleted);

            // Foreign key relationship with User
            modelBuilder.Entity<CouponUsage>()
                .HasOne(cu => cu.User)
                .WithMany()
                .HasForeignKey(cu => cu.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Decimal precision for amounts
            modelBuilder.Entity<CouponUsage>()
                .Property(cu => cu.OriginalAmount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<CouponUsage>()
                .Property(cu => cu.FinalAmount)
                .HasPrecision(10, 2);

            modelBuilder.Entity<CouponUsage>()
                .Property(cu => cu.Savings)
                .HasPrecision(10, 2);
        }

        private void ConfigureUserCouponUsageEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserCouponUsage>()
                .HasKey(ucu => ucu.Id);

            // Index for user and coupon lookups
            modelBuilder.Entity<UserCouponUsage>()
                .HasIndex(ucu => new { ucu.UserId, ucu.CouponCode });

            // Index for email and phone uniqueness checks
            modelBuilder.Entity<UserCouponUsage>()
                .HasIndex(ucu => new { ucu.Email, ucu.PhoneNumber, ucu.Purpose });

            // Index for email status queries
            modelBuilder.Entity<UserCouponUsage>()
                .HasIndex(ucu => new { ucu.Email, ucu.IsEmailSent });

            // Index for blocked users
            modelBuilder.Entity<UserCouponUsage>()
                .HasIndex(ucu => ucu.IsBlocked);

            // Index for soft delete queries
            modelBuilder.Entity<UserCouponUsage>()
                .HasIndex(ucu => ucu.IsDeleted);

            // Foreign key relationship with User
            modelBuilder.Entity<UserCouponUsage>()
                .HasOne(ucu => ucu.User)
                .WithMany()
                .HasForeignKey(ucu => ucu.UserId)
                .OnDelete(DeleteBehavior.Restrict);
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
                .HasIndex(u => u.AadhaarNumber)
                .IsUnique()
                .HasFilter("AadhaarNumber IS NOT NULL");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.PanNumber)
                .IsUnique()
                .HasFilter("PanNumber IS NOT NULL");

            modelBuilder.Entity<User>()
                .HasOne(u => u.StaffProfile)
                .WithOne(s => s.User)
                .HasForeignKey<Staff>(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure the relationship between User and Shop (owned shops)
            modelBuilder.Entity<User>()
                .HasMany(u => u.OwnedShops)
                .WithOne(s => s.Owner)
                .HasForeignKey(s => s.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ignore the Shops property since it's just an alias for OwnedShops
            modelBuilder.Entity<User>()
                .Ignore(u => u.Shops);
        }

        private void ConfigureShopEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Shop>()
                .HasKey(s => s.Id);

            // Index for location-based searches
            modelBuilder.Entity<Shop>()
                .HasIndex(s => new { s.Latitude, s.Longitude })
                .HasFilter("Latitude IS NOT NULL AND Longitude IS NOT NULL");

            // Index for shop status
            modelBuilder.Entity<Shop>()
                .HasIndex(s => s.IsActive);
        }

        private void ConfigureStaffEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Staff>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<Staff>()
                .HasOne(s => s.Shop)
                .WithMany()
                .HasForeignKey(s => s.ShopId)
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
                .HasIndex(b => new { b.ShopId, b.BookingDate });

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
                .HasOne(sc => sc.Shop)
                .WithMany()
                .HasForeignKey(sc => sc.ShopId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for shop categories lookup
            modelBuilder.Entity<ServiceCategory>()
                .HasIndex(sc => new { sc.ShopId, sc.IsActive });

            // Unique constraint for category name per shop
            modelBuilder.Entity<ServiceCategory>()
                .HasIndex(sc => new { sc.ShopId, sc.Name })
                .IsUnique()
                .HasFilter("IsDeleted = 0");
        }

        private void ConfigureServiceOfferEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ServiceOffer>()
                .HasKey(so => so.Id);

            modelBuilder.Entity<ServiceOffer>()
                .HasOne(so => so.Shop)
                .WithMany()
                .HasForeignKey(so => so.ShopId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for faster querying of active offers
            modelBuilder.Entity<ServiceOffer>()
                .HasIndex(so => new { so.ShopId, so.IsActive });

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
                .HasOne(s => s.Shop)
                .WithMany(s => s.Services)
                .HasForeignKey(s => s.ShopId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add new relationship to category
            modelBuilder.Entity<Service>()
                .HasOne(s => s.Category)
                .WithMany(c => c.Services)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Index for finding active services in a shop
            modelBuilder.Entity<Service>()
                .HasIndex(s => new { s.ShopId, s.IsActive });

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

        private void ConfigureServiceSuggestionEntities(ModelBuilder modelBuilder)
        {
            // ServiceNameSuggestion configuration
            modelBuilder.Entity<ServiceNameSuggestion>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<ServiceNameSuggestion>()
                .HasIndex(s => s.Category)
                .HasDatabaseName("IX_ServiceNameSuggestions_Category");

            modelBuilder.Entity<ServiceNameSuggestion>()
                .HasIndex(s => new { s.Category, s.ServiceName })
                .HasDatabaseName("IX_ServiceNameSuggestions_Category_ServiceName");

            modelBuilder.Entity<ServiceNameSuggestion>()
                .HasIndex(s => s.IsActive)
                .HasDatabaseName("IX_ServiceNameSuggestions_IsActive");

            modelBuilder.Entity<ServiceNameSuggestion>()
                .Property(s => s.Category)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<ServiceNameSuggestion>()
                .Property(s => s.ServiceName)
                .IsRequired()
                .HasMaxLength(200);

            modelBuilder.Entity<ServiceNameSuggestion>()
                .Property(s => s.ServiceName)
                .IsRequired()
                .HasMaxLength(200);

            // ServiceDescriptionTemplate configuration
            modelBuilder.Entity<ServiceDescriptionTemplate>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<ServiceDescriptionTemplate>()
                .HasIndex(t => t.Category)
                .HasDatabaseName("IX_ServiceDescriptionTemplates_Category");

            modelBuilder.Entity<ServiceDescriptionTemplate>()
                .HasIndex(t => new { t.Category, t.ServiceName })
                .HasDatabaseName("IX_ServiceDescriptionTemplates_Category_ServiceName");

            modelBuilder.Entity<ServiceDescriptionTemplate>()
                .HasIndex(t => t.IsActive)
                .HasDatabaseName("IX_ServiceDescriptionTemplates_IsActive");

            modelBuilder.Entity<ServiceDescriptionTemplate>()
                .Property(t => t.Category)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<ServiceDescriptionTemplate>()
                .Property(t => t.ServiceName)
                .HasMaxLength(200);

            modelBuilder.Entity<ServiceDescriptionTemplate>()
                .Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(2000);
        }

        private void ConfigureKycVerificationEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<KycVerification>()
                .HasKey(k => k.Id);

            // One-to-one relationship between User and KycVerification
            modelBuilder.Entity<KycVerification>()
                .HasOne(k => k.User)
                .WithOne(u => u.KycVerification)
                .HasForeignKey<KycVerification>(k => k.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for user lookups
            modelBuilder.Entity<KycVerification>()
                .HasIndex(k => k.UserId)
                .IsUnique();

            // Index for status queries
            modelBuilder.Entity<KycVerification>()
                .HasIndex(k => k.Status);

            // Index for document type queries
            modelBuilder.Entity<KycVerification>()
                .HasIndex(k => k.DocumentType);
        }
    }
}

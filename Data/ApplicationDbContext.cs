using Microsoft.EntityFrameworkCore;
using stibe.api.Models.Entities;

namespace stibe.api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Salon> Salons { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<StaffSpecialization> StaffSpecializations { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entity relationships and constraints
            ConfigureUserEntity(modelBuilder);
            ConfigureSalonEntity(modelBuilder);
            ConfigureServiceEntity(modelBuilder);
            ConfigureBookingEntity(modelBuilder);
            ConfigureStaffEntity(modelBuilder);
            ConfigureStaffSpecializationEntity(modelBuilder);
        }
        private void ConfigureStaffSpecializationEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StaffSpecialization>(entity =>
            {
                entity.HasIndex(e => new { e.StaffId, e.ServiceId }).IsUnique();

                entity.HasOne(e => e.Staff)
                      .WithMany(e => e.Specializations)
                      .HasForeignKey(e => e.StaffId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Service)
                      .WithMany()
                      .HasForeignKey(e => e.ServiceId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureUserEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.PhoneNumber).IsUnique();

                entity.HasMany(e => e.OwnedSalons)
                      .WithOne(e => e.Owner)
                      .HasForeignKey(e => e.OwnerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Bookings)
                      .WithOne(e => e.Customer)
                      .HasForeignKey(e => e.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Staff profile relationship
                entity.HasOne(e => e.StaffProfile)
                      .WithOne()
                      .HasForeignKey<Staff>("UserId")
                      .OnDelete(DeleteBehavior.Cascade);

                // Working salon relationship
                entity.HasOne(e => e.WorkingSalon)
                      .WithMany()
                      .HasForeignKey(e => e.SalonId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureSalonEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Salon>(entity =>
            {
                entity.HasMany(e => e.Services)
                      .WithOne(e => e.Salon)
                      .HasForeignKey(e => e.SalonId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Bookings)
                      .WithOne(e => e.Salon)
                      .HasForeignKey(e => e.SalonId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
        private void ConfigureStaffEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Staff>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.PhoneNumber);
                entity.HasIndex(e => new { e.SalonId, e.IsActive });

                entity.HasOne(e => e.Salon)
                      .WithMany()
                      .HasForeignKey(e => e.SalonId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.AssignedBookings)
                      .WithOne(e => e.AssignedStaff)
                      .HasForeignKey(e => e.AssignedStaffId)
                      .OnDelete(DeleteBehavior.SetNull);

                // User relationship
                entity.HasOne(e => e.User)
                      .WithOne(e => e.StaffProfile)
                      .HasForeignKey<Staff>(e => e.UserId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(e => e.Specializations)
                      .WithOne(e => e.Staff)
                      .HasForeignKey(e => e.StaffId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureServiceEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Service>(entity =>
            {
                entity.HasMany(e => e.Bookings)
                      .WithOne(e => e.Service)
                      .HasForeignKey(e => e.ServiceId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureBookingEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasIndex(e => new { e.SalonId, e.BookingDate, e.BookingTime });
                entity.HasIndex(e => e.CustomerId);
                entity.HasIndex(e => e.Status);
            });
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
            var entries = ChangeTracker.Entries<BaseEntity>();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RobotPlanet.Core.entities;
using RobotPlanet.Core.@interface;

namespace RobotPlanet.Wpf.Persistence
{
    /// <summary>
    /// EF Core DbContext mapping the Robot inheritance hierarchy using Table-Per-Hierarchy (TPH)
    /// into the existing SQL Server LocalDB schema, including Value Object conversion for RobotName.
    /// </summary>
    public sealed class RobotPlanetDbContext : DbContext
    {
        public DbSet<Robot> Robots => Set<Robot>();
        public DbSet<RobotGroupEntity> RobotGroups => Set<RobotGroupEntity>();
        public DbSet<WorldMetadataEntity> WorldMetadata => Set<WorldMetadataEntity>();

        public RobotPlanetDbContext(DbContextOptions<RobotPlanetDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var robotNameConverter = new ValueConverter<RobotName, string>(
                v => v.Value,
                v => RobotName.Create(v));

            // Lookup table for RobotGroup
            modelBuilder.Entity<RobotGroupEntity>(entity =>
            {
                entity.ToTable("EfRobotGroups");
                entity.HasKey(g => g.Id);
                entity.Property(g => g.Id).HasConversion<int>().ValueGeneratedNever();
                entity.Property(g => g.Name).HasMaxLength(20).IsRequired();
                entity.HasIndex(g => g.Name).IsUnique();
            });

            // World metadata table
            modelBuilder.Entity<WorldMetadataEntity>(entity =>
            {
                entity.ToTable("EfWorldInfo");
                entity.HasKey(w => w.Id);
                entity.Property(w => w.Id).ValueGeneratedNever();
                entity.Property(w => w.Version).IsRequired();
                entity.Property(w => w.SavedAtUtc).IsRequired();
            });

            // Table-Per-Hierarchy (TPH) inheritance mapping for all 8 Robot types
            modelBuilder.Entity<Robot>(entity =>
            {
                entity.ToTable("EfRobots", t =>
                {
                    t.HasCheckConstraint("CK_EfRobots_Battery", "[Battery] BETWEEN 0 AND 100");
                    t.HasCheckConstraint("CK_EfRobots_Integrity", "[Integrity] BETWEEN 0 AND 100");
                });

                entity.HasKey(r => r.Id);
                entity.Property(r => r.Id).ValueGeneratedNever();

                // Value Object mapping: RobotName <-> NVARCHAR(20)
                entity.Property(r => r.Name)
                    .HasConversion(robotNameConverter)
                    .HasMaxLength(RobotName.MaxLength)
                    .IsRequired();

                entity.Property(r => r.Battery).IsRequired();
                entity.Property(r => r.Integrity).IsRequired();

                // Group enum mapped as int FK to EfRobotGroups + shadow Position property to preserve list order
                entity.Property(r => r.Group)
                    .HasColumnName("GroupId")
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property<int>("Position").IsRequired();

                entity.HasOne<RobotGroupEntity>()
                    .WithMany()
                    .HasForeignKey(r => r.Group)
                    .OnDelete(DeleteBehavior.Restrict);

                // Ignore computed/UI-only properties
                entity.Ignore(r => r.Kind);
                entity.Ignore(r => r.IsOperational);

                // TPH Discriminator column mapping all 8 concrete subclasses
                entity.HasDiscriminator<string>("RobotType")
                    .HasValue<CleanerBot>(nameof(CleanerBot))
                    .HasValue<ExplorerBot>(nameof(ExplorerBot))
                    .HasValue<RepairBot>(nameof(RepairBot))
                    .HasValue<GuardBot>(nameof(GuardBot))
                    .HasValue<JesterBot>(nameof(JesterBot))
                    .HasValue<DeliveryBot>(nameof(DeliveryBot))
                    .HasValue<InspectorBot>(nameof(InspectorBot))
                    .HasValue<RescueBot>(nameof(RescueBot));

                entity.Property("RobotType").HasMaxLength(30).IsRequired();
            });
        }
    }
}

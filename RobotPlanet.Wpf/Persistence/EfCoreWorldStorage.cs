using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RobotPlanet.Core.entities;
using RobotPlanet.Core.@interface;
using RobotPlanet.Core.storage;
using RobotPlanet.Wpf.Properties;

namespace RobotPlanet.Wpf.Persistence
{
    /// <summary>
    /// Entity Framework Core storage provider using Table-Per-Hierarchy (TPH) inheritance mapping
    /// and explicit database transactions against SQL Server LocalDB.
    /// </summary>
    public sealed class EfCoreWorldStorage : IEntityWorldStorage
    {
        private readonly IDbContextFactory<RobotPlanetDbContext> _contextFactory;
        private bool _migrated;

        public EfCoreWorldStorage(IDbContextFactory<RobotPlanetDbContext> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public async Task SaveWorldAsync(IReadOnlyList<Robot> robots, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(robots);

            try
            {
                await EnsureMigratedAsync(token);

                await using RobotPlanetDbContext context = await _contextFactory.CreateDbContextAsync(token);
                await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(token);
                try
                {
                    // Clear existing state in FK-safe order inside a single transaction
                    await context.Robots.ExecuteDeleteAsync(token);
                    await context.RobotGroups.ExecuteDeleteAsync(token);
                    await context.WorldMetadata.ExecuteDeleteAsync(token);

                    // Persist normalized RobotGroup lookup rows for all defined groups
                    foreach (RobotGroup group in Enum.GetValues<RobotGroup>())
                    {
                        context.RobotGroups.Add(new RobotGroupEntity
                        {
                            Id = group,
                            Name = group.ToString()
                        });
                    }

                    // Clone detached domain entities for tracking so EF Core change tracker never interferes with live UI instances
                    for (int i = 0; i < robots.Count; i++)
                    {
                        Robot detachedCopy = CloneForPersistence(robots[i]);
                        context.Robots.Add(detachedCopy);
                        context.Entry(detachedCopy).Property("Position").CurrentValue = i;
                    }

                    context.WorldMetadata.Add(new WorldMetadataEntity
                    {
                        Id = 1,
                        Version = WorldDto.CurrentVersion,
                        SavedAtUtc = DateTime.UtcNow
                    });

                    await context.SaveChangesAsync(token);
                    await transaction.CommitAsync(token);
                }
                catch
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    throw;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception) when (token.IsCancellationRequested)
            {
                throw new OperationCanceledException(token);
            }
            catch (WorldStorageException)
            {
                throw;
            }
            catch (Exception ex) when (IsDatabaseError(ex))
            {
                throw new WorldStorageException(string.Format(Resources.EfCore_Failed, ex.Message), ex);
            }
        }

        public async Task<IReadOnlyList<Robot>> LoadWorldAsync(CancellationToken token = default)
        {
            try
            {
                await EnsureMigratedAsync(token);

                await using RobotPlanetDbContext context = await _contextFactory.CreateDbContextAsync(token);

                WorldMetadataEntity? meta = await context.WorldMetadata
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == 1, token);

                if (meta == null)
                    throw new WorldStorageException(Resources.EfCore_NoWorld);

                if (meta.Version != WorldDto.CurrentVersion)
                    throw new WorldStorageException(string.Format(Resources.EfCore_BadVersion, meta.Version, WorldDto.CurrentVersion));

                // EF Core TPH automatically materializes the exact concrete subclass (CleanerBot, GuardBot, etc.)
                List<Robot> loadedRobots = await context.Robots
                    .AsNoTracking()
                    .OrderBy(r => EF.Property<int>(r, "Position"))
                    .ThenBy(r => r.Id)
                    .ToListAsync(token);

                return loadedRobots;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception) when (token.IsCancellationRequested)
            {
                throw new OperationCanceledException(token);
            }
            catch (WorldStorageException)
            {
                throw;
            }
            catch (Exception ex) when (IsDatabaseError(ex))
            {
                throw new WorldStorageException(string.Format(Resources.EfCore_Failed, ex.Message), ex);
            }
        }

        // IWorldStorage fallback bridge (if invoked via DTO contract)
        public async Task SaveAsync(WorldDto world, string path, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(world);
            var list = new List<Robot>();
            if (world.Robots != null)
            {
                foreach (RobotDto dto in world.Robots)
                {
                    if (dto.Id is not int id || string.IsNullOrWhiteSpace(dto.Type) || dto.Name == null ||
                        dto.Battery is not int battery || dto.Integrity is not int integrity || string.IsNullOrWhiteSpace(dto.Group))
                    {
                        throw new WorldStorageException(Resources.EfCore_InvalidDto);
                    }

                    Robot entity = CreateByTypeName(dto.Type, id, dto.Name, battery, integrity);
                    if (Enum.TryParse<RobotGroup>(dto.Group, ignoreCase: true, out RobotGroup group) && Enum.IsDefined(group))
                        entity.ChangeGroup(group);

                    list.Add(entity);
                }
            }
            await SaveWorldAsync(list, token);
        }

        public async Task<WorldDto> LoadAsync(string path, CancellationToken token = default)
        {
            IReadOnlyList<Robot> robots = await LoadWorldAsync(token);
            return new WorldDto
            {
                Version = WorldDto.CurrentVersion,
                Robots = robots.Select(r => new RobotDto
                {
                    Id = r.Id,
                    Type = r.GetType().Name.Replace("Bot", string.Empty),
                    Name = r.Name.Value,
                    Battery = r.Battery,
                    Integrity = r.Integrity,
                    Group = r.Group.ToString()
                }).ToList()
            };
        }

        private async Task EnsureMigratedAsync(CancellationToken token)
        {
            if (_migrated) return;

            await using RobotPlanetDbContext context = await _contextFactory.CreateDbContextAsync(token);
            await context.Database.MigrateAsync(token);
            _migrated = true;
        }

        private static Robot CloneForPersistence(Robot source)
        {
            Robot clone = source switch
            {
                CleanerBot => new CleanerBot(source.Id, source.Name.Value, source.Battery, source.Integrity),
                ExplorerBot => new ExplorerBot(source.Id, source.Name.Value, source.Battery, source.Integrity),
                RepairBot => new RepairBot(source.Id, source.Name.Value, source.Battery, source.Integrity),
                GuardBot => new GuardBot(source.Id, source.Name.Value, source.Battery, source.Integrity),
                JesterBot => new JesterBot(source.Id, source.Name.Value, source.Battery, source.Integrity),
                DeliveryBot => new DeliveryBot(source.Id, source.Name.Value, source.Battery, source.Integrity),
                InspectorBot => new InspectorBot(source.Id, source.Name.Value, source.Battery, source.Integrity),
                RescueBot => new RescueBot(source.Id, source.Name.Value, source.Battery, source.Integrity),
                _ => throw new WorldStorageException(string.Format(Resources.EfCore_UnknownType, source.GetType().Name))
            };
            clone.ChangeGroup(source.Group);
            return clone;
        }

        private static Robot CreateByTypeName(string typeName, int id, string name, int battery, int integrity)
        {
            return typeName.Trim() switch
            {
                "Cleaner" or "CleanerBot" => new CleanerBot(id, name, battery, integrity),
                "Explorer" or "ExplorerBot" => new ExplorerBot(id, name, battery, integrity),
                "Repair" or "RepairBot" => new RepairBot(id, name, battery, integrity),
                "Guard" or "GuardBot" => new GuardBot(id, name, battery, integrity),
                "Jester" or "JesterBot" => new JesterBot(id, name, battery, integrity),
                "Delivery" or "DeliveryBot" => new DeliveryBot(id, name, battery, integrity),
                "Inspector" or "InspectorBot" => new InspectorBot(id, name, battery, integrity),
                "Rescue" or "RescueBot" => new RescueBot(id, name, battery, integrity),
                _ => throw new WorldStorageException(string.Format(Resources.EfCore_UnknownType, typeName))
            };
        }

        private static bool IsDatabaseError(Exception ex) =>
            ex is SqlException or DbUpdateException or InvalidOperationException;
    }
}

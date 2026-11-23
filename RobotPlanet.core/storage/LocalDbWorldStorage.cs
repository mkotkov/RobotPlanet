using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using RobotPlanet.Core.Properties;

namespace RobotPlanet.Core.storage
{
    /// <summary>
    /// Saves and loads the world in a SQL Server LocalDB database with ADO.NET.
    /// It works with the abstract classes of System.Data.Common only. The application gives it a factory that
    /// creates the real connection (new SqlConnection(...)), so Core needs no database package.
    ///
    /// Tables: RobotGroups, Robots (foreign key to RobotGroups) and WorldInfo (version of the data).
    /// Saving deletes the old rows and inserts the new ones in ONE transaction: it either all happens or nothing does.
    /// All values go through parameters. The "path" argument is not used: a database has its own location.
    /// </summary>
    public sealed class LocalDbWorldStorage : IWorldStorage
    {
        // The database name is put into CREATE DATABASE as an identifier (this cannot be a parameter),
        // so only plain names are accepted.
        private static readonly Regex DatabaseName = new("^[A-Za-z][A-Za-z0-9_]{0,63}$", RegexOptions.CultureInvariant);

        private readonly Func<string, DbConnection> _connectionFactory;
        private readonly string _connectionString;
        private bool _ready;

        public LocalDbWorldStorage(Func<string, DbConnection> connectionFactory, string connectionString)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _connectionString = string.IsNullOrWhiteSpace(connectionString)
                ? throw new ArgumentException(Resources.Db_NoConnectionString, nameof(connectionString))
                : connectionString;
        }

        // ------------------------------------------------------------------ save

        public async Task SaveAsync(WorldDto world, string path, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(world);
            List<RobotRow> rows = ToRows(world);                       // check the data before touching the database
            List<string> groups = rows.Select(r => r.Group).Distinct().ToList();

            try
            {
                await EnsureReadyAsync(token);

                await using DbConnection connection = await OpenAsync(_connectionString, token);
                await using DbTransaction transaction = await connection.BeginTransactionAsync(token);
                try
                {
                    // children first, because Robots.GroupId points to RobotGroups
                    await ExecuteAsync(connection, transaction, LocalDbSql.DeleteRobots, token);
                    await ExecuteAsync(connection, transaction, LocalDbSql.DeleteGroups, token);
                    await ExecuteAsync(connection, transaction, LocalDbSql.DeleteWorldInfo, token);

                    for (int i = 0; i < groups.Count; i++)
                    {
                        await ExecuteAsync(connection, transaction, LocalDbSql.InsertGroup, token,
                            Param("@Id", DbType.Int32, i + 1),
                            Param("@Name", DbType.String, groups[i]));
                    }

                    for (int i = 0; i < rows.Count; i++)
                    {
                        RobotRow row = rows[i];
                        await ExecuteAsync(connection, transaction, LocalDbSql.InsertRobot, token,
                            Param("@Id", DbType.Int32, row.Id),
                            Param("@Position", DbType.Int32, i),
                            Param("@Type", DbType.String, row.Type),
                            Param("@Name", DbType.String, row.Name),
                            Param("@Battery", DbType.Int32, row.Battery),
                            Param("@Integrity", DbType.Int32, row.Integrity),
                            Param("@GroupId", DbType.Int32, groups.IndexOf(row.Group) + 1));
                    }

                    await ExecuteAsync(connection, transaction, LocalDbSql.InsertWorldInfo, token,
                        Param("@Version", DbType.Int32, world.Version ?? WorldDto.CurrentVersion));

                    await transaction.CommitAsync(token);
                }
                catch
                {
                    await RollbackQuietlyAsync(transaction);
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
            catch (Exception ex) when (IsDatabaseProblem(ex))
            {
                throw new WorldStorageException(string.Format(Resources.Db_Failed, ex.Message), ex);
            }
        }

        // ------------------------------------------------------------------ load

        public async Task<WorldDto> LoadAsync(string path, CancellationToken token = default)
        {
            try
            {
                await EnsureReadyAsync(token);

                await using DbConnection connection = await OpenAsync(_connectionString, token);

                object? version = await ScalarAsync(connection, LocalDbSql.SelectVersion, token);
                if (version == null || version is DBNull)
                    throw new WorldStorageException(Resources.Db_NoWorld);

                var dto = new WorldDto { Version = Convert.ToInt32(version), Robots = new List<RobotDto>() };

                await using DbCommand command = connection.CreateCommand();
                command.CommandText = LocalDbSql.SelectRobots;
                await using DbDataReader reader = await command.ExecuteReaderAsync(token);
                while (await reader.ReadAsync(token))
                {
                    dto.Robots.Add(new RobotDto
                    {
                        Id = reader.GetInt32(0),
                        Type = reader.GetString(1),
                        Name = reader.GetString(2),
                        Battery = reader.GetInt32(3),
                        Integrity = reader.GetInt32(4),
                        Group = reader.GetString(5)
                    });
                }
                return dto;
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
            catch (Exception ex) when (IsDatabaseProblem(ex))
            {
                throw new WorldStorageException(string.Format(Resources.Db_Failed, ex.Message), ex);
            }
        }

        // ------------------------------------------------------------------ database and tables

        /// <summary>Makes sure the database and its tables exist. Checked once per run of the application.</summary>
        private async Task EnsureReadyAsync(CancellationToken token)
        {
            if (_ready) return;

            await CreateDatabaseIfMissingAsync(token);

            await using DbConnection connection = await OpenAsync(_connectionString, token);
            await ExecuteAsync(connection, null, LocalDbSql.CreateTables, token);
            _ready = true;
        }

        private async Task CreateDatabaseIfMissingAsync(CancellationToken token)
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = _connectionString };
            string? key = new[] { "Initial Catalog", "Database" }.FirstOrDefault(builder.ContainsKey);
            if (key == null) return;                                  // the connection string names no database

            string database = Convert.ToString(builder[key]) ?? string.Empty;
            if (!DatabaseName.IsMatch(database) || database.Equals("master", StringComparison.OrdinalIgnoreCase))
                throw new WorldStorageException(string.Format(Resources.Db_BadName, database));

            builder[key] = "master";                                  // the database cannot be created from inside itself
            await using DbConnection master = await OpenAsync(builder.ConnectionString, token);

            object? id = await ScalarAsync(master, LocalDbSql.DatabaseId, token, Param("@Name", DbType.String, database));
            if (id == null || id is DBNull)
                await ExecuteAsync(master, null, $"CREATE DATABASE [{database}];", token);
        }

        // ------------------------------------------------------------------ ADO.NET helpers

        private async Task<DbConnection> OpenAsync(string connectionString, CancellationToken token)
        {
            DbConnection connection = _connectionFactory(connectionString);
            try
            {
                await connection.OpenAsync(token);
                return connection;
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        private static async Task<int> ExecuteAsync(
            DbConnection connection, DbTransaction? transaction, string sql, CancellationToken token,
            params (string Name, DbType Type, object Value)[] parameters)
        {
            await using DbCommand command = CreateCommand(connection, transaction, sql, parameters);
            return await command.ExecuteNonQueryAsync(token);
        }

        private static async Task<object?> ScalarAsync(
            DbConnection connection, string sql, CancellationToken token,
            params (string Name, DbType Type, object Value)[] parameters)
        {
            await using DbCommand command = CreateCommand(connection, null, sql, parameters);
            return await command.ExecuteScalarAsync(token);
        }

        private static DbCommand CreateCommand(
            DbConnection connection, DbTransaction? transaction, string sql,
            (string Name, DbType Type, object Value)[] parameters)
        {
            DbCommand command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = transaction;
            foreach ((string name, DbType type, object value) in parameters)
            {
                DbParameter parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.DbType = type;
                parameter.Value = value;
                command.Parameters.Add(parameter);
            }
            return command;
        }

        private static (string Name, DbType Type, object Value) Param(string name, DbType type, object value) =>
            (name, type, value);

        private static async Task RollbackQuietlyAsync(DbTransaction transaction)
        {
            try { await transaction.RollbackAsync(CancellationToken.None); }
            catch (Exception ex) when (IsDatabaseProblem(ex)) { /* the connection is already broken: nothing more to undo */ }
        }

        private static bool IsDatabaseProblem(Exception ex) =>
            ex is DbException or InvalidOperationException or ArgumentException or FormatException;

        // ------------------------------------------------------------------ the data to save

        private sealed record RobotRow(int Id, string Type, string Name, int Battery, int Integrity, string Group);

        /// <summary>Checks that nothing is missing, so the database never gets half a robot.</summary>
        private static List<RobotRow> ToRows(WorldDto world)
        {
            if (world.Robots == null)
                throw new WorldStorageException(Resources.Load_NoRobots);

            var rows = new List<RobotRow>(world.Robots.Count);
            for (int i = 0; i < world.Robots.Count; i++)
            {
                int number = i + 1;
                RobotDto? r = world.Robots[i];
                if (r == null)
                    throw new WorldStorageException(string.Format(Resources.Load_RobotEmpty, number));

                rows.Add(new RobotRow(
                    r.Id ?? throw Missing(number, nameof(RobotDto.Id)),
                    r.Type ?? throw Missing(number, nameof(RobotDto.Type)),
                    r.Name ?? throw Missing(number, nameof(RobotDto.Name)),
                    r.Battery ?? throw Missing(number, nameof(RobotDto.Battery)),
                    r.Integrity ?? throw Missing(number, nameof(RobotDto.Integrity)),
                    r.Group ?? throw Missing(number, nameof(RobotDto.Group))));
            }
            return rows;

            static WorldStorageException Missing(int number, string field) =>
                new(string.Format(Resources.Load_FieldMissing, number, field));
        }
    }
}

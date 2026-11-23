namespace RobotPlanet.Core.storage
{
    /// <summary>
    /// Every SQL statement of the LocalDB storage. Values never appear in the text: they are always
    /// passed as parameters (@Id, @Name, ...), so a robot named "x'; DROP TABLE Robots;--" is just a name.
    /// The same table script is in database.sql, for creating the tables by hand.
    /// </summary>
    internal static class LocalDbSql
    {
        /// <summary>Creates the tables if they do not exist yet. Safe to run many times.</summary>
        public const string CreateTables = @"
IF OBJECT_ID(N'dbo.RobotGroups', N'U') IS NULL
    CREATE TABLE dbo.RobotGroups
    (
        Id   INT          NOT NULL CONSTRAINT PK_RobotGroups PRIMARY KEY,
        Name NVARCHAR(20) NOT NULL CONSTRAINT UQ_RobotGroups_Name UNIQUE
    );

IF OBJECT_ID(N'dbo.Robots', N'U') IS NULL
    CREATE TABLE dbo.Robots
    (
        Id        INT          NOT NULL CONSTRAINT PK_Robots PRIMARY KEY,
        Position  INT          NOT NULL,
        Type      NVARCHAR(30) NOT NULL,
        Name      NVARCHAR(20) NOT NULL,
        Battery   INT          NOT NULL CONSTRAINT CK_Robots_Battery CHECK (Battery BETWEEN 0 AND 100),
        Integrity INT          NOT NULL CONSTRAINT CK_Robots_Integrity CHECK (Integrity BETWEEN 0 AND 100),
        GroupId   INT          NOT NULL CONSTRAINT FK_Robots_RobotGroups REFERENCES dbo.RobotGroups (Id)
    );

IF OBJECT_ID(N'dbo.WorldInfo', N'U') IS NULL
    CREATE TABLE dbo.WorldInfo
    (
        Id         INT       NOT NULL CONSTRAINT PK_WorldInfo PRIMARY KEY CONSTRAINT CK_WorldInfo_Single CHECK (Id = 1),
        Version    INT       NOT NULL,
        SavedAtUtc DATETIME2 NOT NULL
    );";

        // ---- saving (all of these run inside ONE transaction) ----
        public const string DeleteRobots = "DELETE FROM dbo.Robots;";
        public const string DeleteGroups = "DELETE FROM dbo.RobotGroups;";
        public const string DeleteWorldInfo = "DELETE FROM dbo.WorldInfo;";

        public const string InsertGroup =
            "INSERT INTO dbo.RobotGroups (Id, Name) VALUES (@Id, @Name);";

        public const string InsertRobot =
            "INSERT INTO dbo.Robots (Id, Position, Type, Name, Battery, Integrity, GroupId) " +
            "VALUES (@Id, @Position, @Type, @Name, @Battery, @Integrity, @GroupId);";

        public const string InsertWorldInfo =
            "INSERT INTO dbo.WorldInfo (Id, Version, SavedAtUtc) VALUES (1, @Version, SYSUTCDATETIME());";

        // ---- loading ----
        public const string SelectVersion = "SELECT Version FROM dbo.WorldInfo WHERE Id = 1;";

        public const string SelectRobots =
            "SELECT r.Id, r.Type, r.Name, r.Battery, r.Integrity, g.Name AS GroupName " +
            "FROM dbo.Robots AS r INNER JOIN dbo.RobotGroups AS g ON g.Id = r.GroupId " +
            "ORDER BY r.Position;";

        // ---- creating the database itself (connected to master) ----
        public const string DatabaseId = "SELECT DB_ID(@Name);";
    }
}

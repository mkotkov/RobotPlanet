-- RobotPlanet: creating the SQL Server LocalDB database by hand.
-- The application does the same automatically on the first save or load, so this script is optional.
--
-- Run it once:
--     sqlcmd -S "(localdb)\MSSQLLocalDB" -i database.sql
-- or open it in Visual Studio (SQL Server Object Explorer -> (localdb)\MSSQLLocalDB -> New Query).

IF DB_ID(N'RobotPlanet') IS NULL
    CREATE DATABASE RobotPlanet;
GO

USE RobotPlanet;
GO

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
    );
GO

-- Look at the saved world:
SELECT * FROM dbo.WorldInfo;
SELECT r.Id, r.Position, r.Type, r.Name, r.Battery, r.Integrity, g.Name AS GroupName
FROM dbo.Robots AS r
INNER JOIN dbo.RobotGroups AS g ON g.Id = r.GroupId
ORDER BY r.Position;
GO

-- Start from a clean database (deletes the saved world):
-- DELETE FROM dbo.Robots; DELETE FROM dbo.RobotGroups; DELETE FROM dbo.WorldInfo;

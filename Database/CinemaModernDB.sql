/*
    Cinema - script de instalare pentru SQL Server / SSMS
    Instanță folosită de aplicație: .\SQLEXPRESS
    Bază de date: CinemaModernDB

    Scriptul este nedistructiv: creează doar obiectele care lipsesc.
    Datele inițiale sunt adăugate automat de aplicație la prima pornire.
*/

USE master;
GO

IF DB_ID(N'CinemaModernDB') IS NULL
    CREATE DATABASE CinemaModernDB;
GO

USE CinemaModernDB;
GO

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Users PRIMARY KEY,
        Username NVARCHAR(60) NOT NULL,
        FullName NVARCHAR(120) NOT NULL,
        PasswordHash VARBINARY(32) NOT NULL,
        PasswordSalt VARBINARY(16) NOT NULL,
        Role NVARCHAR(20) NOT NULL CONSTRAINT CK_Users_Role CHECK (Role IN ('Admin', 'User')),
        IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSDATETIME()
    );
    CREATE UNIQUE INDEX UX_Users_Username ON dbo.Users(Username);
END;
GO

IF OBJECT_ID('dbo.Films', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Films (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Films PRIMARY KEY,
        Title NVARCHAR(160) NOT NULL,
        Genre NVARCHAR(60) NOT NULL,
        ReleaseYear INT NOT NULL,
        Rating DECIMAL(3,1) NOT NULL,
        DurationMinutes INT NOT NULL,
        BasePrice DECIMAL(10,2) NOT NULL CONSTRAINT DF_Films_BasePrice DEFAULT 30,
        Description NVARCHAR(800) NOT NULL CONSTRAINT DF_Films_Description DEFAULT '',
        IsActive BIT NOT NULL CONSTRAINT DF_Films_IsActive DEFAULT 1
    );
    CREATE UNIQUE INDEX UX_Films_Title ON dbo.Films(Title);
END;
GO

IF OBJECT_ID('dbo.Rooms', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Rooms (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Rooms PRIMARY KEY,
        Name NVARCHAR(80) NOT NULL,
        Type NVARCHAR(40) NOT NULL,
        Floor INT NOT NULL,
        RowsCount INT NOT NULL,
        SeatsPerRow INT NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Rooms_IsActive DEFAULT 1
    );
    CREATE UNIQUE INDEX UX_Rooms_Name ON dbo.Rooms(Name);
END;
GO

IF OBJECT_ID('dbo.Actors', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Actors (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Actors PRIMARY KEY,
        Name NVARCHAR(120) NOT NULL,
        BirthYear INT NOT NULL,
        Nationality NVARCHAR(80) NOT NULL,
        FilmId INT NOT NULL,
        CONSTRAINT FK_Actors_Films FOREIGN KEY (FilmId) REFERENCES dbo.Films(Id)
    );
END;
GO

IF OBJECT_ID('dbo.Screenings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Screenings (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Screenings PRIMARY KEY,
        FilmId INT NOT NULL,
        RoomId INT NOT NULL,
        StartTime DATETIME2(0) NOT NULL,
        Price DECIMAL(10,2) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Screenings_IsActive DEFAULT 1,
        CONSTRAINT FK_Screenings_Films FOREIGN KEY (FilmId) REFERENCES dbo.Films(Id),
        CONSTRAINT FK_Screenings_Rooms FOREIGN KEY (RoomId) REFERENCES dbo.Rooms(Id)
    );
    CREATE UNIQUE INDEX UX_Screenings_Room_Start ON dbo.Screenings(RoomId, StartTime);
    CREATE INDEX IX_Screenings_Film_Start ON dbo.Screenings(FilmId, StartTime);
END;
GO

IF OBJECT_ID('dbo.Reservations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reservations (
        Id BIGINT IDENTITY(1,1) CONSTRAINT PK_Reservations PRIMARY KEY,
        BookingCode NVARCHAR(20) NOT NULL,
        UserId INT NOT NULL,
        ScreeningId INT NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Reservations_CreatedAt DEFAULT SYSDATETIME(),
        TotalAmount DECIMAL(10,2) NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Reservations_Status DEFAULT 'Confirmed'
            CONSTRAINT CK_Reservations_Status CHECK (Status IN ('Confirmed', 'Cancelled')),
        CONSTRAINT FK_Reservations_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_Reservations_Screenings FOREIGN KEY (ScreeningId) REFERENCES dbo.Screenings(Id)
    );
    CREATE UNIQUE INDEX UX_Reservations_BookingCode ON dbo.Reservations(BookingCode);
END;
GO

IF OBJECT_ID('dbo.ReservationSeats', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReservationSeats (
        Id BIGINT IDENTITY(1,1) CONSTRAINT PK_ReservationSeats PRIMARY KEY,
        ReservationId BIGINT NOT NULL,
        ScreeningId INT NOT NULL,
        SeatCode NVARCHAR(12) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ReservationSeats_IsActive DEFAULT 1,
        CONSTRAINT FK_ReservationSeats_Reservations FOREIGN KEY (ReservationId)
            REFERENCES dbo.Reservations(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ReservationSeats_Screenings FOREIGN KEY (ScreeningId)
            REFERENCES dbo.Screenings(Id)
    );
END;
GO

IF COL_LENGTH('dbo.ReservationSeats', 'IsActive') IS NULL
    ALTER TABLE dbo.ReservationSeats ADD IsActive BIT NOT NULL
        CONSTRAINT DF_ReservationSeats_IsActive DEFAULT 1;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.ReservationSeats') AND name='UX_ReservationSeats_Screening_Seat')
    DROP INDEX UX_ReservationSeats_Screening_Seat ON dbo.ReservationSeats;
GO

CREATE UNIQUE INDEX UX_ReservationSeats_Screening_Seat
    ON dbo.ReservationSeats(ScreeningId, SeatCode) WHERE IsActive=1;
GO

IF OBJECT_ID('dbo.LegacyTickets', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LegacyTickets (
        Id BIGINT IDENTITY(1,1) CONSTRAINT PK_LegacyTickets PRIMARY KEY,
        LegacyId BIGINT NOT NULL,
        LegacyFilmId INT NOT NULL,
        LegacyRoomId INT NOT NULL,
        ShowTime NVARCHAR(20) NOT NULL,
        PurchaseDate NVARCHAR(40) NOT NULL,
        Seats NVARCHAR(500) NOT NULL,
        Source NVARCHAR(260) NOT NULL,
        ImportedAt DATETIME2(0) NOT NULL CONSTRAINT DF_LegacyTickets_ImportedAt DEFAULT SYSDATETIME()
    );
    CREATE UNIQUE INDEX UX_LegacyTickets_Source_Id ON dbo.LegacyTickets(Source, LegacyId);
END;
GO

SELECT
    DB_NAME() AS DatabaseName,
    (SELECT COUNT(*) FROM sys.tables) AS TableCount,
    @@SERVERNAME AS SqlServerInstance;
GO

/* Checks if the database named RaceDayDB is there, if not it creates one*/
IF DB_ID('RaceDayDB') IS NULL
BEGIN
     CREATE DATABASE RaceDayDB;
END

/*WHEN CREATED IT USES IT*/
USE RaceDayDB;

/*First table we create is the roles table*/
CREATE TABLE dbo.Roles (
    RoleId      INT IDENTITY(1,1) NOT NULL,
    RoleName    VARCHAR(20)       NOT NULL,
    CONSTRAINT PK_Roles PRIMARY KEY (RoleId),
    CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName)
);

/*Second table we create is the Users table*/
CREATE TABLE dbo.Users (
    UserId          INT IDENTITY(1,1) NOT NULL,
    RoleId          INT               NOT NULL,
    FirstName       VARCHAR(50)       NOT NULL,
    LastName        VARCHAR(50)       NOT NULL,
    Email           VARCHAR(100)      NOT NULL,
    PasswordHash    VARCHAR(255)      NOT NULL,
    PhoneNumber     VARCHAR(20)       NULL,
    RegisteredAt    DATETIME          NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId)
        REFERENCES dbo.Roles (RoleId) ON DELETE NO ACTION
);

/*Third table we create is the Events table*/
CREATE TABLE dbo.Events (
    EventId         INT IDENTITY(1,1) NOT NULL,
    OrganiserId     INT               NOT NULL,
    Name            VARCHAR(100)      NOT NULL,
    Description     VARCHAR(500)      NULL,
    EventDate       DATETIME          NOT NULL,
    Location        VARCHAR(150)      NOT NULL,
    Distance        DECIMAL(6,2)      NOT NULL,
    EventType       VARCHAR(10)       NOT NULL,
    CreatedAt       DATETIME          NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Events PRIMARY KEY (EventId),
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserId)
        REFERENCES dbo.Users (UserId) ON DELETE NO ACTION,
    CONSTRAINT CK_Events_EventType CHECK (EventType IN ('Run', 'Walk', 'Cycle')),
    CONSTRAINT CK_Events_Distance CHECK (Distance > 0)
);

/*4th table we create is the Categories table*/
CREATE TABLE dbo.Categories (
    CategoryId      INT IDENTITY(1,1) NOT NULL,
    EventId         INT               NOT NULL,
    Name            VARCHAR(50)       NOT NULL,
    Description     VARCHAR(200)      NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId)
        REFERENCES dbo.Events (EventId) ON DELETE NO ACTION,
    CONSTRAINT UQ_Categories_Event_Name UNIQUE (EventId, Name)
);

/*5th table we create is the Enrolment table, which is an associative entity that resolves 
the many to many relationship between Participants and Events, Category is an attribute of that association.*/
CREATE TABLE dbo.Enrolments (
    EnrolmentId     INT IDENTITY(1,1) NOT NULL,
    ParticipantId   INT               NOT NULL,
    EventId         INT               NOT NULL,
    CategoryId      INT               NOT NULL,
    EnrolmentDate   DATETIME          NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Enrolments PRIMARY KEY (EnrolmentId),
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantId)
        REFERENCES dbo.Users (UserId) ON DELETE NO ACTION,
    CONSTRAINT FK_Enrolments_Events FOREIGN KEY (EventId)
        REFERENCES dbo.Events (EventId) ON DELETE NO ACTION,
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId)
        REFERENCES dbo.Categories (CategoryId) ON DELETE NO ACTION,
    CONSTRAINT UQ_Enrolments_Participant_Event UNIQUE (ParticipantId, EventId)
);

/*6th and last table we create is the Results table*/
CREATE TABLE dbo.Results (
    ResultId        INT IDENTITY(1,1) NOT NULL,
    EnrolmentId     INT               NOT NULL,
    FinishTime      TIME              NULL,
    FinishPosition  INT               NULL,
    CapturedAt      DATETIME          NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Results PRIMARY KEY (ResultId),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId)
        REFERENCES dbo.Enrolments (EnrolmentId) ON DELETE NO ACTION,
    CONSTRAINT UQ_Results_Enrolment UNIQUE (EnrolmentId),
    CONSTRAINT CK_Results_FinishPosition CHECK (FinishPosition IS NULL OR FinishPosition > 0)
);
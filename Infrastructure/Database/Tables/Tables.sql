CREATE TABLE Petition (
    PetitionId INT IDENTITY(1,1) PRIMARY KEY,
    PetitionSeq NVARCHAR(14) UNIQUE NOT NULL,
    WorldId TINYINT NOT NULL,
    Category TINYINT NOT NULL,
    State TINYINT NOT NULL,
    Grade TINYINT NOT NULL DEFAULT 1,
    Flag TINYINT NOT NULL DEFAULT 0,
    Content NVARCHAR(255) NOT NULL,
    SubmitTime DATETIME NOT NULL,
    QuotaAtSubmit TINYINT NOT NULL,
    CheckOutTime DATETIME NULL,
    LastModifiedTime DATETIME NULL,
    -- User info
    AccountName NVARCHAR(14) NOT NULL,
    AccountUid INT NOT NULL,
    CharName NVARCHAR(16) NOT NULL,
    CharUid INT NOT NULL,
    -- Forced GM
    ForcedGmAccountName NVARCHAR(14) NULL,
    ForcedGmAccountUid INT NULL,
    ForcedGmCharName NVARCHAR(16) NULL,
    ForcedGmCharUid INT NULL,
    -- Assigned GM
    AssignedGmAccountName NVARCHAR(14) NULL,
    AssignedGmAccountUid INT NULL,
    AssignedGmCharName NVARCHAR(16) NULL,
    AssignedGmCharUid INT NULL,
    -- Checkout GM
    CheckOutGmAccountName NVARCHAR(14) NULL,
    CheckOutGmAccountUid INT NULL,
    CheckOutGmCharName NVARCHAR(16) NULL,
    CheckOutGmCharUid INT NULL
);

CREATE TABLE PetitionHistory (
    PetitionSeq NVARCHAR(14) NOT NULL,
    HistorySeq INT NOT NULL,
    Actor NVARCHAR(16) NOT NULL,
    ActionCode TINYINT NOT NULL,
    ActionTime DATETIME NOT NULL,
    Message NVARCHAR(1000) NULL,
    PRIMARY KEY (PetitionSeq, HistorySeq)
);

CREATE TABLE PetitionMemo (
    PetitionSeq NVARCHAR(14) NOT NULL,
    MemoSeq INT NOT NULL,
    Writer NVARCHAR(16) NOT NULL,
    Content NVARCHAR(1000) NOT NULL,
    WriteTime DATETIME NOT NULL,
    PRIMARY KEY (PetitionSeq, MemoSeq)
);

CREATE TABLE PetitionChat (
    PetitionSeq NVARCHAR(14) NOT NULL,
    ChatSeq INT NOT NULL,
    Talker NVARCHAR(16) NOT NULL,
    Message NVARCHAR(255) NOT NULL,
    ChatTime DATETIME NOT NULL,
    PRIMARY KEY (PetitionSeq, ChatSeq)
);

CREATE TABLE PetitionL2Info (
    PetitionSeq NVARCHAR(14) PRIMARY KEY,
    Race INT NOT NULL,
    Class INT NOT NULL,
    [Level] INT NOT NULL,
    Disposition INT NOT NULL,
    SsPosition INT NOT NULL,
    NewChar INT NOT NULL,
    Coordinate NVARCHAR(255) NOT NULL
);

-- Base tables
CREATE TABLE [dbo].[Account] (
    [AccountUid] INT PRIMARY KEY,
    [AccountName] NVARCHAR(14) NOT NULL UNIQUE,
    [Grade] TINYINT NOT NULL DEFAULT 0
);

CREATE TABLE [dbo].[World] (
    [WorldId] TINYINT PRIMARY KEY,
    [WorldName] NVARCHAR(255) NOT NULL,
    [Status] TINYINT NOT NULL DEFAULT 0
);

CREATE TABLE [dbo].[Category] (
    [CategoryId] TINYINT PRIMARY KEY,
    [Name] NVARCHAR(50) NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1
);

-- Quota tracking
CREATE TABLE [dbo].[AccountQuota] (
    [AccountUid] INT PRIMARY KEY,
    [CurrentQuota] INT NOT NULL DEFAULT 0,
    [LastUpdateTime] DATETIME NOT NULL,
    CONSTRAINT [FK_AccountQuota_Account] FOREIGN KEY ([AccountUid]) 
        REFERENCES [dbo].[Account]([AccountUid])
);

-- Template management
CREATE TABLE [dbo].[Template] (
    [TemplateId] INT IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(20) NOT NULL,
    [Type] TINYINT NOT NULL,
    [Content] NVARCHAR(500) NOT NULL,
    [Category] TINYINT NOT NULL,
    [SortOrder] INT NOT NULL,
    [OwnerAccountUid] INT NOT NULL,
    [IsPublic] BIT NOT NULL DEFAULT 0,
    [CreatedBy] NVARCHAR(14) NOT NULL,
    [CreatedTime] DATETIME NOT NULL,
    [ModifiedBy] NVARCHAR(14) NOT NULL,
    [ModifiedTime] DATETIME NOT NULL,
    CONSTRAINT [FK_Template_Account] FOREIGN KEY ([OwnerAccountUid]) 
        REFERENCES [dbo].[Account]([AccountUid]),
    CONSTRAINT [FK_Template_Category] FOREIGN KEY ([Category]) 
        REFERENCES [dbo].[Category]([CategoryId])
);

-- Petition management
CREATE TABLE [dbo].[Petition] (
    [PetitionId] INT IDENTITY(1,1) PRIMARY KEY,
    [PetitionSeq] NVARCHAR(14) NOT NULL UNIQUE,
    [WorldId] TINYINT NOT NULL,
    [Category] TINYINT NOT NULL,
    [State] TINYINT NOT NULL,
    [Grade] TINYINT NOT NULL,
    [Flag] TINYINT NOT NULL DEFAULT 0,
    [Content] NVARCHAR(255) NOT NULL,
    [SubmitTime] DATETIME NOT NULL,
    [QuotaAtSubmit] TINYINT NOT NULL,
    [UserAccountUid] INT NOT NULL,
    [UserCharName] NVARCHAR(16) NOT NULL,
    [UserCharUid] INT NOT NULL,
    [AssignedGmAccountUid] INT NULL,
    [AssignedGmCharName] NVARCHAR(16) NULL,
    [CheckOutTime] DATETIME NULL,
    CONSTRAINT [FK_Petition_World] FOREIGN KEY ([WorldId]) 
        REFERENCES [dbo].[World]([WorldId]),
    CONSTRAINT [FK_Petition_Category] FOREIGN KEY ([Category]) 
        REFERENCES [dbo].[Category]([CategoryId]),
    CONSTRAINT [FK_Petition_UserAccount] FOREIGN KEY ([UserAccountUid]) 
        REFERENCES [dbo].[Account]([AccountUid]),
    CONSTRAINT [FK_Petition_GmAccount] FOREIGN KEY ([AssignedGmAccountUid]) 
        REFERENCES [dbo].[Account]([AccountUid])
);

-- Petition history
CREATE TABLE [dbo].[PetitionHistory] (
    [HistoryId] INT IDENTITY(1,1) PRIMARY KEY,
    [PetitionId] INT NOT NULL,
    [ActionTime] DATETIME NOT NULL,
    [Actor] NVARCHAR(16) NOT NULL,
    [ActionCode] TINYINT NOT NULL,
    [Message] NVARCHAR(1000) NULL,
    CONSTRAINT [FK_PetitionHistory_Petition] FOREIGN KEY ([PetitionId]) 
        REFERENCES [dbo].[Petition]([PetitionId])
);

-- GM Status tracking
CREATE TABLE [dbo].[GmStatus] (
    [StatusId] INT IDENTITY(1,1) PRIMARY KEY,
    [WorldId] TINYINT NOT NULL,
    [GmCharName] NVARCHAR(16) NOT NULL,
    [LoginTime] DATETIME NOT NULL,
    [LogoutTime] DATETIME NULL,
    CONSTRAINT [FK_GmStatus_World] FOREIGN KEY ([WorldId]) 
        REFERENCES [dbo].[World]([WorldId])
);

-- L2 Info
CREATE TABLE [dbo].[L2Info] (
    [PetitionId] INT PRIMARY KEY,
    [Race] INT NOT NULL,
    [Class] INT NOT NULL,
    [Level] INT NOT NULL,
    [Disposition] INT NOT NULL,
    [SsPosition] INT NOT NULL,
    [NewChar] INT NOT NULL,
    [Coordinate] NVARCHAR(255) NOT NULL,
    CONSTRAINT [FK_L2Info_Petition] FOREIGN KEY ([PetitionId]) 
        REFERENCES [dbo].[Petition]([PetitionId])
);
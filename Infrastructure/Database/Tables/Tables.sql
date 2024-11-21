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
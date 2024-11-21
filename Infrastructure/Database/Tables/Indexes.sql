-- Petition table indexes
CREATE NONCLUSTERED INDEX IX_Petition_WorldId_State 
ON Petition(WorldId, State)
INCLUDE (PetitionSeq, SubmitTime);

CREATE NONCLUSTERED INDEX IX_Petition_AccountUid 
ON Petition(AccountUid)
INCLUDE (State, SubmitTime);

CREATE NONCLUSTERED INDEX IX_Petition_CharUid 
ON Petition(CharUid)
WHERE State IN (2, 3, 4, 7, 10, 11); -- Active states only

CREATE NONCLUSTERED INDEX IX_Petition_PetitionSeq 
ON Petition(PetitionSeq)
INCLUDE (State, WorldId);

CREATE NONCLUSTERED INDEX IX_Petition_AssignedGmCharUid
ON Petition(AssignedGmCharUid)
WHERE AssignedGmCharUid IS NOT NULL;

-- PetitionHistory table indexes
CREATE NONCLUSTERED INDEX IX_PetitionHistory_ActionTime
ON PetitionHistory(PetitionSeq, ActionTime)
INCLUDE (Actor, ActionCode);

-- PetitionMemo table indexes
CREATE NONCLUSTERED INDEX IX_PetitionMemo_WriteTime
ON PetitionMemo(PetitionSeq, WriteTime)
INCLUDE (Writer);

-- PetitionChat table indexes
CREATE NONCLUSTERED INDEX IX_PetitionChat_ChatTime
ON PetitionChat(PetitionSeq, ChatTime)
INCLUDE (Talker);

-- Foreign Key Constraints
ALTER TABLE PetitionHistory
ADD CONSTRAINT FK_PetitionHistory_Petition
FOREIGN KEY (PetitionSeq) 
REFERENCES Petition(PetitionSeq)
ON DELETE CASCADE;

ALTER TABLE PetitionMemo
ADD CONSTRAINT FK_PetitionMemo_Petition
FOREIGN KEY (PetitionSeq) 
REFERENCES Petition(PetitionSeq)
ON DELETE CASCADE;

ALTER TABLE PetitionChat
ADD CONSTRAINT FK_PetitionChat_Petition
FOREIGN KEY (PetitionSeq) 
REFERENCES Petition(PetitionSeq)
ON DELETE CASCADE;

ALTER TABLE PetitionL2Info
ADD CONSTRAINT FK_PetitionL2Info_Petition
FOREIGN KEY (PetitionSeq) 
REFERENCES Petition(PetitionSeq)
ON DELETE CASCADE;

-- Add supporting tables and their indexes
CREATE TABLE Category (
    CategoryId TINYINT PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    SortOrder INT NOT NULL
);

CREATE TABLE GmAccount (
    AccountUid INT PRIMARY KEY,
    Grade TINYINT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    LastLoginDate DATETIME NULL
);

CREATE TABLE Account (
    AccountUid INT PRIMARY KEY IDENTITY(1,1),
    AccountName NVARCHAR(14) NOT NULL UNIQUE,
    Password NVARCHAR(16) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    LastLoginDate DATETIME NULL
);

-- Helpful statistics
CREATE STATISTICS STAT_Petition_State_WorldId 
ON Petition(State, WorldId);

CREATE STATISTICS STAT_Petition_AssignedGm_State
ON Petition(AssignedGmCharUid, State);

-- Create a view for active petitions summary
GO
CREATE VIEW vw_ActivePetitions
AS
SELECT 
    p.WorldId,
    p.State,
    COUNT(*) as PetitionCount,
    MIN(p.SubmitTime) as OldestPetition,
    MAX(p.SubmitTime) as NewestPetition
FROM Petition p
WHERE p.State IN (2, 3, 4, 7, 10, 11) -- Active states
GROUP BY p.WorldId, p.State;
GO

-- Create a view for GM workload
CREATE VIEW vw_GmWorkload
AS
SELECT 
    p.AssignedGmCharUid,
    p.AssignedGmCharName,
    COUNT(*) as AssignedPetitions,
    COUNT(CASE WHEN p.State = 3 THEN 1 END) as CheckedOutPetitions,
    MIN(p.SubmitTime) as OldestPetition
FROM Petition p
WHERE p.AssignedGmCharUid IS NOT NULL
    AND p.State IN (2, 3, 4, 7, 10, 11)
GROUP BY p.AssignedGmCharUid, p.AssignedGmCharName;
GO
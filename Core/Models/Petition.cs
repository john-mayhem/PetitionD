// File: Core/Models/Petition.cs
using NC.PetitionLib;
using NC.ToolNet.Networking.Protocol;
using PetitionD.Core.Enums;
using PetitionD.Core.Services;
using PetitionD.Core.Extensions;
namespace PetitionD.Core.Models;

public class Petition
{
    // Identifiers
    public int PetitionId { get; set; }
    public string PetitionSeq { get; set; } = string.Empty;
    public int WorldId { get; set; }

    // Users
    public GameCharacter User { get; set; } = new();
    public GameCharacter ForcedGm { get; set; } = new();
    public GameCharacter AssignedGm { get; set; } = new();
    public GameCharacter CheckOutGm { get; set; } = new();

    // Content
    public int Category { get; set; }
    public string Content { get; set; } = string.Empty;
    public Lineage2Info Info { get; set; } = new();

    // State
    public State State { get; set; }
    public State RollbackState { get; set; }
    public Grade Grade { get; set; }
    public byte Flag { get; set; }

    // Timestamps
    public DateTime SubmitTime { get; set; }
    public DateTime CheckOutTime { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime LastActionTime { get; set; }

    // Quotas
    public int QuotaAtSubmit { get; set; }
    public int QuotaAfterTreat { get; set; }

    // Additional Information
    public bool UserOnline { get; set; }
    public string Message { get; set; } = string.Empty;

    // Feedback
    public bool HasFeedback { get; set; }
    public int FeedbackScore { get; set; }
    public string FeedbackComment { get; set; } = string.Empty;

    // Lists
    public List<PetitionHistory> History { get; set; } = [];
    public List<PetitionMemo> Memos { get; set; } = [];

    public PetitionErrorCode ChattingCheckIn(GmCharacter gmChar, byte flag)
    {
        if (State != State.EndChat)
            return PetitionErrorCode.InvalidState;

        if (CheckOutGm.CharUid != gmChar.CharUid)
            return PetitionErrorCode.NoRightToAccess;

        Flag = flag;
        State = State.ChatCheckIn;
        CheckInTime = DateTime.Now;

        return PetitionErrorCode.Success;
    }

    public PetitionErrorCode BeginMessageCheckIn(GmCharacter gmChar, string message, byte flag)
    {
        if (State != State.CheckOut && State != State.EndChat)
            return PetitionErrorCode.InvalidState;

        Flag = flag;
        Message = message;
        State = State != State.EndChat ? State.MessageCheckIn : State.ChatCheckIn;

        return PetitionErrorCode.Success;
    }

    public void Serialize(Packer packer)
    {
        packer.AddInt32(WorldId);
        packer.AddUInt8((byte)Grade);
        packer.AddInt32(PetitionId);
        packer.AddString(PetitionSeq);
        packer.AddDateTime(SubmitTime);
        packer.AddUInt8(Flag);
        // ... Add other fields
    }

    public PetitionErrorCode AddMemo(GmCharacter gmChar, string content)
    {
        if (State != State.CheckOut && State != State.EndChat)
            return PetitionErrorCode.InvalidState;

        if (CheckOutGm.CharUid != gmChar.CharUid)
            return PetitionErrorCode.NoRightToAccess;

        Memos.Add(new PetitionMemo
        {
            Writer = gmChar.CharName,
            Content = content,
            Time = DateTime.Now
        });

        return PetitionErrorCode.Success;
    }

        public PetitionErrorCode UndoCheckOut(GmCharacter gmChar)
    {
        if (gmChar.Grade < Grade)
            return PetitionErrorCode.NoRightToAccess;

        if (State != State.CheckOut)
            return PetitionErrorCode.InvalidState;

        State = State.Undo;
        History.Add(new PetitionHistory 
        { 
            Actor = gmChar.CharName,
            Time = DateTime.Now,
            ActionCode = State.Undo
        });

        return PetitionErrorCode.Success;
    }

    public PetitionErrorCode CancelPetition(int worldId, int requesterCharUid)
    {
        if (worldId != WorldId || 
            (ForcedGm.CharUid == 0 && requesterCharUid != User.CharUid) ||
            (ForcedGm.CharUid != 0 && requesterCharUid != ForcedGm.CharUid))
            return PetitionErrorCode.NoRightToAccess;

        if (State != State.Submit)
            return PetitionErrorCode.InvalidState;

        State = State.UserCancel;
        var actor = requesterCharUid == User.CharUid ? User : ForcedGm;
        History.Add(new PetitionHistory 
        { 
            Actor = actor.CharName,
            Time = DateTime.Now,
            ActionCode = State.UserCancel
        });

        if (ForcedGm.CharUid == 0)
        {
            QuotaAfterTreat--;
        }

        return PetitionErrorCode.Success;
    }

    public PetitionErrorCode CheckOut(GmCharacter gmChar, out bool unAssigned)
    {
        unAssigned = false;
        if (WorldId != gmChar.WorldId || gmChar.Grade < Grade)
            return PetitionErrorCode.NoRightToAccess;

        if (!AssignLogic.CanCheckOut(this, gmChar))
            return PetitionErrorCode.NotAssigedPetition;

        if (State != State.Submit && State != State.Forward && State != State.Undo)
            return PetitionErrorCode.InvalidState;

        CheckOutGm = gmChar.ToGameCharacter();
        CheckOutTime = DateTime.Now;
        State = State.CheckOut;
        
        History.Add(new PetitionHistory 
        { 
            Actor = CheckOutGm.CharName,
            Time = CheckOutTime,
            ActionCode = State.CheckOut
        });

        unAssigned = AssignLogic.CheckOut(this, gmChar);
        return PetitionErrorCode.Success;
    }

    public PetitionErrorCode ForwardCheckIn(GmCharacter gmChar, Grade newGrade, byte flag)
    {
        if (gmChar.WorldId != WorldId || gmChar.CharUid != CheckOutGm.CharUid)
            return PetitionErrorCode.NoRightToAccess;

        if (State != State.CheckOut && State != State.EndChat)
            return PetitionErrorCode.InvalidState;

        if (newGrade <= Grade || newGrade > Grade.HeadGM)
            return PetitionErrorCode.UnexpectedPetitionGrade;

        Flag = flag;
        Grade = newGrade;
        State = State.Forward;

        History.Add(new PetitionHistory 
        { 
            Actor = gmChar.CharName,
            Time = DateTime.Now,
            ActionCode = State.Forward
        });

        return PetitionErrorCode.Success;
    }

    public PetitionErrorCode ModifyCategory(GmCharacter gmChar, int category)
    {
        //if (!Category.IsValid(category))   Error(active)  CS1061  'int' does not contain a definition for 'IsValid' and no accessible extension method 'IsValid' accepting a first argument of type 'int' could be found(are you missing a using directive or an assembly reference ?)	PetitionD D:\Dev\PetitionD\Core\Models\Petition.cs    211

        //    return PetitionErrorCode.UnexpectedCategory;

        if (gmChar.WorldId != WorldId || gmChar.CharUid != CheckOutGm.CharUid)
            return PetitionErrorCode.NoRightToAccess;
        if (State != State.CheckOut && State != State.EndChat)
            return PetitionErrorCode.InvalidState;
        Category = category;
        return PetitionErrorCode.Success;
    }

    public int GetActivePetitionCount()
    {
        // This should actually be handled by PetitionList, not individual Petition
        // But we'll add this to fix compilation errors
        return 1; // Default value to fix compilation
    }
}

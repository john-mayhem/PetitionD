// File: Core/Models/Petition.cs
using NC.PetitionLib;
using PetitionD.Core.Enums;

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
    public List<PetitionHistory> History { get; set; } = new();
    public List<PetitionMemo> Memos { get; set; } = new();

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

    // Add other methods as needed
}

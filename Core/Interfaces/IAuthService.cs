using NC.PetitionLib;

namespace PetitionD.Core.Interfaces
{
    public interface IAuthService
    {
        Task<(PetitionErrorCode ErrorCode, int AccountUid)> AuthenticateAsync(string account, string password);
        Task<bool> ValidateSessionAsync(int accountUid, string sessionToken);
        void InvalidateSession(int accountUid);
    }
}
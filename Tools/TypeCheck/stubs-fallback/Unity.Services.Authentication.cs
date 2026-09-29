// STUB of com.unity.services.authentication 3.6.1 - namespace Unity.Services.Authentication. Compile-only.
// Members marked // GUESS were not verified against the package source.
using System;
using System.Threading.Tasks;
using Unity.Services.Core;

namespace Unity.Services.Authentication
{
    public static class AuthenticationService
    {
        public static IAuthenticationService Instance => throw null;
    }

    public interface IAuthenticationService
    {
        event Action<RequestFailedException> SignInFailed;
        event Action SignedIn;
        event Action SignedOut;
        event Action Expired;
        event Action<string> SignInCodeReceived; // GUESS
        bool IsSignedIn { get; }
        bool IsAuthorized { get; }
        bool IsExpired { get; }
        bool SessionTokenExists { get; }
        string AccessToken { get; }
        string PlayerId { get; }
        string PlayerName { get; }
        string Profile { get; }
        PlayerInfo PlayerInfo { get; }
        Task SignInAnonymouslyAsync(SignInOptions options = null);
        Task SignInWithAppleAsync(string idToken, SignInOptions options = null);
        Task LinkWithAppleAsync(string idToken, LinkOptions options = null);
        Task UnlinkAppleAsync();
        Task SignInWithGoogleAsync(string idToken, SignInOptions options = null);
        Task LinkWithGoogleAsync(string idToken, LinkOptions options = null);
        Task UnlinkGoogleAsync();
        Task SignInWithGooglePlayGamesAsync(string authCode, SignInOptions options = null);
        Task LinkWithGooglePlayGamesAsync(string authCode, LinkOptions options = null);
        Task UnlinkGooglePlayGamesAsync();
        Task SignInWithFacebookAsync(string accessToken, SignInOptions options = null);
        Task LinkWithFacebookAsync(string accessToken, LinkOptions options = null);
        Task UnlinkFacebookAsync();
        Task SignInWithSessionTokenAsync(SignInOptions options = null); // GUESS
        Task SignInWithUsernamePasswordAsync(string username, string password);
        Task SignUpWithUsernamePasswordAsync(string username, string password);
        Task AddUsernamePasswordAsync(string username, string password);
        Task UpdatePasswordAsync(string currentPassword, string newPassword);
        Task<PlayerInfo> GetPlayerInfoAsync();
        Task<string> GetPlayerNameAsync(bool autoGenerate = true);
        Task<string> UpdatePlayerNameAsync(string playerName);
        Task DeleteAccountAsync();
        void SignOut(bool clearCredentials = false);
        void SwitchProfile(string profile);
        void ClearSessionToken();
    }

    public class PlayerInfo
    {
        public string Id => throw null;
        public string Username => throw null;
        public DateTime? CreatedAt => throw null;
        public DateTime? LastPasswordUpdate => throw null; // GUESS
        public string GetGooglePlayGamesId() => throw null;
        public string GetAppleId() => throw null;
        public string GetGoogleId() => throw null;
        public string GetFacebookId() => throw null;
    }

    public class SignInOptions
    {
        public bool CreateAccount { get => throw null; set { } }
    }

    public class LinkOptions
    {
        public bool ForceLink { get => throw null; set { } }
    }

    public class AuthenticationException : RequestFailedException
    {
        public AuthenticationException(int errorCode, string message, Exception innerException = null) : base(errorCode, message, innerException) { }
        public string Notifications => throw null; // GUESS: type is a list of Notification in the real package
    }

    public static class AuthenticationErrorCodes
    {
        public const int MinValue = 10000;
        public const int ClientInvalidUserState = 10000;
        public const int ClientNoActiveSession = 10001;
        public const int AccountAlreadyLinked = 10002;
        public const int AccountLinkLimitExceeded = 10003;
        public const int ClientUnlinkExternalIdNotFound = 10004;
        public const int ClientInvalidProfile = 10005;
        public const int InvalidParameters = 10006;
        public const int InvalidSessionToken = 10007;
        public const int BannedUser = 10008;
        public const int EnvironmentMismatch = 10009;
    }
}

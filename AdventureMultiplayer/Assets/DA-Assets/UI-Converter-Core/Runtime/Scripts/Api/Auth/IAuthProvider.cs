#if UNITY_EDITOR
using System.Threading.Tasks;

namespace DA_Assets.UCC
{
    public interface IAuthProvider
    {
        string Token { get; }
        bool IsAuthed();
        Task TryRestoreSession();
        void SignIn();
        AuthType CurrentAuthType { get; }
        string UserDisplayName { get; }
        string UserIdShort { get; }
    }
}
#endif
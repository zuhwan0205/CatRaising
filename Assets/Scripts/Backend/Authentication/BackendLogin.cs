using UnityEngine;

using BackEnd;

public class BackendLogin
{
    private const string ManualLoginKey = "CatRaising.ManualLoginRequired";
    public static bool ManualLoginRequired
    {
        get => PlayerPrefs.GetInt(ManualLoginKey, 0) != 0;
        set { PlayerPrefs.SetInt(ManualLoginKey, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public async System.Threading.Tasks.Task<bool> LogoutAsync()
    {
        var completion = new System.Threading.Tasks.TaskCompletionSource<BackendReturnObject>(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
        Backend.BMember.Logout(result => completion.TrySetResult(result));
        var response = await completion.Task;
        return response.IsSuccess();
    }

    public async System.Threading.Tasks.Task<BackendReturnObject> LoginWithSavedTokenAsync(System.Threading.CancellationToken cancellationToken)
    {
        var completion = new System.Threading.Tasks.TaskCompletionSource<BackendReturnObject>(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(() => completion.TrySetCanceled());
        cancellationToken.ThrowIfCancellationRequested();
        if (!Backend.IsInitialized)
            throw new System.InvalidOperationException("뒤끝 초기화가 완료되지 않았습니다. 초기화를 다시 시도해주세요.");
        // 인증 정보의 저장과 갱신은 SDK가 담당합니다. 비밀번호나 토큰을 별도로 저장하지 않습니다.
        Backend.BMember.LoginWithTheBackendToken(result => completion.TrySetResult(result));
        return await completion.Task;
    }

    private static BackendLogin _instance = null;

    public static BackendLogin Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new BackendLogin();
            }

            return _instance;
        }
    }

    public bool CustomSignUp(string id, string pw)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(pw))
        {
            Debug.LogWarning("아이디와 비밀번호를 입력해주세요.");
            return false;
        }

        Debug.Log("회원가입을 요청합니다.");

        var bro = Backend.BMember.CustomSignUp(id, pw);

        if (bro.IsSuccess())
        {
            Debug.Log("회원가입에 성공했습니다. : " + bro);
            return true;
        }
        else
        {
            Debug.LogError("회원가입에 실패했습니다. : " + bro);
            return false;
        }
    }

    public bool CustomLogin(string id, string pw)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(pw))
        {
            Debug.LogWarning("아이디와 비밀번호를 입력해주세요.");
            return false;
        }

        Debug.Log("로그인을 요청합니다.");

        var bro = Backend.BMember.CustomLogin(id, pw);

        if (bro.IsSuccess())
        {
            Debug.Log("로그인이 성공했습니다. : " + bro);
            return true;
        }
        else
        {
            Debug.LogError("로그인이 실패했습니다. : " + bro);
            return false;
        }
    }

    public async System.Threading.Tasks.Task<string> UpdateNicknameAsync(string nickname)
    {
        if (string.IsNullOrEmpty(Backend.UserInDate)) return "로그인 후 이름을 변경해주세요.";
        if (nickname == Backend.UserNickName) return null;
        string owner = Backend.UserInDate;
        var completion = new System.Threading.Tasks.TaskCompletionSource<BackendReturnObject>(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
        Backend.BMember.UpdateNickname(nickname, result => completion.TrySetResult(result));
        var response = await completion.Task;
        if (owner != Backend.UserInDate) return "계정이 변경되었습니다. 다시 로그인해주세요.";
        if (response.IsSuccess()) return null;
        if (response.GetStatusCode() == "409") return "중복된 이름이 있습니다. 다른 이름을 입력해주세요.";
        if (response.GetStatusCode() == "400") return "사용할 수 없는 이름입니다. 다른 이름을 입력해주세요.";
        return "서버에서 이름을 변경하지 못했습니다. 연결을 확인하고 다시 시도해주세요.";
    }
}

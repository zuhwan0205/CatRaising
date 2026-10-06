using System;
using System.Threading.Tasks;
using BackEnd;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class BackendManager : MonoBehaviour
{
    [SerializeField] private UnityEvent onLoginReady = new UnityEvent();
    [SerializeField] private bool openGameSceneAfterLogin = true;
    [SerializeField] private string databaseUuid = "01a0a448-bc6f-72bf-b5ea-c7fb23d4fd0d";
    private bool submitting;
    private bool initialized;
    private bool loginReady;
    private LoadingLoginUI loginUI;

    private void Start()
    {
        loginUI = GetComponent<LoadingLoginUI>();
        if (loginUI == null)
            loginUI = gameObject.AddComponent<LoadingLoginUI>();
        loginUI.Build(this);
        InitializeBackend();
    }

    public void InitializeBackend()
    {
        try
        {
            var result = Backend.Initialize();
            initialized = result.IsSuccess();
            loginUI.ShowInitialization(initialized);
            if (!initialized)
                Debug.LogError("뒤끝 초기화 실패 : " + result);
        }
        catch (Exception exception)
        {
            initialized = false;
            Debug.LogException(exception);
            loginUI.ShowInitialization(false);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Database/Create player_save table (once)")]
    private async void CreatePlayerSaveTable()
    {
        if (!Application.isPlaying || submitting)
        {
            Debug.LogWarning("Play 모드에서 로그인 요청이 끝난 후 실행해주세요.");
            return;
        }
        submitting = true;
        try
        {
            await BackendGameData.CreateTableAsync(databaseUuid);
            loginUI.ShowMessage("player_save 테이블을 생성했습니다. 로그인 버튼을 다시 눌러주세요.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (this != null) loginUI.ShowMessage("테이블 생성 실패. Console 오류와 DB 권한을 확인해주세요.");
        }
        finally { submitting = false; }
    }
#endif

    public async Task SubmitAsync(bool signUp, string id, string password)
    {
        if (!initialized || loginReady || submitting)
            return;
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(password))
        {
            loginUI.ShowMessage("아이디와 비밀번호를 입력해주세요.");
            return;
        }

        submitting = true;
        try
        {
            if (signUp)
            {
                if (BackendLogin.Instance.CustomSignUp(id, password))
                    loginUI.ShowLoginAfterSignUp();
                else
                    loginUI.ShowMessage("회원가입에 실패했습니다. 아이디 중복, 입력 조건 또는 네트워크를 확인해주세요.");
                return;
            }

            if (!BackendLogin.Instance.CustomLogin(id, password))
            {
                loginUI.ShowMessage("로그인에 실패했습니다. 아이디·비밀번호와 네트워크를 확인해주세요.");
                return;
            }

            await CompleteLoginAsync();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (this != null)
                loginUI.ShowMessage("처리 중 오류가 발생했습니다. 잠시 후 다시 시도해주세요.");
        }
        finally { submitting = false; }
    }

    public async Task SubmitGoogleAsync()
    {
        if (!initialized || loginReady || submitting)
            return;
        submitting = true;
        try
        {
#if UNITY_IOS && !UNITY_EDITOR && CAT_BACKND_GOOGLE_IOS
            var completion = new TaskCompletionSource<string>();
            using var cancellation = destroyCancellationToken.Register(() => completion.TrySetCanceled());
            // 콜백은 토큰만 전달합니다. Unity/뒤끝 처리는 await 이후 메인 스레드에서 진행합니다.
            TheBackend.ToolKit.GoogleLogin.iOS.GoogleLogin((success, error, token) =>
                completion.TrySetResult(success ? token : null));
            var token = await completion.Task;
            if (this == null) return;
            if (string.IsNullOrEmpty(token))
            {
                loginUI.ShowMessage("구글 로그인이 취소되었거나 실패했습니다. 다시 시도해주세요.");
                return;
            }
            var result = Backend.BMember.AuthorizeFederation(token, FederationType.Google);
            if (!result.IsSuccess())
            {
                loginUI.ShowMessage("뒤끝 구글 로그인에 실패했습니다. 네트워크와 인증 설정을 확인해주세요.");
                return;
            }
            await CompleteLoginAsync();
#elif UNITY_EDITOR
            loginUI.ShowMessage("구글 로그인은 iOS 기기 빌드에서 테스트해주세요. 에디터에서는 기존 로그인을 사용할 수 있습니다.");
            await Task.CompletedTask;
#else
            loginUI.ShowMessage("iOS 구글 로그인 SDK 및 CAT_BACKND_GOOGLE_IOS 설정이 필요합니다.");
            await Task.CompletedTask;
#endif
        }
        catch (Exception)
        {
            // 인증 예외에는 토큰 등이 포함될 수 있으므로 원문을 출력하지 않습니다.
            if (this != null)
                loginUI.ShowMessage("구글 로그인 처리에 실패했습니다. SDK 설정과 네트워크를 확인해주세요.");
        }
        finally { submitting = false; }
    }

    private async Task CompleteLoginAsync()
    {
        loginUI.ShowMessage("데이터베이스에 연결하고 유저 데이터를 준비합니다…");
        if (!await BackendGameData.Instance.InitializeAndLoadAsync(databaseUuid))
        {
            if (this != null)
                loginUI.ShowMessage(BackendGameData.Instance.LastError);
            return;
        }
        if (this == null)
            return;
        loginReady = true;
        loginUI.ShowCompleted();
        // Inspector에서 로그인 완료 후 씬 전환 등의 동작을 연결합니다.
        onLoginReady.Invoke();
        // 기존 Inspector 씬 전환 이벤트가 있으면 자동 이동과 중복 실행하지 않습니다.
        if (openGameSceneAfterLogin && onLoginReady.GetPersistentEventCount() == 0)
            _ = SceneManager.LoadSceneAsync("GameScene");
    }
}

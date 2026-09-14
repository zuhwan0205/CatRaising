using System;
using BackEnd;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class BackendManager : MonoBehaviour
{
    [SerializeField] private UnityEvent onLoginReady = new UnityEvent();
    [SerializeField] private bool openGameSceneAfterLogin = true;
    private bool initialized;
    private bool loginReady;
    private LoadingLoginUI loginUI;

    private void Start()
    {
        loginUI = GetComponent<LoadingLoginUI>();
        if (loginUI == null) loginUI = gameObject.AddComponent<LoadingLoginUI>();
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
            if (!initialized) Debug.LogError("뒤끝 초기화 실패 : " + result);
        }
        catch (Exception exception)
        {
            initialized = false;
            Debug.LogException(exception);
            loginUI.ShowInitialization(false);
        }
    }

    public void Submit(bool signUp, string id, string password)
    {
        if (!initialized || loginReady) return;
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(password))
        {
            loginUI.ShowMessage("아이디와 비밀번호를 입력해주세요.");
            return;
        }

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

            // 조회 실패와 조회 성공 후 데이터가 없는 경우를 구분합니다.
            if (!BackendGameData.Instance.GameDataGet())
            {
                loginUI.ShowMessage("로그인은 성공했지만 데이터를 불러오지 못했습니다. 로그인을 다시 눌러주세요.");
                return;
            }
            if (BackendGameData.userData == null && !BackendGameData.Instance.GameDataInsert())
            {
                loginUI.ShowMessage("초기 데이터를 저장하지 못했습니다. 로그인을 다시 눌러주세요.");
                return;
            }

            // 기존 행을 갱신하며, 변환 결과 저장 실패 시 로그인 완료를 보류합니다.
            if (BackendGameData.Instance.NeedsMigrationSave && !BackendGameData.Instance.GameDataUpdate())
            {
                loginUI.ShowMessage("기존 데이터를 새 형식으로 저장하지 못했습니다. 로그인을 다시 눌러주세요.");
                return;
            }

            loginReady = true;
            loginUI.ShowCompleted();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            loginUI.ShowMessage("처리 중 오류가 발생했습니다. 잠시 후 다시 시도해주세요.");
            return;
        }

        // Inspector에서 로그인 완료 후 씬 전환 등의 동작을 연결합니다.
        onLoginReady.Invoke();
        // 기존 Inspector 씬 전환 이벤트가 있으면 자동 이동과 중복 실행하지 않습니다.
        if (openGameSceneAfterLogin && onLoginReady.GetPersistentEventCount() == 0)
            SceneManager.LoadSceneAsync("GameScene");
    }
}

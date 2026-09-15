using UnityEngine;

// 씬 진입점: 데이터 기능과 UI를 생성하고 이벤트만 연결합니다.
public class CatGameScene : MonoBehaviour
{
    // 기존 씬/Inspector에 저장된 참조를 유지합니다.
    [SerializeField] private CatGameCatalog catalog;
    [SerializeField] private Font uiFont;
    private bool ownCatalog;
    private CatGameSceneUI view;
    private CatGameSession session;

    private void Start()
    {
        bool preview = BackendGameData.userData == null;
        var data = preview ? BackendUserData.CreateNew() : BackendGameData.userData;
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<CatGameCatalog>();
            ownCatalog = true;
        }
        session = new CatGameSession(data, preview, () => BackendGameData.Instance.GameDataUpdateAsync());
        view = gameObject.AddComponent<CatGameSceneUI>();
        view.Build(data, catalog, uiFont, preview);
        view.SaveRequested += Save;
        view.CharacterSelected += SelectCharacter;
        session.BusyChanged += view.SetBusy;
        session.StatusChanged += view.ShowStatus;
    }

    private async void Save() { await session.SaveAsync(); }
    private async void SelectCharacter(OwnedCharacter character) { await session.SelectCharacterAsync(character); }

    private void OnDestroy()
    {
        // 전송 중인 저장은 완료까지 처리하고 UI 이벤트만 해제합니다.


        if (view != null)
        {
            view.SaveRequested -= Save;
            view.CharacterSelected -= SelectCharacter;
            if (session != null)
            {
                session.BusyChanged -= view.SetBusy;
                session.StatusChanged -= view.ShowStatus;
            }
        }
        if (ownCatalog && catalog != null) Destroy(catalog);
    }
}

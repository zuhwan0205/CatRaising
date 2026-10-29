# 특별 상점 편집 안내

특별 상점은 MVP 방식으로 분리했습니다. 화면 모양(View), 상품 데이터(Catalog), 카드 클릭과 팝업 수명 관리(Presenter)를 각각 담당합니다. 메인 UI는 특별 상점을 열고 닫기만 합니다.

## Unity에서 직접 수정하는 파일
Project 창에서 `Assets/Scripts/Resources/SpecialShop`을 엽니다.

- `SpecialShopView.prefab`: 상점 제목, 배경, 스크롤 영역. `Package Cards`의 Special Shop Grid에서 열 수(Constraint Count), 카드 높이(Cell Size Y), 간격(Spacing), 여백(Padding)을 수정합니다. 카드 폭은 화면 너비에 맞게 자동 계산합니다.
- `SpecialShopCard.prefab`: 모든 상품 카드에 공통으로 쓰는 틀. 이미지 영역, 상품명 위치, 가격 배경, 글자 크기, 모서리 등을 편집합니다. 카드 전체 크기는 목록의 Grid가 결정합니다.
- `SpecialShopPopup.prefab`: 상세 창 배경, 이미지 영역, 구성품 표시, 닫기·가격 버튼 배치입니다.
- `SpecialShopCatalog.asset`: 상품 이름, 예시 가격, 다이아·골드 수량, 상품 이미지와 UV를 수정합니다. Offers에 상품을 추가하면 카드도 늘어납니다. ID는 상품마다 고유하게 정하세요.

프리팹을 더블클릭해 Prefab Mode에서 편집하고 저장하면 다음 화면 진입 때 적용됩니다. 프리팹을 씬에 드래그해서 확인한 경우 Overrides > Apply로 원본에 반영해야 합니다. 기존 UI 디자인 저장 메뉴를 거칠 필요가 없습니다.
상품명·가격·수량·그림은 상품 데이터에서 채우므로 이 값은 Catalog에서 수정하세요. 제목처럼 고정된 문구와 스타일은 프리팹에서 직접 수정하면 됩니다. 스크립트가 잡고 있는 Button/Text/RawImage 참조는 삭제하지 말고, 필요 없는 글씨는 Text 컴포넌트의 체크를 끄면 표시하지 않을 수 있습니다.

## 구조
- SpecialShopCatalog / SpecialShopOffer: 상품 데이터. 결제 상품 ID가 아닌 시안 ID입니다.
- SpecialShopView / SpecialShopCardView / SpecialShopPopupView: 프리팹 참조와 데이터 표시. 배치·색·폰트 설정을 매번 다시 생성하지 않습니다.
- SpecialShopPresenter: 카드 선택 → 팝업 표시, 닫기, 탭 종료 때 이벤트 해제와 팝업 정리.
- SpecialShopGrid: 화면 폭에 맞는 카드 배치.
- CatGameSceneUI.SpecialShop: 내비게이션 및 상점 진입 연결.
- CatGameSceneUI.SpecialShopAuthoring / SpecialShopPrefabSetup: 에디터에서 누락된 초기 에셋만 생성합니다. 배포 빌드에는 생성 코드가 포함되지 않습니다. 이미 있는 프리팹은 덮어쓰지 않습니다.

## 결제 상태
기존처럼 시안입니다. 가격 버튼은 실제 실행 시에도 비활성화하고 스토어 요청이나 보상을 지급하지 않습니다. 가격은 등록된 스토어의 현지화 가격이 아닙니다.

## 검증
CatRaising > Checks > Special Shop Prefabs: 연결, 상품 수, 디자인 유지, 중복 클릭 구독, 팝업 닫기, 해제, 프리팹 보존 검사.
SpecialShopPlayChecks.Run: 배치 전용 실제 GameScene 검증. 서버 없는 미리보기 계정으로 클릭 Raycast, 스크롤, 탭 재진입, 재화 무변경을 검사합니다.

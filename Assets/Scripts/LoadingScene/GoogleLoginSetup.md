# iOS Google 로그인 설정

로그인 화면의 Google로 계속하기 → iOS Google 인증 → 뒤끝 AuthorizeFederation → 기존 player_save 로드/생성 → GameScene 순서입니다. 버튼은 자동 생성됩니다.

## Unity 및 Google 설정 (실제 기기 테스트 전 필수)

1. 뒤끝 공식 설치 문서에서 iOS용 Google 로그인 SDK를 가져옵니다. 해당 문서에 안내된 External Dependency Manager for Unity도 설치합니다.
2. Google Cloud에서 OAuth 동의 화면과 테스트 사용자를 설정하고 iOS OAuth Client ID를 생성합니다. Bundle ID는 Unity iOS 앱의 Bundle Identifier와 일치시킵니다.
3. Unity 메뉴 TheBackend > ToolKit > GoogleLogin Settings의 Ios Client ID에 생성한 값을 입력합니다. 뒤끝 Client App ID나 Signature Key를 입력하는 칸이 아닙니다.
4. SDK 설치가 끝난 후 iOS Player Settings의 Scripting Define Symbols에 CAT_BACKND_GOOGLE_IOS를 추가합니다. 이는 이 프로젝트의 연동 코드 활성화용 심볼입니다. SDK 없이 추가하면 iOS 빌드가 컴파일되지 않습니다.
5. Mac의 Unity에서 iOS 프로젝트를 빌드하고, 네이티브 의존성을 해결한 Xcode 프로젝트에서 서명 및 기기 실행을 진행합니다. SDK의 Xcode 설정 문서도 확인합니다.

## 테스트

- 에디터: Google 버튼은 기기 테스트 안내를 표시하고 다시 조작할 수 있어야 합니다. 기존 회원가입/로그인도 확인합니다.
- iPhone: Google 인증 취소 시 재시도 가능, 인증 중 중복 클릭 차단, 첫 로그인 시 player_save 생성, 재로그인 시 동일 데이터 로드를 확인합니다.
- DB 로드에 실패하면 GameScene으로 넘어가지 않고 오류를 표시합니다.
- 기존 아이디/비밀번호 계정과 Google 계정은 별도 계정입니다. 기존 진행 데이터 자동 병합/계정 연결은 구현하지 않았습니다.

현재 코드 컴파일 확인은 에디터 경로에 한정됩니다. SDK 설치 및 실제 iOS 인증은 기기에서 검증해야 합니다. 토큰과 SDK 인증 오류 원문은 로그에 출력하지 않습니다.

## 공식 문서

- 설치: https://docs.backnd.com/sdk-docs/backend/toolkit/google-login/install-sdk/
- iOS 설정: https://docs.backnd.com/sdk-docs/backend/toolkit/google-login/ios/ios-settings/
- API: https://docs.backnd.com/sdk-docs/backend/toolkit/google-login/ios/code/
- Xcode: https://docs.backnd.com/sdk-docs/backend/toolkit/google-login/ios/xcode-settings/

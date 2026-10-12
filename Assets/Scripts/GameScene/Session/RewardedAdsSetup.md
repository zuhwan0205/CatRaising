# 크리스탈 보상 광고 (테스트 연결)

상점 상단 버튼: 시청 완료 시 크리스탈 10개, UTC 날짜 기준 하루 5회.
wallet JSON의 crystalAdDay / crystalAdCount에 기록하므로 DB 컬럼 추가는 필요 없습니다.
저장 실패 시 현재 세션의 보상을 유지하고 같은 버튼으로 저장을 재시도합니다.

## SDK 연결

1. 공식 Google Mobile Ads Unity 플러그인을 설치합니다.
   https://developers.google.com/admob/unity/quick-start
2. Assets > Google Mobile Ads > Settings에서 플랫폼별 AdMob 앱 ID를 설정합니다.
3. 설치된 SDK에 직접 연결하므로 별도 Scripting Define Symbol은 필요 없습니다.
4. 모바일 테스트 빌드에서 상점 버튼을 누릅니다. 코드는 공식 테스트 광고 단위 ID만 사용합니다.

현재 Google Mobile Ads 11.5.0이 설치되어 있으며 Android 앱 ID가 설정되어 있습니다.
iOS 앱 ID는 아직 비어 있으므로 iOS 빌드 전에 설정해야 합니다.
Unity Editor 광고 화면은 실제 모바일 광고 검증을 대체하지 않습니다.

## 검증할 항목

- 시청 완료: 크리스탈 +10, 재로그인 후 잔액과 남은 횟수 유지
- 중도 종료/로딩 실패: 지급 및 횟수 차감 없음
- 빠른 연속 클릭: 한 광고만 요청
- 저장 실패: 보상 저장 재시도로 재시청 없이 저장
- 5회 완료: 버튼 비활성화, UTC 날짜 변경 후 다시 가능

## 출시 전 남은 작업

실제 앱/광고 단위 ID, 개인정보 동의 흐름(UMP), 실제 기기 테스트가 필요합니다.
현재 일일 제한은 기기 UTC 시각과 클라이언트 데이터 기준의 프로토타입입니다.
서버 시각 및 AdMob SSV 검증에 기반한 서버 보상 지급으로 전환해야 조작을 방지할 수 있습니다.
저장 전에 앱을 강제 종료한 경우의 보상 복구도 서버 검증으로 처리해야 합니다.

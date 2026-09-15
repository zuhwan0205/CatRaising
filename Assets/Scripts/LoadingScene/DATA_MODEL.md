# 고양이 성장게임 저장 구조

현재 저장 구현은 BACKND.Database SDK입니다. 최신 설정과 이전 절차는 ../Database/README.md를 참조하세요.

BackendUserData는 게임 메모리에서 사용하는 모델이고 PlayerSaveRow가 새 DB의 player_save 테이블과 매핑됩니다. 진행도, 재화, 보유 목록, 장착 상태, 방치 보상 상태를 각각 JSON 컬럼에 저장합니다. 최종 전투 스탯 계산, 강화, 보상 지급은 별도 구현 대상입니다.

기존 USER_DATA의 schemaVersion/playerDataJson 및 더 오래된 level/atk 형식은 LegacyGameDataReader가 이전 시에만 읽습니다. 이후 저장은 새 DB에서만 수행하며 기존 서버 행은 보존됩니다.

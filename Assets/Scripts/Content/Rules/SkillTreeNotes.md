# 캐릭터 스킬 트리

각 스킬 최대 5단계, 캐릭터 레벨당 1포인트. 각 갈림길은 한쪽에 1포인트를
투자하면 반대쪽 전체가 잠긴다. 선택한 부모를 5단계까지 배워야 자식이 열린다.
공통 3개는 선행 및 상호 배타 조건이 없다.

- 근거리: 생명력 → 쌍단검(흡혈 / 독), 대검(번개 / 둔기)
- 원거리: 신속 → 강철 화살(폭발 / 거대 화살), 쌍화살(화상 / 집중 사격)
- 공통: 자석, 신발, 회복 물약

현재 원거리 분류는 cat_ninja(샴 궁수), cat_moon(달빛 마법),
cat_lion(용), cat_shadow(눈꽃)이다. 새 캐릭터 추가 시 CatSkillTree.IsRanged도 갱신한다.
수치와 설명은 CatSkillTree.cs의 Effects 및 배율 함수를 기준으로 한다.

구형 6칸 skillRanks는 읽을 때 효과와 사용 포인트를 0으로 취급한다.
다음 스킬 저장에서 17칸 배열로 교체하며, 저장 실패 시 원래 배열을 복원한다.
캐릭터 레벨/성급/경험치는 변경하지 않는다.

독/화상은 각각 중첩당 최대 HP의 0.5%/초, 5초 지속, 재명중 시 지속 갱신.
스킬 단계가 최대 중첩 수이며 상한 5. 적별로 공유하여 파티원별로 중첩 상한이
곱해지지 않는다. 방어를 무시하고 소수점 피해를 유지한다.
최종 상태를 적용한 살아있는 캐릭터가 피해를 처리한다.
일반 명중에만 흡혈·번개·상태 부여가 발동하며 추가 피해에서 재귀 발동하지 않는다.
흡혈은 실제로 깎인 HP 기준이다.

화살은 이동 구간 충돌 검사로 판정하고, 관통 화살은 같은 적에 1회 명중한다.
지형에 막히면 소멸하며 폭발은 사거리 끝까지 도달했을 때만 발생한다.
집중 사격은 대상 변경/사망/사거리 이탈 시 초기화한다.
메인 전투와 파티 전투 모두 FieldCombatController에서 동일하게 처리한다.
레이드 이동 대형은 살아있는 파티원 중 가장 낮은 이동 보너스를 따른다.

자석은 5/4/3/2/1초 간격으로 필드의 모든 경험치 결정을 수집한다.
기존 골드는 자동 지급되므로 시각용 동전을 다시 보상하지 않는다.
물약은 체력 50% 미만일 때 10/20/30/40/50% 회복, 재사용 60초.
사망한 캐릭터를 부활시키지는 않는다.

## 검증

- 프로젝트의 Unity 컴파일 응답 파일을 이용해 Assets/Scripts 전체 C# 컴파일 통과.
- 순수 규칙 검사 18개: 선행 4/5 구분, 갈림길 및 하위 트리 잠금,
  원거리/근거리 제한, 공통 독립, 포인트 부족, 단계 상한, 구형 환급 등.
- 실제 상태 스크립트에 경량 Unity 대역을 연결한 검사 8개:
  중첩 상한, 0.5% 피해, 저장 중 일시정지, 5초 만료, 독/화상 독립,
  풀 재사용 초기화, 캐릭터 변경 초기화, 소수점 피해.
- Unity 플레이 화면/실기기 UI 및 전투 연출은 미확인.

## 생성 이미지

내장 image_gen 도구로 생성한 투명 픽셀 아이콘을 프로젝트에 복사했다.
SkillIcons.png는 4×4 셀, SkillPotion.png는 단일 이미지이다.

경로: Assets/Scripts/Resources/GameUIArt/SkillIcons.png, SkillPotion.png

최종 아틀라스 프롬프트:

> Create ONE game UI icon atlas PNG, a perfectly regular 4 by 4 grid of 16 equally sized square cells, transparent background, no text no borders no labels. Each icon centered in its cell with large 20 percent transparent margins to allow precise grid slicing. Cute crisp pixel art fantasy cat RPG, colorful, chunky pixel outlines. Row 1 left to right: green upward arrow (health upgrade); crossed twin daggers; vampire fangs; green poison droplet. Row 2: huge greatsword; yellow lightning bolt; heavy wooden mace; blue upward arrow (attack speed upgrade). Row 3: single arrow with metallic steel arrowhead; orange explosion; oversized broad arrow; two parallel arrows. Row 4: orange flame; small purple monster with four arrows stuck in it; horseshoe magnet; brown boots. Need an additional potion icon? NO, only these exact sixteen icons. Uniform grid, every cell one icon and no overlap.

최종 물약 프롬프트:

> One centered cute pixel art game UI healing potion icon. Small round glass bottle with red pink liquid, cork stopper, tiny white heart on front, chunky dark pixel outline, gold highlight, vibrant readable silhouette. No text, no border, no background, transparent canvas, 20 percent transparent margin on all sides. Square game skill icon.

# 던전 전투 맵 장식

- 이미지: `Resources/DungeonForestProps.png` (투명 PNG, Point 필터)
- 제작: 내장 image_gen 도구
- 적용: `../Combat/Core/DefenseTileBoard.cs`
- 배치: 나무 3곳, 버섯 4곳, 고양이 석상 1곳, 보급 상자 2곳
- 큰 장식 칸은 배치 목록에서 제외하며 몬스터 이동 경로는 유지합니다. 현재 배치 가능한 칸은 77개입니다.
- 잔디 4종과 길 가장자리 타일로 바닥에 변화를 줍니다.
- 맵 배경 카메라는 첫 렌더 후 멈추고 렌더 텍스처가 소실된 경우에만 다시 그립니다. 기본 렌더러 및 SRP의 카메라 완료 이벤트를 지원합니다.

## 생성 프롬프트
Use case: stylized-concept. Create a cute polished pixel art environment prop ATLAS for a top-down 2.5D cat fantasy tower defense game. Square canvas exact 2x2 grid of FOUR SEPARATE props with generous transparent gutters, no gridlines. TOP LEFT one compact round leafy green tree with a visible warm brown trunk, layered lime and deep green pixel foliage, a few tiny yellow flowers at roots. TOP RIGHT one small cluster of red and cream spotted mushrooms and fern leaves on a tiny moss patch. BOTTOM LEFT one small ancient mossy stone CAT statue on a low square stone pedestal, closed happy eyes, faint cyan rune at base. BOTTOM RIGHT one cozy wooden supply crate with a rolled cream blanket and tiny orange lantern beside it. Each entire prop including a compact dark pixel ground shadow fits strictly inside central 65% of its own quadrant; none cross borders; all centered horizontally and resting at same local bottom baseline. Same 3/4 top-down game view, light from upper left, charming hand-drawn chunky crisp pixel clusters, dark colored outlines, limited harmonious forest palette. No characters, no terrain background except tiny integrated bases, no text or UI. Actual transparent alpha background. Four distinct game-ready isolated props, clear at small mobile size.

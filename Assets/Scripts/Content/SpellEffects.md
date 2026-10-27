# 고양이 공격 스프라이트

내장 image_gen 도구로 제작했습니다. 픽셀풍, 투명 PNG입니다. 원본 이미지를 프로젝트에 복사했고 런타임에는 스프라이트 영역만 나눕니다.

## 적용 파일
- `Resources/DragonBreathVfx.png`: 용 고양이의 4프레임 전방 브레스
- `Resources/SnowflakeVfx.png`: 눈꽃 고양이의 투사체 2프레임 + 폭발 2프레임
- `Resources/MoonlightVfx.png`: 달빛 고양이의 투사체 2프레임 + 폭발 2프레임
- `../Combat/Visuals/CatSpellSprites.cs`: 프레임 영역·기준점·공유 캐시·애니메이션 시간
- `../Combat/Visuals/FieldCombatEffects.cs`: 메인/레이드의 재사용 스프라이트 이펙트
- `../Combat/Core/FieldCombatController.Skills.cs`: 관통/추가 투사체 스킬의 이미지
- `../Combat/Core/DungeonDefenseBattle.cs`: 던전의 투사체·명중 프레임 재사용

용 브레스 0.38초, 투사체 비행 0.25초, 명중 연출 0.22초. 던전은 기존 비행 시간에 한 번만 피해를 처리하고 이후 폭발 연출만 유지합니다. 메인/레이드 기본 공격의 기존 피해 처리 시점은 바꾸지 않습니다. 피격 범위·감속·방어력 감소·무희 강화 수치도 그대로입니다.

Point 필터, mipmap 없음, 원본 크기 유지, 알파 투명 설정. 공유 프레임은 실행 중 재사용합니다. 메인은 보유 고양이의 이펙트를 비동기 사전 준비합니다.

## 최종 제작 프롬프트

### 용 (초안 후 아래 프롬프트로 레이아웃 수정)
Edit this fire breath sprite sheet ONLY to correct its layout for an animation atlas. Keep the exact same cute orange/yellow pixel-art flames and transparent alpha. Critical: shrink each of the FOUR flames so its COMPLETE silhouette including embers fits within the central 60% width AND central 60% height of its own equal quadrant. Four quadrants are EXACT 2 columns by 2 rows. Huge empty transparent gutters. NO flame or ember may cross any cell boundary. Flame narrow emission point at x=20% of each quadrant width and y=50% of its height; far-right flame tip at x=80%. All FOUR origins aligned to the same local coordinate. All frames face right. Preserve crisp pixel-art look. NO grid lines, labels, backgrounds or character. Make empty margins clearly much wider than current reference.

### 눈꽃
Use case: stylized-concept. Production sprite sheet for cute pixel fantasy Unity game, SNOWFLAKE CAT ICE MAGIC VFX, true transparent background. Exactly 2 columns x 2 rows equal cells on square canvas; every sprite strictly isolated within central 70% of its cell with huge transparent gutters, never overlapping cell boundaries. Four images: top left an icy cyan diamond-shaped crystal bolt pointing RIGHT with short sparkling snowflake tail to left; top right a second frame of same rightward crystal bolt with changed ice sparkles; bottom left a centered compact radial burst of sharp pale blue ice crystals and tiny snowflakes; bottom right a centered larger dispersing circular frost explosion with shards and snowflake particles. Crisp chunky retro pixel art readable at tiny size, white highlights, cyan light blue dark blue outlines, consistent palette; solid pixel clusters not airbrush glow. No ground, no characters, no UI, no letters, no grid lines, no borders. Each sprite centered precisely within its quadrant. Bottom burst frames share same center and full diameter less than 70% cell width.

### 달빛
Use case: stylized-concept. Production Unity pixel-art VFX sprite sheet for cute MOONLIGHT STAR MAGE CAT. True alpha transparent backdrop. Square canvas with exact 2 columns by 2 rows equal cells, four isolated sprites centered in each quadrant. Keep every sprite inside central 70% of cell with generous transparent gutters. TOP LEFT: rightward small luminous five-point golden star magic bolt wrapped by a lavender crescent moon, short violet stardust tail to left. TOP RIGHT: second animation of same rightward star-crescent bolt, twinkling particles changed. BOTTOM LEFT: centered compact magical impact, bright golden central star with purple moonlit crescent arc and four little stars around it. BOTTOM RIGHT: expanding circular lavender magic burst, crescent moon arc, scattered golden star sparkles, transparent center. Polished chunky crisp pixel art, limited violet lavender indigo pale yellow palette, cute readable fantasy style, no smooth painterly gradients or airbrushed haze. No text, no borders or grid, no characters, no terrain, no shadow. Exact 2x2 game sprite layout, identical centers, no cross-cell overlap.

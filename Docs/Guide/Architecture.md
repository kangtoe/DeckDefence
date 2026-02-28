# 아키텍처 가이드

## 개요

게임의 전체 소프트웨어 아키텍처와 주요 시스템 간 관계를 정의한다.

---

## 1. 시스템 구조

```
┌─────────────────────────────────────────────┐
│                GameManager                   │
│  (씬 전환, 게임 상태, 캠페인 관리)            │
└──────────┬──────────────────┬───────────────┘
           │                  │
    ┌──────▼──────┐   ┌──────▼──────┐
    │ CombatManager│   │  MapManager  │
    │ (전투 진행)   │   │ (맵 탐색)    │
    └──────┬──────┘   └──────┬──────┘
           │                  │
  ┌────────┼────────┐        │
  │        │        │        │
┌─▼───┐ ┌─▼───┐ ┌──▼──┐  ┌──▼──────┐
│Spawn│ │Card │ │Unit │  │  Shop   │
│Mgr  │ │Mgr  │ │Mgr  │  │  Mgr    │
└─────┘ └─────┘ └─────┘  └─────────┘
```

---

## 2. 전투 데이터 흐름

```
[CampaignData.Deck] → CombatManager 초기화
                      ↓
              CardManager (Draw Pile 생성)
                      ↓
              카드 드로우 → Hand
                      ↓
              카드 사용 → UnitManager.SpawnUnit() 또는 SpellEffect
                      ↓
              사용된 카드 → Discard Pile
```

---

## 3. 전역 이벤트

시스템 간 통신은 이벤트를 통해 수행한다.

| 이벤트 | 발행자 | 구독자 | 데이터 |
|--------|--------|--------|--------|
| OnCombatStarted | CombatManager | CardManager, SpawnManager, ManaSystem, RelicSystem, UI | - |
| OnCardDrawn | CardManager | UI, RelicSystem | CardData |
| OnCardPlayed | CardManager | CombatManager, UI | CardData, Position |
| OnUnitSpawned | UnitManager | UI, RelicSystem | UnitData (전투 유닛 소환 시) |
| OnUnitDied | Unit | UnitManager, UI, RelicSystem | Unit (영웅/적 기지 포함) |
| OnEnemyKilled | Unit | ManaSystem, SpawnManager | EnemyData |
| OnCombatWon | CombatManager | GameManager | RewardData |
| OnCombatLost | CombatManager | GameManager | - |
| OnManaChanged | ManaSystem | UI | Current, Max |
| OnUnitHPChanged | Unit | CombatManager, UI | Unit, Current, Max |

---

## 4. 씬 구성

| 씬 | 용도 |
|-----|------|
| Title | 타이틀/메인 메뉴 |
| HeroSelect | 영웅 선택 |
| Map | 맵 탐색 (상점, 이벤트, 휴식 포함) |
| Combat | 전투 |
| GameOver | 캠페인 종료 (결과 화면) |

- GameManager는 DontDestroyOnLoad로 씬 간 유지
- 각 씬에는 해당 씬의 매니저가 존재

---

## 5. 확장 고려 사항

- **새 카드 추가**: ScriptableObject 생성만으로 추가 가능하도록 설계
- **새 적 타입 추가**: 적 데이터 SO + 행동 패턴 컴포넌트 조합
- **새 유물 추가**: 유물 효과는 이벤트 구독 기반으로 구현하여 독립적 추가 가능
- **새 권역 추가**: 권역 데이터 (적 풀, 배경, 보스) 정의만으로 확장

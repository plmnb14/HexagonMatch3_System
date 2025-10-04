# HexagonMatch3_System
> 2D HexagonMatch3 코어 시스템 소스코드입니다.
--------------------------------------------------

## 프로젝트 개요/목표
- 퍼즐 매칭 3게임의 핵심 루프 구현 (스왑 → 매칭 → 팝 → 낙하 → 재생성 → 체인)
- Hex형(6방향) 격자 기반 보드 로직
--------------------------------------------------

## 주요 시스템
- `HexBoard.cs`     : 보드 오케스트레이션 / 체인 루프 / 팝 / 스폰 관리 | Main
- `HexDropper.cs`   : 낙하 경로 계획 및 적용 / 버퍼 재사용 | 인스턴스
- `HexMatcher.cs`   : 매칭 판정 (순수 계산) | static
- `BlockBase.cs`    : 블록 개별 이동 및 상태 관리 | MonoBehaviour
- `BlockManager.cs` : 블록/VFX 풀링 및 팝 연출 | Singleton
- `BlockData.cs`    : BlockSkin, Enum, Struct 정의 | Data
--------------------------------------------------

## 개발 환경
- Unity 6000.0.48f1
- C#

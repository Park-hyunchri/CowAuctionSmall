# CowAuctionSmall

가축 경매의 대기·진행·낙찰·유찰 정보를 전광판에 표시하는 Windows WPF 프로그램이다. 현재 대상 프레임워크는 `.NET 10`이며, MVVM 구조와 해상도·사업장별 화면을 사용한다.

## 문서를 읽는 순서

| 문서 | 역할 |
| --- | --- |
| [AGENTS.md](AGENTS.md) | 작업 범위, 사용자 변경 보호, 구현·검증·문서 관리 규칙 |
| [기능 문서](Docs/기능/README.md) | 현재 코드에 적용된 기능, 설정, 담당 소스와 제한 사항 |
| [문서 전체 목차](Docs/README.md) | 완료·후속·대체 및 철회 계획, 검토 자료, 사용 안내 |
| [파일 구조 안내](Docs/파일구조_안내.md) | 수정할 기능의 소스 위치와 기존 파일의 이동 위치 |
| [WORKLOG.md](WORKLOG.md) | 작업 당시의 변경·검증 결과와 미확인 범위 |

## 주요 기능

- Netty 신호와 경매 목록 API를 받아 출품 정보·가격·경매 결과를 갱신한다.
- 대기·진행·낙찰·유찰 화면과 페이지 전환, MASTER/SUB 동기화를 제공한다.
- 축산물 이력 API와 SQLite 당일 캐시를 사용하며, 조회가 끝난 개체부터 화면에 반영한다.
- 이력 API 사용 여부와 소유주 변경 비고 표시 여부를 별도로 설정한다.
- 통합 상태바에 응찰서버 연결·경매 상태·페이지 회전·선택 구간·버전을 표시하고 최근 신호와 이력 진행을 별도로 안내한다.

세부 동작과 알려진 문제는 [기능 문서](Docs/기능/README.md)를 기준으로 확인한다. 예전 계획에 적힌 제안이 모두 현재 동작인 것은 아니다.

## 소스 구성

| 위치 | 책임 |
| --- | --- |
| `Domain/` | 경매·이력 데이터와 설정 모델 |
| `Application/` | 경매 수신·상태 처리·이력 조회 흐름과 외부 구현 계약 |
| `Infra/` | HTTP·Netty·SQLite·XML·로그·PC 간 동기화 구현. XML 설정은 `Infra/XmlSettings/` |
| `Presentation/` | WPF 표시·페이지·애니메이션·이력 결과 반영 |
| `ViewModels/`, `Views/` | 화면 상태·표시 값과 XAML |
| `Config/` | 실행 설정과 전광판 배치 |
| `Validation/` | 기능별 검증 도구 |
| `Tools/GAMSTest/` | 운영 서버 없이 실행하는 독립 전광판 점검 프로그램 |

## 개발과 검증

Windows와 .NET 10 SDK를 사용한다. 최초 복원 또는 의존성 변경 시 `dotnet restore CowAuctionSmall.csproj`, 이후 빌드는 `dotnet build CowAuctionSmall.csproj --no-restore`로 수행한다. Visual Studio와 동시에 빌드할 때는 별도 `--artifacts-path`를 사용해 출력 경로가 겹치지 않게 한다.

- [경매·표시 검증 실행 안내](Validation/ReferenceAdoptionChecks/README.md)
- [이력 검증과 오프라인 실행 범위](Validation/OwnerHistoryChecks/README.md)
- [GAMSTest 점검 도구 사용법](Docs/기능/전광판_점검도구.md) · [점검 도구 오프라인 검증](Validation/DisplayTestChecks/README.md)

점검 도구는 Visual Studio에서 `GAMSTest`를 시작 프로젝트로 지정하거나 `dotnet run --project Tools/GAMSTest/GAMSTest.csproj`로 실행한다. 현장용 단일 EXE는 `dotnet publish Tools/GAMSTest/GAMSTest.csproj -c Release -p:PublishProfile=SingleFile-win-x64`로 게시한다.

제품 앱 실행과 일부 검증 경로는 운영 서버 연결을 일으킬 수 있으므로 각 검증 문서의 실행 범위를 확인한다. 소스·설정 변경과 실제 전광판 검증은 구분해서 기록한다.

문서 최신화 기준일: **2026-10-01**. 통합 상태바의 최종 배치·여백·검증 결과는 [화면 기능 문서](Docs/기능/화면표시_페이지전환.md#메인뷰-하단-상태-영역)에 반영했다. 루트의 과거 자료는 Docs로 모았으며, 정리 전 Git 백업과 프로젝트 설정 정리의 검증 결과는 [WORKLOG](WORKLOG.md)를 참고한다.

# .NET 10 전환 계획

> 문서 상태: **완료** · 정리일: 2026-10-01
> 2026-09-11 WORKLOG의 복원·Debug/Release 빌드·별도 게시 검증 기록과 현재 프로젝트의 net10.0-windows를 확인했다. 운영 배포 완료를 뜻하지 않는다.
> 현재 동작: [기능 문서](../../기능/README.md) · [전체 목차](../../README.md) · [작업 기록](../../../WORKLOG.md)

- 작성일: 2026-09-11
- 상태: 전환·복원·Debug/Release 빌드·별도 게시 검증 완료(2026-09-11). 운영 배포 미수행. 아래는 전환 전 설계 기록.
- 목표: 기존 WPF 경매 표시 동작을 유지하며 `net9.0-windows`에서 `net10.0-windows`로 전환
- 연관 작업: [SQLite 당일 캐시 도입 계획](SQLite_당일캐시_도입계획.md)

## 1. SQLite 계획과 분리하는 이유

.NET 전환은 실행 환경·컴파일·패키지·게시 호환성 작업이고, SQLite 도입은 데이터 보관·초기화·API 조회 정책 작업이다. 변경과 검증을 분리하면 장애 원인을 구분하고 이전 배포본으로 복귀하기 쉽다.

권장 순서는 .NET 10 전환 및 기존 기능 검증을 먼저 완료하고, 그 결과를 기준으로 SQLite를 도입하는 것이다. SQLite 사용 자체에 .NET 10이 필수인 것은 아니다. 운영 PC 환경 준비가 늦어지면 두 작업의 순서는 다시 결정할 수 있다.

.NET 10은 LTS이며 공식 지원 종료일은 2028-11-14이다. .NET 9의 지원 종료일은 2026-11-10이므로 전환 계획을 준비할 시점이다. 날짜는 2026-09-11 확인 기준이며 배포 전에 다시 확인한다. [.NET 지원 정책](https://dotnet.microsoft.com/en-us/platform/support/policy)

## 2. 현재 상태와 근거

| 항목 | 읽기 전용 확인 결과 |
|---|---|
| 프로젝트 | `CowAuctionSmall.csproj`: `net9.0-windows`, `UseWPF=true` |
| 게시 프로필 | `Properties/PublishProfiles/FolderProfile.pubxml`: `net9.0-windows` |
| 게시 대상 | `win-x64`, `SelfContained=false`, `PublishSingleFile=true` |
| 게시 위치 | `bin/AMS_Server` |
| 설치 SDK | `10.0.303`, `10.0.400` |
| 설치 .NET 10 런타임 | `Microsoft.NETCore.App 10.0.11`, `Microsoft.WindowsDesktop.App 10.0.11` |
| 개발 IDE 호환성 | 미확인 |
| 운영 PC Windows 버전·아키텍처·런타임 | 미확인 |
| 현재 코드의 .NET 10 빌드·실행 호환성 | 미확인 |

확인 명령은 `dotnet --list-sdks`, `dotnet --list-runtimes`이며 설치나 업데이트는 수행하지 않았다. SDK가 설치되어 있어도 프로젝트의 대상 프레임워크가 자동으로 .NET 10으로 변경되는 것은 아니다.

## 3. 변경 범위

| 파일 | 계획 |
|---|---|
| `CowAuctionSmall.csproj` | `TargetFramework`를 `net10.0-windows`로 변경 |
| `Properties/PublishProfiles/FolderProfile.pubxml` | 게시 대상 프레임워크를 `net10.0-windows`로 맞춤 |
| `WORKLOG.md` | 전환 구현·검증 완료 후 결과 기록 |

초기 범위는 위 파일로 제한한다. 현재 패키지 버전, 업무 로직, XAML 스타일, 언어 문법을 일괄 변경하지 않는다. 호환성 문제로 추가 파일 수정이나 패키지 변경이 필요하면 증상·근거·정확한 파일을 보고하고 추가 승인을 받는다.

기본 빌드 출력은 `bin/Debug/net10.0-windows/`, `bin/Release/net10.0-windows/`로 바뀐다. 기존 게시 프로필의 `bin/AMS_Server` 경로는 프레임워크 폴더와 별개이므로 자동으로 분리되지 않는다. 검증용 게시 경로는 기존 배포본과 겹치지 않는 위치로 구현 전에 확정한다.

## 4. 패키지 및 변경 사항 검토

현재 직접 참조 버전을 기준으로 호환성 검증을 시작한다.

| 패키지 | 현재 버전 | 중점 확인 |
|---|---|---|
| CommunityToolkit.Mvvm | 8.4.0 | 바인딩·Command·메시지 전달 |
| EzDotNetty | 1.0.21 | 연결·수신·재연결·종료 |
| Microsoft.Extensions.DependencyInjection | 10.0.2 | 서비스 생성·해제 |
| Microsoft.Xaml.Behaviors.Wpf | 1.1.135 | 화면 이벤트·Behavior |
| Newtonsoft.Json | 13.0.4 | API 응답 파싱 |
| NLog | 6.0.6 | 설정 로드·파일 출력 |
| WpfAnimatedGif | 2.0.2 | GIF 표시·반복 진입·자원 해제 |

직접·전이 의존성의 대상 프레임워크와 네이티브 구성 요소를 확인한다. 위 목록은 호환성 보증이 아니며, 패키지의 최신 버전으로 일괄 업데이트하지 않는다.

공식 [.NET 10 호환성 변경 목록](https://learn.microsoft.com/en-us/dotnet/core/compatibility/10)을 기준으로 다음 항목의 실제 해당 여부를 검토한다.

- WPF의 빈 ColumnDefinitions·RowDefinitions와 잘못된 DynamicResource 사용에 관한 변경
- SDK·컴파일러·기본 라이브러리의 소스 및 동작 변경
- 단일 파일 앱의 네이티브 라이브러리 검색 동작 변경

현재 게시가 단일 파일 방식이므로 마지막 항목은 SQLite 후속 도입과 연결된다. 일반 Debug 실행 성공만으로 배포본의 SQLite 로딩 성공을 판단하지 않는다.

## 5. 실행 환경과 배포

- 개발 PC의 SDK 및 IDE 지원 여부와 운영 PC의 지원 Windows 버전·x64 환경을 확인한다.
- 현재 게시 방식은 런타임 미포함이므로 운영 PC에 호환되는 .NET 10 Desktop Runtime이 필요하다. 기본 .NET Runtime이나 ASP.NET Runtime만으로 WPF 실행 환경을 대체하지 않는다. [Windows 설치 문서](https://learn.microsoft.com/en-us/dotnet/core/install/windows)
- 배포 시점의 지원되는 최신 서비스 패치 적용 여부를 확인한다. 이번 계획 작성으로 SDK나 런타임을 설치하지 않는다.
- `SelfContained=false`, `PublishSingleFile=true`, `win-x64`를 기본 유지한다. 배포 방식 변경이 필요하면 별도 검토한다.
- 기존 정상 배포본과 운영 설정의 복귀 수단을 확보하고 별도 시험 위치에서 검증한다. 개인정보 포함 데이터나 로그를 불필요하게 복제하지 않는다.
- 운영 교체와 복귀는 승인된 배포 절차로 수행한다. Git 변경 폐기나 기존 `bin` 폴더의 직접 정리로 처리하지 않는다.

## 6. 구현 순서 및 복원 선행 조건

1. 운영 PC·IDE·기존 패키지·게시 방식을 확인하고 현행 .NET 9의 검증 기준을 확보한다.
2. 정확한 파일 수정 범위와 프로젝트 규칙 예외를 확정한다.
3. 프로젝트와 게시 프로필의 대상 프레임워크를 함께 변경한다.
4. 새 대상에 맞는 패키지 자산을 승인된 절차로 복원한다.
5. Debug·Release 빌드와 기존 기능의 회귀 검증을 수행한다.
6. 별도 시험 경로에 게시하고 운영과 동등한 PC에서 실행 검증한다.
7. 검증 결과를 기록하고 .NET 10을 SQLite 후속 작업의 기준으로 확정한다.

대상 프레임워크 변경 후 기존 .NET 9 자산만으로 `--no-restore` 빌드가 가능하다고 가정하지 않는다. .NET 10 대상과 게시 RID에 맞는 복원 자산이 필요하다.

루트 `AGENTS.md`는 `.csproj` 수정과 패키지 복원을 일반 수정 승인에 포함하지 않고 금지한다. 사용자의 직접 준비 또는 명시적인 규칙 예외가 선행되어야 한다. 계획 문서 작성 요청은 실제 전환·복원·배포 승인에 해당하지 않는다.

## 7. 검증 계획

새 대상의 복원 자산 준비 후 수행할 명령:

```powershell
dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore
dotnet build .\CowAuctionSmall.csproj --configuration Release --no-restore
```

게시 명령은 검증용 출력 경로와 복원 범위를 확정한 후 작성한다. 기존 게시 디렉터리를 덮어쓰는 기본 프로필 실행은 검증 계획만으로 수행하지 않는다.

- [ ] 빌드 성공과 기존 대비 새 경고·오류 확인
- [ ] XML 설정·이미지·폰트·로고·NLog 설정 로드
- [ ] 주요 전광판 해상도와 사업장별 View 표시
- [ ] 단일·일괄 경매 진행·낙찰·유찰·날짜 변경
- [ ] 바인딩 갱신·빈 값·금액·문자열·JSON 파싱
- [ ] API 인증·현황 조회·Netty 수신·연결 끊김·재연결
- [ ] 페이지 동기화·타이머·애니메이션·GIF·반복 진입 및 종료
- [ ] 기존 기준 대비 CPU·메모리·장시간 실행과 API 호출량 회귀
- [ ] 단일 파일 게시본의 리소스·네이티브 의존성 로드
- [ ] .NET 10 Desktop Runtime이 준비된 시험 PC에서 실행
- [ ] 기존 정상 배포본으로 복귀하는 절차 확인

현재 빌드·테스트·운영 배포 결과는 모두 미수행이다. .NET 전환만으로 API 호출량이 줄어들 것이라고 기대하지 않는다.

## 8. 완료 기준과 SQLite 후속 작업

프로젝트·게시 설정이 .NET 10으로 일치하고, 빌드·게시본 실행·경매 화면 및 통신 회귀 검증이 통과하면 전환을 완료한다. 실패 또는 미검증 범위는 숨기지 않고 기록한다.

이후 SQLite 계획의 적용 대상을 .NET 10으로 확정하고 라이브러리 버전·출력 경로·단일 파일 게시 로딩을 검증한다. SQLite 추가 전후를 비교할 수 있도록 .NET 10에서의 정상 동작 기준을 먼저 남긴다.

## 9. 참고

- [.NET 지원 정책](https://dotnet.microsoft.com/en-us/platform/support/policy)
- [.NET 업그레이드 개요](https://learn.microsoft.com/en-us/dotnet/core/porting/)
- [.NET 10 호환성 변경](https://learn.microsoft.com/en-us/dotnet/core/compatibility/10)
- [Windows의 .NET 설치](https://learn.microsoft.com/en-us/dotnet/core/install/windows)

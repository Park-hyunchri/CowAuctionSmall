# CODEX HANDOFF

## 2026-09-13 GAMSTest STOP 외부 명령 종료 처리

- `GAMSTest/Views/ControlWindow.xaml.cs`의 수신 콜백에 `Dispatcher.BeginInvoke`, `_isClosing` 확인 및 대소문자 무관 STOP 분기 적용
- STOP은 `Close()`로 기존 서버 취소·타이머 중지·표출창 닫기·애플리케이션 종료 경로를 호출한다. 출하 AMS 실행 함수는 호출하지 않는다.
- 기존 NUMBER/RED/GREEN/BLUE 처리와 단독 실행 시 번호 자동 시작은 유지한다.
- 빌드: `dotnet build GAMSTest\GAMSTest.csproj --configuration Debug --no-restore` 성공, 오류 0개 / 경고 1개(CS0067, `ControlWindowViewModel.cs(66,95)`)
- 이전 사용자 확인 Release/win-x64 빌드·게시 성공 및 자체 동작 정상은 수정 전 결과이며, 해당 Release 경고 개수는 미확인이다.
- 사용자 확인: STOP 수신 시 제어창·표출창·GAMSTest 프로세스 종료 정상, 출하 AMS 실행 상태 유지
- 사용자 확인: 재실행 시 번호 자동 표시와 Named Pipe 재연결 후 NUMBER/RED/GREEN/BLUE 전환 정상
- STOP 종료 및 Named Pipe 외부 연동 확인 완료. 기존 종료 경로의 번호 재표시로 종료 직전 번호 화면이 잠깐 보일 수 있다.
- 이번 수정 파일: `GAMSTest/Views/ControlWindow.xaml.cs`, `WORKLOG.md`, `CODEX_HANDOFF.md`. 기존 다른 소스 변경은 보존했다.
- 실행 중인 GAMSTest 명령 전송 및 Git commit/push: 미수행

## 2026-09-13 GAMSTest Release/win-x64 검증 결과

- 사용자가 확인한 `GAMSTest` Release/win-x64 빌드 성공
- 사용자가 확인한 Release/win-x64 게시 성공
- 사용자가 수행한 GAMSTest 자체 동작 테스트 정상
- 경고 개수: 미확인
- Named Pipe 외부 연동 테스트 여부: 정상(사용자 확인, STOP 및 NUMBER/RED/GREEN/BLUE)
- 이번 반영 범위: `WORKLOG.md`, `CODEX_HANDOFF.md` 기록만 갱신
- 소스 수정 및 Git commit/push: 미수행

## 2026-09-12 GAMSTest MockDataFactory 보강

- 패널 번호 `% 4` 기반 성별(암/수/거세/암소) 및 축종 순환 적용
- 진행·유전·유찰·낙찰 공통 비고 `테스트 데이터 입니다`, 최저가·중량·개체번호 매핑
- EPD 수치·등급(A, B, C, D) 및 농협 우량·뿌리농가 플래그 설정
- 낙찰가·낙찰자 정보 보강으로 화면 공백 제거
- 빌드: `dotnet build GAMSTest\GAMSTest.csproj --configuration Debug --no-restore`
- 결과: 성공 / 오류 0개 / 경고 1개
- 기존 CowAuctionSmall 관련 추적 파일 diff: 변경 없음(0건)

## 2026-09-12 GAMSTest 운영 프로그램 실행 기능

- ControlWindow에 `CowAuctionSmall.exe` 실행 및 Bring to Front 버튼 추가
- `user32.dll`의 `ShowWindowAsync(SW_RESTORE)`, `SetForegroundWindow` 적용
- 실행 중인 CowAuctionSmall 창은 복원 후 전면 활성화
- 미실행 시 배포 경로 및 개발 빌드 경로를 탐색하여 `WorkingDirectory` 유지 실행
- 빌드: `dotnet build GAMSTest\GAMSTest.csproj --configuration Debug --no-restore`
- 결과: 성공 / 오류 0개 / 경고 1개
- 기존 CowAuctionSmall 관련 추적 파일 diff: 변경 없음(0건)
- Git commit/push: 미수행

## 2026-09-12 GAMSTest 작업 인계

- ControlWindow를 [번호], [뷰 페이지] 버튼 구조로 통합
- [뷰 페이지]는 진행, 유전능력, 유찰, 낙찰을 5초 주기로 순환
- 번호 또는 RGB 버튼 클릭 시 순환 타이머 중지
- RGB 실행 시 패널 자식 요소를 제거하고 순수 단색 표시
- SetCustomDisplay 기반 128x128 View 생성 및 users.XML의 AuctionHouseCode, LowestPriceTitle, BidderName 반영
- Mock gValues를 각 View의 DataContext로 주입
- 기존 운영 DLL 참조로 기존 View와 서비스 재사용
- ProjectReference 대신 DLL 참조를 사용하여 GAMSTest 중복 포함 문제 방지

### 빌드 및 상태
- 명령: `dotnet build GAMSTest\GAMSTest.csproj --configuration Debug --no-restore`
- 결과: 성공 / 오류 0개 / 경고 1개
- 경고: `GAMSTest/ViewModels/ControlWindowViewModel.cs(46)` CS0067 `ActionCommand.CanExecuteChanged` 이벤트 미사용
- 기존 CowAuctionSmall 관련 추적 파일 diff: 변경 없음
- Git commit/push: 미수행

## 프로젝트
- 프로젝트명: CowAuctionSmall (출하 AMS / 가축 경매 전광판 클라이언트)
- 기술 스택: C# (.NET 9.0), WPF, MVVM, Windows
- 주요 모듈: Models, ViewModels, Views, Services, NetProto, Config, Utils, Resources

## 마지막 업데이트
- 날짜: 2026-09-04
- 작성자: Codex / 사용자

## 현재 작업 목적
- 가축 경매 현장 전광판 프로그램 안정성 유지
- 회사 PC와 집 PC 간 작업 인계 상태 유지

## 현재까지 완료
- `8746a4f` 네티 재연결 안정화, 상황별 API 폴링 주기 최적화 및 디스플레이 패널 잔재 초기화
- `52c554d` 횡성축협 성별 표출 로직 수정
- `453684c` 횡성축협 행사용/경매용 설정에 따른 진행 화면 타이틀 매핑 및 항목 숨김 적용
- `949ea2a` 횡성축협 분양가 행사 모드 조건부 항목 숨김 및 레이아웃 재배치
- `e09d3c4` 횡성축협 낙찰 화면 전용 분기 우선 처리

## 현재 작업 중
- 해남진도축협(`8808990656106`) 통신 안정화 및 일괄/단일 경매 표출 보완 완료
- 구현·Debug 빌드는 완료됐으며, 사용자의 커밋 요청 대기 중

## 현재 Git 상태
- Branch: `master`
- HEAD: `8746a4f` (`origin/master`와 동일)
- 원격 상태: `HEAD`와 `origin/master` 동일
- Working tree 변경:
  - `Services/NettyAsyncMsgProcess.cs` — 일괄 SD `F` 마감 신호 전달
  - `Services/ServerGetData.cs` — 재연결 직렬화, 일괄 종료 정리, 강제 재조회, 최신 스냅샷 동기화
  - `Services/DisplaySelect.cs` — 일괄 경매 중 화면 회전 정지 및 진행 페이지 바인딩 해제
  - `WORKLOG.md` — 해남진도 작업 기록
  - `CODEX_HANDOFF.md` — 현재 인계 문서 갱신

## 미완료 작업 및 확인 필요 사항
- 실제 해남진도 서버에서 2002 중복 접속 시 30초 동안 재연결하지 않는지 확인
- 실제 128×128 전광판에서 AS 8006·SD `F` 뒤 낙찰/유찰 화면이 즉시 전환되는지 확인
- AS 8001 refresh 및 SZ 수신 후 계류대 위치·비고 변경이 반영되는지 확인
- 주요 화면 상태, 화면 전환·반복 진입, 해상도별 표시, 데이터 바인딩 및 통신 회귀 확인 필요

## 이번 작업의 범위·주의사항
- 대상은 해남진도축협 코드 `8808990656106`이다.
- 장성축협 코드 `8808990657103`의 페이지 순환 및 관련 분기는 보류 지시에 따라 수정하지 않았다.
- 해남진도는 전용 1페이지 진행 화면(`HaenamJindo.xaml`)을 유지하며 페이지 순환 대상에 추가하지 않았다.
- 실제 현장 확인 전에는 이 미커밋 변경을 임의로 되돌리거나 확장하지 않는다.

## 다음 작업 순서
1. `git status` 및 최근 커밋 확인
2. 사용자 신규 작업·오류 요청 접수
3. `AGENTS.md`, `CODEX_HANDOFF.md`, 관련 소스 분석
4. 문제 현상, 원인, 수정 대상 파일, 영향 범위, 검증 계획 보고
5. 사용자의 명시적 수정 승인 후 승인된 파일만 수정
6. 구현 후 최소 빌드·테스트 수행 및 WORKLOG.md 기록

## 빌드·테스트 상태
- 최신 작업 빌드: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` 성공
- 결과: 오류 0개, 기존 경고 100개
- 최신 작업의 실제 전광판·운영 통신 수동 확인: 미수행
- 자동화 테스트: 미수행

## 참고 파일 및 주의사항
- `AGENTS.md`: 수정 승인, 파일 범위, 빌드·테스트 및 보고 규칙
- `WORKLOG.md`: 완료 작업 기록. 실제 Git 상태와 함께 확인
- `기록용.txt`: 과거 이력 참고용이며 현재 소스를 우선함
- `error.md`: 사용자가 요청하지 않는 한 CowAuctionSmall 분석 대상에서 제외
- 예천, 강진완도, 무진장, 정읍 등 축협별 조건문과 설정값 보존
- `.csproj`, NuGet 패키지, Git 설정, 운영 설정값은 별도 승인 없이 변경하지 않음

## Git 작업 원칙
- Commit·Push는 사용자의 명시적 요청이 있을 때만 수행
- 커밋 전 `git status`, 변경 파일 목록, 커밋 메시지를 확인
- 현재 해남진도 안정화·인계 문서 작업의 커밋: 미생성
- 현재 인계 문서 갱신 작업의 Push: 미수행

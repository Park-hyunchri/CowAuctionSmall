# CowAuctionSmall 작업 기록

## 2026-10-15 — GAMSTest 해남진도 친자 배지 옵션 및 최신 화면 반영

- 목적·승인: 사용자가 GAMS 테스트에서 해남진도 친자 배지가 라임색이고 송아지 어미번호가 남는 현상을 검토 요청한 뒤, 제안한 도구 소스 2개·검증·기능 문서·WORKLOG의 수정을 승인했다.
- 원인: GAMSTest는 제품 ViewFactory와 실제 View를 사용하지만 DisplaySelect를 거치지 않아 해남진도 UseCalfPaternityBadge 옵션을 전달하지 않았다. MockDataFactory의 배지 색상도 Lime으로 고정됐다. 로컬 Tools/GAMSTest/bin/Debug/net10.0-windows의 EXE·제품 DLL은 10월 12일 빌드로 송아지 어미번호 제거 전 화면이었다. 첨부 화면의 실제 실행 경로는 조회 시 프로세스가 없어 미확인이다.
- 변경 파일·내용: `Tools/GAMSTest/Services/DisplayTestController.cs`에서 진행 호스트 생성·재사용 시 해남진도 코드에만 UseCalfPaternityBadge를 전달한다. `Tools/GAMSTest/Services/MockDataFactory.cs`의 일반 배지 샘플은 Yellow로 하고 장성의 Lime은 유지했다. `Validation/DisplayTestChecks/ControllerChecks.cs`에 합성 Board/users XML, 축종·친자 표시 설정·진행/유전/낙찰/유찰/대기 반복 전환, 최신 어미번호·최저가 좌표·배지 비겹침·색상·캐시 및 다른 축협 회귀 검사를 추가했다. `Docs/기능/전광판_점검도구.md`에 실제 옵션·샘플·재빌드 기준을 기록하고 이 WORKLOG를 갱신했다.
- 영향·보존: 승인된 5개 파일만 수정했다. 제품 화면 XAML·파서·운영 XML·프로젝트/패키지·자동 버전·버튼 UI 변경 없음. 기본 테스트 축종은 송아지 1이고 전체 슬롯에 같은 샘플 축종을 사용한다. 비육우·번식우는 자동 검사의 테스트 설정 객체를 바꿔 확인했다. 빌드 영향은 제품 참조와 GAMSTest·검증 프로젝트이며 운영 배포본 교체는 하지 않았다.
- 빌드: `dotnet build Tools/GAMSTest/GAMSTest.csproj --configuration Debug --no-restore -m:1 -v:minimal` 최초 CS2012(obj 제품 DLL 쓰기 권한) 실패 후 권한 승인받아 동일 명령 성공, 경고 0·오류 0. 제품과 GAMS DLL을 함께 갱신했다. 기존 복원 자산이 있는 TEMP/CowAuction-SokchoRestore-20261012를 사용한 `dotnet build Validation/DisplayTestChecks/DisplayTestChecks.csproj --configuration Debug --artifacts-path <해당 경로> --no-restore -m:1 -v:minimal`도 성공, 경고 0·오류 0. restore·clean 없음.
- 전체 검사 제한: DisplayTestChecks --skip-measurements --screenshots 실행은 기존 SokchoChecks.cs:111의 개체번호 기대값(`Sokcho view displays the restored entity number`)에서 중단됐다. 속초 소스·해당 검사·Program.cs는 이번에 변경하지 않았다. 전체 통과로 판정하지 않는다.
- 제어 경로 검증: TEMP/GAMSTest-ControllerRunner-35947d58967e4275b7f22bbfe9733e0c의 임시 실행 진입점과 저장소의 실제 ControllerChecks.cs를 SDK csc로 컴파일했다. 저장소 Program.cs를 바꾸지 않고 ControllerChecks.Run(false) 전체를 실행해 938개 통과했다. 임시 진입점의 최초 참조 중복(WindowsBase)과 NLog 초기 로드 문제는 임시 검사기에서 참조·로더 진입점을 바로잡았다. 최종 dotnet exec는 기본 GAMS 출력 폴더의 실제 DLL과 GAMSTest.runtimeconfig.json을 사용했으며 캐시 출력에 대한 실행도 938개 통과했다. 제품 App·운영 서버 접속 없음.
- 화면·파일 확인: TEMP/GAMSTest-Haenam-20261015-Screenshots의 송아지/비육우 진행 PNG를 직접 확인했다. 송아지는 어미번호 없음·노란 친자 배지·15픽셀 행 간격, 비육우는 어미번호 유지·진행 배지 없음이다. 기본 GAMS EXE·DLL·포함 제품 DLL의 파일 버전은 1.1.9784.18463이며 제품 출력과 GAMS 포함 CowAuctionSmall.dll SHA-256이 일치한다.
- Git·문서: git status·관련 diff 및 git diff --check 확인. 기존 CODEX_HANDOFF.md·운영 XML 3개·WORKLOG 변경 보존. CODEX_HANDOFF.md는 이번에 미갱신. commit hash 미생성, push 미수행. 제안 메시지: `fix: GAMSTest 해남진도 친자 배지 표시 동기화`.
- 추가 확인·미수행: 수정 실행본은 Tools/GAMSTest/bin/Debug/net10.0-windows/GAMSTest.exe다. 기존 다른 경로의 단일 EXE 배포본은 교체하지 않았다. 실제 전광판·사용자 첨부 실행본·운영 XML로 앱 실행·Release/전체 솔루션 빌드·게시·배포·100/200슬롯 장시간 측정 미수행. 기존 전체 검사의 속초 개체번호 기대값 실패는 별도 검토 대상이다.

## 2026-10-15 — 해남진도 송아지 검사일·최저가 줄 간격 확대

- 목적·승인: 사용자가 첨부한 송아지 표출 화면을 기준으로 하단 비고를 제외한 브루셀라·구제역·최저 3줄의 간격 조정을 요청했다. 좌표와 수정 파일·검증 방법을 안내한 후 같은 화면의 후속 조정으로 진행했다.
- 원인: 송아지의 어미번호 행 제거 후 세 항목은 59·71·83픽셀의 12픽셀 간격으로 모여 있었고 최저가 아래에 여유가 남았다.
- 변경 파일: `Views/Size128_128/Running/CustomAuctionRunning1/HaenamJindo.xaml`의 송아지 전용 구제역 좌표를 71→74, 최저가를 83→89, 같은 행 산차를 85→91픽셀로 변경했다. 브루셀라 59픽셀을 기준으로 15픽셀 간격을 적용했다. `Validation/ReferenceAdoptionChecks/BindingChecks.cs`의 해당 좌표 기대값을 갱신했다. `Docs/기능/화면표시_페이지전환.md`에 새 좌표를 반영했고 이 WORKLOG 기록을 추가했다.
- 영향·보존: 해남진도 128×128 송아지 진행 화면의 세 줄과 산차 배치만 조정했다. 하단 비고·친자 배지의 위치, 비육우·번식우 좌표, 다른 축협 및 낙찰·유찰 양식, 운영 XML·프로젝트·패키지·자동 버전은 유지한다. 빌드 영향은 제품과 참조 검증 프로젝트다. 앞선 승인 작업 및 기존 사용자 변경을 보존했다.
- 빌드·테스트: `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --configuration Debug --no-restore -m:1 -v:minimal` 제품 및 검증 프로젝트 성공, 경고 0·오류 0. `dotnet Validation/ReferenceAdoptionChecks/bin/Debug/net10.0-windows/ReferenceAdoptionChecks.dll note-host` 145개 통과. 실제 WPF 좌표 기준 최저가·산차와 친자 배지 비겹침, 축종 전환 시 성우 기존 좌표 복귀, 비고 및 배지 회귀를 확인했다. git diff --check 통과. 기존 전체 binding의 화면 개수 기대값 실패는 앞선 기록과 같으며 이번에 재실행하지 않았다.
- 문서·Git: WORKLOG.md 갱신, CODEX_HANDOFF.md는 이번에 미갱신. git status 및 관련 diff 확인. commit hash 미생성, push 미수행. 제안 메시지: `fix: 해남진도 송아지 진행 정보 줄 간격 조정`.
- 추가 확인·미수행: 실제 전광판에서 줄 간격과 하단 가독성 확인 필요. 운영 앱 실행·서버 접속·Release/전체 솔루션 빌드·게시·배포 미수행.

## 2026-10-15 — 해남진도 송아지 진행 양식 및 친자 배지 조정

- 목적·승인: 사용자가 해남진도 진행 화면의 송아지만 어미번호 행을 제거하고 비고 앞에 노란 친자 배지를 표시하며, 비육우·번식우는 현재 양식을 유지하고 배지 없이 표시하도록 요청한 뒤 제안한 파일 범위의 진행을 승인했다.
- 문제·원인: 해남진도 최저가 행은 세로 95픽셀이고 공통 친자 배지는 108픽셀에 위치해 글자 영역이 겹쳤다. 기존 호스트의 배지 표시에는 축종 구분이 없었다. 현재 일반 파서 색상은 Yellow이며 Lime은 장성 전용 값이지만, 이번 해남진도 진행 표시에서는 기존 스냅샷 색상이 Lime이어도 노랑으로 표시하도록 했다.
- 변경 파일·내용: `Views/Size128_128/Running/CustomAuctionRunning1/HaenamJindo.xaml`에서 CowDistinction=1일 때만 어미번호 행을 숨기고 브루셀라·구제역·최저가·산차를 12픽셀 올렸다. 다른 축종의 원래 좌표는 유지한다. `Presentation/Display/DisplaySelect.cs`에서 해남진도 진행 호스트에만 UseCalfPaternityBadge를 설정한다. `Views/Size128_128/Running/RunningNoteHost128.xaml.cs`에는 표시 옵션용 의존성 속성만 추가했다. 동명 `.xaml`에는 옵션 활성 시 송아지만 배지를 표시하고 글자·테두리를 Yellow로 고정하며 실제 배지 표시 여부에 맞춰 비고 시작 위치를 전환하도록 했다. 업무 로직을 code-behind에 추가하지 않았다.
- 검사·문서 파일: `Validation/ReferenceAdoptionChecks/BindingChecks.cs`에 실제 화면 및 DisplayRunningPageNum을 사용하는 해남진도 검사들을 추가했다. `Docs/기능/화면표시_페이지전환.md`에 축종별 표시와 진행 화면 한정 범위를 기록했다. 이 WORKLOG를 최신 항목으로 추가했다. CODEX_HANDOFF.md는 이번 작업에서 갱신하지 않았다.
- 승인·영향: 승인된 소스 4개, 검증 1개, 기능 문서 및 작업 기록만 변경했다. 해남진도 128×128 진행 화면의 송아지 레이아웃·공통 비고 표시 옵션에 한정한다. 비육우·번식우의 기존 양식과 원본 어미번호·친자 판정·색상 데이터, IsPaternityMatch=N 처리, 다른 축협·낙찰·유찰 표시 규칙은 유지한다. 프로젝트·패키지·운영 XML·자동 버전 및 성별 표시 규칙 변경 없음. 빌드 영향은 제품과 참조 검증 프로젝트다.
- 빌드: `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --configuration Debug --no-restore -m:1 -v:minimal`로 제품 및 검증 프로젝트 성공, 경고 0·오류 0. restore·clean·프로젝트 설정 변경 없음.
- 전용·회귀 검증: `dotnet Validation/ReferenceAdoptionChecks/bin/Debug/net10.0-windows/ReferenceAdoptionChecks.dll note-host` 145개 통과. 일반 Application과 합성 데이터로 실제 WPF 바인딩을 확인했으며 제품 App·UDP/페이지 타이머·운영 서버 연결은 시작하지 않았다. 송아지↔비육우↔번식우 반복 전환, 송아지의 실제 최저가/산차 글자 영역과 배지 비겹침, 친자 배지의 노란 글자·테두리, 비육우·번식우 숨김, 친자 없음·설정 비활성 데이터·복원 시 비고 위치, 다른 진행 페이지·다른 축협의 기존 색상, 낙찰·유찰의 기존 배지 유지, 애니메이션 해제 및 바인딩 오류 없음을 확인했다.
- 전체 검사 제한: 같은 검증 실행 파일의 `binding`은 기존 `binding coverage includes all 33 running pages` 고정 개수 검사에서 중단됐다. 이번 작업에서 진행 View 클래스나 해당 기대 개수는 변경하지 않았다. 전체 바인딩 회귀 통과로 판정하지 않으며 승인 목적 밖의 화면 개수 기준은 수정하지 않았다.
- Git·보존: 시작 시 기존 CODEX_HANDOFF.md·운영 XML 3개·WORKLOG.md 변경을 보존했고 git status 및 관련 diff로 이번 작업과 구분했다. git diff --check 통과. commit hash 미생성, push 미수행. 제안 메시지: `fix: 해남진도 송아지 진행 친자 배지 겹침 수정`.
- 추가 확인·미수행: 실제 전광판·운영 서버·실제 수신 패킷·Release/전체 솔루션 빌드·게시·배포 미수행. 현장 송아지/비육우/번식우, 친자일치 유무·표시 설정, 긴 비고 스크롤 및 진행↔낙찰↔유찰 반복 전환을 확인해야 한다. 전체 binding의 화면 개수 기준 불일치는 별도 검토 대상이다.

## 2026-10-15 — 영천 염소 일괄경매 주황색 테두리 복원

- 목적·승인: 이전 버전에서 정상 운영된 영천 염소 일괄경매 테두리가 최신 버전에 누락됐다는 검토 요청 후, 사용자가 제안한 소스 3개 파일의 복원을 승인했다. 사용자는 이전 버전의 2026-10-14 영천 염소경매 정상 운영을 확인했으며, 최신 수정본의 현장 검증과는 구분한다.
- 원인: 최신 소스에 전용 표시 속성, DisplaySelect의 상태 구독·갱신·해제, 주황색 XAML 트리거가 누락돼 있었다. 최신 AuctionDisplayStateMessage는 이전 버전과 형식이 다르고 경매 데이터 갱신보다 먼저 발행된다.
- 변경 파일: `ViewModels/AuctionContPanelViewModel.cs`에 IsBatchGoatBorderHighlighted와 변경 알림을 복원했다. `Presentation/Display/DisplaySelect.cs`에 영천·일괄·염소·상태 11 조건, 최신 경매 상태 수신·보관·UI Dispatcher 갱신·구독 해제 및 해제 후 콜백 무시를 추가했다. 새 화면과 재사용 화면에도 마지막 수신 상태를 반영하며 수신 상태가 없는 초기 화면은 기존 일괄 진행 플래그를 사용한다. `Views/Size128_128/Running/Standard_Goat_Run.xaml`에 고정 #FFA500 테두리와 강조 중 점멸 억제를 복원했다.
- 승인·영향 범위: 승인된 소스 3개와 이 WORKLOG 기록만 변경했다. 적용 대상은 영천 128×128 염소 일괄경매 진행 화면이다. C#/WPF/MVVM 구조, 통신 파서, 타 사업장·축종·단일경매의 기존 표시, 낙찰·유찰 화면, 프로젝트·패키지·자동 버전·설정은 변경하지 않았다. 제품 소스 변경으로 제품 및 참조 검증 프로젝트가 다시 빌드된다.
- 빌드: 최초 `dotnet build CowAuctionSmall.csproj --configuration Debug --no-restore -m:1 -v:minimal`은 WPF Application과 동명 네임스페이스 충돌 CS0234로 실패했다. 추가한 코드에서 System.Windows.Application을 명시한 뒤 `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --configuration Debug --no-restore -m:1 -v:minimal`로 제품 및 검증 프로젝트 빌드 성공, 경고 0·오류 0. restore·clean·프로젝트 파일 변경 미수행.
- 전용 검증: 저장소 외 TEMP/CowAuction-GoatBorder-f7d863c1539547779c44c2628ac60fd4의 일회성 C# 검사기를 설치 SDK csc로 컴파일하고 dotnet exec로 실행해 159개 통과. 제품 App·통신·타이머를 시작하지 않고 컴파일된 제품의 실제 메서드와 WPF View를 검사했다. 시작·중복 시작·종료, 사업장/축종/상태/경매방식/단계 조합, 실제 주황색·점멸 억제·검정 복귀, 단일 전환·연결 상태 초기화, 백그라운드 수신의 UI 전환, 해제 후 예약 콜백, 화면 생성·재사용, 플래그 갱신 순서와 초기 표시를 확인했다. 검사기 최초 실행은 공통 리소스 로드 누락으로 실패해 검사기에 DisplayResources.xaml을 로드한 후 통과했다. 저장소에 테스트 파일을 추가하지 않았다.
- 기존 검사: `dotnet Validation/ReferenceAdoptionChecks/bin/Debug/net10.0-windows/ReferenceAdoptionChecks.dll footer` 119개 통과. 동일 실행의 `stage4`는 DisplayLifetimeChecks.cs:221의 횡성 낙찰 화면 기대값 QQuriSold에서 실패했다. 현재 SetCustomDisplay는 횡성에 HoengseongSold를 반환하며 해당 소스와 검사 파일은 Git diff상 변경 없음이다. 승인 범위 밖의 기대값/낙찰 분기는 변경하지 않았다. 전체 회귀 통과로 판정하지 않는다.
- Git·문서: git status 및 관련 diff로 소스 3개 변경과 기존 사용자 변경(CODEX_HANDOFF.md, 운영 XML 3개, WORKLOG.md)을 구분했다. 기존 WORKLOG 내용 보존, CODEX_HANDOFF.md는 이번 작업에서 미갱신. git diff --check 통과. commit hash 미생성, push 미수행. 제안 메시지: `fix: 영천 염소 일괄경매 주황색 테두리 복원`.
- 추가 확인·미수행: 실제 전광판/운영 서버/패킷, Release·전체 솔루션 빌드, 게시·배포 미수행. 현장에서 일괄 시작·종료·재시작, 진행 중 화면 재진입, 낙찰 청색·유찰 적색 유지 및 타 축협·해상도 회귀를 확인해야 한다. 기존 stage4 횡성 낙찰 기대값 불일치는 별도 검토 대상이다.

## 2026-10-13 — 현재 통합 소스 origin 커밋·푸시

- 목적·승인: 사용자가 현재까지의 내용을 기록하고 GitHub 커밋·푸시하도록 요청했다. 두 원격 중 사용자는 `origin`을 선택했고, `origin/master` 기준 새 브랜치 및 자격 증명 없는 설정 템플릿으로 게시하는 방식을 승인했다.
- 원인·범위: 현재 작업 브랜치는 GitHub master와 분기되어 있었고 기존 로컬 커밋의 `Config/users.XML`에 사용자 ID·암호가 포함돼 있었다. 현재 소스 스냅샷만 `origin/master` 최신 `5604778`의 자식 커밋으로 만들어 기존 비공개 이력을 전송하지 않았다.
- 이력·설정 보호: `WORKLOG.md`의 원격 104개와 현재 96개 항목을 합쳐 181개 제목을 보존했다. 같은 제목의 빈 현재 항목은 원격 본문을 유지했다. `Config/users.XML`, `Config/개users.XML`, `Config/실users.XML`은 GitHub 커밋용 템플릿에서 UserID·Password·Address·AddressEPD·AddressQcn·Port·PageTimerPort 값을 비웠다. 원래 작업 폴더의 운영 설정은 변경하지 않았다.
- GitHub 결과: 새 브랜치 `codex/current-cowauctionsmall-20261013`의 소스 스냅샷 커밋 `70ea39b919fae91e734df4e75711dda4ffff92c3`을 `origin`에 푸시했다. 이 스냅샷은 origin/master 최신 커밋을 부모로 둔 단일 커밋이며, 기존 작업 브랜치와 그 커밋 이력은 푸시하지 않았다. 게시 결과는 이어지는 문서 커밋으로 같은 브랜치에 기록했다.
- 검증: 설정 템플릿의 인증값·운영 주소·포트가 비어 있고 원격 WORKLOG 104개 본문이 모두 보존됨을 확인했다. 구현 검증은 앞선 기록의 Debug 솔루션 빌드 및 ReferenceAdoptionChecks 컴파일 경고 0·오류 0을 참조한다. 자동 검사는 이번 Git 작업에서 재실행하지 않았다.


## 2026-10-12 — 속초 번호 분할 강조 및 익산·군산 비고 친자 보완

- 목적·승인: 사용자가 속초 128×64 진행 화면 번호의 한 TextBlock 분할·가운데 글자색 강조와 익산·군산의 비고 문구 기반 친자 보완·문구 제거를 검토한 뒤 구현을 승인했다.
- 원인: 속초 진행 1·2페이지는 전체 번호와 짧은 번호를 별도 TextBlock으로 겹쳐 표시했다. 익산·군산(`8808990227283`) 파서는 지역만 보정하고 친자 판정은 공통 data[23] 값에 의존해 비고 문구를 처리하지 않았다.
- 변경: `Views/Size128_64/Running/SokchoRunning1_64.xaml`, `SokchoRunning2_64.xaml`의 번호 표시를 `EntityNumberText` 한 개로 바꿨다. 기존 x=31·y=0·11pt, 노란색 번호와 흰색 가운데 강조색을 유지하고 별도 짧은 번호 오버레이를 제거했다. 공통 컨트롤이 번호를 4-4-1로 나눈다.
- 변경: `Infra/Api/AnimalParseData.cs`의 익산·군산 분기에서 `친자일치`, `친자 일치`, `친자확인`, `친자 확인`을 검사한다. 비고에서 가장 먼저 나온 일치 문구 하나로 `PaternityMatch`를 친자일치로 보완하고 그 문구만 제거한다. 나머지 비고는 보존한다. `IsPaternityMatch=N`이면 보완과 문구 제거를 하지 않으며 최종 공통 숨김 처리를 유지한다. 다른 축협의 비고·친자 처리에는 적용하지 않는다.
- 검증 코드·문서: 기존 `Validation/ReferenceAdoptionChecks/ItemUpdateChecks.cs`의 익산·군산 기대값을 새 규칙에 맞게 갱신했다. 자동 검사는 실행하지 않았다. `Docs/기능/경매수신_데이터갱신.md`, `Docs/기능/화면표시_페이지전환.md`, `CODEX_HANDOFF.md`에 현재 규칙과 영향 범위를 기록했다.
- 빌드: `dotnet build CowAuctionSmall.sln -c Debug --no-restore -m:1 -v:minimal` 제품·GAMSTest 성공, 경고 0·오류 0. `ReferenceAdoptionChecks.csproj`는 최초 `--no-restore`에서 CommunityToolkit.Mvvm 8.4.2 캐시 누락으로 실패했으나 `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj -c Debug -m:1 -v:minimal`에서 복원·컴파일 성공, 경고 0·오류 0이다. 검사 실행은 하지 않았다.
- 미수행·영향: 실제 WPF 전광판 번호 가독성·배치, 익산·군산 서버 데이터와 `IsPaternityMatch=N` 현장 동작, Release·게시·배포는 미확인·미수행이다. Git 기준 HEAD `75ebba7`, 브랜치 `backup/pre-cleanup-20261001`; 기존 미커밋·미추적 사용자 변경 보존. commit hash 미생성·push 미수행.


## 2026-10-12 — 속초 128×64 행사·경매 전용 화면 복원과 참고 문서 적용 여부 검토

- 목적·승인: 사용자가 속초 전용 화면과 선택·페이지 수 조건을 함께 복원하도록 요청했다. 첨부 260928 친자 뱃지/비고 문서와 260929 개체번호 문서는 적용 여부만 검토하도록 구분했으며 문서 안의 수정 지시를 실행 승인으로 확대하지 않았다. 이전본·첨부 문서·운영 XML은 수정하지 않았다.
- 문제·원인: 신규본에서 속초 128×64 XAML 6종과 사업장/행사 분기가 빠져 일반 화면이 선택됐다. 일반 최대 2페이지·행사 1페이지 제한도 없었다. 현재 공통 캐시 이름으로 복원하면 일반/속초·일반경매/행사 전환 시 이전 화면이 재사용될 수 있어 이름도 구분했다.
- 화면 복원: `Views/Size128_64/Running/SokchoRunning1_64`, `SokchoRunning2_64`, `SokchoEventRun_64`, `Views/Size128_64/SokchoSold_64`, `SokchoUnSold_64`, `SokchoEventResult_64`의 XAML/code-behind 12개를 이전본에서 복원했다. 좌표·색상·글꼴·고정 문구는 원본과 일치한다. 제거된 NumberFormatConverter는 현재 ItemDisplay 가격/중량 서식으로 대체하고 결과 화면은 Snapshot 바인딩으로 연결했다. 진행 1/2의 예약 Dispatcher 작업·비고 애니메이션은 DataContext 변경·Unloaded 때 해제하도록 연결했다.
- 선택·페이지: `Presentation/Display/AuctionViewFactory.cs`에서 속초 8808990806426·128×64·제목 행사용은 행사 진행 1페이지와 행사 결과, 일반은 진행 1/2와 전용 낙찰·유찰을 선택한다. `DisplaySelect.cs`의 로그인 후 요청 페이지 제한과 캐시 이름을 연동했다. 속초 128×128의 이전 최대 2페이지도 유지했다. 다른 사업장·해상도, 자동 버전·Named Pipe·두 달 로그 보관 정책은 보존했다.
- 검사: 신규 `Validation/DisplayTestChecks/SokchoChecks.cs`와 `Program.cs`에 선택·페이지 경계·제목 공백·다른 사업장 유지·인증 갱신·같은 패널의 일반→속초일반→행사→일반 캐시 전환·6종 실제 바인딩/가격·반복 진입/비고 해제·GAMSTest 수동/자동 점멸을 추가했다. 기존 1,037개에 90개를 더해 최종 1,127개다. 최초 추가 검사 함수명 오기 CS1061 1건과 운영 생성자 생략 객체의 pageTime 초기화 누락을 확인해 검사만 바로잡았다.
- 빌드·검증: `dotnet build Validation/DisplayTestChecks/DisplayTestChecks.csproj --artifacts-path <TEMP>/CowAuction-SokchoRestore-20261012 -m:1 -v:minimal` 및 후속 --no-restore 성공, 최종 경고 0·오류 0. `dotnet <동일 경로>/bin/DisplayTestChecks/debug/DisplayTestChecks.dll --skip-measurements` 최종 1,127개 통과. 원본 6개 XAML과 현재본의 좌표·크기·색상·글꼴·고정 Text 속성 대조도 모두 일치했다.
- Release: `dotnet build CowAuctionSmall.sln -c Release --artifacts-path <동일 경로> --no-restore -m:1 -v:minimal` 제품·GAMSTest 성공, 경고 0·오류 0. 임시 격리 출력에 필요한 최초 복원을 수행했다. 패키지·프레임워크·csproj·게시 프로필 변경 없음.
- 첨부 9/28 문서 검토: 공통 data[23] 친자 판정·노란 친자 뱃지·공통 비고 호스트와 장성 첫 키워드/색상 규칙은 존재한다. 진행/결과 호스트는 RunningNoteHost128로 통합됐다. 익산군산의 비고 문구로 친자를 보완/제거하는 예외는 현재 없다. 장성 IsPaternityMatch=N 무시 문제는 최종 공통 숨김 처리로 수정돼 있다. 문서와 완전 동일한 상태는 아니며 이번에 파서·호스트를 변경하지 않았다.
- 첨부 9/29 문서 검토: 정읍 진행 화면을 포함해 EntityNumberText의 한 TextBlock·3개 Run·4-4-1·가운데 색상 강조 방식이 공통 구현으로 적용돼 있다. EntityNumberPartConverter 대신 컨트롤 자체가 분할하고 가운데 배경 강조 속성은 현재 없다. XAML 활성 객체는 EntityNumberText 59개이며 원본 오버레이를 유지한 이번 속초 복원 화면 6개에는 EntityNumberShort 직접 바인딩이 남는다. 문서의 배경 강조/전체 화면 확대까지 완전 적용된 것은 아니며 이번 요청은 검토만 수행했다.
- 문서·영향: 화면 기능·점검 도구·검증 README·CODEX_HANDOFF에 속초 복원과 검사 범위를 기록했다. 소스 영향은 속초 화면 생성·페이지 수·비고 수명이며 제품을 참조하는 도구·검증도 빌드된다. 첨부 문서의 별도 요구는 적용하지 않았다.
- Git·추가 확인: 기준 HEAD 75ebba7, 브랜치 backup/pre-cleanup-20261001. 시작 기존 변경 95개 항목 보존, commit hash 미생성·push 미수행. 게시/배포·제품 앱 실행·속초 실서버·전광판·실제 패킷·장시간·다중 DPI·전체 제품 all 검사 미수행. 현장 일반/행사·가격·번호·비고·페이지 전환을 확인해야 한다.


## 2026-10-12 — 로그 두 달 보관·자동 삭제와 배치·뿌리농가 설정 기준 확인

- 목적·승인: 사용자가 파일별 ZIP의 장단점을 확인하고 관리 인력·시간 낭비를 막기 위해 두 달 이상 로그의 자동 삭제를 요청했다. 운영 화면 해상도·최대 및 최적 배치를 제공했고 기존 IsShowQQuri=N을 X로 바꿔야 하는지도 질문했다. 최적 배치의 오기는 사용자 답변으로 8행×40열로 확정했다.
- 문제·원인: LogArchiveService는 원본 압축 후 ZIP을 무기한 보관해 저장량이 계속 증가했다. 두 달 보관 정책으로 대체하되 파일별 압축·검증 방식은 유지한다.
- 변경 파일: `Infra/Logging/LogArchiveService.cs`에 만료 판정·독점 핸들 삭제·Deleted 집계를 추가했다. 로컬 오늘.AddMonths(-2)까지 경계일을 포함해 Logs 하위 .log·.log.zip·.log.<SHA-256 64자리>.zip만 삭제한다. 날짜 충돌은 더 최신 파일명/상위 폴더 날짜를 사용하고 날짜 없으면 마지막 수정일을 사용한다. 새 ZIP은 원본 수정일을 유지한다. 만료 원본은 압축하지 않고 삭제하며 다른 ZIP·.tmp·비로그 파일·폴더·링크 경로는 보존한다. 사용 중·읽기 전용·권한 부족은 다음 주기에 재시도한다.
- 앱·검사: `App.xaml.cs`의 기존 로그 압축 안내에 만료삭제 건수를 추가했다. `Validation/ReferenceAdoptionChecks/LogArchiveChecks.cs`에 경계일·윤년·월말·날짜 충돌·미지 ZIP 보존·잠금/읽기 전용 재시도·삭제 중 취소·압축 원본 날짜·작업자 삭제 집계를 검사했다. 기존 취소 검사는 호출 횟수 대신 잠금/ZIP 게시 시점으로 확인하도록 조정했다.
- 설정 확인: N은 제거되지 않았다. Y는 서버의 뿌리농가/우량 정보를 반영, N은 해당 활성화 경로 미사용, X는 미적용 전용 화면을 선택한다. N을 X로 일괄 치환하지 않았다. 원본 users.XML·Board.XML·뿌리농가 처리 소스는 변경하지 않았다.
- 배치 기록: `Docs/기능/화면표시_페이지전환.md`에 사용자 최대 2560×1600의 11×20, 3840×1080의 7×30, 듀얼 5120×1600의 11×40, 7680×1080의 7×60과 최적 8×40·6×60을 기록했다. 128×128 슬롯·배율 100% 가정의 계산이며 장비 실측은 아니다. GAMSTest 400슬롯 제한 때문에 최대 듀얼 440/420슬롯은 미지원이고 최적 320/360슬롯은 제한 안이다. 제한 변경은 이번에 수행하지 않았다.
- 문서·인계: `Docs/기능/설정_로그.md`의 무기한 보관을 두 달 정책으로 대체하고 N/X 차이를 추가했다. 위 화면 문서와 `CODEX_HANDOFF.md`에 기준·결과·후속 확인을 남겼다.
- 영향·빌드: 제품 App 로깅 수명에 연결되는 자동 압축/만료 정리와 통계, 검증·문서. 경매·이력·사업장 화면·운영 설정·게시 프로필·패키지·프레임워크·자동 버전은 보존했다. 참조 제품 변경으로 GAMSTest도 빌드되지만 도구가 운영 로그 정리를 시작하지는 않는다.
- 검증: `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --artifacts-path <TEMP>/CowAuction-LogRetention-20261012 -m:1 -v:minimal` 및 추가 검사 후 같은 명령 --no-restore 성공, 각각 경고 0·오류 0. `dotnet <동일 경로>/bin/ReferenceAdoptionChecks/debug/ReferenceAdoptionChecks.dll log-archive` 최초 42개, 최종 44개 통과. 검사 파일은 고유 임시 경로의 가상 로그이며 운영 Logs를 처리하지 않았다.
- Release: `dotnet build CowAuctionSmall.sln -c Release --artifacts-path <동일 경로> -m:1 -v:minimal` 제품·GAMSTest 성공, 경고 0·오류 0. 새 임시 출력·구성에 필요한 복원을 수행했다. 패키지 버전 변경 없음.
- 미수행·추가 확인: 운영 앱 실행·기존 실제 로그 삭제·게시/배포·물리 전광판·링크/정션 실환경·DPI·최대 배치 성능 검증·전체 all 검사 미수행. 새 제품 EXE를 실행할 때 두 달 삭제 정책이 시작된다. 빈 날짜 폴더는 남긴다.
- Git: 기준 HEAD `75ebba7`, 브랜치 `backup/pre-cleanup-20261001`, 시작 기존 변경 95개 항목 보존. commit hash 미생성·push 미수행.


## 2026-10-12 — GAMSTest 동작 복원 사용자 확인 완료

- 확인 근거: 사용자가 수동 테두리 점멸 제거·외부 NUMBER/RGB/STOP 복원 작업에 대해 “정상 동작 확인 완료”로 회신했다. 첨부 화면에서 외부 전광판 제어 프로그램과 GAMSTest 제어창, RED 단색 표출을 확인했다. 다른 명령과 점멸 동작은 정지 이미지 단독 증거가 아닌 사용자 확인으로 구분한다.
- 상태: 요청한 동작의 사용자 확인 완료. 실제 사용한 EXE 경로·파일 버전·원격 PC 구성·물리 전광판 여부는 미확인이다. 앞선 오프라인 검사·게시본 실행 결과는 아래 구현 기록의 당시 결과로 보존한다.
- 변경 파일·영향: `WORKLOG.md`, `CODEX_HANDOFF.md`에 사용자 확인 결과만 추가했다. 소스·설정·배포본 변경과 빌드·앱 실행은 해당 없음. 문서 내용 및 diff 공백 검사로 확인한다.
- Git: 기존 사용자 변경 보존. commit hash 미생성·push 미수행.


## 2026-10-12 — GAMSTest 수동 테두리 점멸 제거와 외부 Named Pipe 명령 복원

- 목적·승인: 사용자가 개별 진행 버튼은 테두리가 깜박이지 않아야 하며 외부 전광판 제어 프로그램의 번호·RGB·STOP이 동작하지 않는다고 알려 해당 동작 복원을 요청했다. 수동 진행·유전 점멸 제거와 이전 외부 명령 규약 복원을 수행했다.
- 문제·원인: `DisplayTestController`가 수동 선택과 자동 순환 모두 IsRunning=true로 표시했고 동일 모드 재선택은 상태 변경 없이 반환했다. 신규 도구에서 이전 `GAMSTest.DisplayCommand` Named Pipe 수신 서비스와 제어창 연결이 빠져 있었다.
- 변경 파일·내용: `Tools/GAMSTest/Services/DisplayTestController.cs`에 자동 순환 여부를 전달해 수동 진행·유전은 점멸 없이 표시하고 자동 순환에서만 활성화한다. 같은 페이지를 수동 재선택해도 점멸을 종료하며 기존 화면 캐시·공통 비고·스크롤 수명은 유지한다.
- 외부 명령: 신규 `Tools/GAMSTest/Services/NamedPipeCommandServer.cs`에 이전 Pipe 이름·바이트 스트림·한 연결당 텍스트 한 줄 규약과 연결 재수신을 복원했다. `ViewModels/ControlWindowViewModel.cs`는 NUMBER·RED·GREEN·BLUE를 기존 Command로 전달해 밝기 및 색상 기억을 유지한다. `Views/ControlWindow.xaml.cs`는 Dispatcher에서 처리하고 STOP은 Close()로 제어창·표출창·타이머·캐시·Pipe를 정리한다. 종료 뒤 예약된 명령은 무시하고 출하 AMS 실행은 호출하지 않는다. 취소 토큰은 수신 작업 완료 뒤 해제한다.
- 검증 파일: `Validation/DisplayTestChecks/ControllerChecks.cs`에 네 해상도의 수동/자동 점멸과 같은 페이지 재선택·비고 유지 검사를 추가했다. 신규 `NamedPipeChecks.cs`는 고유한 검사 Pipe로 공백·대소문자·빈/미지 명령·연결 끊김·재연결·밝기·번호·STOP·수신 취소를 검사한다. `Program.cs`에서 실행하고 `README.md`에 검사 범위를 기록했다. 운영 Pipe나 실제 외부 제어 프로그램에 검사 명령을 보내지 않는다.
- 문서·인계: `Docs/기능/전광판_점검도구.md`에 수동/자동 차이와 외부 명령 규약·재게시 필요·현장 확인 범위를 추가하고 `CODEX_HANDOFF.md`를 갱신했다.
- 영향·빌드: 도구의 화면 상태와 외부 명령·종료 수명 및 검증·문서. 제품 ProjectReference로 제품·도구·검사 프로젝트가 함께 빌드된다. 제품 업무 로직·운영 XML·게시 프로필·자동 버전·낙찰 화면 수정은 보존했다. 패키지·프레임워크 변경은 없다.
- 빌드·검사: `dotnet build Validation/DisplayTestChecks/DisplayTestChecks.csproj --artifacts-path <TEMP>/CowAuction-Gams-Pipe-Manual-20261012 -m:1 -v:minimal` 성공, 경고 0·오류 0. `dotnet <동일 경로>/bin/DisplayTestChecks/debug/DisplayTestChecks.dll --skip-measurements` 1,037개 통과. 새 임시 출력에 필요한 최초 복원을 수행했다. 성능 측정·전체 제품 all 검사는 미수행.
- 게시: `dotnet publish Tools/GAMSTest/GAMSTest.csproj -c Release -p:PublishProfile=SingleFile-win-x64 --artifacts-path <동일 경로> -o <동일 경로>/publish-tool -m:1 -v:minimal` 성공. RID·자체 포함 게시 자산을 복원했다. 출력은 GAMSTest.exe 한 개, 147,571,583바이트, 파일·제품 버전 `1.1.9781.25800` 일치. 기존 배포본은 교체하지 않았다.
- 실제 EXE 검사: 기존 GAMSTest 프로세스·기본 Pipe가 없는 것을 확인한 후 위 임시 게시본만 실행했다. 기본 Pipe로 NUMBER·RED·GREEN·BLUE를 전달한 뒤 프로세스 유지, STOP 뒤 5초 이내 종료 코드 0을 확인했다. 색상·배치·두 창 종료는 앞선 WPF 검사로 별도 확인했다. 외부 전광판 제어 프로그램 자체, 원격 PC, 운영 서버, 물리 전광판 및 현장 실행 경로 교체는 미수행이다.
- Git: 기준 HEAD `75ebba7`, 브랜치 `backup/pre-cleanup-20261001`. 기존 미커밋·미추적 95개 항목을 보존했다. commit hash 미생성·push 미수행. 현장에서는 외부 제어 프로그램이 수정된 GAMSTest.exe를 실행하도록 경로·배포본을 확인해야 한다.


## 2026-10-12 — 횡성 낙찰 화면 적용 범위 제한과 일반 낙찰 분기 복원

- 목적·승인: 사용자가 HoengseongSold는 횡성에만 적용해야 한다고 확인한 뒤, 횡성 일반 분기 추가·일반 화면 기본 분기 복원의 최소 수정안을 승인했다.
- 문제·원인: `SetCustomDisplay.CustomAuctionSold_128`의 기본 분기 세 곳이 모두 HoengseongSold를 반환해 전용 분기가 없는 다른 축협까지 횡성 화면으로 표시됐다. 제품과 GAMSTest가 같은 화면 생성 경로를 공유하므로 둘 다 영향을 받았다.
- 변경 파일·내용: `Presentation/Display/SetCustomDisplay.cs`에 횡성 일반 경매 HoengseongSold 분기를 명시적으로 추가했다. 횡성 행사용 HoengseongBunyangSold는 유지하고, 기본 분기를 뿌리농가 표시 X의 Standard_non_X_Sold·그 외 비육우/번식우 QQuriSold_Weight·나머지 QQuriSold로 복원했다. 장성·양평·예천·해남진도·화순 전용 화면과 염소·말 우선 처리는 유지했다.
- 검증 파일: `Validation/ReferenceAdoptionChecks/BidderChecks.cs`에 횡성 X/Y/N × 축종 1/2/3 × 일반/행사용, 일반 및 과거 전용 분기가 제거된 사업장 7종의 동일 조합, 선택기와 공통 AuctionViewFactory의 결과, 기존 전용 5축협 우선 처리, 횡성/일반 사업장의 염소·말을 확인하는 153개 검사를 추가했다. 기존 낙찰자 표시·스크롤 검사를 보존했다.
- 문서·인계: `Docs/기능/화면표시_페이지전환.md`에 현재 선택 기준과 복원 범위를 기록하고 `CODEX_HANDOFF.md`에 승인·결과·남은 확인을 추가했다. 과거에 제거된 모든 사업장 전용 분기를 복원한 작업은 아니다.
- 영향 범위·빌드: 128×128 낙찰 화면 선택과 이를 공유하는 출하 AMS 테스트. 파서·성별·이력·유찰·다른 해상도·운영 XML·게시 프로필·자동 버전 설정은 변경하지 않았다. 테스트 도구는 제품 ProjectReference로 함께 빌드된다.
- 빌드: `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --artifacts-path <TEMP>/CowAuction-Hoengseong-Sold-20261012 -m:1 -v:minimal`, `dotnet build CowAuctionSmall.sln -c Debug --artifacts-path <동일 경로> -m:1 -v:minimal` 모두 성공, 각각 경고 0·오류 0. 새 임시 출력에 필요한 최초 복원을 수행했다.
- 테스트: `dotnet <동일 경로>/bin/ReferenceAdoptionChecks/debug/ReferenceAdoptionChecks.dll bidder` 434개 오프라인 검사 통과. 실제 WPF 화면을 가짜 데이터로 검사하며 제품 App·로그인·운영 서버를 실행하지 않았다.
- Git: 기준 HEAD `75ebba7`, 브랜치 `backup/pre-cleanup-20261001`, 시작 기존 변경 95개 항목 보존. 화면 선택기의 기존 HasRunning3 변경과 검사 파일의 Infra 참조 변경도 유지했다. commit hash 미생성·push 미수행.
- 추가 확인·미수행: Release·게시·현장 EXE 교체·물리 전광판·운영 통신·전체 all 검사 미수행. 현장에서는 횡성 일반/행사용 및 다른 축협의 낙찰 화면·가격 제목·낙찰자 표시를 확인해야 한다.


## 2026-10-12 — 출하 AMS·출하 AMS 테스트 자동 버전 복원 및 유지 규칙 기록

- 목적·승인: 사용자가 신규 수령본 `CowAuctionSmall`의 두 프로젝트에서 제거된 자동 버전을 복원하고, 고정 버전으로 돌아가지 않도록 이유와 기록을 보관하도록 요청했다. 비교 대상이었던 다른 두 폴더는 수정하지 않았다.
- 문제·원인: 제품은 고정 Version=1.0.1.30만 남고 GAMSTest는 자동 버전 속성이 없었다. 다른 수정본이 같은 버전으로 표시되면 현장 EXE 식별·장애 추적·배포 교체 확인이 어려워진다. 제거한 작성자나 경위는 미확인이다.
- 변경 파일·내용: `CowAuctionSmall.csproj`, `Tools/GAMSTest/GAMSTest.csproj`에 BuildDate·BuildTime과 Version·AssemblyVersion·FileVersion·InformationalVersion을 `1.1.<2000-01-01 이후 일수>.<당일 경과 초/2 정수 변환값>`으로 설정했다. IncludeSourceRevisionInInformationalVersion=false로 제품 버전의 Git 해시 접미사를 제외했다.
- 재발 방지 기록: `AGENTS.md`에 사용자 명시적 요청 없이 고정 버전으로 변경하거나 자동 속성을 제거하지 않는 규칙과 소스 수령·통합·게시 설정 변경 시 양쪽 프로젝트를 확인하는 기준을 추가했다. `Docs/기능/설정_로그.md`, `Docs/기능/전광판_점검도구.md`에 유지 이유·계산 방식·한계를 기록했고 `CODEX_HANDOFF.md`를 생성해 다음 작업에 전달한다. 문서 규칙이며 외부 수정이나 임의 덮어쓰기를 기술적으로 차단하는 기능은 아니다.
- 영향 범위: 두 앱의 빌드·게시 메타데이터와 버전 표시. 제품 ProjectReference로 도구 빌드 시 제품도 함께 빌드된다. C#/WPF/MVVM·경매 동작·사업장 화면·패키지·프레임워크·운영 XML·게시 프로필의 기존 변경은 보존했다.
- 빌드: `dotnet build CowAuctionSmall.sln -c Debug --artifacts-path <TEMP>/CowAuction-New-AutoVersion-20261012 -m:1 -v:minimal`, 동일 경로의 `-c Release --no-restore` 모두 성공, 각각 경고 0·오류 0. 새 임시 경로에 필요한 최초 복원을 수행했다.
- 게시: 제품 `dotnet publish CowAuctionSmall.csproj -c Release -p:PublishProfile=FolderProfile --artifacts-path <동일 경로> -o <동일 경로>/publish-product -m:1 -v:minimal`, 도구 `dotnet publish Tools/GAMSTest/GAMSTest.csproj -c Release -p:PublishProfile=SingleFile-win-x64 --artifacts-path <동일 경로> -o <동일 경로>/publish-tool -m:1 -v:minimal` 성공. RID·자체 포함 게시에 필요한 복원을 수행했으며 기존 배포 위치를 덮어쓰지 않았다.
- 버전 검증: EXE·DLL의 FileVersion·ProductVersion과 DLL의 AssemblyVersion을 비교했다. Debug는 제품 `1.1.9781.23879`, 도구 `1.1.9781.23884`, Release는 제품 `1.1.9781.23898`, 도구 `1.1.9781.23901`로 각각 일치했다. 게시 직후 제품 `1.1.9781.23914`, 도구 `1.1.9781.23918`도 각각 일치했다. 도구 게시 폴더는 GAMSTest.exe 한 개, 147,567,487바이트다. 두 프로젝트 자동 속성 4개와 관련 diff 공백 검사를 확인했다.
- 한계·추가 확인: 시간값은 약 2초 단위이며 같은 구간의 빌드는 같은 버전일 수 있다. PC 시계가 되돌아가면 버전도 되돌아가고 빌드 생략 시 기존 버전을 유지한다. 두 앱 평가 시각이 다르면 서로 다른 숫자가 정상이다. 운영 서버·전광판·앱 실행·Visual Studio 게시 UI·현장 배포는 미수행. 업무 동작 변경이 없어 경매 기능 검사는 반복하지 않았다.
- Git: 기준 HEAD `75ebba7`, 브랜치 `backup/pre-cleanup-20261001`. 시작 시 기존 미커밋·미추적 93개 항목을 보존했다. commit hash 미생성·push 미수행.


## 2026-10-06 — 경매품 축협별 파싱 보정 통합

- 목적·승인: 사용자가 `AnimalParseData` 곳곳에 섞인 축협별 처리를 `return gv` 직전의 switch 기반 함수로 모으도록 요청했다.
- 문제·원인: 횡성 성별, 익산·군산 지역, 상주 지역, 영천·보령 임신 개월, 장성 비고 처리가 공통 필드 파싱 사이와 메서드 하단에 나뉘어 있어 적용 순서와 조합별 범위를 파악하기 어려웠다. 익산·군산에는 공통 `data[23]` 친자 처리와 별도로 비고 문구를 다시 해석하는 과거 예외도 남아 있었다.
- 변경 파일: `Infra/Api/AnimalParseData.cs`에 `ApplyAuctionHouseOverrides`를 추가하고 공통 파싱 완료 후 한 번 호출하도록 정리했다. 익산·군산 case는 수신 지역 원문의 관내·관외 판정만 담당한다. `ChangeSexName`의 공통 변환과 횡성 SV에서 월령이 없을 때 성별 갱신을 제외하는 필드 보호는 유지했다. `IsPaternityMatch=N`은 축협별 보정 뒤 최종 적용하여 장성 예외도 동일하게 숨긴다.
- 동작·영향: 익산·군산의 관내 판정은 잘린 표시 문자열이 아닌 수신 지역 원문에서 `익산`·`군산` 포함 여부를 확인한다. 친자 결과는 모든 축협이 `data[23]`과 `users.XML`의 공통 설정을 사용하며 익산·군산 비고 원문을 임의로 제거하지 않는다. 통신·저장·화면 선택에는 변경이 없다.
- 검증 파일: `Validation/ReferenceAdoptionChecks/ItemUpdateChecks.cs`에 익산·군산 지역과 공통 친자 처리, 상주 지역, 영천·보령 임신 값, 횡성 월령별 성별 및 SV 보호, 장성 앞글자 추출과 공통 숨김 설정 검사를 추가했다.
- 빌드·테스트: 제품 Debug 빌드와 `ReferenceAdoptionChecks` Debug 빌드 모두 경고 0·오류 0. `ReferenceAdoptionChecks.dll updates` 62개 통과. 최초 빌드에서 공통 친자 표시 변수 누락 1건을 확인해 정리한 뒤 재검증했다.
- 문서: `Docs/기능/경매수신_데이터갱신.md`에 공통 파싱 후 축협별 최종 보정 구조와 담당 규칙을 반영했다.
- Git: 기준 HEAD `75ebba7`, 브랜치 `backup/pre-cleanup-20261001`. 기존 사용자 변경과 Infra 이관 상태를 보존했다. commit 미생성·push 미수행.
- 미수행·추가 확인: 운영 서버·실제 전광판·운영 앱 실행·Release 빌드·배포는 미수행이다.


## 2026-10-02 - 속초 128×64 진행 화면 애니메이션 브러시 형식 보정

### 작업 일자

- 2026-10-02

### 작업 목적

- GAMSTest에서 128×64 뷰 페이지 선택 시 진행 1페이지의 UI 애니메이션 예외를 방지한다.

### 원인

- 속초 128×64 진행 1페이지의 `ObjectAnimationUsingKeyFrames`에서 `DiscreteObjectKeyFrame.Value`를 문자열(`Black`, `Red`)로 지정하고 있었다. 해당 값은 개체형으로 처리되어 `Border.BorderBrush`가 요구하는 `Brush` 형식과 맞지 않을 수 있다.
- 같은 크기의 공통 진행 1페이지는 키 프레임 값에 `SolidColorBrush`를 명시해 형식을 맞추고 있었다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_64/Running/SokchoRunning1_64.xaml`: 두 키 프레임 값을 각각 검정·빨강 `SolidColorBrush`로 지정했다. 애니메이션 방식과 0초/0.6초 전환 시점은 유지했다.
- `WORKLOG.md`: 오류 분석과 변경 및 검증 결과를 기록했다.

### 영향 범위

- 속초 128×64 진행 1페이지의 테두리 깜박임 애니메이션 값 형식만 변경했다. GAMSTest 로직, 공통 128×64, 128×128·160×64·320×64 화면은 수정하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall\CowAuctionSmall.csproj --configuration Debug --no-restore --nologo`: 성공, 오류 0개 / 경고 102개.
- `dotnet build .\GAMSTest\GAMSTest.csproj --configuration Debug --no-restore --nologo`: 성공, 오류 0개 / 경고 1개.
- 속초 XAML XML 파싱: 정상.
- 128×64 뷰 페이지 실제 순환 및 애니메이션 확인: 정상(사용자 확인, 첨부 화면).

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 속초 128x64 진행 애니메이션 브러시 형식 보정`


## 2026-10-02 - GAMSTest Board.XML 해상도 적용

### 작업 일자

- 2026-10-02

### 작업 목적

- GAMSTest가 `Board.XML`의 `<Size>`에 설정된 전광판 해상도로 계류대 배열과 테스트 화면을 표시하도록 한다.

### 원인

- `DisplayWindow`가 `BoardXmlParser`를 통해 `BoardList`를 `DisplayTestController.Attach()`에 넘기고 파서도 `<Size>`를 읽지만, 컨트롤러가 해당 값을 무시하고 모든 보드 칸과 뷰를 128×128로 고정하고 있었다.
- 계류대 번호 화면도 XAML에서 128×128 크기로 고정되어 있었다.

### 변경 파일 및 내용

- `GAMSTest/Services/DisplayTestController.cs`: 파싱된 크기에 따라 128×128, 128×64, 160×64, 320×64 보드 칸과 행 높이를 설정하고, 해당 크기의 진행·낙찰·유찰 뷰를 선택하도록 했다. 속초 128×64는 일반·행사 전용 뷰 선택을 유지한다. 별도 노트 오버레이는 128×128에서만 사용해 64픽셀 높이 화면을 가리지 않게 했다.
- `GAMSTest/Views/BoardNumberView.xaml`, `BoardNumberView.xaml.cs`: 계류대 번호 화면이 실제 보드 칸 크기에 맞춰 늘어나고, 64픽셀 화면에서는 제목과 번호 글자 크기를 줄이도록 했다.
- `WORKLOG.md`: 분석, 변경 범위와 검증 결과를 기록했다.

### 영향 범위

- 변경은 GAMSTest 화면의 크기 및 뷰 선택에만 적용된다. CowAuctionSmall 운영 프로그램, XML 설정 파일, 프로젝트 파일은 수정하지 않았다.
- 지원되는 `<Size>` 값은 `128,128`, `128,64`, `160,64`, `320,64`이며 알 수 없는 값이나 누락값은 기존 128×128 동작을 유지한다.

### 빌드 및 테스트 결과

- `dotnet build .\GAMSTest\GAMSTest.csproj --configuration Debug --no-restore --nologo`: 성공, 오류 0개 / 기존 경고 1개 (`ControlWindowViewModel.ActionCommand.CanExecuteChanged` 미사용).
- `git diff --check`: 공백 오류 없음.
- 네 가지 해상도에 따른 보드 칸·뷰 분기 정적 확인: 정상.
- WPF 창의 실제 전광판 크기별 육안 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: GAMSTest에 Board XML 해상도 적용`


## 2026-10-02 - 속초 128×64 화면 Aliased 렌더링 적용

### 작업 일자

- 2026-10-02

### 작업 목적

- 속초 전용 128×64 화면의 글자 렌더링을 기존 공통 128×64 화면과 동일하게 맞춘다.

### 원인

- 속초 전용 XAML은 `TextOptions.TextFormattingMode="Display"`만 설정되어 렌더링 모드가 WPF 기본값 `Auto`에 맡겨져 있었다.
- 기존 공통 128×64 화면은 `TextOptions.TextRenderingMode="Aliased"`를 명시하고 있다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_64/Running/SokchoRunning1_64.xaml`, `SokchoRunning2_64.xaml`, `SokchoEventRun_64.xaml`: 진행 화면에서 Aliased 렌더링을 명시했다.
- `CowAuctionSmall/Views/Size128_64/SokchoSold_64.xaml`, `SokchoUnSold_64.xaml`, `SokchoEventResult_64.xaml`: 결과 화면에서 Aliased 렌더링을 명시했다.
- `WORKLOG.md`: 변경 및 검증 결과를 기록했다.

### 영향 범위

- 속초 전용 128×64 여섯 화면의 글자 래스터화 방식만 변경된다. 글꼴, 글자 크기, 배치, 바인딩과 화면 선택 조건은 변경하지 않았다.
- 일반 진행 1·2페이지의 실제 전광판 확인은 다음 속초 경매일 예정이며, 이번 작업에서는 미수행이다.

### 빌드 및 테스트 결과

- XAML XML 구문 확인: 여섯 파일 모두 정상 파싱.
- `dotnet build .\CowAuctionSmall\CowAuctionSmall.csproj --configuration Debug --no-restore --nologo`: 성공, 오류 0개 / 경고 102개.
- 전광판 육안 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 속초 128x64 화면 Aliased 렌더링 적용`


## 2026-10-02 - 속초양양축협 128×64 일반·한우경진 전용 화면 통합

### 작업 일자

- 2026-10-02

### 작업 목적

- 구형 속초양양축협 출하 AMS의 128×64 일반 경매 및 연 1회 한우경진 화면을 신형 WPF 프로그램에 속초 전용 View로 통합한다.
- 전광판 크기는 기존대로 `Board.XML`의 `<Size>` 값을 사용하고, 속초 `Board.XML`이 `128,128`이면 기존 128×128 공통 View를 표시한다.

### 원인

- 기존 공통 128×64 진행 2페이지는 `RegistrationCategory`(본인 혈통)를 표시하지만 속초 운영 화면은 `MotherLevel`(어미 혈통)을 표시한다.
- 신형 프로그램에 속초 한우경진 128×64 전용 화면과 속초 사업장 선택 분기가 없었다.
- 128×128에서 페이지 수가 3~4로 설정되면 기존 화면 생성 조건상 128×64 경로로 내려갈 수 있어 속초에 한해 최대 2페이지 제한이 필요했다.

### 검토한 운영 설정

- 속초양양축협 사업장 코드: `8808990806426`.
- 일반 `Board.XML`: 개별 보드 `128,64`, 20열×7행, 진행 2페이지와 낙찰·유찰 화면 구성.
- `한우경진Board.XML`: 개별 보드 `128,64`, 동일한 20열×7행 배열, 진행·낙찰·유찰 상태에서 동일한 개체정보 양식 사용.
- 필드 의미: `SpaceIndex` 계류대번호, `SipNumber` 출품번호, `MotherLevel` 어미 혈통, `RegistrationCategory` 본인 혈통, `CalvingNumber` 어미산차, `Pregnant` 임신개월수, `EntityNumber` 축산개체관리번호, `BirthMonth` 개월령, `Birth` 생년월일.
- 한우경진 여부는 횡성과 동일하게 `users.XML`의 `<LowestPriceTitle>행사용</LowestPriceTitle>` 값으로 판단한다.

### 변경 파일 및 내용

- `CowAuctionSmall/Services/DisplaySelect.cs`: 속초 사업장 코드와 `128×64` 크기를 함께 검사하여 일반·한우경진 전용 View를 선택하도록 했다. 속초 128×64 일반은 최대 2페이지, 행사는 1페이지로 제한했다. 속초 `128×128`은 최대 2페이지로 제한하고 기존 128×128 공통 진행·낙찰·유찰 선택 로직을 그대로 사용하도록 했다.
- `CowAuctionSmall/Views/Size128_64/Running/SokchoRunning1_64.xaml`, `.xaml.cs`: 속초 일반 진행 1페이지의 개체번호, 성별, 지역, 출하주, 최저가, 중량, 생년월일, 비고 화면과 비고 흐름 표시를 추가했다.
- `CowAuctionSmall/Views/Size128_64/Running/SokchoRunning2_64.xaml`, `.xaml.cs`: 속초 일반 진행 2페이지의 KPN, 어미산차, 계대, 어미 혈통, 임신개월수, 비고 화면과 비고 흐름 표시를 추가했다.
- `CowAuctionSmall/Views/Size128_64/SokchoSold_64.xaml`, `.xaml.cs`: 속초 전용 낙찰 화면을 추가했다.
- `CowAuctionSmall/Views/Size128_64/SokchoUnSold_64.xaml`, `.xaml.cs`: 속초 전용 유찰 화면을 추가했다.
- `CowAuctionSmall/Views/Size128_64/Running/SokchoEventRun_64.xaml`, `.xaml.cs`: 한우경진 진행 화면을 추가했다.
- `CowAuctionSmall/Views/Size128_64/SokchoEventResult_64.xaml`, `.xaml.cs`: 한우경진 낙찰·유찰 공통 결과 화면을 추가했다.
- `WORKLOG.md`: 속초 설정 검토, 구현 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 별도 후속 인수인계 항목 없이 본 작업 기록으로 충분하여 갱신하지 않았다.

### 영향 범위

- 속초 사업장 코드 `8808990806426`과 `Board.XML` 크기 `128,64` 조합에만 전용 화면이 적용된다.
- 속초 `128,128`은 전용 64 View를 사용하지 않고 기존 128×128 공통 View를 사용한다.
- 강진완도·하동·무진장 분기와 기존 공통 128×64/128×128/160×64/320×64 View는 수정하지 않았다.
- 통신, 데이터 파싱, `Board.XML`/`users.XML`, 프로젝트 파일은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall\CowAuctionSmall.csproj --configuration Debug --no-restore --nologo`: 성공, 오류 0개 / 기존 경고 102개.
- 신규 속초 XAML 6개 XML 파싱 확인: 정상.
- 속초 진행 2페이지가 `MotherLevel`을 사용하고 `RegistrationCategory`를 사용하지 않는지 정적 확인: 정상.
- 속초 전용 분기가 `Size128x64` 조건을 포함하고, `Size128x128`은 기존 공통 분기로 유지되는지 정적 확인: 정상.
- 일반 낙찰·유찰 화면의 실제 전광판 표시 확인: 정상(사용자 확인). 낙찰 화면은 128×64 패널별 낙찰 정보와 빈 계류대 로고가 함께 정상 배치되는 것을 첨부 화면으로 확인했다.
- 한우경진 화면의 실제 전광판 표시 확인: 정상(사용자 확인). 지역·출하주, 개체번호, 생년월일, 성별, KPN, 혈통번호와 빈 계류대 로고가 128×64 배열에 정상 배치되는 것을 첨부 화면으로 확인했다.
- 일반 진행 1·2페이지와 한우경진 진행·낙찰·유찰 상태 전환별 동일 양식 유지 확인: 미수행.
- 속초 `Board.XML`을 `128,128`로 변경했을 때 기존 128×128 공통 View 표시 확인: 정상(사용자 확인). 진행·낙찰·유찰 화면과 빈 계류대 로고가 128×128 패널 배열에 정상 배치되는 것을 첨부 화면으로 확인했다.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `feat: 속초양양 128x64 일반·한우경진 화면 추가`


## 2026-10-02 - 128×128 진행 화면 비고 하단 위치 보정

### 작업 일자

- 2026-10-02

### 작업 목적

- 횡성축협 128×128 경매 진행 화면의 비고를 가능한 아래쪽에 표시하면서 경매 진행 테두리 깜박임과 겹치지 않도록 한다.

### 원인

- 진행용 공통 비고 영역이 `Canvas.Top="110"`, `Height="18"`로 화면 하단 `Y=128`까지 확장되어 2px 외곽 테두리 영역과 겹칠 수 있었다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/RunningNoteHost128_Running.xaml`: 비고 영역의 시작 위치를 `Y=110`에서 `Y=111`로 1px 내리고, 높이를 18px에서 15px로 줄여 하단 경계를 `Y=126`으로 제한했다.
- `WORKLOG.md`: 비고 위치 보정 내용과 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 구조나 후속 인수인계 사항이 없어 갱신하지 않았다.

### 영향 범위

- 횡성축협을 포함하여 진행용 공통 호스트를 사용하는 128×128 소 경매 진행 화면의 비고 위치에 적용된다.
- 비고의 좌우 영역, 글꼴, 색상, 데이터 바인딩과 흐름 애니메이션 속도는 변경하지 않았다.
- 친자 뱃지, 낙찰·유찰 화면, 화면 선택 및 데이터 파싱 로직은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore --nologo` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 비고 영역이 `X=2~126`, `Y=111~126` 범위에 배치되어 2px 외곽 테두리 내부에 위치하는지 정적 확인: 정상.
- 실제 횡성축협 전광판의 진행 테두리 깜박임과 비고 간격 확인: 미수행.
- 긴 비고 흐름 표시, 친자 뱃지 동시 표시 및 다른 128×128 소 경매 진행 화면 회귀 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 128x128 진행 화면 비고 하단 위치 보정`


## 2026-10-01 — GAMSTest 런타임 포함 단일 EXE 게시

- 목적·승인: 사용자가 GAMSTest를 단일 EXE로 바꾸도록 승인했다. Windows x64용 런타임 포함 배포와 기본 가상 설정 내장을 구현했다.
- 변경 파일: `Tools/GAMSTest/Properties/PublishProfiles/SingleFile-win-x64.pubxml`에 자체 포함·단일 파일·네이티브 라이브러리 추출·트리밍 비활성·심볼 제외와 별도 게시 경로를 지정했다. `GAMSTest.csproj`는 Examples XML을 EmbeddedResource로 포함하고 외부 복사를 중지한다. `Services/TestSettingsLoader.cs`는 외부 설정이 없을 때 내장 XML을 읽는다. `.gitignore`는 이 자격 증명 없는 파일 시스템 게시 프로필 하나만 공유 가능하도록 예외를 추가했다.
- 호환성 조정: 최초 게시 NETSDK1150은 제품 EXE 프로젝트를 화면 DLL로 재사용하는 특성 때문에 발생했다. 이 프로필에만 ValidateExecutableReferencesMatchSelfContained=false를 적용했다. 제품의 SelfContained·게시 설정은 변경하지 않았다. 최초 결과에 참조 프로젝트 PDB가 남아 게시 목록에서 PDB를 제외하고 새 출력 폴더로 검증했다.
- 검증 변경: `Validation/DisplayTestChecks/ReferenceUiChecks.cs`에 외부 XML 없이 내장 8개 배치·표시 옵션·색상 로드와 외부 파일 미생성을 확인하는 검사를 반영했다.
- 빌드·테스트: `dotnet build Validation/DisplayTestChecks/DisplayTestChecks.csproj --artifacts-path <TEMP>/CowAuction-GAMSTest-SingleFile-Checks-20261001 -m:1 -v:minimal` 경고 0·오류 0. 해당 출력의 `DisplayTestChecks.dll --skip-measurements` 1,014개 통과.
- 게시: `dotnet publish Tools/GAMSTest/GAMSTest.csproj -c Release -p:PublishProfile=SingleFile-win-x64 --no-restore --artifacts-path <TEMP>/CowAuction-GAMSTest-SingleFile-20261001 -o <동일 경로>/single-exe -m:1 -v:minimal` 최종 성공. 최초 RID 변경에 필요한 복원을 수행했다. 새 패키지·SDK 설치·제품 배포본 교체는 없다.
- 결과물: 게시 폴더 전체 파일이 GAMSTest.exe 하나, 147,565,391바이트. 외부 XML·DLL·PDB·운영 Config·NLog.config 없음. includedFrameworks에 .NET/Windows Desktop 10.0.12 포함을 확인했다.
- 실제 실행: EXE만 있는 폴더에서 실행하여 기본 128×128 8개 배치(512×256), 진행·유전·낙찰·유찰·색상·번호 전환을 확인했다. 파일 선택창으로 가상 외부 XML을 열어 160×64 4개 배치(320×128)를 적용하고 선택 상자로 128×64(256×128)로 변경한 뒤 전환과 정상 종료를 확인했다. WPF 네이티브 DLL은 단일 파일 임시 추출 경로에서 로드됐다. 자동화의 최초 ComboBox 텍스트 요소 선택은 패턴 미지원이어서 실제 ListItem 선택으로 검증했다.
- 문서: 루트 README·기능 문서·검증 README·완료 계획 16절·Docs 목차에 단일 EXE 게시 명령·출력·설정 탐색·검증 범위를 기록했다.
- 보호·Git: 시작 상태와 기존 csproj diff, 도구·검증 기준 스냅샷을 확인했다. 기존 제품 변경·로그 압축·Infra 이관·사용자 XML은 보존했다. 기준 HEAD 75ebba7, 브랜치 backup/pre-cleanup-20261001. commit 미생성·push 미수행.
- 미수행·추가 확인: 출하 AMS 버튼의 운영 앱 실행·운영 서버·실제 전광판·.NET 미설치 별도 PC·운영 배포 미수행. 일반 빌드 폴더는 기존 개발 출력이며 현장 배포에는 게시 폴더의 EXE를 사용한다.


## 2026-10-01 — 오늘 이외 로그 자동 ZIP 압축

- 목적·승인: 사용자가 오늘 날짜의 .log 이외 파일을 자동 압축하도록 요청했다. 저장 위치는 현재 NLog가 사용하는 실행 파일 폴더의 Logs 하위로 제한한다.
- 문제·원인: 기존 비동기 파일 기록은 있으나 날짜별 로그 보관량을 줄이는 작업은 없었다. 단순 오래된 폴더 삭제 대신 검증된 압축본을 계속 보존한다.
- 변경 파일: `Infra/Logging/LogArchiveService.cs`에 날짜 판정·순차 압축·원본 독점 열기·길이/이름/SHA-256 검증·원본 핸들 정리·ZIP 충돌 보존·재시도·취소·작업자 수명을 구현했다. `App.xaml.cs`는 로깅 초기화 후 시작하고 종료 시 취소와 최대 1초 대기를 연결한다. NLog.config·운영 XML·기존 로그 큐 정책·NuGet 패키지는 변경하지 않았다.
- 동작·보존: 시작 시 및 1시간마다 낮은 우선순위 전용 스레드 하나가 처리한다. 오늘 파일명/날짜 폴더는 유지하고 날짜 없는 .log는 마지막 수정일 기준으로 판단한다. 오늘 이외 날짜 파일은 같은 폴더의 `.log.zip`으로 보관하며 ZIP 보존 기간은 무제한이다. 검증 완료 전 원본을 정리하지 않는다. 링크/정션은 건너뛰고 개별 원본은 같은 파일 핸들로만 정리한다.
- 실패·진단: 사용 중·권한·압축/검증 실패는 원본을 유지해 재시도한다. 기존 ZIP은 덮어쓰지 않으며 다르면 해시 접미사 ZIP을 사용한다. 취소와 게시 후 원본 정리 전 중단에서 재개할 수 있다. 진단은 완료·유지·재시도 건수만 기록하고 로그 본문·개인정보·파일 경로를 추가 기록하지 않는다.
- 검증 파일: `Validation/ReferenceAdoptionChecks/LogArchiveChecks.cs` 추가, Program의 `log-archive` 단독 및 `logging`/`all` 연결. 오늘 보호·내용 동일성·충돌·잠금·권한·취소·재시도·날짜 변경·작업자 수명을 가상 로그로 검사한다.
- 빌드: `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --artifacts-path <TEMP>/CowAuction-LogArchive-20261001 -m:1 -v:minimal` 및 같은 경로 `-c Release --no-restore` 성공. 최종 Debug·Release 모두 경고 0·오류 0. 최초 필요한 복원 후 --no-restore로 검증했다.
- 테스트: Debug `ReferenceAdoptionChecks.dll logging` 88개 통과(기존 59 + 압축 29). 최종 작업자 자원 정리 후 Debug·Release `log-archive` 각각 29개 통과. 실제 운영 파일을 건드리지 않고 고유 임시 테스트 폴더만 생성·정리했다.
- 문서: `Docs/기능/설정_로그.md`와 검증 README에 경로·주기·날짜 기준·검증·원본 정리·ZIP 보존·한계를 기록했다. 기존 작업 기준 스냅샷과 App/Program 차이를 대조하고 인코딩을 유지했다.
- 영향·Git: 기능은 제품 앱 로깅 수명에만 연결한다. GAMSTest는 제품 App을 실행하지 않아 자동 압축을 시작하지 않는다. 기존 Infra 이관·GAMSTest·사용자 XML 변경은 보존했다. 기준 HEAD 75ebba7, 브랜치 backup/pre-cleanup-20261001. commit 미생성·push 미수행.
- 미수행·추가 확인: 운영 앱 실행·기존 운영 로그 즉시 압축·운영 배포·실제 디스크 장애·프로세스 강제 종료·링크/정션 실환경 검증 미수행. 실제 적용은 새 빌드로 앱을 실행할 때 시작된다. 강제 종료 시 남을 수 있는 임시 ZIP은 원본 삭제 근거로 사용하지 않는다.


## 2026-10-01 — GAMSTest 보드 크기 선택 상자 복원

- 목적·승인: 사용자가 기존 UI가 선택 상자였다고 정정했다. 직전 읽기 전용 문구 추가는 사용자 의도를 잘못 해석한 변경이므로 직접 크기를 선택하는 ComboBox로 복원했다.
- 변경 파일: `Tools/GAMSTest/Views/ControlWindow.xaml`에 네 크기 선택 상자와 양방향 바인딩을 복원했다. `ViewModels/ControlWindowViewModel.cs`는 XML 가져오기와 수동 크기 변경의 적용 경로를 공유하고 SelectedResolution을 통지한다. 읽기 전용 문구 속성은 제거했다.
- 동작: XML 열기 시 Size 자동 선택, 수동 선택 시 배치·사업장·표시 설정을 유지한 채 번호 화면 재구성, XML 재열기 시 파일의 Size 복원. 원본 XML 쓰기·설정 영구 저장은 하지 않는다. 아래 출하 AMS·파일 열기 버튼 배치는 유지했다.
- 검증 파일: `Validation/DisplayTestChecks/ReferenceUiChecks.cs`, `ControllerChecks.cs`에서 네 크기·배치/설정 보존·XML 불변·재열기와 실제 ComboBox 양방향 바인딩을 확인했다. 기능 문서·검증 안내·완료 계획 15절·목차를 최신화했다.
- 빌드: `dotnet build Validation/DisplayTestChecks/DisplayTestChecks.csproj --artifacts-path <TEMP>/CowAuction-GAMSTest-UI-20261001 --no-restore -m:1 -v:minimal` 경고 0·오류 0.
- 테스트: 같은 출력의 `DisplayTestChecks.dll --skip-measurements --screenshots <TEMP>/CowAuction-GAMSTest-SizeSelect-20261001/screenshots` 1,012개 통과. 제어창 캡처에서 선택 상자와 아래 버튼 위치를 확인했다.
- 게시: `dotnet publish Tools/GAMSTest/GAMSTest.csproj -c Release --no-restore --artifacts-path <동일 경로> -o <TEMP>/CowAuction-GAMSTest-SizeSelect-20261001/publish-tool -m:1 -v:minimal` 성공. 운영 배포본과 분리했다.
- 보호·Git: 시작 상태·diff와 대상 스냅샷을 확인하고 기존 사용자 XML·제품 변경을 보존했다. 기준 HEAD 75ebba7, 브랜치 backup/pre-cleanup-20261001. commit 미생성·push 미수행.
- 미수행: 운영 서버·실제 전광판·다중 PC 검증은 미수행이다.


## 2026-10-01 — GAMSTest 현재 보드 크기 표시 복원

- 목적·승인: 사용자가 출하 AMS 위에 보드 크기 표시를 남기도록 요청했다. XML Size 자동 적용은 유지하면서 현재 크기를 확인할 수 있게 한다.
- 문제·원인: XML 직접 열기로 변경하면서 해상도 선택 상자와 크기 표시가 함께 사라졌다.
- 변경 파일: `Tools/GAMSTest/Views/ControlWindow.xaml`에 읽기 전용 크기 문구를 추가하고 `ViewModels/ControlWindowViewModel.cs`에 BoardSizeText와 파일 적용 성공 후 변경 알림을 추가했다. 오른쪽 상단 여백을 사용하여 기존 버튼 위치를 유지했다. `Docs/기능/전광판_점검도구.md` 사용법을 갱신했다.
- 검증: `dotnet build Validation/DisplayTestChecks/DisplayTestChecks.csproj --artifacts-path <TEMP>/CowAuction-GAMSTest-UI-20261001 --no-restore -m:1 -v:minimal` 경고 0·오류 0. 같은 출력의 `DisplayTestChecks.dll --skip-measurements --screenshots <TEMP>/CowAuction-GAMSTest-BoardSize-20261001/screenshots` 기존 검사 1,000개 통과. 새 테스트를 추가하지 않고 기존 바인딩·배치 검사와 캡처로 문구 위치·잘림 여부를 확인했다.
- 게시: `dotnet publish Tools/GAMSTest/GAMSTest.csproj -c Release --no-restore --artifacts-path <동일 경로> -o <TEMP>/CowAuction-GAMSTest-BoardSize-20261001/publish-tool -m:1 -v:minimal` 성공. 기존 운영 배포본과 분리했다.
- 영향·보호: 표시 문구와 XML 적용 후 알림만 변경했다. 사용자 XML·기존 제품 변경·전환 로직은 보존했다. 시작 상태·diff와 소스 스냅샷, 최종 변경을 확인했다.
- Git: 기준 HEAD 75ebba7, 브랜치 backup/pre-cleanup-20261001. commit 미생성·push 미수행.
- 미수행·추가 확인: 운영 서버·실제 전광판·파일 선택창 마우스 조작 미수행. 크기 자동 갱신은 코드 연결을 확인했으며 기존 파일 읽기 검사를 통과했다.


## 2026-10-01 — GAMSTest 진행 비고 전환 개선

- 목적·승인: 사용자의 비고 끊김 개선 요청에 따라 같은 화면 재선택 시 재부착을 생략하고 진행 페이지 간 공통 비고를 유지하도록 수정했다.
- 문제·원인: 기존 컨트롤러는 같은 모드를 다시 눌러도 모든 화면을 분리·재연결하고 IsRunning을 false/true로 바꿨다. 진행 페이지마다 별도 비고 호스트를 만들어 전환 시 비고 애니메이션을 다시 시작했다. 계속 표시 중의 프레임 저하 원인은 실측하지 않았다.
- 변경 파일·영향: `Tools/GAMSTest/Services/DisplayTestController.cs`에 현재 모드 중복 처리 생략과 슬롯별 진행 비고 호스트 공유를 적용했다. 공통 비고가 있는 128×128 진행 페이지는 본문만 교체하며 숨긴 본문 DataContext는 해제한다. 낙찰·유찰·번호·단색·자체 비고 화면 이동과 설정 교체·종료 정리는 유지한다. 제품 View·공통 애니메이션·운영 통신 코드는 수정하지 않았다.
- 검증: `Validation/DisplayTestChecks/ControllerChecks.cs`에서 실제 WPF 호스트·애니메이션·클록 동일성, 시간 진행, 반복 클릭·수동/자동 페이지 전환, 불필요한 Loaded/Unloaded·IsRunning 변경 없음, 숨긴 본문 해제, 결과·단색·설정 변경 정리, 번식우 자체 비고 예외를 확인했다.
- 빌드: `dotnet build Validation/DisplayTestChecks/DisplayTestChecks.csproj --artifacts-path <TEMP>/CowAuction-GAMSTest-UI-20261001 --no-restore -m:1 -v:minimal` 경고 0·오류 0.
- 테스트: 같은 출력의 `DisplayTestChecks.dll --skip-measurements` 1,000개 통과. 프레임률 및 100/200슬롯 성능 측정은 미수행이다.
- 게시: `dotnet publish Tools/GAMSTest/GAMSTest.csproj -c Release --no-restore --artifacts-path <동일 경로> -o <TEMP>/CowAuction-GAMSTest-NoteFlow-20261001/publish-tool -m:1 -v:minimal` 성공. 운영 배포본과 분리했다.
- 문서: 기능 문서·완료 계획 14절·Docs 목차·검증 README에 동작·검증 범위와 한계를 기록했다.
- 사용자 변경 보호·Git: 시작 상태·관련 diff·미추적 파일 스냅샷을 기준으로 컨트롤러·검증만 수정했다. 기존 사용자 설정·제품 수정·Infra 변경은 보존했다. 기준 HEAD 75ebba7, 브랜치 backup/pre-cleanup-20261001. commit 미생성·push 미수행.
- 미수행·추가 확인: 첫 화면 전체 생성 지연·지속 렌더링 부하·실제 전광판·운영 서버·다중 PC·장시간 실행 미수행. 이번 결과는 전환 시 비고 재시작 제거와 수명 정리 검증이다.


## 2026-10-01 — GAMSTest Board.XML 직접 열기

- 목적·승인: 사용자가 출하 AMS 아래 파일 열기 버튼으로 다른 경로의 Board.XML을 선택하는 변경을 승인했다. 수동 해상도 선택을 제거하고 XML의 Size를 적용한다.
- 문제·원인: 시작 시 자동 탐색만 제공하여 경로가 다른 현장 배치 선택이 어려웠고 수동 해상도 선택이 XML 설정과 다를 수 있었다.
- 변경 파일: `Tools/GAMSTest/Views/ControlWindow.xaml/.cs`에 출하 AMS 아래 파일 버튼·경로 툴팁·OpenFileDialog를 연결했다. `ViewModels/ControlWindowViewModel.cs`는 선택·취소·검증·적용·오류 처리를 담당하고 `Models/TestSettings.cs`, `Services/TestSettingsLoader.cs`는 현재 경로와 기존 users 설정을 유지한 보드 가져오기를 제공한다. 사용하지 않는 해상도 선택 속성·보조 형식을 제거했다.
- 동작·영향: Size·첫 MultiBoard·번호 배치만 교체한다. 성공 시 번호 화면으로 전환하고 이전 순환·캐시를 정리한다. 취소·읽기 실패·형식 오류·미지원 크기·400슬롯 초과는 기존 화면·순환·설정을 보존한다. XML 쓰기·경로 영구 저장·제품 통신 변경은 없다.
- 검증 파일: `Validation/DisplayTestChecks/ReferenceUiChecks.cs`, `ControllerChecks.cs`에서 파일 선택 콜백과 가상 XML로 네 크기·번호 순서·중복·users 설정 유지·원본 불변·취소/실패 보존·툴팁·버튼 배치를 검증했다. 기능 문서·완료 계획 13절·Docs 목차·검증 README를 최신화했다.
- 빌드: `dotnet build Validation/DisplayTestChecks/DisplayTestChecks.csproj --artifacts-path <TEMP>/CowAuction-GAMSTest-UI-20261001 --no-restore -m:1 -v:minimal` 경고 0·오류 0.
- 테스트: 동일 출력의 `DisplayTestChecks.dll --skip-measurements --screenshots <TEMP>/CowAuction-GAMSTest-BoardPicker-20261001/screenshots` 977개 통과. 캡처 확인 중 파일 버튼 잘림을 발견해 오른쪽 세로 배치를 조정한 뒤 재검증했다. 성능 측정은 반복하지 않았다.
- 게시: `dotnet publish Tools/GAMSTest/GAMSTest.csproj -c Release --no-restore --artifacts-path <동일 경로> -o <TEMP>/CowAuction-GAMSTest-BoardPicker-20261001/publish-tool -m:1 -v:minimal` 성공. 기존 운영 출력과 분리했다.
- 사용자 변경 보호·Git: 시작 상태·대상 diff와 미추적 파일 스냅샷을 확인했다. 기존 Infra 이관·제품 수정·사용자 XML은 그대로 보존했다. 기준 HEAD 75ebba7, 브랜치 backup/pre-cleanup-20261001. commit 미생성·push 미수행.
- 미수행·추가 확인: 실제 파일 선택창 마우스 조작·운영 서버·물리 전광판·다중 PC 미수행. 파일 선택은 주입 콜백과 실제 가상 XML 파일로 검증했다.


## 2026-10-01 — GAMSTest 기존 UI 복원과 해상도 선택

- 목적·승인: 사용자가 참고 GAMSTest와 같은 UI·동작을 유지하고 해상도만 추가하도록 요청하고 진행을 승인했다. 추가 에이전트 없이 해당 도구 범위에서 수정했다.
- 문제·원인: 초기 이관에서 큰 설정 대시보드와 별도 조작 흐름을 추가하여 참고 도구와 모양·사용법이 달라졌다.
- UI 파일: `Tools/GAMSTest/Views/ControlWindow.xaml`과 ViewModel을 기존 460×210 제어창·버튼·색상·밝기로 맞췄다. `app.ico`와 csproj에 기존 아이콘을 반영했다. `DisplayWindow.xaml`은 테두리·스크롤바 없는 전체 배치로 바꾸고 `BoardNumberView.xaml/.cs`로 파란 테두리 번호 화면과 해상도별 높이를 적용했다. 화면에 추가된 조작은 네 해상도 선택이다.
- 시작·설정·수명: `App.xaml.cs`, `Models/TestSettings.cs`, `Services/TestSettingsLoader.cs`, `Views/ControlWindow.xaml.cs`에서 자동 설정 탐색·첫 MultiBoard·StartPosition·제어창 위치·창 종료 정리를 연결했다. XML은 읽기 전용이며 선택한 해상도는 메모리에만 적용한다.
- 동작: `Services/DisplayTestController.cs`, `ViewModels/ControlWindowViewModel.cs`에서 밝기 170·기존 Green·마지막 단색·5초 순환과 BoardPage에 제한받지 않는 수동 유전 화면을 맞췄다. `Services/AuctionApplicationLauncher.cs`는 명시적인 출하 AMS 버튼의 기존 프로세스 활성화·실행 파일 탐색·실행을 담당한다. 현재 제품의 화면 재사용·캐시·DataContext 해제는 유지했다.
- 검증 파일: `Validation/DisplayTestChecks/ControllerChecks.cs`, `Program.cs`, `ReferenceUiChecks.cs`, README에 제어창·번호·설정 탐색·기존 조작·실행 콜백 검사와 성능 측정 생략 옵션을 반영했다.
- 빌드: `dotnet build Validation/DisplayTestChecks/DisplayTestChecks.csproj --artifacts-path <TEMP>/CowAuction-GAMSTest-UI-20261001 -m:1 -v:minimal`, `dotnet build Tools/GAMSTest/GAMSTest.csproj -c Release --artifacts-path <동일 경로> --no-restore -m:1 -v:minimal` 성공. 제품·도구·검증 Debug와 제품·도구 Release 경고 0·오류 0. 필요한 최초 복원 이후 --no-restore로 검증했다.
- 테스트: `DisplayTestChecks.dll --skip-measurements --screenshots <동일 경로>/screenshots` 962개 통과. 네 해상도 번호 이미지와 제어창 이미지를 확인했다. 100/200슬롯 성능 측정은 이번 UI 변경에서 반복하지 않았다.
- 실행 비교: 참고 Release 파일을 임시 위치에 복사하고 가상 XML로 실행하여 원본 제어창이 460×210임을 확인했다. 현재 Release도 가상 설정으로 실행하여 동일 제어창과 512×256 번호 표시창을 확인하고 정상 종료했다. 참고 설치본은 수정하지 않았다.
- 게시: `dotnet publish Tools/GAMSTest/GAMSTest.csproj -c Release --no-restore --artifacts-path <동일 경로> -o <동일 경로>/publish-tool -m:1 -v:minimal` 성공. 실행 파일 GAMSTest.exe와 Examples 가상 XML 2개를 확인했고 운영 Config·NLog.config는 포함하지 않았다. 기존 운영 배포본을 덮어쓰지 않았다.
- 문서·최종 확인: 기능 문서·완료 계획 12절·Docs 목차·검증 안내를 최신화했다. 로컬 문서 링크 72개, 작업 시작 스냅샷과의 파일 차이, Git 상태·diff 공백을 확인했다. 기존 Infra 변경과 사용자 XML은 이번 작업에 포함하지 않고 보존했다.
- Git: 기준 HEAD 75ebba7, 브랜치 backup/pre-cleanup-20261001. commit 미생성·push 미수행.
- 미수행·추가 확인: 출하 AMS 버튼으로 실제 운영 앱을 실행하는 검증은 미수행이며 콜백·가상 경로로 대체했다. 물리 전광판·운영 서버·다중 PC·장시간 실행도 미수행이다.


## 2026-10-01 — GAMSTest 다중 해상도 전광판 점검 도구

- 목적·승인: 사용자가 작성된 점검 도구 이관 계획의 구현과 필요한 에이전트 사용을 승인했다. 에이전트 3명이 공통 화면 생성·도구·검증을 분담하고 통합 담당이 설정·리소스·빌드·문서를 연결했다.
- 문제·원인: 참고 도구는 128×128 고정, 과거 표시 ViewModel·별도 비고·제품 DLL 경로 참조에 의존했다. 현재 DisplaySelect를 직접 생성하면 운영 통신·페이지 동기화가 시작되어 오프라인 점검에 사용할 수 없었다.
- 제품 변경: `Presentation/Display/AuctionViewFactory.cs` 추가, DisplaySelect의 생성 경로 위임, SetCustomDisplay의 3페이지 지원 조건 공유, AuctionContPanelViewModel의 표시 전용 생성자 추가. `Views/Common/DisplayResources.xaml`로 기존 리소스를 공유하고 App.xaml이 참조한다. 기존 네 해상도·사업장별 선택과 128×64 특수 순서, Unknown 해상도의 기본 순서를 보존했다. 기존 회전 정책·통신·업무 규칙을 수정하지 않았다.
- 도구 변경: `Tools/GAMSTest/`의 독립 .NET 10 WPF 앱·제어창/표시창·설정 스냅샷·복수 배치·가상 자료·번호/단색/RGB 강도·대기/진행/결과·단일 순환 타이머·캐시와 수명 정리·가상 예제 XML을 구현했다. 네 해상도를 지원하며 320×64 진행 2페이지/결과는 미구현 표시 후 선택·순환에서 제외한다. 설정 로더는 표시 항목만 읽고 운영 인증 항목을 보관하지 않는다.
- 프로젝트: 루트 csproj에서 Tools 파일 중복 수집을 제외하고 솔루션에 GAMSTest를 추가했다. ProjectReference를 사용하며 새 패키지·SDK 설치는 없다. 도구의 SDK 하위 프로젝트 콘텐츠 복사 단계를 제한하여 제품 Config·NLog.config가 도구 빌드/게시본으로 전파되지 않게 했다.
- 검증 추가: `Validation/DisplayTestChecks/`에 실제 View 생성·선택 일치, XML/DTD/중복 슬롯, 설정 스냅샷, 바인딩, 숨긴 화면 DataContext 해제, 순환/종료, 제어창 콘텐츠 및 100/200슬롯 측정을 추가했다.
- 빌드: `--artifacts-path <TEMP>/CowAuction-GAMSTest-20261001 -m:1`로 기존 출력과 분리했다. 제품 Debug, ReferenceAdoptionChecks, DisplayTestChecks(제품·도구 포함) Debug 및 `dotnet build CowAuctionSmall.sln -c Release ... --no-restore` 모두 경고 0·오류 0. 최초 필요한 복원 후 재빌드는 --no-restore를 사용했다.
- 테스트: 공통 생성 경로 적용 후 `ReferenceAdoptionChecks.dll all` 5,728개 통과. `DisplayTestChecks.dll --screenshots <TEMP>/.../screenshots` 989개 통과 및 네 해상도/모드·제어창 이미지 확인. 마지막 Unknown/특수 사업장 경계 보완과 검사 2개 추가 후 `DisplayTestChecks.dll` 전체 991개 통과. 최초 대기 모드 기대 개수 오류·nullable 경고 3개는 수정 후 재검증했다.
- 게시: `dotnet publish Tools/GAMSTest/GAMSTest.csproj -c Release --no-restore --artifacts-path <동일 경로> -o <동일 경로>/publish-tool -m:1` 성공. 운영 배포본은 덮어쓰지 않았다. 게시 실행 파일은 GAMSTest.exe이며 XML은 Examples의 가상 2개뿐이다. 제품 Config·NLog.config 미포함 확인.
- 측정: 최종 숨겨진 HWND 측정에서 100/200슬롯 준비 2,928.3/3,813.1 ms, 최초 15회 전환의 레이아웃 포함 평균 956.9/1,436.1 ms였다. 캐시는 500/1,000개로 유지, 6회 순환 후 관리 메모리 93.9/186.0 MiB에서 종료 후 각각 3.2 MiB로 내려왔다. 부하에 따른 변동이 있으며 실제 전광판 지연·도미노 해소·장시간 누수 부재를 보장하지 않는다. 상세 수치는 완료 계획 11절에 기록했다.
- 문서: 계획을 `Docs/계획/완료/`로 옮기고 구현 근거를 추가했다. 기능 사용법·검증 README·전체 목차·루트 README·파일구조 안내를 갱신했다. 문서 로컬 링크 165개, 신규 소스 UTF-8/공백, 관련 diff를 확인했다.
- 사용자 변경 보호: 기존 Infra 이름 변경 및 사용자 XML 변경을 보존했다. 검사 전후 Config XML 4개의 SHA-256이 동일하다. 기존 미커밋 이름 변경을 이번 도구의 신규 수정으로 보고하지 않는다.
- Git: 기준 HEAD 75ebba7, 브랜치 backup/pre-cleanup-20261001. 이번 작업 commit 미생성·push 미수행.
- 미수행·추가 확인: 운영 제품 App·서버·실제 전광판·다중 PC·장시간 실행·운영 배포 미수행. 200슬롯의 전환 지연은 현장 조건에서 확인해야 한다. 기존 새로고침 회전 문제·미구현 화면 신규 디자인·로그 압축은 이번 범위가 아니다.


## 2026-10-01 — Infrastructure → Infra, Configuration → XmlSettings 명칭 변경

- 목적·승인: 사용자가 합의한 `Infra`와 `XmlSettings` 명칭으로 변경하도록 요청했다. 기존 계층 구조와 기능을 유지하면서 경로·네임스페이스를 맞췄다.
- 변경 파일·내용: `Infrastructure/`의 소스 32개를 `Infra/`로 이동하고 XML 파서 3개는 `Infra/XmlSettings/`에 배치했다. `CowAuctionSmall.Infrastructure.*`를 `CowAuctionSmall.Infra.*`, 설정 네임스페이스를 `CowAuctionSmall.Infra.XmlSettings`로 변경했다. App·Application·Presentation·ViewModels·MainWindow code-behind와 두 Validation 프로젝트의 참조까지 총 59개 소스 파일에 반영했다.
- 문서: 루트 README, Docs 목차·파일구조 안내·기능 문서의 현재 경로를 갱신했다. 과거 계획의 본문과 기존 WORKLOG는 보존하고 실제 소스 링크만 새 위치로 연결했다. 검증 README에 이번 전체 검사 결과를 추가했다.
- 영향 범위: 명칭·경로·참조·현재 안내 문서. 비교 검사에서 전체 추적 소스가 기존 HEAD에 명칭 치환만 적용한 결과와 일치했으며 예상 밖 코드 변경은 없었다. 패키지·프로젝트 설정·XML 형식·운영 값·업무 로직은 변경하지 않았다. 기존 사용자 XML 변경 3개는 보존했다.
- 빌드: `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --artifacts-path <TEMP>/CowAuction-InfraRename-20261001 -v:minimal`, 이어 같은 경로의 `Validation/OwnerHistoryChecks/OwnerHistoryChecks.csproj` 빌드 모두 성공. 각 경고 0·오류 0. 새 artifacts 경로에 필요한 복원만 수행했다.
- 테스트: 해당 출력의 `ReferenceAdoptionChecks.dll all` 5,728개 통과, `OwnerHistoryChecks.dll --offline` 55+25=80개 통과. 기존 경매 수신·화면·설정 파싱·로그·이력·캐시 검사를 활용했고 이름 변경만을 위한 신규 검사는 추가하지 않았다.
- 확인: 활성 소스의 이전 네임스페이스·경로 참조 없음, 이동 32개/참조 변경 59개 대조, 갱신 문서 로컬 링크 227개 존재 확인, diff 공백·최종 상태 확인.
- Git: 기준 HEAD 75ebba7, 브랜치 backup/pre-cleanup-20261001. 이번 변경 commit 미생성·push 미수행.
- 미수행·추가 확인: 운영 앱·실제 서버·다중 PC·전광판·Release·배포 미수행. 이 검증은 기존 새로고침 회전 문제 등의 해결을 의미하지 않는다.


## 2026-10-01 — 문서 최신화와 GitHub 반영 준비

- 목적·승인: 사용자의 문서·Git·GitHub 최신화 요청에 따라 미커밋 구현과 후속 UI 조정을 정리하여 기존 비공개 백업 원격에 반영한다.
- 문서 변경: 루트 `README.md`, `Docs/README.md`, `Docs/기능/README.md`, `화면표시_페이지전환.md`, `설정_로그.md`, 완료 상태바 계획, 검증 README를 현재 배치와 단계별 검사 기록에 맞췄다. 최초 계획·이전 실행 결과는 이력으로 구분하고 완료 계획 10절에 최종 상태를 정리했다.
- 반영 범위: 상태바 통합·버전 인접 배치·구간 명칭/위치·여백 조정, 수신/회전 상태 전달, 검증 코드·문서. 사용자 직접 변경인 `Views/Size128_128/CustomAuctionSold/JangseongSold.xaml`의 B 모드 여백·글자 크기·번호 괄호 표시도 현재 내용으로 포함한다. 이 변경은 에이전트 구현으로 보고하지 않는다.
- 제외·보존: 인증정보가 포함된 `Config/users.XML`의 현재 변경과 미추적 사용자 XML 2개는 이전 백업 기준대로 로컬에 보존한다. 기존 Git 이력에 있는 설정 파일을 삭제하거나 이력을 재작성하지 않았다.
- 검증: 문서 로컬 링크 112개 존재, 포함 대상 diff 공백·상태 확인. 기존 최종 소스 빌드 경고 0·오류 0 및 `footer` 119개 결과를 대조했다. `stage3` 594개와 `stage4` 837개는 각각 당시 단계의 기록으로 구분했다. 이번 문서/Git 작업에서는 소스 수정·빌드·앱 실행을 반복하지 않았다.
- Git 사전 확인: `backup/pre-cleanup-20261001`, 기준 HEAD `977679a`, 원격 backup과 사전 차이 0/0. 대상 `AlchemistTF/CowAuctionSmall-backup`의 private=true와 push 권한 확인. origin은 변경하지 않는다.
- 커밋 메시지: `feat: 메인 상태바 통합 및 구간 표시·문서 최신화`. commit hash 미생성·push 미수행(이 기록 작성 시점). 커밋·푸시 후 로컬/원격 HEAD 일치를 확인하여 완료 보고한다.
- 미수행·추가 확인: 운영 서버·전광판·배포 미수행. 기존 새로고침 후 회전 재개 문제는 후속 과제로 유지한다.


## 2026-10-01 — 구간 정보 위치 이동과 상태바 여백 확대

- 목적·승인: 사용자의 구간 정보를 버전 왼쪽으로 배치하고 사진의 좁은 항목 간격을 개선하려는 요청을 반영했다.
- 변경: `Views/MainWindow.xaml`에 구간 셀을 페이지 상태와 버전 사이에 배치하고 빈 값이면 숨긴다. 최근 수신 줄은 신호 문구만 표시하며 합친 안내는 툴팁으로 유지한다. 공통 Padding은 12,5, 최소 높이는 28, 페이지 번호·상태·점 간격을 확대했다.
- 문서: `Docs/기능/화면표시_페이지전환.md`와 완료 상태바 계획에 현재 배치와 후속 검증을 기록했다.
- 영향: 하단 XAML 배치·여백만 변경. 구간 파싱·조회, 경매 상태 문구·처리, 사용자 XML·JangseongSold.xaml 및 기존 미커밋 변경 보존.
- 검증: `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --artifacts-path <TEMP>/CowAuction-Footer-20261001 --no-restore -v:minimal` 성공, 경고 0·오류 0. 해당 출력의 `ReferenceAdoptionChecks.dll footer` 119개 통과. 128/320/768/1200 폭 항목 배치·창 높이·로그 높이 확인, 1200/320 합성 렌더 육안 확인. 별도 신규 검사 코드는 추가하지 않았다.
- Git: 기준 HEAD 977679a, commit 미생성·push 미수행. 변경 diff 공백·상태 확인.
- 미수행: 운영 앱·서버 연결·실제 전광판·배포. 폭이 좁으면 기존 설계대로 상태바가 여러 줄로 표시된다.


## 2026-10-01 — 구간 번호 유지와 경매 선택 범위 명칭 정리

- 목적·승인: 범위 명칭 논의 후 사용자가 구간 번호도 유지하도록 요청했다. 사전 안내한 대로 `1구간`은 보존하고 `출품`을 `경매프로그램 선택 구간`으로 변경했다.
- 변경 파일·내용: `ViewModels/MainWindowViewModel.cs`의 확인된 범위 표시 문구만 변경. `Validation/ReferenceAdoptionChecks/AuctionSectionChecks.cs`, `FooterStatusChecks.cs`의 기대 문구·합성 예시와 `Docs/기능/화면표시_페이지전환.md`의 현재 표시 설명을 갱신했다.
- 영향 범위: 하단 상태 문구. 경매 종료·최근 수신 안내 문구, 구간 수신·조회·필터·경매 처리 동작은 유지했다. 기존 사용자 변경도 보존했다.
- 검증: `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --artifacts-path <TEMP>/CowAuction-Footer-20261001 --no-restore -v:minimal` 성공, 경고 0·오류 0. 해당 출력의 `ReferenceAdoptionChecks.dll stage3` 594개, `footer` 115개 통과. diff 공백 확인.
- Git: 기준 HEAD 977679a, commit 미생성·push 미수행.
- 미수행·추가 확인: 운영 앱·서버·실제 전광판·배포 미수행. 긴 문구는 기존 말줄임·툴팁을 사용한다.


## 2026-10-01 — 상태바 버전을 페이지 상태 옆으로 배치

- 목적·승인: 사용자의 “버전도 옆으로 붙이자” 요청에 따라 상태 항목을 한 흐름으로 읽도록 배치했다.
- 원인·변경: `Views/MainWindow.xaml`의 오른쪽 고정 버전과 폭별 표시 분기를 제거하고 페이지 상태 다음 셀로 통일. `ViewModels/MainWindowViewModel.cs`에서 사용하지 않는 `IsWideStatusBar` 속성과 알림을 제거했다. 좁은 폭의 줄바꿈·로그 높이는 유지한다.
- 문서: `Docs/기능/화면표시_페이지전환.md`와 완료 상태바 계획에 현재 배치를 반영했다.
- 검증: `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --artifacts-path <TEMP>/CowAuction-Footer-20261001 --no-restore -v:minimal` 성공, 경고 0·오류 0. 같은 출력의 `ReferenceAdoptionChecks.dll footer` 115개 통과. 1200 폭 합성 렌더에서 버전이 페이지 상태 옆에 붙는 것을 확인했다.
- 범위·Git: 기존 상태바 구현과 사용자 XML·JangseongSold.xaml 변경 보존. 기준 HEAD 977679a, commit 미생성·push 미수행. 변경은 하단 배치와 불필요해진 표시 속성·문서에 한정한다.
- 미수행: 운영 앱 실행·서버 접속·실제 전광판·배포. 관련 없는 기존 회귀 검사는 반복하지 않았다.


## 2026-10-01 — 메인뷰 하단 상태바·흰색 안내 통합

- 목적·승인: 상태바 통합 계획 작성 후 사용자의 에이전트 분담 구현 요청에 따라 3명이 경매 상태, 회전 상태, UI·검증을 나누어 진행했다.
- 문제·원인: 경매·이력 안내와 하단 모드 표시가 분리되고, 기존 연결상태 문구가 페이지 고정·복구도 나타내 혼동할 수 있었다. 타이머 실행 정보 없이 비고정 상태만으로 회전을 표시하면 실제 정지를 놓칠 수 있었다.
- 변경 파일·내용:
  - `Views/MainWindow.xaml`, `ViewModels/MainWindowViewModel.cs`, 신규 `ViewModels/MainStatusViewModel.cs`: 상태바·흰색 안내를 로그 위로 통합, 구조화된 표시 속성·구독·해제, 폭별 줄바꿈과 버전 정렬. `Views/MainWindow.xaml.cs`는 실측 높이 전달만 추가.
  - 신규 `Domain/Auction/AuctionDisplayState.cs`, `Application/Messaging/AuctionDisplayStateMessage.cs`, `Application/Auction/NettyAsyncMsgProcess.cs`: 검증된 방식·단계·번호를 표시용으로 전달. 8003 예정가 변경과 미확정 신호를 진행으로 오인하지 않으며 실패 시 상태 초기화.
  - `Infrastructure/Netty/netty/NettyControllable.cs`: 실제 연결 종료 메시지 전달. `Infrastructure/Netty/netty/handlers/AuctionClientInboundDecoder.cs`: 활성 채널 예외는 기존 연결 예외 콜백으로 전달하여 종료와 구분.
  - `Presentation/Display/PageRotationSnapshot.cs`, `Application/Messaging/DataChangedMessage.cs`, `Presentation/Display/DisplaySelect.cs`: 실제 타이머 실행 값 전달, 단발 타이머 재시작·상태 변화 시 발행 보완. 회전 정책 변경 없음.
  - `Validation/ReferenceAdoptionChecks/` 신규 `AuctionDisplayStateChecks.cs`, `FooterRotationChecks.cs`, `FooterStatusChecks.cs`와 `Program.cs`: 상태 전이·실제 UI 레이아웃 오프라인 검사. `HistoryProgressUiChecks.cs`의 옛 높이 탐색을 이름 탐색으로 변경. `DisplayLifetimeChecks.cs`의 오래된 사업장 기대값을 기존 HEAD와 일치시켰으며 제품 선택 정책은 보존.
  - `Docs/README.md`, `Docs/기능/화면표시_페이지전환.md`, `Validation/ReferenceAdoptionChecks/README.md`: 실제 동작·검증 범위 반영. 상태바 계획은 결과를 기록하고 `Docs/계획/완료/`로 이동.
- 영향 범위: 하단 표시와 표시용 메시지, 연결 예외 표시 정확성. 경매품 화면·API·캐시·설정·회전 재개 정책은 유지. 사용자 변경 `Config/users.XML`과 미추적 사용자 XML 2개 보존. 기존 계획·목차 변경을 이어 반영했다.
- 빌드: `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --artifacts-path <TEMP>/CowAuction-Footer-20261001 -v:minimal`, 후속 동일 경로 `--no-restore` 빌드 모두 성공. 앱·검증 프로젝트 경고 0개·오류 0개. Visual Studio 출력과 분리했다.
- 검사: `dotnet <검증출력>/ReferenceAdoptionChecks.dll footer` 113개, `stage3` 594개, `stage4` 837개 통과. 첫 stage4 실패는 기존 테스트가 옛 3페이지 대상 사업장을 기대한 원인이었으며 HEAD와 대조해 검증 데이터를 정정 후 통과했다. 폭 128/320/768/1200, 로그 높이, 창 높이, 메뉴 보존 확인. 320/1200 폭 합성 화면 PNG 육안 확인.
- Git: 기준 HEAD `977679a`, 브랜치 `backup/pre-cleanup-20261001`. 이번 작업 commit hash 미생성, push 미수행. 현재 요청은 구현·검증이며 별도 Git 게시를 수행하지 않았다.
- 추가 확인·미수행: 실제 운영 앱·서버 접속·전광판·다중 PC·여러 시스템 DPI·Release·배포 미수행. 초기 새로고침 후 회전 재개 누락은 별도 후속 과제로 유지한다.


## 2026-10-01 — GitHub 비공개 백업 후 루트 자료·프로젝트 설정 정리

- 목적·승인: 사용자의 전체 상태 Git 백업 후 정리 요청과 본인 계정의 비공개 백업 저장소 선택에 따라 실행했다. 루트에 남은 과거 문서·백업 파일, 존재하지 않는 폴더 선언과 이전 PC의 패키지 경로를 정리한다.
- 선행 백업: 정리 전 소스·문서 변경을 `b5370d4`로 커밋하고 새 비공개 백업 저장소의 `backup/pre-cleanup-20261001` 브랜치에 푸시했다. 원격 해시 일치를 확인한 뒤 파일 정리를 시작했다. 기존 origin/master는 변경하지 않았다. 인증정보가 있는 사용자 XML 3개는 이번 변경 커밋에서 제외하고 로컬 원본·별도 사본을 보존했다.
- 문서 이동: `소스 설명/`의 Excel 1개·PowerPoint 2개를 `Docs/참고자료/`로, 루트 `기록용.txt`를 `Docs/이력/`으로, `Docs/경고.md`·`메시지.md`를 `Docs/진단기록/`으로 이동했다. 총 6개 파일의 SHA256이 이동 전과 같음을 확인했다. 비어 있는 이전 `소스 설명/` 폴더를 제거했다.
- 파일·설정 정리: Git 백업에 보존된 `App.xaml.cs.bak`를 제거했다. `CowAuctionSmall.csproj`에서 이전 PC의 NLog.config 패키지 절대경로 제외 항목과 존재하지 않는 Resources·CustomRunning_64 폴더 선언만 제거했다.
- 안내 문서: `README.md`에서 없는 CODEX_HANDOFF 링크를 제거하고, `Docs/README.md`에 새 자료 위치와 링크를 반영했다. 이 WORKLOG의 기존 기록은 보존했다.
- 영향 범위: 문서 배치·사용하지 않는 백업과 프로젝트 메타데이터. 앱 소스·기능·패키지 버전·대상 프레임워크·아이콘·운영 XML은 변경하지 않았다. bin/obj/.vs 수동 삭제나 운영 배포본 교체는 수행하지 않았다.
- 빌드: `dotnet build CowAuctionSmall.csproj --artifacts-path <임시경로>/artifacts -v:minimal` 성공, 경고 0개·오류 0개. 임시경로는 `%TEMP%/CowAuction-Cleanup-20261001-r4ge32ah`. 새 출력 경로에 필요한 복원을 수행하고 기존 Visual Studio 출력과 분리했다.
- 검증: 이동 자료 6개 내용 보존, 운영 설정 해시 보존, 갱신한 두 목차의 로컬 링크 61개 존재, 변경 diff·Git 상태를 확인했다. 업무 로직 변경이 없어 별도 기능 테스트는 추가하지 않았다.
- Git: 현재 브랜치 `backup/pre-cleanup-20261001`, 정리 기준 커밋 `b5370d4`, 원격 백업 확인 완료. 정리 결과 commit hash: 미생성(이 기록 작성 시점), 정리 결과 push: 미수행(이 기록 작성 시점). 기록 검증 후 별도 정리 커밋으로 같은 백업 원격에 반영한다.
- 추가 확인·미수행: Release 빌드·앱 실행·운영 서버 접속·실제 전광판 확인·배포 미수행. 이번 빌드 성공은 운영 화면 검증 완료를 의미하지 않는다.


## 2026-09-30 — AS 구간 범위 백그라운드 조회와 상태줄 갱신 연결

- 목적·승인: 사용자의 “수정해보자” 요청에 따라 AS에서 받은 축종·구간 번호를 API 범위 조회와 연결. 앞서 승인된 프로그램 실행 확인 범위에서 실제 상태줄도 검증.
- 문제·원인: AS에는 날짜·시작/끝 번호가 없으며 기존 구현은 선택적 조회 진입점만 제공하여, 서버에 범위가 있어도 상태줄에는 미확인이 남았음.
- 변경 파일·내용:
  - `Application/Auction/AuctionSectionLookup.cs`: 별도 비동기 조회와 세마포어, 순차 상태 큐에서 결과 반영. 사업장·날짜·축종·구간 키로 정상 범위 5분, 실패 30초 캐시(최대 64개, 세션 메모리). 동일 진행 요청 재사용, 구간 변경·모드 전환·명시적 새로고침·종료 취소. 요청 제한 10초, 늦은 응답의 구간·날짜 재검증. SZ의 명시적 범위도 재사용 가능.
  - `Application/Abstractions/IAuctionSectionSource.cs`: 구간 조회 경계 추가.
  - `Infrastructure/Api/ServerAuctionDataSource.cs`: 실제 ServerConn 구간 조회 연결.
  - `Infrastructure/Api/ServerConn.cs`: 구간 조회를 API 호출 집계에 포함.
  - `Application/Auction/NettyAsyncMsgProcess.cs`: 일괄 AS/SZ/AT를 구간 조회 조정자에 연결, AS 8001 및 AT에서 캐시 초기화. 확인된 결과를 기존 구간 메시지로 전달. 종료 정리.
  - `Application/Auction/ServerGetData.cs`: 로그인 사업장과 현재 선택 날짜(없으면 오늘), 서비스 종료 토큰으로 조회 구성.
  - `Domain/Auction/AuctionSectionState.cs`: 검증된 API 범위를 담는 불변 상태 생성 메서드 추가.
  - `Validation/ReferenceAdoptionChecks/AuctionSectionLookupChecks.cs`, `AuctionSectionChecks.cs`: 반복 수신 중복 조회 억제, 상태 큐 비차단, 늦은 구간·날짜 응답 차단, 캐시·명시 새로고침·실패 재시도·SZ 범위·종료 검증.
  - `WORKLOG.md`: 완료 기록.
- 영향 범위: 수신 구간의 상태줄 표시를 위한 조회와 메모리 캐시. 기존 화면 대상 필터·경매 업무 데이터·페이지 전환·애니메이션·운영 XML·SQLite 구조 변경 없음.
- 빌드·검증:
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --artifacts-path C:/Users/jyj/AppData/Local/Temp/CowAuction-SectionLookup-zema28d2/artifacts -v:minimal`: 분리 출력 복원·빌드 성공, 경고 0개·오류 0개.
  - `dotnet C:/Users/jyj/AppData/Local/Temp/CowAuction-SectionLookup-zema28d2/artifacts/bin/ReferenceAdoptionChecks/debug/ReferenceAdoptionChecks.dll stage3`: 594개 오프라인 검증 통과.
  - 현재 소스 users.XML과 검증 실행본 설정 해시 일치 확인. 제가 실행해 둔 이전 검증 창만 정상 종료하고 새 검증 빌드 실행.
  - 실제 신호 수신 후 UI Automation으로 흰색 상태줄의 `구분 1(송아지) · 1구간 · 출품 1~999번` 및 마스터·연결 중 표시 확인. 검증 앱은 실행 상태로 유지.
  - 작업 전 사본 비교 및 관련 diff·공백 검사 확인.
- Git 정보: master, HEAD 52c554d. 기존 사용자 변경 보존, commit hash 미생성, push 미수행.
- 추가 확인: 실제 구간·날짜 연속 전환과 API 장애는 합성 응답으로 검증했으며 현장 재현은 미수행. 실제 전광판 장비·Release 빌드·배포 미수행.


## 2026-09-30 — 경매 신호 상태줄에 수신 구간 표시

- 목적·승인: 사용자의 “그렇게 추가하자” 요청에 따라 흰색 경매 신호 옆에 구분 코드·명칭·구간·출품번호 범위를 표시.
- 문제·원인: 수신 구간 상태는 보관하지만 기존 AuctionSignalText에는 신호 설명만 표시됨.
- 변경 파일·내용:
  - `Application/Messaging/AuctionSignalMessage.cs`: 불변 상태를 전달하는 AuctionSectionMessage 추가.
  - `Application/Auction/NettyAsyncMsgProcess.cs`: 구간 상태 변경 시에만 별도 메시지 게시. 잘못된 신호·AT 초기화도 전달하여 이전 범위 표시 제거.
  - `ViewModels/MainWindowViewModel.cs`: UI Dispatcher에서 구간 문구 구성, 코드 0(전체)·1(송아지)·2(비육우)·3(번식우) 표시. 확인된 범위는 출품 시작~끝번, AS는 범위 미확인, 잘못된 입력은 구간 정보 미확인. 모드 초기화 시 구간 문구 제거. 신호와 구간 변경 모두 결합 문구 변경 알림, 종료 시 메시지 구독 해제.
  - `Views/MainWindow.xaml`: 기존 흰색 상태줄과 툴팁을 AuctionSignalDisplayText에 연결. 기존 높이·색상·말줄임 유지.
  - `Validation/ReferenceAdoptionChecks/AuctionSectionChecks.cs`: 실제 ViewModel 문구·변경 알림, 중복 수신 억제, 잘못된 입력과 단일 모드 전환의 UI 메시지 검증 추가.
  - `WORKLOG.md`: 완료 기록.
- 영향 범위: 하단 상태 안내만 변경. 화면 대상 필터·경매 업무 메시지·API 자동 호출·운영 XML·애니메이션 변경 없음. 기존 사용자 변경 보존.
- 빌드·검증:
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --artifacts-path C:/Users/jyj/AppData/Local/Temp/CowAuction-SectionStatus-xc0izdqa/artifacts -v:minimal`: 복원·빌드 성공, 경고 0개·오류 0개.
  - `dotnet C:/Users/jyj/AppData/Local/Temp/CowAuction-SectionStatus-xc0izdqa/artifacts/bin/ReferenceAdoptionChecks/debug/ReferenceAdoptionChecks.dll stage3`: 오프라인 검증 581개 통과.
  - 작업 전 사본과 관련 diff 및 공백 검사 확인.
- Git 정보: master, HEAD 52c554d. commit hash 미생성, push 미수행.
- 추가 확인: 실제 운영 앱·수신 신호·전광판 화면 및 Release 빌드·배포 미수행. 좁은 창에서는 기존처럼 말줄임되며 전체 문구는 툴팁에 제공.


## 2026-09-30 — 일괄경매 축종·구간 메타데이터 수신 기반 추가

- 목적·승인: 사용자의 “좋아 가져와보는 수정을 해보자” 요청으로 Total_AMS__의 SZ/AS 구간 해석과 구간 조회 계약을 현재 구조에 맞게 도입. 화면 필터 적용은 이번 범위에서 제외.
- 문제·원인: 기존 Netty 수신 처리에서 AS는 업무 상태만, SZ는 날짜만 다음 단계로 전달하여 축종·구간 번호·시작/끝 번호를 재사용할 수 없었음.
- 변경 파일·내용:
  - `Domain/Auction/AuctionSectionState.cs`: 불변 구간 상태와 범위 타입. SZ 날짜·사업장·축종 0~3·구간·명령·양 끝 출품번호 검증, AS 고정 위치 12/13 해석. 숫자 오버플로·역전 범위·누락·지원하지 않는 값은 미확인 처리. 999 상한 하드코딩 없음.
  - `Application/Auction/NettyAsyncMsgProcess.cs`: 기존 순차 수신 큐에서 CurrentSection 원자 교체. 일괄 AS는 범위 미확인 상태로 교체하며 이전 SZ 범위를 끌어오지 않음. 유효한 AT 수신 시 초기화. 기존 업무 메시지 및 화면 갱신 흐름 유지.
  - `Infrastructure/Api/AuctionSectionApiClient.cs`: 요청 시에만 구간 API 폼 조회. stnList의 RG_SQNO가 유일하게 일치하는 항목에서 유효한 ST_AUC_NO/ED_AUC_NO 반환. 실패·빈 응답·중복 구간은 null, 호출자 취소 전파. 원문 저장·로깅 없음.
  - `Infrastructure/Api/ServerConn.cs`: 로그인 세션의 인증 정보와 사업장, 명시적 날짜를 사용하는 ReadAuctionSectionRangeAsync 진입점 추가. 기존 날짜별 PostQcn과 분리.
  - `Validation/ReferenceAdoptionChecks/AuctionSectionChecks.cs`, `Program.cs`: stage3/all에 도메인·실제 수신 소비자·가짜 HTTP 검증 연결.
  - `WORKLOG.md`: 완료 기록.
- 상태 사용: NettyAsyncMsgProcess.CurrentSection에서 IsValid/IsRangeConfirmed와 메타데이터 확인. AS는 날짜·범위를 포함하지 않아 해당 값은 null. API 조회 결과는 반환만 하며 현재 상태나 화면에 자동 반영하지 않음.
- 영향 범위: 일괄경매 구간 정보의 메모리 보관 및 선택적 조회 기반. 현재 목록 표시·날짜 갱신·낙찰/유찰 처리·타이머·SQLite·users.XML 변경 없음. API 자동 호출 추가 없음. 참고 프로젝트 수정 없음.
- 빌드·검증:
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --artifacts-path C:/Users/jyj/AppData/Local/Temp/CowAuction-SectionMetadata-gondy7ra/artifacts -v:minimal`: 분리 출력 복원·빌드 성공, 경고 0개·오류 0개.
  - `dotnet C:/Users/jyj/AppData/Local/Temp/CowAuction-SectionMetadata-gondy7ra/artifacts/bin/ReferenceAdoptionChecks/debug/ReferenceAdoptionChecks.dll stage3`: 569개 오프라인 검증 통과.
  - 정상/누락/잘못된 날짜·축종·범위, AS 후행 구분자·구간 0, 과거 범위 초기화, 단일 모드 전환, 기존 SZ/AS 업무 메시지 유지, API 선택·폼 계약·실패·취소 확인.
  - 작업 전 사본 비교 및 관련 diff/공백 검사 완료.
- Git 정보: master, HEAD 52c554d. 기존 사용자 변경 보존. commit hash 미생성, push 미수행.
- 추가 확인: 실제 운영 API·수신 신호·전광판 확인, Release 빌드 및 배포 미수행. 실제 구간 필터와 API 결과의 상태 반영은 후속 적용 사항.


## 2026-09-30 — 페이지 수와 관계없이 모드 상태줄 표시

- 목적·승인: 사용자의 “맞아 그렇게 변경하자” 요청에 따라 마스터·서브 모드, 상태 및 1페이지 점을 일관되게 표시.
- 문제·원인: 총 페이지가 1이면 PageIndicatorVisibility를 Collapsed로 설정하여 모드·상태를 포함한 하단 전체가 숨겨졌음.
- 변경 파일·내용:
  - `ViewModels/MainWindowViewModel.cs`: 상태줄 초기값과 페이지 상태 수신 시 Visibility를 Visible로 유지. 기존 점 생성 루프는 1페이지에도 점 하나를 생성하므로 유지.
  - `Validation/ReferenceAdoptionChecks/WarningBoundaryChecks.cs`: 사용자 선행 변경인 창 높이 여유 134에 맞춰 기존 높이 기대값 두 곳을 226에서 262로 갱신.
  - `WORKLOG.md`: 완료 기록.
- 영향 범위: 하단 모드·상태줄 표시. 사용자 변경인 XAML 높이 106 및 창 높이 여유 134 보존. 페이지 회전·역할 판정·통신 동작 변경 없음.
- 빌드·검증:
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --artifacts-path C:/Users/jyj/AppData/Local/Temp/CowAuction-ModeIndicator-b697wfdh/artifacts -v:minimal`: 복원·빌드 성공. 이후 `--no-restore` 재빌드 성공, 경고 0개·오류 0개.
  - `dotnet C:/Users/jyj/AppData/Local/Temp/CowAuction-ModeIndicator-b697wfdh/artifacts/bin/ReferenceAdoptionChecks/debug/ReferenceAdoptionChecks.dll stage4`: 기존 창 높이 기대값으로 최초 실패, 갱신 후 오프라인 검증 805개 통과.
  - 작업 전 사본과 비교하여 ViewModel 두 줄 및 검증 기대값 두 곳만 변경 확인. 단일 페이지 점 생성 및 표시줄 XAML 바인딩 확인.
- Git 정보: master, HEAD 52c554d. 기존 사용자 변경 보존, commit hash 미생성, push 미수행.
- 추가 확인: 실제 운영 앱·서버 연결·전광판 및 Release 빌드·배포 미수행. 실제 실행 화면의 표시 확인은 남아 있음.


## 2026-09-30 - 128×128 염소 낙찰자 이름 위치·크기 보정

### 작업 일자

- 2026-09-30

### 작업 목적

- 수원축협에서 영천축협 염소 일괄경매를 진행할 때 128×128 염소 낙찰 화면의 낙찰자 이름을 `낙찰자 :` 라벨 오른쪽에 기존 크기로 표시한다.

### 원인

- `GoatSold`의 낙찰자 값 영역이 `Grid.Column="1"`에서 시작하여 0~1열을 사용하는 `낙찰자 :` 라벨 영역과 겹쳤다.
- 값 영역이 남은 전체 폭으로 늘어나면서 이름의 시작 위치가 다른 128×128 낙찰 화면보다 왼쪽으로 배치됐다.
- Y 모드 이름 컨트롤에 폰트 크기가 지정되지 않아 공용 `BidderNameFlow`의 기본값 13px가 적용됐으며, 기존 염소 낙찰 화면에서 사용하던 18px보다 작게 표시됐다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/GoatSold.xaml`: 낙찰자 값 영역을 2열부터 3개 열에 배치하고 왼쪽 여백 7px을 적용했다. Y 모드 이름은 18px로 복원하고 글자 잘림을 막기 위해 컨트롤 높이를 22px로 조정했다.
- `WORKLOG.md`: 염소 낙찰자 이름 위치 보정 내용과 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 128×128 `GoatSold`를 사용하는 염소·말 낙찰 화면의 낙찰자 표시 위치와 Y 모드 이름 크기에 적용된다.
- 낙찰자 표시 데이터와 Y/N/B 분기, 진행·유찰 화면, 영천 염소 일괄경매 진행 테두리 및 통신·파싱 로직은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall\CowAuctionSmall.csproj --configuration Debug --no-restore --nologo`: 성공, 오류 0개 / 기존 경고 102개.
- 낙찰자 값 영역 `Grid.Column="2"`, `Grid.ColumnSpan="3"`, `Margin="7,0,0,0"` 적용 확인: 정상.
- Y 모드 이름 `FontSize="18"`, `Height="22"` 적용 확인: 정상.
- 실제 수원축협 전광판에서 Y 모드 이름 위치와 18px 크기 확인: 정상.
- 긴 이름 흐름 표시 및 N/B 모드 확인: 미수행.
- 영천 염소 일괄경매 진행 테두리 회귀 확인: 코드 변경 없음, 실제 화면 확인 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 염소 낙찰자 이름 위치와 크기 보정`


## 2026-09-29 — 중복 유찰 화면을 QQuriUnSold로 통합

- 목적·승인: 사용자가 QQuriUnSold에 출하지역·출하주 표시를 직접 추가한 뒤 “좋아 내가 추가했으니 이제 삭제할건 정리할수 있겠지?”라고 요청하여 기존 OutLineUnSold 두 화면과 선택 참조 정리.
- 문제·원인: OutLineUnSold와 OutLineUnSold_2는 테두리 색상만 다른 중복 화면이고, QQuriUnSold에서 중량·육종/농협우량 및 추가된 출하지역·출하주를 함께 표시할 수 있음.
- 변경 파일·내용:
  - `Presentation/Display/SetCustomDisplay.cs`: 춘천·해남진도·곡성과 홍천의 유찰 화면 생성 반환값을 QQuriUnSold로 변경. 해당 사업장 우선 분기는 유지하여 X/능력번호 조건으로 다른 기본 화면에 빠지지 않도록 함.
  - 삭제: `Views/Size128_128/CustomAuctionUnSold/OutLineUnSold.xaml`, `OutLineUnSold.xaml.cs`, `OutLineUnSold_2.xaml`, `OutLineUnSold_2.xaml.cs`.
  - `WORKLOG.md`: 구현·검증 기록.
- 영향 범위: 위 네 사업장의 128×128 소 유찰 화면을 QQuriUnSold로 통일. 홍천도 공통 OrangeRed 테두리를 사용. 기타 특별 유찰 화면, 염소/말 분기, 기본 X 조건은 유지.
- 사용자 변경 보호: QQuriUnSold.xaml은 편집하지 않고 작업 전후 SHA-256 일치 확인. 사용자가 바꾼 장성 분기 등 SetCustomDisplay의 나머지 내용은 보존하고 두 생성식만 변경했음을 사본 비교로 확인.
- 빌드·검증:
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --artifacts-path C:/Users/jyj/AppData/Local/Temp/CowAuction-UnSoldMerge-h_nac40k/artifacts -v:minimal`: 분리 경로 복원·빌드 성공, 경고 0개·오류 0개.
  - `dotnet C:/Users/jyj/AppData/Local/Temp/CowAuction-UnSoldMerge-h_nac40k/artifacts/bin/ReferenceAdoptionChecks/debug/ReferenceAdoptionChecks.dll binding`: **3,390개 통과**. 진행 33개·결과 33개 화면과 공통 비고 호스트 바인딩 검증.
  - 제품·검증 소스의 삭제 타입 참조 없음 확인. 임시 사본과 관련 diff 확인.
- Git 정보: master, HEAD 52c554d. 기존 사용자 변경·미추적 파일 보존, commit hash 미생성, push 미수행.
- 추가 확인: 실제 경매 앱·운영 서버 연결·실제 전광판·Release 빌드·배포 미수행. 사용자 추가 항목의 현장 글자 배치와 긴 출하주 이름 표시 확인은 남아 있음.


## 2026-09-29 — 이력 결과 개체별 즉시 반영 통일

- 목적·승인: 사용자의 “필요한 에이젼트 수 사용 및 수정 진행하자” 요청으로 개체별 즉시 반영 계획 구현. 조회 조정자·화면 반영 담당 2명과 통합·검증 담당 1명, 총 3명으로 진행.
- 문제·원인: 캐시는 준비 후 묶어 반영하고 API는 500ms마다 전체 결과 사전을 게시하여, 동일 결과를 서로 다른 시점 규칙으로 적용함. 인자 없는 화면 콜백은 표시 중인 전체 경매품을 복제·전달함. 일반 DisplayRunning 재진입은 이력만 바뀌어도 페이지 1 선택·경매 진행 제어를 실행할 수 있었음.
- 변경 파일·내용:
  - `Presentation/Display/AnimalHistoryBatchCoordinator.cs` → `AnimalHistoryCoordinator.cs`: 캐시/API 단건 게시, 대상별 요청 식별·취소, 신규 추가 시 기존 작업 유지, 새로고침 병합. 전체 staged 복사·500ms 타이머·마지막 전체 Commit 제거. UI 완료까지 비동기 대기하고 실제 게시 슬롯을 worker 수+1로 제한. 취소된 게시도 UI 실행 전 슬롯을 반환하지 않음. 상태 알림은 최대 1개 대기, 완료 집계는 증분 처리.
  - `Domain/AnimalHistory/HistoryItemUpdate.cs`: 개체번호·캐시 날짜 식별·요청 ID·조회 결과의 단건 전달 계약.
  - `ViewModels/MainWindowViewModel.cs`: `ApplyHistoryResult`와 현재 표시 개체 검증, 해당 슬롯만 복제·갱신, 동일 내용 생략, 비고 숨김과 이름/주소 갱신 분리 유지. 전체 `UpdatePanels`와 대상 재등록 호출 제거. 설정 비활성화 시 조정자 초기화.
  - `Presentation/Display/DisplaySelect.cs`: `UpdateHistoryDisplay`로 기존 화면 모델·LastData만 갱신. 페이지 선택·진행 제어를 다시 호출하지 않아 2·3페이지와 스크롤을 유지.
  - `Validation/ReferenceAdoptionChecks/HistoryBatchChecks.cs`, `HistoryBackgroundChecks.cs`, `FarmAddressChecks.cs`, `HistoryNoteSettingsChecks.cs`: 일괄 반영 전제를 단건 반영 계약에 맞추고 신규/중복/취소/빈 결과/실패/SQLite 저장 실패 검증 보완.
  - `Validation/ReferenceAdoptionChecks/HistoryTestUi.cs`: 단건 UI 적용 완료를 기다리는 검증 도우미.
  - `Validation/ReferenceAdoptionChecks/HistoryItemDisplayChecks.cs`: 실제 WPF View·DisplaySelect를 합성 데이터로 구성하여 해당 슬롯만 변경, 다른 개체 참조 유지, 다중 슬롯, 최신 가격·상태·원래 비고 보존, 숨김 옵션, 진행/대기 2·3페이지·스크롤 보존 검증.
  - `Validation/ReferenceAdoptionChecks/Program.cs`: 신규 화면 검증을 history/all에 연결.
  - `Docs/축산물이력_개체별즉시반영_변경계획_2026-09-29.md`: 구현 결과·검증 범위 기록. `WORKLOG.md`: 완료 기록.
- 영향 범위: 현재 보드의 이력 대상 개체에 대한 조회 결과 전달과 표시 갱신. API 서비스·250ms 요청 설정·운영 XML·SQLite 스키마/보존 정책·경매 목록 폴링·SV 업무 처리는 변경하지 않음. 상태줄 진행률 타입 이름은 기존 이름을 유지.
- 빌드·검증:
  - `%TEMP%/CowAuction-HistoryIndividual-_5sz49ng/artifacts`에 분리 출력. 최초 해당 경로 복원 후 `--no-restore` 사용하여 Visual Studio의 obj/bin과 분리.
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore --artifacts-path C:/Users/jyj/AppData/Local/Temp/CowAuction-HistoryIndividual-_5sz49ng/artifacts -v:minimal`: 최종 경고 0개·오류 0개.
  - 같은 경로의 `ReferenceAdoptionChecks.dll history`: 중간 269개 통과. 페이지 보존 보완 후 `dotnet C:/Users/jyj/AppData/Local/Temp/CowAuction-HistoryIndividual-_5sz49ng/artifacts/bin/ReferenceAdoptionChecks/debug/ReferenceAdoptionChecks.dll all`: 최종 **5,547개 통과**.
  - 첫 UI 검증의 테스트용 리소스 누락과 상태줄 알림의 200ms 지연을 기다리지 않던 검증을 수정한 뒤 재검증. 제품 App·운영 통신은 시작하지 않음.
  - 501건 캐시를 단건 게시하고 추가 1건만 새로 읽어 반영함을 확인. 가짜 UI 대기열 최대 2개 관찰, 대상 20회 교체·UI 정지에서도 결과와 상태 알림 합계 6개 이하 검증. 해당 시간 측정은 테스트의 UI 폴링·운영체제 타이머 대기를 포함하여 실제 렌더링 성능으로 보고하지 않음.
  - 설정 파일 해시, 변경 전 사본과 관련 diff, 이전 기록 보존, 옛 조정자/전체 반영 코드 참조 제거 확인.
- Git 정보: master, HEAD 52c554d. 작업 전 변경·미추적 파일 보존. commit hash 미생성, push 미수행.
- 추가 확인: 실제 전광판의 지속 운용·운영 서버 신규 등록·외부 이력 API·Release 빌드·배포 미수행. 기존 허용량 제한과 빈 이력 재조회 정책은 유지. 소규모 사전 API 실측 결과를 이번 화면 반영 변경의 실측 성능으로 확대하지 않음.


## 2026-09-29 — 이력 요청 간격 설정과 점진적 화면 반영

- 목적·승인: 실측 결과를 바탕으로 제안한 요청 간격 조정·캐시 우선 표시·응답 묶음 반영에 대한 사용자의 “좋아 수정해보자” 요청.
- 문제·원인: API 응답보다 최소 1초 요청 간격이 전체 처리 시간을 지배하고, 배치 전체가 끝나야 화면을 갱신하여 캐시·먼저 완료된 결과도 대기함. 앞선 소규모 실측에서 4건 조회가 1초 간격 약 3.06초, 250ms 간격 약 0.80초였음. 운영 전체 처리 시간 보장은 아님.
- 변경 파일·내용:
  - `Domain/Settings/AnimalTraceSettings.cs`: 누락 설정 기본 1000ms, 최소 250ms, 0·음수의 기본값 복귀 규칙 정의.
  - `Infrastructure/Configuration/UserXmlParser.cs`: AnimalTrace/RequestIntervalMs 파싱. 누락·잘못된 문자열·정수 범위 초과는 1000ms 유지.
  - `Application/AnimalHistory/AnimalHistoryService.cs`: 고정 1000ms 하한 대신 검증한 설정값으로 요청 시작 간격 제어. 동시 요청 기본 1개, 일일 예산·인증/허용량 중단·재시도·취소 경로 유지.
  - `Config/users.XML`: RequestIntervalMs=250 추가. 기존 설정 바이트 보존 검증. 다른 사용자 XML은 변경하지 않아 항목이 없으면 1000ms 적용.
  - `Presentation/Display/AnimalHistoryBatchCoordinator.cs`: 캐시 준비 후 먼저 UI에 게시하고, 새 결과는 500ms마다 누적 스냅샷으로 게시. UI가 밀리면 최신 스냅샷 하나로 합치고 완료 결과는 지연 없이 게시. 부분 반영 중 새로고침 병합 상태 유지, 대상/날짜 변경·초기화·종료 시 이전 결과 차단. 캐시 준비 대기도 세대 취소에 반응하도록 변경.
  - `Presentation/Display/HistoryBatchProgress.cs`: 캐시 우선·조회된 정보부터 반영하는 상태 문구로 변경.
  - `Validation/ReferenceAdoptionChecks/HistoryBackgroundChecks.cs`: XML 경계값, 실제 서비스의 250ms 시작 간격, 캐시 조기 표시, 이름·주소 부분 반영, UI 게시 합치기, 새로고침 중복 방지, 이전 결과 무효화, 빠른 간격의 허용량 중단 검증 추가.
  - `Validation/ReferenceAdoptionChecks/HistoryBatchChecks.cs`: 전체 완료 전 숨김을 전제한 설명을 Dispatcher 게시·결과 합치기 규칙으로 변경.
  - `Validation/ReferenceAdoptionChecks/DisplayLifetimeChecks.cs`: 현재 사용자 소스의 3페이지 대상 사업장에 맞게 6개 테스트 입력 갱신. 사업장 선택 소스 자체는 변경하지 않음.
  - `WORKLOG.md`: 구현·검증 기록.
- 영향 범위: 이력 API 요청 간격과 표시 중 경매품의 이력 비고·소유주/주소 반영 시점. 기존 가격·경매 상태·비고 표시 옵션·API 원문 저장 정책 변경 없음.
- 빌드·검증:
  - Visual Studio obj/bin 경합을 피하도록 `%TEMP%/CowAuction-HistoryFast-82y6_a2q/artifacts`에 분리 출력. 최초 해당 경로 복원 후 `--no-restore` 사용.
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore --artifacts-path C:/Users/jyj/AppData/Local/Temp/CowAuction-HistoryFast-82y6_a2q/artifacts -v:minimal`: 경고 0개·오류 0개.
  - `dotnet C:/Users/jyj/AppData/Local/Temp/CowAuction-HistoryFast-82y6_a2q/artifacts/bin/ReferenceAdoptionChecks/debug/ReferenceAdoptionChecks.dll history`: 233개 통과.
  - 동일 검증 프로그램 `all`: 최종 5,501개 통과. 합성 즉시 응답으로 요청 시작 간격 약 255ms 확인. 운영 통신을 사용하지 않은 검증임.
  - 최초 전체 검증은 현재 3페이지 사업장과 예전 테스트 기대값 차이로 실패. 사용자 소스를 보존하고 테스트만 맞춘 후 전체 재검증 통과.
  - XML 파싱·250ms 값·소스/분리 출력 XML 일치·추가 줄 외 기존 바이트 보존·관련 변경 비교 확인.
- Git 정보: master, HEAD 52c554d. 기존 사용자 변경·미추적 파일 보존. commit hash 미생성, push 미수행.
- 추가 확인: 이번 구현 후 실제 경매 앱·전광판·장시간 API 조회·Release 빌드·배포 미수행. 250ms는 앞선 소규모 실측 기반이며 제공기관의 지속 호출 허용량은 미확인. 필요 시 XML 값을 1000 이상으로 조정 가능. 설정 변경 적용에는 앱 재실행 필요.


## 2026-09-29 — 빌드 경고 36건 정리

- 목적·승인: 사용자의 “경고 36건을 정리 하자” 요청. 첨부 목록의 컴파일러·MVVM 경고를 실제 원인에 맞게 해결하고 기존 동작 검증.
- 문제·원인: null 허용 계약 불일치·설정/응답/보드 누락 경로의 역참조, 사용되지 않는 필드·변수, ObservableProperty 필드 직접 대입, DPI 없는 구형 FormattedText 생성자. 별도 경로 기준 빌드에서 파일·위치·진단별 중복 제거 후 36건 재현.
- 변경 파일·내용:
  - `Infrastructure/Api/AnimalParseData.cs`: 누락 가능한 뿌리농가·낙찰자·중량 표시 설정 처리 및 사용하지 않는 로컬 변수 2곳 제거.
  - `Infrastructure/Api/ServerConn.cs`: 확인된 JArray를 사용하고 null 항목이 포함된 전체 경매 응답은 실패 처리하여 기존 데이터 유지 경로로 전달.
  - `Infrastructure/Configuration/BoardXmlParser.cs`: 로고 보드 목록을 지역 변수로 초기화하고 결과에 연결.
  - `Infrastructure/Netty/netty/AuctionShareNettyClient.cs`: 미사용 필드 2개 제거, 연결 실패 시 null인 반환값·지역 변수의 타입 계약 명시.
  - `Infrastructure/Netty/netty/GlobalDefine.cs`: 선택적 AppSettings 문자열의 null 허용 명시.
  - `Infrastructure/Netty/netty/handlers/AuctionClientInboundDecoder.cs`: 타이머 콜백 state의 null 허용, 읽지 않는 상태 필드와 대입 제거.
  - `Presentation/Converters/Mokpo/MokpoEPD.cs`, `NH_EPD_ShowToVisibilityConverter.cs`: 값·변환 결과의 null 처리와 타입 계약 보완.
  - `Presentation/Display/DisplaySelect.cs`: 타이머 sender의 null 허용 규격 일치.
  - `ViewModels/MainWindowViewModel.cs`: 창 위치·크기를 생성된 속성으로 대입하여 변경 알림 전달, 보드 행 목록을 한 번 확인하고 빈 목록의 크기 계산 방지.
  - `Views/MainWindow.xaml.cs`: 재시작 실행 파일 경로 누락 시 보류, 해제될 수 있는 메모리 타이머의 null 처리.
  - `Views/Common/OutlinedTextBlock.cs`: Visual의 PixelsPerDip을 사용하는 생성자로 변경, DPI 변경 시 글자 측정·도형 캐시 무효화.
  - `Views/Size128_128/Running/RunningNoteHost128.xaml.cs`: 비어 있거나 해제되는 PageContent의 null 허용 계약 명시.
  - `Views/Size128_128/CustomAuctionSold/SangjuSold.xaml.cs`, `YecheonSold.xaml.cs`: 사용하지 않는 애니메이션 필드 제거.
  - `Validation/ReferenceAdoptionChecks/WarningBoundaryChecks.cs`, `UiChecks.cs`: 누락 설정의 API/SV 파싱, 창 초기화 값·알림, 누락/빈 보드, 로고 파싱, Converter 빈 값, 외곽선 글자의 DPI·캐시 갱신 검증 추가.
  - `Validation/ReferenceAdoptionChecks/BindingChecks.cs`: nullable PageContent의 실제 화면 존재 여부를 확인하도록 검증 코드 보완.
  - `Validation/ReferenceAdoptionChecks/BidderChecks.cs`, `DisplayLifetimeChecks.cs`: 작업 전부터 품목 3도 중량 화면을 선택하는 사용자 소스 변경을 보존하고, 이전 기준을 기대하던 테스트만 현재 동작에 맞춤. SetCustomDisplay 소스는 이번 작업에서 변경하지 않음.
  - `WORKLOG.md`: 구현·검증 기록.
- 영향 범위: 위 경고 관련 타입·빈 값 처리·변경 알림·DPI 렌더링. 경고 억제 옵션 추가 없음. 기존 사용자 설정·패키지·경매 화면 선택 정책 유지.
- 빌드·검증:
  - 분리 출력 루트: `%TEMP%/CowAuction-Warnings-ad67e9d0/artifacts`. .NET의 `--artifacts-path`를 사용해 프로젝트별 obj·bin을 Visual Studio 작업 경로와 분리. 최초 빌드에서 해당 경로로 패키지 복원, 이후 `--no-restore` 사용.
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore --artifacts-path C:/Users/jyj/AppData/Local/Temp/CowAuction-Warnings-ad67e9d0/artifacts -v:minimal`: 제품·검증 프로젝트 모두 최종 경고 0개·오류 0개.
  - `dotnet C:/Users/jyj/AppData/Local/Temp/CowAuction-Warnings-ad67e9d0/artifacts/bin/ReferenceAdoptionChecks/debug/ReferenceAdoptionChecks.dll all`: 최종 5,473개 통과. API/SV·통신 상태·이력 캐시·로그·낙찰자·바인딩·화면 수명 주기 및 추가 경계 검증 포함.
  - 최초 전체 검증은 기존 품목 3 화면 선택과 오래된 테스트 기대값 차이로 실패. 사용자 동작을 유지하고 기대값을 수정한 뒤 `stage4` 805개 및 전체 재검증 통과.
- Git 정보: master, HEAD 52c554d. 작업 직전 사본과 관련 diff 확인, 기존 사용자 변경 보존. commit hash 미생성, push 미수행.
- 추가 확인: 운영 서버 연결·실제 전광판/다중 모니터 DPI 이동·메모리 초과 재시작·Release 빌드·배포 미수행. IDE 코드 스타일 제안 87건은 이번 경고 정리 범위에 포함하지 않음.


## 2026-09-29 — 나머지 사용자 XML에 이력 비고 숨김 설정 추가

- 목적·승인: 사용자의 “다른 users.xml” 요청에 따라 다른 사용자 설정 파일에도 ShowOwnerChangeNote=N 적용.
- 문제·원인: 기본 users.XML에만 새 항목이 있어 다른 설정 파일을 사용하면 기본값인 비고 표시가 적용됨.
- 변경 파일·내용: `Config/개users.XML`, `Config/장성_users.XML`의 AnimalTrace에 ShowOwnerChangeNote=N 한 줄 추가. 두 파일의 기존 API 사용 Y와 나머지 바이트 유지. 기존에 삭제된 설정 파일은 복원하지 않음. `WORKLOG.md`에 기록 추가.
- 영향 범위: 현재 존재하는 사용자 XML 3개 모두 이력 조회는 사용하고 소유주 변경 비고는 숨기도록 설정. 기본 users.XML은 이번 작업에서 변경하지 않음.
- 빌드·검증: `dotnet build CowAuctionSmall.csproj --no-restore -v:q -clp:ErrorsOnly` 성공(오류 0개·기존 경고 36개). 세 소스 XML과 Debug 출력 XML의 파싱·API=Y·비고=N·파일 해시 일치 확인. 추가한 줄을 제외한 원본 바이트 보존 검증.
- Git 정보: 기존 사용자 변경·미추적 설정 파일·삭제 상태 보존. commit hash 미생성, push 미수행.
- 추가 확인: 운영 앱 실행·실제 전광판 확인·Release 빌드·배포 미수행. 설정 파일 적용 후 재실행 필요.


## 2026-09-29 — 이력제 API 사용과 소유주 변경 비고 표출 설정 분리

- 목적·승인: 사용자의 IsAnimalProductsTraceability=Y, ShowOwnerChangeNote=N 추가 요청. API·SQLite 캐시·3페이지 정보를 사용하면서 소유주 변경 비고만 숨김.
- 문제·원인: 기존에는 이력 조회가 활성화되면 생성된 소유주 변경 문구를 항상 경매 비고에 추가했다.
- 변경 파일·내용:
  - `Domain/Settings/AnimalTraceSettings.cs`: ShowOwnerChangeNote 속성 추가, 기존 파일 호환을 위해 기본값 true.
  - `Infrastructure/Configuration/UserXmlParser.cs`: AnimalTrace/ShowOwnerChangeNote 파싱. N(공백·대소문자 정규화)이면 숨김, Y·누락 등은 기존 표시 동작 유지. Enabled 판단은 기존 API 사용 설정·서비스 키 조건 유지.
  - `ViewModels/MainWindowViewModel.cs`: 인증 후 런타임 설정 적용. 화면 갱신 시 소유주 변경 비고만 설정에 따라 비우고 HistoryOwner는 항상 조회 결과로 반영. 원본 경매 Note 유지.
  - `Config/users.XML`: AnimalTrace 안에 ShowOwnerChangeNote=N 추가. 기존 API 사용 Y 및 나머지 사용자 설정 유지.
  - `Validation/ReferenceAdoptionChecks/HistoryNoteSettingsChecks.cs`: 합성 XML의 기본값·Y/N, 실제 UpdatePanels와 공통 비고 호스트의 표시 전환, 농가명·주소 및 확정 조회 결과 유지 검증.
  - `Validation/ReferenceAdoptionChecks/FarmAddressChecks.cs`: 위 검증 연결, 비고 숨김 상태에서도 기존 API 조회·캐시 저장/재사용·실패 복구 검증 수행.
  - `WORKLOG.md`: 구현·검증 기록.
- 영향 범위: 공통 화면 갱신 경로에서 이력제 추가 비고만 숨김. 이력 조회·캐시 저장 구조·3페이지·일반 경매 비고·비고 생성 건수 집계 유지. XML 변경은 앱 재실행 후 적용.
- 빌드·검증:
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -v:q -clp:ErrorsOnly`: 제품 포함 빌드 오류 0개. 신규 검증 코드의 nullable 경고 1개 해결(기존 제품 경고 72개), 최종 증분 빌드 오류·경고 0개.
  - `dotnet Validation/ReferenceAdoptionChecks/bin/Debug/net10.0-windows/ReferenceAdoptionChecks.dll farm-address`: 최종 141개 통과. 같은 실행 명령의 `binding`: 3,460개 통과.
  - 소스·Debug 출력 XML 모두 API=Y, 비고=N 확인. 실제 운영 서버 접속 없이 합성 데이터·오프라인 WPF로 검증.
- Git 정보: master, HEAD 52c554d. 기존 사용자 변경 보존, 관련 소스의 작업 전 사본과 diff 확인. commit hash 미생성, push 미수행.
- 추가 확인: 운영 앱 실행·실제 전광판·Release 빌드·배포 미수행. 새 개인정보 저장 항목 없음.


## 2026-09-29 — 128×128 낙찰 화면을 XML 설정·품목 기준으로 통합

- 목적·승인: 사용자가 지정한 X → Standard_non_X_Sold, Y/N → QQuriSold(품목 2는 QQuriSold_Weight) 규칙과 다섯 전용 사업장만 유지하는 방향 반영.
- 문제·원인: 다수 사업장 코드가 일반 낙찰 화면을 강제 선택했고, 호출부는 XML 표시 설정 대신 개체의 뿌리농가 참여값을 전달했다. 낙찰 화면 재사용 키에는 행사용 제목 조건이 누락되어 있었다.
- 변경 파일·내용:
  - `Presentation/Display/SetCustomDisplay.cs`: 장성·양평·예천·해남진도·화순 전용 화면과 기존 횡성 행사용 분양 예외를 유지. 나머지는 IsShowQQuri가 X이면 Standard_non_X_Sold, Y/N이면 품목 2에 QQuriSold_Weight, 그 외 QQuriSold 선택. 기존 염소·말 우선 처리 및 X 이외 미지정 값의 공통 화면 대체 처리 유지. 사용하지 않는 bidderCode·nh_ability_1_num 인자 제거.
  - `Presentation/Display/DisplaySelect.cs`: 이미 읽은 XML IsShowQQuri를 선택 함수에 전달. 화면 재사용 키에 설정값·품목·가격 제목을 반영하여 화면 전환 누락 방지.
  - `Validation/ReferenceAdoptionChecks/BidderChecks.cs`: 변경된 함수 인자·품목별 선택 조건 반영. 기존 QQuriSold의 괄호 번호 표기와 어긋난 기대값을 현재 XAML 표기에 맞춤.
  - `Validation/ReferenceAdoptionChecks/DisplayLifetimeChecks.cs`: 실제 DisplaySold 경로에서 XML과 개체값이 다른 경우, 일반/중량/X 전환, 반복 재사용, 다섯 전용 화면, 행사 진입·해제, 말 화면 확인.
  - `WORKLOG.md`: 구현·검증 기록.
- 영향 범위: 128×128 낙찰 화면 선택과 낙찰 화면 재사용 조건. 일반 화면으로 전환된 사업장은 새 규칙의 기존 공통 XAML을 표시한다. 기존 전용 XAML 파일·운영 XML·진행/유찰 선택 로직은 변경하지 않음.
- 빌드·검증:
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -v:q -clp:ErrorsOnly`: 제품 포함 빌드 오류 0개·기존 경고 72개. 검증 기대값 수정 후 증분 빌드 오류·경고 0개.
  - `dotnet Validation/ReferenceAdoptionChecks/bin/Debug/net10.0-windows/ReferenceAdoptionChecks.dll bidder`: 최종 281개 통과. 최초 실행은 기존 괄호 표기와 테스트 기대값 차이로 실패 후 기대값 수정하여 통과.
  - 같은 실행 명령의 `stage4`: 792개 통과, `binding`: 3,460개 통과. 합성 데이터·오프라인 WPF 사용.
- Git 정보: master, HEAD 52c554d. 작업 직전 사본과 관련 diff 확인, 기존 사용자 변경 보존. commit hash 미생성, push 미수행.
- 추가 확인: 운영 서버 연결·실제 전광판 확인·Release 빌드·배포 미수행. 공통 화면으로 바뀐 사업장의 실제 표시 배치는 수동 확인 필요.


## 2026-09-29 — 사육지의 미등록 특별시 이름 생략 보완

- 목적·승인: 특별시 이름이 남는 원인 확인 후 사용자의 “수정하자” 요청 반영.
- 문제·원인: 첫 지역명이 명시된 목록과 일치할 때만 생략하여, 응답에 포함된 전남광주통합특별시는 표시 문자열에 남았음.
- 변경 파일·내용:
  - `Presentation/Display/AuctionDisplayFormatter.cs`: 기존 지역명·약칭 목록에 더해 첫 단어의 특별시·광역시·특별자치시·특별자치도 접미사를 판별하여 생략. 일반 시·군·구 및 지역명만 있는 응답은 기존 동작 유지.
  - `Validation/ReferenceAdoptionChecks/FarmAddressChecks.cs`: 기존 WPF 표시 검증의 합성 주소를 미등록 특별시 이름으로 변경하여 생략·원문 보존·스크롤 확인.
  - `WORKLOG.md`: 구현 및 검증 기록.
- 영향 범위: 이력 화면의 사육지 표시 문자열. API·캐시 원문 및 운영 설정 변경 없음.
- 빌드·검증: `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -v:q -clp:ErrorsOnly` 성공(오류 0개·기존 경고 72개). `dotnet Validation/ReferenceAdoptionChecks/bin/Debug/net10.0-windows/ReferenceAdoptionChecks.dll farm-address` 121개 통과. 작업 직전 사본과 관련 diff 확인.
- Git 정보: master, HEAD 52c554d. 기존 사용자 변경 보존. commit hash 미생성, push 미수행.
- 추가 확인: 운영 앱 실행·실제 전광판 육안 확인·Release 빌드·배포 미수행.


## 2026-09-29 — 사육지 특별시·광역시 이름 생략

- 목적·승인: 도 이름 생략에 이어 사용자의 “시도 빼자” 요청 반영. 앞서 유지했던 특별시·광역시·특별자치시를 도와 동일한 단계의 생략 대상으로 적용한다고 안내 후 진행.
- 문제·원인: 화면 주소 포맷에서 도만 생략하고 서울·부산 등 최상위 시 이름은 유지하고 있었음.
- 변경 파일·내용: `Presentation/Display/AuctionDisplayFormatter.cs`의 첫 행정구역 생략 대상에 서울·부산·대구·인천·광주·대전·울산·세종의 정식 명칭 및 통용 약칭 추가. 원본 API·캐시·스냅샷 유지. 행정구역 이름만 있는 응답은 유일한 위치 정보 보존.
- 영향 범위: 이력 화면의 사육지 표시 문자열. 도 뒤에 오는 일반 시·군까지 연속 제거하는 처리는 하지 않음.
- 빌드·검증: `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -v:q -clp:ErrorsOnly` 성공(오류 0개·기존 경고 72개). `dotnet Validation/ReferenceAdoptionChecks/bin/Debug/net10.0-windows/ReferenceAdoptionChecks.dll farm-address` 기존 검증 121개 통과. 작업 직전 사본과 diff 확인.
- Git 정보: master, HEAD 52c554d. commit hash 미생성, push 미수행. 기존 사용자 변경 보존.
- 추가 확인: 운영 API 호출·전광판 육안 확인·Release 빌드·배포 미수행.


## 2026-09-29 — 사육지 도 이름 생략 및 이력 상태 문구 명확화

- 목적·승인: 사용자의 도 이름 생략 수정 요청과 “표시 문구 30건”이 50건 처리 결과와 다른 이유 확인 요청 반영.
- 문제·원인: 사육지는 API 원문 전체를 좁은 화면에 표시했다. 상태줄의 Notes는 비어 있지 않은 소유주 변경 비고의 개수인데 “표시 문구”로 안내하여 조회·주소 표시 개수로 오해할 수 있었다. 캐시 50·성공 0은 50건을 캐시로 처리하고 새 API 성공 호출은 없다는 의미이다.
- 변경 파일·내용:
  - `Presentation/Display/AuctionDisplayFormatter.cs`: 알려진 도 이름·특별자치도 이름·도 약칭이 주소 첫 단어이면 화면 문자열에서만 생략. 서울·부산 등 시 이름과 알 수 없는 접두어는 유지. 도 이름만 있는 응답은 유일한 위치 정보를 유지하고 빈 주소는 정보 없음 처리.
  - `ViewModels/AuctionItemDisplayViewModel.cs`: HistoryFarmAddressText에 화면용 주소 포맷 적용. 원본 스냅샷·API·캐시 주소 유지.
  - `Presentation/Display/HistoryBatchProgress.cs`: 완료 문구의 “표시 문구”를 “소유주 변경 비고”로 변경. 집계 기준과 조회 대상 수는 유지.
  - `Validation/ReferenceAdoptionChecks/FarmAddressChecks.cs`: 기존 WPF 검증을 도 이름이 포함된 주소로 갱신하여 표시 생략·원문 보존·긴 주소 스크롤을 함께 확인.
- 영향 범위: 이력 화면의 사육지 표시 문자열과 상태줄 문구. 소유주 변경 비고 생성 조건 및 이름·주소 조회·저장 방식 변경 없음. 비고 생성 건수는 전체 처리 건수나 사육지 표시 건수와 다를 수 있음.
- 빌드·검증:
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -v:q -clp:ErrorsOnly`: 오류 0개·기존 경고 72개.
  - `dotnet Validation/ReferenceAdoptionChecks/bin/Debug/net10.0-windows/ReferenceAdoptionChecks.dll farm-address`: 121개 통과.
  - 같은 명령의 `binding`: 3,460개 통과, 실제 상태줄 바인딩 검증 포함.
- Git 정보: master, HEAD 52c554d. 작업 직전 사본과 관련 diff 확인, 기존 사용자 변경 보존. commit hash 미생성, push 미수행.
- 추가 확인: 실제 운영 API 호출·전광판 확인·Release 빌드·배포 미수행. 화면에 나타난 개별 20건의 비고 미생성 사유는 미확인이며, 비고 개수만으로 주소 누락을 판단하지 않음.


## 2026-09-29 — 이력 조회 화면의 최신 소유주·사육지 표시

- 목적·승인: 마지막 소유주의 farmerNm·farmAddr를 같은 신고 이력에서 가져와 3페이지에 표시하는 제안에 대한 사용자의 “좋아 그렇게 넣어보자” 승인.
- 문제·원인: 이력 API는 optionNo=2로 호출하지만 파서는 날짜·신고구분·소유주만 읽었다. 서비스·배치가 비고 문자열만 전달하고, CowHistorySearchRun은 경매 데이터의 농가명·출하 지역을 표시했다.
- 변경 파일·내용:
  - `Domain/AnimalHistory/AnimalHistoryResult.cs`, `HistoryLookupResult.cs`: 신고별 FarmAddress, 불변 이름·주소 쌍 AnimalHistoryOwner, 조회 결과 LatestOwner 추가. 이력 캐시 내용 버전 2 적용.
  - `Infrastructure/Api/AnimalHistoryParser.cs`: farmAddr 파싱 및 주소까지 포함한 중복 제거. 주소 누락은 빈 값으로 허용하고 기존 필수 필드 검증 유지.
  - `Application/AnimalHistory/AnimalHistoryNoteFormatter.cs`: 가장 최근 신고 날짜의 유일한 기록에서 이름·주소를 함께 선택. 같은 최신 날짜에 여러 기록이 있으면 선택하지 않음. 기존 양수 비고 생성 규칙 유지.
  - `Application/AnimalHistory/AnimalHistoryService.cs`: 메모리·일일 캐시 결과에 이름·주소 전달. 주소가 없는 버전 1 캐시는 재조회 대상으로 처리. 정상 버전 2 캐시·조회 실패 시 기존 정상 쌍 유지, 날짜 변경 시 제거.
  - `Presentation/Display/AnimalHistoryBatchCoordinator.cs`: 비고와 소유주·주소를 하나의 조회 결과로 보관하여 기존 배치 완료 시점에 함께 적용. 기존 비고 전용 호출 호환 유지.
  - `Domain/Auction/gValues.cs`, `Domain/Auction/AuctionItemUpdate.cs`, `Application/Auction/AuctionItemStore.cs`: 표시용 HistoryOwner의 복사·비교·해시 반영. 일반 경매 직렬화와 API/SV 갱신 필드에서 제외. 동일 개체의 부분 갱신에는 보존하고 다른 개체로 교체될 때 해제.
  - `ViewModels/MainWindowViewModel.cs`, `AuctionItemDisplayViewModel.cs`: 확정된 배치의 비고·최신 소유주를 함께 반영. 표시 문자열과 정보 없음 대체 문구 제공, 날짜 변경 시 이전 정보 제거.
  - `Views/Size128_128/Running/CowHistorySearchRun.xaml`: ItemDisplay에 바인딩, 농가명·사육지를 이력제 정보로 표시. 주소 라벨을 사육지로 변경하고 긴 이름·주소는 기존 공통 흐름 컨트롤로 표시. 개체·어미 번호는 경매 스냅샷 사용 유지.
  - `Validation/ReferenceAdoptionChecks/FarmAddressChecks.cs`, `Program.cs`, `SnapshotChecks.cs`, `BindingChecks.cs`: 최신·중복 날짜·주소 누락·캐시 업그레이드·실패 유지·배치 적용·개체 교체·WPF 표시/스크롤·주소만 변경되는 갱신 검증. 공통 바인딩 검증은 Snapshot 경로를 인식하도록 확장.
- 저장 범위·보존: 기존 일일 이력 캐시의 날짜·신고구분·이름에 사육지 주소를 추가. 저장 위치는 `%LOCALAPPDATA%/CowAuctionSmall/Cache/<설치경로 해시>/auction-cache.db`의 animal_history 항목. 기존 날짜 변경 감지(실행 중 점검 또는 다음 실행·접근) 시 삭제 규칙 유지. 원본 XML 응답·운영 로그·일반 경매 캐시에 주소를 추가 저장하지 않음.
- 영향 범위: 이력 API 후처리·캐시·배치 및 이력 조회 화면. API 호출 옵션·요청 제한, 경매 원본 농가명·출하 지역, 운영 XML은 유지. 최신 기록이 모호하면 이름·사육지 모두 정보 없음, 주소만 누락되면 소유주 이름은 표시하고 사육지는 정보 없음.
- 빌드·검증:
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -v:q -clp:ErrorsOnly`: 제품 포함 빌드 오류 0개·기존 경고 72개. 추가 검증 코드의 nullable 경고 1개를 해결한 최종 증분 빌드 오류·경고 0개.
  - `dotnet Validation/ReferenceAdoptionChecks/bin/Debug/net10.0-windows/ReferenceAdoptionChecks.dll farm-address`: 최종 121개 통과(스냅샷 검증 포함).
  - 같은 명령의 `history`: 181개 통과(추가 개체 교체 검증 전 실행), `updates`: 55개, `stage3`: 494개, `binding`: 3,460개 통과.
  - 최초 전체 바인딩 검증은 새 Snapshot 경로를 검사기가 인식하지 못해 실패. 검사 경로를 확장한 뒤 통과. 운영 API 호출 없이 합성 응답·캐시와 오프라인 WPF로 검증.
- Git 정보: master, HEAD 52c554d. 기존 사용자 변경을 보존하고 작업 직전 사본과 관련 diff 확인. commit hash 미생성, push 미수행.
- 추가 확인: 실제 API 응답의 표시 및 전광판 육안 확인·Release 빌드·배포는 미수행. 버전 1 캐시는 다음 조회에서 API를 다시 호출하므로 기존 호출 간격·한도 내에서 조회가 진행된다.


## 2026-09-29 — 추가 진행 페이지 선택을 SetCustomDisplay로 이관

- 목적·승인: 사용자의 CustomAuctionRunning3_128 제안 및 “좋아 그렇게 수정하자” 승인. 사업장 조건을 한 곳에서 관리하고 실제 등록된 페이지 수로 순환한다.
- 문제·원인: CreatePageBySize의 3페이지 선택 조건과 NormalizeRunningPageCount의 허용 조건에 서로 다른 사업장 코드가 들어갈 수 있었다. 화면 선택 함수만 바꾸면 총 페이지 수 제한은 남아 3페이지에 도달하지 못했다.
- 변경 파일·내용:
  - `Presentation/Display/SetCustomDisplay.cs`: CustomAuctionRunning3_128 추가. 사용자가 현재 지정한 사업장에는 CowHistorySearchRun, 다른 사업장에는 null 반환.
  - `Presentation/Display/DisplaySelect.cs`: 3페이지 선택을 위 함수로 위임하고 사업장별 NormalizeRunningPageCount 제거. XML의 생성 요청 수와 실제 등록 수를 분리. 표준 해상도에서 없는 페이지는 null로 제외하고 등록 목록 개수로 순환·동기화용 수와 인덱스 갱신. 로그인 전후에는 원래 설정의 요청 수를 보존하고 화면 등록 전까지 1페이지 기준으로 대기한다. 기존 128×64 생성 순서 유지.
  - `Validation/ReferenceAdoptionChecks/DisplayLifetimeChecks.cs`: 로그인 전후부터 실제 DisplayRunning 호출까지 검증. 대상 사업장 0·1·2·3·4페이지 설정, 비대상 및 이전 대상 사업장의 null 제외, 주요 해상도, 반복 갱신·캐시 재사용, 순환 및 비고 호스트 표시 검증 갱신.
- 영향 범위: 진행 화면의 선택·등록 및 순환 페이지 수. 기존 진행 중 1페이지 고정 규칙과 낙찰·유찰 화면은 유지. 사용자가 변경한 사업장 및 BoardPage=3 XML은 수정하지 않음.
- 빌드·검증:
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -v:q -clp:ErrorsOnly`: 오류 0개·경고 72개.
  - `dotnet Validation/ReferenceAdoptionChecks/bin/Debug/net10.0-windows/ReferenceAdoptionChecks.dll stage4`: 776개 통과.
  - 같은 명령의 `binding`: 3,470개 통과.
  - 앞서 승인된 실행 확인의 연속으로 기본 Debug 실행 파일 실행. 로그인·목록 수신 후 Rotation 로그의 총 페이지 수가 3(page=1/3), timer=on임을 확인하고 검증용 프로세스에 정상 종료 요청. 실제 3페이지 렌더링 육안 확인 및 원격 장치 동기화 검증은 미수행.
- Git 정보: master, HEAD 52c554d. 작업 직전 임시 사본과 대조하여 기존 변경 보존. commit hash 미생성, push 미수행.
- 추가 확인: 실제 전광판에서 이력 화면 내용 확인 필요. Release 빌드·배포 미수행. XML BoardPage는 사용할 페이지 수의 상한으로 유지되므로 2이면 3페이지 생성을 시도하지 않음.


## 2026-09-29 — 로그인 후 사업장별 페이지 수 재계산

- 목적·승인: 앞선 3페이지 수정 후에도 대상 사업장에서 이력 화면이 보이지 않는다는 후속 보고. 기존 변경 목적을 완료하기 위한 오류 수정.
- 문제·원인: MainWindowViewModel은 로그인 전에 사업장 코드를 비워 DisplaySelect를 생성한다. 앞서 추가한 NormalizeRunningPageCount가 이때 3페이지 설정을 일반 사업장 상한인 2로 제한했고, 로그인 후 ApplyAuthenticatedUserInfo는 사업장 코드만 바꾸어 페이지 수가 2로 남았다. 이전 오프라인 검증은 로그인 전후 순서를 포함하지 못했다.
- 변경 파일·내용:
  - `Presentation/Display/DisplaySelect.cs`: 원래 설정값을 읽는 UpdateRunningPageCount를 생성자와 로그인 정보 적용 시점에 공통 호출. 로그인한 사업장·해상도로 페이지 수를 재계산하며 이미 제한된 수를 다시 입력으로 쓰지 않음. 사용자 3페이지 선택 분기와 XML 유지.
  - `Validation/ReferenceAdoptionChecks/DisplayLifetimeChecks.cs`: 빈 사업장 코드로 제한된 초기 상태에서 실제 ApplyAuthenticatedUserInfo를 호출한 뒤 총 페이지 수와 화면 생성·순환 검증. PageSetting 우선순위 및 기존 Auction.BoardPage 설정 방식 모두 확인.
- 영향 범위: 로그인 정보 적용 직후, 최초 경매 목록 조회 전의 진행 페이지 수. 경매 진행 중 1페이지 고정 및 운영 통신 로직 변경 없음.
- 빌드·검증:
  - 회귀 테스트를 먼저 적용하여 기존 코드에서 128×128 대상 사업장 3페이지 검증 실패 재현.
  - 수정 후 `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -v:q -clp:ErrorsOnly`: 오류 0개·경고 72개.
  - `dotnet Validation/ReferenceAdoptionChecks/bin/Debug/net10.0-windows/ReferenceAdoptionChecks.dll stage4`: 723개 통과. 로그인 적용부터 페이지 선택·순환까지 합성 데이터 검증이며 운영 연결·타이머 시작 없음.
- Git 정보: master, HEAD 52c554d. 작업 직전 사본과 대조하여 기존 사용자 변경 보존. commit hash 미생성, push 미수행.
- 추가 확인: 재시작·로그인 후 실제 전광판 3페이지 표시 및 장치 간 동기화 확인 필요. 운영 앱 실행·Release 빌드·배포 미수행.


## 2026-09-29 — 128×128 사업장별 3페이지 생성 경로 수정

- 목적·승인: 3페이지 설정 시 128×64 화면이 나오는 원인 확인 후 사용자의 “변경해줘” 승인 반영.
- 문제·원인: InitializePages가 표준 해상도이면서 총 페이지 수가 2 이하일 때만 CreatePageBySize를 호출했다. 3페이지 설정은 128×64 기본 생성 분기로 들어가 사용자가 추가한 이력 화면 분기에 도달하지 못했다.
- 변경 파일·내용:
  - `Presentation/Display/DisplaySelect.cs`: 표준 해상도는 페이지 수와 무관하게 CreatePageBySize에 현재 번호를 전달. NormalizeRunningPageCount로 대상 사업장 128×128은 최대 3, 다른 표준 해상도는 최대 2, 기존 128×64는 최대 4페이지로 제한. 생성자에서 순환·동기화용 총 페이지 수를 정규화하고 페이지 생성에도 동일한 제한 적용. 사용자가 추가한 CowHistorySearchRun 선택 및 XML 설정은 그대로 보존.
  - `Validation/ReferenceAdoptionChecks/DisplayLifetimeChecks.cs`: 생성자를 우회한 오프라인 페이지 검증 추가. 대상 사업장의 1·3·초과 페이지 설정, 다른 사업장 및 160×64·320×64 제한, 128×64 일반·특수 순서, 모델·크기 연결, 캐시 재사용, 순환 및 공통 비고 호스트 내 페이지 교체 확인.
- 영향 범위: 진행 페이지 생성·총 페이지 수. 기존 경매 진행 중 1페이지 고정 규칙과 낙찰·유찰 화면 선택은 유지. 실제 운영 설정·연결 정보 변경 없음.
- 빌드·검증:
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -v:q -clp:ErrorsOnly`: 최초 오류 0개·경고 72개, 검증 코드 조정 후 증분 빌드 오류·경고 0개.
  - `dotnet Validation/ReferenceAdoptionChecks/bin/Debug/net10.0-windows/ReferenceAdoptionChecks.dll stage4`: 707개 통과(페이지 관련 검증 111개 추가).
  - 같은 명령의 `binding`: 3,470개 통과, 진행 33개·결과 35개 화면 포함.
  - 최초 테스트의 네임스페이스 기반 단정은 기존 AuctionRunning2의 네임스페이스가 폴더와 다른 점 때문에 실패하여 실제 화면 타입 검증으로 수정. 제품 코드 실패가 아니며 재검증 통과.
- Git 정보: master, HEAD 52c554d. 기존 미추적 소스 및 사용자 변경을 작업 직전 사본과 대조. commit hash 미생성, push 미수행.
- 추가 확인: 운영 앱·전광판 육안 확인, 실제 장치 간 페이지 동기화, Release 빌드 및 배포 미수행. 향후 다른 사업장에 페이지를 추가할 때 생성 분기와 NormalizeRunningPageCount의 지원 수를 함께 갱신해야 함.


## 2026-09-29 - 128×128 축협 낙찰자 이름 흐름 표시 통일

### 작업 일자

- 2026-09-29

### 작업 목적

- 실제 사용하는 128×128 소 경매 낙찰 화면 전체에서 낙찰자 이름 5자는 고정 표시하고 6자 이상은 한 줄로 흐르게 한다.
- Y 모드에서도 파싱 과정에서 잘리지 않은 전체 이름을 흐름 표시에 사용한다.

### 원인

- 공용 `BidderNameFlow`는 이미 5자 고정·6자 이상 흐름 조건을 지원하지만 일부 낙찰 View에만 적용돼 있었다.
- `BidderName=Y` 파싱은 대부분 이름을 3자로, 해남진도·양평은 5자로 줄여 `Bidder`에 저장하므로 흐름 컨트롤이 있어도 6자 이상 이름을 받을 수 없었다.
- 전역 `Bidder` 잘림 규칙을 제거하면 128×64·160×64 화면에도 영향을 주므로 128×128 흐름 표시용 전체 이름을 `BidderString`에 별도로 보존할 필요가 있었다.

### 변경 파일 및 내용

- `CowAuctionSmall/Services/AnimalParseData.cs`: Y 모드의 URL 디코딩된 전체 낙찰자 이름을 `BidderString`에 저장하고, 유효하지 않은 낙찰 데이터에서는 이전 이름이 남지 않도록 `BidderString`을 `-`로 초기화했다. 기존 `Bidder` 잘림 규칙은 유지했다.
- 기존 흐름 View 6개(`AnseongSold`, `JangseongSold`, `JecheonDanyangSold`, `QQuriSold`, `QQuriSold_Weight`, `Standard_non_X_Sold`): Y 모드 `BidderNameFlow` 입력을 `Bidder`에서 `BidderString`으로 변경했다.
- 단일 낙찰자 표시 View 4개(`YangpyeongSold`, `YecheonSold`, `HaenamJindoSold`, `MokpoMuanSinanSold`): N/X 모드는 기존 `Bidder` 고정 표시를 유지하고 Y/B 모드는 전체 이름을 `BidderNameFlow`로 표시하도록 변경했다.
- 이름+참가번호 View 4개(`QQuriSold_v3`, `HoengseongBunyangSold`, `HoengseongSold`, `HwasunSold`): 참가번호 줄은 유지하고 이름 줄만 기존 위치·색상·크기에 맞춘 `BidderNameFlow`로 변경했다.
- 공용 `BidderNameFlow`와 `FlowTextAnimation`은 변경하지 않았다.
- `WORKLOG.md`: 낙찰자 전체 이름 보존, 적용 View와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 실제 화면 선택 분기에서 사용하는 128×128 소 경매 낙찰 View 14개에 적용된다.
- Y와 B 모드의 이름은 5자까지 고정, 6자 이상은 초당 18px로 반복 흐름 표시된다.
- N 모드 참가번호와 X 모드 마스킹 이름의 기존 고정 표시는 유지된다.
- `Bidder` 값과 기존 잘림 규칙을 유지하므로 128×64·160×64 낙찰 화면에는 영향이 없다.
- 진행·유찰 화면, 낙찰가, 개체번호, 화면 선택 분기와 공용 애니메이션 수명 주기는 변경하지 않았다.

### 빌드 및 테스트 결과

- 실제 사용하는 128×128 소 경매 낙찰 View 14개 모두 `BidderNameFlow` 포함 여부 확인: 정상.
- Y 모드 흐름 컨트롤에 기존 잘린 `Bidder` 바인딩이 남아 있지 않은지 확인: 정상.
- Y 모드 전체 이름 저장 1개 및 무효 데이터 이름 초기화 2개 분기 확인: 정상.
- 공용 기준 `Text.Length <= 5` 고정 및 6자 이상 강제 흐름 로직 유지 확인: 정상.
- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore --nologo` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- `git diff --check`: 공백 오류 없음. 기존 파일의 LF→CRLF 변환 예정 경고만 확인했다.
- 실제 전광판에서 Y/B/N 모드별 5자·6자 이름, 참가번호 위치 및 반복 화면 진입 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 128x128 낙찰자 이름 흐름 표시 통일`


## 2026-09-29 - 128×128 축협 화면 개체번호 구간별 표시 일괄 적용

### 작업 일자

- 2026-09-29

### 작업 목적

- 실제 화면 선택 분기에서 사용하는 128×128 소 경매 View 전체에 `4+4+1` 개체번호 구간별 표시 방식을 적용한다.
- 전체번호 위에 단축번호를 덮는 구조를 제거하고 가운데 네 자리만 `users.xml`의 단축번호 전경·배경색으로 표시한다.

### 원인

- 축협별 진행·낙찰·유찰 View 대부분이 전체 개체번호와 단축번호를 별도 `TextBlock`으로 겹쳐 표시해 글꼴과 좌표에 따라 색상 경계 및 간격이 달라질 수 있었다.
- 정읍과 상주 화면에서 하나의 `TextBlock`과 여러 `Run`을 사용하는 방식의 실제 표출 및 `users.xml` 색상 연동이 정상임을 확인했다.
- `SetCustomDisplay`의 128×128 화면 선택 분기를 기준으로 개체번호를 표시하는 실제 사용 View 36개 중 기존 적용 6개를 제외한 30개가 기존 오버레이 방식으로 남아 있었다.

### 변경 파일 및 내용

- 진행 1페이지 11개: `Standard_non_QQuri_Run1_1`, `Standard_non_X_Run1`, `StandardQQuri_Run1`, `GimjeRun`, `HaenamJindo`, `MokpoMuanSinan`, `YangpyeongRun`, `YecheonRun`, `HoengseongRun`, `Hwasun`, `Jangseong` XAML에 구간별 표시를 적용했다.
- 낙찰 12개: `Standard_non_X_Sold`, `QQuriSold_Weight`, `JecheonDanyangSold`, `JangseongSold`, `YangpyeongSold`, `YecheonSold`, `HaenamJindoSold`, `MokpoMuanSinanSold`, `QQuriSold_v3`, `HoengseongBunyangSold`, `HoengseongSold`, `HwasunSold` XAML에 구간별 표시를 적용했다.
- 유찰 7개: `OutLineUnSold`, `HoengseongUnSold`, `YecheonUnSold`, `Standard_non_X_UnSold`, `HwasunUnSold`, `YangpyeongUnSold`, `JangseongUnSold` XAML에 구간별 표시를 적용했다.
- 각 View에 기존 `EntityNumberPartConverter`를 리소스로 등록하고, 앞 네 자리·단축번호 네 자리·마지막 한 자리를 하나의 `TextBlock`에서 표시하도록 변경했다.
- 가운데 네 자리는 `EntityNumberShortForeground`와 `EntityNumberShortBackground`, 나머지는 `EntityNumberForeground`를 사용한다.
- 명시적으로 `굴림체`를 사용하던 승인 대상의 축종·혈통·개체번호는 `굴림`으로 변경했다. 각 View의 기존 FontSize와 시작 좌표 및 표시 조건은 유지했다.
- 횡성·화순 낙찰/유찰 View 내부의 염소용 단축번호 표시는 변경하지 않았다.
- `WORKLOG.md`: 실제 사용 View 전수 적용 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 128×128 소 경매의 실제 사용 진행 1페이지 13개, 낙찰 14개, 유찰 9개가 모두 동일한 `4+4+1` 표시 구조를 사용한다.
- 진행 2페이지, 염소·말 화면, 현재 화면 선택 분기에서 생성되지 않는 보관 View, 데이터 파싱, ViewModel 및 화면 선택 로직은 변경하지 않았다.
- 해남진도 진행 화면은 기존에 전체번호만 표시했으나 이번 적용으로 가운데 네 자리의 설정 색상·배경도 표시한다.
- 횡성 진행 화면의 고정 `Red/Black` 단축번호 색상은 다른 축협과 동일하게 `users.xml` 동적 색상으로 변경했다.

### 빌드 및 테스트 결과

- 승인 대상 30개 XAML 각각에 `EntityNumberPartConverter` 리소스 1개, `First`/`Last` 구간 바인딩 각 1개가 존재하는지 확인: 정상.
- 승인 대상 30개 XAML의 명시적 `굴림체` 잔존 여부 확인: 없음.
- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore --nologo` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- `git diff --check`: 공백 오류 없음. 기존 파일의 LF→CRLF 변환 예정 경고만 확인했다.
- 실제 축협별 진행·낙찰·유찰 전광판 표출 확인: 미수행.
- 해남진도 진행 및 횡성 진행의 변경된 단축번호 색상 연동 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 128x128 축협 화면 개체번호 표시 통일`


## 2026-09-29 - 상주 진행·낙찰·유찰 개체번호 구간별 색상 표시

### 작업 일자

- 2026-09-29

### 작업 목적

- 상주축협의 진행·낙찰·유찰 화면에서도 정읍축협과 동일하게 `4+4+1` 개체번호의 가운데 네 자리를 겹침 없이 설정 색상과 배경으로 표시한다.

### 원인

- 상주축협 코드 `8808990657639`와 `IsShowQQuri=N` 조건은 진행 1페이지 `Standard_non_QQuri_Run1`, 낙찰 `AnseongSold`, 유찰 `QQuriUnSold`를 사용한다.
- 세 화면 모두 전체 개체번호와 네 자리 단축번호를 서로 다른 `TextBlock`으로 겹쳐 표시해 글꼴 렌더링과 좌표에 따라 색상 경계와 번호 간격이 달라질 수 있었다.
- 진행 2페이지 `AuctionRunning2`와 `AuctionRunning2_3`은 개체번호를 표시하지 않아 변경 대상에서 제외했다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/Standard_non_QQuri_Run1.xaml`: 공용 `IsShowQQuri=N` 진행 화면의 오버레이를 제거하고 하나의 `TextBlock` 안에서 `4+4+1` 구간을 표시하도록 변경했다.
- `CowAuctionSmall/Views/Size128_128/CustomAuctionSold/AnseongSold.xaml`: 상주를 포함한 공용 낙찰 화면의 개체번호를 동일한 구간별 표시 방식으로 변경했다.
- `CowAuctionSmall/Views/Size128_128/QQuriUnSold.xaml`: 상주를 포함한 공용 유찰 화면의 개체번호를 동일한 구간별 표시 방식으로 변경했다.
- 세 화면 모두 기존 `EntityNumberPartConverter`를 재사용하고 가운데 네 자리는 `EntityNumberShortForeground`와 `EntityNumberShortBackground`, 나머지는 `EntityNumberForeground`를 사용한다.
- `CowAuctionSmall/Views/Size128_128/Running/Standard_non_QQuri_Run1.xaml`: 실제 표출 비교를 위해 진행 1행의 축종·혈통·개체번호 글꼴을 `굴림체 11px`에서 `굴림 11px`로 변경하고 기존 좌표를 유지했다.
- `CowAuctionSmall/Views/Size128_128/CustomAuctionSold/AnseongSold.xaml`: 낙찰 화면에서 축종과 개체번호 사이의 간격을 줄이고 마지막 숫자의 우측 여유를 확보하도록 개체번호 Margin.Left를 `6`에서 `0`으로 변경했다.
- `WORKLOG.md`: 상주 화면 선택 경로, 변경 내용, 공용 영향 범위와 검증 결과를 기록하고 정읍 현장 확인 결과를 갱신했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 상주축협 128×128 진행 1페이지, 낙찰 및 유찰 화면에 적용된다.
- `Standard_non_QQuri_Run1`을 사용하는 다른 `IsShowQQuri=N` 사업장, `AnseongSold`를 공유하는 사업장 및 `QQuriUnSold`를 사용하는 다른 사업장에도 동일한 표시 방식이 적용된다.
- 진행 2페이지, 염소용 개체번호, 데이터 파싱, ViewModel 및 화면 선택 분기는 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore --nologo` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 기존 Converter와 상주 진행·낙찰·유찰 XAML 리소스의 컴파일: 성공.
- 실제 상주축협 128×128 전광판에서 `4+4+1` 구간별 색상·배경·간격 정상 표출: 사용자 확인 완료.
- 상주 진행 1행의 `굴림 11px` 표출 및 마지막 한 자리 비잘림: 사용자 확인 완료.
- 상주 `AnseongSold` 낙찰 화면에서 축종과 개체번호 간격 및 마지막 한 자리 비잘림 확인: 미수행.
- 공용 View를 사용하는 다른 축협 화면 회귀 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 상주 화면 개체번호 구간별 색상 표시`


## 2026-09-29 - 정읍 진행·낙찰·유찰 개체번호 구간별 색상 표시

### 작업 일자

- 2026-09-29

### 작업 목적

- `4+4+1` 형식의 개체번호 위에 단축번호를 겹쳐 표시하지 않고, 하나의 텍스트에서 가운데 네 자리만 설정 색상과 배경으로 표시한다.

### 원인

- 기존 화면은 전체 개체번호와 네 자리 단축번호를 서로 다른 `TextBlock`으로 같은 위치에 겹쳐 표시해 글꼴 렌더링과 좌표에 따라 색상 경계와 번호 간격을 반복해서 보정해야 했다.
- 전체 개체번호는 이미 `4 4 1` 형식이고 가운데 네 자리 값도 `EntityNumberShort`로 제공되므로, 앞·가운데·마지막 구간을 한 줄에서 직접 표시할 수 있다.

### 변경 파일 및 내용

- `CowAuctionSmall/Models/Converter/EntityNumberPartConverter.cs`: 전체 개체번호에서 공백을 제거한 뒤 앞 네 자리와 마지막 한 자리를 안전하게 반환하는 Converter를 추가했다.
- `CowAuctionSmall/Views/Size128_128/Running/Standard_non_X_Run1_1.xaml`: 정읍 진행 화면의 전체번호·단축번호 오버레이를 제거하고 하나의 `TextBlock` 안에서 `4+4+1` 구간을 표시하도록 변경했다.
- `CowAuctionSmall/Views/Size128_128/QQuriSold.xaml`: 공용 낙찰 화면의 개체번호를 동일한 구간별 표시 방식으로 변경했다.
- `CowAuctionSmall/Views/Size128_128/CustomAuctionUnSold/NamwonUnSold.xaml`: 정읍·목무신 공용 유찰 화면의 개체번호를 동일한 구간별 표시 방식으로 변경했다.
- 가운데 네 자리는 기존 `EntityNumberShortForeground`와 `EntityNumberShortBackground`를 사용하고 앞 네 자리와 마지막 한 자리는 기존 `EntityNumberForeground`를 사용한다.
- `WORKLOG.md`: 원인, 변경 내용, 공용 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 정읍축협 사업장 코드 `8808990656953`의 128×128 진행 1페이지, 낙찰 및 유찰 화면에 적용된다.
- `QQuriSold`를 사용하는 전용 낙찰 화면이 없는 다른 축협과 `NamwonUnSold`를 공유하는 목무신 유찰 화면에도 동일한 표시 방식이 적용된다.
- 개체번호가 없는 `Eumseong2` 유전 화면, 염소용 개체번호, 데이터 파싱, ViewModel 및 화면 선택 분기는 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore --nologo` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- Converter와 진행·낙찰·유찰 XAML 리소스의 컴파일: 성공.
- 실제 정읍축협 128×128 전광판에서 `2198 6792 8`의 구간별 색상·배경·간격 및 `users.xml` 색상 연동 정상: 사용자 확인 완료.
- 공용 `QQuriSold` 사용 축협과 목무신 유찰 화면 회귀 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 개체번호 가운데 네 자리 직접 색상 표시`


## 2026-09-29 - 정읍축협 진행 화면 개체번호 위치 보정

### 작업 일자

- 2026-09-29

### 작업 목적

- 정읍축협 128×128 진행 1페이지의 전체 개체번호와 가운데 네 자리 단축번호의 상대 간격을 구형 화면과 일치시킨다.

### 원인

- 최근 진행 화면 보정으로 전체 개체번호와 단축번호가 각각 X=44, X=74에 배치되어 두 항목의 상대 간격이 30px이었다.
- 두 항목을 함께 왼쪽으로 이동한 1차 수정은 상대 간격 30px을 그대로 유지해 화면 표시가 달라지지 않았다.
- 구형 정읍 전용 화면은 전체번호 X=44, 단축번호 X=72로 상대 간격 28px을 사용했으며, 개체번호 파싱과 바인딩 값은 정상이었다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/Standard_non_X_Run1_1.xaml`: 전체 개체번호 Margin.Left `44`를 유지하고 단축번호 Margin.Left를 `74`에서 `72`로 변경해 구형 화면의 상대 간격 28px을 복원했다.
- 단축번호 Width `26`, 왼쪽 정렬, ZIndex, 동적 전경·배경과 데이터 바인딩은 유지했다.
- `WORKLOG.md`: 원인, 변경 내용, 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 정읍축협 사업장 코드 `8808990656953`의 128×128 진행 1페이지에 적용된다.
- 진행 2페이지 `Eumseong2`, 낙찰 `QQuriSold`, 유찰 `NamwonUnSold`, 화면 선택 분기 및 개체번호 파싱은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 전체 개체번호 X=44, 단축번호 X=72 및 기존 Width·스타일·바인딩 유지 여부: 코드 확인 완료.
- 실제 정읍축협 128×128 전광판에서 `2198 6792 8`의 전체번호·단축번호 위치 정상 표출: 사용자 확인 완료.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 정읍 진행 화면 개체번호 위치 보정`


## 2026-09-28 — B 낙찰 참가번호 가운데 정렬

- 목적·승인: 사용자의 “숫자만 가운데 정렬 이걸로 진행 수정하자” 요청 반영.
- 문제·원인: 이름+번호 화면의 참가번호에도 왼쪽 정렬이 적용되어 한쪽으로 치우쳐 표시됨.
- 변경 파일·내용: `Views/Size128_128/QQuriSold.xaml`의 B 참가번호 TextAlignment를 Left에서 Center로 변경. 이름의 정렬·스크롤은 유지하고 번호는 표시 영역 가운데에 고정한다.
- 영향 범위: 통합 QQuriSold의 B 모드 참가번호 한 곳. 이름 길이에 따른 위치 계산이나 Margin 추가 없음. 기존 사용자 변경 보존.
- 빌드·검증: `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -v:q -clp:ErrorsOnly` 성공(오류 0개·경고 72개). `dotnet Validation/ReferenceAdoptionChecks/bin/Debug/net10.0-windows/ReferenceAdoptionChecks.dll bidder` 기존 오프라인 검증 281개 통과.
- Git 정보: master, HEAD 52c554d. commit hash 미생성, push 미수행.
- 추가 확인: 실제 운영 앱·전광판의 육안 확인 및 배포 미수행.


## 2026-09-28 — QQuriSold 낙찰 화면 통합

- 목적·승인: 사용자의 “좋아 통합하자” 승인에 따라 QQuriSold_v2·QQuriSold_v3를 QQuriSold로 통합한다.
- 문제·원인: 사용하지 않는 v2와 일반 B·곡성에서 사용하는 v3가 별도 화면으로 남아 표시 및 스크롤 수정 지점이 분산되어 있었다.
- 변경 파일·내용:
  - `Presentation/Display/SetCustomDisplay.cs`: 곡성은 QQuriSold를 선택하고, 일반 B 설정을 v3로 바꾸던 분기 제거. 이미 앞에서 처리되는 장성의 중복 조건 제거.
  - `Views/Size128_128/QQuriSold.xaml`: 기존 배치·색상·이름 스크롤 유지. X 모드도 파서가 제공한 마스킹 이름을 표시하도록 조건 추가. 이름 흐름 설명을 실제 표시 폭 기준으로 정정.
  - `Views/Size128_128/CustomAuctionSold/QQuriSold_v2.xaml`, `QQuriSold_v2.xaml.cs`, `QQuriSold_v3.xaml`, `QQuriSold_v3.xaml.cs`: 삭제.
  - `Validation/ReferenceAdoptionChecks/BidderChecks.cs`: 통합 화면 대상으로 기존 이름 스크롤·번호 고정·폭 변경·빈 값·Unload/Reload 검증 갱신. 일반·곡성의 Y/N/B/X 선택, 전용 화면 선택 유지, 모드 전환 표시 검증 추가.
- 영향 범위: 일반 B 및 곡성의 화면은 QQuriSold 배치·색상으로 통일하며, B 참가번호도 QQuriSold의 괄호 없는 형태를 사용한다. 장성·임실·염소·뿌리농가 X 전용 화면 및 운영 XML은 유지. 기존 사용자 변경은 작업 직전 임시 사본과 대조하여 보존.
- 빌드·검증:
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -v:q -clp:ErrorsOnly`: 기본 출력 경로 빌드 성공, 오류 0개·경고 72개.
  - `dotnet Validation/ReferenceAdoptionChecks/bin/Debug/net10.0-windows/ReferenceAdoptionChecks.dll bidder`: 281개 통과.
  - 같은 명령의 `binding`: 3,470개 통과(진행 33개·결과 35개 화면). `stage4`: 596개 통과.
  - 운영 앱·서버 접속 없이 합성 데이터로 WPF 검증. 과거 작업 기록을 제외한 v2·v3 참조 없음. 전체 diff 검사에서 기존 무관 파일의 공백 경고는 유지.
- Git 정보: master, HEAD 52c554d. 작업 전 상태 213개 항목. commit hash 미생성, push 미수행.
- 추가 확인: 실제 전광판에서 통합 배치·색상과 긴 이름 흐름 육안 확인 필요. 운영 실행·Release 빌드·배포 미수행.


## 2026-09-28 — B 낙찰 화면의 이름 스크롤 적용

- 목적·승인: 이름+번호 설정에서 이름이 흐르지 않는다는 보고 후 사용자의 “수정하자” 승인. QQuriSold_v3의 이름은 표시 폭 초과 시 흐르고 참가번호는 고정 표시한다.
- 문제·원인: 일반 조합에서 BidderName=B이면 선택되는 QQuriSold_v3는 이름을 일반 TextBlock/Run으로 표시했다. 앞서 개선한 공통 BidderNameFlow가 적용되지 않았으며, 가운데 정렬된 부모는 이름의 유효 표시 폭도 명시적으로 제한하지 않았다.
- 변경 파일·내용:
  - `Views/Size128_128/CustomAuctionSold/QQuriSold_v3.xaml`: 이름·번호 StackPanel을 해당 Grid 영역 안에 Stretch 배치. 이름을 BidderNameFlow로 교체하고 기존 WhiteSmoke·Bold를 유지, 폰트 12·높이 17 적용. 참가번호는 아래 줄에 고정·중앙 정렬. 기존 B 표시 조건과 가격·중량·개체번호 영역 유지.
  - `Validation/ReferenceAdoptionChecks/BidderChecks.cs`: 실제 SetCustomDisplay의 B 선택으로 생성한 화면을 WPF에 올려 이름 넘침·짧은 이름·참가번호 고정·가로 폭 변경·새 낙찰자·빈 이름·Unload/Reload를 검증. 여러 Run으로 구성된 참가번호는 TextRange로 읽는다.
- 영향 범위: QQuriSold_v3를 사용하는 낙찰 화면. 공통 컨트롤·파서·화면 선택 규칙·운영 XML 및 다른 전용 낙찰 화면은 변경하지 않음.
- 빌드·검증:
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -v:q -clp:ErrorsOnly`: 기본 bin/obj 경로에서 오류 0개·경고 72개. 최종 검증 코드 수정 후 증분 빌드는 오류·경고 0개.
  - `dotnet Validation/ReferenceAdoptionChecks/bin/Debug/net10.0-windows/ReferenceAdoptionChecks.dll bidder`: **263개** 통과, 실제 B 낙찰 화면 검증 10개 추가.
  - 같은 출력의 `binding`: **3,524개**, `stage4`: **596개** 통과. 모두 합성 데이터·오프라인 WPF이며 운영 앱 생성자·서버 연결 없음.
- Git 정보: `master`, HEAD `52c554d`, 시작 상태 213개 항목. 관련 파일의 기존 변경을 임시 사본으로 보존하고 이번 변경만 대조. commit hash `미생성`, push `미수행`.
- 추가 확인: 실제 전광판·운영 앱 실행·Release 빌드·배포는 미수행. 다른 전용 화면에서 사용하는 일반 TextBlock까지 일괄 교체하지 않았음.


## 2026-09-28 — 가져온 이력 조회 화면의 XAML·바인딩 정리

- 목적·승인: CowHistorySearchRun 확인 결과에 대한 사용자의 “좋아 변경하자” 승인. 주소는 “현재 출하 지역을 표시”, 화면 연결 위치는 “아직 미정”이라는 후속 답변을 반영한다.
- 문제·원인: 가져온 XAML의 UserControl 시작 태그 중간에 종료 태그가 들어갔고, 옛 Converter 네임스페이스·삭제된 Converter 및 미사용 리소스를 참조했다. 주소에 최저가를 표시하고 개체·어미 번호 앞에 002를 고정으로 붙였다.
- 변경 파일·내용:
  - `Views/Size128_128/Running/CowHistorySearchRun.xaml`: 루트 태그 복구, Converter 및 미사용 등급 스타일 제거. 루트는 기존 진행 ViewModel을 유지하고 내부 Grid에서 CowInfo를 사용한다. 농가명 OwnerName·주소 Location·개체 EntityNumber·어미 MatherEntityNumber를 단방향으로 연결. 지역의 동적 색상, 기본 레이아웃·안내 문구·테두리 유지. 파서가 제공하는 표시용 번호를 사용하고 고정 002 접두사는 제거.
  - `Validation/ReferenceAdoptionChecks/README.md`: 이전 화면 리소스 오류 해결 및 최신 전체 바인딩 검증 결과 기록.
- 영향 범위: 해당 조회 화면과 검증 안내 문서. 코드 비하인드·공통 파서·모델·설정 XML·화면 선택 및 페이지 순서는 변경하지 않음. 주소는 전체 상세 주소가 아니라 현재 데이터에 있는 출하 지역이다. 개체·어미 번호는 기존 표시용 형식을 사용한다.
- 빌드·검증:
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -p:OutputPath=<임시 폴더>/Debug -p:IntermediateOutputPath=<임시 폴더>/Intermediate/ -v:q -clp:ErrorsOnly`: 오류 0개·경고 72개.
  - 해당 출력의 `dotnet ReferenceAdoptionChecks.dll binding`: **3,524개** 통과. 진행 33개·결과 37개·공통 호스트의 실제 WPF 바인딩, 데이터 교체·재로드 포함.
  - 같은 출력의 `stage4`: **596개** 통과.
  - XML 문법·주소/농가명 연결·불필요한 가격 및 접두사 참조 제거·기존 코드 비하인드 보존과 변경 diff 확인.
- Git 정보: `master`, HEAD `52c554d`, 시작 상태 213개 항목. 미추적 원본도 임시 사본으로 보존하고 이번 변경을 기준 사본과 비교함. commit hash `미생성`, push `미수행`.
- 추가 확인: 페이지 연결 위치는 사용자 미정으로 유지. 운영 앱·서버 연결·실제 전광판·Release 빌드·배포는 미수행.


## 2026-09-28 — 비고 호스트 통합과 스크롤 위치 보존

- 목적·승인: 비고 호스트 통합 계획에 대한 사용자의 “좋아 진행하자” 승인. 대기·진행·낙찰·유찰의 비고 표시를 한 호스트로 통일한다.
- 문제·원인: 두 호스트가 같은 배지·비고 레이아웃과 애니메이션 관리 코드를 복제하고 DataContext 접근 및 위치 저장 경로만 다르게 사용했다. 추가 WPF 검증에서 색상 Run 재구성의 중간 텍스트 변경이 스크롤을 재시작하고, Begin 직후 동기 Seek로 저장 위치가 실제 표시 클록에 유지되지 않는 문제도 확인했다.
- 변경 파일·내용:
  - `Views/Size128_128/Running/RunningNoteHost128.xaml`·`.xaml.cs`: ItemDisplay·ScrollOwner 입력, 비고 영역의 공통 스냅샷 바인딩, 개체 식별값 비교·예약 초기화·해제. 본문 DataContext는 유지.
  - `RunningNoteHost128_Running.xaml`·`.xaml.cs` 삭제: 모든 실행·검증 참조를 공통 호스트로 이관.
  - `Presentation/Display/DisplaySelect.cs`: 진행 ItemDisplay 단방향 연결, 낙찰·유찰 최초 및 동일 상태 갱신의 표시 객체 공유. 기존 적용 조건·제외 페이지 유지.
  - `Presentation/Display/DisplayPanelStore.cs`: 호스트 필드 단일화, ItemDisplay 바인딩·값·ScrollOwner·본문 참조 해제.
  - `Presentation/Animation/FlowTextAnimation.cs`: 선택적 텍스트 변경 합치기를 공통 호스트에 적용해 최종 문구가 같으면 클록 유지. 예약 작업 해제. Begin과 위치 이동을 다음 틱에 함께 적용하는 Seek로 복귀 위치 수정. 다른 호출부는 기존 텍스트 처리 기본값 유지.
  - `Validation/ReferenceAdoptionChecks/BindingChecks.cs`, `DisplayLifetimeChecks.cs`, `UiChecks.cs`, `Program.cs`: 단일 호스트 입력·상태 전환·해제 검사, 전용 `note-host` 진입점. 사용자 추가 화면을 포함해 진행 화면 수를 33개로 확인하고 화면별 오류를 수집한 뒤 전체 실패 보고.
  - `Validation/OwnerHistoryChecks/ScrollChecks.cs`: 실제 WPF 클록·색상·저장 위치·빈 값·빠른 교체 검증 확장.
  - 검증 README 두 개, `Docs/비고호스트_통합계획_2026-09-28.md`: 사용법·완료 범위·기존 실패 기록.
- 영향 범위·보존: 128×128 비고 호스트와 스크롤 위치 복원. 개별 화면·NoteText 문구/색상 정책·통신·설정·캐시는 변경하지 않음. ViewModel·NoteText 신규 계층 또는 추가 복제 없음. 사업장·축종·해상도 선택 조건은 그대로 유지.
- 빌드·검증:
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -p:OutputPath=<임시 폴더>/Debug -p:IntermediateOutputPath=<임시 폴더>/Intermediate/ -v:q -clp:ErrorsOnly`: 오류 0개·경고 72개.
  - `dotnet build Validation/OwnerHistoryChecks/OwnerHistoryChecks.csproj --no-restore -p:OutputPath=<임시 폴더>/Owner -p:IntermediateOutputPath=<임시 폴더>/OwnerIntermediate/ -v:q -clp:ErrorsOnly`: 오류 0개·경고 72개.
  - 해당 출력의 `ReferenceAdoptionChecks.dll note-host`: 76개, `stage4`: 596개, `bidder`: 253개 통과.
  - 해당 출력의 `OwnerHistoryChecks.dll --scroll-offline`: 32개 통과. 같은 개체·가격 갱신, 색상만 변경, 제외 페이지 복귀, 다른 개체의 동일 비고, null·연속 교체·해제 검증 포함.
  - `binding`: 진행 33개·결과 37개 및 공통 호스트 검사를 실행, 전체 결과는 실패. 유일한 오류는 별도 사용자 화면 `CowHistorySearchRun.xaml`의 미정의 NumberFormatConverter 리소스다. 해당 파일은 수정하지 않음. 다른 진행 32개·결과 37개 및 공통 호스트 검사는 통과. 이전 옛 네임스페이스 오류는 작업 시작 시 사용자가 이미 수정한 상태였다.
- Git 정보: `master`, HEAD `52c554d`, 시작 상태 215개 항목. 시작 사본과 변경분을 대조해 기존 작업을 보존했으며, 기존 XAML의 공백 경고는 이번 변경과 구분함. commit hash `미생성`, push `미수행`.
- 추가 확인: 운영 앱·전광판 실행·서버 연결·Release 빌드·배포는 미수행. CowHistorySearchRun의 별도 리소스 오류가 해결되어야 전체 binding 통과를 확인할 수 있음. 메모리·CPU 절감률은 미측정.


## 2026-09-28 — 김제·임실 진행 화면 폴더 정리

- 목적·승인: 사용자의 “변경하자” 승인. 사업장별 첫 번째 진행 화면을 `CustomAuctionRunning1`에 모은다.
- 변경 파일·내용:
  - `Views/Size128_128/Running/GimjeRun.xaml`·`.xaml.cs`, `ImsilRun.xaml`·`.xaml.cs`를 `Running/CustomAuctionRunning1/`으로 이동. XAML의 `x:Class`·`xmlns:local`과 코드 네임스페이스만 새 위치에 맞춤.
  - `CowAuctionSmall.csproj`: GimjeRun 코드 파일의 Compile Update 경로 수정.
  - 기존 화면 내용·바인딩·색상·애니메이션은 보존. SetCustomDisplay에는 대상 네임스페이스 using이 이미 있어 추가 수정 없이 김제 화면을 선택함. 임실 화면 선택 분기는 새로 추가하지 않음.
- 빌드·검증:
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -p:OutputPath=<임시 폴더>/Debug -p:IntermediateOutputPath=<임시 폴더>/Intermediate/ -v:q -clp:ErrorsOnly`: 오류 0개·경고 74개. 첫 기본 중간 경로 빌드는 WPF 생성 멤버 누락 등 110개 오류가 발생하여 기존 obj를 삭제하지 않고 별도 중간 경로에서 검증함.
  - 해당 출력의 `ReferenceAdoptionChecks.dll stage4`: 596개 통과.
  - `binding`: 기존 고정 조건인 진행 화면 32개 검사에서 중단. 사용자 추가 파일 CowHistorySearchRun을 포함한 실제 개수는 33개. 관련 없는 검증 조건·화면은 수정하지 않음.
  - 임시 오프라인 Harness에서 기존 BindingChecks.CheckRunning을 이동한 두 화면에 직접 실행: 바인딩·갱신 160개 통과. 새 타입·옛 타입 부재와 기존 김제 선택 함수가 이동한 타입을 반환하는 것도 확인함.
  - 마지막 `dotnet build CowAuctionSmall.csproj --no-restore -p:OutputPath=<임시 폴더>/Product -v:q -clp:ErrorsOnly`: 오류 1개. 작업 도중 편집된 `CowHistorySearchRun.xaml:7`의 `CowAuctionSmall.Models.Converter` 참조로 CS0234 발생. 이 사용자 파일은 수정하지 않음. 따라서 최신 전체 작업 트리의 일반 빌드는 실패 상태임.
- Git 정보: `master`, HEAD `52c554d`, 시작 상태 208개 항목. 이동 전 임시 사본 대비 허용한 네임스페이스·경로 외 내용 보존 확인. commit hash `미생성`, push `미수행`.
- 추가 확인: 운영 서버 연결·앱 실행·배포는 미수행. 별도 작업 중인 CowHistorySearchRun의 네임스페이스 오류 및 전체 바인딩 검사 개수는 후속 확인 필요.


## 2026-09-28 — 낙찰자 전체 이름 유지와 표시 폭 기준 스크롤

- 목적·승인: 사용자의 “좋아 그렇게 변경하자” 승인. 이름 표시 Y와 이름+참가번호 B는 전체 이름을 유지하고 X만 기존 3글자 제한·마스킹을 적용한다.
- 문제·원인: Y에서 사업장에 따라 이름을 3글자 또는 5글자로 잘랐다. 공통 낙찰자 컨트롤은 5글자 이하의 스크롤을 막고 6글자 이상은 표시 폭과 관계없이 흘렸다.
- 변경 파일·내용:
  - `Infrastructure/Api/AnimalParseData.cs`: Y의 사업장별 절단 분기 제거. B의 전체 이름·참가번호와 X의 기존 마스킹, 번호 표시·결과 대기 처리는 유지.
  - `Views/Common/BidderNameFlow.xaml.cs`: 기본 최소 글자 수를 0으로 변경하고 강제 스크롤 해제. 컨트롤 폭 변경 시 애니메이션을 다시 초기화해 확장·축소 모두 반영. 기존 언로드 정리·예약 작업 취소 유지.
  - `Validation/ReferenceAdoptionChecks/BidderChecks.cs` 신규, `Program.cs`: `bidder` 검증 진입점. 합성 이름으로 API/SV·일반/기존 특례 사업장·Y/X/B/N·빈 이름·HTML 문자, 실제 WPF 폭·글꼴·텍스트 변경·반복 로드/언로드 검사.
- 영향 범위: 공통 파서가 처리하는 모든 낙찰자 이름과 `BidderNameFlow`를 사용하는 낙찰 화면. 일반 TextBlock·개별 Storyboard로 구성된 다른 화면은 이번에 공통 컨트롤로 이관하지 않았다. 출하주, 비고 스크롤, 운영 XML은 변경하지 않음.
- 빌드·검증:
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -p:OutputPath=<임시 검증 폴더>/Debug -v:q -clp:ErrorsOnly`: 최초 빌드 오류 0개·경고 74개, 검증 코드 수정 후 최종 증분 빌드 오류·경고 0개.
  - 해당 출력의 `dotnet ReferenceAdoptionChecks.dll bidder`: 253개 통과.
  - 같은 출력의 `stage4`: 596개, `binding`: 3,485개, `updates`: 55개 통과. 모두 합성 데이터·오프라인 검증이며 운영 서버 연결 없음.
  - 초기 검증의 URL 인코딩 가정은 기존 파서가 HTML 디코딩을 사용한다는 확인 후 합성 입력을 HTML 인코딩으로 수정했다. 제품 디코딩 정책은 변경하지 않음.
- Git 정보: `master`, HEAD `52c554d`, 시작 상태 206개 항목. 기존 미커밋·미추적 변경 보존, 시작 사본 대비 수정 범위·인코딩·줄바꿈 확인. commit hash `미생성`, push `미수행`.
- 추가 확인: 변경 후 운영 앱·실제 전광판 실행, Release 빌드·배포는 미수행. 기존 일반 TextBlock 화면의 긴 이름 표시·스크롤은 별도 확인 대상.


## 2026-09-28 - 공통 친자 뱃지 크기 및 비고 기준선 보정

### 작업 일자

- 2026-09-28

### 작업 목적

- 128×128 공통 화면의 친자 뱃지를 28×17 도트 영역과 일반 글꼴 형태로 조정하고, 비고를 뱃지 글자와 같은 라인에 표시한다.

### 원인

- 기존 뱃지는 높이 18, 굵은 11px 글꼴과 내부 여백 1을 사용하여 하단 여백이 2줄로 보였고, 비고에는 위쪽 여백 1px이 있어 뱃지 글자보다 낮게 표시됐다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/RunningNoteHost128.xaml`: 친자 뱃지 높이를 18에서 17로, 내부 여백을 1에서 0으로 조정하고 글꼴을 12px 일반 굵기로 변경했다. 비고 위쪽 여백을 제거했다.
- `CowAuctionSmall/Views/Size128_128/Running/RunningNoteHost128_Running.xaml`: 진행용 공통 호스트에도 동일한 친자 뱃지 크기·글꼴과 비고 기준선을 적용했다.
- `WORKLOG.md`: 변경 목적, 원인, 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 두 공통 호스트를 사용하는 128×128 일반우 진행·낙찰·유찰 화면의 친자 뱃지와 비고 표시에 적용된다.
- 친자 판정, 표시 조건, 문구·색상 바인딩과 애니메이션 동작은 변경하지 않았다.
- 공통 호스트를 사용하지 않는 화면에는 영향이 없다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 두 공통 호스트의 뱃지 크기·글꼴 및 비고 여백 일치 여부: 코드 확인 완료.
- 실제 128×128 LED 전광판 표시 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 공통 친자 뱃지 크기와 비고 정렬 보정`


## 2026-09-28 - 장성축협 친자 뱃지 설정 조건 보완

### 작업 일자

- 2026-09-28

### 작업 목적

- 장성축협의 `친자/으뜸/혈통` 뱃지가 `IsPaternityMatch=Y`일 때만 동작하도록 설정 조건을 일치시킨다.

### 원인

- 공통 친자 판정은 `IsPaternityMatch=N`일 때 비활성화되지만, 이후 실행되는 장성 전용 비고 키워드 처리가 설정값을 확인하지 않고 `PaternityMatch`를 다시 활성화했다.

### 변경 파일 및 내용

- `CowAuctionSmall/Services/AnimalParseData.cs`: 장성 전용 비고 키워드 처리를 `IsPaternityMatch=Y`일 때만 실행하고, 그 외 설정에서는 뱃지 값을 비활성화한 채 비고 원문을 유지하도록 변경했다.
- `WORKLOG.md`: 원인, 변경 조건, 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 장성축협 코드 `8808990817675`의 128×128 진행·낙찰·유찰 화면에 공통 호스트로 표시되는 친자 뱃지 데이터에 적용된다.
- `IsPaternityMatch=Y`의 기존 `친자/으뜸/혈통` 문구·색상과 비고 키워드 제거 동작은 유지한다.
- 익산군산 및 다른 축협의 친자 판정, `RunningNoteHost128`, `RunningNoteHost128_Running`과 개별 View는 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 장성 `IsPaternityMatch=Y`에서 기존 키워드 처리를 유지하고, 그 외 설정에서는 뱃지를 비활성화하며 `Note`를 변경하지 않는 분기 확인: 코드 확인 완료.
- 실제 장성축협 128×128 진행·낙찰·유찰 화면 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 장성 친자 뱃지 설정 조건 보완`


## 2026-09-28 - 영주축협 진행 화면 단축번호 잘림 보정

### 작업 일자

- 2026-09-28

### 작업 목적

- 영주축협 128×128 진행 화면에서 네 자리 단축번호의 마지막 숫자가 잘리고 아래 전체 개체번호 일부가 드러나는 현상을 보정한다.

### 원인

- `Standard_non_QQuri_Run1`의 단축번호 표시 폭이 22px로 네 자리 숫자의 실제 렌더링 폭과 거의 같아 PC별 DPI·글꼴 렌더링 차이에 따라 마지막 숫자의 오른쪽 획이 잘릴 수 있었다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/Standard_non_QQuri_Run1.xaml`: 단축번호 시작 위치를 X=86에서 X=87로 1px 이동하고, 기존 글꼴·크기·정렬을 유지한 채 Width를 22에서 25로 늘렸다.
- `WORKLOG.md`: 원인, 수정값, 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 영주축협을 포함해 `Standard_non_QQuri_Run1`을 사용하는 128×128 진행 화면에 적용된다.
- 전체 개체번호 위치, 다른 진행 View, 낙찰·유찰 화면, 데이터 바인딩 및 화면 선택 분기는 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 단축번호 X=87, Width=25와 기존 스타일 유지 확인: 코드 확인 완료.
- 실제 영주축협 128×128 전광판에서 `3713`, `8916` 끝자리 표시 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 영주 진행 화면 단축번호 잘림 보정`


## 2026-09-28 - 장성축협 진행·낙찰·유찰 화면 개체번호 정렬 보정

### 작업 일자

- 2026-09-28

### 작업 목적

- 장성축협 128×128 진행·낙찰·유찰 화면에서 전체 개체번호와 가운데 네 자리 단축번호의 상대 위치와 색상 경계를 안정화한다.

### 원인

- 진행 화면은 전체번호 X=60, 단축번호 X=80으로 시작점 차이가 20px에 불과해 두 번호가 많이 겹쳤다.
- 낙찰 화면은 굴림 13px 전체번호와 X=73의 굵은 단축번호를 겹쳐 사용해 실제 앞 네 자리와 공백 폭에 맞지 않았다.
- 낙찰 단축번호를 X=76과 X=77로 순차 보정한 뒤에도 실제 화면에서 가운데 네 자리가 왼쪽으로 겹쳐 보여 추가 보정이 필요했다.
- 유찰 화면은 전체번호와 단축번호가 모두 X=44여서 서로 완전히 겹쳤다.
- 세 화면에 픽셀 반올림 설정이 없어 화면 배율과 카드 위치에 따라 미세한 색상 경계 차이가 날 수 있었다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/CustomAuctionRunning1/Jangseong.xaml`: 전체번호 X=60을 유지하고 단축번호를 X=80에서 X=88로 이동했다. 두 번호를 굴림체 11px·왼쪽 정렬로 맞추고 단축번호 Width=22, 일반 굵기를 적용했다.
- `CowAuctionSmall/Views/Size128_128/CustomAuctionSold/JangseongSold.xaml`: 전체번호 X=43을 유지하고 실제 화면 확인 결과를 반영해 단축번호를 X=73에서 X=78로 이동했다. 두 번호를 굴림체 13px·왼쪽 정렬로 맞추고 단축번호 Width=28, 일반 굵기를 적용했다.
- `CowAuctionSmall/Views/Size128_128/CustomAuctionUnSold/JangseongUnSold.xaml`: 전체번호 X=44를 유지하고 단축번호를 X=44에서 X=75로 분리했다. 두 번호를 굴림체 12px·왼쪽 정렬로 맞추고 단축번호 Width=26, 일반 굵기를 적용했다.
- 세 화면에 `UseLayoutRounding=True`, `SnapsToDevicePixels=True`를 적용했다.
- `WORKLOG.md`: 원인, 수정값, 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 장성축협 코드 `8808990817675`의 `Jangseong` 진행 1페이지, `JangseongSold` 낙찰, `JangseongUnSold` 유찰 화면에 적용된다.
- 개체번호를 표시하지 않는 `TestRunning2` 유전 화면과 화면 선택 분기, 데이터 바인딩 및 다른 사업장 View는 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 진행 전체번호 X=60/단축번호 X=88, 낙찰 X=43/78, 유찰 X=44/75와 각 스타일 확인: 코드 확인 완료.
- 실제 장성축협 128×128 전광판에서 `1815 3928`, `1771 4069`, `2069 6213 1` 색상 경계 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 장성 진행·낙찰·유찰 화면 개체번호 정렬 보정`


## 2026-09-27 - 보령축협 낙찰 화면 금액 위치 보정

### 작업 일자

- 2026-09-27

### 작업 목적

- 보령축협 낙찰 화면에서 최저가와 낙찰가 금액을 왼쪽으로 이동해 라벨과 금액 사이의 여백을 줄인다.

### 원인

- 최저가와 낙찰가 값이 Grid 3번째 열에서 Margin.Left=7을 사용해 라벨의 콜론과 금액 사이가 넓게 표시됐다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/CustomAuctionSold/JecheonDanyangSold.xaml`: 최저가와 낙찰가 값의 Margin.Left를 7에서 0으로 변경했다.
- 두 금액의 글자 크기, 색상, 왼쪽 정렬, 세로 가운데 정렬과 라벨 위치는 유지했다.
- `WORKLOG.md`: 변경 사유, 적용값, 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 보령축협 코드 `8808990684321`과 제천단양축협 코드 `8808990656526`이 공유하는 `JecheonDanyangSold` 128×128 낙찰 화면의 최저가·낙찰가 금액 위치에 적용된다.
- 유찰·진행 화면, 데이터 바인딩 및 화면 선택 분기는 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 최저가·낙찰가 값의 Margin.Left=0과 기존 스타일 유지 확인: 코드 확인 완료.
- 실제 보령축협 128×128 전광판에서 `최저가 : 10,400`, `낙찰가 : 11,390` 간격 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 보령 낙찰 화면 금액 위치 보정`


## 2026-09-27 - 보령축협 유찰 화면 단축번호 미세 조정

### 작업 일자

- 2026-09-27

### 작업 목적

- 보령축협 유찰 화면에서 가운데 네 자리 단축번호의 실제 전광판 표시 위치를 오른쪽으로 미세 조정한다.

### 원인

- 전체번호 X=44와 굴림체 12px의 계산상 단축번호 시작점은 X=74였지만, 실제 화면에서는 가운데 네 자리가 약 1px 왼쪽으로 겹쳐 보였다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Standard_non_X_UnSold.xaml`: 단축번호 Margin.Left를 X=74에서 X=75로 변경했다.
- 전체번호 X=44, Width=26, 굴림체 12px, 왼쪽 정렬과 나머지 화면 배치는 유지했다.
- `WORKLOG.md`: 미세 조정 사유와 변경값 및 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 보령축협 코드 `8808990684321`과 `Standard_non_X_UnSold` 공통 조건을 사용하는 다른 사업장의 일반 소 유찰 화면 단축번호에 적용된다.
- 낙찰·진행 화면, 데이터 바인딩 및 화면 선택 분기는 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 전체번호 X=44와 단축번호 X=75 및 기존 스타일 유지 확인: 코드 확인 완료.
- 실제 보령축협 128×128 전광판에서 `1870 2954 3` 색상 경계 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 보령 유찰 화면 단축번호 위치 보정`


## 2026-09-27 - 보령축협 낙찰·유찰 화면 행 정렬 보정

### 작업 일자

- 2026-09-27

### 작업 목적

- 보령축협 128×128 유찰 화면의 항목 시작선과 개체번호 색상 경계를 맞추고, 제천단양·보령 공통 낙찰 화면의 축종·개체번호 및 낙찰가 행 기준선을 통일한다.

### 원인

- `Standard_non_X_UnSold`에서 최저·중량·출하주는 X=2였지만 축종은 X=0이었고, 전체번호와 단축번호가 모두 X=44여서 서로 겹쳤다.
- `JecheonDanyangSold`에서 축종은 11px와 음수 위쪽 여백을 사용했지만 번호는 12px로 세로 기준선이 달랐다.
- 낙찰 번호가 비고정폭 굴림 12px인 상태에서 단축번호를 30px 뒤에 겹쳐 표시해 가운데 네 자리 경계가 약 1.56px 어긋날 수 있었다.
- 낙찰가 라벨과 값은 각각 13px·14px였고 라벨에 세로 가운데 정렬이 없어 같은 행에서 기준선이 달랐다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Standard_non_X_UnSold.xaml`: 축종 시작점을 X=2로 맞추고 전체번호 X=44, 단축번호 X=74로 분리했다.
- 유찰 전체번호와 단축번호를 굴림체 12px·왼쪽 정렬로 고정하고 단축번호 Width=26, 일반 굵기를 적용했다.
- `CowAuctionSmall/Views/Size128_128/CustomAuctionSold/JecheonDanyangSold.xaml`: 축종·전체번호·단축번호를 굴림체 12px와 세로 가운데 정렬로 통일하고 축종 시작점을 X=2로 맞췄다.
- 낙찰가 라벨과 값을 13px·세로 가운데 정렬로 통일했으며 기존 금액 시작점 Margin.Left=7을 유지했다.
- 두 화면에 `UseLayoutRounding=True`, `SnapsToDevicePixels=True`를 적용했다.
- `WORKLOG.md`: 원인, 수정값, 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 보령축협 코드 `8808990684321`의 유찰 화면과 보령·제천단양 공통 낙찰 화면에 적용된다.
- `Standard_non_X_UnSold` 공통 조건을 사용하는 다른 사업장의 일반 소 유찰 화면에도 시작선과 번호 정렬 변경이 적용된다.
- 데이터 바인딩, 화면 선택 분기, 진행 화면 및 다른 낙찰·유찰 View는 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 유찰 최저·중량·출하주·축종 X=2와 전체번호 X=44/단축번호 X=74 확인: 코드 확인 완료.
- 낙찰 축종·번호 굴림체 12px와 낙찰가 라벨·값 13px 세로 가운데 정렬 확인: 코드 확인 완료.
- 실제 보령축협 128×128 전광판에서 번호 색상 경계와 행 기준선 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 보령 낙찰·유찰 화면 행 정렬 보정`


## 2026-09-27 - 문경축협 진행 화면 개체번호 정렬 보정

### 작업 일자

- 2026-09-27

### 작업 목적

- 문경축협 전용 `Standard_non_QQuri_Run1_1` 128×128 진행 화면에서 전체 개체번호와 가운데 네 자리 단축번호의 상대 위치를 안정화한다.

### 원인

- 전체 개체번호는 X=60, 단축번호는 X=80에 배치돼 두 시작점 차이가 20px에 불과했다.
- 단축번호의 Width가 30이고 왼쪽 정렬과 일반 굵기가 명시되지 않았으며, 두 번호에 글꼴과 크기가 고정되지 않아 김제 진행 화면과 다른 위치로 표시됐다.
- 문경 전용 혈통 스타일도 글꼴·크기·왼쪽 정렬이 고정되지 않아 전체 개체번호 시작 영역과 겹쳐 보였다.
- 전체번호 앞 네 자리와 공백의 렌더링 폭이 27.5px인데 문경 화면에는 픽셀 반올림 설정이 없어 카드 위치와 화면 배율에 따라 가운데 네 자리의 색상 경계가 미세하게 달라 보였다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/Standard_non_QQuri_Run1_1.xaml`: 김제 진행 화면 기준으로 전체번호를 X=60에서 X=58로, 단축번호를 X=80에서 X=86으로 조정했다.
- 두 번호에 `FontFamily=굴림체`, `FontSize=11`, `HorizontalAlignment=Left`를 적용하고, 단축번호 Width를 `30`에서 `22`로 변경하며 `FontWeight=Normal`을 적용했다.
- 문경 전용 혈통 스타일을 X=35, 굴림체 11px, 왼쪽 정렬로 맞춰 개체번호 X=58 영역과 겹치지 않도록 했다. 기존 Cyan 색상은 유지했다.
- 김제 진행 화면과 동일하게 루트 `UserControl`에 `UseLayoutRounding=True`, `SnapsToDevicePixels=True`를 적용해 소수 픽셀 좌표를 장치 픽셀 경계에 맞췄다.
- 기존 동적 전경·배경, 혈통·출하주·육종·중량 등 나머지 진행 화면 배치는 유지했다.
- `WORKLOG.md`: 원인, 수정값, 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 문경축협 코드 `8808990656427`의 `Standard_non_QQuri_Run1_1` 128×128 진행 1페이지 개체번호 행에 적용된다.
- 낙찰 `AnseongSold`, 유찰 `QQuriUnSold`, 진행 2페이지 및 다른 사업장 View에는 적용되지 않는다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 전체번호 X=58, 단축번호 X=86/Width=22 및 두 항목의 굴림체 11px·왼쪽 정렬 확인: 코드 확인 완료.
- 혈통 X=35, 굴림체 11px·왼쪽 정렬 및 개체번호 X=58과의 분리 확인: 코드 확인 완료.
- 루트 픽셀 반올림과 장치 픽셀 맞춤 설정 확인: 코드 확인 완료.
- 실제 문경축협 128×128 전광판에서 `2181 0741 2` 간격 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 문경 진행 화면 개체번호 정렬 보정`


## 2026-09-27 - 양평축협 낙찰 화면 개체번호 정렬 보정

### 작업 일자

- 2026-09-27

### 작업 목적

- 양평축협 `YangpyeongSold` 128×128 낙찰 화면에서 전체 개체번호와 가운데 네 자리 단축번호의 상대 위치를 안정화한다.

### 원인

- 전체 개체번호는 X=45, 단축번호는 X=77에 배치됐지만 두 항목의 왼쪽 정렬이 명시되지 않았다.
- 단축번호의 표시 폭도 고정되지 않았고 두 시작점 간격이 32px여서 13px 개체번호의 가운데 네 자리보다 오른쪽에 표시될 수 있었다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/CustomAuctionSold/YangpyeongSold.xaml`: 전체번호 X=45를 유지하고 `HorizontalAlignment=Left`를 적용했다.
- 단축번호를 X=77에서 X=75로 이동하고 Width `28`, `HorizontalAlignment=Left`, `FontWeight=Normal`을 적용했다.
- 기존 FontSize `13`, 동적 전경·배경과 나머지 낙찰 화면 배치는 유지했다.
- `WORKLOG.md`: 원인, 수정값, 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 양평축협 코드 `8808990643625`의 `YangpyeongSold` 128×128 낙찰 화면 개체번호 행에 적용된다.
- 진행 `YangpyeongRun`, 유전 `Eumseong2`, 유찰 `YangpyeongUnSold`, 가격·낙찰자 및 다른 사업장 View에는 적용되지 않는다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 전체번호 X=45, 단축번호 X=75/Width=28 및 두 항목의 왼쪽 정렬 확인: 코드 확인 완료.
- 실제 양평축협 128×128 전광판에서 `2204 8536 8`, `2204 8684 2` 간격 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 양평 낙찰 화면 개체번호 정렬 보정`


## 2026-09-27 - 해남진도축협 낙찰 화면 개체번호 정렬 보정

### 작업 일자

- 2026-09-27

### 작업 목적

- 해남진도축협 `HaenamJindoSold` 128×128 낙찰 화면에서 전체 개체번호와 가운데 네 자리 단축번호의 상대 위치를 안정화한다.

### 원인

- 전체 개체번호는 X=44, 단축번호는 X=72에 배치됐지만 두 항목의 왼쪽 정렬이 명시되지 않았다.
- 단축번호의 표시 폭도 고정되지 않아 전체번호의 가운데 네 자리와 일관된 위치로 겹치기 어려웠다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/CustomAuctionSold/HaenamJindoSold.xaml`: 전체번호 X=44를 유지하고 `HorizontalAlignment=Left`를 적용했다.
- 단축번호를 X=72에서 X=74로 이동하고 Width `26`, `HorizontalAlignment=Left`, `FontWeight=Normal`을 적용했다.
- 기존 글자 크기, 동적 전경·배경과 나머지 낙찰 화면 배치는 유지했다.
- `WORKLOG.md`: 원인, 수정값, 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 해남진도축협 코드 `8808990656106`의 `HaenamJindoSold` 128×128 낙찰 화면 개체번호 행에 적용된다.
- 진행 `HaenamJindo`, 유찰 `OutLineUnSold`, 가격·낙찰자 및 다른 사업장 View에는 적용되지 않는다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 전체번호 X=44, 단축번호 X=74/Width=26 및 두 항목의 왼쪽 정렬 확인: 코드 확인 완료.
- 실제 해남진도축협 128×128 전광판에서 `1775 4190 5`, `1944 8431 1` 간격 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 해남진도 낙찰 화면 개체번호 정렬 보정`


## 2026-09-27 - 횡성축협 진행·낙찰 화면 개체번호 정렬 보정

### 작업 일자

- 2026-09-27

### 작업 목적

- 횡성축협 `HoengseongRun` 진행 화면과 일반 `HoengseongSold` 낙찰 화면의 전체 개체번호와 가운데 네 자리 단축번호를 동일한 기준으로 정렬한다.

### 원인

- 낙찰 화면은 전체번호 Margin.Left `40`, 단축번호 Margin.Left `77`을 사용해 두 시작점 간격이 37px이었다.
- 진행 화면 `HoengseongRun`은 전체번호 X=42, 단축번호 X=74로 32px 간격을 사용하고 있어 화면 간 번호 배치가 달랐다.
- 두 번호가 Canvas 안에서 Margin으로 배치되고 왼쪽 정렬이 명시되지 않아 이후 위치 조정 기준도 불명확했다.
- 낙찰 화면의 현장 보정 결과에 맞춰 진행 화면 단축번호도 X=76으로 통일할 필요가 있었다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/CustomAuctionSold/HoengseongSold.xaml`: 전체번호를 `Canvas.Left=42`로 변경하고, 현장 화면 확인에 따라 단축번호를 `Canvas.Left=76`으로 최종 조정했다.
- 두 TextBlock에 `Canvas.Top=0`과 `HorizontalAlignment=Left`를 명시하고 기존 Margin 좌표를 제거했다.
- `CowAuctionSmall/Views/Size128_128/Running/CustomAuctionRunning1/HoengseongRun.xaml`: 전체번호 X=42를 유지하고 현장 화면 확인에 따라 단축번호를 X=72로 최종 조정했으며 두 항목에 왼쪽 정렬을 명시했다.
- FontSize `13`, 진행 화면 단축번호의 빨간색·검은 배경, 낙찰 화면의 동적 전경·배경과 기존 굵기는 유지했다.
- `WORKLOG.md`: 원인, 수정값, 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 횡성축협 코드 `8808990656885`의 `HoengseongRun` 진행 화면과 일반 `HoengseongSold` 128×128 낙찰 화면 개체번호 행에 적용된다.
- 유전 `HoengseongRun2`, 유찰 `HoengseongUnSold` 및 행사용 `HoengseongBunyangSold` 화면은 변경하지 않았다.
- 가격, 낙찰자, 화면 분기와 데이터 바인딩은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 진행 화면 전체번호 X=42/단축번호 X=72, 낙찰 화면 전체번호 X=42/단축번호 X=76 및 두 항목의 왼쪽 정렬 확인: 코드 확인 완료.
- 실제 횡성축협 128×128 전광판에서 진행 `2205 3978 6`과 낙찰 `1980 7802 9`, `2139 4913 0` 간격 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 횡성 진행·낙찰 화면 개체번호 정렬 보정`


## 2026-09-27 - 제천단양·보령 낙찰 화면 개체번호·금액 정렬 보정

### 작업 일자

- 2026-09-27

### 작업 목적

- 제천단양·보령 공통 `JecheonDanyangSold` 128×128 낙찰 화면에서 전체 개체번호와 가운데 네 자리 단축번호의 상대 위치를 안정화하고 최저가·낙찰가 금액 시작선을 맞춘다.

### 원인

- 전체 개체번호는 Grid 영역의 시작점에서 표시됐지만 왼쪽 정렬이 명시되지 않았다.
- 단축번호는 같은 Grid 영역에서 `HorizontalAlignment=Center`와 Margin.Left `5`를 함께 사용해 숫자 렌더링 폭에 따라 전체번호와 기준점이 달라질 수 있었다.
- 최저가는 별도 Margin이 없고 낙찰가는 Margin.Left `10`을 사용해 두 금액의 시작 위치가 서로 달랐다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/CustomAuctionSold/JecheonDanyangSold.xaml`: 전체 개체번호에 `HorizontalAlignment=Left`를 명시하고 기존 시작 위치를 유지했다.
- 단축번호를 왼쪽 정렬로 변경하고 Margin.Left `30`, Width `26`으로 고정했다.
- 최저가와 낙찰가 값을 모두 왼쪽 정렬하고 Margin.Left `7`, 세로 가운데 정렬로 맞췄다.
- 개체번호 FontSize `12`, FontWeight `Normal`, 동적 전경·배경 및 최저가 12px·낙찰가 14px 크기는 유지했다.
- `WORKLOG.md`: 원인, 수정값, 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 제천단양축협 코드 `8808990656526`과 보령축협 코드 `8808990684321`의 일반 소 128×128 낙찰 화면 개체번호 1행과 최저가·낙찰가 금액 위치에 적용된다.
- 사업장별 화면 분기, 낙찰자, 진행·유찰 화면과 다른 낙찰 View에는 적용되지 않는다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 전체번호 왼쪽 정렬, 단축번호 왼쪽 정렬 및 Margin.Left `30`/Width `26` 확인: 코드 확인 완료.
- 최저가·낙찰가 값의 왼쪽 정렬 및 Margin.Left `7` 확인: 코드 확인 완료.
- 실제 제천단양축협 128×128 전광판에서 `2175 4786 0`, `2185 0519 3` 간격과 최저가·낙찰가 금액 시작선 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 제천단양 낙찰 화면 개체번호·금액 정렬 보정`


## 2026-09-27 - 뿌리농가 미적용 낙찰 화면 개체번호 정렬 보정

### 작업 일자

- 2026-09-27

### 작업 목적

- 공통 `Standard_non_X_Sold` 128×128 낙찰 화면에서 전체 개체번호와 가운데 네 자리 단축번호의 상대 위치를 안정화한다.

### 원인

- 전체 개체번호는 Grid 영역의 시작점에서 표시됐지만 왼쪽 정렬이 명시되지 않았다.
- 단축번호는 같은 Grid 영역에서 `HorizontalAlignment=Center`와 Margin.Left `5`를 함께 사용해 숫자 렌더링 폭에 따라 전체번호와 기준점이 달라질 수 있었다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Standard_non_X_Sold.xaml`: 전체 개체번호에 `HorizontalAlignment=Left`를 명시하고 기존 시작 위치를 유지했다.
- 단축번호를 왼쪽 정렬로 변경하고 Margin.Left `30`, Width `26`으로 고정했다.
- FontSize `12`, FontWeight `Normal`, 동적 전경·배경과 나머지 낙찰 화면 배치는 유지했다.
- `WORKLOG.md`: 원인, 수정값, 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- `Standard_non_X_Sold`를 사용하는 128×128 일반 소 낙찰 화면의 개체번호 1행에 적용된다.
- 낙찰가, 중량, 낙찰자, 진행·유찰 화면과 다른 낙찰 View에는 적용되지 않는다.
- 앞서 승인된 수원축협 사업장 코드 수정은 그대로 유지했다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 전체번호 왼쪽 정렬, 단축번호 왼쪽 정렬 및 Margin.Left `30`/Width `26` 확인: 코드 확인 완료.
- 실제 128×128 전광판에서 `2171 7559 6`, `1859 7369 4` 간격 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 뿌리농가 미적용 낙찰 개체번호 정렬 보정`


## 2026-09-27 - 수원축협 낙찰 화면 사업장 코드 수정

### 작업 일자

- 2026-09-27

### 작업 목적

- 수원축협 사업장 코드 `8808990656496`에서 지정된 128×128 낙찰 화면이 선택되도록 코드 오타를 바로잡는다.

### 원인

- `MokpoMuanSinanSold` 분기의 수원 코드가 `8808998656496`으로 등록돼 실제 코드 `8808990656496`과 일치하지 않았다.
- 이 불일치로 수원축협의 뿌리농가 `X` 낙찰 화면은 전용 분기 대신 기본 `Standard_non_X_Sold`로 선택됐다.

### 변경 파일 및 내용

- `CowAuctionSmall/Services/SetCustomDisplay.cs`: `MokpoMuanSinanSold` 분기의 수원 코드를 `8808998656496`에서 `8808990656496`으로 수정했다.
- 진행 화면, 유찰 화면, XAML과 다른 사업장 코드는 변경하지 않았다.
- `WORKLOG.md`: 원인, 변경 내용, 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 수원축협 코드 `8808990656496`의 일반 소 128×128 낙찰 화면은 `MokpoMuanSinanSold`를 사용한다.
- 뿌리농가 `X` 조건의 진행 화면은 기존 기본 분기에 따라 `Standard_non_X_Run1`을 계속 사용한다.
- 목무신, 춘천, 거창, 홍천, 예산 등 같은 낙찰 View를 공유하는 다른 사업장 분기는 유지된다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 정확한 수원 코드가 `MokpoMuanSinanSold`에 연결되고 오타 코드가 제거됐는지 확인: 코드 확인 완료.
- 실제 수원축협 128×128 전광판에서 진행·낙찰 화면 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 수원 낙찰 화면 사업장 코드 수정`


## 2026-09-27 - 홍천축협 낙찰·유찰 화면 분기 수정

### 작업 일자

- 2026-09-27

### 작업 목적

- 홍천축협 사업장 코드 `8808990674605`에서 지정된 128×128 낙찰·유찰 화면이 선택되도록 분기를 바로잡는다.

### 원인

- 이천축협 코드 `8808990844220`이 `AnseongSold` 분기와 홍천용 `MokpoMuanSinanSold` 분기에 중복 등록돼, 앞쪽 `AnseongSold`가 먼저 선택됐다.
- 홍천축협의 실제 사업장 코드는 `8808990674605`이며, 기존 홍천 유찰 분기도 이천 코드에 연결돼 있었다.
- 프로젝트에는 `OutLineUnSold_1` 클래스가 없고 첫 번째 외곽선 유찰 화면은 `OutLineUnSold`이다.

### 변경 파일 및 내용

- `CowAuctionSmall/Services/SetCustomDisplay.cs`: `MokpoMuanSinanSold`의 홍천 코드를 `8808990844220`에서 `8808990674605`로 변경했다.
- 홍천 유찰 분기를 `8808990674605`와 `OutLineUnSold` 조합으로 변경했다.
- 이천 코드 `8808990844220`의 기존 `AnseongSold` 분기는 유지했다.
- 진행 화면 분기와 낙찰·유찰 XAML은 변경하지 않았다.
- `WORKLOG.md`: 원인, 변경 내용, 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 홍천축협 코드 `8808990674605`의 128×128 낙찰 화면은 `MokpoMuanSinanSold`, 유찰 화면은 `OutLineUnSold`를 사용한다.
- 뿌리농가 `X` 조건의 진행 화면은 기존 기본 분기에 따라 `Standard_non_X_Run1`을 계속 사용한다.
- 이천축협 코드 `8808990844220`의 `AnseongSold` 낙찰 분기는 유지된다.
- 화면 XAML, 데이터 바인딩, 파싱 및 다른 사업장 분기는 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 홍천 코드의 낙찰·유찰 분기 및 이천 코드의 낙찰 분기 유지 여부: 코드 확인 완료.
- 실제 홍천축협 128×128 전광판에서 진행·낙찰·유찰 화면 전환 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 홍천 낙찰·유찰 화면 분기 수정`


## 2026-09-27 - 거창축협 진행·낙찰 화면 개체번호 정렬 보정

### 작업 일자

- 2026-09-27

### 작업 목적

- 거창축협 128×128 진행 및 낙찰 화면에서 전체 개체번호와 가운데 네 자리 단축번호의 간격과 오버레이 위치를 안정화한다.

### 원인

- 거창 사업장 코드 `8808990659701`은 뿌리농가 `X` 조건에서 진행 화면으로 공통 `Standard_non_X_Run1`, 낙찰 화면으로 `MokpoMuanSinanSold`를 사용한다.
- 진행 화면은 전체번호와 단축번호가 모두 X=40에 있고 단축번호 Width가 30이며 왼쪽 정렬이 명시되지 않아 오버레이 위치가 불안정했다.
- 낙찰 화면은 전체번호 X=44, 단축번호 X=72로 배치돼 현장 화면에서 가운데 네 자리 단축번호의 미세 보정이 필요했다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/Standard_non_X_Run1.xaml`: 전체번호를 X=44, 단축번호를 X=74로 변경하고 두 항목에 `HorizontalAlignment=Left`를 적용했다.
- 진행 화면 단축번호 Width를 `30`에서 `26`으로 줄이고 `FontWeight=Normal`을 명시했다.
- `CowAuctionSmall/Views/Size128_128/CustomAuctionSold/MokpoMuanSinanSold.xaml`: 전체번호 X=44를 유지하면서 왼쪽 정렬을 명시하고, 단축번호를 X=72에서 X=74로 이동했다.
- 낙찰 화면 단축번호 Width `26`, `HorizontalAlignment=Left`, `FontWeight=Normal`을 적용했다.
- 동적 전경·배경, 축종과 나머지 진행·낙찰 정보 배치는 유지했다.
- `WORKLOG.md`: 분기 근거, 수정값, 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- `Standard_non_X_Run1`을 사용하는 전용 진행 View가 없는 뿌리농가 `X` 조합의 128×128 진행 1페이지에 적용된다.
- `MokpoMuanSinanSold`를 사용하는 목무신, 춘천, 거창, 홍천, 수원, 예산 계열 128×128 낙찰 화면에 적용된다.
- 진행 2페이지, 유찰 화면, 다른 전용 View, C# 분기와 파싱에는 적용되지 않는다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 진행·낙찰 전체번호 X=44, 단축번호 X=74/Width=26, 왼쪽 정렬과 일반 굵기 확인: 코드 확인 완료.
- 실제 거창축협 128×128 전광판에서 `9999 0000 6`, `2173 6073 7` 간격 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 거창 진행·낙찰 화면 개체번호 정렬 보정`


## 2026-09-27 - 이천 계열 낙찰 화면 단축번호 우측 위치 보정

### 작업 일자

- 2026-09-27

### 작업 목적

- 이천축협 128×128 낙찰 화면에서 가운데 네 자리 단축번호를 전체 개체번호와 정확히 맞춘다.

### 원인

- 이천 사업장 코드 `8808990844220`은 낙찰 화면에서 공통 `AnseongSold`를 사용한다.
- 단축번호가 Grid 2~4열 영역에서 `HorizontalAlignment=Center`, Margin.Left `18`로 배치돼 Margin 변경량과 실제 이동량이 일치하지 않았고, 현장 화면에서 전체번호보다 약 1px 왼쪽에 표시됐다.
- 단축번호를 왼쪽 정렬로 고정한 뒤에도 전체번호가 가운데 정렬을 사용해 문자열 렌더링 폭에 따라 전체번호의 시작점이 미세하게 달라졌고, 숫자 조합별로 상대 위치 차이가 발생했다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/CustomAuctionSold/AnseongSold.xaml`: 단축번호를 `HorizontalAlignment=Left`로 변경하고 해당 Grid 영역 기준 Margin.Left를 `36`으로 설정해 오른쪽으로 보정했다.
- Margin.Left `34` 적용 화면에서도 단축번호가 약 1px 왼쪽에 있어 현장 화면 기준 `35`로 최종 조정했다.
- 전체번호도 `HorizontalAlignment=Left`, Margin.Left `6`으로 고정해 숫자 조합이 바뀌어도 전체번호와 단축번호의 상대 위치가 일정하도록 보정했다.
- 전체번호 왼쪽 정렬 적용 후 현장 화면에서 단축번호가 약 1px 왼쪽에 있어 Margin.Left를 `35`에서 `36`으로 최종 조정했다.
- 단축번호 FontSize `13`, FontWeight `Normal`, 동적 전경·배경과 전체 개체번호 위치는 유지했다.
- 최저가, 낙찰가, 중량, 낙찰자 영역과 C# 분기는 변경하지 않았다.
- `WORKLOG.md`: 원인, 수정값, 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- `AnseongSold`를 공유하는 이천, 안성, 남원, 음성, 파주연천, 문경, 익산군산, 고성, 평택, 상주, 논산계룡, 구미칠곡, 포항, 옥천의 128×128 낙찰 화면 1행에 적용된다.
- 다른 낙찰 View, 진행·유전·유찰 화면 및 C# 소스에는 적용되지 않는다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 단축번호 `HorizontalAlignment=Left`, Margin.Left `36`, FontSize `13`, FontWeight `Normal` 확인: 코드 확인 완료.
- 전체번호 `HorizontalAlignment=Left`, Margin.Left `6`, FontSize `13` 확인: 코드 확인 완료.
- 실제 이천축협 128×128 전광판에서 단축번호 위치 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 이천 계열 낙찰 화면 단축번호 위치 보정`


## 2026-09-27 - 순창축협 진행 화면 1행 개체번호 정렬 보정

### 작업 일자

- 2026-09-27

### 작업 목적

- 순창축협 128×128 진행 1페이지에서 송아지, 혈통, 전체 개체번호와 가운데 네 자리 단축번호가 겹치거나 붙지 않도록 정렬한다.

### 원인

- `IsShowQQuri=Y` 조건에서 사용하는 공통 `StandardQQuri_Run1`만 기존 `굴림` 글꼴과 암시적 글자 크기, Grid 기본 정렬을 유지하고 있었다.
- 혈통 X=37, 전체번호 X=60, 단축번호 X=80 및 Width=30 조합으로 인해 4-4-1 개체번호의 공백과 단축번호 오버레이 위치가 불안정했다.
- `BidderName=N`은 낙찰자 표시 방식이며 진행 화면 View 선택에는 영향을 주지 않는다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/StandardQQuri_Run1.xaml`: 1행 네 TextBlock에 `굴림체`, FontSize `11`, `HorizontalAlignment=Left`를 적용했다.
- 혈통 X를 `37`에서 `35`, 전체번호 X를 `60`에서 `58`, 단축번호 X를 `80`에서 `86`으로 조정하고 단축번호 Width를 `30`에서 `22`로 변경했다.
- 단축번호에 `FontWeight=Normal`, `Panel.ZIndex=150`을 적용하고 `SnapsToDevicePixels=True`를 추가했다.
- 다른 공통 진행 화면에서 정상 확인된 1행 배치 규격과 동일하게 맞췄다.
- `WORKLOG.md`: 원인, 변경 내용, 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 순창축협을 포함해 전용 진행 View가 없고 `IsShowQQuri=Y` 또는 어미 으뜸 값 조건으로 `StandardQQuri_Run1`을 사용하는 128×128 진행 1페이지에 적용된다.
- 진행 2페이지, 낙찰·유찰 화면, 전용 진행 View, C# 분기와 설정 파일에는 적용되지 않는다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 1행 좌표와 글꼴, 정렬, 단축번호 폭 및 ZIndex 확인: 코드 확인 완료.
- 실제 순창축협 128×128 전광판에서 송아지·혈통·4-4-1 개체번호가 잘리거나 겹치지 않고 일정한 간격으로 표시되는 것을 사용자 제공 화면으로 확인했다.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 순창 진행 화면 개체번호 정렬 보정`


## 2026-09-27 - 페이지 전환 동작 상태 초록색 표시 추가

### 작업 일자

- 2026-09-27

### 작업 목적

- 출하AMS 하단 상태표시창에서 페이지 전환 동작 여부를 초록색 원으로 즉시 확인할 수 있게 한다.

### 원인

- 기존 상태표시창은 페이지 전환 동작 중일 때 `페이지전환` 문구만 표시해 동작 상태를 시각적으로 구분하는 표시가 없었다.
- `PageIndicatorStateMessage.IsFrozen`으로 전환과 고정 상태를 이미 구분하고 있으므로 별도의 전환 로직 변경 없이 표시 상태만 추가할 수 있다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/MainWindow.xaml`: 페이지 전환 상태 영역을 가로 StackPanel로 변경하고 `페이지전환` 문구 뒤에 9px 초록색 원을 추가했다. 원 추가 시 잘림을 방지하도록 영역 폭을 `85`에서 `100`으로 조정했다.
- `CowAuctionSmall/ViewModels/MainWindowViewModel.cs`: 총 페이지가 2개 이상이고 `IsFrozen=false`일 때만 초록색 원을 표시하는 `PageRotationActiveVisibility` 상태를 추가했다.
- `WORKLOG.md`: 원인, 변경 내용, 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 출하AMS 하단 상태표시창의 페이지 전환 상태 문구와 초록색 원 표시에만 영향이 있다.
- 실제 페이지 전환 타이머, 마스터·서브 동기화, 경매 화면과 `users.xml` 설정은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 표시 매핑 확인: `BoardPage=1 → 항목 숨김`, `BoardPage=2~4 + IsFrozen=false → 페이지전환 + 초록색 원`, `BoardPage=2~4 + IsFrozen=true → 페이지고정 + 원 숨김`: 코드 확인 완료.
- 실제 출하AMS 상태표시창 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `feat: 페이지 전환 동작 상태 표시 추가`


## 2026-09-27 - 정읍축협 진행 화면 개체번호 간격 복원

### 작업 일자

- 2026-09-27

### 작업 목적

- 정읍축협 128×128 진행 1페이지에서 전체 개체번호와 가운데 네 자리 단축번호 오버레이의 간격을 정상화한다.

### 원인

- 정읍축협 전용 `JeongeupRun`이 공통 `Standard_non_X_Run1_1`로 통합되는 과정에서 전체번호와 단축번호가 모두 X=40에 배치됐다.
- 단축번호의 검은 배경이 전체번호 앞부분을 덮어 `2108 4098 7`의 그룹 간 공백과 강조 위치가 어긋났다.
- 통합 전 `JeongeupRun`은 전체번호 X=44, 단축번호 X=72로 두 항목 사이에 28px 간격을 사용했다.
- 좌표 복원 후 단축번호 Width `30`이 뒤 공백까지 덮어 마지막 한 자리가 단축번호에 붙어 보였다.
- 단축번호에는 Width가 지정돼 있지만 `HorizontalAlignment="Left"`가 없어 Margin.Left 값이 절대 X좌표가 아니라 남은 Grid 영역의 가운데 정렬에 영향을 주며, Margin을 높일수록 실제 글자가 오른쪽으로 이동했다.
- 추가 현장 확인에서 Margin.Left `60`도 단축번호가 오른쪽에 있어 왼쪽 미세 보정이 필요했다.
- Margin.Left `56` 적용 화면에서도 소폭 왼쪽 보정이 추가로 필요했다.
- Margin.Left만 반복 조정하면 Width가 지정된 TextBlock의 Grid 가운데 정렬 효과 때문에 변경량과 실제 이동량이 일치하지 않았다.
- 절대 좌표 X=72 적용 화면에서도 단축번호가 약 2px 오른쪽에 있어 추가 왼쪽 보정이 필요했다.
- 절대 좌표 X=70 적용 화면에서는 단축번호가 1px 왼쪽에 있어 앞쪽 공백이 지워졌다.
- X=71 조정도 Aliased 픽셀 반올림으로 X=70과 거의 동일하게 보였으며, 위치보다 Width `28`인 검은 배경이 앞뒤 공백 중 하나를 덮는 것이 핵심 원인이었다.
- Width `26` 적용 후 현장 화면 기준 단축번호를 1px 오른쪽으로 미세 조정할 필요가 있었다.
- 절대 좌표 X=73 적용 후에도 현장 화면 기준 1px 오른쪽 미세 조정이 필요했다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/Standard_non_X_Run1_1.xaml`: 전체번호 Margin.Left를 `40`에서 `44`로, 단축번호 Margin.Left를 `40`에서 `72`로 변경해 기존 정읍 전용 좌표를 복원했다.
- 단축번호 Width를 `30`에서 `28`로 줄여 가운데 네 자리 뒤 공백이 검은 배경에 가려지지 않도록 보정했다.
- 단축번호에 `HorizontalAlignment="Left"`를 추가해 Grid 가운데 정렬 영향을 제거하고, 현장 화면 기준 절대 좌표 `74`로 최종 보정했다.
- 단축번호 Width를 `28`에서 `26`으로 줄여 앞쪽 공백을 유지하면서 뒤쪽 한 자리 앞의 공백도 드러나도록 보정했다.
- 이후 Margin.Left 1px 변경이 실제 표시 위치 1px 변경으로 일치하도록 좌표 기준을 안정화했다.
- 단축번호 ZIndex, 동적 전경·배경과 나머지 진행 화면 레이아웃은 유지했다.
- `WORKLOG.md`: 원인, 기존 좌표 근거, 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 정읍축협 사업장 코드 `8808990656953`의 128×128 진행 1페이지에 적용된다.
- 진행 2페이지, 낙찰·유찰 화면, 다른 축협 뷰 및 C# 파싱·분기에는 적용되지 않는다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 전체번호 Margin.Left `44`, 단축번호 Margin.Left `74`, Width `26` 및 `HorizontalAlignment="Left"` 적용 여부를 확인했다.
- 실제 정읍축협 128×128 전광판에서 `2108 4098 7` 간격과 오버레이 위치 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 정읍 진행 화면 개체번호 간격 복원`


## 2026-09-27 - 공통 낙찰 화면 단축번호 좌측 위치 보정

### 작업 일자

- 2026-09-27

### 작업 목적

- 정읍축협을 포함해 공통 `QQuriSold`를 사용하는 128×128 낙찰 화면에서 전체 개체번호의 가운데 네 자리와 단축번호 오버레이 위치를 맞춘다.

### 원인

- 단축번호가 전체 번호와 동일한 Grid 영역에서 가운데 정렬되면서 왼쪽 Margin `22`가 적용되어 전체 번호의 가운데 네 자리보다 오른쪽에 표시됐다.
- 정읍축협은 중량을 표시하는 `AnseongSold`가 아니라 중량 항목이 비활성화된 공통 `QQuriSold`를 사용한다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/QQuriSold.xaml`: `EntityNumberShort`의 Margin.Left를 `22`에서 `20`으로 변경해 실제 표시 위치를 약 1px 왼쪽으로 이동했다.
- 단축번호의 FontSize `13`, FontWeight `Normal`, 동적 전경·배경과 전체 개체번호 위치는 유지했다.
- `WORKLOG.md`: 정읍 화면 분기 확인, 수정값, 공통 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 정읍축협을 포함해 전용 낙찰 뷰가 없고 `IsShowQQuri`가 `X`가 아니며 `BidderName`이 `B`가 아닌 조합의 128×128 공통 낙찰 화면에 적용된다.
- `QQuriSold_v3`, `AnseongSold`, 다른 전용 낙찰 화면, 진행·유전·유찰 화면 및 C# 소스에는 적용되지 않는다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- `EntityNumberShort`의 Margin.Left `20`, FontSize `13`, FontWeight `Normal` 유지 여부를 확인했다.
- 실제 정읍축협 128×128 전광판에서 `2109 9860 0` 단축번호 오버레이 위치 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 공통 낙찰 화면 단축번호 위치 보정`


## 2026-09-27 - 영주축협 유찰 화면 개체번호 정렬 보정

### 작업 일자

- 2026-09-27

### 작업 목적

- 영주축협 128×128 유찰 화면에서 축종 행의 시작 위치를 최저·중량·육종 항목과 맞추고 단축번호 오버레이를 오른쪽으로 보정한다.

### 원인

- 영주축협 사업장 코드 `8808990687094`는 전용 유찰 분기가 없어 공통 `QQuriUnSold`를 사용한다.
- 축종은 X=0에서 시작하지만 최저·중량·육종 항목은 X=2에서 시작해 왼쪽 정렬선이 달랐다.
- 고정 폭 26px인 단축번호의 오버레이가 전체 개체번호의 가운데 네 자리보다 왼쪽에 표시됐다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/QQuriUnSold.xaml`: 축종 Margin.Left를 `0`에서 `2`로 변경해 다른 정보 항목의 시작선과 맞췄다.
- 단축번호 Margin.Left를 `44`에서 `46`으로 변경해 고정 폭 가운데 배치 기준 실제 표시 위치를 약 1px 오른쪽으로 이동했다.
- 전체 개체번호 Margin.Left `44`, 단축번호 Width `26`, 색상·배경과 다른 유찰 항목은 유지했다.
- `WORKLOG.md`: 영주 설정, 수정값, 공통 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 영주축협과 공통 `QQuriUnSold`를 사용하는 다른 축협의 128×128 소 유찰 화면에 적용된다.
- 영주축협 진행 화면은 `BoardPage=1`, `IsShowQQuri=N`에 따라 `Standard_non_QQuri_Run1`을 사용하며 변경하지 않았다.
- `BidderName=B`에 따른 `QQuriSold_v3` 낙찰 화면, 염소 유찰 화면 및 C# 소스는 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 축종 Margin.Left `2`, 전체 번호 Margin.Left `44`, 단축번호 Margin.Left `46` 및 Width `26`을 확인했다.
- 실제 영주축협 128×128 전광판에서 축종 시작선과 단축번호 오버레이 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 영주 유찰 화면 개체번호 정렬 보정`


## 2026-09-27 - 화순축협 낙찰 화면 단축번호 위치 보정

### 작업 일자

- 2026-09-27

### 작업 목적

- 화순축협 128×128 소 낙찰 화면에서 전체 개체번호의 가운데 네 자리와 단축번호 오버레이 위치를 맞춘다.

### 원인

- 소 개체번호의 단축번호가 전체 번호의 가운데 네 자리보다 약 1px 왼쪽에 표시됐다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/CustomAuctionSold/HwasunSold.xaml`: 소 화면 `EntityNumberShort`의 Margin.Left를 `73`에서 `74`로 변경했다.
- 전체 번호, 글꼴, 색상, 배경, 굵기와 염소 화면의 단축번호 위치는 유지했다.
- `WORKLOG.md`: 수정값, 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 화순축협 사업장 코드 `8808990661315`의 128×128 소 낙찰 화면에만 적용된다.
- 염소 낙찰 화면, 다른 축협 화면, 진행·유찰 화면 및 C# 소스에는 적용되지 않는다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 소 단축번호 Margin.Left가 `74`이고 염소 단축번호 Margin.Left는 `40`으로 유지된 것을 확인했다.
- 실제 화순축협 128×128 전광판에서 가운데 네 자리 오버레이 위치 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 화순 낙찰 화면 단축번호 위치 보정`


## 2026-09-27 - 안성 계열 낙찰 화면 개체번호 간격 조정

### 작업 일자

- 2026-09-27

### 작업 목적

- 안성축협 낙찰 화면의 축종과 개체번호 간격을 현장 정상 확인된 공통 `QQuriSold` 배치와 동일하게 맞춘다.

### 원인

- `AnseongSold`의 1행은 가로 `StackPanel`에서 축종의 실제 글자 폭 뒤에 개체번호를 이어 붙여 축종에 따라 개체번호 시작 위치가 달라졌다.
- 공통 `QQuriSold`는 Grid 열을 기준으로 축종, 전체 번호와 단축번호를 고정 배치해 1행 간격이 안정적이었다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/CustomAuctionSold/AnseongSold.xaml`: 1행 가로 `StackPanel`을 공통 `QQuriSold`와 동일한 3개 `TextBlock` Grid 배치로 교체했다.
- 축종은 Grid 0~1열, FontSize 13, Margin `-2,-2,0,0`으로 설정하고 전체 번호와 단축번호는 Grid 2~4열에 가운데 정렬했다.
- 단축번호는 Margin.Left를 `22`에서 `18`로 이동하고 FontSize 13, FontWeight `Normal`과 기존 동적 전경·배경을 유지했다.
- 최저가와 낙찰가 값의 Margin.Left를 `9`에서 `7`로 조정하고, 라벨·값의 상단 여백은 0으로 유지해 큰 가격 글자의 상단 잘림을 방지했다.
- 중량 행의 3px 상단 여백을 제거하고 가격 행의 상단 여백도 0으로 맞춰 비육우·번식우 화면의 축종, 최저가, 낙찰가와 중량 행 간격을 균일하게 조정했다.
- 제목, 낙찰자 영역과 C# 분기·파싱은 변경하지 않았다.
- `WORKLOG.md`: 변경 원인, 공통 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- `AnseongSold`를 공유하는 안성, 남원, 음성, 파주연천, 문경, 이천, 익산군산, 고성, 평택, 상주, 논산계룡, 구미칠곡, 포항, 옥천의 128×128 낙찰 화면 1행에 적용된다.
- 공통 `QQuriSold`, 다른 전용 낙찰 화면, 진행·유찰 화면 및 C# 소스에는 적용되지 않는다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- `AnseongSold`의 축종 13px, 단축번호 Margin.Left 18, 가격 행 Margin.Top 0 및 중량 행 여백 속성을 확인했다.
- 실제 안성축협 128×128 전광판에서 송아지·비육우·번식우의 개체번호 간격, 가격 글자 잘림과 중량 행 간격이 정상 표출되는 것을 확인했다.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 안성 계열 낙찰 화면 개체번호 간격 조정`


## 2026-09-27 - 뿌리농가 미적용 진행 화면 1행 정렬 안정화

### 작업 일자

- 2026-09-27

### 작업 목적

- 안성축협을 포함해 `qquri=N`일 때 사용하는 128×128 진행 1페이지에서 축종, 혈통, 전체 개체번호와 단축번호를 전주축협에서 현장 검증된 글꼴·간격으로 표시한다.

### 원인

- 공통 `Standard_non_QQuri_Run1`의 1행은 비례폭 굴림 기본 글꼴과 기존 좌표를 사용해 `번식우 + 혈통 + 개체번호` 조합에서 간격과 단축번호 오버레이 정렬이 불안정했다.
- 루트에 레이아웃 반올림 및 디바이스 픽셀 맞춤 설정이 없어 렌더링 환경에 따라 글자 경계가 번질 여지가 있었다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/Standard_non_QQuri_Run1.xaml`: 1행의 축종·혈통·전체 번호·단축번호에 굴림체 11px와 좌측 정렬을 적용했다.
- 1행 좌표를 축종 X=0, 혈통 X=35, 전체 번호 X=58, 단축번호 X=86으로 조정하고 단축번호를 Width 22, FontWeight `Normal`로 설정했다.
- 루트에 `UseLayoutRounding="True"`와 `SnapsToDevicePixels="True"`를 적용했다.
- 바인딩, 동적 색상 리소스, 다른 표시행과 C# 분기·파싱은 변경하지 않았다.
- `WORKLOG.md`: 변경 원인, 공통 영향 범위와 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 별도 전용 진행 뷰가 없고 `qquri=N`으로 `Standard_non_QQuri_Run1`을 사용하는 모든 128×128 진행 1페이지에 적용된다.
- `StandardQQuri_Run1`, 전주 전용 `GimjeRun`, 진행 2페이지, 낙찰·유찰 화면 및 C# 소스에는 적용되지 않는다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- `git diff --check`: 공백 오류 없음. 기존 작업 파일을 포함한 줄바꿈 변환 경고만 확인했다.
- XAML에서 굴림체 11px, 좌측 정렬, 좌표 및 단축번호 Width·FontWeight 속성을 확인했다.
- 실제 안성축협 128×128 전광판에서 송아지·비육우·번식우의 혈통 및 `4-4-1` 개체번호 간격과 마지막 자리 정상 표출을 확인했다.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 뿌리농가 미적용 진행 화면 개체번호 정렬`


## 2026-09-27 - 128×128 낙찰 화면 단축번호 일반 굵기 확대 적용

### 작업 일자

- 2026-09-27

### 작업 목적

- 공통 `QQuriSold`에서 현장 확인된 단축번호 일반 굵기 표시를 장성 전용 화면을 제외한 실제 사용 중인 128×128 낙찰 화면에 확대 적용한다.

### 원인

- 일부 낙찰 화면의 `EntityNumberShort`에 `FontWeight="Bold"`가 남아 전체 개체번호와 단축번호의 글자 폭·굵기가 달랐다.
- 장성축협 전용 화면은 기존 표시 정책을 유지해야 하므로 변경 대상에서 제외했다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Standard_non_X_Sold.xaml`: 단축번호 FontWeight를 `Bold`에서 `Normal`로 변경했다.
- `CowAuctionSmall/Views/Size128_128/QQuriSold_Weight.xaml`: 단축번호 FontWeight를 `Bold`에서 `Normal`로 변경했다.
- `CowAuctionSmall/Views/Size128_128/CustomAuctionSold/JecheonDanyangSold.xaml`: 단축번호 FontWeight를 `Bold`에서 `Normal`로 변경했다.
- `CowAuctionSmall/Views/Size128_128/CustomAuctionSold/QQuriSold_v3.xaml`: 단축번호 FontWeight를 `Bold`에서 `Normal`로 변경했다.
- `CowAuctionSmall/Views/Size128_128/CustomAuctionSold/YecheonSold.xaml`: 단축번호 FontWeight를 `Bold`에서 `Normal`로 변경했다.
- 각 화면의 Margin, FontSize, 정렬, 전경·배경과 바인딩은 유지했다. `JangseongSold.xaml`은 변경하지 않았다.
- `WORKLOG.md`: 확대 적용 대상과 장성 제외 범위 및 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 영천 및 `IsShowQQuri=X` 기본 화면, 임실, 제천단양·보령, 곡성 및 낙찰자 이름+번호 화면, 예천의 128×128 낙찰 화면 단축번호에 적용된다.
- 장성축협 화면, 미사용 `QQuriSold_v2`, 도달하지 않는 염소용 블록, 진행·유찰 화면 및 C# 소스는 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 5개 대상 화면의 단축번호 FontWeight `Normal`과 기존 위치·크기·색상이 유지되고 장성 화면은 변경되지 않은 것을 확인했다.
- 실제 128×128 전광판에서 각 축협별 단축번호 위치 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 낙찰 화면 단축번호 굵기 통일`


## 2026-09-27 - 공통 낙찰 화면 단축번호 굵기 조정

### 작업 일자

- 2026-09-27

### 작업 목적

- 전주축협 김제·고산 가축시장을 포함해 공통 `QQuriSold`를 사용하는 128×128 낙찰 화면에서 단축번호의 과도한 굵기를 제거하고 위치 보정 전 기준 화면을 확보한다.

### 원인

- 단축번호에 `FontWeight="Bold"`가 적용돼 전체 개체번호와 글자 폭·굵기가 달라 오버레이 정렬을 판단하기 어려웠다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/QQuriSold.xaml`: `EntityNumberShort`의 FontWeight를 `Bold`에서 `Normal`로 변경했다. Margin `22`, FontSize `13`, 가운데 정렬, 전경·배경과 바인딩은 유지했다.
- `WORKLOG.md`: 공통 낙찰 화면 단축번호 굵기 변경과 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 전주축협 김제·고산 가축시장과 공통 `QQuriSold`를 사용하는 다른 축협의 128×128 낙찰 화면 단축번호에 적용된다.
- 전체 개체번호 위치, 낙찰 화면의 다른 항목, 진행·유찰 화면, 전용 낙찰 화면 및 C# 소스는 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 단축번호 FontWeight `Normal`과 기존 Margin `22`, FontSize `13`, 색상·배경·바인딩이 유지된 것을 확인했다.
- 실제 128×128 전광판에서 단축번호 일반 굵기와 전체 개체번호 오버레이 위치가 정상임을 사용자가 확인했다.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 공통 낙찰 화면 단축번호 굵기 조정`


## 2026-09-27 - 전주축협 단축번호 오버레이 시작점 보정

### 작업 일자

- 2026-09-27

### 작업 목적

- 전주축협 고산·김제 가축시장의 128×128 진행 1페이지에서 굴림체 11px 전체 번호의 가운데 네 자리와 단축번호 오버레이를 일치시킨다.

### 원인

- 전체 번호 X=58에서 굴림체 11px의 앞 네 자리와 공백 폭은 약 27.5px이므로 가운데 네 자리 시작점은 약 X=85.5이다.
- 단축번호 오버레이가 X=78에 있어 전체 번호 앞부분을 덮고 가운데 네 자리와 어긋났다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/GimjeRun.xaml`: 단축번호 Margin.Left를 `78`에서 `86`으로 이동했다. Width `22`, 굴림체 11px, 좌측 정렬, FontWeight `Normal`, ZIndex `150`, 전경·배경 및 바인딩은 유지했다.
- `WORKLOG.md`: 단축번호 오버레이 시작점 보정과 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 승인 범위에 없어 갱신하지 않았다.

### 영향 범위

- 사업장 코드 `8808990656441`을 사용하는 전주축협 고산·김제 가축시장의 128×128 진행 1페이지 1행 단축번호 위치에만 적용된다.
- 전체 번호·혈통·축종, 다른 행, C# 소스, 다른 화면 및 다른 축협은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 단축번호 X=86과 기존 Width `22`, 굴림체 11px, 좌측 정렬 및 스타일이 유지된 것을 확인했다.
- 실제 128×128 전광판에서 `송아지 혈통 2168 5501 7`의 혈통·전체 번호 비겹침, `5501` 오버레이 일치 및 마지막 숫자 `7` 비잘림을 사용자가 확인했다.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 전주축협 단축번호 오버레이 위치 보정`


## 2026-09-27 - 전주축협 1행 굴림체·픽셀 정렬 적용

### 작업 일자

- 2026-09-27

### 작업 목적

- 전주축협 고산·김제 가축시장의 128×128 진행 1페이지 1행을 고정폭 글꼴과 명시적 좌측 정렬로 구성해 혈통·개체번호 겹침, 마지막 숫자 잘림 및 단축번호 오버레이 잔재를 해소한다.
- C# 개체번호 파싱과 다른 화면을 변경하지 않고 전주 진행 화면에만 적용한다.

### 원인

- 비고정폭 굴림 12px과 단축번호의 기본 정렬 상태에서는 글자 폭과 실제 배치 시작점이 일정하지 않았다.
- 픽셀 라운딩이 명시되지 않아 작은 전광판에서 서브픽셀 경계가 글자 잔재나 겹침으로 보일 가능성이 있었다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/GimjeRun.xaml`: 1행 축종·혈통·전체 번호·단축번호에 `FontFamily="굴림체"`, `FontSize="11"`, `HorizontalAlignment="Left"`를 적용했다. 좌표를 축종 X=0, 혈통 X=35, 전체 번호 X=58, 단축번호 X=78과 Width `22`로 조정했다. UserControl에 `UseLayoutRounding="True"`와 `SnapsToDevicePixels="True"`를 적용했다.
- `WORKLOG.md`: 고정폭 글꼴, 명시적 좌측 정렬 및 픽셀 라운딩 적용 내용과 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 승인 범위에 없어 갱신하지 않았다.

### 영향 범위

- 사업장 코드 `8808990656441`을 사용하는 전주축협 고산·김제 가축시장의 128×128 진행 1페이지 1행 및 해당 뷰의 픽셀 라운딩에 적용된다.
- C# 파싱, 다른 행의 폰트·좌표, 다른 화면 및 다른 축협은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 1행 네 요소의 굴림체 11px·좌측 정렬, 지정 좌표·폭 및 UserControl 픽셀 라운딩이 적용된 것을 확인했다.
- 실제 128×128 전광판에서 `송아지 혈통 2168 5501 7` 비겹침·비잘림과 오버레이 잔재 제거 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 전주축협 개체번호 1행 고정폭 정렬 적용`


## 2026-09-27 - 전주축협 혈통·개체번호 겹침 해소

### 작업 일자

- 2026-09-27

### 작업 목적

- 전주축협 고산·김제 가축시장의 128×128 진행 1페이지에서 혈통과 전체 개체번호 첫 숫자가 겹쳐 보이는 현상을 해소한다.
- 전체 번호와 단축번호의 상대 거리를 유지하면서 두 요소를 함께 우측으로 이동한다.

### 원인

- 혈통 X=37과 전체 번호 X=57 사이의 간격이 실제 글리프 폭보다 좁아 전체 번호 첫 숫자가 약 2픽셀 겹쳤다.
- 화면 우측에는 마지막 숫자 뒤로 이동 가능한 여유 공간이 있었다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/GimjeRun.xaml`: 전체 번호를 X=57에서 X=62로, 단축번호를 X=74에서 X=79로 각각 5픽셀 우측 이동했다. 두 요소의 상대 거리 17픽셀과 단축번호 Width `26`, FontWeight `Normal`, ZIndex `150`, 전경·배경 및 바인딩은 유지했다. 혈통 X=37은 변경하지 않았다.
- `WORKLOG.md`: 혈통·전체 번호 겹침 해소를 위한 5픽셀 평행 이동과 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 승인 범위에 없어 갱신하지 않았다.

### 영향 범위

- 사업장 코드 `8808990656441`을 사용하는 전주축협 고산·김제 가축시장의 128×128 진행 1페이지 1행 전체 번호·단축번호 위치에만 적용된다.
- 혈통, 다른 행, 다른 화면, C# 소스 및 다른 축협은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 혈통 X=37, 전체 번호 X=62, 단축번호 X=79, Width `26`, 상대 거리 17픽셀 및 기존 스타일이 유지된 것을 확인했다.
- 실제 128×128 전광판에서 혈통·`2168` 비겹침과 마지막 숫자 `7` 비잘림 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 전주축협 혈통과 개체번호 간격 조정`


## 2026-09-27 - 전주축협 단축번호 오버레이 최종 좌표·폭 보정

### 작업 일자

- 2026-09-27

### 작업 목적

- 전주축협 고산·김제 가축시장의 128×128 진행 1페이지에서 단축번호 왼쪽에 노출되는 밑바탕 은색 글자 잔재를 제거한다.
- 단축번호 `5501` 네 자리를 잘림 없이 온전히 표시한다.

### 원인

- 단축번호 오버레이 X=77이 밑바탕 `5501` 시작점 X=74보다 3픽셀 오른쪽에 있어 은색 글자 일부가 노출됐다.
- Width `22`가 단축번호 네 자리 글자 폭보다 좁아 마지막 `1`이 잘렸다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/GimjeRun.xaml`: 단축번호를 X=77에서 X=74로 3픽셀 좌측 이동하고 Width를 `22`에서 `26`으로 확장했다. 혈통 X=37, 전체 번호 X=57과 단축번호 FontWeight `Normal`, ZIndex `150`, 전경·배경 및 바인딩은 유지했다.
- `WORKLOG.md`: 은색 잔재 제거와 단축번호 네 자리 표출을 위한 좌표·폭 보정 및 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 승인 범위에 없어 갱신하지 않았다.

### 영향 범위

- 사업장 코드 `8808990656441`을 사용하는 전주축협 고산·김제 가축시장의 128×128 진행 1페이지 1행 단축번호 오버레이에만 적용된다.
- 혈통·전체 번호 위치, 다른 행, 다른 화면 및 다른 축협은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 혈통 X=37, 전체 번호 X=57, 단축번호 X=74, Width `26`, FontWeight `Normal`, ZIndex `150`과 동적 색상·배경이 유지된 것을 확인했다.
- 실제 128×128 전광판에서 은색 잔재 제거, `5501` 네 자리 및 마지막 숫자 `7` 비침범 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 전주축협 단축번호 오버레이 좌표와 폭 보정`


## 2026-09-27 - 전주축협 혈통·단축번호 오버레이 정밀 보정

### 작업 일자

- 2026-09-27

### 작업 목적

- 전주축협 고산·김제 가축시장의 128×128 진행 1페이지에서 단축번호 왼쪽에 노출되는 밑바탕 은색 글자 잔재를 제거한다.
- 축종 `송아지`와 혈통 텍스트의 겹침을 해소한다.

### 원인

- 단축번호 오버레이 X=80이 밑바탕 `5501`보다 오른쪽에 있어 왼쪽 은색 글자 일부가 노출됐다.
- 혈통 X=35가 축종 `송아지` 영역과 맞닿아 글자가 겹쳤다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/GimjeRun.xaml`: 혈통 X좌표를 `35`에서 `37`로 복원하고, 단축번호를 X=80에서 X=77로 이동하며 Width를 `24`에서 `22`로 조정했다. 전체 번호 X=57과 단축번호 FontWeight `Normal`, ZIndex `150`, 전경·배경 및 바인딩은 유지했다.
- `WORKLOG.md`: 혈통 위치 복원과 단축번호 오버레이 좌표·폭 보정의 원인·영향·검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 승인 범위에 없어 갱신하지 않았다.

### 영향 범위

- 사업장 코드 `8808990656441`을 사용하는 전주축협 고산·김제 가축시장의 128×128 진행 1페이지 1행 혈통·단축번호에만 적용된다.
- 전체 번호 위치, 다른 행, 다른 화면 및 다른 축협은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 혈통 X=37, 전체 번호 X=57, 단축번호 X=77, Width `22`, FontWeight `Normal`, ZIndex `150`과 동적 색상·배경이 유지된 것을 확인했다.
- 실제 128×128 전광판에서 은색 잔재 제거, `5501` 네 자리 및 마지막 숫자 `7`, 축종·혈통 비겹침 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 전주축협 혈통과 단축번호 위치 정밀 보정`


## 2026-09-27 - 전주축협 개체번호 1행 좌측 이동·오버레이 폭 확장

### 작업 일자

- 2026-09-27

### 작업 목적

- 전주축협 고산·김제 가축시장의 128×128 진행 1페이지에서 단축번호 `5501`의 마지막 숫자 누락과 전체 개체번호 마지막 숫자 `7`의 우측 잘림을 해소한다.
- 축종 `송아지`와의 충돌을 피하면서 혈통·전체 번호·단축번호를 동일 방향으로 이동해 상대 위치를 유지한다.

### 원인

- 단축번호 Width `20`은 4자리 글자 폭보다 좁아 마지막 숫자가 잘렸다.
- 전체 번호가 X=60에서 시작해 128px 전광판 우측 경계에 걸렸다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/GimjeRun.xaml`: 1행 혈통을 X=35, 전체 번호를 X=57, 단축번호를 X=80으로 3픽셀씩 좌측 이동했다. 단축번호 Width를 `20`에서 `24`로 확장했으며 FontWeight `Normal`, ZIndex `150`, 전경·배경과 바인딩은 유지했다.
- `WORKLOG.md`: 단축번호 폭 확장과 1행 좌표 보정의 원인·영향·검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 승인 범위에 없어 갱신하지 않았다.

### 영향 범위

- 사업장 코드 `8808990656441`을 사용하는 전주축협 고산·김제 가축시장의 128×128 진행 1페이지 1행 혈통·전체 번호·단축번호에만 적용된다.
- 다른 행, 공통 진행 화면, 다른 축협 및 낙찰·유찰 화면은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 혈통 X=35, 전체 번호 X=57, 단축번호 X=80, Width `24`, FontWeight `Normal`, ZIndex `150`과 동적 색상·배경이 유지된 것을 확인했다.
- 실제 128×128 전광판에서 `2168 5501 7` 전체 표출과 축종·혈통 비겹침 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 전주축협 개체번호 1행 폭과 좌표 보정`


## 2026-09-27 - 전주축협 개체번호 오버레이 일반 굵기·폭 조정

### 작업 일자

- 2026-09-27

### 작업 목적

- 전주축협 고산·김제 가축시장의 128×128 진행 1페이지에서 단축번호 Bold 글리프 팽창으로 발생한 공백 지워짐과 마지막 숫자 잘림을 해소한다.
- `송아지`와 `혈통`의 텍스트 간격을 정상화한다.

### 원인

- 단축번호에 적용한 Bold가 글리프 폭을 키워 26px 오버레이 안에서 주변 공백과 마지막 숫자 영역을 침범했다.
- 반복 좌표 보정 과정에서 혈통과 축종의 간격도 좁아져 텍스트 충돌 위험이 있었다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/GimjeRun.xaml`: 1행 혈통 Margin.Left를 `37`, 전체 번호를 `60`, 단축번호를 `83`으로 설정했다. 단축번호 Width를 `26`에서 `20`으로 축소하고 FontWeight를 `Bold`에서 `Normal`로 원복했다. ZIndex `150`, 전경·배경과 바인딩은 유지했다.
- `WORKLOG.md`: 단축번호 일반 굵기 원복과 오버레이 폭 조정에 따른 1행 정합성 정상화 및 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 사업장 코드 `8808990656441`을 사용하는 전주축협 고산·김제 가축시장의 128×128 진행 1페이지 1행 혈통·전체 번호·단축번호에만 적용된다.
- 축종 선언, 다른 행, 공통 진행 화면, 다른 축협 및 낙찰·유찰 화면은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 혈통 X=37, 전체 번호 X=60, 단축번호 X=83이며 단축번호 Width `20`, FontWeight `Normal`, ZIndex `150`과 동적 색상·배경이 적용된 것을 확인했다.
- 실제 128×128 전광판에서 공백 보존, 마지막 숫자 비잘림 및 `송아지/혈통` 비겹침 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 전주축협 단축번호 굵기와 오버레이 폭 조정`


## 2026-09-26 - 전주축협 개체번호 1행 초미세 좌표 보정

### 작업 일자

- 2026-09-26

### 작업 목적

- 전주축협 고산·김제 가축시장의 128×128 진행 1페이지에서 `송아지`와 `혈통` 글자 충돌을 해소한다.
- 단축번호 오버레이 잔재와 마지막 숫자 붙음·잘림을 함께 방지한다.

### 원인

- 혈통을 X=34로 이동하면서 폭 35px인 `송아지` 영역과 겹쳤다.
- 전체 번호와 단축번호의 직전 좌표 X=57/X=80은 23픽셀 상대 거리는 유지했지만 실제 글리프와 오버레이 정렬에 오차가 있었다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/GimjeRun.xaml`: 1행 혈통의 Margin.Left를 `34`에서 `36`, 전체 번호를 `57`에서 `58`, 단축번호를 `80`에서 `81`로 보정했다. 전체 번호와 단축번호의 상대 거리 `23` 및 단축번호 Width `26`, `FontWeight="Bold"`, ZIndex `150`, 전경·배경과 바인딩은 유지했다.
- `WORKLOG.md`: 축종·혈통 충돌과 개체번호 정렬 문제 해소를 위한 초미세 좌표 보정 및 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 사업장 코드 `8808990656441`을 사용하는 전주축협 고산·김제 가축시장의 128×128 진행 1페이지 1행 혈통·전체 번호·단축번호에만 적용된다.
- 축종 선언, 다른 행, 공통 진행 화면, 다른 축협 및 낙찰·유찰 화면은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 혈통 X=36, 전체 번호 X=58, 단축번호 X=81이며 번호 간 상대 거리 23픽셀과 단축번호 Width `26`, Bold, ZIndex `150`이 유지된 것을 확인했다.
- 실제 128×128 전광판에서 `송아지/혈통` 비겹침, 오버레이 잔재 제거 및 마지막 숫자 비잘림 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 전주축협 개체번호 1행 좌표 정밀 보정`


## 2026-09-26 - 전주축협 개체번호 1행 3픽셀 좌측 이동

### 작업 일자

- 2026-09-26

### 작업 목적

- 전주축협 고산·김제 가축시장의 128×128 진행 1페이지에서 개체번호 마지막 숫자의 우측이 외곽 테두리에 걸려 잘리는 현상을 해소한다.
- 전체 번호와 단축번호 사이의 검증된 상대 거리 23픽셀을 유지한다.

### 원인

- 굴림 12px 전체 개체번호가 X=60에서 시작해 가용 폭 124px의 우측 경계에 마지막 숫자가 걸렸다.
- 전체 번호만 이동하면 혈통과 단축번호의 정렬 및 오버레이 상대 위치가 달라지므로 1행 관련 요소의 평행 이동이 필요했다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/GimjeRun.xaml`: 1행 혈통의 Margin.Left를 `37`에서 `34`, 전체 번호를 `60`에서 `57`, 단축번호를 `83`에서 `80`으로 각각 3픽셀 좌측 이동했다. 단축번호 Width `26`, `FontWeight="Bold"`, ZIndex, 전경·배경 및 바인딩은 유지했다.
- `WORKLOG.md`: 개체번호 마지막 자리 잘림 해결을 위한 1행 평행 이동과 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 사업장 코드 `8808990656441`을 사용하는 전주축협 고산·김제 가축시장의 128×128 진행 1페이지 1행 혈통·전체 번호·단축번호에만 적용된다.
- 축종, 다른 행, 공통 진행 화면, 다른 축협 및 낙찰·유찰 화면은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- 혈통 X=34, 전체 번호 X=57, 단축번호 X=80이며 전체 번호와 단축번호의 상대 거리 23픽셀 및 단축번호 Width `26`, Bold가 유지된 것을 확인했다.
- 실제 128×128 전광판에서 마지막 숫자 비잘림과 단축번호 오버레이 정렬 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 전주축협 개체번호 1행 우측 잘림 보정`


## 2026-09-26 - 전주축협 단축번호 오버레이 최종 좌표 보정

### 작업 일자

- 2026-09-26

### 작업 목적

- 전주축협 고산·김제 가축시장의 128×128 진행 1페이지에서 단축번호 왼쪽에 느낌표처럼 남는 밑바탕 은색 글자 잔재를 제거한다.

### 원인

- 단축번호 오버레이가 X=85에서 시작해 밑바탕 가운데 첫 글자의 실제 시작점인 X=83보다 2픽셀 오른쪽에 있었다.
- 이 차이로 밑바탕 은색 `5`의 왼쪽 수직선 일부가 노출됐다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/GimjeRun.xaml`: 1행 `EntityNumberShort`의 Margin을 `85,0,0,110`에서 `83,0,0,110`으로 조정했다. Width `26`, 전경·배경, 바인딩 및 `FontWeight="Bold"`는 유지했다.
- `WORKLOG.md`: 느낌표 형태의 잔재 제거를 위한 최종 X좌표 보정과 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 사업장 코드 `8808990656441`을 사용하는 전주축협 고산·김제 가축시장의 128×128 진행 1페이지 1행 단축번호에만 적용된다.
- 전체 개체번호, 단축번호 폭·색상·굵기, 다른 행, 공통 진행 화면, 다른 축협 및 낙찰·유찰 화면은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- `EntityNumberShort`의 X좌표가 83으로 변경되고 Width `26`, 동적 색상, 배경 및 Bold가 유지된 것을 확인했다.
- 실제 128×128 전광판에서 느낌표 형태의 은색 잔재 제거와 가운데 4자리 완전 덮임 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 전주축협 단축번호 오버레이 최종 좌표 보정`


## 2026-09-26 - 전주축협 단축번호 오버레이 X좌표 정밀 보정

### 작업 일자

- 2026-09-26

### 작업 목적

- 전주축협 고산·김제 가축시장의 128×128 진행 1페이지에서 단축번호 왼쪽에 노출되는 밑바탕 은색 글자 잔재를 제거한다.

### 원인

- 단축번호 오버레이가 X=88에서 시작해 밑바탕 전체 번호의 가운데 4자리 시작점인 X=85보다 3픽셀 오른쪽으로 치우쳤다.
- 이 차이로 가운데 첫 글자 왼쪽에 밑바탕 은색 글자의 일부가 노출됐다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/GimjeRun.xaml`: 1행 `EntityNumberShort`의 Margin을 `88,0,0,110`에서 `85,0,0,110`으로 조정했다. Width `26`과 `FontWeight="Bold"`는 유지했다.
- `WORKLOG.md`: 오버레이 좌측 틈새 제거를 위한 X좌표 정밀 보정과 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 사업장 코드 `8808990656441`을 사용하는 전주축협 고산·김제 가축시장의 128×128 진행 1페이지 1행 단축번호에만 적용된다.
- 전체 개체번호, 단축번호 폭과 굵기, 다른 행, 공통 진행 화면, 다른 축협 및 낙찰·유찰 화면은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` (`CowAuctionSmall` 폴더에서 실행): 성공, 오류 0개 / 기존 경고 102개.
- `EntityNumberShort`의 X좌표가 85로 변경되고 Width `26`, Bold 및 나머지 바인딩이 유지된 것을 확인했다.
- 실제 128×128 전광판에서 은색 글자 잔재 제거와 가운데 4자리 완전 덮임 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 전주축협 단축번호 오버레이 좌표 정밀 보정`


## 2026-09-26 - 전주축협 진행 1페이지 단축번호 오버레이 보정

### 작업 일자

- 2026-09-26

### 작업 목적

- 전주축협 고산·김제 가축시장의 128×128 진행 1페이지에서 전체 개체번호의 첫 번째 공백을 보존한다.
- 가운데 단축 4자리를 굵은 흰색 글자로 선명하게 강조한다.

### 원인

- 전체 개체번호가 X=60에서 시작하는 상태에서 단축번호의 검은 배경이 X=80, Width=30으로 겹쳐 첫 4자리 다음 공백까지 덮었다.
- 검은 화면 배경과 단축번호의 검은 배경이 같고 전체 번호 Silver와 단축번호 White의 대비도 작아 강조가 불분명했다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/GimjeRun.xaml`: 1행 `EntityNumberShort`의 Margin을 `88,0,0,110`, Width를 `26`으로 보정하고 `FontWeight="Bold"`를 적용했다.
- `WORKLOG.md`: 단축번호 오버레이 보정 원인, 변경 범위 및 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 사업장 코드 `8808990656441`을 사용하는 전주축협 고산·김제 가축시장의 128×128 진행 1페이지 1행 단축번호에만 적용된다.
- 전체 개체번호 바인딩과 좌표, 임신 미표시 설정, 다른 행, 공통 진행 화면, 다른 축협 및 낙찰·유찰 화면은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall\CowAuctionSmall.csproj --configuration Debug --no-restore`: 성공, 오류 0개 / 기존 경고 102개.
- `EntityNumberShort`에 X=88, Width=26, Bold가 적용되고 전체 번호 선언이 유지된 것을 확인했다.
- 실제 128×128 전광판에서 첫 번째 공백 보존, 가운데 4자리 강조 및 마지막 숫자 비잘림 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 전주축협 단축번호 오버레이 위치 보정`


## 2026-09-26 - 전주축협 진행 1페이지 개체번호 레이아웃 동기화

### 작업 일자

- 2026-09-26

### 작업 목적

- 전주축협 고산·김제 가축시장의 128×128 진행 1페이지 1행 개체번호를 `Standard_non_QQuri_Run1`과 동일한 위치와 겹침 순서로 표시한다.
- 전체 개체번호의 4-4-1 띄어쓰기를 유지하면서 가운데 단축 4자리를 별도 색상과 배경으로 강조한다.

### 원인

- `GimjeRun` 1행의 축종·혈통·전체 개체번호·단축번호 Margin과 ZIndex가 정상 현장 화면인 `Standard_non_QQuri_Run1`과 달라 단축번호 강조가 가려지고 우측 끝자리가 잘릴 수 있었다.
- 개체번호 파싱과 색상 리소스는 정상이며 1행 XAML 배치 속성만 동기화가 필요했다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/GimjeRun.xaml`: 1행 축종과 혈통의 Margin을 각각 `0,0,0,0`, `37,0,0,0`으로 조정하고 ZIndex를 각각 `100`, `50`으로 설정했다. 전체 개체번호는 Margin `60,0,0,108`, ZIndex `100`, 단축번호는 Margin `80,0,0,110`, Width `30`, ZIndex `150` 및 기존 동적 전경·배경 리소스를 적용해 표준 N 화면과 동일하게 맞췄다.
- `WORKLOG.md`: 원인, 변경 범위 및 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 사업장 코드 `8808990656441`을 사용하는 전주축협 고산·김제 가축시장의 128×128 진행 1페이지 1행에만 적용된다.
- 임신 미표시 설정, 진행 2~7행 레이아웃과 바인딩, 공통 `Standard_non_QQuri_Run1`, 다른 축협 및 다른 경매 상태 화면은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall\CowAuctionSmall.csproj --configuration Debug --no-restore`: 성공, 오류 0개 / 기존 경고 102개.
- `GimjeRun.xaml` 1행의 네 선언이 지정된 Margin, Width, ZIndex 및 동적 색상 리소스를 사용하는지 확인했다.
- `git diff --check`: 공백 오류 없음. Git 줄바꿈 변환 안내만 확인했다.
- 실제 128×128 전광판에서 `2168 5224 8` 형식과 가운데 단축 4자리 강조 및 우측 끝자리 비잘림 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 전주축협 개체번호 1행 레이아웃 동기화`


## 2026-09-26 - 전주축협 고산·김제 진행 화면 표준 N 구성 적용

### 작업 일자

- 2026-09-26

### 작업 목적

- 동일한 사업장 코드를 사용하는 전주축협 고산·김제 가축시장의 128×128 진행 1페이지를 뿌리농가 N 표준 화면 구성으로 통일한다.
- 전주축협에서는 임신 정보가 수신되어도 `임신:` 항목을 표시하지 않는다.

### 원인

- 전주축협 고산·김제 가축시장은 모두 `뿌리농가=N`이지만 사업장 코드 `8808990656441`의 전용 분기로 인해 표준 N 화면이 아닌 `GimjeRun`을 사용했다.
- 공통 `Standard_non_QQuri_Run1`에는 임신 값이 있을 때 `임신:` 항목을 표시하는 기능이 있어 전주축협 요구사항과 한 항목이 달랐다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/Size128_128/Running/GimjeRun.xaml`: `Standard_non_QQuri_Run1`의 글꼴 렌더링, 개체번호·단축번호 구성 및 전체 행 좌표를 동일하게 적용하고 임신 표시 블록만 제외했다.
- `WORKLOG.md`: 전주축협 고산·김제 진행 화면 구성과 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 사업장 코드 `8808990656441`을 함께 사용하는 전주축협 고산·김제 가축시장의 128×128 진행 1페이지에 적용된다.
- 전주축협 진행 화면에서는 임신 값 유무와 관계없이 `임신:` 항목이 표시되지 않는다.
- 공통 `Standard_non_QQuri_Run1`, 사업장 분기, 하단 친자 배지와 친자확인 비고, 다른 축협, 낙찰·유찰 및 진행 2페이지는 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall\CowAuctionSmall.csproj --configuration Debug --no-restore`: 성공, 오류 0개 / 기존 경고 102개.
- `GimjeRun.xaml`과 `Standard_non_QQuri_Run1.xaml` 비교 결과 클래스명과 임신 표시 블록·리소스를 제외한 화면 구성 및 좌표가 동일함을 확인했다.
- WPF XAML 컴파일 성공 및 `git diff --check` 공백 오류 없음 확인. Git 줄바꿈 변환 안내만 확인했다.
- 고산·김제 실제 128×128 전광판에서 표준 N 화면 및 임신 미표시 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 전주축협 진행 화면에 표준 N 구성 적용`


## 2026-09-26 - 수원 영천 염소 진행 테두리 테스트 빌드 생성

### 작업 일자

- 2026-09-26

### 작업 목적

- 영천축협 전용 염소 일괄경매 진행 테두리를 수원축협의 실제 일괄경매 신호로 확인할 수 있는 별도 Release 테스트 빌드를 생성한다.
- 운영 소스에는 수원 테스트 조건을 남기지 않는다.

### 원인

- 기존 기능은 영천축협 코드 `8808990656687`에서만 활성화되어 일괄경매 방식이 동일한 수원축협 코드 `8808990656496`에서는 동작을 확인할 수 없었다.
- `users.XML`의 날짜 설정은 과거 출품자료만 조회하며 일괄경매 시작·종료 신호를 재생하지 않아 전체 흐름을 시험할 수 없다.

### 변경 파일 및 내용

- `CowAuctionSmall/Services/DisplaySelect.cs`: 별도 빌드 생성 중에만 수원 코드를 임시 허용한 뒤 빌드 완료 후 원복했다. 최종 소스 변경은 없다.
- `artifacts/YeongcheonGoatBorder_TEST_Suwon_20260930`: 수원 테스트용 Release 산출물을 생성했다. Git 제외 경로이며 출력 폴더의 기본 `Config`는 현장 배포용이 아니다.
- `WORKLOG.md`: 테스트 빌드 생성 및 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 테스트 산출물에서만 영천과 수원 코드의 128×128 염소 일괄경매 진행 화면에 고정 주황색 테두리가 적용된다.
- 일괄경매 방식 `10`, 염소 `5`, 진행 상태 `11` 조건은 유지했다.
- 상주·충주의 단일경매 방식 `20`, 다른 축종 및 운영 소스에는 영향이 없다.
- 현장 적용 시 수원 운영 폴더의 기존 `Config/users.XML`과 `Config/Board.XML`을 유지해야 한다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall\CowAuctionSmall.csproj --configuration Release --no-restore --output .\artifacts\YeongcheonGoatBorder_TEST_Suwon_20260930`: 성공, 오류 0개 / 기존 경고 102개.
- 산출물 버전: `1.0.9765.21563`.
- `CowAuctionSmall.dll`에서 영천 코드 `8808990656687`과 수원 코드 `8808990656496` 포함을 확인했다.
- `CowAuctionSmall.exe` SHA-256: `2D0A4A01C42718E516CD419DE2731D5A8AABCFF166754B204882319453065A55`.
- `CowAuctionSmall.dll` SHA-256: `41A00D0A7EA4462C6D3C072989DE6DC258C6D03C7CA4F3B731010C8773BA5BAD`.
- 빌드 후 `DisplaySelect.cs` 원복 및 Git diff 없음 확인.
- 수원 실제 장비에서 `AS 8004` 시작, 고정 주황색 테두리, `AS 8006` 종료 해제 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `docs: 수원 염소 테두리 테스트 빌드 기록`


## 2026-09-23 — 축산물 이력 백그라운드 조회와 진행 상태 안내

- 목적·승인: 백그라운드 조회 계획에 대한 사용자의 “수정 진행하자” 승인. 캐시와 API 대기를 분리하고 요청 제어·대상 우선순위·진행 상태를 개선한다. 전체 처리 완료 후 일괄 표시는 유지한다.
- 문제·원인: 조정자가 한 요청의 완료를 기다린 뒤 다음 캐시를 확인했고, 서비스가 HTTP 응답·재시도 대기 동안 단일 세마포어를 점유했다. 중간 처리 상태가 없어 조회 중인지 추가 문구가 없는지 구분하기 어려웠다.
- 변경 파일·내용:
  - `Application/AnimalHistory/AnimalHistoryService.cs`: 캐시 전용 조회, 결과 분류, 짧은 요청 허가·예산 잠금, 동시 수·시작 간격 분리, 재시도 중 슬롯 반환, 소비자 수에 따른 요청 공유·취소. 기존 당일 예산 키·중단 기록·캐시 형식 유지.
  - 신규 `Domain/AnimalHistory/HistoryLookupResult.cs`: 정상 캐시·성공·실패·중단과 사용 가능한 문구를 별도로 표현.
  - `Domain/Settings/AnimalTraceSettings.cs`: `MaxConcurrentRequests=1` 추가. 서비스는 1~4 범위 지원. 기존 최소 1초와 일일 로컬 제한 없음 유지. XML 파서와 운영 XML 변경 없음.
  - `Presentation/Display/AnimalHistoryBatchCoordinator.cs`: 백그라운드 캐시 선확보, 최대 4개 비동기 작업의 대기열, 최신 대상 세대·겹치는 요청 재사용, 임시/확정 결과 분리, 200ms 진행 알림 합치기와 최종 상태 즉시 전달.
  - 신규 `Presentation/Display/HistoryBatchProgress.cs`: 진행 단계·집계·표시 문구.
  - `ViewModels/MainWindowViewModel.cs`: 화면 위치 순 대상 전달, 진행 상태 구독·해제, 반영 실패 전달.
  - `Views/MainWindow.xaml`: 기존 하단 70픽셀 안에 이력 상태줄 추가. 경매 신호와 로그는 유지하며 로그 영역 높이가 한 줄 줄어듦.
  - 신규 `Validation/ReferenceAdoptionChecks/HistoryBackgroundChecks.cs`, `HistoryProgressUiChecks.cs`, 기존 `Program.cs`, `BindingChecks.cs`: 합성 API·캐시·진행률·실제 상태 영역 XAML 검증 연결.
  - `Docs/축산물이력_백그라운드조회_개선계획_2026-09-23.md`, `Docs/소유주이력_설정_검증.md`, 검증 README: 구현·설정·한계 기록.
- 보존·제한: 문구 생성 조건, 비고 색상·스크롤, 가격·경매 상태, 캐시 저장 항목·보존 기간은 유지한다. 공식 자료 검색과 로컬 가이드에서 계정의 동시 호출 허용 수치를 확인하지 못했으므로 운영은 동시 1건·최소 1초다. 비동기 작업 4개와 실제 HTTP 동시 수를 구분한다. 동시 수 설정은 현재 소스에서만 관리한다.
- 빌드·검증:
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -p:OutputPath=$env:TEMP\CowAuction-HistoryBackground-Debug -v:q -clp:ErrorsOnly`: 제품 컴파일 오류 0개·기존 경고 78개. 마지막 검사 파일 변경 후 증분 빌드는 오류·경고 0개.
  - 위 출력의 `ReferenceAdoptionChecks.dll history`: **64개** 통과. 캐시 우선 준비, 실제 요청 간격·동시 수·예약 예산, 중복 요청·취소, 최신 대상 전환과 겹침 재사용, UI 일괄 반영·실패·진행률 보호 포함.
  - 같은 출력의 `binding`: **3,485개**, `updates`: **55개**, `stage4`: **596개** 통과. 상태 영역은 실제 XAML과 생성자를 우회한 ViewModel로 검사하며 제품 App·Window의 통신·타이머는 시작하지 않음.
  - `dotnet build Validation/OwnerHistoryChecks/OwnerHistoryChecks.csproj --no-restore -p:OutputPath=$env:TEMP\CowAuction-HistoryBackground-Owner -v:q -clp:ErrorsOnly`: 오류·경고 0개. 위 출력의 `OwnerHistoryChecks.dll --offline`: **55+25개**, `--scroll-offline`: **18개** 통과.
  - `dotnet build CowAuctionSmall.csproj -c Release --no-restore -p:OutputPath=$env:TEMP\CowAuction-HistoryBackground-Release -v:q -clp:ErrorsOnly`: 오류 0개·기존 경고 78개.
  - 합성 캐시 1건·신규 3건, 응답 지연 1,500ms·요청 시작 간격 1,000ms에서 새 경로의 동시 1건 **4,515.3ms**, 동시 2건 **3,534.2ms**. 요청은 각각 3회, 문구 4건으로 동일. 실제 운영 API나 이전 제품 바이너리의 전후 성능 측정이 아님.
- Git 정보: `master`, HEAD `52c554d`, 시작 상태 206개 항목. 시작 사본과 SHA-256 목록으로 기존 변경·운영 XML·관련 없는 파일 보존 확인. commit hash `미생성`, push `미수행`.
- 추가 확인: 실제 운영 API의 호출 허용 조건·전체 수집 시간, 전광판에서 상태줄 가독성, 전체 앱 운영 실행·배포는 미수행. 정상 신규 조회는 기본 요청 간격이 유지되므로 전반적인 속도 개선율을 확정하지 않는다.


## 2026-09-23 — 성별 표시 ViewModel·XAML 이관과 세 글자 가로 압축

- 목적·승인: 수정된 성별 계획에 대한 사용자의 “좋아 진행하자” 승인. `미경산`·`비거세`를 원문으로 두 글자 폭에 표시하고 `프리마틴`은 `프리`로 표시한다.
- 문제·원인: 성별 스타일 조회용 `CopyGValues`가 전체 경매품을 복제했고 128×64·160×64 Converter는 Style을 반복 생성했다. 예천 Converter는 Grid·테두리·글자를 반복 생성하고 세 글자를 앞 두 글자로 잘랐다. V3는 이미 정적 스타일을 재사용하던 점을 구분했다.
- 변경 파일·내용:
  - `ViewModels/AuctionItemDisplayViewModel.cs`: 원본 스타일 키, 표시 문자열, 예천 등록구분 분기·숨김과 글자 길이 판단을 추가. 원본 파서와 데이터는 유지.
  - 신규 `Views/Common/SexDisplayStyles.xaml`, `App.xaml`: 프로필별 공통 스타일과 단방향 표시 바인딩 등록.
  - 신규 `Views/Common/SexOutlineDisplay.xaml`·`.xaml.cs`: 예천의 고정 UI 요소와 XAML 트리거. 연결 코드는 InitializeComponent만 수행.
  - 기존 View XAML 55개: 39개 화면의 활성 표시 41곳 이관 및 미사용 리소스 정리. 횡성의 46픽셀 폭과 기존 TranslateTransform을 유지. 성별 외 요소·속성은 작업 시작 사본과 XML 구조로 비교 확인.
  - `Domain/Auction/gValues.cs`: 다른 활성 사용처가 없는 `CopyGValues` 속성 제거. `Clone()`과 갱신 경계의 소유권은 유지.
  - `Presentation/Converters`의 `CowTypeToStyleConverter_128_64.cs`, `_160_64.cs`, `_V3.cs`, `_OutLine_V3.cs` 4개 제거.
  - 신규 `Validation/ReferenceAdoptionChecks/SexDisplayChecks.cs`, `SexStyleBaseline.json`, 기존 `ReferenceAdoptionChecks.csproj`, `BindingChecks.cs`, `UiChecks.cs`: 변경 전 스타일 76개, 실제 바인딩·교체·압축 초기화·테두리 재사용·geometry 잘림 검사.
  - `Validation/OwnerHistoryChecks/UiChecks.cs`: 일반 Application을 직접 만드는 검증 환경에 새 공통 스타일 등록.
  - `Docs/성별표시_구조개선계획_2026-09-23.md`, `Validation/ReferenceAdoptionChecks/README.md`: 완료 범위와 검증 안내.
- 영향 범위·구현 판단:
  - 일반 세 글자는 기존 두 글자 프로필 폭을 기준으로 가로 `2/3`, 횡성은 실제 폭에 맞춰 `46/72`, 예천 외곽선은 테두리 여백을 고려해 `0.6`을 적용. 세로 배율과 위치 이동은 유지.
  - 160×64의 프리마틴은 새 두 글자 문구에 맞춰 가로 배율 `0.5 → 0.7`; 기존 세로 `0.8` 유지. 원본 키로 색상을 선택한다.
  - 예천은 레이아웃 반올림으로 14포인트 외곽선 글자가 사라지는 경우를 확인해 해당 컨트롤의 반올림을 해제했다. 테두리 치수·두께·모서리는 유지.
  - 화순 유찰 2곳·상주 유찰 1곳의 기존 검정 기본 스타일은 별도 fallback으로 보존. 다른 색상 정책을 함께 수정하지 않았다.
  - NOTE·추가 이력, 가격·중량·임신, 개체번호, 파서·Netty·API·운영 설정은 변경하지 않았다.
- 빌드·테스트 결과:
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -p:OutputPath=$env:TEMP\CowAuction-SexDisplay-Debug -v:q -clp:ErrorsOnly`: 오류 0개, 기존 경고 78개.
  - 위 출력의 `ReferenceAdoptionChecks.dll binding`: **3,471개** 통과. 진행 32개·결과 37개와 공통 컨트롤의 실제 WPF 바인딩 검사.
  - 같은 출력의 `ReferenceAdoptionChecks.dll stage4`: **596개**, `updates`: **55개** 통과.
  - `dotnet build Validation/OwnerHistoryChecks/OwnerHistoryChecks.csproj --no-restore -p:OutputPath=$env:TEMP\CowAuction-SexDisplay-Owner -v:q -clp:ErrorsOnly`: 오류·경고 0개. 위 출력의 `OwnerHistoryChecks.dll --scroll-offline`: **18개** 통과.
  - `dotnet build CowAuctionSmall.csproj -c Release --no-restore -p:OutputPath=$env:TEMP\CowAuction-SexDisplay-Release -v:q -clp:ErrorsOnly`: 오류 0개, 기존 경고 78개.
  - 임시 오프라인 도구로 합성 데이터의 128×64·160×64·횡성·예천 대표 화면 16개를 렌더링하고 육안 확인. 제품 App·운영 통신은 시작하지 않았다.
  - 최종 검색에서 제거한 Converter·CopyGValues의 활성 C#/XAML 참조 없음. Git 전체 diff 검사에서 나온 기존 후행 공백은 시작 사본에도 존재하며 이번 추가 행의 후행 공백은 없다.
- Git 정보: `master`, HEAD `52c554d`, 시작 상태 201개 항목. 기존 변경을 시작 시점 사본과 구분해 보존. commit hash `미생성`, push `미수행`.
- 추가 확인: 실제 전광판·운영 통신·전체 앱 실행, 전후 성능 수치 비교, 운영 배포는 미수행. 이번 변경의 목적은 불필요한 복제·생성 제거와 표시 규칙 통일이며 속도 개선율을 측정한 결과는 아니다.


## 2026-09-23 — 가격·중량·임신 표시 판단의 ViewModel 이관

- 목적·승인: 사용자가 이관 목록의 3번까지 지정했다. 최저가·낙찰가, 중량, 임신 표시 판단을 공통 표시 ViewModel로 먼저 이관한다.
- 문제·원인: 숫자 서식과 결장 문구 처리가 한 Converter에 섞였고, 중량 Converter 2개의 빈 값 처리가 달랐다. Running과 Sold·UnSold의 DataContext도 달라 공통 표시 속성을 그대로 재사용할 수 없었다.
- 변경 파일·내용:
  - 신규 `ViewModels/AuctionItemDisplayViewModel.cs`: 원본을 복제한 화면 스냅샷과 가격·중량 표시 문자열, 임신 표시 여부를 제공한다.
  - 신규 `Presentation/Display/AuctionDisplayFormatter.cs`: 기존 숫자 해석·반올림·현재 문화권을 유지하는 서식 함수와 중량 단위 함수를 제공한다.
  - `ViewModels/AuctionContPanelViewModel.cs`: `ItemDisplay` 생성·교체 알림을 추가한다. 동일 데이터는 기존처럼 스냅샷과 표시 값을 재사용한다.
  - `Presentation/Display/DisplaySelect.cs`: 결과 View의 DataContext를 공통 표시 ViewModel로 연결한다. View 재사용, LastData 비교와 NoteHost의 원본 스냅샷 연결은 유지한다.
  - 관련 XAML 61개: 가격·중량·임신 바인딩 128개를 이관하고 단방향 바인딩을 명시한다. 결과 View 내부의 미이관 필드는 `Snapshot`을 사용한다. 색상·굵기·배치와 다른 Converter는 유지한다.
  - `Presentation/Converters`의 `NumberFormatConverter.cs`, `WeightKGConverter.cs`, `ChuncheonWeightConverter.cs`, `PregnancyValueConverter.cs`: 활성 참조가 없어진 클래스 4개 삭제.
  - 신규 `Validation/ReferenceAdoptionChecks/ItemDisplayChecks.cs`, 기존 `BindingChecks.cs`, `UiChecks.cs`: 고정 표시 규칙, 실제 바인딩·갱신과 결과 진입 경계 검증.
  - `Docs/Converter_표시ViewModel_정리계획_2026-09-23.md`, `Validation/ReferenceAdoptionChecks/README.md`: 1차 완료 범위와 실행 결과 기록.
- 영향 범위: 세 경매 상태에서 같은 표시 ViewModel을 사용한다. 기존 쉼표가 없던 `AuctionUnSold_64`, `AuctionUnSold_160_64`, `SanjuUnSold`에도 쉼표를 적용한다. 단위를 붙이는 중량의 빈 값·공백·0·`-`는 `-`로 통일한다. 해남진도의 `결장 → -`, 임신 X/미정/양수 개월, 화면별 단위·서식은 보존한다. NOTE·추가 이력 원상복구 결과, 원본 모델·통신·캐시 계약은 변경하지 않았다.
- 빌드·테스트 결과:
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -p:OutputPath=$env:TEMP\CowAuction-ItemDisplay-Debug -v:q -clp:ErrorsOnly`: 최종 오류 0개, 기존 경고 78개.
  - 위 출력의 `ReferenceAdoptionChecks.dll binding`: 2,155개 통과. 진행 32개·결과 37개 화면, 공통 컨트롤, 읽기 전용 표시 속성과 실제 WPF 바인딩 확인. 첫 실행에서 발견한 Run의 기본 양방향 바인딩 충돌을 `OneWay`로 수정 후 재검증했다.
  - 같은 출력의 `ReferenceAdoptionChecks.dll updates`: 55개 통과. `stage4`: 596개 통과. 합성 데이터의 갱신·수명 주기 회귀 확인이며 운영 통신 검증은 아니다.
  - `dotnet build Validation/OwnerHistoryChecks/OwnerHistoryChecks.csproj --no-restore -p:OutputPath=$env:TEMP\CowAuction-ItemDisplay-Owner -v:q -clp:ErrorsOnly`: 오류·경고 0개. 위 출력의 `OwnerHistoryChecks.dll --scroll-offline`: 실제 WPF 시계 기반 18개 통과.
  - `dotnet build CowAuctionSmall.csproj -c Release --no-restore -p:OutputPath=$env:TEMP\CowAuction-ItemDisplay-Release -v:q -clp:ErrorsOnly`: 오류 0개, 기존 경고 78개.
- Git 정보: `master`, HEAD `52c554d`, 시작 상태 196개 항목. 기존 변경은 시작 시점 사본과 구분해 보존했다. commit hash `미생성`, push `미수행`.
- 추가 확인: 실제 서버·전광판 육안 확인, 성능 전후 비교, 운영 배포는 미수행. 기존 빈 320×64 결과 화면은 생성·해제와 바인딩 오류만 검증했다. 목록의 4~6번과 스타일 Converter 정리는 미착수다.


## 2026-09-23 - 영천 일괄 염소경매 진행 테두리 표시

### 작업 일자

- 2026-09-23

### 작업 목적

- 영천축협 염소 일괄경매 진행 중 모든 출하 염소의 진행 화면 테두리를 깜빡임 없는 주황색으로 표시한다.
- 낙찰 화면의 청색 테두리와 유찰 화면의 적색 테두리는 기존 표시를 유지한다.

### 원인

- 일괄경매 시작 시 전체 경매 상태만 진행 중으로 설정되고 개별 염소의 `IsRunning`은 변경되지 않아 공용 염소 진행 화면의 테두리 상태로 일괄경매 진행 여부를 표현할 수 없었다.
- 기존 `IsRunning` 트리거를 사용하면 빨간색 점멸이 실행되고, 공용 염소 View를 조건 없이 변경하면 다른 축협에도 영향을 준다.

### 변경 파일 및 내용

- `CowAuctionSmall/ViewModels/AuctionContPanelViewModel.cs`: 영천 일괄 염소경매의 고정 테두리 표시 여부를 전달하는 속성을 추가했다.
- `CowAuctionSmall/Services/DisplaySelect.cs`: 영천 축협 코드, 일괄경매, 염소, 진행 상태를 모두 확인하여 경매 시작·종료 시 표시 속성을 갱신하도록 했다.
- `CowAuctionSmall/Views/Size128_128/Running/Standard_Goat_Run.xaml`: 전용 표시 속성이 활성화되면 기존 점멸을 중단하고 고정 주황색 테두리를 적용하도록 했다.
- `WORKLOG.md`: 작업 내용과 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 축협 코드 `8808990656687`인 영천축협의 128×128 염소 일괄경매 진행 화면에만 주황색 테두리가 적용된다.
- 영천 단일경매, 영천의 다른 축종, 다른 축협의 염소경매 및 기존 낙찰·유찰 View는 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall\CowAuctionSmall.csproj --configuration Debug --no-restore`: 성공, 오류 0개 / 경고 102개.
- XAML 구문 검사와 WPF XAML 컴파일: 성공.
- 조건 매핑 확인: `영천(8808990656687) + 일괄(10) + 염소(5) + 진행(11) → 고정 주황색`, 그 외 조건은 비활성화.
- 낙찰 `GoatSold`의 청색과 유찰 `GoatUnSold`의 적색 테두리가 별도 View에 유지되는 것을 확인했다.
- `git diff --check`: 공백 오류 없음. Git 줄바꿈 변환 안내만 확인했다.
- 영천 실제 장비에서 일괄경매 시작·종료 및 낙찰·유찰 화면 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `feat: 영천 염소 일괄경매 진행 테두리 표시`


## 2026-09-23 - 상태표시창에 페이지 전환 상태 추가

### 작업 일자

- 2026-09-23

### 작업 목적

- 출하AMS 하단 상태표시창에서 페이지 회전 여부를 `페이지전환` 또는 `페이지고정`으로 확인할 수 있게 한다.
- 설정된 진행 페이지가 1개인 경우에는 페이지 전환 상태 항목을 표시하지 않는다.

### 원인

- 기존 상태표시창은 설정된 총 페이지 수만 표시하고 실제 페이지 회전 가능 상태와 경매 진행 등에 따른 첫 페이지 고정 상태를 구분해 표시하지 않았다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/MainWindow.xaml`: 페이지 수와 버전 사이에 페이지 전환 상태 표시 영역을 추가했다.
- `CowAuctionSmall/ViewModels/MainWindowViewModel.cs`: `PageIndicatorStateMessage.IsFrozen`에 따라 `페이지전환` 또는 `페이지고정`을 표시하고, 총 페이지가 2개 이상일 때만 해당 영역을 표시하도록 했다.
- `WORKLOG.md`: 작업 내용과 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 출하AMS 하단 상태표시창의 페이지 전환 상태 문구와 표시 여부에만 영향이 있다.
- 실제 페이지 회전, 경매 진행 중 첫 페이지 고정, 마스터·서브 동기화 및 전광판 화면 구성은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall\CowAuctionSmall.csproj --configuration Debug --no-restore`: 성공, 오류 0개 / 경고 102개.
- 표시 매핑 확인: `BoardPage=1 → 항목 숨김`, `BoardPage=2~4 + IsFrozen=false → 페이지전환`, `BoardPage=2~4 + IsFrozen=true → 페이지고정`.
- `git diff --check`: 공백 오류 없음. Git 줄바꿈 변환 안내만 확인했다.
- 실제 장비에서 페이지 회전·고정 상태 전환 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `feat: 상태표시창에 페이지 전환 상태 추가`


## 2026-09-23 - 단일경매 준비 상태 테두리 점멸 방지

### 작업 일자

- 2026-09-23

### 작업 목적

- 단일경매 시작 전 `8002(경매 준비)` 상태에서 대상 전광판 테두리가 점멸하지 않도록 한다.

### 원인

- 단일경매의 `8002(경매 준비)`와 `8003(경매 시작)` 상태가 같은 분기에서 모두 `IsRunning=true`로 처리되어, 준비 상태부터 진행 중 테두리 애니메이션이 시작됐다.

### 변경 파일 및 내용

- `CowAuctionSmall/Services/ServerGetData.cs`: `8002`와 `8003`의 공통 데이터 준비 흐름은 유지하면서 `8003`에서만 `IsRunning=true`가 되도록 변경했다.
- `WORKLOG.md`: 작업 내용과 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 단일경매 `8002`, `8003` 상태 처리와 진행 중 테두리 점멸 여부에 영향이 있다.
- `8002`의 출품 데이터 준비, 새로고침 차단 및 대상 화면 전환 흐름은 유지한다.
- `8004(경매 진행)`, 일괄경매, XAML 애니메이션 및 사업장별 화면 구성은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall\CowAuctionSmall.csproj --configuration Debug --no-restore`: 성공, 오류 0개 / 경고 102개.
- 상태 매핑 확인: `8002 → IsRunning=false`, `8003 → IsRunning=true`, `8004 → IsRunning=true`.
- `git diff --check`: 공백 오류 없음. Git 줄바꿈 변환 안내만 확인했다.
- 춘천 실제 장비에서 준비·시작·진행·종료 상태 전환 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 단일경매 준비 상태의 테두리 점멸 방지`


## 2026-09-22 — 비고 중복 생략 변경 원상복구

- 목적·승인: 사용자가 서버 `Note`에 이미 이력 형태의 내용이 들어 있었음을 확인한 뒤 이번 중복 생략 변경의 원상복구를 요청했다.
- 문제·원인: 같은 이름이 빨간 서버 비고와 흰 추가 이력에 함께 표시된 상황을 색상 변경 문제로 해석하여 별도 중복 생략 개선을 진행했으나, 사용자가 해당 개선을 철회했다.
- 복구 파일·내용: 중복 생략 구현 직전 사본과 비교해 아래 기존 파일 9개를 바이트 단위로 복구했다. 이전 개체번호·MVVM 바인딩·비동기 로그 개선 등 기존 작업은 유지했다.
  - `Domain/Auction/gValues.cs`: `DisplayNote`를 기존 비고와 조회 문구의 단순 합성으로 복구.
  - `Views/Common/NoteText.cs`: 원본 비고·구분자는 빨간색, 추가 이력은 흰색으로 구성하는 기존 Run 처리 복구. 중복 문구를 자동으로 숨기지 않음.
  - `Validation/OwnerHistoryChecks/Program.cs`, `NoteColorChecks.cs`, `ScrollChecks.cs`: 이번 중복 생략 전용 진입점·검사를 제거하고 기존 검사 복구.
  - `Validation/ReferenceAdoptionChecks/BindingChecks.cs`: 이번 중복 생략 검사를 제거하고 기존 바인딩 검사 복구.
  - `Docs/소유주이력_설정_검증.md`, `Validation/ReferenceAdoptionChecks/README.md`, `Validation/OwnerHistoryChecks/README.md`: 중복 생략 기능 설명·검증 결과 추가분 복구.
- 제거 파일: 이번 작업에서 추가한 `Domain/Auction/AuctionNoteComposer.cs`, `Validation/OwnerHistoryChecks/NoteCompositionChecks.cs` 2개 제거.
- 계획 기록: `Docs/비고_이력출처구분_중복표시개선_계획_2026-09-22.md`는 기록을 보존하되 철회·원상복구 완료 상태로 표시했다.
- 검증: 별도 TEMP 출력 `CowAuction-NoteRevert-Debug`에 두 검증 프로젝트를 `dotnet build <project> --no-restore -p:OutputPath=$noteRevertOutput -v:q`로 빌드. ReferenceAdoptionChecks 빌드 오류 0개·기존 경고 80개, OwnerHistoryChecks 빌드 오류·경고 0개.
  - `dotnet "$noteRevertOutput\ReferenceAdoptionChecks.dll" binding`: 1,497개 통과.
  - `dotnet "$noteRevertOutput\OwnerHistoryChecks.dll" --scroll-offline`: 18개 통과.
  - 기존 파일 9개의 작업 전 사본과 일치 및 신규 파일 2개 제거 확인. 원본 두 필드·캐시·통신 규격 변경 없음.
- Git 정보: 기존 미커밋·미추적 작업을 보존. commit hash `미생성`, push `미수행`. 운영 배포 `미수행`.
- 추가 확인: 이번 복구에서는 Release 재빌드·운영 연결·실제 전광판 확인 `미수행`. 운영 표시 내용이나 저장된 원본 비고를 삭제하지 않았다.


## 2026-09-22 — 개체번호 가운데 배경 강조 제거

- 목적·승인: 사용자가 불필요한 `HighlightBackground` 삭제를 요청했다. 가운데 번호의 색상·굵기 강조는 유지한다.
- 문제·원인: 공통 개체번호 컨트롤에 기존 겹침 표시의 배경 강조 기능이 남아 있었다.
- 변경 파일·내용:
  - `Views/Common/EntityNumberText.cs`: 배경 강조 DependencyProperty, CLR 속성, 가운데 Run 배경 지정 코드 제거.
  - `Views/Size128_128`, `Views/Size128_64`, `Views/SIze_160_64`, `Views/Size_320_64`의 관련 XAML 49개: `HighlightBackground` 지정 49곳만 제거. 좌표·ZIndex·색상·굵기는 유지.
  - `Validation/ReferenceAdoptionChecks/EntityNumberChecks.cs`: 삭제된 속성 설정과 배경 전용 검사 제거. 번호 분리·축약 모드·동적 색상·굵기·갱신 검사는 유지.
  - `Docs/개체번호_공통표시_사용법.md`, `Validation/ReferenceAdoptionChecks/README.md`: 사용 가능한 속성과 설정 사용처, 이번 검증 결과 갱신.
- 영향 범위: 전체 개체번호의 가운데 Run 배경 강조만 제거. `EntityNumberShortBackground`는 단독 축약 표시 10곳의 TextBlock 배경에 사용 중이므로 설정·리소스·파서는 유지한다. 기존 사용자 변경 보존.
- 빌드·테스트 결과:
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -p:OutputPath=$env:TEMP\CowAuction-HighlightRemoval-Debug -v:q`: 오류 0개, 기존 경고 80개.
  - 위 출력의 `ReferenceAdoptionChecks.dll entity`: 34개 통과. `ReferenceAdoptionChecks.dll binding`: 1,497개 통과(진행 32개·결과 37개 화면, 비고 호스트·공통 배지).
  - 시작 시점 사본과 비교: XAML 변경이 대상 속성 제거에 한정됨을 확인. XML 유효성, BOM·줄바꿈 보존, 제품·검증 소스의 삭제 속성 참조 없음 확인.
- Git 정보: `master`, HEAD `52c554d`, 시작 상태 194개 항목. commit hash `미생성`, push `미수행`.
- 추가 확인: Release 재검증·운영 통신·실제 전광판 육안 확인·운영 배포 `미수행`. 현장 배경 위에서 가운데 번호의 가독성 확인 필요.


## 2026-09-22 — 개체번호 공통 표시와 중간 강조

- 목적·승인: 사용자가 전체 번호 위에 `EntityNumberShort`를 겹쳐 표시하는 방식의 개선을 요청하고, 하나의 TextBlock과 Run으로 가운데 네 자리만 강조하는 방법의 구현을 승인했다.
- 문제·원인: 53개 화면에서 전체 번호와 축약 번호가 중복 관리되고, 강조용 TextBlock의 좌표·배경·폭을 별도로 맞추고 있었다. 같은 경매품의 전체 번호와 축약 번호를 모델·파서·캐시에 각각 보관했다.
- 제품 변경 파일·목적:
  - `Views/Common/EntityNumberText.cs` 신규: TextBlock 기반 공통 컨트롤. Run 3개 재사용, 전체/축약 표시, 중간 네 자리의 색상·배경·굵기, 염소 마지막 3자리 표시, 빈 값·잘못된 형식·앞자리 0 처리. 자체 DataContext 변경이나 외부 이벤트 구독·타이머 없음.
  - `Views/Size128_128`의 기본·사업장별 Running/Sold/UnSold, `Views/Size128_64`, `Views/SIze_160_64`, `Views/Size_320_64/Running`의 개체번호 XAML 총 53개: 공통 컨트롤 63개(전체 52개, 축약 11개) 적용, 겹쳐 있던 강조 TextBlock 51개 제거. 기존 전체 번호의 위치·주변 요소·기본 스타일과 단독 축약 표시의 위치를 유지했다. 기존 설정 리소스를 강조 속성에 연결했다.
  - `Domain/Auction/gValues.cs`: `EntityNumberShort` 속성 및 비교·해시·복제·문자열 출력에서 중복 항목 제거.
  - `Infrastructure/Api/AnimalParseData.cs`: SV 변경 필드에서 축약 속성 제거, 별도 축약 계산 제거. 기존 전체 번호 생성과 경매 데이터 판정 유지.
  - `Infrastructure/Persistence/AuctionSnapshotCache.cs`: 축약 필드의 신규 캐시 저장 제외. 기존 버전 캐시는 그대로 읽음.
- 검증 변경 파일:
  - `Validation/ReferenceAdoptionChecks/EntityNumberChecks.cs` 신규, `Program.cs`: `entity`/`all` 검사 연결. 중간 스타일·축약 모드·동적 색상·모드 전환·원본 갱신·빈 값·SV 연결 검증.
  - `Validation/ReferenceAdoptionChecks/BindingChecks.cs`: 축약 속성 대신 전체 번호와 새 컨트롤의 실제 인라인 텍스트를 검사. 진행 32개·결과 37개 화면의 값과 바인딩 오류, 중첩 호스트와 낙찰·유찰 갱신 검사 유지.
  - `Validation/OwnerHistoryChecks/MoreChecks.cs`: 신규 캐시에 축약 값이 저장되지 않는지, 구 버전 축약 필드가 남은 합성 캐시를 읽을 수 있는지 추가 검증.
- 빌드·테스트 결과(실행 중인 제품과 분리한 TEMP 출력, 운영 연결 없음):
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -p:OutputPath=$env:TEMP\CowAuction-EntityValidation-Debug -v:q`: 오류 0개, 최초 제품 빌드 기존 경고 80개. 이후 검사 증분 빌드 경고·오류 0개.
  - 생성된 `ReferenceAdoptionChecks.dll entity`: **35개 통과**. `all`: **2,843개 통과**.
  - 같은 프로젝트 `-c Release`, 출력 `CowAuction-EntityValidation-Release`: 오류 0개, 기존 경고 80개. 생성된 DLL `entity` **35개**, `binding` **1,497개 통과**.
  - `dotnet build Validation/OwnerHistoryChecks/OwnerHistoryChecks.csproj --no-restore -p:OutputPath=$env:TEMP\CowAuction-EntityValidation-History -v:q`: 경고·오류 0개. 생성된 `OwnerHistoryChecks.dll --offline`: **55 + 25 = 80개 통과**.
  - 정적 검사: 53개 XAML의 XML 유효성·BOM 보존, 기존 요소 수 대비 정확히 겹침 51개 제거, 모든 번호 바인딩이 전체 번호를 사용함을 확인했다.
- 문서: `Docs/개체번호_공통표시_사용법.md` 신규, 검증 README 갱신. `Number`, `DisplayMode`, `CowDistinction`, 강조 속성 사용법과 구 설정·캐시 호환을 기록했다.
- 영향 범위: 개체번호 표시와 해당 중복 데이터 제거. `EntityNumberShortForeground`/`EntityNumberShortBackground` 설정 이름은 호환을 위해 유지한다. 어미·자축 번호, 경매 판정·통신 규격·예정가·비고 동작 변경 없음.
- Git 정보: 시작 시 `master`, HEAD `52c554d`, 상태 192개 항목. 이전 미커밋·미추적 작업을 시작 시점 사본과 비교하여 보존했다. 이번 commit hash `미생성`, push `미수행`, 운영 배포 `미수행`.
- 추가 확인: 실제 전광판·운영 통신 미수행. 사업장별 글자 폭과 주변 표시 간격·색상·굵기의 가독성은 현장 확인 필요. 빌드·오프라인 검사를 실제 전광판 검증으로 간주하지 않는다.


## 2026-09-22 — MVVM 경매 화면 바인딩 통일

- 목적·승인: 대기·진행·낙찰·유찰의 경매품 표시를 직접 속성 바인딩으로 통일하는 계획을 사용자가 승인하고, 총 3명으로 구현·검증을 진행하도록 요청했다.
- 문제·원인: 진행 화면의 ViewModel과 결과 화면의 경매품 객체가 서로 다른 바인딩 경로를 사용해 유찰 화면에 잘못된 `CowInfo.EntityNumberShort`가 남아 있었다. `gValues`는 속성 변경 알림이 없어 표시 컨텍스트만 바꾸면 동일 원본 객체 수정의 갱신이 누락될 수 있었다.
- 제품 변경 파일·목적:
  - `ViewModels/AuctionContPanelViewModel.cs`: 원본과 분리한 표시 스냅샷, 무변경 갱신 생략, `CowInfo` setter의 갱신 경로 통합, 비고 변경 전후 비교 및 위치 초기화, `Note` setter의 객체 교체와 진행 상태 유지.
  - `Presentation/Display/DisplaySelect.cs`: 낙찰·유찰 최초 표시와 변경 갱신에서 표시 스냅샷을 전달하고 기존 컨트롤 재사용·무변경 생략 유지.
  - `Views/Size128_128/Running`, `Views/Size128_64/Running`, `Views/SIze_160_64/Running`, `Views/Size_320_64/Running`의 진행 페이지 XAML 32개: 기존 내부 레이아웃에 `CowInfo` 컨텍스트를 연결하고 속성 경로 517곳 전환. 루트 ViewModel 유지, 진행 상태와 비고는 구체적인 상위 화면 참조. 정확한 페이지 목록은 계획 문서 4.2절에 기록.
  - `Views/Size128_128/Running/CustomAuctionRunning1/Hwasun.xaml`: 위 32개 중 하나이며 성별 스타일 Converter 2곳의 입력을 문자열에서 경매품 객체로 수정.
  - `Views/Common/NhQquriBadge.xaml`: 경매품 객체를 직접 받아 배지 표시.
  - `Views/Size128_128/CustomAuctionUnSold/HoengseongUnSold.xaml`: 잘못된 축약 개체번호 바인딩 수정.
  - `Views/Size128_128/Running/RunningNoteHost128_Running.xaml`, `.xaml.cs`: 경매품 표시 컨텍스트 분리, 개체 변경 감지에 내부 Canvas 연결. 동일 비고의 다른 개체로 바뀌면 애니메이션과 저장 위치 초기화. 같은 개체 갱신은 기존 위치 유지.
- 검증 변경 파일:
  - `Validation/ReferenceAdoptionChecks/BindingChecks.cs` 신규: 잘못된 경로 탐지 확인, 진행 32개·결과 37개 화면의 값/바인딩/갱신/수명, 공통 배지·중첩 호스트, 실제 낙찰·유찰 진입점의 동일 원본 갱신·재사용, 501개 알림·교체 검사.
  - 같은 폴더 `Program.cs`, `UiChecks.cs`, `DisplayLifetimeChecks.cs`, `README.md`: `binding`/`all` 연결, 진행 화면과 호스트의 실제 ViewModel 컨텍스트로 검사 수정, 검증 범위·실행 방법 기록.
  - `Validation/OwnerHistoryChecks/Program.cs`, `ScrollChecks.cs`, `UiChecks.cs`, `README.md`: 일반 Application 사용, 진행 비고 호스트 ViewModel 연결, 같은 비고의 다른 개체 전환 검사 강화, UDP 없는 `--scroll-offline` 모드 추가.
- 영향 범위: 제품 38개 파일(XAML 35개, C# 3개), 검증 9개, 문서 2개. 가격·경매 판정·프로토콜·API·배치 좌표·패키지 변경 없음. 표시용 `CowInfo`/결과 컨텍스트는 직접 수정하지 않는 스냅샷이며 원본 입력 수정 후 갱신 진입점을 사용한다.
- 빌드·테스트 명령 및 결과(각 출력은 실행 중인 제품과 분리한 TEMP 경로):
  - `dotnet build CowAuctionSmall.csproj --no-restore -p:OutputPath=$env:TEMP\CowAuction-BindingProduct-Debug -v:q`: 오류 0개, 경고 80개.
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -p:OutputPath=$env:TEMP\CowAuction-BindingValidation-Debug -v:q`: 최종 증분 빌드 오류 0개, 경고 0개. 생성된 `ReferenceAdoptionChecks.dll all`: **2,805개 통과**.
  - 같은 검사 프로젝트에 `-c Release`, 출력 `CowAuction-BindingValidation-Release`: 오류 0개, 기존 경고 80개. 생성된 DLL `binding`: **1,493개 통과**.
  - `dotnet build Validation/OwnerHistoryChecks/OwnerHistoryChecks.csproj --no-restore -p:OutputPath=$env:TEMP\CowAuction-BindingValidation-History -v:q` 및 생성된 DLL `--offline`: 빌드 오류 0개, **55 + 22 = 77개 통과**.
  - 같은 DLL `--scroll-offline`: **18개 통과**. 수정 전 DLL에서는 동일 비고의 다른 개체 전환 실패를 재현한 뒤 수정 후 통과를 확인했다. 최종 검사 프로그램 증분 빌드 오류·경고 0개.
  - 501품목 무변경/가격 1건/전체 변경의 알림·교체 **0/1/501회**, 할당 **40/1,024/493,024바이트**. 최종 Debug 갱신 시간 0.24/0.33/0.40ms는 ViewModel 전달 경계 측정이며 실제 렌더링 시간이 아니다.
- 문서: `Docs/MVVM_경매화면_바인딩통일_변경계획_2026-09-22.md`에 실행 결과·변경 계약·제한 사항 추가. XAML XML/BOM/요소 수, 문서 링크와 시작 시점 대비 diff 확인.
- Git 정보: 시작 시 `master`, HEAD `52c554d`, 상태 183개 항목, 최종 상태 192개 항목. 기존 미커밋·미추적 변경은 보존했으며 이번 범위는 별도 시작 시점 파일 사본과 비교했다. 이번 commit hash `미생성`, push `미수행`, 운영 배포 `미수행`.
- 추가 확인: 실제 전광판 배치·겹침·색상·스크롤 속도 및 운영 통신은 미수행. 기존 320×64 결과 2개와 진행 2페이지는 빈 구현으로 기능을 추가하지 않았다. 소유주 이력의 기존 전체 UI 검사는 UDP를 시작하므로 실행하지 않았으며 오프라인 모드만 실행했다.


## 2026-09-22 — Debug 출력·파일 로그 비동기 처리

- 목적·승인: 사용자가 Netty 원문과 다른 Debug 출력, 파일 로그가 경매 처리를 지연시키는 문제의 개선 계획을 승인하고 “좋아 에이젼트수량 맞춰서 진행하자”로 구현·검증을 요청했다. 총 3명이 Debug 경로·NLog/앱 수명·검증을 분담했다.
- 문제·원인: 기존 Debug 출력은 수신·업무·UI 호출 스레드에서 직접 실행됐고, NLog File 대상에는 비동기 래퍼가 없었다. 출력과 파일 기록 지연이 호출 측에 영향을 줄 수 있었다. 실제 수신 로그의 6.261초 차이가 전부 로깅 때문인지는 미확인이다.
- 제품 변경 파일·목적:
  - `Infrastructure/Logging/AsyncDebugOutput.cs` 신규: 전용 백그라운드 스레드 하나, 2,048건·400만 문자 대기 큐, 최대 128건·약 64K 문자 묶음·최초 입력 기준 최대 약 50ms 대기, 원문·발생 시각·등록 순번을 구현했다. 포화·실패·미완료·대기/출력 시간 집계와 제한 시간 종료를 제공한다. 보간식은 출력 비활성 시 평가를 생략하고 Debug 조건부 호출을 유지한다.
  - `Infrastructure/Logging/LoggingLifetime.cs` 신규: NLog AsyncTargetWrapper를 상속한 `ObservedAsyncTarget`으로 기존 큐·기록 기능을 재사용하며 등록·완료·실패·포화 누락·미완료를 집계한다. 설정 재적용도 같은 집계를 사용하고 실패 요약을 제한된 빈도로 전달한다. Flush/Shutdown을 별도 종료 작업에서 수행해 호출자의 대기 시간을 제한한다.
  - `NLog.config`: 기존 File 대상을 `ObservedAsyncWrapper`로 감싸 10,000건·200건 묶음·Discard를 명시한다. 기존 파일 경로·레이아웃은 보존했다.
  - `App.xaml.cs`: 사용자 정의 NLog 대상 등록을 설정 로드·Logger 생성보다 먼저 수행한다. 파일 실패 요약을 Debug와 기존 상태 안내에 연결하고, 서비스 정리·최종 로그 이후 파일/Debug 종료에 합계 2초의 추가 대기 예산을 사용한다. 자동 ProcessExit Shutdown의 중복 대기를 해제했다.
  - `Infrastructure/Netty/netty/handlers/AuctionClientInboundDecoder.cs`: 수신 원문을 공통 Debug 큐로 전달한다. 원문 필드·빈 값은 유지하고 `[시각] [#순번] [Netty 수신]`을 붙인다.
  - `Infrastructure/Netty/netty/AuctionDelegate.cs`, `AuctionShareNettyClient.cs`, `NettyControllable.cs`: 연결·해제 Debug 호출을 전환하고 반복 Ping은 선택적 진단으로 처리한다. 저빈도 Console 설정·시작/연결 오류의 기존 Release 가시성은 유지한다.
  - `Application/Auction/NettyAsyncMsgProcess.cs`, `ServerGetData.cs`: 업무 Debug 출력을 공통 경로로 전환하고 반복 조회 주기 출력은 진단 옵션으로 처리한다.
  - `Infrastructure/Api/ServerConn.cs`, `Infrastructure/Configuration/XmlParserCont.cs`: 조회·설정 오류 Debug 출력을 전환한다.
  - `Presentation/Display/DisplaySelect.cs`, `ViewModels/AuctionContPanelViewModel.cs`: UI 출력 전환, 반복 tick·회전·진행 화면 진단을 옵션으로 제어한다.
- 사용 옵션: Debug 원문 출력은 기본 켜짐. `COWAUCTION_DEBUG_OUTPUT=0`으로 제품 Debug 출력을 끄고, `COWAUCTION_DIAGNOSTICS=1`로 반복 진단을 켤 수 있다. 환경 변수는 앱 시작 시 읽는다. Release에서는 Debug 호출이 제거된다. Netty 원문을 새로 파일에 저장하지 않는다.
- 검증·문서 변경: `Validation/ReferenceAdoptionChecks/LoggingChecks.cs` 신규, 같은 폴더 `Program.cs`에 `logging`/`all` 연결, `README.md`에 검사·측정 설명 추가. `Docs/Debug_파일로그_비동기처리_개선계획_2026-09-22.md`에 구현 결과·확인 한계를 기록했다.
- 최종 명령·결과(제품 실행·운영 연결 없이 별도 TEMP 출력, 복원 없음):
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -p:OutputPath=$env:TEMP\CowAuction-LoggingValidation-Debug -v:q`: Debug 제품 포함 성공, 오류 0개·경고 84개. 시작 전 기준도 오류 0개·경고 84개였다.
  - `dotnet "$env:TEMP\CowAuction-LoggingValidation-Debug\ReferenceAdoptionChecks.dll" all`: **1,312개 통과**. 시작 전 1,253개에 로그 검사 59개를 추가했다.
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj -c Release --no-restore -p:OutputPath=$env:TEMP\CowAuction-LoggingValidation-Release -v:q`: Release 제품 포함 성공, 오류 0개·경고 84개.
  - `dotnet "$env:TEMP\CowAuction-LoggingValidation-Release\ReferenceAdoptionChecks.dll" logging`: **59개 통과**, Release의 문자열 평가·원문 수집·출력 작업 시작이 제거되는 검사 포함.
  - `dotnet build Validation/OwnerHistoryChecks/OwnerHistoryChecks.csproj --no-restore -p:OutputPath=$env:TEMP\CowAuction-LoggingValidation-History -v:q`: 해당 증분 빌드 오류 0개·경고 0개.
  - `dotnet "$env:TEMP\CowAuction-LoggingValidation-History\OwnerHistoryChecks.dll" --offline`: **55+22=77개 통과**.
- 검증 내용: 501건 원문·빈 필드·시각·순서, 다중 생산자, 건수/문자량 상한, 느린 출력 중 생산자 완료, Debug/파일 소비 독립성, 쓰기 실패, XML 설정 재적용, 제한 시간 종료·중복 종료·늦은 입력을 확인했다. 포화 검사에서 파일 Submitted=502, Written=9, Dropped=493, Pending=0을 확인했다. 제품 NLog 설정을 별도 LogFactory에 읽은 뒤 TEMP 파일 경로로 바꾸고 501건·최종 Warn/Error·설정 재적용 후 메시지의 실제 파일 기록도 확인했다.
- 합성 측정: 501건에 대해 동기 가짜 Debug 출력의 생산자 시간 약 7,585.66ms·501회 호출, 비동기는 등록 0.64ms·전체 배출 59.44ms·4회 호출·누락 0건이었다. 가짜 대상이 요청한 1ms sleep이 Windows 타이머 정밀도 때문에 약 15ms로 실행된 조건이다. 실제 Visual Studio가 7.6초에서 59ms로 개선됐다는 뜻이 아니다.
- 조건별 합성 등록 시간: 모두 끔/Debug만/파일만/둘 다는 0.01/0.70/0.74/1.62ms, 생산자 스레드 할당량은 0/40,416/144,304/184,720바이트였다. 파일 비교도 묶음당 지연을 주는 가짜 대상이다. 전체 프로세스 CPU·메모리·실제 디스크·Netty·UI 성능 수치로 해석하지 않는다.
- 영향 범위·남은 확인: 승인된 출력 경로와 수명 범위만 변경했다. 패키지·경매 로직·API 주기·화면 정책은 유지했다. 정상 부하에서 보존하고 과부하 누락을 명시하지만 Warn/Error까지 무손실을 보장하지 않는다. Debug 큐 상한 외에 소비 중 묶음과 문자열 조합 공간이 존재한다. 파일 미완료는 큐 대기+기록 중이며, 종료 제한을 넘긴 경우 실제 유실 여부는 확정할 수 없다. 이미 시작된 파일 실패 안내 콜백은 종료와 겹칠 수 있으나 UI의 disposed/shutdown 검사가 보호한다.
- 수동 확인: 현재 앱에는 자동 적용되지 않는다. 재빌드·재실행 후 실제 Visual Studio 원문·새로고침 체감 지연·파일 기록과 전광판을 확인해야 한다. 운영 연결·전광판 성능 비교·배포 미수행.
- Git·보호: master, 기준 HEAD 52c554d. 시작 시 TEMP `CowAuction-logging-baseline-k747dnr9`에 관련 소스·설정·문서를 보관하고 최종 차이를 비교했다. 기존 사용자 변경과 BOM·기존 줄바꿈·과거 기록을 보존했다. commit hash 미생성, push 미수행.


## 2026-09-22 — SV/API 공통 변경 감지·부분 갱신 구현

- 목적·승인: 사용자가 SV와 API의 변경 여부를 공통으로 빠르게 확인하고 의미 없는 건수 출력을 제거하도록 계획한 뒤 “좋아 진행하자”로 구현·검증을 승인했다. 주 에이전트의 통합과 파서·검증 담당을 포함해 3명이 분담했다.
- 문제·원인: 기존 SV 처리는 예정가만 읽으며 동일 값에도 알림을 보냈다. API는 변경 알림에 더해 전체 목록을 다시 전달했다. SV/API의 뒤쪽 필드 위치와 누락 의미가 달라 단순 객체 교체 시 API 전용 정보와 최신 경매 상태를 잃을 수 있었다.
- 제품 변경 파일·목적:
  - `Domain/Auction/AuctionItemUpdate.cs` 신규: 유효 필드 집합과 식별 정보를 표현하고 필드 접근자를 한 번 생성해 재사용한다.
  - `Domain/Auction/gValues.cs`: 저장 대상에서 제외한 사업장·회차·수신 필드 메타데이터와 복제 처리를 추가한다.
  - `Application/Abstractions/IAuctionPacketParser.cs`: SV/API 구분과 부분 갱신 결과 계약을 추가한다.
  - `Infrastructure/Api/AnimalParseData.cs`: 형식별 매핑, 누락·빈 값·잘못된 값 처리와 기존 사업장 표시 변환을 연결한다. SV 36~38번은 API 38~40번에 대응하며 빈 비고는 삭제, 빈/잘못된 예정가는 보존, 0 가격은 결장으로 처리한다.
  - `Application/Auction/AuctionSnapshotParser.cs`: 공통 파싱과 사업장·회차·중복 출품번호 검증을 연결한다.
  - `Application/Auction/AuctionItemStore.cs` 신규: 출품/계류대 사전 조회, 실제 값 비교, API 전용·이력 정보 보존, SV 필드별 변경 순서와 API 일치 확인, 요청 이후 개체 교체·삭제 경합 보호를 구현한다.
  - `Application/Auction/AuctionDisplayUpdateBatcher.cs` 신규: 첫 변경 즉시 전달, 약 30ms 간격의 후속 알림 합치기, 마지막 변경·초기화·종료 타이머 수명을 처리한다.
  - `Application/Auction/ServerGetData.cs`: SV/API 반영 경로를 연결하고 진행·낙찰 우선권을 유지한다. 초기·세션 전환·재동기화는 전체 목록, 반복 조회는 변경/삭제/통계만 보낸다. 낙찰 단건 재조회가 다른 개체를 반환하면 기존 결과와 섞지 않는다. AS/AF·초기화 전 대기 알림을 반영하고 Stop/Dispose 이후 알림을 차단한다.
  - `Presentation/Display/BoardDisplayTracker.cs`: 데이터 변경 없는 통계 갱신을 허용한다.
  - `ViewModels/MainWindowViewModel.cs`: 삭제된 계류대·이력 대상과 통계만 갱신하는 메시지를 처리하고 `------------ 1` 출력을 제거한다. 서비스의 변경 건수 반복 출력도 제거했다. 기존 `[Netty 수신]` 원문 Debug 출력은 유지한다.
- 검증·문서 변경 파일:
  - `Validation/ReferenceAdoptionChecks/ItemUpdateChecks.cs` 신규, `Program.cs`: 공통 변경 감지·묶음·경합 검사 및 501건 측정 모드를 추가한다.
  - 같은 폴더의 `SnapshotChecks.cs`, `ServiceChecks.cs`, `ResultChecks.cs`, `RefreshChecks.cs`: 실제 39필드 합성 SV, 메타데이터 복제·낙찰 재확인·날짜/개체 보호 검사를 보완한다. 불필요한 전체/동일 알림에 의존하던 검사는 실제 상태와 변경 알림을 검사하도록 조정한다.
  - 같은 폴더의 `README.md`, `Docs/SV_API_공통변경감지_갱신개선계획_2026-09-22.md`: 실행 방법·측정 결과·현장 확인 범위를 기록한다.
- 최종 검증 명령·결과(실행 중인 사용자 앱과 분리한 TEMP 출력, 복원·운영 연결 없음):
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -p:OutputPath=$env:TEMP\CowAuction-SvValidation-Final-Debug -v:q`: Debug 제품 포함 성공, 오류 0개·경고 84개. 시작 전 기준 빌드도 경고 84개였다.
  - `dotnet "$env:TEMP\CowAuction-SvValidation-Final-Debug\ReferenceAdoptionChecks.dll" all`: **1,253개 assertion 통과**. 변경 전 기준은 1,194개였다.
  - `dotnet build CowAuctionSmall.csproj -c Release --no-restore -p:OutputPath=$env:TEMP\CowAuction-SvValidation-Final-Release -v:q`: 성공, 오류 0개·경고 84개.
  - `dotnet build Validation/OwnerHistoryChecks/OwnerHistoryChecks.csproj --no-restore -p:OutputPath=$env:TEMP\CowAuction-SvValidation-Final-History -v:q`: 성공, 해당 증분 빌드 출력 오류 0개·경고 0개.
  - `dotnet "$env:TEMP\CowAuction-SvValidation-Final-History\OwnerHistoryChecks.dll" --offline`: **55+22=77개 통과**.
- 측정 결과: 합성 501건에 대해 기존 알림/전달 행은 각 경우 501/501이었다. 최종 동일 입력은 0/0, 3건 변경은 2/3, 전체 변경은 3/501, 같은 출품 반복 변경은 2/2였다. 알림 횟수는 30ms 경계에 따라 변한다. 업무 처리 시간은 약 20/23/32/28ms, 할당량은 약 8.09/8.10/10.08/7.65MB였다. 기존 가격만 처리할 때의 약 15/11/11/12ms 및 1.3~1.4MB보다 증가했다. 전체 필드 처리를 추가한 결과이며 CPU·메모리 개선으로 보고하지 않는다. 원문 Debug 출력·디버거·실제 렌더링을 제외한 합성 검사로 중복 화면 전달 감소를 확인했다.
- 영향 범위·추가 확인: 승인한 공통 갱신 범위의 변경이다. 패키지·API 주소/주기·설정·화면 디자인·원문 저장 정책 변경은 없다. 실제 사업장별 확장 SV, 전광판 표시 지연, 시작/낙찰과 겹친 새로고침은 현장 확인 미수행이다. 서버 공통 버전 부재로 API 단독 수정과 오래된 복제 응답을 완전히 구분할 수 없으며, SV 필드 보호는 일치 확인 또는 세션 초기화까지 유지한다. 현재 실행 중인 앱에는 자동 적용되지 않으므로 재빌드·재실행이 필요하다.
- Git·보호: master, 기준 HEAD 52c554d. 시작 시 보관한 관련 파일과 최종 diff를 비교해 이번 변경을 구분하고 기존 사용자 변경·BOM·줄바꿈·과거 기록을 보존했다. 새 파일도 위에 명시한 범위만 추가했다. commit hash 미생성, push·배포·운영 교체 미수행.


## 2026-09-22 — SV 현재 예정가 수신 반영 및 AS 가격 덮어쓰기 수정

- 목적·승인: 사용자가 실제 수신 후 예정가가 바뀌지 않는 문제를 제시했고 현재 가격이 612만원임을 확인했다. 해당 오류의 수신·반영 경로를 수정했다.
- 문제·원인: `AnimalParseData`의 기존 필드 정의상 SV[26]은 최초 최저가, SV[27]은 현재 최저가다. Netty 디코더가 일반 SV를 다음 처리기로 전달하지 않았고 업무 처리기의 SV 경로도 비활성화되어 있었다. 앞선 구현이 AS[4]를 변경 예정가로 해석하여 SV의 실제 가격과 다른 값으로 덮어쓸 수 있었다. AS8003에 가격 필드가 있으므로 변경 예정가로 사용할 수 있다는 앞선 판단을 정정한다.
- 제품 변경 파일:
  - `Infrastructure/Netty/netty/handlers/AuctionClientInboundDecoder.cs`: SV 원문을 다음 처리기로 전달한다. 기존 새로고침 감지·조회 경로는 유지한다.
  - `Application/Auction/NettyAsyncMsgProcess.cs`: 수신 SV 배열을 업무 처리기로 전달한다.
  - `Application/Auction/ServerGetData.cs`: SV[2] 출품번호와 SV[27] 현재 최저가를 검증하여 해당 출품의 LowestPrice와 화면 알림에 반영한다. AT 수신 전에도 가격 처리가 가능하다. 기존 가격 오버레이를 사용해 오래된 HTTP 응답을 방어하고 초기 목록·날짜 전환 중에는 새 목록에 적용한다. 0은 기존 파서처럼 결장으로 표시한다. AS8003은 가격을 변경하지 않고 AS8004는 SV/API 가격을 유지하며 진행 상태만 변경한다.
- 검증 변경 파일: `Validation/ReferenceAdoptionChecks/ServiceChecks.cs`, `BatchStateChecks.cs`, `RefreshChecks.cs`. 개인정보가 없는 합성 패킷으로 실제 디코더 전달, AT 이전 수신, SV의 최초가 312/현재가 612와 AS 값 12의 불일치, 가격 하향, 시작·중복 수신, 잘못된 가격·짧은 패킷, 초기 목록·재동기화·날짜 전환·조회 회귀를 검사한다. 기존 AS 가격 기반 테스트는 SV 가격 수신을 포함하도록 수정했다.
- 검증 명령·결과:
  - `dotnet run --project Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -- all`: 실행 중인 CowAuctionSmall.exe 파일 잠금으로 MSB3027/MSB3021 실패. 사용자 프로세스는 종료하지 않았다.
  - `dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -p:OutputPath=<임시검증폴더> -p:UseAppHost=false -v:q`: 성공, 해당 증분 빌드 출력 경고 0개·오류 0개. 기존 전체 컴파일 경고 해소를 의미하지 않는다.
  - `dotnet <임시검증폴더>/ReferenceAdoptionChecks.dll all`: 오프라인 검사 1,194개 통과. 실제 운영 통신은 수행하지 않았다.
- 영향 범위·확인 사항: 예정가 출처와 수신 전달 수정. 다른 SV 필드·낙찰 결과를 가격 수신만으로 덮어쓰지 않는다. 현재 실행 중인 앱에는 적용되지 않았으므로 디버깅 중지 후 재빌드·실행하여 실제 가격 변경·시작을 확인해야 한다. 운영 연결·실제 전광판 확인·배포·복원 미수행.
- Git·보호: master, 기준 HEAD 52c554d. 시작 시 관련 파일을 보관해 차이를 확인했고 기존 사용자 변경·인코딩·줄바꿈과 이전 기록을 보존했다. commit hash 미생성, push 미수행.


## 2026-09-22 — Netty 수신 원문 Debug 출력

- 목적·승인: 사용자가 신호 요약이 아니라 Netty로 수신된 원문 전체를 보고 필드·값·조정 오류를 확인하고 싶다고 명시했다. 해당 요청에 따라 수신 원문을 Visual Studio Debug 출력에 표시한다.
- 문제·원인: 직전 출력은 코드와 상태만 추려 출품번호·가격·빈 필드 등 실제 수신 내용을 확인할 수 없었다.
- 변경 파일·내용: `Infrastructure/Netty/netty/handlers/AuctionClientInboundDecoder.cs`의 Decode 진입 시 수신 시각과 원문 문자열을 출력한다. 필드 분리·검증·메시지 분기 전에 기록하므로 AS/AT/AF/SD/SZ/SV 및 기타 수신 문자열을 그대로 확인할 수 있다. `Application/Auction/NettyAsyncMsgProcess.cs`의 중복 요약 Debug 출력을 제거했다.
- 영향 범위: Debug 빌드의 수신 출력만 변경한다. 원문은 기존 UTF-8 문자열 디코더가 전달한 프레임 내용이며 앞단에서 제거된 줄 종료 문자는 포함하지 않는다. 필드 순서·빈 값·구분자를 재조합하거나 마스킹하지 않는다. 파일 로그는 기존 요약을 유지하며 송신·인증 요청 출력은 추가하지 않았다. 경매 처리·화면 상태는 변경하지 않았다.
- 검증: `dotnet build CowAuctionSmall.csproj -c Debug --no-restore -v:q` 성공, 오류 0개·경고 84개. 시작 파일 대비 diff, 기존 인코딩/CRLF와 WORKLOG 보존, 최종 Git 상태 확인. 출력문 변경에 대한 신규 테스트 추가·기존 자동 검사 재실행은 미수행.
- 추가 확인·미수행: 실제 운영 서버 연결 및 Visual Studio 실수신 확인은 미수행. Debug로 다시 실행하여 출력 창에서 `[Netty 수신]` 확인 필요. 운영 실행·배포·복원 미수행.
- Git: master, 기준 HEAD 52c554d. 기존 사용자 변경 보존. commit hash 미생성, push 미수행.


## 2026-09-22 — Visual Studio 경매 수신 로그 식별 개선

- 목적·승인: 사용자가 반복되는 OnStringArrMsg 출력 대신 AS|8002|처럼 수신 신호를 알아볼 수 있는 로그를 요청하여 관련 출력만 수정했다.
- 문제·원인: 수신 전달 단계마다 고정 문구가 중복 출력되었고 배열/메서드를 문자열로 출력하여 System.String[]·System.Func가 표시되었다. 정상 단일경매 대기 등에도 잘못된 데이터 길이 부족 문구가 출력되었다.
- 변경 파일·내용:
  - `Application/Auction/NettyAsyncMsgProcess.cs`: 수신 시각·허용된 신호 코드/상태·한글 의미를 한 줄로 Debug 출력한다. 예: `[11:03:43.123] [경매 수신] AS|8002| 경매 대기 신호 수신`. SZ 배열 출력과 정상 상태의 잘못된 길이 오류 출력을 제거했다.
  - `Infrastructure/Netty/netty/NettyControllable.cs`: 같은 신호 전달 시 반복되던 고정 문구를 제거했다.
  - `Application/Auction/ServerGetData.cs`: 중복된 상태 갱신·AF/SD 객체 출력을 제거하고 AS8006 처리 로그를 명시적 코드·의미로 변경했다.
- 영향 범위: Visual Studio Debug 출력과 단일경매 종료 처리 로그. 기존 허용 코드/상태 요약을 재사용하며 원문·인증 정보·개인정보를 새로 기록하지 않는다. 수신 분기·가격·경매 상태·화면 동작은 변경하지 않았다.
- 검증: `dotnet run --project Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -- stage3` 성공. Debug 빌드 및 오프라인 경매 검사 477개 통과. nullable·미사용·MVVM 등 컴파일 경고는 남아 있다. 시작 시 보관한 파일 대비 diff·인코딩/CRLF·공백 및 최종 Git 상태 확인.
- 추가 확인·미수행: 실제 운영 서버/전광판 연결과 Visual Studio 디버거에서 실수신 출력 확인은 미수행. Debug로 다시 실행하여 수신 로그 확인 필요. 운영 앱 실행·게시·배포·패키지 복원 미수행.
- Git: master, 시작 HEAD 52c554d. 기존 미커밋·미추적 변경을 보존했다. commit hash 미생성, push 미수행.


## 2026-09-22 — 경매 수신·상태·표시 단계별 안정성 개선

- 목적·승인: 경매프로그램 문서와 현재 WPF 대조 결과를 바탕으로 사용자가 단계별 수정 및 총 3명(구현·검토·검증) 진행을 승인했다. 기존 미커밋·미추적 파일을 기준으로 관련 부분만 수정했다. MAUI의 과거 실측을 WPF 실행 검증으로 간주하지 않았다.
- 문제·원인: 가격 불일치 시 진행 플래그와 출품번호가 모순되었고, AF 결과코드와 이전 낙찰 정보가 섞일 수 있었다. 초기 목록 전 시작·방식 전환·재동기화에서 상태 수명이 불완전했으며, 새로고침이 정기 조회나 후속 SV에 의존했다. 날짜 변경 중 이전 날짜 개체/조회 응답이 새 날짜에 섞이는 경합도 독립 검토에서 확인했다.

| 단계 | 이번 변경 |
|---|---|
| 1. 가격·시작 | 유효한 AS8003/8004 가격 반영, 초기 목록 전 시작 보존·조회, 재경매의 이전 낙찰 정보 제거, 잘못된 가격 거부 |
| 2. 낙찰 정보 | AF 결과·참가번호·금액 동시 반영, 이름은 HTTP 결과의 상태/번호/금액 일치 후 보정. 중복 AF와 지연 응답에도 확인된 결과 보존. 원 참가번호는 메모리 전용이며 캐시 저장 항목은 변경하지 않음 |
| 3. 상태 수명 | 목록 없이도 일괄 시작/종료 반영, 회차 종료·방식 변경 때 진행 상태 해제. 재동기화 경계 이후 새 시작은 보존 |
| 4. 새로고침 | AS8001 즉시 조회, 성공 응답에서 Refresh·완료 안내. 후속 SV가 즉시 요청을 취소하지 않게 함. 선택 날짜 검사와 날짜별 기존 목록 사용 제한으로 이전 개체/응답 혼합 방지 |
| 5. 수신 안내 | 기존 70px 로그 영역 안에 최근 수신 시각·표시 명령·카운트다운·정지/종료 신호 안내. 기존 서버 목록과 계류대 배치 유지. A/H는 안내만 수행하며 이전 요청을 무효화하지 않음 |
| 6. 운영·진단 | 기존 23~01시/200초 검사 유지. 진행·동기화·처리 중이거나 최근 경매 신호 후 60초 이내에는 종료 유예. 수신 원문 로그를 고정 코드/상태 요약으로 교체 |

- 제품 변경 파일·목적:
  - `Application/Auction/ServerGetData.cs`: 가격/결과 오버레이, 상태·날짜 수명, 즉시 조회와 응답 검증, 야간 종료 판단 연결.
  - `Application/Auction/NettyAsyncMsgProcess.cs`, `AuctionInput.cs`, `AuctionIngress.cs`: 가격 신호 전달, 공통 새로고침, 방식 전환, 재동기화 버전·날짜 유효성·작업 진행 여부, 신호 시각·안내.
  - 신규 `Application/Auction/AuctionSignalSummary.cs`, `Application/Messaging/AuctionSignalMessage.cs`: 허용 신호 요약과 운영 안내. `COWAUCTION_DIAGNOSTICS=1`인 프로세스에서 업무 큐·조회 적용 요약을 추가 기록한다. 영구 환경 변수는 변경하지 않았다.
  - `Domain/Auction/gValues.cs`, `Infrastructure/Api/AnimalParseData.cs`: 결과 확인용 참가번호와 복제. JsonIgnore 적용 및 기존 캐시 허용 목록 유지.
  - 신규 `Domain/Auction/NightShutdownPolicy.cs`, `Views/MainWindow.xaml.cs`: 종료 유예 정책과 UI 스레드에서 최종 조건 재확인.
  - `Infrastructure/Netty/netty/handlers/AuctionClientInboundDecoder.cs`, `Infrastructure/Netty/netty/NettyControllable.cs`: 수신·연결 결과의 원문 Debug/파일 로그 제거 및 요약.
  - `ViewModels/MainWindowViewModel.cs`, `Views/MainWindow.xaml`: 최근 수신 안내 바인딩과 구독 해제, 기존 전체 로그 영역 높이 유지.
- 검증 변경: `Validation/ReferenceAdoptionChecks/ServiceChecks.cs`, `Program.cs`, 신규 `ResultChecks.cs`, `BatchStateChecks.cs`, `RefreshChecks.cs`, `SignalChecks.cs`, `NightShutdownChecks.cs`. fake 데이터/조회와 완료 시점 제어로 가격·결과·일괄 수명·새로고침·날짜 경합·민감 원문 배제·종료 조건을 검사한다. 검증 안내 README 갱신.
- 단계 검증: `dotnet run --project Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -- stage3`를 각 단계 변경 후 실행하여 235 → 282 → 301 → 319 → 451개 assertion 통과.
- 최종 검증:
  - `dotnet run --project Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -- all`: Debug 빌드 및 **1,181개** assertion 통과. 날짜 전환 중 같은 출품번호의 다른 개체/계류대, 결과 선행 시 전체 목록 교체 포함.
  - `dotnet build CowAuctionSmall.csproj -c Release --no-restore -v:minimal`: 성공, 경고 **86개**, 오류 **0개**. nullable·미사용·MVVM 등 경고가 남아 있다.
  - `dotnet run --project Validation/OwnerHistoryChecks/OwnerHistoryChecks.csproj --no-restore -- --offline`: **55+22=77개** 회귀 검사 통과.
  - 수정 XAML XML 파싱, 시작 시 보관한 관련 소스 대비 diff/공백 및 최종 Git 상태 확인. 패키지·SDK·프로젝트 버전 변경 및 restore는 수행하지 않음.
- 영향 범위·한계: 승인한 경매 수신·조회·표시·종료 안정성 범위. SZ 구간 숫자 의미와 미응찰 판정 규격은 미확인이므로 클라이언트의 새로운 구간/미응찰 필터 및 기본영상 전환은 구현하지 않았다. SD(F)를 확정적인 일시정지 상태로 취급하지 않는다. 불완전 AF 결과는 후속 HTTP 조회로 보정한다.
- 추가 수동 확인·미수행: 실제 서버/전광판에서 가격 변경 직후 시작, 낙찰자 옵션 Y/X/B/N, F5, 날짜·단일/일괄 전환, 운영 상태줄 가독성, 야간 종료 유예. 전체 사업장/해상도·다중 PC 동기화의 실제 장비 검증은 미수행. 제품 앱 실행·운영 접속·게시·배포 교체 미수행.
- Git: `master`, 시작 HEAD `52c554d`. 기존 폴더 개편·설정·화면 등 사용자 변경은 보존했고 이번 변경으로 보고하지 않는다. commit hash `미생성`, push `미수행`.


## 2026-09-22 - 상태표시창에 설정된 총 페이지 수 표시

### 작업 일자

- 2026-09-22

### 작업 목적

- 상태표시창의 `페이지 N` 문구를 현재 회전 페이지가 아니라 `users.xml`의 `BoardPage` 총 설정값으로 고정 표시한다.

### 원인

- 기존 상태표시창이 `PageIndicatorStateMessage.CurrentPage`를 사용해 진행 페이지에서는 `페이지 1`, 유전 페이지에서는 `페이지 2`처럼 화면 전환에 따라 변경됐다.

### 변경 파일 및 내용

- `CowAuctionSmall/ViewModels/MainWindowViewModel.cs`: 상태 문구에 현재 페이지 대신 1~4 범위로 보정된 전체 페이지 수를 사용하도록 변경했다.
- `WORKLOG.md`: 작업 내용과 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 상태표시창의 `페이지 N` 문구에만 영향이 있다.
- 실제 페이지 전환, 마스터·서브 동기화, 경매 화면과 `users.xml` 설정은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall\CowAuctionSmall.csproj --configuration Debug --no-restore`: 성공, 오류 0개 / 경고 102개.
- 표시 매핑 확인: `BoardPage=1 → 페이지 1`, `BoardPage=2 → 페이지 2`, `BoardPage=4 → 페이지 4`.
- 실제 페이지 회전 중 문구 고정 여부 수동 확인: 미수행.
- `git diff --check`: 공백 오류 없음. Git 줄바꿈 변환 안내만 확인했다.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 상태표시창에 설정된 총 페이지 수 표시`


## 2026-09-22 - 1개월 경과 로그 정리 및 자정 정상 종료

### 작업 일자

- 2026-09-22

### 작업 목적

- 프로그램 시작 시 한 달이 지난 날짜별 로그 폴더를 자동 삭제한다.
- 실행 중인 프로그램을 다음 자정에 기존 창 종료 흐름으로 정상 종료한다.

### 원인

- `Logs/yyyy-MM-dd/yyyy-MM-dd.log` 구조로 로그가 계속 누적되지만 기존 보관 기간이나 삭제 처리가 없었다.
- 기존 자동 종료 타이머는 약 200초마다 검사해 밤 11시부터 새벽 1시 사이 임의 시점에 `Application.Shutdown()`을 직접 호출했다.

### 변경 파일 및 내용

- `CowAuctionSmall/App.xaml.cs`: NLog 초기화 후 `DateTime.Today.AddMonths(-1)`보다 오래된 `yyyy-MM-dd` 형식 로그 폴더를 삭제하도록 했다. 형식이 다른 폴더와 재분석 지점은 제외하고 입출력·권한 오류는 경고 로그로 남긴다.
- `CowAuctionSmall/Views/MainWindow.xaml.cs`: 다음 자정까지의 시간을 한 번 예약하고, 자정 도달 시 ESC와 공통 정상 종료 메서드를 호출하도록 했다. 공통 종료 경로는 중복 요청을 막고 Netty 종료를 기다린 뒤 메인 창을 닫는다.
- `WORKLOG.md`: 작업 내용과 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 실행 파일의 `Logs` 바로 아래 날짜형 폴더 중 한 달 기준일보다 오래된 폴더와 내부 로그는 프로그램 시작 시 삭제되며 복구할 수 없다.
- 날짜 형식이 아닌 폴더, 기준일 당일과 이후 로그는 유지된다.
- 프로그램이 자정까지 실행 중이면 창 닫기와 `App.OnExit` 자원 정리 경로로 종료된다. 자동 재실행은 포함하지 않았다.
- 전광판, 경매 처리, 통신 패킷과 프로젝트 설정은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall\CowAuctionSmall.csproj --configuration Debug --no-restore`: 성공, 오류 0개 / 경고 102개.
- 2026-09-22 기준 경계 계산 확인: `2026-08-21` 삭제 대상, `2026-08-22`와 이후 날짜 유지.
- `manual`처럼 날짜 형식이 아닌 폴더명 제외 확인.
- 다음 자정 계산 결과가 현재 시각보다 크고 24시간 이내인지 확인.
- `git diff --check`: 공백 오류 없음. Git 줄바꿈 변환 안내만 확인했다.
- 실제 운영 로그 삭제 및 실제 자정 도달 종료: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `feat: 오래된 로그 정리와 자정 정상 종료 추가`


## 2026-09-22 - 출하AMS 하단 상태표시창 상시 표시

### 작업 일자

- 2026-09-22

### 작업 목적

- 전광판 표출 화면 바로 아래에 응찰서버, 페이지 동기화 모드, 경매 상태, 뿌리농가 설정, 현재 페이지와 버전을 한 줄로 보여주는 상태표시창을 상시 표시한다.
- 기존 로그 내용창은 상태표시창 아래에 유지한다.

### 원인

- 기존 페이지 상태표시창은 설정된 페이지가 2개 이상일 때만 표시됐다.
- 응찰서버 연결, 단일·일괄 경매 진행 상태, 뿌리농가 설정과 버전 정보가 하나의 상태표시창으로 구성되어 있지 않았다.

### 변경 파일 및 내용

- `CowAuctionSmall/Views/MainWindow.xaml`: 하단 배치를 `전광판 화면 → 상태표시창 → 로그 내용창` 순서로 변경하고 상태 항목을 가로로 배치했다.
- `CowAuctionSmall/ViewModels/MainWindowViewModel.cs`: 상태 바인딩 속성과 메시지 구독을 추가하고 뿌리농가·페이지·버전·응찰서버·경매 상태를 갱신하도록 했다.
- `CowAuctionSmall/Models/DataChangedMessage.cs`: 단일·일괄 경매 표시 상태 전달용 `AuctionDisplayStateMessage`를 추가했다.
- `CowAuctionSmall/Services/ServerGetData.cs`: 단일경매 진행 계류대 번호와 단일 종료, 일괄 시작·종료 상태를 전달하도록 했다.
- `CowAuctionSmall/NetProto/netty/NettyControllable.cs`: 응찰서버 채널 해제 시 연결 실패 상태 메시지를 전달하도록 했다.
- `WORKLOG.md`: 작업 내용과 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- 메인 창 하단 상태표시와 로그 창의 표시 순서에 영향이 있다.
- 단일경매 READY·START·PROGRESS와 종료, 일괄경매 시작·종료 신호가 상태표시창에 반영된다.
- 전광판 진행·낙찰·유찰 View, 통신 패킷 형식, 마스터 선출 방식과 프로젝트 설정은 변경하지 않았다.

### 빌드 및 테스트 결과

- `dotnet build .\CowAuctionSmall\CowAuctionSmall.csproj --configuration Debug --no-restore`: 실패. `CowAuctionSmall/obj/project.assets.json`이 없어 `NETSDK1004`가 발생했다.
- 프로젝트 규칙에 따라 패키지 복원은 수행하지 않았다.
- `MainWindow.xaml` XML 구문 분석: 성공.
- `git diff --check`: 공백 오류 없음. Git 줄바꿈 변환 안내만 확인했다.
- 응찰서버 실제 연결·해제, 마스터/서브 복수 실행, 단일·일괄 경매 신호와 1~4페이지 화면 수동 확인: 미수행.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `feat: 출하AMS 하단 상태표시창 상시 표시`


## 2026-09-20 - 개발 문서 폴더 상위 이동

### 작업 일자

- 2026-09-20

### 작업 목적

- DeveloperKit 적용으로 생성된 `Docs`와 기존 `소스 설명` 폴더를 프로젝트 폴더 밖의 저장소 상위로 이동한다.

### 원인

- 실행 프로젝트에 포함되지 않는 개발·참고 문서가 `CowAuctionSmall` 프로젝트 폴더 안에 배치되어 프로젝트 소스와 문서의 경계가 명확하지 않았다.

### 변경 파일 및 폴더

- `CowAuctionSmall/Docs`를 저장소 상위의 `Docs`로 이동했다.
- `CowAuctionSmall/소스 설명`을 저장소 상위의 `소스 설명`으로 이동했다.
- `WORKLOG.md`: 폴더 이동과 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- C# 소스, XAML, 프로젝트와 솔루션 설정은 변경하지 않았다.
- 빌드 및 런타임 동작에는 영향이 없다.
- `Docs` 내부의 `Docs/...` 문서 경로는 저장소 루트 기준으로 일치한다.

### 테스트 결과

- `Docs`: 상위 이동 후 파일 15개 확인.
- `소스 설명`: 상위 이동 후 파일 5개 확인.
- Git 추적 파일 279개를 새 구조에 대조해 누락 0개를 확인했다.
- `dotnet sln .\CowAuctionSmall.sln list`: `CowAuctionSmall\CowAuctionSmall.csproj` 참조 확인.
- `dotnet sln .\GAMSTest.sln list`: `GAMSTest\GAMSTest.csproj` 참조 확인.
- 코드 및 프로젝트 설정 변경이 없어 빌드는 수행하지 않았다.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `chore: 개발 문서 폴더를 저장소 상위로 이동`


## 2026-09-20 - AMS 서버 솔루션 폴더 구조 정리

### 작업 일자

- 2026-09-20

### 작업 목적

- 저장소 상위 폴더를 `AMS_Server`로 변경하고, 메인 애플리케이션과 테스트 도구를 각각 `CowAuctionSmall`, `GAMSTest` 형제 폴더로 분리한다.
- `CowAuctionSmall.sln`과 `GAMSTest.sln`은 두 프로젝트의 상위 폴더에 유지한다.

### 원인

- 기존 저장소 루트에 메인 프로젝트 파일과 `GAMSTest` 폴더가 혼재해 두 프로젝트의 경계가 폴더 구조에 명확히 드러나지 않았다.
- 프로젝트 이동 후 솔루션, 테스트용 DLL 참조와 공통 설정 파일의 상대경로를 새 구조에 맞게 변경해야 했다.

### 변경 파일 및 폴더

- 저장소 폴더: `CowAuctionSmall`에서 `AMS_Server`로 이름을 변경한다.
- `CowAuctionSmall/`: 기존 메인 프로젝트 소스, 설정, 리소스와 `CowAuctionSmall.csproj`를 하위 폴더로 이동했다.
- `CowAuctionSmall.sln`: 메인 프로젝트 경로를 `CowAuctionSmall\CowAuctionSmall.csproj`로 변경했다.
- `GAMSTest/GAMSTest.csproj`: 메인 DLL 참조와 `Board.XML`, `users.XML` 참조를 `CowAuctionSmall` 하위 경로로 변경했다.
- `WORKLOG.md`: 폴더 구조 변경과 검증 결과를 기록했다.
- `CODEX_HANDOFF.md`: 갱신하지 않았다.

### 영향 범위

- C# 소스, WPF 화면, MVVM 책임 구조와 런타임 동작은 변경하지 않았다.
- Visual Studio에서는 변경된 상위 경로의 솔루션 파일을 다시 열어야 한다.
- Git에서는 메인 프로젝트 파일이 `CowAuctionSmall/` 아래로 이동한 것으로 표시된다.
- 기존 `bin`, `obj`, `.vs`는 직접 이동하거나 삭제하지 않고 저장소 상위에 유지했다.

### 빌드 및 테스트 결과

- `dotnet sln .\CowAuctionSmall.sln list`: 성공, `CowAuctionSmall\CowAuctionSmall.csproj` 참조 확인.
- `dotnet sln .\GAMSTest.sln list`: 성공, `GAMSTest\GAMSTest.csproj` 참조 확인.
- `dotnet build .\CowAuctionSmall\CowAuctionSmall.csproj --configuration Debug --no-restore`: 실패. 새 프로젝트 위치에 `obj\project.assets.json`이 없어 `NETSDK1004`가 발생했다.
- 기존 루트의 자산 파일을 중간경로로 지정한 비복원 빌드도 기존 자산 파일이 삭제된 `.dotnet-home-versioning` 패키지 경로를 가리켜 `NETSDK1064`로 실패했다.
- `dotnet build .\GAMSTest\GAMSTest.csproj --configuration Debug --no-restore`: 실패. 메인 프로젝트 DLL을 생성하지 못해 참조 어셈블리와 관련 형식을 찾지 못했다.
- 프로젝트 규칙에 따라 NuGet 패키지 복원은 수행하지 않았다.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `chore: AMS 서버 솔루션 폴더 구조 정리`


## 2026-09-18 - .NET CLI 임시 폴더 제외

### 작업 일자

- 2026-09-18

### 작업 목적

- 빌드 검증 중 생성된 저장소 내부 .NET CLI 임시 캐시를 제거하고 향후 Git 변경 목록에서 제외한다.

### 수정 파일

- `.gitignore`: `.dotnet-home-*/` 제외 패턴을 추가한다.
- `WORKLOG.md`: 작업 및 검증 결과를 기록한다.

### 삭제 대상

- `.dotnet-home-paternity-trace`
- `.dotnet-home-versioning`

### 영향 범위

- 소스 코드, 프로젝트 설정, 게시 산출물에는 영향이 없다.
- 저장소 루트에서 생성되는 동일 형식의 로컬 .NET CLI 캐시만 Git 미추적 항목에서 제외된다.

### 테스트 결과

- 삭제 후 `git status --short`에서 두 임시 폴더가 표시되지 않음을 확인했다.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `chore: .NET CLI 임시 폴더 제외`


## 2026-09-18 - 빌드·게시 자동 버전 설정

### 작업 일자

- 2026-09-18

### 작업 목적

- 빌드와 폴더 게시 시점마다 실행 파일과 화면 표시용 어셈블리 버전을 자동으로 갱신한다.

### 수정 파일

- `CowAuctionSmall.csproj`: 2000-01-01 기준 일수와 2초 단위 시간을 이용한 4자리 버전을 `Version`, `AssemblyVersion`, `FileVersion`, `InformationalVersion`에 공통 적용한다.
- `WORKLOG.md`: 작업 및 검증 결과를 기록한다.

### 영향 범위

- 빌드·게시 산출물의 파일/어셈블리 버전과 `MainWindowViewModel.VersionShow()`에서 표시하는 버전이 매 빌드 시점 기준으로 변경된다.
- 소스 동작, 통신 프로토콜, 화면 레이아웃 및 사업장별 표시 규칙은 변경하지 않는다.

### 빌드 및 테스트 결과

- 빌드: `dotnet build .\CowAuctionSmall.csproj --configuration Release --no-restore` 성공.
- 결과: 오류 0개, 경고 102개.
- 버전 확인: `bin\Release\net9.0-windows\CowAuctionSmall.dll`의 어셈블리 버전 `1.0.9757.23949` 확인.
- 런타임 복원: `dotnet restore .\CowAuctionSmall.csproj --runtime win-x64` 성공.
- 게시: `dotnet publish .\CowAuctionSmall.csproj --configuration Release --no-restore -p:PublishProfile=FolderProfile` 성공.
- 게시 산출물: `bin\AMS_Server\CowAuctionSmall.exe` 파일 버전 `1.0.9757.24047` 확인.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `chore: 장성 친자 추적 로그와 자동 버전 설정`


## 2026-09-18 - 장성 친자 배지 원인 추적 로그 추가

### 작업 일자

- 2026-09-18

### 작업 목적

- 장성축협 친자 배지 미표출 시 서버 원본 친자값, 비고 분류, 최종 파싱값, 배지 표시값을 개체 단위로 대조할 수 있게 한다.

### 원인

- 기존 로그에는 친자 원본 필드와 파싱 후 배지 관련 값이 기록되지 않아, 데이터 수신·파싱·화면 바인딩 중 누락 지점을 구분할 수 없었다.

### 수정 파일

- `Services/AnimalParseData.cs`: 장성 코드(`8808990817675`)에 한해 원본 친자값이 `1`이거나 원본 비고에 `친자일치`가 있는 개체의 추적 로그를 추가했다.
- `WORKLOG.md`: 작업 및 검증 결과를 기록했다.

### 수정 내용

- 원본 비고를 파싱 전 상태로 보관해 `contains-친자일치`, `empty`, `other`로 분류한다.
- `[JangseongPaternityTrace]` 로그에 출품번호, 계류대, 원본 친자값, 비고 분류, 최종 친자값, 배지 문구·색상만 기록한다.
- 동일 출품번호·계류대의 동일 추적 상태는 한 번만 기록해 API 폴링에 따른 로그 반복을 막는다.
- 농가명, 개체관리번호, 원본 비고 전문은 기록하지 않는다.

### 영향 범위

- 장성축협 데이터 파싱 시에만 임시 추적 로그가 추가된다.
- 친자 판정값, 비고 내용, 화면 바인딩 및 다른 조합의 표시 로직은 변경하지 않는다.

### 빌드 및 테스트 결과

- 빌드: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` 성공.
- 결과: 오류 0개, 경고 102개.
- 정적 확인: 장성·원본 친자값 `1`, 비고 `친자일치`, 비고 없음+원본 친자값 `1` 조건에서만 추적 로그가 남도록 확인했다.
- 현장 확인: 미수행. 신버전 배포 후 `[JangseongPaternityTrace]` 값과 실제 배지 표시를 대조해야 한다.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `chore: 장성 친자 배지 추적 로그 추가`


## 2026-09-17 — 축산물 이력 조회 결과를 묶어서 표시

- 목적·승인: 사용자의 “변경 계획안을 세우고 진행하자”에 따라 `Docs/축산물이력_묶음표시_변경계획_2026-09-17.md`를 먼저 작성하고 구현·검증을 수행했다. 3명이 구현·독립 검토·오프라인 검증을 분담했다.
- 문제·원인: MainWindowViewModel이 개체별 조회 완료마다 화면을 갱신하고, 다른 경매 메시지의 UpdatePanels에서도 서비스의 중간 캐시를 직접 읽어 이력이 하나씩 뒤늦게 붙었다.
- 신규 `Presentation/Display/AnimalHistoryBatchCoordinator.cs`: 현재 표시 대상의 유효 이력번호를 중복 제거해 단일 흐름에서 조회한다. 임시 조회 결과와 화면 확정 결과를 분리하고, 묶음 완료 후 UI 작업 한 번으로 확정한다. 대상 세대·캐시 기준일·종료 상태를 조회 중과 UI 반영 직전에 확인한다. 대상 변경은 최신 묶음으로 전환하고 반복 polling·수동 연타의 중복 작업을 막는다. 성공 빈값은 이전 문구를 지우고 실패는 기존 정상 표시를 보존한다.
- `ViewModels/MainWindowViewModel.cs`: 개별 QueueHistoryAsync와 서비스 캐시 직접 표시를 제거했다. 자동 조회·수동 새로고침·기준일 변경·Dispose를 조정자에 연결하고 확정된 이력만 표시한다. 완료 시 현재 _visibleCows를 복사하여 이력만 갱신하므로 최신 경매 가격·상태·개체 교체를 덮어쓰지 않는다. UI 표시 실패의 기존 상태 알림을 유지했다.
- `Application/AnimalHistory/AnimalHistoryService.cs`: 읽기 전용 Epoch와 GetAnimalHistoryNoteAsync를 추가해 현재 기준일의 결과 유무·문구를 함께 전달한다. 개별 API 호출·최소 1초 간격·재시도·인증 중단·캐시 저장/보존 로직은 유지했다.
- 검증 파일: 신규 `Validation/ReferenceAdoptionChecks/HistoryBatchChecks.cs`, 기존 Program.cs·README.md에 history 검사 경로를 추가했다. 가짜 지연 응답·UI 작업 큐·메모리 저장소로 부분 노출 차단, 캐시/신규 혼합, 중복·반복 입력, 실패/정상 빈값, 자동→수동 전환, 수동 연타, A→B→A, 지연 UI 콜백, 기준일·Reset·Dispose 및 서비스 결과 의미를 검사했다.
- 검증 명령·결과: `dotnet run --project Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -- history` 28개 통과. 같은 프로젝트 `-- all` 전체 926개 통과. `dotnet run --project Validation/OwnerHistoryChecks/OwnerHistoryChecks.csproj --no-restore -- --offline` 55+22=77개 통과.
- 빌드: `dotnet build CowAuctionSmall.csproj -c Debug --no-restore -v:q` 및 Release 모두 성공. 각각 경고 88개·오류 0개로 직전 기준선과 동일. 패키지 설치·복원·csproj 변경 없음.
- 영향·한계: 경매 진행·가격·낙찰·일반 비고는 계속 즉시 갱신하고 이력만 묶어서 반영한다. 많은 신규 대상이나 지연 응답에서는 묶음 전체의 이력 표시까지 대기가 길어질 수 있다. 실패 후 같은 대상의 반복 polling은 재조회하지 않으며 대상 변경·수동 새로고침·캐시 기준일 변경으로 새 묶음을 시작한다. 이미 시작된 개별 HTTP의 취소 정책과 경매 조회 날짜/당일 캐시 정책은 변경하지 않았다.
- 보호·미수행: 기존 구조 개편·사용자 변경·인코딩/줄바꿈을 보존했다. 인증키·Config·API 주소/파라미터·DB 스키마/보존·텍스트 스크롤·GIF 미지원 정책 유지. 제품 App·운영 서버·전광판·배포 실행 미수행. 전체 ViewModel 연결과 실제 동시 표시 체감은 수동 확인 대상이다.
- Git: master, 시작 기준 52c554d. 시작 상태·관련 diff와 대상 파일의 현재 내용을 별도 보관하고 최종 관련 변경을 확인했다. 새 코드의 공백 검사 통과. commit hash 미생성, push 미수행.


## 2026-09-17 — WPF 유지 기반 절충형 파일 구조 개편

- 목적·승인: 파일 위치와 코드 의존성을 업무 규칙·처리 흐름·외부 구현·화면 표시로 구분한다. 사용자의 구조 개편 계획 실행 요청에 따라 총 3명이 통합 구현·통신/저장 구현·독립 검증을 분담했다. 단일 WPF 프로젝트와 GIF 미지원 정책을 유지했다.
- 문제·원인: 기존 Models·Services·Utils에 데이터·파서·저장·화면 컨트롤이 혼재하고 Application에 해당하는 처리 코드가 HTTP·XML·Netty·WPF 구현을 직접 사용했다. 파일 이동과 실제 책임 분리를 별도 단계로 진행했다.
- 이동 파일: 기존 소스 80개를 Domain·Application·Infrastructure·Presentation 및 Views/Common으로 이동했다. 기존 경로와 최종 경로 전부를 `Docs/파일구조_안내.md`의 이동표에 기록했다. 소스 namespace, 소비자의 using·완전 수식 이름, XAML xmlns와 검증 참조를 갱신했다. Views의 기존 해상도·사업장 경로는 유지했다.
- Domain: 경매 모델·상태 해석·계류대별 개체 선택 규칙, 설정과 이력 결과를 배치했다. `Domain/Settings/AnimalTraceSettings.cs`로 설정을 분리하고 `AuctionAuthenticationResult.cs`, `Domain/Auction/AuctionRoundInfo.cs`를 추가해 외부 응답 형식 의존을 차단했다. `LoginResult`의 JSON 파싱과 기존 Qcn은 Infrastructure/Api에 유지했다.
- Application: 기존 IAuctionDataSource와 신규 `IAppLog`, `IAuctionSessionRuntime`, `IAuctionPacketParser`, `IAnimalHistoryClient`, `IDailyCacheStore` 계약을 Application/Abstractions에 배치했다. 경매·이력 처리 코드의 Infrastructure·WPF 직접 참조를 제거했다. 내부 접속 결과 메시지는 결과 문자열만 전달한다.
- `Application/Auction/ServerGetData.cs`: `AuctionSnapshotParser.cs`로 응답 검증·정렬을, `AuctionPollingLifetime.cs`로 polling 시작·취소·완료를 분리했다. 경매 상태 변경·세대 검사·재동기화는 기존 단일 소비 흐름에 유지했다. 상태 조정자를 의미 없이 여러 파일로 나누지는 않았다.
- Infrastructure: `AuctionSessionRuntime.cs`, `ServerAuctionDataSource.cs`가 기존 설정·인증·조회·연결 구현을 계약에 연결한다. 기존 AnimalParseData·AnimalHistoryClient·DailyCacheService·NLogger가 필요한 계약을 구현한다. Netty 재시작 요청은 `ApplicationRestartRequestedMessage.cs`로 전달하고 PageTimerSync는 dispatch 함수를 주입받아 UI 직접 참조를 제거했다.
- Presentation: `DisplayPanelStore.cs`로 패널 상태·진행 ViewModel·두 비고 호스트 해제 수명을 분리하고 `RunningPageSchedule.cs`로 기존 UTC 페이지 계산을 추출했다. DisplaySelect의 화면 선택·고정·전환 동작과 ViewModel·호스트 해제 순서를 보존했다. OutlinedTextBlock은 Converter 대신 Views/Common에 배치했다.
- `App.xaml.cs`: 새 계약의 실제 구현을 서비스에 등록하고 재시작 요청의 구독·실행·해제를 담당한다. PageTimerSync의 UI dispatch는 DisplaySelect에서 기존 Dispatcher 인스턴스를 캡처해 BeginInvoke로 전달한다. MainWindowViewModel은 기존 보드·변경 감지 분리를 활용하여 이번에는 필요한 참조만 갱신했다.
- Validation: 기존 검사의 namespace·생성자·가짜 조회 경계를 갱신했다. 신규 `ReferenceAdoptionChecks/DisplayLifetimeChecks.cs`, `PollingLifetimeChecks.cs`에서 패널 해제·페이지 계산·주기 작업 취소와 fake 초기화 대기 취소를 검사한다. OwnerHistoryChecks에 `--offline`을 추가해 RunChecks+MoreChecks만 실행하고 제품 App·UiChecks·LiveChecks로 진입하지 않게 했다. 양쪽 README와 구조 개편 계획·현재 구조 안내를 갱신했다.
- 기준선·단계 검증: 시작 Reference 전체 882개 통과. 1단계 Debug 및 stage1 46개·stage2 34개·stage3 219개 통과. 2단계 Debug 및 stage3 219개 통과. 파일 이동 완료 시 Debug·Release 각각 경고 90개·오류 0개 및 전체 882개 통과. 이동 중 빠진 NLogger import와 책임 분리 후 남은 CowDisplaySelector 완전 수식 참조를 수정하고 검증을 이어갔다.
- 최종 경매 검사: `dotnet run --project Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -- all`에서 897개 통과. 이후 fake 네트워크 대기→StopAsync 취소 검사 1개 추가 후 같은 명령의 `-- stage3`에서 222개 통과. 현재 정의 총 898개이며 마지막 추가 후 전체를 반복 실행한 것으로 기록하지 않는다.
- 이력·저장 검사: 기존 복원 자산 문제로 `dotnet restore Validation/OwnerHistoryChecks/OwnerHistoryChecks.csproj` 수행 후 `dotnet run --project Validation/OwnerHistoryChecks/OwnerHistoryChecks.csproj --no-restore -- --offline` 성공. fake HTTP와 고유 임시 SQLite/XML을 사용하는 55+22=77개 통과. 패키지 버전·csproj 변경 없음.
- 최종 빌드: `dotnet build CowAuctionSmall.csproj -c Debug --no-restore`, `dotnet build CowAuctionSmall.csproj -c Release --no-restore` 모두 성공. 최종 Debug는 증분으로 경고 0개·오류 0개를 출력했으며, 같은 단계의 실제 Debug 컴파일과 최종 Release는 경고 88개·오류 0개다. 기존 기준선 90개 대비 2개 감소이며 경고 전체 해소를 의미하지 않는다.
- 구조·변경 보호: 이동 80건의 원본 소스 위치 제거·대상 존재, 계층 namespace/폴더 불일치 0건, XAML/code-behind 75쌍 일치, 이전 namespace 실행 참조 0건과 금지 의존성 제거를 확인했다. 이동 전 현재 파일을 별도 보관하고 내용 식별값으로 Config·프로젝트·게시 프로필·규칙 등 18개 보존을 확인했다. 기존 파일 인코딩·줄바꿈 유지, 이동 후 빈 기존 소스 디렉터리 12개만 제거했다. bin·obj·.vs·.git 수동 정리 없음.
- 범위·한계: 승인된 구조 개편·책임 분리·검증 범위. 기존 경매 표시 정책·큐/세대 처리·HTTP 형식·캐시 키/JSON·DB 경로/보존·사용자 설정·정적 로고 정책 유지. 모든 클래스의 설정 의존성을 추상화하거나 전체 MVVM을 재작성한 작업은 아니다. 실제 서버·전광판·전체 사업장 화면·다중 PC master/sub/fallback·성능 전후 비교·운영 앱 실행·게시·배포는 미수행이다.
- Git: master, 시작 기준 52c554d. 기존 미커밋·미추적 내용을 보존하여 이동했고 관련 diff 및 새 파일을 검토했다. 전체 git diff --check에는 기존 Config/users.XML 및 RunningNoteHost128.xaml의 공백 오류가 남아 있다. commit hash 미생성, push 미수행.


## 2026-09-17 — 참고 프로젝트 선별 적용 및 GIF 미지원 정리

- 목적·승인: 참고 프로젝트 비교 계획의 4단계를 사용자의 “구현 진행하자”에 따라 구현했다. 총 3명의 에이전트가 구현·통신 검토·오프라인 검증을 분담했다. GIF는 지원하지 않는다.
- 문제·원인: GIF 자동 선택·표시 의존성이 남아 있었고, 상태 문자열 직접 파싱과 변경 가능한 메시지 데이터, 조회 결과와 경매 이벤트의 비동기 적용이 표시 불일치·오래된 응답 반영 위험을 만들었다. 전체/부분 갱신 및 원본 행/실제 슬롯 집계 구분도 보완했다.
- `CowAuctionSmall.csproj`, `Models/LogoManager.cs`, `Utils/ImageCacheHelper.cs`: WpfAnimatedGif 참조·자동 탐색·표시 경로 제거. BMP·PNG·JPG·JPEG 실제 디코더만 허용하고 GIF 위장 파일도 차단. 기본 logo.bmp 대체와 실패 시 빈 로고 표시. 기존 다른 패키지 버전·이미지 파일 보존.
- 신규 `Models/Structures/BoardSourceStatistics.cs`, `Services/BoardDisplayTracker.cs`: 원본 무효·중복 행과 현재 고유 계류대, 물리 슬롯의 표시·제외·로고 집계 분리. 전체 교체·부분 병합·명시적 삭제 처리.
- 신규 `Models/Structures/DisplayAuctionState.cs`, `Services/AuctionStateResolver.cs`: UI 생성에서 상태 판단 분리. 비정상 값에 로고, 22·23 결과 우선, 실제 상태 24는 기존 로고. AF24 이벤트의 기존 updateResultStatus:false 동작은 유지.
- `Models/DataChangedMessage.cs`, `Models/Structures/gValues.cs`: 송신 시 컬렉션과 값 복제, 전체/부분 및 원본 집계 전달. 누락된 Clone 필드와 표시 비교 항목 보완.
- 신규 `Services/AuctionIngress.cs`, `AuctionInput.cs`, `IAuctionDataSource.cs`: 상태 큐 256·HTTP 큐 16, 타입별 입력·세대 관리, 중복 요청 병합, 오래된 응답 차단, 포화·예외 진단과 전체 조회 재동기화 경로. 오프라인 가짜 조회 서비스 주입 지원.
- `Services/ServerGetData.cs`, `NettyAsyncMsgProcess.cs`, `NetProto/netty/handlers/AuctionClientInboundDecoder.cs`, `App.xaml.cs`: HTTP 조회와 상태 적용 분리, 진행 위치·확정 결과 보존, 날짜별 상태 분리, 불완전한 API 행 거부, QCN 날짜 경합 차단, 취소 가능한 polling과 구독/큐 종료. Netty 수신 스레드의 직접 HTTP 조회 제거.
- 신규 `Services/DisplayUpdateTracker.cs`, `Models/Structures/PageRotationSnapshot.cs`, `Services/DisplaySelect.cs`: 변경 없는 표시 생략, 마지막 값의 독립 복제 비교, 재사용 비고 호스트 수명 정리, 페이지 고정 사유 상태화. 기존 공통 컨트롤·사업장 화면·PC 동기화 프로토콜 유지.
- `ViewModels/MainWindowViewModel.cs`, `Views/MainWindow.xaml`: 패널 이름 인덱스, 전체 조회 누락 슬롯 초기화와 부분 갱신 보존, 보드 진단, 비동기 이력 표시 순서 보완, 종료 후 UI 반영 방지, 페이지 고정 사유 tooltip.
- 신규 `Validation/ReferenceAdoptionChecks/`: 합성 로고·보드·상태·스냅샷·큐·통신·표시 및 WPF 직접 생성 검사. fake 조회 서비스와 숨겨진 HWND만 사용하고 제품 App·MainWindow·DisplaySelect·InitializeAsync는 실행하지 않음. 설정·운영 서버·실제 개인정보 미사용. `Docs/참고프로젝트_선별적용_개선계획_2026-09-17.md`에 완료 결과 반영.
- 복원: `dotnet restore CowAuctionSmall.csproj`, `dotnet restore Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj` 성공.
- 단계 검증: `dotnet run --project Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -- stage1` 및 stage2·stage3·stage4로 단계별 검증 수행. 이후 같은 명령의 `-- all`에서 879개 assertion 통과. 마지막 QCN 날짜 경합 보완 후 `-- stage3`로 다시 빌드·검증해 219개 통과(신규 3개 포함). 최종 정의는 총 882개이며 마지막 변경 뒤 전체 재실행은 하지 않았다.
- 최종 빌드: `dotnet build CowAuctionSmall.csproj -c Debug --no-restore`, `dotnet build CowAuctionSmall.csproj -c Release --no-restore` 모두 성공. 각각 경고 90개·오류 0개. nullable·미사용·obsolete·MVVM 등의 경고가 남아 있다.
- 영향·한계: 승인된 4단계 범위. 큐 포화 시 무손실을 보장하지 않으며 진단 후 전체 조회로 재동기화한다. 종료 후 상태 반영은 차단하지만 기존 HTTP API가 전송 취소를 제공하지 않아 시작된 요청은 내부적으로 완료될 수 있다.
- 추가 확인·미수행: 실제 서버·전광판 연동, 모든 사업장/해상도 조합, 전체 MVVM·이력 서비스·페이지 회전 연동, 다중 PC master/sub·timeout fallback, 실제 성능 전후 측정. 운영 앱 실행·게시·배포 교체·SDK 설치 미수행. 합성 검사 통과를 현장 성공이나 성능 향상으로 해석하지 않는다.
- Git·변경 보호: master, 시작 기준 52c554d. 시작 시 보관한 관련 파일과 비교하여 이번 변경을 구분하고 기존 미커밋·미추적 변경과 설정을 보존했다. 전체 diff 검사에서는 기존 미수정 파일(Config/users.XML·Services/AnimalParseData.cs·RunningNoteHost128.xaml)의 공백 오류가 남아 있으며 이번 작업 대상으로 확대하지 않았다. commit hash 미생성, push 미수행.


## 2026-09-16 - 양평 진행 1페이지 중량/육종 설정 반영

### 작업 일자

- 2026-09-16

### 작업 목적

- 양평축협 진행 1페이지에서 송아지가 `SelectShowWeight_EPD` 설정과 관계없이 육종으로 고정 표출되는 문제를 수정한다.

### 원인

- `YangpyeongRun.xaml`이 송아지 구분값(`1`)만으로 육종 표시를 결정하고, `SelectShowWeight_EPD` 값을 조건에 사용하지 않았다.

### 수정 파일

- `Views/Size128_128/Running/CustomAuctionRunning1/YangpyeongRun.xaml`: 송아지는 설정값 `Y`일 때 중량, `N`일 때 육종을 표시하도록 다중 조건을 추가했다.
- `WORKLOG.md`: 작업 및 검증 결과를 기록한다.

### 영향 범위

- 양평축협 128x128 진행 1페이지의 송아지 표시만 변경한다.
- 비육우·번식우의 중량 표시, 진행 2페이지 유전능력 화면, 낙찰·유찰 및 장성축협 화면은 변경하지 않는다.

### 빌드 및 테스트 결과

- 빌드: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` 성공(오류 0개, 기존 경고 100개).
- 정적 확인: 송아지 `SelectShowWeight_EPD=Y`는 중량, `N`은 육종으로 분기하며 비육우·번식우의 기존 중량 분기는 유지한다.
- 현장 수동 확인: 미수행. 양평 송아지에서 `Y`/`N` 각각의 진행 1페이지 표시 확인 필요.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 양평 진행 화면 중량 설정 반영`


## 2026-09-16 - 문경 단일경매 낙찰 후 진행 화면 재전환 방지

### 작업 일자

- 2026-09-16

### 작업 목적

- 문경축협 단일경매에서 낙찰 확정된 번식우(SIP 361, 개체번호 `1430 1945 4`)가 후속 `AS8002` 신호로 진행 화면 및 테두리 점멸 상태로 되돌아가는 문제를 방지한다.

### 원인

- 낙찰 결과 `AF=22` 뒤에 동일 SIP의 `AF=24`, `AS=8002`가 수신되면, 신버전이 `AS8002`를 즉시 진행 상태로 처리하여 낙찰 상태 `22`를 `11`로 덮어썼다.
- 구버전은 이 단독 `AS8002`를 진행 상태 전환에 사용하지 않아 낙찰 화면을 유지했다.

### 수정 파일

- `Services/ServerGetData.cs`: 낙찰 상태(`22`)이고 진행 중이 아닌 동일 SIP는 후속 `AS8002`, `AS8003`, `AS8004`로 진행 상태 전환하지 않도록 차단 로그와 함께 건너뛴다.
- `WORKLOG.md`: 작업 및 검증 결과를 기록한다.

### 영향 범위

- 단일경매에서 낙찰 확정된 SIP의 후속 진행 신호만 차단한다.
- 유찰(`23`) 후 재경매 및 일반적인 `11` 상태의 경매 시작 처리에는 영향을 주지 않는다.

### 빌드 및 테스트 결과

- 빌드: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` 성공(오류 0개, 기존 경고 100개).
- 정적 확인: `AF=22` 후 `AS8002/8003/8004`는 상태를 변경하지 않고, `23` 또는 `11` 상태는 기존 진행 처리 경로를 유지한다.
- 현장 수동 확인: 미수행. `22 -> 24 -> AS8002` 시퀀스에서 낙찰 화면 유지, 유찰 후 실제 재경매의 진행 화면 전환 확인 필요.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 낙찰 후 단독 진행 신호 재전환 방지`


## 2026-09-16 - 문경 진행 1페이지 출하주명 위치 미세 조정

### 작업 일자

- 2026-09-16

### 작업 목적

- 문경축협 128×128 진행 1페이지의 출하주명을 기준 위치에서 오른쪽으로 2px 이동한다.

### 원인

- 화면 비교 결과 출하주명 시작 위치를 조금 더 오른쪽으로 조정할 필요가 있었다.

### 수정 파일

- `Views/Size128_128/Running/Standard_non_QQuri_Run1_1.xaml`: `CowInfo.OwnerName`의 좌측 여백을 `32`에서 `34`로 변경했다.
- `WORKLOG.md`: 작업 및 검증 결과를 기록했다.

### 영향 범위

- 문경축협 코드(`8808990656427`)의 128×128 진행 1페이지 출하주명 위치만 오른쪽으로 2px 이동한다.
- 출하주명 색상, 지역명, 개체번호 및 다른 축협 화면에는 영향을 주지 않는다.

### 빌드 및 테스트 결과

- 빌드: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` 성공 (오류 0개, 기존 경고 100개).
- 정적 확인: `CowInfo.OwnerName`의 좌측 여백이 `34`로 설정되어 있다.
- 수동 화면 확인: 미수행. 문경 코드(`8808990656427`)의 128×128 진행 1페이지에서 출하주명이 이전보다 오른쪽으로 2px 이동했는지 확인 필요.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 문경 진행 1페이지 출하주명 위치 미세 조정`


## 2026-09-16 - 문경 진행 1페이지 출하주명 시작 위치 정렬

### 작업 일자

- 2026-09-16

### 작업 목적

- 문경축협 128×128 진행 1페이지에서 출하주명 첫 글자를 `육종 :ABCC`의 `A` 시작 위치에 맞춘다.

### 원인

- 출하주명이 출하지역명 뒤에 이어 표시되어 지역명 글자 폭에 따라 시작 위치가 달라졌다.

### 수정 파일

- `Views/Size128_128/Running/Standard_non_QQuri_Run1_1.xaml`: 출하지역명과 출하주명을 별도 TextBlock으로 분리하고, 출하주명 좌측 여백을 `32`로 고정했다.
- `WORKLOG.md`: 작업 및 검증 결과를 기록했다.

### 영향 범위

- 문경축협 코드(`8808990656427`)의 128×128 진행 1페이지 출하주명 시작 위치에만 적용된다.
- 출하주명 색상, 개체번호, 지역명 값, 다른 축협 화면 및 진행 페이지 선택 로직에는 영향을 주지 않는다.

### 빌드 및 테스트 결과

- 빌드: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` 성공 (오류 0개, 기존 경고 100개).
- 정적 확인: 출하지역명은 좌측 여백 `2`, 출하주명은 좌측 여백 `32`로 분리되어 있다.
- 수동 화면 확인: 미수행. 문경 코드(`8808990656427`)의 128×128 진행 1페이지에서 출하주명 첫 글자가 `육종 :` 뒤 `A`의 시작 위치와 일치하는지 확인 필요.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: 문경 진행 1페이지 출하주명 위치 정렬`


## 2026-09-16 - 문경 진행 1페이지 출하주명 색상 변경

### 작업 일자

- 2026-09-16

### 작업 목적

- 문경축협 128×128 진행 1페이지의 출하주명 색상을 Cyan에서 Yellow로 변경한다.

### 원인

- 문경 전용 화면의 출하주명 Foreground가 Cyan으로 고정되어 요청 색상과 달랐다.

### 수정 파일

- `Views/Size128_128/Running/Standard_non_QQuri_Run1_1.xaml`: `CowInfo.OwnerName`의 Foreground를 `Yellow`로 변경했다.
- `WORKLOG.md`: 작업 및 검증 결과를 기록했다.

### 영향 범위

- 문경축협 코드(`8808990656427`)의 128×128 진행 1페이지 출하주명 표시에만 적용된다.
- 개체번호, 지역명, 다른 축협 화면 및 진행 페이지 선택 로직에는 영향을 주지 않는다.

### 빌드 및 테스트 결과

- 빌드: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` 성공 (오류 0개, 기존 경고 100개).
- 정적 확인: 변경된 `CowInfo.OwnerName` 바인딩의 Foreground가 `Yellow`인지 확인.
- 수동 화면 확인: 미수행. 문경 코드(`8808990656427`)의 128×128 진행 1페이지에서 출하주명이 노란색으로 표시되는지 확인 필요.

### Git

- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `fix: set Mungyeong owner name color to yellow`


## 2026-09-15 — 일반 비고 빨간색·축산물 이력 흰색 표시

- 목적: 사용자 요청에 따라 일반 비고와 축산물 이력을 서로 다른 글자색으로 구분하면서 하나의 영역에서 연속 스크롤한다.
- 신규 `Views/Common/NoteText.cs`: Normal·History 연결 속성으로 한 TextBlock 안에 빨간색 일반 비고·구분자와 흰색 이력 Run 구성. 빈 값·대시 처리 유지. 바인딩 업데이트를 모아 처리하고 Text 값을 먼저 동기화하여 기존 스크롤 측정과 변경 감지를 유지.
- 비고 바인딩 XAML 13개: 공통 연결 속성 적용. 128×128 공통 진행·낙찰·유찰 호스트, 기존 별도 비고 화면 및 128×64·160×64·320×64 진행 화면에 반영.
- `ViewModels/AuctionContPanelViewModel.cs`: NoteChangeOwner 읽기 속성·변경 알림 추가, 일반 비고와 이력의 변경을 각각 전달. 원본 Note·API·SQLite·사용 여부 설정은 변경하지 않음.
- 신규 `Validation/OwnerHistoryChecks/NoteColorChecks.cs`, `UiChecks.cs`: 합성·색상·빈 값·미사용·이력 단독 6개, 진행/낙찰/유찰 및 대표 해상도 실제 WPF Run 색상 7개 검증 추가. 설정·검증 문서 갱신.
- 검증: `dotnet run --project .\Validation\OwnerHistoryChecks\OwnerHistoryChecks.csproj --configuration Release --no-restore` 성공(55+22+6+16+31=130개). 스크롤 수명 주기 16개 회귀 통과. `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` 성공. Debug·Release 경고 90·오류 0. XAML 13개 파싱·속성 연결 및 관련 diff 확인.
- 영향·미수행: 승인된 비고 색상 구분 범위. 실제 운영 서버 추가 조회·앱 실행·재게시·모든 전광판 장비 검증 미수행. 기존 사용자 변경 보존.
- Git: status·관련 diff 확인, commit hash 미생성, push 미수행.


## 2026-09-15 — 축산물 이력 사용 여부 옵션 연결

- 목적·원인: 사용자가 AnimalTrace 하위에 추가한 IsAnimalProductsTraceability로 조회와 비고 표시를 함께 제어해 미사용 시 API 호출을 없앤다.
- `Models/Structures/AnimalHistoryResult.cs`, `Models/XMLParser/UserXmlParser.cs`: Y이며 인증키가 있을 때만 Enabled 활성화. 기본 N, 누락·빈 값·잘못된 값은 비활성화.
- `ViewModels/MainWindowViewModel.cs`, `Services/AnimalHistoryService.cs`: 미사용 시 자동·수동 조회 차단, 메모리 이력 표시 차단, 기존 Note 유지. 당일 DB는 삭제하지 않음. 설정 적용은 재시작 기준.
- 대체 사업장 XML 7개: AnimalTrace 하위에 기본 N 추가. 사용자가 작성한 현재 users.XML의 Y와 인증키는 수정하지 않고 보존.
- `Validation/OwnerHistoryChecks/Program.cs`, `UiChecks.cs`: Y 설정의 기존 표시 검증 유지, N·누락·잘못된 값·수동 호출 차단·캐시 보존·신규 저장 없음·실제 WPF 원래 비고 유지 검증 추가. SQLite 계획과 설정·검증 문서 갱신.
- 검증: `dotnet run --project .\Validation\OwnerHistoryChecks\OwnerHistoryChecks.csproj --configuration Release --no-restore` 성공(55+22+16+24=117개). `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` 성공. Debug·Release 경고 90·오류 0. XML 8개 실제 경로·Y/N 값 확인.
- 영향·미수행: 이력 API에만 적용, 경매 현황 조회 정책 유지. 실제 서버 추가 호출·앱 실행·재게시 미수행. 기존 사용자 변경 보존, 관련 diff 확인. commit hash 미생성, push 미수행.


## 2026-09-15 — 페이지 전환 시 비고 스크롤 연속성 보완

- 목적·원인: 소유주 이력이 기존 비고 뒤에 연결되어 있으나 페이지 전환 때 공통 비고 호스트가 애니메이션을 매번 재생성해 문구 뒷부분 도달 전에 되돌아가는 현상을 실제 앱에서 재현함.
- `Views/Size128_128/Running/RunningNoteHost128_Running.xaml.cs`, `RunningNoteHost128.xaml.cs`: 실행 중 애니메이션 재사용, 같은 개체의 데이터 객체 교체에도 연속 유지. 다른 개체·다른 ViewModel은 재초기화하고 문구 변경은 기존 텍스트 변경 이벤트로 재시작. Dispatcher 초기화 중복 예약 방지, Unloaded 시 대기 작업 취소·애니메이션 해제.
- `Validation/OwnerHistoryChecks/ScrollChecks.cs`, `UiChecks.cs`: 실제 WPF 클록을 사용한 16개 검증 추가. 페이지 반복 전환 뒤 꼬리 부분 위치 유지·동일 개체 갱신·문구 변경·개체 변경·언로드 및 재진입 검증. 설정·검증 문서 반영.
- 검증: `dotnet run --project .\Validation\OwnerHistoryChecks\OwnerHistoryChecks.csproj --configuration Release --no-restore` 성공(48+22+16+21=107개). `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` 성공. Debug·Release 빌드 경고 90·오류 0. 관련 diff --check 통과.
- 실제 앱: 최신 Debug 실행 후 동일 문구의 X 좌표가 -227, -264, -300까지 진행하고 문구 전체 폭 328을 지나 정상 반복함을 UI Automation으로 확인. 이후에도 지속 이동 확인. 문구 불변·앱 응답 정상, 확인용 앱 종료 요청. 개인정보·원문은 기록하지 않음.
- 영향·범위: 승인된 비고 스크롤 보완. API·SQLite·호출 정책 변경 없음. 실제 전체 사업장 및 전광판 조합 검증·재게시 미수행.
- Git: 관련 status·diff 확인, 기존 사용자 변경 보존. commit hash 미생성, push 미수행.


## 2026-09-15 — 임의 하루 100회 호출 제한 해제

- 목적·원인: 사용자 지정 또는 공급자 공식 한도가 아닌 기본 100회 예산 때문에 신규 개체 조회가 중단됨. 사용자가 해당 제한 해제를 요청함.
- `Models/Structures/AnimalHistoryResult.cs`: DailyLimit 기본값 0(로컬 일일 제한 없음). `Services/AnimalHistoryService.cs`: 양수 제한이 설정된 경우에만 집계와 비교. 기존 호출 집계·API 인증 오류 및 실제 한도 초과 중단·최소 요청 간격·캐시 정책 유지.
- `Validation/OwnerHistoryChecks/Program.cs`: 기본 무제한 설정, 기존 집계 100에서 101번째 요청 성공·집계 보존·재시작 후 추가 요청 검증. SQLite 계획 및 설정·검증 문서 갱신.
- 검증: `dotnet run --project .\Validation\OwnerHistoryChecks\OwnerHistoryChecks.csproj --configuration Release --no-restore` 성공(48+22+21=91개). `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` 성공(경고 90·오류 0). Release 빌드도 검증 프로젝트 실행에서 성공.
- 영향 범위: 승인된 로컬 호출 제한 해제. 운영 DB 삭제·집계 초기화·실제 API 추가 호출·앱 실행·게시 미수행. 스크롤 문제는 이번 변경에 포함하지 않음.
- Git: 관련 status·diff 확인, 기존 사용자 변경 보존. commit hash 미생성, push 미수행.


## 2026-09-15 — 이력 API 설정을 XML 인증키 하나로 간소화

- 목적·원인: 사용자 요청에 따라 여러 XML 옵션과 별도 키 파일 구성을 단순화한다.
- users.XML 및 대체 XML 7개: AnimalTrace 하위에 ServiceKey만 남기고 기존 로컬 키를 이전. 실제 키는 작업 기록에 남기지 않음.
- `Models/Structures/AnimalHistoryResult.cs`, `Models/XMLParser/UserXmlParser.cs`, `Services/AnimalHistoryClient.cs`: XML 인증키 읽기, 주소·예산 100회·간격 1000ms 소스 기본값 적용, 키가 비어 있으면 조회 비활성화. 옵션 2·재시도 유지.
- `Services/AnimalHistoryService.cs`: 이전 당일 호출 예약·중단 상태의 식별자를 유지해 설정 전환으로 호출 한도가 초기화되지 않도록 함.
- `CowAuctionSmall.csproj`: 로컬 JSON 자동 복사 설정 제거. 기존 JSON 파일과 제외 규칙은 보존하고 앱에서는 더 이상 읽지 않음. XML은 기존 출력·게시 정책에 따라 키도 함께 전달됨.
- `Validation/OwnerHistoryChecks/` 4개 소스: 가상 인증키를 XML/설정에 직접 공급, 고정 endpoint의 가짜 HTTP 처리, XML 키·소스 기본값·빈 키·HTTP 미호출 검증 추가. SQLite 계획·설정 검증·OpenAPI 가이드 3개 문서 갱신.
- 검증: `dotnet run --project .\Validation\OwnerHistoryChecks\OwnerHistoryChecks.csproj --configuration Release --no-restore` 성공, 45+22+21=88개 통과. Debug `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` 성공(경고 90·오류 0). Release도 검증 프로젝트 빌드 성공. XML 8개 키 이전 일치·단일 항목 확인, 관련 소스 diff --check 통과.
- 영향·미수행: 실제 API 추가 호출·운영 앱 실행·재게시 미수행. 화면 이탈 개체의 대기 요청 취소는 이번 설정 변경 범위에 포함하지 않음. 이전 사용자 변경 보존.
- Git: status 및 관련 변경 확인, commit hash 미생성, push 미수행. 승인된 설정 간소화 범위와 일치.


## 2026-09-15 — SQLite 당일 캐시 및 소유주 변경 이력

- 목적·원인: 반복 이력 호출을 줄이고 기존 비고를 보존하면서 소유주 변경을 표시한다. 승인된 구현·XML 설정·문서·검증 범위에 해당한다.
- 신규 `Services/DailyCacheService.cs`, `AuctionSnapshotCache.cs`: 단일 daily_cache, 한국 날짜 초기화, 늦은 응답 차단, 설치별 로컬 저장·잠금, 만료 설치본 정리, 허용된 화면 필드 저장 및 인증 범위 분리.
- 신규 `Services/AnimalHistoryClient.cs`, `AnimalHistoryParser.cs`, `AnimalHistoryNoteFormatter.cs`, `AnimalHistoryService.cs`, `Models/Structures/AnimalHistoryResult.cs`: 옵션 2 조회, 별도 파싱·표시, 정상 0건 포함 캐시, 동시 요청 공유, 예약 예산·재시도·인증 및 한도 중단 보존.
- `CowAuctionSmall.csproj`, `App.xaml.cs`: Microsoft.Data.Sqlite 10.0.12, 서비스 등록·종료, 개발 인증 파일 복사·게시 제외, Validation 소스의 앱 컴파일 제외.
- `Models/Structures/UserInfo.cs`, `Models/XMLParser/UserXmlParser.cs`, users.XML 및 대체 XML 7개: AnimalTrace 주소·활성화·키 파일·예산·간격 설정. DailyLimit 100은 공식 한도가 아닌 로컬 기본 예산이며 PC 간 합산하지 않는다.
- `Models/Structures/gValues.cs`, `Services/AnimalParseData.cs`: 원본 TraceNumber 및 NoteChangeOwner·DisplayNote 분리, 복제·비교 반영. `Services/ServerConn.cs`, `ServerGetData.cs`: 경매 캐시 복원·갱신, 통신 실패와 정상 빈 응답 구분.
- `ViewModels/MainWindowViewModel.cs`, `AuctionContPanelViewModel.cs`, `Views/MainWindow.xaml`, 비고 바인딩 XAML 13개: 비동기 응답 시 표시 객체 교체, 기존 Note 보존, 날짜 변경 초기화, 상태창 우클릭 수동 갱신. 초기 로고와 다른 패널 갱신 시 이력 유지.
- `Validation/OwnerHistoryChecks/`: 재실행 가능한 가상 검증 및 명시적 실제 조회 모드. SQLite 계획·OpenAPI Markdown 가이드 및 신규 `Docs/소유주이력_설정_검증.md`: 구현·설정·배포·검증 기록.
- 빌드: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore` 및 Release 성공. 최종 증분 경고 0·오류 0이며 전체 경고 해소를 뜻하지 않는다.
- 자동 검증: `dotnet run --project .\Validation\OwnerHistoryChecks\OwnerHistoryChecks.csproj --configuration Release --no-restore` 성공. 기본 41·추가 22·WPF 21, 총 84개 통과.
- 게시: `dotnet publish .\CowAuctionSmall.csproj --configuration Release --runtime win-x64 --self-contained false -p:PublishSingleFile=true --output .\bin\OwnerHistory_Validation_20260915` 성공(경고 90·오류 0). 네이티브 DLL 포함·키 제외 확인. 게시 앱 직접 실행에서 SQLite 로딩·스키마 1·단일 테이블·경매/제어 저장을 확인하고 종료 요청.
- 실제 연동: 실제 API 정상 응답·이력 2건·변경 문구 생성 확인. 실제 앱 로그인 코드 일치·응찰 연결·당일 경매/이력/제어 저장 확인. 원본 응답·개체번호·소유주명·인증 정보는 기록하지 않음.
- Git: 기존 .NET 전환·로그인·사업장 화면 등 사용자 변경 보존. status·관련 diff 확인. 전체 diff --check에는 AnimalParseData 빈 줄 및 RunningNoteHost128의 후행 공백이 남아 있으며 관련 없는 포맷 정리는 하지 않음. commit hash 미생성, push 미수행.
- 추가 확인: 모든 사업장·실제 전광판 조합 검증은 미수행. 앱 종료 중 날짜 삭제는 다음 실행 시 수행하며 외부 백업 삭제는 해당 없음. 배포 시 로컬 키 별도 설치 필요. 공식 한도 및 공유 계정 총사용량은 로컬 예산과 별개.


## 2026-09-15 - 테스트용 일괄경매 상태 및 결과 로그 추가

### 작업 날짜

- 2026-09-15

### 작업 목적

- 일괄경매 로그만으로 송아지, 비육우, 번식우별 진행 시작·종료와 낙찰/유찰 결과 화면 반영 흐름을 검증할 수 있도록 테스트 전용 로그를 추가했다.

### 원인

- 기존 일괄경매 로그에는 시작·종료 Debug 출력만 있고, 대상 개체의 축종·패널·진행 상태 및 결과 재조회 값을 출품번호별로 대조할 정보가 부족했다.

### 수정 파일

- `Services/ServerGetData.cs`: 일괄 `AS8004` 시작과 `AS8006`/`SD F` 종료 시 대상 개체를 `[TestBatchState]`, `[TestBatchItem]`으로 기록하고, 결과 재조회 시 `[TestResult]`를 기록한다.
- `WORKLOG.md`: 작업 및 검증 결과를 기록한다.

### 영향 범위

- 경매 상태 판정, API 재조회, 화면 선택, 타이머 및 통신 동작은 변경하지 않았다.
- 테스트 중 로그량만 증가한다. 테스트 종료 후 `[TestBatchState]`, `[TestBatchItem]`, `[TestResult]` 접두사를 검색해 추가한 로그 블록을 제거할 수 있다.
- 기존 `[TestUI]` 로그의 `selected`와 `view`를 같은 `sip`의 `[TestResult]`와 대조해 실제 낙찰/유찰 View 선택을 확인한다.

### 빌드 결과

- 명령: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 결과: 성공, 오류 0개, 기존 경고 100개.

### 테스트 결과

- 정적 확인: `git diff --check` 통과.
- 현장 일괄경매 메시지 및 실제 화면 동작 확인: 미수행.
- 수동 확인 항목: 송아지·비육우·번식우 각 1건 이상에 대해 `[TestBatchItem] phase=start` → `[TestResult] status=22 또는 23` → `[TestUI] selected=Sold 또는 UnSold` 순서와 `sip`, `type`, `panel` 일치 여부를 확인한다.

### Git

- 변경 파일: `Services/ServerGetData.cs`, `Services/DisplaySelect.cs`, `WORKLOG.md`.
- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `test: log batch auction state and result flow`


## 2026-09-15 - 테스트용 경매 상태 및 UI 선택 로그 추가

### 작업 날짜

- 2026-09-15

### 작업 목적

- 현장 테스트 로그만으로 출품번호별 유찰, 재경매 진행, 낙찰/유찰 전환과 실제 선택된 화면을 판정할 수 있도록 테스트 전용 로그를 추가했다.

### 원인

- 기존 로그에는 일부 경매 종료 및 재경매 초기화 정보만 있고, 상태 전환과 패널별 최종 View 선택 결과가 공통 식별자로 연결되어 있지 않았다.

### 수정 파일

- `Services/ServerGetData.cs`: 단일경매 `AS8002`/`AS8003`/`AS8004` 및 종료 처리에서 이전/이후 상태, 진행 플래그, 가격, 재경매 여부를 `[TestAuctionState]`로 기록한다.
- `Services/DisplaySelect.cs`: 패널별 상태 코드, 선택 모드, 실제 View 클래스, 페이지, Blink 기대 상태를 `[TestUI]`로 기록한다.
- `WORKLOG.md`: 작업 및 검증 결과를 기록한다.

### 영향 범위

- 경매 상태 판정, View 선택, 통신, 페이지 타이머 및 XAML Storyboard 동작은 변경하지 않았다.
- 테스트 중 로그량만 증가한다. 테스트 종료 후 `[TestAuctionState]`, `[TestUI]` 접두사를 검색해 추가한 로그 블록을 제거할 수 있다.
- `blink-expected`는 Running View 선택에 따른 기대 상태이며, 실제 Storyboard 프레임 동작을 측정한 값은 아니다.

### 빌드 결과

- 명령: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 결과: 성공, 오류 0개, 기존 경고 100개.

### 테스트 결과

- 정적 확인: `git diff --check` 통과.
- 현장 경매 메시지 및 실제 화면 동작 확인: 미수행.
- 수동 확인 항목: 동일 출품번호의 진행 → 유찰 → 재경매 진행 → 낙찰/유찰 순서에서 `[TestAuctionState]`와 `[TestUI]`의 `sip`, `status`, `selected`, `view`, `blink-expected`가 시간순으로 일치하는지 확인한다.

### Git

- 변경 파일: `Services/ServerGetData.cs`, `Services/DisplaySelect.cs`, `WORKLOG.md`.
- Commit hash: 미생성
- Push 여부: 미수행
- 제안 commit 메시지: `test: add auction state UI verification logs`


## 2026-09-15 GAMSTest 빌드 파일 버전 자동 변경

### 작업 일자

- 2026-09-15

### 작업 목적

- 동일한 파일 버전으로 인한 GAMSTest 배포 파일 식별 혼선을 줄이기 위해, 빌드 시 파일 버전을 자동 변경한다.

### 원인

- `GAMSTest/GAMSTest.csproj`에 `FileVersion` 설정이 없어 .NET SDK 기본 파일 버전 `1.0.0.0`이 적용됐다.

### 수정 파일

- `GAMSTest/GAMSTest.csproj`: 2000-01-01 이후 경과 일수와 하루 중 2초 단위 시간을 사용해 `FileVersion`과 `InformationalVersion`을 빌드 시 자동 설정한다.
- `WORKLOG.md`: 작업 및 검증 결과 기록.

### 영향 범위

- GAMSTest 빌드 출력의 EXE/DLL 파일 속성 버전만 변경된다.
- Windows 버전 구성요소 제한을 지키기 위해 버전은 `1.0.<2000-01-01 이후 경과 일수>.<하루 중 2초 단위 시간>` 형식이다.
- 2초 이내에 연속 빌드하면 같은 파일 버전이 될 수 있다.

### 빌드 결과

- 명령: `dotnet build GAMSTest\GAMSTest.csproj --configuration Debug --no-restore`
- 결과: 성공, 오류 0개, 경고 1개.
- 기존 경고: `GAMSTest/ViewModels/ControlWindowViewModel.cs(66,95)` CS0067 `ControlWindowViewModel.ActionCommand.CanExecuteChanged` 이벤트 미사용.

### 테스트 결과

- 파일 속성 확인: `GAMSTest/bin/Debug/net9.0-windows/GAMSTest.exe`의 파일 버전 `1.0.9754.16186` 확인.
- 실제 화면 동작 확인: 미수행.

### Git

- 변경 파일: `GAMSTest/GAMSTest.csproj`, `WORKLOG.md`.
- Commit hash: 미생성.
- Push 여부: 미수행.
- 제안 commit 메시지: `build: GAMSTest 파일 버전 자동 변경`


## 2026-09-15 - Jeongeup auction sold amount alignment

### 작업 내용

- `Views/Size128_128/QQuriSold.xaml`에서 최저가와 낙찰가 금액 바인딩의 상단 여백을 4px로 맞추어 라벨과 세로 위치를 정렬했다.

### 영향 범위

- 정읍을 포함해 공통 `QQuriSold.xaml`을 사용하는 낙찰 화면의 두 금액 줄만 영향받는다.

### 빌드 결과

- 명령: `dotnet build .\\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 결과: 성공, 오류 0개, 기존 경고 100개

### 테스트 결과

- 정적 확인: 두 금액 바인딩의 상단 여백이 라벨의 4px과 일치함을 확인.
- 실제 전광판 수동 확인: 미수행

### Git

- 제안 commit 메시지: `fix: 정읍 낙찰 금액 줄 정렬`
- Commit hash: 미생성
- Push 여부: 미수행


## 2026-09-14 — 축산물 이력 API 로컬 인증키 설정

- 목적: 사용자가 제공한 일반 인증키를 이후 연동에 사용할 로컬 설정에 추가.
- 변경: `Config/animalTrace.local.json`에 URL 인코딩된 인증키 저장, `.gitignore`에 해당 파일 제외 규칙 추가, OpenAPI Markdown 가이드에 파일 경로와 인코딩 주의사항·현재 구현 범위 기록. WORKLOG에는 키 값을 기록하지 않음.
- 영향 범위: 로컬 설정 보관만 완료. 앱 설정 로더·API 호출·배포 전달은 아직 구현하지 않았으며, 기존 통신 동작 변경 없음. 요청한 인증키 추가 범위에 해당.
- 검증: JSON 파싱 및 입력값 일치 확인, git check-ignore로 Git 제외 확인, .gitignore diff --check 통과.
- 빌드·서버 인증 테스트: 미수행. 실행 코드 변경 없음. 실제 키 유효성은 미확인.
- Git: 기존 변경 보존, commit hash 미생성, push 미수행. 제안 메시지: `docs: document local AnimalTrace credential configuration`.
- 추가 확인: 후속 연동에서 설정 로드와 인증 요청 검증 필요.


## 2026-09-14 단일 경매 재경매 화면 유찰 복귀 및 점멸 중단 수정

### 작업 날짜

- 2026-09-14

### 작업 목적

- 재경매 시작 후 과거 유찰 API 스냅샷이 진행 화면을 덮어쓰는 문제와 단일 경매의 정상 카운트다운 종료가 일괄 경매 종료 처리로 전달되는 문제를 수정한다.

### 원인

- 재경매 시작 시 진행 중 출품번호의 API 동기화를 한 번 허용하여, 직전 유찰 상태가 `AuctionResultStatus = "11"`, `IsRunning = true` 상태를 덮어썼다.
- 단일 경매 `SD|...|F|0`도 일괄 경매 완료 처리로 전달됐다.

### 수정 파일

- `Services/ServerGetData.cs`: 진행 중 출품번호의 API 동기화를 제외하고, `8002`/`8003`도 재경매 진행 상태로 즉시 화면에 전달하며, 단일 경매 ESC 취소 신호를 일괄 경매 완료 처리와 분리했다.
- `Services/NettyAsyncMsgProcess.cs`: 단일 경매 `8002`/`8003`을 진행 상태로 전달하고, `SD F -1`만 단일 경매 취소 처리로 전달하도록 변경했다.
- `WORKLOG.md`: 작업 및 검증 결과 기록.

### 영향 범위

- 단일 경매의 재경매 시작, 진행 화면 유지, 테두리 점멸, 정상 카운트다운 종료 및 ESC 취소 처리.
- XAML 및 WPF 점멸 Storyboard는 변경하지 않는다.

### 빌드 결과

- 명령: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 결과: 성공, 오류 0개, 기존 경고 100개

### 테스트 결과

- 정적 확인: 진행 중 SIP의 API 동기화 제외, `8002`/`8003`/`8004` 진행 상태 갱신, 단일 경매 `SD F 0` 미전달 및 `SD F -1` 취소 분기를 확인.
- 실제 경매 연동 및 3대 출하 콘트롤 PC 표시 확인: 미수행

### Git

- 제안 commit 메시지: `fix: 단일 경매 재경매 진행 상태 보호`
- Commit hash: 미생성
- Push 여부: 미수행


## 2026-09-13 GAMSTest STOP 외부 명령 종료 처리

### 작업 날짜

- 2026-09-13

### 작업명

- Named Pipe STOP 수신 시 기존 창 종료 경로 호출

### 작업 목적

- 외부 STOP 명령으로 GAMSTest를 종료할 수 있도록 한다.

### 문제 현상

- 기존 수신 콜백이 모든 명령을 표출 함수로 전달하여 STOP을 무시했다.

### 원인

- `DisplayTestController.HandleExternalCommand()`에는 NUMBER/RED/GREEN/BLUE 분기만 있으며 창 종료 분기는 없다.

### 수정 파일

- `GAMSTest/Views/ControlWindow.xaml.cs`: 수신 콜백에 STOP 분기와 비동기 UI 호출 적용
- `WORKLOG.md`: 구현 및 검증 결과 기록
- `CODEX_HANDOFF.md`: STOP 처리와 후속 확인 항목 인계

### 수정 내용

- `Dispatcher.BeginInvoke`로 UI 처리를 예약하고 `_isClosing`이면 반환한다.
- 대소문자 구분 없이 STOP을 받으면 `Close()`를 호출하고, 나머지 명령은 기존 표출 함수에 전달한다.
- 기존 `OnClosed()`의 서버 취소, 페이지 타이머 중지·번호 표시, 표출창 닫기 및 애플리케이션 종료 경로를 재사용한다.

### 영향 범위

- GAMSTest의 외부 명령 수신 및 종료 경로에 한정하며, C#/WPF/MVVM 구조를 유지한다.
- 기존 NUMBER/RED/GREEN/BLUE 처리와 단독 실행 시 번호 자동 시작을 유지한다.
- 출하 AMS 실행 함수와 운영 프로젝트는 변경하지 않는다. 빌드 대상은 GAMSTest 프로젝트 하나다.
- 기존 종료 시 번호 재표시로 종료 직전에 번호 화면이 잠깐 보일 수 있다.

### 빌드 결과

- 대상: `GAMSTest/GAMSTest.csproj`, Debug
- 명령: `dotnet build GAMSTest\GAMSTest.csproj --configuration Debug --no-restore`
- 결과: 성공, 오류 0개, 경고 1개
- 경고: `ControlWindowViewModel.cs(66,95)` CS0067, `ActionCommand.CanExecuteChanged` 이벤트 미사용
- 이전 사용자 확인 Release/win-x64 빌드·게시 성공 및 자체 동작 정상 기록은 유지하며, 해당 Release 경고 개수는 미확인이다.

### 테스트 결과

- 정적 확인: STOP 분기, 종료 중 명령 무시, 기존 명령 전달 및 기존 종료 경로 재사용 확인
- 사용자 확인: STOP 수신 시 제어창·표출창·GAMSTest 프로세스 종료 정상
- 사용자 확인: 종료 후 출하 AMS 실행 상태 유지
- 사용자 확인: GAMSTest 재실행 시 번호 자동 표시 정상
- 사용자 확인: 재실행 후 Named Pipe 재연결 및 NUMBER/RED/GREEN/BLUE 전환 정상

### Git

- 제안 commit 메시지: `fix: GAMSTest STOP 명령 종료 처리`
- Commit hash: 미생성
- Push 여부: 미수행

### 추가 확인사항

- 이번 STOP 종료 및 Named Pipe 외부 연동 확인 항목은 사용자 확인으로 완료했다.


## 2026-09-13 GAMSTest Release/win-x64 빌드·게시 및 자체 동작 확인

### 작업 내용
- 사용자가 확인한 `GAMSTest` Release/win-x64 빌드 및 게시 성공 결과 반영
- 사용자가 수행한 GAMSTest 자체 동작 테스트 정상 결과 반영

### 검증
- Release/win-x64 빌드: 성공(사용자 확인)
- Release/win-x64 게시: 성공(사용자 확인)
- 자체 동작 테스트: 정상(사용자 확인)
- 경고 개수: 미확인
- Named Pipe 외부 연동 테스트: 정상(사용자 확인, STOP 및 NUMBER/RED/GREEN/BLUE)

### Git
- 소스 수정: 없음
- Commit: 미수행
- Push: 미수행


## 2026-09-13 GAMSTest Named Pipe 외부 테스트 명령 수신 추가

### 작업 내용
- `GAMSTest/Services/NamedPipeCommandServer.cs`에 `GAMSTest.DisplayCommand` Named Pipe 수신 서버 추가
- `GAMSTest/Views/ControlWindow.xaml.cs`에서 서버를 시작하고 UI Dispatcher를 통해 명령 처리
- `GAMSTest/Services/DisplayTestController.cs`에 `NUMBER`, `RED`, `GREEN`, `BLUE` 외부 명령 처리 추가
- 인자 없이 실행할 때 번호 테스트가 자동 시작되는 기존 동작 유지
- `전광판제어` 프로젝트와 기존 운영 UDP 코드는 수정하지 않음

### 검증
- 명령: `dotnet build GAMSTest\\GAMSTest.csproj --configuration Debug --no-restore`
- 결과: 미완료. 저장소 내 `GAMSTest\\bin`·`obj` 디렉터리 생성 권한 제한 및 `project.assets.json` 부재로 빌드 실패
- 소스 diff 및 변경 파일 범위 확인 완료

### Git
- Commit: 미생성
- Push: 미수행


## 2026-09-13 버전 1.0.1.23 반영

### 작업 내용
- `CowAuctionSmall.csproj`의 애플리케이션 버전을 `1.0.1.23`으로 변경한 내용 기록

### 검증
- 명령: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 결과: 성공, 컴파일 오류 0개(기존 경고 존재)

### Git
- Commit: 미생성
- Push: 미수행


## 2026-09-13 횡성축협 전광판 성별 표출 월령 기준 수정

### 작업 내용
- `Services/AnimalParseData.cs`의 횡성축협 전용 `HoengseongSex`에서 거세·암 성별을 `month <= 9` 기준으로 표출하도록 수정
- 9개월 이하: 거세는 "거세", 암은 "암"으로 표출
- 9개월 초과: 거세는 "비육", 암은 "암소"로 표출
- 수·프리마틴/프리 및 기타 성별 처리와 타 축협 로직은 유지

### 검증
- 명령: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 결과: 성공, 컴파일 오류 0개(기존 경고 존재)

### Git
- Commit: 미생성
- Push: 미수행


## 2026-09-13 횡성축협 유찰/낙찰 개체번호 좌표 보정

- 유찰 `EntityNumberShort` Margin을 `74`로 조정
- 일반 낙찰 및 분양 낙찰 `EntityNumberShort` Margin을 `72`로 조정
- Git commit/push: 미수행

### Git
- Commit: 미수행
- Push: 미수행


## 2026-09-12 GAMSTest MockDataFactory 가상 데이터 보강

### 작업 내용
- 패널 번호 `% 4` 기반 성별(암/수/거세/암소) 및 축종 순환
- 진행·유전·유찰·낙찰 공통 비고 `테스트 데이터 입니다` 설정
- 최저가, 중량, 개체번호, 출하주, 지역, 생년월일 등 표시 데이터 매핑
- EPD 수치와 등급(A, B, C, D) 및 농협 우량·뿌리농가 플래그 설정
- 낙찰가 및 낙찰자 정보 보강으로 화면 공백 제거

### 검증
- 명령: `dotnet build GAMSTest\GAMSTest.csproj --configuration Debug --no-restore`
- 결과: 성공, 오류 0개, 경고 1개
- 기존 CowAuctionSmall 관련 추적 파일 diff: 변경 없음(0건)


## 2026-09-12 GAMSTest 운영 프로그램 실행 버튼 추가

### 작업 내용
- GAMSTest ControlWindow에 `CowAuctionSmall.exe` 실행 및 창 활성화(Bring to Front) 버튼 추가
- `user32.dll`의 `ShowWindowAsync(SW_RESTORE)`, `SetForegroundWindow` 적용
- 실행 중인 프로세스는 최소화 해제 후 전면 활성화
- 미실행 시 배포 경로와 개발 빌드 경로를 탐색하고 `WorkingDirectory`를 유지하여 실행

### 검증
- 명령: `dotnet build GAMSTest\GAMSTest.csproj --configuration Debug --no-restore`
- 결과: 성공, 오류 0개, 경고 1개
- 기존 CowAuctionSmall 관련 추적 파일 diff: 변경 없음(0건)


## 2026-09-12 GAMSTest 단독 테스트 실행 및 빌드 검증

### 작업 내용
- ControlWindow 버튼을 [번호], [뷰 페이지] 구조로 통합
- [뷰 페이지] 클릭 시 진행, 유전능력, 유찰, 낙찰 화면을 5초 주기로 순환
- 번호 또는 RGB 버튼 클릭 시 순환 타이머 중지
- RGB 실행 시 패널 자식 요소 제거 후 단색 표시
- SetCustomDisplay 기반 128x128 View 생성
- users.XML의 AuctionHouseCode, LowestPriceTitle, BidderName 반영
- Mock gValues를 각 View의 DataContext로 주입
- 기존 운영 DLL 참조로 기존 View와 서비스 재사용
- ProjectReference 대신 DLL 참조 방식으로 구성하여 GAMSTest 중복 포함 문제 방지

### 검증
- 명령: `dotnet build GAMSTest\GAMSTest.csproj --configuration Debug --no-restore`
- 결과: 성공, 오류 0개, 경고 1개
- 경고: `GAMSTest/ViewModels/ControlWindowViewModel.cs(46)` CS0067 `ActionCommand.CanExecuteChanged` 이벤트 미사용
- 기존 CowAuctionSmall 관련 추적 파일 diff: 변경 없음

### Git
- Commit: 미생성
- Push: 미수행



## 2026-09-11 — 나머지 사업장 XML URL 자리 표시자 적용

- 목적·원인: 대체 사업장 설정 7개에 현황·EPD URL 고정 코드가 남아 있어 현재 users.XML과 설정 방식이 달랐다.
- 변경 파일: `Config/1users.XML`, `Config/고창부안_users.XML`, `Config/곡성_users.XML`, `Config/구미칠곡_users.XML`, `Config/목무신_users.XML`, `Config/춘천_users.XML`, `Config/해남_users.XML`.
- 파일별 변경: Address와 AddressEPD의 사업장 숫자 구간 2곳씩 총 14곳을 `{AuctionHouseCode}`로 변경. 비교용 AuctionHouseCode와 모든 나머지 내용, UTF-8 BOM·줄바꿈 유지.
- `WORKLOG.md`: 이번 작업 기록. 승인된 7개 파일 수정 및 검증 범위와 일치. 기존 다른 변경과 삭제 상태인 개발 설정은 보존.
- 검증: 7개 XML 파싱, 파일별 치환 위치 2개, 비교 코드 유지, 두 주소 외 원문 동일, 저장 후 내용 일치 확인. 대상 git diff --check 통과.
- 빌드: `dotnet build .\CowAuctionSmall.csproj --configuration Release --no-restore` 성공, 증분 빌드 경고 0개·오류 0개. 출력 요약 옵션 추가 사용. 전체 소스 경고가 해소됐다는 의미는 아님.
- 출력 검증: Release 출력의 대상 7개 XML이 소스와 SHA-256 동일함을 확인.
- 영향 범위·미수행: 대체 설정의 URL 표현만 변경. Debug 빌드·별도 게시 폴더 갱신 및 각 사업장 계정 실제 접속은 미수행. 현재 사용 중인 users.XML 전환 없음.
- Git: status·대상 diff 확인, 기존 사용자 변경 보존. commit hash 미생성, push 미수행.
- 제안 commit 메시지: `fix: use auction house placeholders in alternate user configs`


## 2026-09-11 — 로그인 대기 중 기존 시작 화면 복원

- 목적·원인: 로그인 뒤에 패널을 생성하도록 변경하면서 로그인 대기 중 검은 메인 창과 안내문만 나타났다. 기존처럼 시작 시 패널과 로고를 표시한다.
- `ViewModels/MainWindowViewModel.cs`: 통신 시작 전에 DisplaySelect와 패널·로고 생성. 새 로그인 대기 문구 제거. 로그인 전 화면용 사업장 코드는 비워 두고, 로그인 후 기존 DisplaySelect에 인증된 UserInfo 적용. 패널 재생성 없음.
- `Services/DisplaySelect.cs`: UI 스레드에서 최초 조회 전 런타임 UserInfo와 캐시된 사업장 코드를 함께 갱신하는 메서드 추가. 기존 사용자 변경 보존.
- `Docs/로그인_사업장코드_적용계획.md`: 시작 화면 구성 순서 보완 내역 추가.
- `WORKLOG.md`: 이번 작업 기록. 요청한 시작 화면 복원 및 필요한 검증 범위와 일치.
- 영향 범위: 로그인 전 로고·패널, 인증 후 사업장별 표시. 인증 전 경매 API 차단과 상태창 코드 비교는 유지. 화면과 타이머·구독을 로그인 후 중복 생성하지 않는다.
- 빌드: `dotnet build .\CowAuctionSmall.csproj --configuration Release --no-restore` 성공, 경고 94개·오류 0개. 출력 요약 옵션 추가 사용.
- 테스트: 임시 WPF 프로젝트에서 가짜 HTTP 로그인 응답을 지연시킨 일치·불일치·실패 3개 시나리오, 각 11개 총 33개 검증 통과. 초기 패널·로고, 대기 중 유지, 로그인 전 조회 없음, 동일 패널·DisplaySelect 유지, 인증 코드 및 비교 안내, 실패 후 로고 유지·조회 차단 확인. 운영 계정·서버는 가상 테스트에 사용하지 않음.
- 실행 확인: 실제 Release 창을 UI Automation으로 검사하여 코드 일치·응찰서버 접속 성공, 기존 하단 상태창 위치, 추가 로그인 대기 문구 없음, 창 정상 응답 확인. 앱은 실행 상태로 유지.
- Git: 대상 diff --check 및 status 확인. 기존 사용자 변경 보존. commit hash 미생성, push 미수행.
- 제안 commit 메시지: `fix: restore startup logos while awaiting authentication`
- 미수행·남은 확인: 실제 경매 진행·낙찰·유찰 및 모든 해상도, Debug 재빌드. 로그인 후 사업장 코드 적용은 가상 응답의 불일치 사례로도 확인함.


## 2026-09-11 — 로그인 사업장 코드 기준 통신·화면 초기화

### 작업 목적·문제 원인

- XML 사업장 코드는 로그인 결과와 비교하는 예상값으로 사용하고, 실제 API·응찰 인증·화면은 로그인 응답의 사업장 코드로 통일한다.
- 기존에는 로그인에서 토큰만 읽고 URL·화면이 XML 코드를 사용했다. DI 생성자에서 통신이 시작되어 화면 준비와 경합할 수 있었다.

### 변경 파일·내용과 영향 범위

- `Docs/로그인_사업장코드_적용계획.md`: URL 자리 표시자와 비교 알림, 초기 로그인 범위 및 검증 결과 명시.
- `Config/users.XML`: 현황·EPD URL 사업장 구간을 `{AuctionHouseCode}`로 변경. 비교용 설정과 기존 사용자 변경 유지.
- `Models/Structures/LoginResult.cs`: 토큰과 문자열 사업장 코드 검증 결과를 함께 전달.
- `Services/AuctionApiAddressResolver.cs`: 로그인 코드 치환, 알려진 기존 숫자 URL 호환, 잘못된 설정 거부.
- `Services/ServerConn.cs`: 로그인 성공 후에만 세션 확정, 인증 전 요청 차단, 현황·차수·응찰 인증에 로그인 코드 적용.
- `Services/ServerGetData.cs`: 중복 초기화 차단, 예상 코드 불일치 1회 안내, 화면 준비 후 조회 시작.
- `ViewModels/MainWindowViewModel.cs`: 로그인 결과로 DisplaySelect 생성, Dispatcher 적용과 MainContainer 변경 알림, 종료 후 초기화 차단.
- `WORKLOG.md`: 작업 결과 기록.
- 승인된 계획 수정 및 구현 범위와 일치. SQLite·패키지·조회 주기 변경 없음. 기존 다른 수정은 보존했다.

### 빌드 결과

- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore`: 성공, 경고 94개·오류 0개.
- `dotnet build .\CowAuctionSmall.csproj --configuration Release --no-restore`: 성공, 경고 94개·오류 0개.
- 빌드의 출력 요약 옵션만 추가 사용. 패키지 변경·복원 없음.

### 테스트 결과

- 격리 임시 프로젝트에서 URL 24개·로그인 응답 27개, 총 51개 통과. 누락·null·공백·숫자 JSON·비ASCII 코드·토큰 오류·주소 오류 포함.
- 빌드된 Release DLL과 가짜 HTTP 처리기를 사용한 통합 검증 8개 통과: 불일치 코드 적용, 요청 코드·토큰 강제 일치, 다음 로그인 실패 시 세션 제거와 후속 QCN·현황·Netty 차단.
- 임시 진단 프로젝트만 SDK 복원, 외부 패키지 다운로드 없음. 실제 계정은 가상 테스트에 사용하지 않음.
- 초기화 순서 코드 검토: 메시지 구독 → 로그인 검증 → Dispatcher 화면 구성 완료 → 조회 시작. 예상 코드가 비어 있으면 비교 생략, 불일치는 1회 안내.
- 실제 Release 실행에서 로그인 확인 16:08:14, 응찰 서버 접속 성공 16:08:15, 차수 조회 성공 및 창 정상 응답 확인.
- 테스트 앱 정상 종료. 종료 시 기존 Netty 해제 로그가 ERROR 수준으로 1건 기록되었으며 실행 중 인증·연결 실패는 없었음.
- 대상 소스·설정 `git diff --check` 통과. XML 파싱과 BOM·줄바꿈, 한글 안내문 확인.

### Git

- git status와 대상 diff 확인. 기존 사용자 변경·이전 .NET 전환 및 포트 변경과 이번 작업을 구분함.
- 신규 로그인 결과·URL 해석기·계획 문서는 미추적 상태. commit hash: 미생성. push: 미수행.
- 제안 메시지: `fix: use authenticated auction house code for requests and display startup`

### 추가 확인·제한

- 실제 다른 사업장 계정의 불일치 알림 화면, 단일·일괄 경매 진행·낙찰·유찰 및 모든 해상도 수동 검증: 미수행.
- 계정 전환·재로그인 UI는 기존 기능에 없으며 이번 범위에 포함하지 않음.
- EPD는 현재 실제 호출부가 없어 주소 해석·검증만 적용. 신규 EPD API 호출 없음.


## 2026-09-11 — 응찰 서버 포트 설정 정정 및 실제 접속 확인

### 작업 날짜·작업명

- 2026-09-11 — 성공한 .NET 9 레퍼런스 기준으로 응찰 서버 포트 정정

### 작업 목적

- .NET 10 실행 시 반복되는 응찰 서버 연결 실패를 해결한다.

### 문제 현상과 원인

- 현재 설정의 응찰 서버 TCP 연결은 8~10초 내 완료되지 않았고, 앱은 연결 완료 응답 대기 시간 초과를 반복했다.
- 성공한 레퍼런스와 서버 IP는 같지만 포트가 달랐다. 레퍼런스 포트는 별도 TCP 검사에서 연결됐다.

### 수정 파일과 내용

- `Config/users.XML`: `Auction/Port`만 성공한 레퍼런스의 값으로 변경. 계정·주소·다른 설정과 기존 사용자 변경 보존.
- `WORKLOG.md`: 이번 변경과 검증 결과 기록.
- 승인된 포트 정정 및 필요한 빌드·실행 검증 범위와 일치한다. 실제 포트·계정·서버 값은 기록하지 않는다.

### 영향 범위

- 현재 users.XML을 사용하는 앱의 응찰 서버 접속 대상 포트.
- Release 빌드에 설정을 자동 복사했다. Debug 출력 및 기존 별도 게시 검증 폴더는 이번 작업에서 재생성하지 않았다.
- 소스 통신 로직과 패키지 버전은 변경하지 않았다.

### 빌드 결과

- 명령: `dotnet build .\CowAuctionSmall.csproj --configuration Release --no-restore '-consoleLoggerParameters:ErrorsOnly;Summary'`
- 결과: 성공, 경고 50개·오류 0개. 증분 빌드이므로 이전 전체 빌드의 경고 수와 직접 비교하지 않는다.

### 테스트 결과

- [x] 레퍼런스의 Auction/Port와 수정 값 일치 확인.
- [x] Release 출력의 users.XML이 소스 설정과 일치함을 확인.
- [x] .NET 10 Release 실행 후 15:43:42 API 토큰 발급 성공.
- [x] 15:43:43 응찰 서버 접속 성공 로그 확인.
- [x] 실행 프로세스의 창 생성과 정상 응답 확인.
- [ ] 실제 경매 진행·낙찰·유찰·재접속 및 장시간 안정성: 미수행.
- 포트 변경만으로 현재 계정과 기존 TLS 코드에서 서버 접속 성공을 확인했다. 모든 경매 기능의 정상 동작을 보장하는 검증은 아니다.

### Git

- 상태와 diff를 확인하며 작업 전에 존재한 설정 변경을 보존했다. 기존 설정 차이를 이번 작업으로 포함하지 않는다.
- 제안 commit 메시지: `fix: 응찰 서버 포트를 정상 접속 설정에 맞춤`
- Commit hash: 미생성.
- Push 여부: 미수행.

### 추가 확인사항

- 현재 Release 프로그램은 실행 중이다.
- Debug 실행이나 게시본 배포 전에는 해당 구성으로 다시 빌드·게시하여 변경 설정을 반영해야 한다.


## 2026-09-11 — .NET 10 대상 전환 및 빌드·게시 검증

### 작업 날짜

- 2026-09-11

### 작업명

- .NET 9 WPF 프로젝트와 게시 프로필을 .NET 10으로 전환

### 작업 목적

- 기존 경매 표시 및 통신 코드를 유지하면서 .NET 10 대상으로 빌드·게시한다.

### 문제 현상과 원인

- 개발 PC에 .NET 10 SDK가 설치되어 있지만 프로젝트와 게시 프로필이 `net9.0-windows`를 지정하고 있었다.
- SDK 설치만으로 대상 프레임워크가 전환되지 않으므로 두 설정의 변경이 필요했다.

### 수정 파일과 내용

- `CowAuctionSmall.csproj`: `TargetFramework`만 `net9.0-windows`에서 `net10.0-windows`로 변경.
- `Properties/PublishProfiles/FolderProfile.pubxml`: `TargetFramework`만 `net10.0-windows`로 변경. win-x64, 런타임 미포함, 단일 파일 게시 설정 유지.
- `WORKLOG.md`: 이번 구현과 검증 결과 추가.

### 승인 범위와 기존 변경 보존

- 승인된 두 설정 파일의 프레임워크 변경, 필요한 복원·검증, 작업 기록 범위에서 수행했다.
- 작업 전에 변경되어 있던 앱 버전 1.0.1.23, 패키지 버전, 설정 파일 포함 항목 및 그 외 사용자 변경을 보존했다.
- EzDotNetty 1.0.21과 DotNetty 0.7.6 의존성이 복원 결과에 유지됨을 확인했다. 패키지 참조 버전 추가 변경은 수행하지 않았다.

### 영향 범위

- Debug·Release 기본 출력 대상이 `net10.0-windows`로 변경된다.
- 런타임 미포함 게시본 실행 PC에는 호환되는 .NET 10 Desktop Runtime이 필요하다.
- 기존 게시 경로 `bin/AMS_Server`는 유지하되 검증 시 별도 출력 폴더로 재정의했다. 운영 게시본 교체는 미수행이다.

### 빌드·복원·게시 결과

- 사용 SDK: `10.0.400`.
- `dotnet restore .\CowAuctionSmall.csproj --verbosity minimal`: 성공.
- `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore --verbosity quiet`: 성공, 경고 100개·오류 0개.
- `dotnet build .\CowAuctionSmall.csproj --configuration Release --no-restore '-consoleLoggerParameters:ErrorsOnly;Summary'`: 성공, 경고 100개·오류 0개.
- `dotnet restore .\CowAuctionSmall.csproj -p:PublishProfile=FolderProfile --verbosity minimal`: 성공.
- `dotnet publish .\CowAuctionSmall.csproj --configuration Release --no-restore -p:PublishProfile=FolderProfile -p:PublishDir=<검증폴더절대경로> '-consoleLoggerParameters:ErrorsOnly;Summary'`: 성공, 경고 100개·오류 0개.
- 검증 폴더: `bin/NET10_Validation_20260911_152447`.
- 경고에는 nullable, 미사용 멤버, 기존 API 사용 및 MVVM Toolkit 관련 항목이 포함된다. 동일한 현재 소스를 .NET 9로 비교 빌드하지 않았으므로 신규 경고 여부는 미확인이다.

### 테스트 결과

- [x] 프로젝트와 게시 프로필이 `net10.0-windows`로 일치함을 확인.
- [x] 복원 자산에 `net10.0-windows`, `net10.0-windows/win-x64` 대상 존재 확인.
- [x] Debug runtimeconfig가 .NET 10 및 WindowsDesktop 런타임을 대상으로 함을 확인.
- [x] EzDotNetty 1.0.21을 유지한 Debug·Release 컴파일 및 단일 파일 게시 성공.
- [x] 검증 출력의 `CowAuctionSmall.exe` 생성 확인.
- [ ] 앱 실행 및 실제 서버 TLS 연결·수신·재접속·종료 확인: 미수행.
- [ ] 단일·일괄 경매 진행·낙찰·유찰과 주요 해상도별 화면 확인: 미수행.
- [ ] 운영 PC에서 게시본 실행 및 장시간 메모리·연결 안정성 확인: 미수행.
- 빌드·게시 성공은 실제 통신 호환성이나 화면 동작 검증 완료를 의미하지 않는다.

### Git

- `git status`와 대상 diff 확인. 기존 사용자 변경과 이번 대상 프레임워크 변경을 구분했다.
- 게시 프로필은 `.gitignore`의 `*.pubxml` 규칙으로 추적되지 않는다. 로컬 수정만 반영되며 강제 추가나 ignore 규칙 변경은 수행하지 않았다.
- 전체 `git diff --check`에는 기존 `Services/AnimalParseData.cs`와 `Views/Size128_128/Running/RunningNoteHost128.xaml`의 공백 경고가 있다. 관련 없는 변경을 정리하지 않았다.
- 제안 commit 메시지: `build: WPF 프로젝트를 .NET 10으로 전환`
- Commit hash: 미생성.
- Push 여부: 미수행.

### 추가 확인사항

- .NET 10 Desktop Runtime이 준비된 운영과 동등한 시험 환경에서 실제 통신·화면 검증이 필요하다.
- 다른 PC에서는 Git 추적 대상이 아닌 게시 프로필의 대상 프레임워크도 별도로 맞춰야 한다.
- 이전 .NET 9 정상 배포본은 보존했고 운영 교체·실행 및 복귀 검증은 미수행이다.


## 2026-09-11 — 상태창 사업장 코드 비교 결과 표시

- 목적·원인: 기존에는 불일치 안내만 있어 설정 코드와 로그인 코드의 차이를 화면에서 확인할 수 없었다.
- 변경 파일: `Services/ServerGetData.cs` — 로그인 성공 후 기존 상태창에 일치 시 `[코드 일치]`, 불일치 시 `[코드 불일치]`와 로그인 시 코드·작성한 코드를 각각 표시한다. 불일치해도 로그인 사업장으로 진행한다. 설정 코드가 없으면 기존처럼 비교를 생략한다.
- 변경 파일: `WORKLOG.md` — 이번 작업 기록. 사용자 요청 범위와 일치한다.
- 영향 범위: 기존 DataStringMessage를 통한 상태창 안내만 변경. 코드 값을 파일 로그에 추가하지 않는다.
- 빌드: `dotnet build .\CowAuctionSmall.csproj --configuration Release --no-restore` 성공, 경고 94개·오류 0개. 출력 요약 옵션 추가 사용.
- 검증: 일치·불일치·설정 없음 분기와 상태창 바인딩 코드 확인, 대상 git diff --check 통과. 사용자 추가 요청으로 Release 앱을 실제 실행하고 Windows UI Automation으로 MsgTextBox 값을 직접 읽어 `[코드 일치]` 및 `[응찰서버 접속 성공]` 표시, 연결 실패 안내 없음, 창 정상 응답을 확인했다. 확인 후 앱은 실행 상태로 유지했다. Debug 재빌드: 미수행.
- Git: status·대상 diff 확인, 이전 변경 보존. commit hash 미생성, push 미수행.
- 제안 commit 메시지: `fix: show auction house code comparison in status panel`
- 추가 확인: 실제 불일치 계정으로 상태창의 두 코드 표시 수동 확인.

### 작업 날짜

- 2026-09-03

### 작업명

- 장성 비고 첫 키워드 뱃지 분리 및 비장성 친자 뱃지 표시 조건 제한

### 작업 목적

- 장성축협 데이터의 비고에 `친자`, `으뜸`, `혈통`이 없을 때 하단에 빈 청록색 테두리가 표시되지 않도록 한다.
- 키워드가 둘 이상이면 비고에서 가장 먼저 등장한 항목만 뱃지로 분리하고 나머지는 비고에 유지한다.
- 장성축협 이외 사업장은 친자검사 결과가 `친자일치`일 때만 노란색 `친자` 뱃지를 표시한다.

### 문제 현상

- 장성 128×128 낙찰 화면에서 글자 없이 청록색 테두리만 있는 친자 뱃지가 표시될 수 있었다.
- 장성 비고에 키워드가 둘 이상이면 코드에 작성된 검사 순서로 선택되고 선택된 키워드가 모두 제거됐다.
- 비장성 사업장은 친자검사 결과와 관계없이 `FrontNoteWord`에 `친자`가 설정되고 있었다.

### 원인

- 수신 데이터의 친자검사 결과로 `PaternityMatch`가 활성화된 뒤, 장성 비고에 표시할 키워드가 없어도 값이 유지됐다.
- 장성 키워드를 `if / else if` 순서로 검사하고 `Replace`로 제거하여 비고상의 등장 순서와 중복 횟수를 반영하지 못했다.
- 비장성 분기에 `PaternityMatch == "친자일치"` 조건이 없었다.

### 수정 파일

- `Services/AnimalParseData.cs` — 장성 첫 키워드 선택·단일 제거와 비장성 친자일치 조건 적용
- `WORKLOG.md` — 승인된 작업 내역 기록

### 수정 내용

- 장성 비고에서 `친자`, `으뜸`, `혈통`의 위치를 찾아 가장 앞에 등장한 키워드를 선택한다.
- 선택 결과를 `switch`로 구분하여 `친자`는 Lime, `으뜸`은 Aqua, `혈통`은 Yellow로 설정한다.
- 선택된 키워드는 `Remove`로 첫 번째 한 번만 제거하여 뒤에 나온 같은 키워드나 다른 키워드를 비고에 유지한다.
- 세 키워드가 모두 없으면 `FrontNoteWord`와 `PaternityMatch`를 빈 문자열로 설정하고 비고는 변경하지 않는다.
- 장성 이외 사업장은 `PaternityMatch`가 `친자일치`인 경우에만 `FrontNoteWord`를 `친자`, 색상을 Yellow로 설정하고 그 외에는 뱃지 글자를 비운다.
- 비장성 사업장의 원래 `PaternityMatch` 값은 변경하지 않는다.

### 영향 범위

- 사업장 코드 `8808990817675`의 장성 비고 키워드 처리와 그 외 사업장의 친자 뱃지 설정에 적용
- 낙찰자 이름 흐름 표시와 XAML 레이아웃은 변경하지 않음

### 빌드 결과

- 대상: `CowAuctionSmall.csproj` Debug 구성
- 명령 또는 방법: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 결과: 성공
- 경고·오류: 경고 100개, 오류 0개. 이번 분기 변경으로 발생한 컴파일 오류는 없다.

### 테스트 결과

- [x] `으뜸, 친자`에서 `으뜸`이 뱃지로 선택되고 `친자`가 비고에 남는지 로직 확인
- [x] `친자, 으뜸`과 `혈통, 친자, 으뜸`에서 비고상 첫 키워드가 선택되는지 로직 확인
- [x] `친자, 친자`에서 첫 번째 `친자`만 제거되고 두 번째 `친자`가 비고에 남는지 로직 확인
- [x] 키워드 없음에서 뱃지가 숨겨지고 비고가 유지되는지 로직 확인
- [x] 비장성의 `친자일치`는 `친자`/Yellow, `-`·불일치·빈 값은 뱃지 글자가 비워지는지 로직 확인
- [ ] 실제 장성 데이터로 키워드 없음·친자·으뜸·혈통 표시 확인
- [ ] 실제 128×128 전광판에서 빈 테두리 제거 확인
- 결과 및 미수행 항목: Debug 빌드와 정적 확인은 성공했다. 실제 경매 데이터와 전광판을 이용한 시각 확인은 미수행했다.

### Git

- 제안 commit 메시지: `fix: 비고 첫 키워드와 친자 뱃지 표시 조건 정리`
- Commit hash: 미생성
- Push 여부: 미수행

### 추가 확인사항

- 선택된 키워드가 비고 중간에 있으면 키워드만 제거하므로 주변 구분자 형식은 원본 데이터에 따라 남을 수 있다.


## 2026-09-08 유찰 후 동일 출품번호 재경매 전광판 갱신 수정

- 수정 파일: `Services/ServerGetData.cs`, `WORKLOG.md`
- 재경매 시 이전 낙찰·응찰 정보를 초기화하고 진행 상태로 전환
- 변경된 최저가를 반영하고 `DataChangedMessage`로 진행 화면 갱신
- 정상 재경매 동기화 1회에 한해 실행 중 개체 차단 예외 적용
- `dotnet build --no-restore`: 미수행
- 실제 통신·전광판 수동 검증: 미수행
- Git commit hash: 미생성 / Git push: 미수행
- Push 여부: 미수행

### 추가 확인사항

- 장성축협 코드 `8808990657103`의 페이지 순환 분기는 보류 지시에 따라 변경하지 않았다.


## 2026-09-04 — 해남진도 통신 안정화 및 일괄/단일 경매 표출 보완

### 작업 날짜

- 2026-09-04

### 작업명

- 해남진도축협(`8808990656106`) 통신 안정화와 일괄·단일 경매 표출 상태 보완

### 작업 목적

- 중복 접속 시 재연결 경쟁을 차단하고 30초 쿨다운을 보장한다.
- 일괄 경매의 시작·마감 신호가 누락돼 진행 화면이 남는 현상을 방지한다.
- 수동·SZ 새로고침과 낙찰 결과 보정 후 화면 데이터가 이전 상태로 돌아가는 현상을 막는다.

### 문제 현상

- 일괄 경매 마감 SD `F` 신호가 ServerGetData까지 전달되지 않았다.
- 일괄 AS 8004/8006 처리 일부가 목록 null 여부에 의존했고, 화면의 진행 상태가 남을 수 있었다.
- 단일 낙·유찰 보정 후 최신 스냅샷이 이전 값이면 다음 폴링에서 화면이 되돌아갈 수 있었다.

### 원인

- 일괄 경매 SD 분기가 비어 있었다.
- 네티 재연결 진입이 원자적으로 직렬화되지 않았고, 결과 재조회 경로가 직접 API 호출과 표준 처리 루틴으로 나뉘어 있었다.

### 수정 파일

- `Services/NettyAsyncMsgProcess.cs` — 일괄 SD `F` 신호 전달
- `Services/ServerGetData.cs` — 네티 단일 진입, 일괄 종료 정리, 강제 QCN/표준 재조회, 최신 스냅샷 동기화
- `Services/DisplaySelect.cs` — 일괄 경매 중 페이지 회전 정지와 진행 페이지 바인딩 해제
- `WORKLOG.md` — 승인된 작업 내역 기록

### 수정 내용

- `SemaphoreSlim`으로 네티 재연결 시도를 직렬화하고 중복 접속 쿨다운 확인도 같은 진입 구간에서 수행한다.
- AS 8004는 목록 유무와 무관하게 일괄 진행 상태를 설정한다.
- AS 8006 및 SD `F`에서 진행 개체의 `IsRunning`을 해제하고 300ms 후 표준 API 재조회를 실행한다.
- 대기 상태의 AS 8001 refresh와 SZ는 강제 QCN 확인 후 표준 `ProcessMessageAsync()` 경로로 처리한다.
- 낙·유찰 보정 데이터를 최신 스냅샷에도 반영한다.
- 해남진도 전용 1페이지 매핑은 변경하지 않았고, 장성축협(`8808990657103`) 분기는 수정하지 않았다.

### 영향 범위

- 해남진도 현장 통신·표출 안정화 공통 경로에 적용된다.
- 장성축협 페이지 순환 및 해남진도 전용 XAML 매핑은 변경하지 않았다.

### 빌드 결과

- 대상: `CowAuctionSmall.csproj` Debug 구성
- 명령 또는 방법: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 결과: 성공
- 경고·오류: 경고 100개, 오류 0개. 기존 널 허용·이벤트 대리자·사용되지 않는 필드 경고가 있으며 이번 변경의 컴파일 오류는 없다.

### 테스트 결과

- [x] 일괄 SD `F`가 `DataToServerGetAF_SD`로 전달되는지 코드 흐름 확인
- [x] AS 8004/8006 및 SD `F`의 일괄 상태 해제·300ms 후 재조회 흐름 확인
- [x] AF 낙·유찰 보정 시 두 데이터 스냅샷 동기화 확인
- [x] 일괄 경매 중 페이지 회전 정지 조건 확인
- [x] Debug 빌드 성공 확인
- [ ] 실제 해남진도 서버에서 2002 중복 접속 후 30초 동안 재시도하지 않는지 확인
- [ ] 실제 128×128 전광판에서 AS 8006·SD `F` 후 낙찰/유찰 화면 전환 확인
- 결과 및 미수행 항목: 정적 흐름과 Debug 빌드는 확인했으며, 운영 통신 및 실물 전광판 검증은 미수행했다.

### Git

- 제안 commit 메시지: `fix: 해남진도 경매 통신 및 표출 상태 안정화`
- Commit hash: 미생성


## 2026-09-03 — 장성 비고 첫 키워드 뱃지 및 비장성 친자 표시 조건 처리

### 작업 날짜

- 2026-09-03

### 작업명

- 장성 비고 첫 키워드 뱃지 분리 및 비장성 친자 뱃지 표시 조건 제한

### 작업 목적

- 장성축협 데이터의 비고에 `친자`, `으뜸`, `혈통`이 없을 때 하단에 빈 청록색 테두리가 표시되지 않도록 한다.
- 키워드가 둘 이상이면 비고에서 가장 먼저 등장한 항목만 뱃지로 분리하고 나머지는 비고에 유지한다.
- 장성축협 이외 사업장은 친자검사 결과가 `친자일치`일 때만 노란색 `친자` 뱃지를 표시한다.

### 문제 현상

- 장성 128×128 낙찰 화면에서 글자 없이 청록색 테두리만 있는 친자 뱃지가 표시될 수 있었다.
- 장성 비고에 키워드가 둘 이상이면 코드에 작성된 검사 순서로 선택되고 선택된 키워드가 모두 제거됐다.
- 비장성 사업장은 친자검사 결과와 관계없이 `FrontNoteWord`에 `친자`가 설정되고 있었다.

### 원인

- 수신 데이터의 친자검사 결과로 `PaternityMatch`가 활성화된 뒤, 장성 비고에 표시할 키워드가 없어도 값이 유지됐다.
- 장성 키워드를 `if / else if` 순서로 검사하고 `Replace`로 제거하여 비고상의 등장 순서와 중복 횟수를 반영하지 못했다.
- 비장성 분기에 `PaternityMatch == "친자일치"` 조건이 없었다.

### 수정 파일

- `Services/AnimalParseData.cs` — 장성 첫 키워드 선택·단일 제거와 비장성 친자일치 조건 적용
- `WORKLOG.md` — 승인된 작업 내역 기록

### 수정 내용

- 장성 비고에서 `친자`, `으뜸`, `혈통`의 위치를 찾아 가장 앞에 등장한 키워드를 선택한다.
- 선택 결과를 `switch`로 구분하여 `친자`는 Lime, `으뜸`은 Aqua, `혈통`은 Yellow로 설정한다.
- 선택된 키워드는 `Remove`로 첫 번째 한 번만 제거하여 뒤에 나온 같은 키워드나 다른 키워드를 비고에 유지한다.
- 세 키워드가 모두 없으면 `FrontNoteWord`와 `PaternityMatch`를 빈 문자열로 설정하고 비고는 변경하지 않는다.
- 장성 이외 사업장은 `PaternityMatch`가 `친자일치`인 경우에만 `FrontNoteWord`를 `친자`, 색상을 Yellow로 설정하고 그 외에는 뱃지 글자를 비운다.
- 비장성 사업장의 원래 `PaternityMatch` 값은 변경하지 않는다.

### 영향 범위

- 사업장 코드 `8808990817675`의 장성 비고 키워드 처리와 그 외 사업장의 친자 뱃지 설정에 적용
- 낙찰자 이름 흐름 표시와 XAML 레이아웃은 변경하지 않음

### 빌드 결과

- 대상: `CowAuctionSmall.csproj` Debug 구성
- 명령 또는 방법: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 결과: 성공
- 경고·오류: 경고 100개, 오류 0개. 이번 분기 변경으로 발생한 컴파일 오류는 없다.

### 테스트 결과

- [x] `으뜸, 친자`에서 `으뜸`이 뱃지로 선택되고 `친자`가 비고에 남는지 로직 확인
- [x] `친자, 으뜸`과 `혈통, 친자, 으뜸`에서 비고상 첫 키워드가 선택되는지 로직 확인
- [x] `친자, 친자`에서 첫 번째 `친자`만 제거되고 두 번째 `친자`가 비고에 남는지 로직 확인
- [x] 키워드 없음에서 뱃지가 숨겨지고 비고가 유지되는지 로직 확인
- [x] 비장성의 `친자일치`는 `친자`/Yellow, `-`·불일치·빈 값은 뱃지 글자가 비워지는지 로직 확인
- [ ] 실제 장성 데이터로 키워드 없음·친자·으뜸·혈통 표시 확인
- [ ] 실제 128×128 전광판에서 빈 테두리 제거 확인
- 결과 및 미수행 항목: Debug 빌드와 정적 확인은 성공했다. 실제 경매 데이터와 전광판을 이용한 시각 확인은 미수행했다.

### Git

- 제안 commit 메시지: `fix: 비고 첫 키워드와 친자 뱃지 표시 조건 정리`
- Commit hash: 미생성
- Push 여부: 미수행

### 추가 확인사항

- 선택된 키워드가 비고 중간에 있으면 키워드만 제거하므로 주변 구분자 형식은 원본 데이터에 따라 남을 수 있다.


## 2026-09-03 — 128×128 Y/N/B 분기 낙찰 화면 이름 흐름 표시 적용

### 작업 날짜

- 2026-09-03

### 작업명

- 장성 및 선별한 6개 128×128 낙찰 화면의 긴 낙찰자 이름 흐름 표시

### 작업 목적

- `BidderName=Y/B`인 대상 낙찰 화면 7개에서 낙찰자 이름이 5글자를 초과하면 한 줄로 흘러가도록 표시한다.
- 흐름 표시와 애니메이션 생명 주기를 공통 `BidderNameFlow` 컨트롤로 분리한다.

### 문제 현상

- 대상 낙찰 화면의 이름 영역은 폭과 높이가 고정되어 있어 긴 이름이 여러 줄로 배치되거나 일부가 잘릴 수 있었다.
- 기존 `FlowTextAnimation`은 9글자 이상이면서 표시 폭을 초과할 때만 움직여 6글자부터 흐르게 하는 조건을 적용할 수 없었다.

### 원인

- 각 화면의 Y 이름 TextBlock은 줄바꿈되고 B 이름 TextBlock은 고정 폭에서 잘릴 수 있었다.
- `FlowTextAnimation`의 최소 글자 수와 폭 초과 조건이 고정되어 있었다.
- 일부 장성 경매 데이터는 낙찰자 이름이 파싱 단계에서 3글자로 제한될 수 있었다.

### 수정 파일

- `Views/Common/BidderNameFlow.xaml` — 한 줄 이름과 클리핑 영역을 가진 공통 컨트롤 추가
- `Views/Common/BidderNameFlow.xaml.cs` — 애니메이션 시작·중지 및 설정 속성 추가
- `Views/Size128_128/CustomAuctionSold/JangseongSold.xaml` — 이름 표시 모드에 공통 컨트롤 적용
- `Views/Size128_128/CustomAuctionSold/AnseongSold.xaml` — Y/B 이름 표시 모드에 공통 컨트롤 적용
- `Views/Size128_128/CustomAuctionSold/JecheonDanyangSold.xaml` — Y/B 이름 표시 모드에 공통 컨트롤 적용
- `Views/Size128_128/GoatSold.xaml` — Y/B 이름 표시 모드에 공통 컨트롤 적용
- `Views/Size128_128/QQuriSold.xaml` — Y/B 이름 표시 모드에 공통 컨트롤 적용
- `Views/Size128_128/QQuriSold_Weight.xaml` — Y/B 이름 표시 모드에 공통 컨트롤 적용
- `Views/Size128_128/Standard_non_X_Sold.xaml` — Y/B 이름 표시 모드에 공통 컨트롤 적용
- `Services/FlowTextAnimation.cs` — 최소 글자 수와 길이 기준 강제 흐름 옵션 추가
- `Services/AnimalParseData.cs` — 장성 이름 표시 모드에서 전체 낙찰자 이름 유지
- `WORKLOG.md` — 승인된 작업 내역 기록

### 수정 내용

- `BidderNameFlow`에 `Text`, `ScrollThreshold`, `Speed` 의존성 속성을 추가했다.
- `BidderNameFlow`의 공통 표시 기본값을 노란색과 13pt로 지정했다.
- 컨트롤의 `Loaded`/`Unloaded`에서 애니메이션을 생성·해제하고 지연 초기화 작업도 함께 정리한다.
- 대상 7개 화면의 Y 모드 이름 글자 크기를 길이와 관계없이 13으로 통일하고 기존 글자 크기 전환 트리거를 제거했다.
- 대상 7개 화면의 Y/B 모드에서 5글자 이하는 고정하고 6글자 이상은 오른쪽에서 왼쪽으로 반복 이동하도록 설정했다.
- B 모드는 윗줄 이름에 화면의 기존 B 이름 글자 크기를 적용한 `BidderNameFlow`를 배치하고 아랫줄에 참가번호를 표시한다.
- B 참가번호의 기존 글자 크기와 N 모드의 숫자 표시 방식은 화면별로 그대로 유지했다.
- 대상 7개 화면의 14개 이름 컨트롤에서 기본값과 중복되던 색상, 흐름 임계값, 속도와 Y 글자 크기 지정을 제거했다.
- 염소 화면은 흐름 컨트롤이 이름 영역의 가로 폭을 사용하도록 낙찰자 Grid의 가로 정렬을 Stretch로 변경했다.
- 기존 Note 호출은 기본 최소 길이 8과 폭 초과 조건을 그대로 사용하도록 선택 인자의 기본값을 유지했다.
- 장성 조합의 이름 표시 모드에서는 뿌리농가·유전능력 여부와 관계없이 전체 이름을 화면에 전달한다.

### 영향 범위

- 장성, 안성, 제천단양, 염소, 꾸리, 꾸리 중량형, 일반 비-X형 128×128 낙찰 화면의 `BidderName=Y/B` 이름 표시
- `FlowTextAnimation` 생성자에 선택 인자가 추가되지만 기존 호출 동작은 기본값으로 유지
- 위 7개 이외 낙찰 화면과 `BidderName=N`, 진행·유찰 화면에는 새 컨트롤을 적용하지 않았다.
- 이번 6개 화면 확대 단계에서는 `AnimalParseData.cs`의 이름 전달 조건을 추가로 변경하지 않았다.

### 빌드 결과

- 대상: `CowAuctionSmall.csproj` Debug 구성
- 명령 또는 방법: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 결과: 성공
- 경고·오류: 경고 102개, 오류 0개. 새 `BidderNameFlow` 관련 경고는 출력되지 않았다.

### 테스트 결과

- [x] `BidderNameFlow` XAML 및 code-behind 컴파일
- [x] 기존 `FlowTextAnimation` 호출부 컴파일 호환성
- [x] 5글자 이하 정지 및 6글자 이상 강제 흐름 조건 소스 확인
- [x] 장성 및 선별한 6개 낙찰 화면에서만 `BidderNameFlow`가 참조되는지 검색 확인
- [x] 대상 7개 화면의 Y 이름 13pt, B 이름 위·번호 아래 배치 정적 확인
- [x] 대상 6개 화면의 기존 N 표시 및 B 참가번호 글자 크기 유지 확인
- [x] 14개 `BidderNameFlow` 사용처의 중복 기본값 제거 및 공통 기본값 상속 확인
- [ ] 실제 128×128 전광판에서 대상 7개 화면의 5·6·10글자 이름 표시
- [ ] 이름 흐름과 하단 Note 흐름 동시 재생
- [ ] 화면 반복 진입 및 `N/B` 모드 수동 확인
- 결과 및 미수행 항목: Debug 빌드와 정적 확인은 성공했다. 실제 경매 데이터와 전광판을 이용한 시각·수명 주기 테스트는 미수행했다.

### Git

- 제안 commit 메시지: `feat: 128x128 분기 낙찰 화면 이름 흐름 표시 확대`
- Commit hash: 미생성
- Push 여부: 미수행

### 추가 확인사항

- 실제 전광판에서 대상 화면별 속도 18과 이름 영역의 세로 위치가 적절한지 확인이 필요하다.
- 매우 긴 이름은 한 주기가 길어지므로 운영 데이터 기준으로 속도 조정 여부를 확인해야 한다.
- 장성 외 Y 모드 데이터는 기존 파싱 조건에 따라 이름이 3글자로 제한될 수 있으므로 전체 이름 흐름이 필요한 사업장은 파싱 범위를 별도로 검토해야 한다.


## 2026-09-01 — 횡성축협 성별 표출 로직 버그 수정

### 작업 날짜

- 2026-09-01

### 작업명

- 횡성축협 진행 화면 성별 표출 규칙 수정

### 작업 목적

- 등록 성별 `거세`가 `암소`로 잘못 표출되는 문제를 수정하고, 송아지·번식우의 암소 표출 규칙을 구버전 기준으로 맞춘다.

### 수정 파일

- `Services/AnimalParseData.cs` — 횡성 성별 변환 조건 수정
- `WORKLOG.md` — 작업 내역 기록

### 수정 내용

- 등록 성별 `거세` 또는 비육우는 `비육`으로 표출
- 송아지의 등록 성별 `암`은 `암`으로 유지
- 성우·번식우의 등록 성별 `암`은 `암소`로 표출
- 등록 성별 `수`는 월령 조건과 관계없이 `수`로 유지
- 기타 성별값은 서버 원본 문자열을 유지

### 빌드 결과

- 명령: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 결과: 미수행

### Git

- Commit hash: 미생성
- Push 여부: 미수행


## 2026-08-31 — 횡성축협 분양가 행사 모드 동적 레이아웃 적용

### 작업 날짜

- 2026-08-31

### 작업명

- 횡성축협 진행·낙찰 화면 분양가 행사 모드 조건부 표시 및 일반 경매 복구

### 작업 목적

- `LowestPriceTitle`이 `분양가`일 때만 행사 전용 레이아웃을 적용하고, 일반 경매에서는 기존 레이아웃을 자동 복구한다.

### 수정 파일

- `Views/Size128_128/Running/CustomAuctionRunning1/HoengseongRun.xaml` — 행사 모드에서 출하지역·농가명·임신 영역 숨김
- `Views/Size128_128/CustomAuctionSold/HoengseongSold.xaml` — 행사 모드에서 최저가 줄 숨김 및 후속 줄 위치 조정
- `WORKLOG.md` — 승인된 작업 내역 기록

### 수정 내용

- 진행 화면에 `CowInfo.LowestPriceTitle == "분양가"` DataTrigger를 적용했다.
- 분양가 모드에서 Location, OwnerName, Pregnant를 `Collapsed` 처리하고 일반 모드에서는 `Visible`로 복구한다.
- 낙찰 화면에서 분양가 모드일 때 최저가 줄을 숨기고, 낙찰가·낙찰자·소/염소 개체번호를 각각 22px·40px·74px로 배치한다.
- 일반 모드에서는 기존 22px·40px·58px·90px 레이아웃을 유지한다.

### 영향 범위

- 횡성축협 128×128 진행·낙찰 화면의 분양가 행사 모드 레이아웃
- 내정가·시중가 일반 경매 모드의 기존 표시 자동 복구
- 성별·중량 및 다른 조합·해상도·유찰 화면은 변경하지 않았다.

### 빌드 결과

- 대상: `CowAuctionSmall.csproj` Debug 구성
- 명령 또는 방법: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 결과: 미수행
- 경고·오류 또는 미수행 사유: 작업 기록 작성 후 빌드 예정

### 테스트 결과

- [ ] 진행 화면 분양가 모드
- [ ] 진행 화면 일반 경매 자동 복구
- [ ] 낙찰 화면 분양가 모드
- [ ] 낙찰 화면 일반 경매 자동 복구
- [ ] 소·염소 개체번호 위치
- [ ] Debug 빌드 및 XAML 컴파일
- 결과 및 미수행 항목: 구현 후 검증 예정

### Git

- 제안 commit 메시지: `fix: 횡성축협 분양가 행사 모드 동적 레이아웃 적용`
- Commit hash: 미생성
- Push 여부: 미수행

### 추가 확인사항

- 실제 128×128 전광판에서 행사·일반 모드의 텍스트 겹침과 위치 확인이 필요하다.

CowAuctionSmall C# WPF MVVM 가축 경매 전광판 프로젝트의 변경 이력을 기록한다.


## 2026-08-31 — 횡성축협 낙찰 화면 전용 분기 우선 처리

### 작업 날짜

- 2026-08-31

### 작업명

- 횡성축협 `HoengseongSold` 낙찰 화면 선택 오류 수정

### 작업 목적

- 횡성축협 낙찰 화면에서 `QQuriSold_v3`가 먼저 선택되지 않고 `HoengseongSold`가 표출되도록 한다.

### 문제 현상

- 횡성축협 `nhCode=8808990656885`이고 `bidderCode=B`인 낙찰 건에서 `HoengseongSold.xaml` 대신 `QQuriSold_v3.xaml`이 표출되었다.

### 원인

- `CustomAuctionSold_128()`에서 `bidderCode == "B"` 조건이 조합별 전용 화면 switch보다 먼저 평가되었다.

### 수정 파일

- `Services/SetCustomDisplay.cs` — 조합별 전용 낙찰 화면을 먼저 선택하고 일반 조합에만 `bidderCode=B`를 적용
- `WORKLOG.md` — 승인된 작업 내역 기록

### 수정 내용

- 염소·말 특수 축종 검사를 유지했다.
- `nhCode` 기반 조합별 전용 낙찰 화면 switch를 먼저 평가하도록 변경했다.
- 전용 화면이 없는 일반 조합에서 기본 결과가 `QQuriSold`일 때만 `bidderCode=B`이면 `QQuriSold_v3`를 반환하도록 변경했다.
- 횡성 `8808990656885`는 `bidderCode`와 무관하게 `HoengseongSold`를 반환한다.

### 영향 범위

- 128×128 낙찰 화면 선택 로직
- 횡성 및 기존 조합별 전용 낙찰 화면의 우선순위
- 일반 조합의 `bidderCode=B` 화면 선택
- 진행·유찰 화면 및 XAML 파일은 변경하지 않았다.

### 빌드 결과

- 대상: `CowAuctionSmall.csproj` Debug 구성
- 명령 또는 방법: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 결과: 성공
- 경고·오류 또는 미수행 사유: 오류 0개, 기존 경고 100개

### 테스트 결과

- [x] 특수 축종 검사 유지 여부
- [x] 횡성 `nhCode=8808990656885` 전용 화면 우선 여부
- [x] 일반 조합 `bidderCode=B` 화면 분기
- [x] 기본 `is_QQuri` 분기 유지 여부
- [x] Debug 빌드 및 XAML 컴파일
- 결과 및 미수행 항목: 소스 분기 및 diff 점검 완료. 실제 전광판 실행, 화면 전환·반복 진입, 자동화 테스트는 미수행.

### Git

- 제안 commit 메시지: `fix: 횡성축협 낙찰 화면 전용 분기 우선 처리`
- Commit hash: 미생성
- Push 여부: 미수행

### 추가 확인사항

- 실제 횡성축협 전광판에서 `bidderCode=B` 낙찰 건이 `HoengseongSold`로 표출되는지 확인 필요


## 2026-08-31 — 영주축협 낙찰자 이름·번호 동시 표시

### 작업 날짜

- 2026-08-31

### 작업명

- `BidderName=B` 설정 시 이름과 번호를 함께 표시하는 낙찰 화면 적용

### 작업 목적

- `BidderName`이 `B`일 때 128×128 낙찰 화면에서 낙찰자 이름과 번호를 모두 표시한다.
- 이번 작업 반영 버전을 `1.0.1.21`로 변경한다.

### 문제 현상

- `QQuriSold_v3`는 이름·번호 동시 표시 레이아웃을 갖고 있었지만, `CustomAuctionSold_128()`에서 `BidderName=B` 설정을 화면 선택 조건으로 사용하지 않았다.

### 원인

- `bidderCode` 인자가 낙찰 화면 선택 메서드에 전달되지만 `B` 조건 분기에 사용되지 않았다.
- `B` 파싱 경로에서 `BidderString`에 길이 제한이 적용되어 이름 표시가 잘릴 수 있었다.

### 수정 파일

- `Services/SetCustomDisplay.cs` — `bidderCode=B`일 때 `QQuriSold_v3` 선택
- `Services/AnimalParseData.cs` — `B` 파싱 시 이름과 번호 필드 명시적 할당
- `Config/users.XML` — `<BidderName>B</BidderName>` 적용
- `WORKLOG.md` — 승인된 작업 내역 기록

### 수정 내용

- 염소·말 화면을 제외하고 `bidderCode=B`이면 `QQuriSold_v3`를 반환하도록 추가했다.
- `B` 파싱 시 `gv.BidderString`에 낙찰자 이름, `gv.BidderNum`에 낙찰자 번호를 할당하도록 정리했다.
- `users.XML`의 `BidderName`을 `B`로 변경했다.
- 프로그램 버전을 `1.0.1.21`로 변경했다.

### 영향 범위

- 128×128 낙찰 화면의 낙찰자 표시 및 화면 선택
- 염소·말 낙찰 화면과 `BidderName`이 `Y` 또는 `N`인 기존 흐름은 유지한다.
- 실제 영주축협 운영 설정 및 전광판 표출은 별도 확인이 필요하다.

### 빌드 결과

- 대상: `CowAuctionSmall.csproj` Debug 구성
- 명령 또는 방법: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 결과: 성공
- 경고·오류 또는 미수행 사유: 오류 0개, 기존 경고 100개

### 테스트 결과

- [ ] 영주축협 128×128 낙찰 화면
- [ ] 낙찰자 이름 표시
- [ ] 낙찰자 번호 표시
- [ ] `BidderName=Y/N` 기존 화면 선택 회귀
- [ ] 염소·말 낙찰 화면 회귀
- [ ] 화면 전환 및 반복 진입
- 결과 및 미수행 항목: Restore 제외 Debug 빌드 성공. 실제 전광판 실행 확인과 자동화 테스트는 미수행.

### Git

- 제안 commit 메시지: `feat: 낙찰자 이름과 번호 동시 표시`
- Commit hash: 미생성
- Push 여부: 미수행

### 추가 확인사항

- 현재 `Config/users.XML`의 `AuctionHouseCode`가 영주축협 운영 설정인지 확인 필요
- 실제 전광판에서 이름 길이와 번호 표시의 잘림·겹침 여부 확인 필요


## 2026-08-31 HoengseongSold 낙찰 화면 분양가 제목 표시 수정

### 작업 날짜

- 2026-08-31

### 작업명

- HoengseongSold 128x128 낙찰 화면 2번째 줄 가격 제목 조건부 바인딩

### 작업 목적

- `LowestPriceTitle`이 `분양가`일 때 2번째 줄도 `분양가:`로 표시하고 일반 경매 표시를 유지

### 문제 현상 및 원인

- 2번째 줄 가격 제목이 XAML에 `내정가:`로 하드코딩되어 있었음
- `LowestPriceTitleFull`도 `분양가` 조건에서 `내정가:`를 반환하고 있었음

### 수정 파일 및 내용

- `Models/Structures/gValues.cs`: `분양가` 조건에서 `LowestPriceTitleFull`이 `분양가:`를 반환하도록 수정
- `Views/Size128_128/CustomAuctionSold/HoengseongSold.xaml`: 2번째 줄 TextBlock을 `LowestPriceTitleFull` 바인딩으로 변경
- `WORKLOG.md`: 본 작업 기록 추가

### 영향 범위

- 분양가 모드: 2번째 줄 `분양가:`, 3번째 줄 `분양가:`, 4번째 줄 `당첨자:`
- 일반 경매 모드의 기존 내정가/시중가/최저가 및 낙찰가/낙찰자 표시 유지
- 기존 128x128 좌표·스타일·3·4번째 줄 바인딩은 변경하지 않음

### 빌드 결과

- 명령: `dotnet build .\\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 결과: 성공, 오류 0개, 기존 경고 100개

### 테스트 결과

- [x] Debug 빌드 및 XAML 컴파일
- [x] diff로 바인딩·반환 로직 확인
- [ ] 실제 전광판 분양가/일반 경매 수동 확인 (미수행)

### Git

- 제안 commit 메시지: `fix: HoengseongSold 분양가 가격 제목 표시`
- Commit hash: 미생성
- Push 여부: 미수행

### 추가 확인사항

- 실제 users.XML 값별 전광판 표시를 수동 확인해야 함

## 2026-08-31 횡성축협 행사 전용 분양 낙찰 뷰 분리

- 작업 내용: `LowestPriceTitle` 값에 따라 횡성축협 128x128 낙찰 뷰를 행사형/경매형으로 분리
- 변경 파일: `Services/DisplaySelect.cs`, `Services/SetCustomDisplay.cs`, `Views/Size128_128/CustomAuctionSold/HoengseongSold.xaml`, `Views/Size128_128/CustomAuctionSold/HoengseongBunyangSold.xaml`, `Views/Size128_128/CustomAuctionSold/HoengseongBunyangSold.xaml.cs`
- 주요 변경: `행사용`이면 신규 분양 뷰, 그 외에는 기존 경매 뷰를 선택하도록 라우팅하고, 두 뷰의 128x128 레이아웃을 분리함
- 빌드: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 빌드 결과: 성공, 오류 0개, 기존 경고 100개
- 테스트 결과: 자동 테스트 미수행; diff 및 XAML 컴파일 확인
- Git commit: 미생성
- Git push: 미수행
- 추가 확인: users.XML 값별 실제 전광판 화면 수동 확인 필요

## 2026-08-31 횡성축협 LowestPriceTitle 표시 및 행사 모드 숨김 조건 수정

- 작업 내용: `행사용`/`경매용` 설정값을 횡성 진행 화면 표시 제목으로 변환하고 행사 모드 숨김 조건 수정
- 변경 파일: `Models/Structures/gValues.cs`, `Views/Size128_128/Running/CustomAuctionRunning1/HoengseongRun.xaml`
- 주요 변경: 행사형은 `분양가:`, 경매형 및 기본값은 `내정:`/`내정가:`로 표시하며 기존 `시중가`/`최저가` 특수 제목은 유지함. 행사형에서 Location, OwnerName, Pregnant를 숨김
- 빌드: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 빌드 결과: 성공, 오류 0개, 기존 경고 100개
- 테스트 결과: 자동 테스트 미수행; Debug 빌드 및 XAML 컴파일 확인
- Git commit: 미생성
- Git push: 미수행
- 추가 확인: 행사형에서 성별·중량 유지 및 경매형에서 일반 정보 표시 확인 필요


## 2026-08-28 — LogoBoard 누락 시 로고 기본값 처리

### 작업 날짜

- 2026-08-28

### 작업명

- LogoBoard 설정 누락 및 패널 ID 불일치 시 로고 처리 보완

### 작업 목적

- `board.XML`의 `LogoBoard` 태그가 누락되거나 비어 있어도 프로그램이 기동 실패하지 않도록 한다.

### 문제 현상

- `LogoBoard`가 누락되거나 빈 리스트일 때 `LogoManager.GetLogoFileName(panelId)`의 `[0]` 접근에서 `ArgumentOutOfRangeException`이 발생했다.

### 원인

- `_boardInfo?.LogoBoard?[0]`은 리스트가 null인 경우만 보호하고, 빈 리스트인 경우는 보호하지 않았다.

### 수정 파일

- `Models/LogoManager.cs` — LogoBoard·패널 ID 검증 및 기본 로고 경로 처리
- `WORKLOG.md` — 승인된 작업 내역 기록

### 수정 내용

- `LogoBoard`가 null 또는 빈 리스트이면 `logo.bmp`를 반환한다.
- 일치하는 로고 행이 없으면 `logo.bmp`를 반환한다.
- 빈 패널명은 `Config\\logo.bmp` 기본 경로로 반환한다.
- 기존 GIF 우선 탐색 및 캐시 로직은 유지한다.

### 영향 범위

- 로고 파일명 및 로고 경로 선택 흐름
- 경매 상태, 통신·파싱 및 다른 화면 로직에는 영향이 없어야 한다.

### 빌드 결과

- 대상: `CowAuctionSmall.csproj` Debug 구성
- 명령 또는 방법: `dotnet build .\\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 결과: 성공
- 경고·오류 또는 미수행 사유: 오류 0개, 기존 경고 100개

### 테스트 결과

- [ ] LogoBoard 누락
- [ ] LogoBoard 빈 리스트
- [ ] 유효하지 않은 패널 ID
- [ ] 기본 로고 경로 fallback
- [ ] GIF 우선 탐색 및 캐시 회귀
- 결과 및 미수행 항목: Restore 제외 Debug 빌드 성공. 실제 전광판 실행 및 자동화 테스트는 미수행.

### Git

- 제안 commit 메시지: `fix: LogoBoard 누락 시 기본 로고 처리`
- Commit hash: 미생성
- Push 여부: 미수행

### 추가 확인사항

- 실제 배포 환경에서 `Config\\logo.bmp` 파일 존재 여부 확인 필요


## 2026-08-28 — 횡성 프리마틴 성별 표시 축약

### 작업 날짜

- 2026-08-28

### 작업명

- 횡성축협 진행 화면 `프리마틴` 성별 표시를 `프`로 축약

### 작업 목적

- 128×128 진행 화면에서 `프리마틴` 또는 `프리`가 `프` 한 글자로 표시되도록 한다.

### 문제 현상

- 횡성축협 진행 화면에서 성별이 `프리`로 표시되었다.

### 원인

- 횡성 전용 성별 처리에 `프리마틴`·`프리` 축약 규칙이 없었다.
- `CowTypeToStyleConverter_V3`에 `프` 스타일 매핑이 없었다.

### 수정 파일

- `Services/AnimalParseData.cs` — 횡성 전용 성별 축약 처리
- `Models/Converter/CowTypeToStyleConverter_V3.cs` — `프` 1글자 스타일 추가

### 수정 내용

- 횡성축협 성별 처리 내부에서 `프리마틴` 또는 `프리`를 `프`로 변환한다.
- `프`에 `수`와 동일한 파란색 브러시, 28pt, 1글자용 여백을 적용한다.
- 다른 축협의 성별 데이터에는 축약 규칙을 적용하지 않는다.

### 영향 범위

- 횡성축협 진행 화면의 `프리마틴`·`프리` 표시
- 다른 축협 및 낙찰·유찰 화면에는 영향이 없어야 한다.

### 빌드 결과

- 대상: `CowAuctionSmall.csproj` Debug 구성
- 명령 또는 방법: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 결과: 미수행
- 경고·오류 또는 미수행 사유: 작업 기록 시점에 빌드 예정

### 테스트 결과

- [ ] 경매 진행 화면
- [ ] 낙찰 화면
- [ ] 유찰 화면
- [ ] 화면 전환 및 반복 진입
- [ ] 데이터 바인딩 갱신과 빈 값 처리
- [x] 사업장별 조건 분기
- [ ] 주요 전광판 해상도
- [ ] 통신·파싱 회귀 확인
- 결과 및 미수행 항목: 소스 수정 완료. 빌드 및 실제 전광판 확인은 미수행.

### Git

- 제안 commit 메시지: `fix: 횡성 프리마틴 성별 표시를 프로 축약`
- Commit hash: 미생성
- Push 여부: 미수행

### 추가 확인사항

- Restore 제외 Debug 빌드 결과 확인 필요
- 실제 횡성 전광판에서 `프`의 색상, 크기, 위치 확인 필요


## 2026-08-28 — 횡성 진행 화면 성별 표시 복구

### 작업 날짜

- 2026-08-28

### 작업명

- 성별 텍스트 우측 배치에 따른 표시 누락 수정

### 작업 목적

- 가격 영역과 겹치지 않도록 이동한 성별 텍스트가 화면에서 사라진 문제를 복구한다.

### 문제 현상

- `암소`, `수`, `거세` 등 성별 텍스트가 진행 화면에 표시되지 않았다.

### 원인

- `CowTypeToStyleConverter_V3`가 성별별 `Margin`을 스타일로 적용하고 있어 XAML의 `Canvas.Left`와 합산되면서 텍스트가 124px 캔버스 바깥으로 밀렸다.

### 수정 파일

- `Views/Size128_128/Running/CustomAuctionRunning1/HoengseongRun.xaml` — 성별 텍스트의 로컬 여백·폭·우측 정렬 조정

### 수정 내용

- 성별 TextBlock에 `Margin="0"`을 지정해 Converter의 위치용 여백을 무효화했다.
- `Canvas.Left="78"`, `Width="46"`, `TextAlignment="Right"`로 2글자 성별도 캔버스 안에서 우측 정렬되도록 변경했다.
- 기존 성별별 글꼴 크기와 색상 스타일은 유지했다.

### 영향 범위

- 횡성축협 128×128 진행 화면의 성별 표시 위치
- 실제 전광판에서 모든 성별 문자열의 최종 겹침 여부는 미확인이다.

### 빌드 결과

- 대상: `CowAuctionSmall.csproj` Debug 구성
- 명령 또는 방법: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 결과: 성공
- 경고·오류 또는 미수행 사유: 오류 0개, 기존 경고 100개

### 테스트 결과

- [ ] 경매 진행 화면
- [ ] 낙찰 화면
- [ ] 유찰 화면
- [ ] 화면 전환 및 반복 진입
- [ ] 데이터 바인딩 갱신과 빈 값 처리
- [x] 사업장별 조건 분기
- [ ] 주요 전광판 해상도
- [ ] 통신·파싱 회귀 확인
- 결과 및 미수행 항목: Restore 제외 Debug 빌드 성공. 실제 전광판 실행 확인은 미수행.

### Git

- 제안 commit 메시지: `fix: 횡성 진행 화면 성별 표시 복구`
- Commit hash: 미생성
- Push 여부: 미수행

### 추가 확인사항

- 실제 전광판에서 `암소`, `수`, `거세` 표시와 가격 영역의 겹침 여부 확인 필요



## 2026-08-28 — 횡성축협 분양가·성별 텍스트 겹침 보완

### 작업 날짜

- 2026-08-28

### 작업명

- 128×128 횡성 진행 화면 가격·성별 배치 조정

### 작업 목적

- `분양가: 2,500` 표출 시 우측 성별 텍스트와 겹치는 현상을 줄인다.

### 문제 현상

- 128px 폭에서 가격 금액과 우측 `암소`·`수` 등 성별 텍스트가 겹쳤다.

### 원인

- 성별 TextBlock이 좌측 기준으로 배치되어 가격 영역과 충돌했다.
- 가격 제목 증가에 비해 금액 시작 위치가 넓게 떨어져 있었다.

### 수정 파일

- `Views/Size128_128/Running/CustomAuctionRunning1/HoengseongRun.xaml` — 성별 우측 배치 및 가격 간격 조정

### 수정 내용

- 성별 표시를 Canvas 좌표 92px, 폭 30px, 우측 정렬로 배치했다.
- 가격 금액 시작 좌표를 48px에서 43px로 당겼다.
- `gValues.cs`의 `분양가:` 포맷은 기존 요구사항대로 유지했다.

### 영향 범위

- 횡성축협 128×128 진행 화면의 가격·성별 배치
- 실제 전광판에서 모든 성별 문자열의 겹침 여부는 미확인이다.

### 빌드 결과

- 대상: `CowAuctionSmall.csproj` Debug 구성
- 명령 또는 방법: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 결과: 성공
- 경고·오류 또는 미수행 사유: 오류 0개, 기존 경고 100개

### 테스트 결과

- [ ] 경매 진행 화면
- [ ] 낙찰 화면
- [ ] 유찰 화면
- [ ] 화면 전환 및 반복 진입
- [ ] 데이터 바인딩 갱신과 빈 값 처리
- [x] 사업장별 조건 분기
- [ ] 주요 전광판 해상도
- [ ] 통신·파싱 회귀 확인
- 결과 및 미수행 항목: Restore 제외 Debug 빌드 성공. 실제 전광판 실행 테스트는 미수행.

### Git

- 제안 commit 메시지: `fix: 횡성 진행 화면 가격 성별 겹침 보완`
- Commit hash: 미생성
- Push 여부: 미수행

### 추가 확인사항

- 실제 128px 전광판에서 `분양가: 2,500`, `분양가: 결장`과 성별 표시 확인 필요



## 2026-08-28 — 횡성축협 행사 분양가 표시 보완

### 작업 날짜

- 2026-08-28

### 작업명

- 횡성축협 진행 화면 분양가 표시 및 낙찰·유찰 화면 기존 제목 유지

### 작업 목적

- 횡성축협 연 1회 행사에서 진행 화면의 `내정:` 제목을 `분양가:`로 표시한다.
- 행사 중에도 낙찰·유찰 화면은 기존 `내정가` 표시를 유지한다.
- 행사 종료 후 `Config/users.XML`의 설정값을 되돌릴 수 있도록 한다.

### 문제 현상

- 진행 화면에서 행사 중에도 가격 제목이 `내정:`으로 표시된다.
- `LowestPriceTitle`을 `분양가`로 설정하면 기존 변환 규칙상 `분양:`으로 줄어들 수 있다.
- 낙찰·유찰 화면은 행사 설정에 따라 제목이 변경될 수 있다.

### 원인

- 횡성 진행 화면 가격 제목이 XAML에 `내정:`으로 하드코딩되어 있었다.
- `LowestPriceTitleShort`가 모든 제목의 마지막 `가`를 제거한다.
- 낙찰·유찰 화면이 설정값 제목을 직접 사용하고 있었다.

### 수정 파일

- `Models/Structures/gValues.cs` — `분양가` 진행/낙찰 제목 예외 처리
- `Views/Size128_128/Running/CustomAuctionRunning1/HoengseongRun.xaml` — 설정 기반 진행 제목 바인딩 및 가격 위치 조정
- `Views/Size128_128/CustomAuctionSold/HoengseongSold.xaml` — 행사 중에도 `내정가:` 고정
- `Views/Size128_128/CustomAUctionUnSold/HoengseongUnSold.xaml` — 행사 중에도 `내정가 :` 고정
- `Config/users.XML` — 행사 제목을 `분양가`로 설정

### 수정 내용

- `분양가`일 때 진행 화면용 제목은 `분양가:`를 반환하도록 변경했다.
- `분양가`일 때 낙찰 화면용 제목은 `내정가:`를 반환하도록 예외 처리했다.
- 횡성 진행 화면의 가격 제목을 `LowestPriceTitleShort`에 바인딩했다.
- 제목이 한 글자 늘어나는 경우를 고려해 가격 표시 좌표를 오른쪽으로 조정했다.
- 횡성 낙찰·유찰 화면의 가격 제목은 `내정가` 계열로 고정했다.
- `Config/users.XML`에 `<LowestPriceTitle>분양가</LowestPriceTitle>`를 적용했다.

### 영향 범위

- 횡성축협 128×128 진행 화면의 가격 제목 표시
- 횡성축협 낙찰·유찰 화면의 가격 제목은 기존 표시 유지
- 다른 사업장의 설정 및 화면에는 영향이 없어야 한다.
- 실제 전광판에서 성별 텍스트와의 겹침 여부는 미확인이다.

### 빌드 결과

- 대상: `CowAuctionSmall.csproj` Debug 구성
- 명령 또는 방법: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --no-restore`
- 결과: 미수행
- 경고·오류 또는 미수행 사유: 작업 완료 후 수행 예정

### 테스트 결과

- [ ] 경매 진행 화면
- [ ] 낙찰 화면
- [ ] 유찰 화면
- [ ] 화면 전환 및 반복 진입
- [ ] 데이터 바인딩 갱신과 빈 값 처리
- [x] 사업장별 조건 분기
- [ ] 주요 전광판 해상도
- [ ] 통신·파싱 회귀 확인
- 결과 및 미수행 항목: 소스 수정 및 설정값 반영까지 완료. 빌드와 실제 실행 화면 확인은 미수행.

### Git

- 제안 commit 메시지: `fix: 횡성축협 행사 분양가 표시 보완`
- Commit hash: 미생성
- Push 여부: 미수행

### 추가 확인사항

- Restore 제외 Debug 빌드 결과 확인 필요
- 진행 화면에서 `분양가: 2,500`, `분양가: 결장` 표출 확인 필요
- 성별 텍스트와 가격 제목·금액의 겹침 여부 확인 필요
- 낙찰·유찰 화면에서 `내정가` 표시 유지 확인 필요


## 2026-08-28 — 횡성축협 낙찰 화면 행사 타이틀 동적 표시

- 요청 내용: `LowestPriceTitle`이 `분양가`일 때 횡성축협 낙찰 화면의 `낙찰가:`를 `분양가:`, `낙찰자:`를 `당첨자:`로 표시하고 평소에는 기존 제목을 유지한다.
- 변경 파일:
  - `Models/Structures/gValues.cs`
  - `Views/Size128_128/CustomAuctionSold/HoengseongSold.xaml`
  - `WORKLOG.md`
- 주요 변경:
  - `SuccessfulBidPriceTitle`, `SuccessfulBidderTitle` 프로퍼티를 추가했다.
  - `HoengseongSold.xaml`의 두 하드코딩 제목을 신규 프로퍼티 바인딩으로 교체했다.
  - 기존 `LowestPriceTitleShort`, `LowestPriceTitleFull` 변환 로직과 128x128 배치를 유지했다.
- 검증:
  - 빌드 명령: `dotnet build .\\CowAuctionSmall.csproj --configuration Debug --no-restore`
  - 빌드 결과: 성공, 오류 0개, 경고 100개
  - 수동 화면 확인: 미수행
- Git commit: 미생성
- Git push: 미수행

- 실제 춘천축협 전광판에서 진행·낙찰·유찰 전환 시 뱃지가 표시되지 않는지 확인 필요

---

## 2026-08-20 — 해남진도축협 출하지역 및 등록구분 표시 개선

### 작업 날짜

- 2026-08-20

### 작업명

- 해남진도축협 출하지역 및 등록구분 표시 개선

### 작업 목적

- 해남진도축협 경매 진행 화면에서 출하주의 실제 지역을 해남 또는 진도로 정확하게 표시한다.
- KPN 아래에 개체 등록구분을 표시하고, 미등록 개체는 `미등`이 아닌 `미등록`으로 표시한다.
- 해남진도축협에만 조건을 적용하여 다른 사업장의 기존 동작을 유지한다.

### 문제 현상

- 진도 지역 출하주의 주소가 `전남진도군...` 형식으로 수신될 때 출하지역이 `진도`가 아닌 `전남`으로 표시되었다.
- KPN 아래에 표시되어야 하는 `등록 : 혈통` 등의 등록구분이 표시되지 않았다.
- 등록구분 표시를 활성화한 후 미등록 개체가 `미등`으로 표시되었다.

### 원인

- `AnimalParseData.cs`의 공통 출하지역 파싱에서 길이가 긴 주소의 앞 두 글자만 사용하여 `전남진도군...`이 `전남`으로 잘렸다.
- 해남진도 전용 XAML에서 `RegistrationCategory`를 `ShowMatherBloodConverter`에 전달했지만, 해당 Converter는 `Y` 또는 `N` 값을 기준으로 표시 여부를 판단하므로 `혈통`, `기초`, `고등`, `미등` 값에서는 등록구분 영역이 숨겨졌다.
- 공통 파싱 결과의 미등록 값은 `미등`이며, 기존 `MotherLevelConverter`에도 이를 `미등록`으로 확장하는 해남진도 전용 규칙이 없었다.

### 수정 파일

- `Services/AnimalParseData.cs` — 해남진도축협 출하지역 파싱 조건 추가
- `Views/Size128_128/Running/CustomAuctionRunning1/HaenamJindo.xaml` — 등록구분 행 상시 표시 및 해남진도 전용 변환 매개변수 적용
- `Models/Converter/MotherLevelConverter.cs` — 해남진도 화면에서만 `미등`을 `미등록`으로 변환

### 수정 내용

- 해남진도축협 사업장 조건에서 원본 주소에 `해남`이 포함되면 출하지역을 `해남`으로 설정한다.
- 원본 주소에 `진도`가 포함되면 출하지역을 `진도`로 설정한다.
- 기존 익산·군산 사업장 조건과 다른 사업장의 공통 파싱 흐름은 유지한다.
- 해남진도 128×128 경매 진행 화면에서 등록구분 영역을 숨기던 잘못된 Visibility 조건을 제거했다.
- KPN 아래에 `등록 : 기초/혈통/고등/미등록` 내용이 항상 표시되도록 변경했다.
- `ConverterParameter=HaenamJindo`를 지정한 경우에만 `미등`을 `미등록`으로 변환하도록 했다.
- 해남진도 전용 처리임을 관련 C# 코드와 XAML에 주석으로 기록했다.

### 영향 범위

- 해남진도축협 출하 개체의 출하지역 파싱 결과
- 해남진도축협 128×128 경매 진행 화면의 등록구분 표시
- `MotherLevelConverter` 공통 코드에 조건이 추가되었지만 `HaenamJindo` 매개변수를 지정한 화면에서만 새 변환이 적용된다.
- 다른 사업장과 다른 화면의 기존 `미등` 표시 및 등록구분 변환에는 영향이 없도록 제한했다.
- 해남진도축협의 낙찰·유찰 화면 및 다른 해상도에 대한 영향은 미확인이다.

### 빌드 결과

- 대상: `CowAuctionSmall.csproj` Debug 구성
- 명령 또는 방법: `dotnet build .\CowAuctionSmall.csproj --configuration Debug --artifacts-path <임시 출력 경로>`
- 결과: 성공
- 경고·오류 또는 미수행 사유: 오류 0개, 기존 경고 100개

### 테스트 결과

- [ ] 경매 진행 화면
- [ ] 낙찰 화면
- [ ] 유찰 화면
- [ ] 화면 전환 및 반복 진입
- [ ] 데이터 바인딩 갱신과 빈 값 처리
- [x] 사업장별 조건 분기
- [ ] 주요 전광판 해상도
- [ ] 통신·파싱 회귀 확인
- 결과 및 미수행 항목:
  - 해남진도 경매 진행 화면에서 출하지역과 등록구분 행이 표시되는 것은 사용자 확인 완료.
  - 최종 변경된 `미등록` 문구의 실제 실행 화면 표출 확인은 미수행.
  - 해남진도 전용 사업장 코드와 Converter 매개변수 조건은 소스 diff 및 빌드로 확인.
  - 낙찰·유찰 화면, 화면 반복 전환, 다른 해상도, 다른 사업장 실제 실행 테스트는 미수행.
  - 자동화 테스트는 미수행.

### Git

- Git commit 메시지: `fix: 해남진도축협 출하지역 및 등록구분 표시 개선`
- Commit hash: `a66fe1819ee7ec149c0304b4030aabd02201e3c6`
- Push 여부: 완료 (`origin/master`)

### 추가 확인사항

- 최종 배포 프로그램에서 미등록 개체가 `등록 : 미등록`으로 표시되는지 확인이 필요하다.
- 실제 전광판에서 세 글자인 `미등록`이 잘리거나 다른 항목과 겹치지 않는지 확인이 필요하다.
- 낙찰·유찰 화면과 다른 전광판 해상도에 대한 테스트는 미수행이다.
- 실행 중인 배포 파일 및 운영 설정 교체는 미수행이다.
- 현재 Git 작업 트리에는 이번 작업과 별개인 `AGENTS.md` 수정과 미추적 `WORKLOG.md`가 존재한다.

---


## 기록 원칙

- 승인받아 실제로 완료한 작업만 기록한다.
- 새 기록은 이 안내문 바로 아래에 추가하여 **최신 작업이 위쪽**에 오도록 한다.
- 기존 기록은 삭제하거나 과거 내용까지 임의로 수정하지 않는다.
- 확인하지 않은 내용은 추측하지 않고 `미확인`, 실행하지 않은 검증은 `미수행`, 관련 없는 항목은 `해당 없음`으로 적는다.
- Git commit 전에는 commit hash를 `미생성`, push 여부를 `미수행`으로 기록한다.
- 비밀번호, API 키, 개인정보, 운영 서버 주소 등 민감한 정보는 기록하지 않는다.

---

<!-- 새 작업 기록을 이 주석 바로 아래에 추가하세요. -->


## YYYY-MM-DD — 작업명

### 작업 날짜

- YYYY-MM-DD

### 작업명

- 한 줄로 식별 가능한 작업명

### 작업 목적

- 왜 이 변경이 필요한지 설명

### 문제 현상

- 사용자가 본 증상과 발생 조건
- 예: 낙찰 화면으로 전환한 뒤 친자일치 뱃지가 이전 개체 값으로 남아 표시됨

### 원인

- 확인된 기술적 원인과 근거
- 추정인 경우 `추정`이라고 명시

### 수정 파일

- `경로/파일명.cs` — 수정 이유
- `경로/파일명.xaml` — 수정 이유

### 수정 내용

- 파일별 핵심 변경 사항
- 기존 동작을 유지하기 위해 고려한 내용

### 영향 범위

- 영향받는 화면, ViewModel, Service, 상태 또는 사업장
- 영향이 없어야 하는 화면·해상도·사업장
- 예: 경매 진행/낙찰/유찰 화면, 128×128 전광판, 춘천축협 조건 분기

### 빌드 결과

- 대상:
- 명령 또는 방법:
- 결과: 성공 / 실패 / 미수행
- 경고·오류 또는 미수행 사유:

### 테스트 결과

- [ ] 경매 진행 화면
- [ ] 낙찰 화면
- [ ] 유찰 화면
- [ ] 화면 전환 및 반복 진입
- [ ] 데이터 바인딩 갱신과 빈 값 처리
- [ ] 사업장별 조건 분기
- [ ] 주요 전광판 해상도
- [ ] 통신·파싱 회귀 확인
- 결과 및 미수행 항목:

### Git

- 제안 commit 메시지: `fix: 변경 내용을 간결하게 작성`
- Commit hash: 미생성
- Push 여부: 미수행

### 추가 확인사항

- 운영 환경 또는 실제 전광판에서 추가로 확인할 내용
- 남은 위험이나 후속 작업

---


## 작성 예시 — 2026-08-20 — 춘천축협 친자일치 뱃지 비활성 처리

> 아래 내용은 형식을 보여 주기 위한 예시다. 실제 작업 기록으로 사용하지 않는다.

### 작업 날짜

- 2026-08-20

### 작업명

- 춘천축협 친자일치 뱃지 비활성 처리

### 작업 목적

- 춘천축협 전광판에서는 친자일치 정보를 제공하지 않으므로 잘못된 뱃지가 표시되지 않게 한다.

### 문제 현상

- 춘천축협 경매 진행 및 낙찰 화면에 다른 사업장과 동일한 친자일치 뱃지가 표시된다.

### 원인

- 공통 파싱 결과의 `PaternityMatch` 값이 사업장 구분 없이 화면에 전달된다.

### 수정 파일

- `Services/AnimalParseData.cs` — 춘천축협 사업장 조건에서 표시값을 비활성화

### 수정 내용

- 기존 사업장 코드 판별 흐름을 사용해 춘천축협일 때 `PaternityMatch` 표시값을 `-`로 설정한다.
- 다른 사업장의 기존 파싱 및 표시 흐름은 유지한다.

### 영향 범위

- 춘천축협의 경매 진행·낙찰·유찰 화면에서 사용하는 친자일치 표시값
- 다른 사업장, 비고 영역 및 화면 레이아웃에는 영향 없음

### 빌드 결과

- 대상: CowAuctionSmall 솔루션 Debug 구성
- 명령 또는 방법: 기존 로컬 의존성을 사용한 restore 없는 빌드
- 결과: 성공
- 경고·오류 또는 미수행 사유: 신규 오류 없음

### 테스트 결과

- [x] 경매 진행 화면
- [x] 낙찰 화면
- [x] 유찰 화면
- [x] 화면 전환 및 반복 진입
- [x] 데이터 바인딩 갱신과 빈 값 처리
- [x] 사업장별 조건 분기
- [ ] 주요 전광판 해상도
- [x] 통신·파싱 회귀 확인
- 결과 및 미수행 항목: 개발 화면에서 조건 분기는 정상. 실제 128×128 전광판 확인은 미수행.

### Git

- 제안 commit 메시지: `fix: 춘천축협 친자일치 뱃지 비활성 처리`
- Commit hash: 미생성
- Push 여부: 미수행

### 추가 확인사항


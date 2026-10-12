# GAMSTest 오프라인 검증

.NET 10 Windows WPF의 STA 실행 파일이다. 가상 데이터와 숨겨진 HWND로 실제 화면을 생성하며 제품 App·MainWindow 또는 운영 통신을 시작하지 않는다.

저장소 루트에서 실행한다. Visual Studio 빌드와 충돌하지 않도록 별도 출력 경로를 사용한다.

```powershell
dotnet build Validation/DisplayTestChecks/DisplayTestChecks.csproj --artifacts-path "$env:TEMP\CowAuction-GAMSTest-Checks" -m:1
dotnet "$env:TEMP\CowAuction-GAMSTest-Checks\bin\DisplayTestChecks\debug\DisplayTestChecks.dll"
```

화면 PNG도 저장하려면 마지막 명령에 `--screenshots "$env:TEMP\CowAuction-GAMSTest-Checks\screenshots"`를 붙인다. 가상 자료만 저장하며 운영 스크린샷을 수집하지 않는다.

검사 항목은 네 해상도·사업장/품목별 선택 일치, 128×64 특수 순서, 320×64 미구현 화면, 설정 오류·DTD 거부·읽기 전용 가져오기·중복 슬롯, 실제 바인딩·캐시 재사용·설정 스냅샷, 대기/진행 상태, 수동/자동 전환·종료 정리, 제어창 콘텐츠 배치다.

100/200슬롯은 준비 시간, UI 연결 및 레이아웃 처리, CPU 누적 시간, 3회/6회 순환 및 종료 후 관리 메모리를 출력한다. 이 검사는 물리 전광판 출력 속도나 장시간 누수 부재를 증명하지 않는다. 실제 실행 결과는 [WORKLOG](../../WORKLOG.md), 사용법은 [점검 도구 문서](../../Docs/기능/전광판_점검도구.md)를 참고한다.

## 기존 UI 복원 검증

`--skip-measurements`를 붙이면 100/200슬롯 성능 측정을 생략하고 기능 검사를 실행한다. UI 변경 검증에서는 이 옵션과 `--screenshots`를 함께 사용했다.

추가 검사는 460×210 제어창, 파란 번호 테두리, 네 해상도 즉시 적용, 기존 버튼·밝기 170·Green 색 강도, BoardPage와 수동 유전 화면의 관계, 5초 순환 목록, 미구현 버튼 비활성화, 창 종료 정리, 자동 설정 경로 우선순위·StartPosition을 포함한다. 경매 프로그램 버튼은 주입한 콜백과 가상 실행 파일 경로로 검사하며 운영 앱을 실행하지 않는다.

2026-10-01 UI 복원 후 기능 검사 962개 통과. 성능 측정을 제외한 수치이므로 이전 전체 검사 개수와 직접 비교하지 않는다.

## Board.XML 파일 선택 검증

후속 변경에서 수동 해상도 선택을 제거하고 파일 선택으로 대체했다. 2026-10-01 `--skip-measurements --screenshots <임시 경로>` 검사 977개 통과. 네 크기를 각각 XML로 읽고 번호 순서·중복·첫 MultiBoard·원본 불변·현재 경로·users 표시 설정 유지·성공 시 순환과 캐시 정리를 확인한다. 취소·파일 누락·잘못된 XML·미지원 크기·400슬롯 초과 시 기존 화면과 순환을 유지하는지 검사한다. 파일 선택은 콜백으로 주입하며 작은 제어창 안에 버튼이 들어가는지도 확인한다.

## 진행 비고 연속성 검증

2026-10-01 비고 호스트 공유 및 같은 모드 재부착 생략 후 `--skip-measurements` 1,000개 통과. 실제 WPF 호스트에서 진행 재클릭·진행/유전 반복 전환·자동 순환 시 비고 호스트와 애니메이션 클록 동일성, 클록 시간 진행, Loaded/Unloaded와 IsRunning 불필요 변경이 없는지 확인한다. 숨긴 본문 DataContext 해제, 낙찰·단색·설정 교체 시 비고 정리, 자체 비고가 있는 번식우 페이지 전환도 검사한다. 프레임률과 100/200슬롯 성능 측정은 이번 실행에서 수행하지 않았다.

## 보드 크기 선택 상자 복원

후속 사용자 정정으로 출하 AMS 위에 크기 선택 상자를 복원했다. 네 크기 수동 변경 시 배치·users 설정·원본 XML 유지, XML 재선택 시 Size 복원, 실제 ComboBox의 양방향 바인딩을 추가 확인했다. `--skip-measurements --screenshots <임시 경로>` 검사 1,012개 통과. 완료 계획 13절의 수동 선택 제거 기록은 당시 변경 이력이며 현재 동작은 기능 문서를 따른다.

## 단일 EXE 게시 검증 — 2026-10-01

- `dotnet publish Tools/GAMSTest/GAMSTest.csproj -c Release -p:PublishProfile=SingleFile-win-x64 --artifacts-path <TEMP>/CowAuction-GAMSTest-SingleFile-20261001 -o <동일 경로>/single-exe -m:1 -v:minimal` 성공. 최초 복원 후 최종 게시에는 --no-restore를 사용했다.
- 최종 게시 폴더의 파일은 GAMSTest.exe 하나이며 147,565,391바이트다. 운영 Config·NLog.config·외부 XML·PDB가 없다. includedFrameworks에 .NET/Windows Desktop 10.0.12가 포함됨을 확인했다.
- 별도 artifacts 경로의 Debug 빌드 경고 0·오류 0, `DisplayTestChecks.dll --skip-measurements` 1,014개 통과. 외부 XML이 없는 경우 내장 예제의 배치·표시 옵션·색상과 파일을 만들지 않는 동작을 검사했다.
- 실제 게시 EXE를 해당 폴더를 작업 디렉터리로 하여 실행했다. 기본 128×128의 8개 배치(512×256), 진행·유전·낙찰·유찰·색상·번호 버튼을 확인했다. 실제 파일 선택창으로 임시 가상 Board.XML을 열어 160×64의 4개 배치(320×128)를 적용하고 선택 상자로 128×64(256×128)로 변경한 뒤 화면 전환과 정상 종료를 확인했다.
- 네이티브 WPF DLL이 단일 파일의 임시 추출 경로에서 로드됨을 확인했다. 출하 AMS 버튼의 운영 앱 실행, 운영 서버·실제 전광판·.NET 미설치 별도 PC에서의 실행은 미수행이다.
## 2026-10-12 외부 명령·수동 점멸 회귀 검사

동일 실행에 SokchoChecks를 포함한다. 속초 일반/행사 화면 6종·페이지 수·로그인 후 사업장 변경·캐시 모드 전환·실제 WPF 바인딩·비고 Unloaded 정리·도구 수동/자동 상태를 가상 데이터로 확인한다. 실제 운영 생성자는 실행하지 않는다. 속초 복원 후 최종 `--skip-measurements`는 1,127개 통과했으며 장비·운영 통신 검증을 뜻하지 않는다.

`--skip-measurements` 실행에 `NamedPipeChecks`를 포함한다. 실제 운영 Pipe와 충돌하지 않도록 고유한 검사 Pipe를 사용해 연결·재연결·NUMBER·RGB·STOP·종료 취소를 확인한다. 가짜 배치로 제어창과 표출창의 종료·화면 캐시 해제를 검사하며 운영 App이나 외부 전광판 제어 프로그램은 실행하지 않는다. ControllerChecks는 네 해상도의 수동 진행/유전 점멸 비활성, 자동 순환 활성, 같은 페이지 수동 재선택 시 점멸 종료와 비고 스크롤 유지도 검사한다.


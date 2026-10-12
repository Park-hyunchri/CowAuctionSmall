# 참고 프로젝트 선별 적용 오프라인 검증

2026-10-01 `Infra/XmlSettings` 명칭 변경: 제품·검증 네임스페이스 변경 후 `all` **5,728개** 통과. `OwnerHistoryChecks --offline`은 **55+25=80개** 통과. 두 검증 프로젝트 빌드는 각각 경고 0·오류 0이며 `<TEMP>/CowAuction-InfraRename-20261001`의 분리 출력으로 확인했다. 제품 앱·운영 API·UDP 실행은 하지 않았다. 기능 변경 없이 폴더와 네임스페이스를 변경했으며 아래 개별 기능 기록은 당시 결과로 유지한다.

2026-10-01 하단 상태바 최종 배치: `footer` **119개** 통과, 빌드 경고 0·오류 0. 구간 명칭 변경 시 `stage3` **594개**, 최초 통합 시 `stage4` **837개** 통과 기록이 있다. 후속 배치 조정은 `footer`로 검증했다. `footer`는 실제 Netty 수신 상태·예외 전달, 연결 종료·중복 접속·재접속, 실제 페이지 타이머 전이, 표시 문구·Dispatcher·종료 방어와 실제 MainWindow 하단 XAML을 검사한다. 폭 128/320/768/1200에서 로그 높이·상태 항목 범위·창 높이를 확인하고 320/1200 폭의 합성 PNG를 검증 출력 폴더에 생성한다. 제품 Window 생성자·운영 API·UDP는 시작하지 않는다. 이전 기록 수치는 당시 결과다.

```powershell
$footerArtifacts = Join-Path $env:TEMP 'CowAuction-Footer-20261001'
dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --artifacts-path $footerArtifacts -v:minimal
dotnet "$footerArtifacts/bin/ReferenceAdoptionChecks/debug/ReferenceAdoptionChecks.dll" footer
```

2026-09-28 비고 호스트 통합: `note-host` **76개**, `stage4` **596개**, `bidder` **253개** 통과. `note-host`는 진행/결과 입력, 실제 DisplayRunningPageNum·DisplaySold·DisplayUnSold 경로, 제외 페이지와 화면 전환 후 참조 해제를 검사한다. 운영 App 생성자·통신은 시작하지 않는다. 빌드 후 `dotnet <검증 출력 경로>/ReferenceAdoptionChecks.dll note-host`로 실행한다.

전체 `binding`은 추가된 CowHistorySearchRun을 포함해 진행 **33개**를 대상으로 하며, 진행 화면별 실패를 수집한 후 결과·공통 호스트 검사까지 실행하고 마지막에 실패를 보고한다. 비고 호스트 통합 당시 CowHistorySearchRun의 NumberFormatConverter 리소스 참조로 실패했으나, 2026-09-28 후속 화면 수정으로 해결했다. 현재 `binding` **3,524개**, `stage4` **596개** 통과. 농가명은 OwnerName, 주소는 사용자 지정에 따라 출하 지역 Location을 표시하며 이 화면의 실제 페이지 연결은 미정이다.

2026-09-23 이력 백그라운드 조회 개선 후 `history` **64개**가 통과했다. 기존 묶음 반영 검사 28개와 신규 36개로, 캐시 준비가 HTTP에 막히지 않는지, 최소 요청 간격·동시 수·예산 보존, 재시도 슬롯 반환, 구독 취소와 중복 요청 공유, 겹치는 대상 재사용, 세대·종료 보호 및 진행 집계를 검사한다. HTTP와 저장소는 합성이며 운영 API·캐시는 사용하지 않는다.

합성 처리 비교는 캐시 1건·신규 3건, 응답 지연 1,500ms, 시작 간격 1,000ms를 유지한다. 동시 1건 약 **4,515ms**, 동시 2건 약 **3,534ms**였고 결과와 실제 가짜 클라이언트 호출 수는 같았다. 새 처리 경로의 동시 수 비교이며 과거 제품 바이너리와의 전후 측정은 아니다. 운영 기본값은 동시 1건·최소 1초를 유지하므로 이 수치를 운영 개선율로 사용하지 않는다.

`binding` **3,485개**는 실제 MainWindow 하단 상태 영역의 XAML을 추출해 표시·갱신·단방향 바인딩·종료 후 무시를 추가 검사한다. MainWindow 생성자의 서버·타이머를 시작하지 않는다. 실행 위치는 저장소 루트다. `updates` **55개**, `stage4` **596개**, 별도 `OwnerHistoryChecks --offline` **80개**, `--scroll-offline` **18개**도 통과했다. Debug·Release 제품 빌드는 오류 0개·기존 경고 78개였다.

```powershell
dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -p:OutputPath=$env:TEMP\CowAuction-HistoryBackground-Debug -v:q
dotnet "$env:TEMP\CowAuction-HistoryBackground-Debug\ReferenceAdoptionChecks.dll" history
dotnet "$env:TEMP\CowAuction-HistoryBackground-Debug\ReferenceAdoptionChecks.dll" binding
```

2026-09-23 성별 표시 이관 후 `binding` **3,471개**가 통과했다. `SexDisplayChecks`는 원본 성별 보존, `프리마틴 → 프리`, 3글자 가로 압축과 초기화, 예천 등록구분·숨김·테두리 재사용을 검사한다. `SexStyleBaseline.json`은 변경 전 Converter에서 확보한 4개 프로필·19개 입력의 76개 고정 기준이다. 기존 색상·폰트·굵기·Margin·변환과 실제 32개 진행/37개 결과 화면의 갱신을 비교한다. 문자열을 객체용 Converter에 전달하던 3곳의 기본 검정 스타일도 그대로 검증한다. 글꼴 크기와 실제 글자 폭은 다를 수 있으므로 실제 FormattedText와 외곽선 geometry로 압축·잘림을 확인한다.

성별용 ResourceDictionary는 일반 Application에 명시적으로 등록한다. 운영 App·통신은 시작하지 않는다. `stage4` **596개**, `updates` **55개**, 별도 `OwnerHistoryChecks --scroll-offline` **18개**도 통과했다. Debug·Release 빌드는 오류 0개·기존 경고 78개였다. 대표 화면 16개의 합성 렌더링도 확인했으며 실제 전광판 가독성과 성능 전후 비교는 미수행이다. 아래 기록의 검사 횟수는 각 이전 작업 당시 결과다.

```powershell
dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -p:OutputPath=$env:TEMP\CowAuction-SexDisplay-Debug -v:q
dotnet "$env:TEMP\CowAuction-SexDisplay-Debug\ReferenceAdoptionChecks.dll" binding
dotnet "$env:TEMP\CowAuction-SexDisplay-Debug\ReferenceAdoptionChecks.dll" stage4
dotnet "$env:TEMP\CowAuction-SexDisplay-Debug\ReferenceAdoptionChecks.dll" updates
```

2026-09-23 가격·중량·임신 표시 판단을 `AuctionItemDisplayViewModel`로 이관했다. Running 루트는 기존 패널 ViewModel의 `ItemDisplay`를 사용하고, Sold·UnSold 루트에는 공통 표시 ViewModel을 연결한다. 결과 화면의 일반 항목 영역과 별도 NoteHost·배지는 `gValues` 스냅샷을 유지한다. 아래 2026-09-22의 결과 화면 DataContext 설명과 수치는 당시 기록이다.

이번 `binding`은 숫자·결장 문구·중량 단위·임신 조건·문화권·스냅샷 소유권의 고정 기대값 검사와 실제 화면 바인딩을 포함하여 **2,155개**가 통과했다. 가격 네 자리 변경, 빈 중량, 임신 표시/해제, 같은 원본 참조의 갱신, 낙찰/유찰 전환, 단방향 Run 바인딩을 확인한다. `updates` **55개**, `stage4` **596개**, 별도 OwnerHistoryChecks의 `--scroll-offline` **18개**도 통과했다. Debug·Release 빌드는 오류 0개, 기존 경고 78개였다. 검증은 합성 데이터·오프라인 화면으로 수행했으며 실제 서버·전광판과 성능 전후 비교는 미수행이다.

```powershell
dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -p:OutputPath=$env:TEMP\CowAuction-ItemDisplay-Debug -v:q
dotnet "$env:TEMP\CowAuction-ItemDisplay-Debug\ReferenceAdoptionChecks.dll" binding
dotnet "$env:TEMP\CowAuction-ItemDisplay-Debug\ReferenceAdoptionChecks.dll" updates
dotnet "$env:TEMP\CowAuction-ItemDisplay-Debug\ReferenceAdoptionChecks.dll" stage4
```

2026-09-22 가운데 번호의 배경 강조 제거 후에는 배경 전용 검사를 제거하여 `entity` **34개**, `binding` **1,497개**가 통과했다. 별도 TEMP 출력 Debug 빌드는 오류 0개, 기존 경고 80개였다. `HighlightBackground` 속성과 XAML 지정 49곳을 제거했으며 색상·굵기·번호 분리 검사는 유지했다. 아래 최초 구현 시점의 배경 검사와 실행 수는 과거 기록이다.

2026-09-22 개체번호 공통 표시 검사는 `entity` 모드 또는 `all`에 포함한다. `EntityNumberChecks`는 하나의 TextBlock 안의 Run 3개, 가운데 네 자리의 색상·굵기·배경, 동적 리소스 변경, 글자 크기 상속, 소 4자리/염소 3자리 축약, 잘못된 값·삭제·새 번호·축종 변경과 SV 파서 연결을 검사한다. 별도의 `EntityNumberShort` 데이터 속성은 사용하지 않는다. `BindingChecks`도 `EntityNumberText.Number` 바인딩과 실제 인라인 텍스트를 확인하도록 변경했다. Debug 전체 **2,843개**, Release `entity` **35개**, Release `binding` **1,497개**가 통과했다. 사용법과 전환 범위는 [개체번호 공통 표시](../../Docs/개체번호_공통표시_사용법.md)를 참조한다.

2026-09-22 MVVM 바인딩 검사는 `binding`으로 단독 실행하거나 `all`에 포함한다. 일반 `System.Windows.Application`과 화면에 표시하지 않는 `HwndSource`를 사용하며 제품 `App`, 운영 서버, UDP 페이지 동기화를 시작하지 않는다. 의도적으로 잘못된 경로를 넣어 WPF 바인딩 진단 Listener가 실제 오류를 검출하는지 먼저 검사한 뒤, 대상 화면의 오류 없음과 실제 표시 값을 함께 확인한다. Listener는 검사가 끝나면 제거한다.

검사 범위는 진행 페이지 **32개**, 결과 화면 **37개**, 진행/결과 비고 호스트, 공통 배지다. 결과 화면 중 기존 320×64 낙찰·유찰 2개는 데이터 표시가 없는 빈 구현이므로 생성·해제와 오류 없음만 확인한다. 같은 이유로 빈 진행 320×64 2페이지는 데이터 바인딩 대상 32개에 포함하지 않는다. 진행 루트에는 `AuctionContPanelViewModel`, 결과 화면과 배지에는 `gValues`를 연결한다. 새 객체 교체, 원본 객체를 변경한 뒤 `UpdateCowInfo` 호출, 가격·개체번호·EPD·체형 표시, 비고/이력 변경·삭제, 친자 배지, `IsRunning`만 변경한 진행 테두리, 중첩 진행 페이지, 반복 로드/해제를 확인한다. 공개된 `CowInfo`는 읽기 전용 표시 스냅샷으로 취급하며 임의의 내부 직접 수정으로 WPF 갱신을 보장하는 계약은 아니다.

낙찰→유찰→낙찰 검사에서는 생성자의 UDP·타이머 시작만 우회한 `DisplaySelect`에 최소한의 화면 저장소와 합성 설정을 주입하여 실제 `DisplaySold`/`DisplayUnSold`를 호출한다. 원본을 같은 참조로 수정해 다시 전달해도 화면과 비고가 갱신되고, 컨트롤은 재사용되며, 동일 데이터는 표시 컨텍스트를 교체하지 않는지 확인한다. 원본과 표시 객체의 소유권도 검사한다.

단독 Debug `binding` **1,493개** 검사가 통과했다. 501개 ViewModel에 무변경 / 가격 1건 변경 / 전체 변경을 전달했을 때 `CowInfo` 알림과 스냅샷 교체는 각각 **0 / 1 / 501회**였다. 같은 실행의 할당은 **40 / 1,024 / 493,024바이트**, 시간은 약 **0.27 / 0.66 / 0.96ms**였다. 이는 ViewModel 전달 경계의 측정으로 실제 전광판 렌더링 완료 시간이 아니다. 글자 위치·겹침·색상·스크롤 속도의 육안 비교는 현장 확인 항목이다.

```powershell
$bindingOutput = Join-Path $env:TEMP 'CowAuction-BindingValidation-Debug'
dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -p:OutputPath=$bindingOutput -v:q
dotnet "$bindingOutput\ReferenceAdoptionChecks.dll" binding
```

2026-09-22 Debug·파일 로그 비동기 검사는 `logging`으로 단독 실행하거나 `all`에 포함해 실행한다. 합성 원문 501건의 시각·순번·빈 필드·순서 보존, 다중 생산자, 건수/문자량 포화, 출력 예외, 정상 종료·종료 시간 초과·늦은 입력, Debug/Release 및 비활성 옵션의 문자열 계산 생략을 검사한다. 출력 대상을 이벤트로 막은 상태에서도 생산자가 완료되는지 확인하므로 단순한 처리 시간 추정에 의존하지 않는다.

NLog 검사는 별도 `LogFactory`에 제품 `NLog.config`를 읽고, **로그 호출 전에 파일 경로를 고유 TEMP 디렉터리로 바꾼다.** 합성 파일 501건과 최종 Warn/Error, XML 설정 재적용·누락/실패/미완료 집계·제한 시간 종료를 검증한 뒤 해당 임시 파일을 제거한다. 파일 출력이 막힌 상태에서도 Debug 소비가 완료되는지 검사한다. 운영 로그 파일·운영 서버·제품 App은 사용하지 않는다.

최종 별도 TEMP 출력의 Debug·Release 빌드는 각각 **오류 0개 / 경고 84개**, Debug `all`은 **1,312개**, Release `logging`은 **59개**가 통과했다. 새 검사 수는 59개이며 기존 경매 검사 1,253개도 통과했다. 별도 `OwnerHistoryChecks --offline`은 **55개 + 22개 = 77개**가 통과했다. Release 검사는 검사 프로그램도 Release로 빌드하여 `[Conditional("DEBUG")]` 호출의 부수 효과와 원문 수집·출력 작업 생성이 사라지는지 확인한다.

실제 Debug 실행 비교에서는 시작 전에 `COWAUCTION_DEBUG_OUTPUT=0`을 지정하면 제품 Debug 원문·진단 출력을 끈다. 기본값은 켜짐이며 `COWAUCTION_DIAGNOSTICS=1`은 반복 화면·타이머 진단을 추가한다. 이 옵션은 파일 업무 로그를 끄지 않는다. Visual Studio 출력 창을 숨기는 것과 출력 기능 자체를 끄는 것을 구분해 측정한다. Release에서는 제품 Debug 출력이 제외된다.

```powershell
$debugOutput = Join-Path $env:TEMP 'CowAuction-LoggingValidation-Debug'
$releaseOutput = Join-Path $env:TEMP 'CowAuction-LoggingValidation-Release'
dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -p:OutputPath=$debugOutput -v:q
dotnet "$debugOutput\ReferenceAdoptionChecks.dll" all
dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj -c Release --no-restore -p:OutputPath=$releaseOutput -v:q
dotnet "$releaseOutput\ReferenceAdoptionChecks.dll" logging
```

합성 느린 출력에 대한 Debug 실행에서 501건의 동기 생산자 시간은 약 **7,586ms / 출력 501회**, 비동기 등록은 **0.64ms / 최종 비우기까지 59.44ms / 출력 4회**였고 누락은 없었다. 가짜 출력은 호출마다 `Thread.Sleep(1)`을 요청하며, 이 환경에서는 Windows 타이머 단위 때문에 실제 대기가 약 15ms였다. 이는 **실제 Visual Studio가 7.6초에서 59ms로 빨라졌다는 결과가 아니다.** 제어된 느린 출력이 생산자를 직접 기다리게 하지 않고 묶음 호출이 줄어드는지를 보여준다.

같은 Debug 실행에서 출력 꺼짐 / Debug만 / 파일만 / 둘 다의 등록 시간은 각각 **0.01 / 0.70 / 0.74 / 1.62ms**, 생산자 스레드 할당은 **0 / 40,416 / 144,304 / 184,720바이트**였다. 해당 비교의 파일 대상도 묶음마다 지연을 주는 가짜 대상이다. 파일 실제 쓰기 성공은 별도 TEMP 파일 검사로 확인한다. 할당량은 큐에 남은 메모리나 프로세스 전체 CPU 사용량이 아니며, 실제 디스크·디버거·Netty 수신·화면 렌더링 성능 비교는 현장 확인 항목이다. Debug와 파일을 모두 끈 조건도 단순 루프 기준선이며 운영 설정의 로그 비활성화를 변경하지 않는다.

2026-09-22 SV/API 공통 갱신 검사는 `updates`로 단독 실행하거나 `all`에 포함해 실행한다. 실제 39필드 SV와 75필드 API의 공통 값/사업장 표시 옵션/낙찰자 위치, 필드 부재와 잘못된 숫자, 비고 삭제, API 전용 정보 보존, 지연 응답과 일치 확인 경합, 동일 출품의 개체 교체, 계류대 이동 및 전체 목록 삭제를 검사한다. 표시 묶음은 최초 즉시 전달, 최신 값 합치기, 복사본 소유권, Clear/Dispose 이후 콜백 무효화를 검사한다.

해당 변경 후 별도 TEMP 출력 폴더의 Debug·Release 빌드는 각각 **오류 0개 / 경고 84개**, `all`은 **1,253개 assertion 통과**였다. 별도 `OwnerHistoryChecks --offline`은 **55개 + 22개 = 77개 통과**했다. 동일 측정에서 `입력 501건 → 알림 횟수 / 전달 행 수`는 전체 동일 **0 / 0**, 3건 변경 **2 / 3**, 전체 변경 **3 / 501**, 동일 출품 반복 변경 **2 / 2**이었다. 알림 횟수는 실행 속도와 30ms 묶음 경계에 따라 소폭 달라질 수 있다. 업무 처리 시간은 각각 약 **20 / 23 / 32 / 28ms**, 할당량은 약 **8.1 / 8.1 / 10.1 / 7.7MB**로, 가격 하나만 읽던 기존 처리보다 늘었다. 이번 측정으로 확인한 것은 처리 범위 확대와 화면 전달량 감소이며 CPU 시간·메모리 할당 감소나 실제 전광판 표시 지연 개선을 입증한 것은 아니다.

같은 출품번호의 개체 교체 도중 도착한 과거 API 응답 보호와, 다른 개체의 AF 재조회 응답이 현재 개체/낙찰 결과를 바꾸지 못하는 회귀 검사도 최종 전체 통과 결과에 포함한다.

`updates-baseline`은 기능 assertion 없이 동일한 합성 501건의 전체 동일·3건 변경·501건 변경·동일 출품 반복 변경 처리량을 출력한다. 변경 전 제품 DLL을 별도 출력 폴더에 보존한 상태에서 이 측정 도구를 실행한 기준선은 네 시나리오 모두 **알림 501회 / 전달 행 501개**였다. `state_ms`는 단일 업무 소비 큐 처리 완료까지, `flushed_ms`는 표시 묶음 비우기 완료까지이며 실제 화면 렌더링 시간이 아니다. 할당량은 프로세스 전체의 대략값이므로 독립된 정밀 마이크로벤치마크로 해석하지 않는다. 원문 출력 비용을 제외하기 위해 Netty 디코더 대신 동일한 업무 메시지를 직접 주입하고, 디버거 연결 여부도 함께 출력한다. 기존 기준선은 표시 묶음 비우기 API가 없어 60ms 대기를 사용했으므로 `flushed_ms` 전후 비교에는 적합하지 않다.

실행 중인 제품 출력 경로와 분리해 검증하려면 다음처럼 빌드 후 생성된 검사 DLL을 실행한다.

```powershell
$validationOutput = Join-Path $env:TEMP 'CowAuction-SvValidation'
dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -p:OutputPath=$validationOutput -v:q
dotnet "$validationOutput\ReferenceAdoptionChecks.dll" updates
dotnet "$validationOutput\ReferenceAdoptionChecks.dll" all
```

2026-09-22 경매 안정성 보완: `stage3`에 가격 변경 직후 시작, AF 결과/참가번호/금액 일치, 일괄 상태 수명, 즉시 새로고침, 날짜 전환 중 개체 혼합 방지, 최근 수신 안내·원문 배제, 야간 종료 유예 검사를 추가했다. 최종 `--no-restore -- all`에서 **1,181개 assertion**이 통과했다. 운영 앱·서버 접속이나 실제 종료 없이 fake 조회와 합성 데이터로 검사하며, 실제 전광판 가독성·서버 프로토콜 규격을 검증한 결과는 아니다. SZ 구간/미응찰 필터는 규격 미확인으로 추가하지 않았다. 상세 변경과 명령은 [WORKLOG](../../WORKLOG.md)의 2026-09-22 기록을 참조한다.

WPF의 이미지 디코더와 상태/데이터 처리 클래스를 검사하는 콘솔 프로젝트다. 제품 `App`, `MainWindow`, `DisplaySelect`를 생성하지 않으며 운영 서버나 실제 계류대 데이터를 사용하지 않는다. 서비스 통합 검사는 fake `IAuctionDataSource`와 fake `HttpMessageHandler`를 주입한 `ServerGetData`를 생성한다. 일반 서비스 검사는 `InitializeAsync`를 호출하지 않는다. 예외적으로 취소 수명 검사는 완전한 fake `IAuctionSessionRuntime`이 네트워크 사용 불가를 반환하도록 주입한 뒤 `InitializeAsync`의 대기를 `StopAsync`로 취소하고, 인증·연결·조회가 호출되지 않았음을 확인한다. 설정 파일도 읽거나 변경하지 않는다. 로고 검증용 이미지와 GIF 차단용 fixture는 고유 임시 디렉터리에 생성하고 검증 후 해당 디렉터리만 제거한다.

```powershell
dotnet restore Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj
dotnet run --project Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -- stage1
dotnet run --project Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -- stage2
dotnet run --project Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -- stage3
dotnet run --project Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -- stage4
dotnet run --project Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -- history
dotnet run --project Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --no-restore -- all
```

- `stage1`: 정적 로고 공유·GIF 직접/대문자/확장자 위장 차단·손상/누락 시 대체 표시, 보드 전체/부분 갱신 집계·삭제 메시지·원본 API 집계 보존.
- `stage2`: 잘못된 상태 입력, 낙찰/유찰 결과 우선, 시작·종료·결과 순서와 재경매 상태 판단.
- `stage3`: 메시지와 원본 컬렉션/객체의 소유권 분리 및 복제 필드 보존, 순차 큐의 순서·포화·예외·오래된 응답·종료 처리. 실제 AT/AS/AF 핸들러를 합성 메시지로 호출해 HTTP 재조회가 대기 중이어도 후속 경매 상태가 반영되는지 검사한다. 날짜 전환 시 진행 중 위치 보호 해제와 불완전한 API 행 거부, 요청 완료 결과와 서비스 종료 대기도 확인한다.
- `stage4`: 501 슬롯 합성 데이터의 반복 갱신 생략 및 실제 변경 감지, 페이지 번호 보정과 정지 사유, 화면 컨트롤 직접 생성과 숨겨진 HWND에서 Loaded/Unloaded·비고·낙찰자 이름 확인. 일반 `System.Windows.Application`만 생성한다.
- `history`: 합성 개체번호와 개별 완료 제어, 지연 UI 큐로 이력 묶음의 단일 반영·부분 노출 차단·중복 제거·동일 대상 반복 조회 억제·수동 연타 합류·대상/날짜 변경·Reset/Dispose 후 오래된 반영 차단을 검사한다. 메모리 캐시와 fake 이력 클라이언트로 성공 빈 값, 실패, 기존 캐시 보존 및 날짜 변경 시 결과 무효화도 확인한다.

통과 횟수는 개별 assertion 수다. 실제 전광판 렌더링·운영 프로토콜 연동·다중 PC 동기화 성공이나 성능 개선 수치를 의미하지 않는다.

해상도별 직접 생성 검사에서는 합성 `gValues`를 DataContext로 사용한다. 별도 `DisplayPanelStore` 수명 검사에서는 실제 `AuctionContPanelViewModel`을 생성해 해제 여부를 확인한다. 전체 앱의 화면 선택·페이지 회전·소유주 이력 서비스까지 연결하는 검사는 아니다. 공통 낙찰자 이름 컨트롤의 애니메이션 생성/해제는 반복해서 검사한다.

2026-09-17 최종 검증: 위 `--no-restore -- all` 명령의 빌드와 실행이 성공했으며 **879개 assertion이 모두 통과**했다. 불완전한 HTTP 재조회 응답이 현재 세대에서 도착해도 기존 화면 데이터를 유지하는 검사도 포함한다. 기존 프로젝트의 nullable·미사용·obsolete·MVVM 경고가 출력되었으며 이 검증 프로젝트 자체의 빌드 오류는 없었다. 실제 전광판·다중 PC 동기화·전체 사업장 화면 조합과 성능 전후 비교는 미수행이다.

같은 날 QCN 날짜 경합 보완 후 `--no-restore -- stage3`로 관련 검증을 다시 빌드·실행해 **219개 assertion이 통과**했다. 새로 추가된 3개는 이전 날짜 QCN 요청의 발송 차단, 현재 날짜 결과 적용, 조회 중 날짜 변경 시 캐시·타이머의 오래된 결과 반영 차단이다. `QueueQcnRequestAsync`와 fake 조회 서비스만 사용하며 Netty 재연결 메서드는 호출하지 않는다. 최종 검사 정의는 총 882개이며, 마지막 변경 후 전체를 반복 실행하지 않고 영향받는 3단계를 재검증했다.

같은 날 파일 구조 개편 검증에서는 기준선과 파일 이동 완료 후 각각 `all` **882개**, 책임 분리와 새 수명·페이지 계산 검사를 추가한 뒤 `all` **897개**가 통과했다. 마지막 fake 초기화 취소 검사 추가 후 영향받는 `stage3` **222개**가 통과했으며, 당시 전체 검사 정의는 **898개**였다. 마지막 추가 후 전체 반복 실행은 하지 않았다. 별도 `OwnerHistoryChecks --offline`은 **55개 + 22개 = 77개**를 통과했다. 실제 앱·전광판·PC 간 통신 및 성능 전후 비교는 계속 미수행이다.

같은 날 축산물 이력 묶음 표시 변경 후 `history` **28개**, `all` **926개**, `OwnerHistoryChecks --offline` **77개**가 모두 통과했다. `dotnet build CowAuctionSmall.csproj -c Debug --no-restore -v:q` 및 동일 명령의 `Release` 빌드는 각각 경고 **88개**, 오류 **0개**로 성공했다. 이력 검사는 조정자와 서비스 경계의 오프라인 검증이며, 제품 ViewModel 전체 연결과 실제 전광판의 묶음 표시 경험은 미검증이다. 실제 서버 접속은 수행하지 않았다.

## 오늘 이외 로그 압축 — 2026-10-01

`log-archive`는 새 `LogArchiveChecks`만 실행한다. `logging`은 기존 비동기 로그 검사와 압축 검사를 함께 실행하며 `all`에도 포함된다. 제품 App이나 운영 서버를 시작하지 않고 고유 TEMP 폴더의 가상 파일만 처리한다.

```powershell
dotnet build Validation/ReferenceAdoptionChecks/ReferenceAdoptionChecks.csproj --artifacts-path "$env:TEMP\CowAuction-LogArchive-20261001" -m:1 -v:minimal
dotnet "$env:TEMP\CowAuction-LogArchive-20261001\bin\ReferenceAdoptionChecks\debug\ReferenceAdoptionChecks.dll" log-archive
```

Debug·Release 빌드 경고 0·오류 0. 압축 검사 각각 29개 통과, Debug `logging` 통합 검사 88개 통과. 오늘 파일/폴더 보호·날짜 없는 파일 수정일·오늘 이외 날짜·빈 파일·여러 버퍼 크기의 바이트 일치·다른 확장자·재실행·기존 ZIP 일치/손상/충돌·사용 중/읽기 전용 파일·내용 추가 후 재시도·취소·ZIP 게시 후 중단 복구·백그라운드 실행·날짜 변경·중복 시작/종료를 확인했다. 운영 로그·실제 디스크 장애·프로세스 강제 종료 및 링크/정션의 실환경 검증은 미수행이다.

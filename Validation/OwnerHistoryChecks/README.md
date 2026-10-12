# 이력·캐시 오프라인 검증

```powershell
dotnet run --project Validation/OwnerHistoryChecks/OwnerHistoryChecks.csproj --no-restore -- --offline
```

`--offline`은 `Program.RunChecks`와 `MoreChecks.Run`만 실행한다. 파싱·이력 조회·캐시 재사용·동시 요청·날짜 전환·보존 정책을 fake `HttpMessageHandler`와 고유 임시 SQLite/XML 파일로 검사한다. `DailyCacheService`에 임시 경로를 명시하여 운영 캐시를 사용하지 않는다.

`--scroll-offline`은 일반 `System.Windows.Application`과 공통 `RunningNoteHost128`을 생성하여 합성 데이터로 실제 WPF 스크롤 시계를 검사한다. 진행 방식은 `ItemDisplay` 단방향 바인딩과 `ScrollOwner`, 결과 방식은 `ItemDisplay` 직접 전달을 사용한다. 2026-09-28 통합 후 **32개** 통과: 페이지 변경·동일 데이터·가격만 변경 시 유지, 같은 문구의 원본/이력 색상 변경 시 위치 유지, 비고·개체 변경 시 재시작, 제외 페이지 복귀 위치 복원, null 복구·빠른 교체·반복 로드/해제·대기 중 초기화 취소를 확인한다. 제품 `App`, `DisplaySelect`, UDP 동기화, API·캐시는 시작하지 않는다.

2026-09-22 MVVM 변경에서 동일 비고를 가진 다른 개체로 바뀔 때 현재 스크롤 시계를 먼저 뒤쪽으로 이동시킨 뒤 새 시계로 재시작하는지 검사하도록 보강했다. 수정 전 진행 호스트에서 해당 검사가 실패했고, 경매품 영역의 컨텍스트 변경을 애니메이션 수명에 연결한 수정 후 **18개 스크롤 검사**가 통과했다.

```powershell
$scrollOutput = Join-Path $env:TEMP 'CowAuction-BindingValidation-History'
dotnet build Validation/OwnerHistoryChecks/OwnerHistoryChecks.csproj --no-restore -p:OutputPath=$scrollOutput -v:q
dotnet "$scrollOutput\OwnerHistoryChecks.dll" --scroll-offline
```

기본 실행은 이후 `UiChecks.Run`까지 진입한다. 이 경로도 제품 App 대신 일반 Application을 사용하지만 기존 `DisplaySelect` 등 화면·통신 관련 객체를 생성하므로 위 두 제한된 오프라인 모드와 구분한다. `--live`는 실제 조회 기능이다. 기본 실행과 `--live`는 이번 검증에서 실행하지 않는다. 이력 오프라인 검사 성공은 실제 앱·운영 서버·전광판 검증 성공을 의미하지 않는다.

2026-09-17 구조 개편 검증에서 기존 복원 자산을 갱신한 뒤 위 명령을 실행하여 `Program.RunChecks` **55개**, `MoreChecks.Run` **22개**, 합계 **77개**가 통과했다. 기존 검증의 라이브·UI 코드는 새 네임스페이스와 생성자에 맞춰 컴파일만 확인했으며 실행하지 않았다.

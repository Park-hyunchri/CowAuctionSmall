# 코드 리뷰 (현 상태 기준)

## 검토 범위
- `Services/FlowTextAnimation.cs`
- `Services/DisplaySelect.cs`
- `ViewModels/AuctionContPanelViewModel.cs`
- `Models/Structures/gValues.cs`

## 주요 이슈 (우선순위 순)

### 1) 이벤트 해제 누락 가능성 (메모리/성능 누수) [수정 완료]
- 위치: `Services/FlowTextAnimation.cs:93`
- 내용: `_isAnimating`이 `false`인 상태에서 `Stop()`이 호출되면 `StopAnimation(false)`가 즉시 `return`하여 `UnhookUnload/UnhookSizeChanged/UnhookTextChanged`가 실행되지 않습니다.
- 재현 시나리오: 텍스트가 짧아서 스크롤이 시작되지 않은 경우(`_shouldScroll == false`). `Start()`에서 이벤트는 등록되지만 애니메이션은 시작되지 않음 → `Unloaded` 시 `Stop()` 호출 → 이벤트 해제 누락.
- 영향: 뷰가 제거되어도 핸들러가 남아 메모리 누수/불필요 콜백 발생 가능.
- 제안: `StopAnimation`에서 `_isAnimating`이 `false`여도 `keepHooks == false`면 해제 로직을 수행하도록 조건 수정.

### 2) 바인딩 갱신 누락 가능성 (UI 미갱신) [수정 완료]
- 위치: `ViewModels/AuctionContPanelViewModel.cs:81`
- 내용: `IsRunning` 변경 시 `OnPropertyChanged(nameof(_isRunning))` 호출로 속성명 불일치.
- 영향: XAML 바인딩이 `IsRunning`을 참조한다면 변경 알림이 전달되지 않아 UI가 갱신되지 않을 수 있음.
- 제안: `OnPropertyChanged(nameof(IsRunning))`로 수정.

### 3) 타이머 재생성 시 중복 실행/리소스 누수 가능성 [수정 완료]
- 위치: `Services/DisplaySelect.cs:137`
- 내용: `StartPageRotation()`에서 `_timer`를 새로 생성하지만 기존 타이머 Dispose/Stop 처리가 없음.
- 영향: `OnRefreshMsg` 등에서 여러 번 호출되면 타이머 중복으로 페이지 전환이 겹치거나 리소스 누수가 발생할 수 있음.
- 제안: 새 타이머 생성 전 기존 `_timer`의 `Stop()` 및 `Dispose()` 처리 후 재생성하거나 단일 타이머 재사용.

### 4) 업데이트 스킵 로직의 충돌 위험 [수정 완료]
- 위치: `Models/Structures/gValues.cs:12`
- 내용: `UpdateSignature()`가 많은 필드의 `GetHashCode()`를 합산해 `int` 해시로 비교. 해시 충돌 시 데이터가 바뀌어도 업데이트가 스킵될 수 있음.
- 영향: 드물지만 실제 데이터 변경이 UI에 반영되지 않는 버그로 이어질 수 있음.
- 제안: `HashCode` 구조체 사용 또는 `long`/`ulong` 기반으로 충돌 가능성 완화. 혹은 핵심 필드 비교로 변경.

### 5) Equals/GetHashCode 불일치 [수정 완료]
- 위치: `Models/Structures/gValues.cs:471`, `Models/Structures/gValues.cs:535`
- 내용: `Equals()`는 일부 필드만 비교하지만 `GetHashCode()`는 기본 구현 사용.
- 영향: `Dictionary`/`HashSet` 키로 사용 시 동작 불일치 가능. (현재 사용 여부에 따라 리스크 수준 달라짐)
- 제안: `Equals`와 동일한 기준의 `GetHashCode` 구현 또는 `Equals` 제거.

## 추가 개선 포인트 (리스크 낮음)
- 위치: `Services/DisplaySelect.cs:734`
- 내용: `VirtualizingPanel.SetIsVirtualizing`가 `VirtualizingStackPanel`에 직접 적용되어도 실제 가상화 효과는 제한적일 수 있음.
- 영향: 오해/착각으로 성능이 개선된다고 믿을 가능성.
- 제안: 가상화는 `ItemsControl` 기반 구조에서 효과적이므로 실제 컨테이너 구조 확인 필요.

## 테스트/검증 제안
- 노트가 짧아 스크롤이 시작되지 않는 경우, 페이지 전환 시 이벤트가 제대로 해제되는지 확인.
- `IsRunning` 바인딩 값이 실제로 반영되는지(테두리 점멸 등).
- `UpdateSignature` 스킵 로직 적용 후에도 모든 화면 요소가 정상 갱신되는지 샘플 데이터로 확인.
- 페이지 회전 타이머가 재시작될 때 중복 타이머가 동작하지 않는지 확인.

## 요약
현재 구조는 GPU/CPU 최적화를 위한 방향성이 좋고, 페이지 트리 제거/업데이트 스킵/노트 복원 로직이 체감 성능에 도움이 될 수 있습니다. 다만 **이벤트 해제 누락, 바인딩 알림 이름 오타, 타이머 재생성 누락**은 실제 동작 오류로 이어질 수 있어 우선 수정이 필요합니다.

---

## 추가 코드 리뷰 (다른 소스)

### 6) 데이터 파싱 길이 검증 부족 (IndexOutOfRange 위험) [수정 완료]
- 위치: `Services/AnimalParseData.cs:171`
- 내용: `data.Length > 41` 검사만 하고 `data[45]~data[71]`를 직접 접근.
- 영향: 서버 데이터가 짧게 올 경우 `IndexOutOfRangeException`로 파싱 전체가 실패 가능.
- 제안: `data.Length`를 충분히 확인하거나 전부 `SafeGet`로 통일.

### 7) 테스트 코드가 실데이터를 덮어씀
- 위치: `Services/AnimalParseData.cs:241`
- 내용: “테스트” 블록에서 `Is_Nh_QQuri`를 짝/홀로 강제 설정하고 능력치를 상수/랜덤으로 덮어씀.
- 영향: 실제 데이터가 들어와도 화면이 테스트 값으로 변질됨(표출 오류).
- 제안: 테스트 블록 제거 또는 `#if DEBUG`로 분리.

### 8) 네티 메시지 길이 검증 부재 + 조건 우선순위 오류
- 위치: `Services/NettyAsyncMsgProcess.cs:139`, `Services/NettyAsyncMsgProcess.cs:153`, `Services/NettyAsyncMsgProcess.cs:200`
- 내용: `data[6]` 접근이 길이 체크 전에 수행됨. `data.Length >= 7 && runningState == AS.PROGRESS || runningState == AS.COMPLETED`는 우선순위 때문에 `COMPLETED`일 때 길이 체크가 무시됨.
- 영향: 짧은 메시지 수신 시 즉시 예외로 네티 처리 중단 가능.
- 제안: 길이 체크를 먼저 수행하고, 조건은 괄호로 묶어 정확히 평가.

### 9) async void 사용으로 예외 누락 위험
- 위치: `Services/NettyAsyncMsgProcess.cs:245`
- 내용: `Process_NettyState_AF`가 `async void`라 예외가 전파되지 않음.
- 영향: 통신 오류/예외가 로깅 없이 사라질 수 있음.
- 제안: `Task` 반환으로 변경하고 호출부에서 await 처리.

### 10) 응답 리스트가 비어있을 때 예외 가능 + 반환 null 불일치
- 위치: `Services/ServerConn.cs:170`
- 내용: `currentInfoList.First()` 사용 전에 빈 리스트 확인 없음. 또한 `SvInfoRequest`는 `List<string>` 반환인데 null 반환.
- 영향: 서버가 빈 응답을 주면 예외 발생. 호출부에서 null 처리 누락 시 NRE 가능.
- 제안: `currentInfoList?.Count > 0` 검사 후 처리, 반환 타입을 `List<string>?`로 조정.

### 11) 인증서 검증 무효화 + HttpClient 재생성
- 위치: `Services/ServerConn.cs:226`
- 내용: TLS 인증서 검증을 항상 통과시키고, 요청마다 `HttpClient`를 새로 생성.
- 영향: 보안 취약점 및 소켓 고갈 위험. DI로 주입한 `_http` 설정이 무시됨.
- 제안: 인증서 검증은 옵션화하고, `GetCurrentInfo`도 `_http`를 재사용하도록 변경.

### 12) 공유 리스트 동시 접근 (경합/예외 가능)
- 위치: `Services/ServerGetData.cs:300`, `Services/ServerGetData.cs:490`
- 내용: `_latestAuctionDataList`, `_beforeAuctionDataList`를 갱신하는 스레드와 메시지 핸들러가 동시에 접근. 일부 구간만 lock 사용.
- 영향: `InvalidOperationException` 또는 중간 상태 데이터 전파 가능.
- 제안: 리스트 접근 규칙을 통일하고 lock 범위를 명확히 하거나 스냅샷 복사 사용.

### 13) 로고 리스트 인덱스 가정
- 위치: `Models/LogoManager.cs:64`
- 내용: `LogoBoard?[0]`를 바로 접근. LogoBoard가 비어 있으면 `ArgumentOutOfRangeException`.
- 영향: 로고 XML이 비거나 비정상일 때 앱 시작 시 크래시 가능.
- 제안: 리스트 길이 검사 후 접근.

### 14) UI 로그 문자열 무제한 누적
- 위치: `ViewModels/MainWindowViewModel.cs:239`
- 내용: `MainWindowTextBox`에 계속 `+=`하여 누적.
- 영향: 장시간 실행 시 메모리/렌더링 비용 증가.
- 제안: 로그 라인 수 제한(예: 최근 N줄 유지) 또는 파일로 분리.

### 15) 로그 호출 오류 및 메시지 불일치
- 위치: `Views/MainWindow.xaml.cs:78`, `Views/MainWindow.xaml.cs:106`
- 내용: `logger.Equals(...)`는 로그 기록이 아님. 메모리 임계값(6GB)과 로그 문구(3GB 초과)가 불일치.
- 영향: 실제 장애 원인 추적이 어려움.
- 제안: `logger.LogInfo`로 수정하고 메시지 정합성 맞추기.

### 16) XML 파서의 강제 int 파싱 [수정 완료]
- 위치: `Models/XMLParser/BoardXmlParser.cs:39`
- 내용: `int.Parse`로 바로 변환.
- 영향: 설정 XML에 비정상 값이 있으면 앱 시작 시 바로 크래시.
- 제안: `int.TryParse`로 유효성 검사 후 로깅.

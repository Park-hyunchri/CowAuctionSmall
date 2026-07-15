# Master/Sub 회전 불일치 + 유전능력 페이지 고정 개선 계획 (2026-02-24)

## 1. 배경
- 현 구조는 `MASTER`가 회전 타이머를 구동하고, `SUB`는 동기 신호를 받아 화면만 따라가는 방식이다.
- 운영 중 아래 현상이 발생할 수 있다.
1. `MASTER`는 회전 중인데 `SUB1`만 회전이 멈춤
2. 유전능력 페이지(예: 2페이지)로 전환된 뒤 해당 페이지에 고정됨

## 2. 원인 가설 (코드 기준)
1. `SUB`는 자체 회전 타이머를 사용하지 않아 동기 신호 누락/잠김 시 정지 상태가 길어질 수 있음
2. `ShouldFreezeRunningRotation()` 조건(`_lockFirstPageUntilRefresh`, `IsAuctionInProgress`)이 한쪽 PC에서만 유지되면 동작 불일치가 발생할 수 있음
3. 일부 분기에서 타이머만 정지하고 페이지를 명시적으로 1페이지로 복귀시키지 않아, 정지 시점의 페이지가 그대로 고정될 수 있음

## 3. 목표
1. `MASTER`/`SUB` 간 회전 상태 불일치 최소화
2. `SUB` 동기 신호 미수신 시 자동 복구(로컬 fallback)
3. 타이머 정지 시 유전능력 페이지 고정 방지
4. 문제 재현 시 원인 추적 가능한 로그 확보

## 4. 범위
- `Services/DisplaySelect.cs`
- `Services/PageTimerSync.cs`
- (선택) `Config/users.XML` / `Models/XMLParser/UserXmlParser.cs`에 fallback 관련 설정 값 추가

## 5. 설계 방향

### 5.1 SUB 동기 미수신 fallback
- `DisplaySelect`에 `lastPageSyncReceivedUtc`를 기록
- `SUB` 상태에서 일정 시간(예: 3~5초) 동기 미수신이면 로컬 회전 fallback 진입
- 동기 재수신 시 즉시 `SUB` 동기 모드로 복귀

### 5.2 정지 분기에서 페이지 강제 복귀
- 회전 중지 분기(`Refresh else`, freeze 분기 등)에서 항상 `ApplyRunningPageToAll(1)` 수행
- 타이머 stop만 하고 화면이 현재 페이지에 남는 경로 제거

### 5.3 freeze 상태 전이 명확화
- `_lockFirstPageUntilRefresh` 설정/해제 시점 로그 강화
- `Refresh` 수신 전/후 상태값을 기록하여 한쪽 PC만 lock 유지되는 문제를 추적 가능하게 함

### 5.4 로그 강화
- 다음 이벤트를 단일 포맷으로 기록
1. `MASTER/SUB` 전환
2. `SUB` 동기 수신 시각 갱신
3. fallback 진입/복귀
4. 회전 stop/start + 현재 페이지
5. lock 설정/해제

## 6. 구현 단계

### Step 1. 관측 지표 추가 (로그만)
- 상태 전이 로그 추가
- 운영 로그로 실제 불일치 발생 패턴 확인

### Step 2. 고정 페이지 방지 우선 수정
- 정지 분기에서 `ApplyRunningPageToAll(1)` 강제
- 유전능력 페이지 고정 현상 즉시 차단

### Step 3. SUB fallback 도입
- 동기 미수신 타임아웃 기반 로컬 회전 시작
- 동기 재수신 시 SUB 모드 복귀

### Step 4. 설정 외부화(선택)
- `users.XML`에 `SubFallbackTimeoutMs`, `EnableSubFallback` 추가
- 미설정 시 안전 기본값 사용

### Step 5. 안정화
- 반복 테스트(2PC, 네트워크 단절/복구, Refresh 연속 수신) 후 임계값 보정

## 7. 검증 시나리오
1. 정상 2PC 연결: `MASTER` 회전 시 `SUB` 동기 회전 유지
2. `SUB` 네트워크 일시 단절: 타임아웃 후 fallback 회전, 복구 시 동기 복귀
3. `Refresh` 연속 수신: 양쪽 lock 해제/재시작 일관성 확인
4. 경매 진행/종료 반복: 1페이지 고정과 회전 재개가 의도대로 동작
5. 유전능력 페이지 전환 중 stop 이벤트: 1페이지로 복귀되는지 확인

## 8. 완료 기준 (Acceptance Criteria)
1. `MASTER` 회전 중 `SUB` 단독 정지 현상 재현률이 현저히 감소
2. 유전능력 페이지 고정 재현 불가
3. 로그만으로 상태 전이(전환/lock/fallback)를 추적 가능
4. 기존 경매 진행/표출 기능 회귀 없음

## 9. 리스크 및 대응
1. fallback 임계값이 너무 짧으면 모드 전환이 빈번해질 수 있음
- 대응: 히스테리시스(진입/복귀 임계 분리) 적용
2. 양쪽 동시 MASTER 위험
- 대응: `PageTimerSync` 우선순위 규칙(IP 비교/heartbeat) 유지 + 복귀 로직 보수적으로 설계

## 10. 롤백 계획
- Step 2 이후 문제 발생 시 fallback 기능만 feature flag로 비활성화
- 기존 동기 방식 유지 상태로 즉시 복귀 가능하게 구현

## 11. 진행 현황 (2026-02-24)
- Step 1 완료: `[Rotation]`, `[PageSync]`, lock/fallback 상태 로그 추가
- Step 2 완료: 회전 정지 분기에서 `ApplyRunningPageToAll(1)` 강제 적용
- Step 3 완료: `SUB` 동기 미수신 시 fallback 진입/동기 수신 시 복귀 로직 적용
- Step 4 완료: `users.XML` 설정(`EnableSubFallback`, `SubFallbackTimeoutMs`) 외부화 및 파싱/적용
- 로컬 검증 완료: 빌드 성공, ESC 종료 정상, 신규 로그(`config fallback-enabled`, `Rotation/PageSync`) 기록 확인

### Step 5 진행 항목
1. 2PC 정상 연결 상태에서 `MASTER` 회전 시 `SUB` 동기 유지 확인
2. `SUB` 네트워크 단절 후 `SubFallbackTimeoutMs` 경과 시 fallback 회전 진입 확인
3. 네트워크 복구 후 `SUB`가 동기 모드로 복귀하는지 확인
4. `Refresh` 연속 수신 시 양쪽 lock 해제/재시작 일치 여부 확인
5. 경매 진행/종료 반복 시 유전능력 페이지 고정 재발 여부 확인

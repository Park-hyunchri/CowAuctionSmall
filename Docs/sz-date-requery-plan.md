# SZ 날짜 재조회 계획

## 목표
- `SZ` 신호에 날짜가 포함되면(예: `SZ|...|20260219|...|P|...`) 해당 날짜를 기준 데이터로 사용한다.
- "오늘 차수데이터가 없음" 상태여도 `SZ` 날짜의 경매 개체를 화면에 표시한다.

## 현재 문제
- `_beforeAuctionDataList`가 `null`인 상태에서 `SZ`가 들어올 수 있다.
- 기존 `SZ` 처리에 로고 강제 로직(`AuctionResultStatus = "00"`, `beforeList.Clear()`)이 있어 실제 개체 표시를 가릴 수 있다.
- 메시지 전달 단계와 API 조회 단계에서 날짜 사용이 일관되지 않다.

## 목표 동작
- 날짜 `D`를 포함한 `SZ(P)` 수신 시:
1. `D` 기준으로 차수(`QCN`)를 조회한다.
2. `D` 기준으로 목록(`SV/SC`)을 조회한다.
3. 파싱 후 `_beforeAuctionDataList`를 교체한다.
4. 복사 리스트로 `DataChangedMessage`를 보내 UI를 갱신한다.
- 재조회 실패 시에는 기존 화면 데이터를 유지하고 사유만 로그로 남긴다.

## 범위
- `Services/NettyAsyncMsgProcess.cs`
- `Services/ServerConn.cs`
- `Services/ServerGetData.cs`
- 선택 사항: `Views/Size128_128/Running/Standard_non_X_Run1.xaml` UI 일관성 점검

## 구현 단계
1. `NettyAsyncMsgProcess`
- `DataToServerGetArrMsg`에 `SZ` 날짜를 포함해 전달한다.
- 날짜 동기화를 위해 `DataToServerConnMsg(data[2])` 전달은 유지한다.

2. `ServerConn`
- current-info 조회에 강제 날짜 파라미터를 지원한다.
- `SvInfoRequest(..., date)` 호출 시 날짜가 URL 선택까지 전달되도록 보장한다.

3. `ServerGetData` (`case "SZ"`)
- 메시지 payload에서 `szDate`를 읽는다.
- 날짜 기반 재조회 경로(`QCN` -> `SV/SC`)를 실행한다.
- 전체 row를 `Parse_PacketApi`로 파싱한다.
- 파싱 결과를 정렬 후 `_beforeAuctionDataList`에 교체 반영한다.
- 아래 메시지를 전송한다:
  - `DisplaySelectRefresh("Refresh")`
  - `DataChangedMessage(new List<gValues>(...))`
- 정상 재조회 성공 시 로고 강제 로직은 제거/우회한다.

4. 실패 처리
- 사용자/토큰/날짜가 유효하지 않으면 로그 후 분기를 종료한다.
- API 응답이 비어 있으면 로그를 남기고 이전 리스트를 유지한다.
- 일시 실패 상황에서 현재 화면 데이터를 비우지 않는다.

## 검증 체크리스트
- [ ] 오늘 차수데이터가 없어도 날짜 포함 `SZ` 수신 시 개체가 표시된다.
- [ ] `_beforeAuctionDataList`가 `null`이어도 날짜 포함 `SZ` 수신 시 리스트가 재구성되어 표시된다.
- [ ] `SZ` 재조회 성공 시 예상치 못한 로고 전환이 발생하지 않는다.
- [ ] `Standard_non_X_Run1`의 등록 텍스트 색상이 스타일 기준으로 유지된다(상속 `lime` 오동작 없음).

## 로깅 계획
- 다음 항목에 대해 1줄 로그를 추가한다:
  - 수신된 `SZ` 날짜
  - 재조회 시작/종료
  - 파싱된 개체 수
  - 폴백 사유(입력 오류 / API 빈 응답 / 파싱 결과 없음)

## 범위 외
- `CowDisplaySelector`의 우선순위/선별 로직 재설계
- `SZ` 날짜 재조회와 무관한 일반 `AS`/`SV` 동작 변경

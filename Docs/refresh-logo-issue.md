# 새로고침 시작 신호 + 로고 고정 현상 의심 지점 (2026-01-26)

아래는 로그의 "새로고침 시작 신호"와 화면이 로고만 고정되는 현상과 연결될 수 있는 코드 지점입니다. 각 항목은 실제 동작 흐름에 영향이 큰 부분만 추렸습니다.

## 1) AS.NONE(8001)에서 무조건 새로고침 트리거
- 위치: `Services/NettyAsyncMsgProcess.cs:175-180`
- 내용: 단일 경매에서 AS 상태가 NONE(8001)일 때 **조건 없이** 새로고침 메시지를 보냅니다.
  - `RefreshAuctionSV_Message("Refresh")` 전송
  - `DataToServerGetArrMsg(... "AS", "8001", "refresh")` 전송
- 영향: 경매일 아침/준비(READY, START) 단계에서 서버가 8001을 잠깐 내보내면 새로고침이 바로 시작됨.
- 로그 대응: "새로고침 시작 신호"는 이 경로에서 시작되는 케이스와 일치.

## 2) 새로고침 차단 조건이 _runRunSipNumber 하나뿐
- 위치: `Services/ServerGetData.cs:611-621`
- 내용: `AS 8001 refresh` 메시지를 받으면 `_runRunSipNumber == -1`일 때만 새로고침 시작 신호를 띄움.
- 문제 가능성:
  - `_runRunSipNumber`는 **8004(PROGRESS)** 에서만 설정됨 (`Services/ServerGetData.cs:580-606`).
  - 8002(READY), 8003(START)에서는 값이 -1로 유지 → 새로고침 차단이 되지 않음.
  - 가격 불일치 등으로 8004 처리 실패 시에도 -1 유지 → 새로고침이 계속 허용됨.

## 3) Refresh 모드가 SV 메시지를 “가로채고” 버릴 수 있음
- 위치: `NetProto/netty/handlers/AuctionClientInboundDecoder.cs:107-116, 156-160, 264-268`
- 동작:
  - Refresh 신호를 받으면 2초 동안 `_isRefreshMsg = true`.
  - 그동안 들어오는 **첫 번째 SV 메시지를 가로채서** `HandleRefreshAsync`로 처리.
  - 그런데 `aucDt == 오늘`이면 실제 새로고침 호출을 하지 않고 끝남.
- 영향:
  - 오늘 경매일인데도 Refresh 신호가 떴다면 **SV 메시지가 소모만 되고 UI 갱신이 안 되는 상황**이 발생할 수 있음.
  - 결과적으로 화면이 로고 상태로 고정되는 현상과 연결될 가능성.

## 4) 로고 고정은 AuctionResultStatus == "00" 또는 기본값 경로
- 위치: `Services/DisplaySelect.cs:905-933`
- 내용: AuctionResultStatus가 11/22/23이 아니면 무조건 로고 표시.
- 로고로 치환하는 경로:
  - `Services/ServerGetData.cs:397` (삭제 처리 시 일괄 로고 치환)
  - `Services/ServerGetData.cs:488` (계류대 이동 시 기존 위치 로고)
  - `Services/ServerGetData.cs:737` (SZ 메시지 수신 시 전체 로고)
- 영향: 특정 메시지(SZ 등)나 데이터 삭제가 발생하면 **전체 패널이 로고로 갱신**될 수 있음.

## 5) 새로고침 SV 경로가 "기존 데이터 있음"이면 갱신을 건너뜀
- 위치: `Services/ServerGetData.cs:632-664`
- 내용: `SV`(새로고침 데이터) 수신 시, `beforeList.Count > 0`이면 갱신을 생략하고 로그만 찍음.
- 영향: Refresh 신호는 뜨는데 실제 데이터 갱신이 안 되어 로고 화면이 그대로 남을 수 있음.

## 6) _currentRefreshDate가 실제로는 설정되지 않음
- 위치: `Services/ServerGetData.cs:49, 164-165`
- 내용: 날짜 오버라이드는 변수만 있고 할당 경로가 없음.
- 영향: Refresh로 다른 날짜를 봐야 하는 상황에서 날짜가 그대로면 **데이터 없음/로고 고정**을 유발할 수 있음.

---

### 로그와 매칭되는 핵심 흐름 요약
1. 서버에서 AS 8001(대기) 잠깐 발생 → Refresh 트리거
2. Refresh 모드가 SV 메시지를 가로채고, 오늘 날짜면 실제 갱신 없이 종료
3. UI는 로고 상태로 유지되거나, 데이터 삭제 경로로 로고로 교체됨

필요하면 위 의심 지점들 기준으로 로그를 더 찍어서 흐름을 좁히면 빠르게 확인 가능.

# 단일경매 MAUI 실수신 증거표

2026-09-17 KST. [버튼별 해석](단일경매_버튼별_MAUI실수신.md)과 함께 읽는다. 시각은 조작 표식이며 수신 시각 자체가 아니다. NettyCodeProcess 경계의 입력 순서이고 SV·SS 전체 수신은 관찰하지 않는다.

최초 파일은 덮어쓰기 전에 도구로 읽은 행의 재구성 발췌본이다. 원본 전체 추적 복구본이 아니다. 첫/두 번째 재시작은 각각 보관한 추적 파일에서 생성했다. 실패한 시도와 앱 종료 후 표식을 성공 검사로 세지 않는다.

## 최초 검토 발췌 재구성본

로컬 근거: `artifacts/single-rx/pre-restart-reviewed.txt` (Git 제외).

| 조작 표식 | 시작 시각 | 다음 표식 전 관찰 순서 | 판정 참고 |
|---|---|---|---|
| `single-business-boundary` | 22:56:35.166 | 업무 입력 없음 | 경계 표식; 버튼 아님 |
| `single-refresh-verified` | 22:56:46.093 | AS(8001) → AS(8002) |  |
| `single-refresh-verified-end` | 22:56:51.067 | 업무 입력 없음 | 경계 표식; 버튼 아님 |
| `single-held-open` | 22:57:15.504 | 업무 입력 없음 |  |
| `single-held-select` | 22:57:36.500 | AS(8001) → AS(8002) |  |
| `single-start` | 22:57:43.327 | AS(8002) → AS(8003) → AS(8004) |  |
| `single-space-pause` | 22:57:47.127 | SD(F) |  |
| `single-space-resume` | 23:00:55.262 | 검증 불가 | NightSchedule 이후: 수신 검증 불가 |

## 첫 재시작 원문 보관본

로컬 근거: `artifacts/single-rx/first-restart-received.txt` (Git 제외).

| 조작 표식 | 시작 시각 | 다음 표식 전 관찰 순서 | 판정 참고 |
|---|---|---|---|
| `single-restarted-boundary` | 23:03:16.538 | 업무 입력 없음 | 경계 표식; 버튼 아님 |
| `single-start-repeat` | 23:03:27.033 | AS(8002) → AS(8003) → AS(8004) |  |
| `single-pause-repeat` | 23:03:28.890 | SD(F) |  |
| `single-resume-repeat` | 23:03:30.040 | 업무 입력 없음 |  |
| `single-cancel-repeat` | 23:03:31.206 | SD(F) → AF(24) → AS(8002) |  |
| `single-force-unsold-start` | 23:03:57.517 | AS(8002) → AS(8003) → AS(8004) |  |
| `single-force-unsold` | 23:03:59.128 | AS(8006) → AF(23) |  |
| `single-bid-start` | 23:04:12.369 | AS(8002) → AS(8003) → AS(8004) → SD(C) → SD(C) → SD(C) → SD(F) → AS(8006) → AF(23) | 응찰 전 자동 유찰 포함 |
| `single-browser-bid` | 23:04:42.906 | 업무 입력 없음 | 실패/지연 시도. 낙찰 성공으로 계산 금지 |
| `single-force-sold` | 23:04:44.759 | 업무 입력 없음 | 실패/지연 시도. 낙찰 성공으로 계산 금지 |
| `single-bid-retry-start` | 23:05:28.361 | AS(8002) → AS(8003) → AS(8004) → SD(C) → SD(C) → SD(C) → SD(F) → AS(8006) → AF(23) | 응찰 전 자동 유찰 포함 |
| `single-force-sold-valid` | 23:06:03.013 | 업무 입력 없음 | 실패/지연 시도. 낙찰 성공으로 계산 금지 |
| `single-fast-bid-start` | 23:06:27.746 | AS(8002) → AS(8003) → AS(8004) |  |
| `single-fast-force` | 23:06:31.921 | AS(8006) → AF(22) | AF22 및 앱 종료 반영 후 NightSchedule. 화면 캡처 실패 |

## 두 번째 재시작 원문 보관본

로컬 근거: `artifacts/single-rx/second-restart-received.txt` (Git 제외).

| 조작 표식 | 시작 시각 | 다음 표식 전 관찰 순서 | 판정 참고 |
|---|---|---|---|
| `single-second-restart` | 23:07:03.927 | 업무 입력 없음 | 경계 표식; 버튼 아님 |
| `single-greeting` | 23:07:18.528 | 업무 입력 없음 |  |
| `single-voice-stop-1` | 23:07:19.941 | 업무 입력 없음 |  |
| `single-ending` | 23:07:20.964 | 업무 입력 없음 |  |
| `single-voice-stop-2` | 23:07:21.924 | 업무 입력 없음 |  |
| `single-guide` | 23:07:22.916 | 업무 입력 없음 |  |
| `single-voice-stop-3` | 23:07:23.910 | 업무 입력 없음 |  |
| `single-spare1` | 23:07:24.882 | 업무 입력 없음 |  |
| `single-spare2` | 23:07:25.847 | 업무 입력 없음 |  |
| `single-spare3` | 23:07:26.834 | 업무 입력 없음 |  |
| `single-spare4` | 23:07:27.871 | 업무 입력 없음 |  |
| `single-spare5` | 23:07:28.851 | 업무 입력 없음 |  |
| `single-spare6` | 23:07:29.885 | 업무 입력 없음 |  |
| `single-status-ready` | 23:07:30.844 | 업무 입력 없음 |  |
| `single-status-running` | 23:07:31.817 | 업무 입력 없음 |  |
| `single-status-sold` | 23:07:32.796 | 업무 입력 없음 |  |
| `single-status-held` | 23:07:33.817 | 업무 입력 없음 |  |
| `single-connection-board` | 23:07:34.814 | 업무 입력 없음 |  |
| `single-connection-video` | 23:07:35.772 | 업무 입력 없음 |  |
| `single-connection-pen` | 23:07:36.783 | 업무 입력 없음 |  |
| `single-play-idle` | 23:07:37.766 | 업무 입력 없음 |  |
| `single-pause-idle` | 23:07:38.735 | 업무 입력 없음 |  |
| `single-manual-disabled` | 23:07:39.698 | 업무 입력 없음 |  |
| `single-aux-end` | 23:07:40.693 | 업무 입력 없음 | 경계 표식; 버튼 아님 |
| `single-price-up` | 23:07:58.778 | AS(8003) |  |
| `single-price-down-restore` | 23:08:00.400 | AS(8003) |  |
| `single-price-right-down` | 23:08:01.566 | AS(8003) |  |
| `single-price-up-restore` | 23:08:02.743 | AS(8003) |  |
| `single-category-fatten` | 23:08:03.916 | 업무 입력 없음 |  |
| `single-category-breeding` | 23:08:05.122 | 업무 입력 없음 |  |
| `single-category-calf-restore` | 23:08:06.279 | 업무 입력 없음 |  |
| `single-board-switch` | 23:08:07.439 | AS(8002) → AS(8003) → AS(8004) → SD(F) → AF(24) → AS(8002) |  |
| `single-round-open` | 23:08:09.112 | 업무 입력 없음 |  |
| `single-round-confirm` | 23:08:30.145 | AS(8007) |  |
| `restore-batch-entry` | 23:09:30.474 | AT(10) |  |
| `restore-batch-complete` | 23:09:57.158 | 업무 입력 없음 | 종료 표식. 이후 관찰 없음 |

## 추출·판정 기준

표식은 다음 표식까지의 경계다. 버튼 조작과 모든 구간 내 이벤트 사이 인과를 자동 확정하지 않는다. 자동 카운트다운/결과를 포함한 구간은 별도 표시했다. 마지막 종료 표식 뒤의 빈 구간은 버튼 무신호 검사가 아니다. 디코더 초기 AT10→AT20→AT20 검토 발췌는 시간대가 다르므로 위 최초 업무 구간에 붙이지 않았다.

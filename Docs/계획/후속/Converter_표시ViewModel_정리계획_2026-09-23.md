# Converter 및 경매품 표시 ViewModel 정리 계획

> 문서 상태: **부분 완료** · 정리일: 2026-10-01
> 가격·중량·임신 1차 이관 외에 별도 성별 표시 계획도 구현됐다. 농협·혈통 문구·품목·EPD 등 나머지 범위는 후속 대상으로 남긴다.
> 현재 동작: [기능 문서](../../기능/README.md) · [전체 목차](../../README.md) · [작업 기록](../../../WORKLOG.md)

- 작성일: 2026-09-23
- 상태: 부분 완료 — 가격·중량·임신 1차 이관 및 별도 성별 표시 이관 완료. 그 외 범위는 후속 작업.
- 목적: 중복 Converter와 분산된 표시 판단을 정리하고, 최저가·낙찰가 등 공통 표시 규칙을 경매 상태에 관계없이 재사용한다.
- 최초 요청은 계획 문서 작성이었으며, 후속 요청에서 아래 1차 범위의 구현을 승인했다.

## 1차 적용 결과 — 2026-09-23

사용자가 이관 목록의 3번까지를 지정하여 최저가·낙찰가, 중량, 임신 정보 표시 판단을 먼저 이관했다. 농협·혈통 문구, 품목·EPD·친자 판단과 스타일 정리는 후속 범위다. 아래 전체 계획의 1~6단계를 모두 완료한 것은 아니다.

- `AuctionItemDisplayViewModel`에 가격·중량 표시 문자열과 `ShowPregnancy`를 추가했다. 숫자 서식과 중량 단위는 `AuctionDisplayFormatter`에서 계산한다.
- Running은 `AuctionContPanelViewModel.ItemDisplay`, Sold·UnSold는 루트 DataContext에서 같은 표시 ViewModel을 사용한다. 이번에 이관하지 않은 필드와 NoteHost는 기존 `gValues` 스냅샷 경로를 유지한다.
- 관련 XAML 61개를 수정하고 표시 바인딩 128개를 이관했다. 읽기 전용 표시 속성에는 `Mode=OneWay`를 명시했다.
- `NumberFormatConverter`, `WeightKGConverter`, `ChuncheonWeightConverter`, `PregnancyValueConverter`의 활성 참조를 제거하고 클래스 4개를 삭제했다. 다른 Converter는 유지했다.
- 의도한 표시 차이: 기존 쉼표가 없던 유찰 화면 3곳도 가격 쉼표를 적용한다. 단위가 있는 중량의 빈 값·공백·0·`-`는 `-`로 통일한다. 숫자만 표시하는 중량의 기존 서식은 유지한다.
- Debug·Release 빌드 오류 0개, 기존 경고 78개. `binding` 2,155개, `updates` 55개, `stage4` 596개, 별도 `--scroll-offline` 18개 통과. 실제 서버·전광판 확인과 성능 전후 비교는 미수행이다.

아래 현황 표는 최초 계획 작성 시점의 기준이며, 위 1차 적용 결과를 함께 참조한다.

## 1. 현재 확인한 문제

현재 `Presentation/Converters`에는 Converter 소스 23개가 있다. 개수 자체보다 책임과 사용 방식의 차이가 문제다.

| 대상 | 현재 동작과 문제 |
| --- | --- |
| `NumberFormatConverter` | 문자열 숫자를 해석하여 `#,##0`으로 표시하면서, 파라미터에 따라 `결장`을 `-`로 바꾸는 상태 처리도 수행한다. 최저가·낙찰가·중량에 함께 사용된다. |
| `WeightKGConverter`, `ChuncheonWeightConverter` | 중량에 `kg`를 붙이는 목적은 같지만 공백·빈 문자열·입력 타입 처리가 다르다. |
| `SelectShowWeight_EPDString`, `SelectShowWeightEpdMultiConverter`, `MokpoEPD` | 중량·EPD 표시 판단이 분산되어 있다. 단순 Y/N, 품목 5 예외, 사업장별 품목 그룹 조건이 서로 다르다. |
| `PregnancyValueConverter`, `NhLabelConverter` 등 | 임신 표시 여부와 품목별 문구 등 데이터 해석이 XAML Converter에 들어 있다. |
| `CowTypeToStyleConverter_*` | 성별 해석과 화면별 색상·크기·여백이 결합되어 있다. 128×64·160×64 구현은 Convert 호출마다 Style과 Setter를 생성한다. |
| `CowTypeToStyleConverter_V3`, `BloodFontStyle`, `CowCowDist_NumToString` | 이미 Style을 정적으로 재사용하는 구현도 있다. 모든 Converter가 매번 리소스를 만드는 것은 아니다. |
| 친자 관련 Converter와 `HasPaternityMatch` | Converter는 `일치` 여부를, 현재 속성은 결과 문자열의 존재 여부를 판단한다. 이름만 보고 같은 조건으로 합칠 수 없다. |
| XAML 리소스 선언 | 같은 파일에서 사용되지 않는 선언이 있다. 다른 리소스 사전·코드 참조까지 확인해야 삭제 여부를 확정할 수 있다. |

Running의 루트는 `AuctionContPanelViewModel`이고 내부 항목 영역은 `CowInfo`를 사용한다. Sold·UnSold는 `DisplaySelect`에서 복제한 `gValues`를 직접 DataContext에 연결한다. Running ViewModel에만 표시 속성을 추가하면 세 상태에서 재사용할 수 없다.

## 2. 책임 분리와 보존 범위

| 위치 | 담당할 책임 |
| --- | --- |
| 원본 모델 `gValues` | 수신 데이터, 기존 복제·동등 비교·캐시 계약 유지 |
| 공통 경매품 표시 ViewModel | 가격 표시 문자열, 임신·품목·EPD 등 표시 조건과 문구 제공 |
| 공통 표시 함수 | 숫자 서식, 중량 문구 등 중복 계산 구현. 네트워크·UI 접근 없는 함수로 구성 |
| 각 View의 XAML | 색상, 굵기, 크기, 여백, 해상도별 배치와 Style·Trigger |
| 남기는 Converter | bool→Visibility, 실제 화면 너비에 따른 위치 계산 등 단순 변환과 View 전용 계산 |

적용 원칙:

- 화면마다 다른 색상과 굵기를 지정할 수 있어야 한다. 화면별 디자인 값을 표시 ViewModel의 Brush·Style 속성으로 옮기지 않는다.
- 기존 사용자 설정의 동적 색상 리소스와 사업장별 예외를 보존한다.
- 원본 가격에 쉼표를 저장하지 않는다. 통신·비교·캐시는 기존 원본 값을 사용한다.
- SV/API 파싱, 비동기 로그, 타이머와 Storyboard 구조는 이번 목적에 필요한 연결부 외에는 변경하지 않는다.
- 최근 원상복구한 NOTE·추가 이력의 내용, 결합 순서와 색상은 그대로 유지한다. 중복 생략을 다시 도입하지 않는다.
- 폴더 전체 개편, 패키지 업데이트, 원본 모델의 모든 표시 속성 일괄 제거는 포함하지 않는다.
- Converter 개수 감소나 속도 개선 비율을 사전에 목표 수치로 확정하지 않는다.

## 3. 공통 표시 ViewModel 연결안

신규 `ViewModels/AuctionItemDisplayViewModel.cs`를 공통 항목 표시 대상으로 둔다. 이름은 구현 시 기존 네이밍에 맞게 조정할 수 있다.

1. 현재 화면용 스냅샷을 받아 필요한 표시 값을 계산한다. 수신 원본을 직접 수정하지 않는다.
2. Running의 패널 ViewModel이 공통 항목 표시 ViewModel을 보유한다. 항목 영역의 DataContext를 이 객체로 연결하고, 진행 상태·명령·스크롤 제어는 기존 패널 ViewModel에 둔다.
3. Sold·UnSold도 같은 공통 항목 표시 ViewModel을 DataContext로 사용한다. View 재사용과 `LastData` 비교는 유지한다.
4. 화면에서 계속 사용하는 원본 항목은 필요한 읽기 전용 전달 속성으로 제공한다. 기존 원본 스냅샷은 이행 중 `Snapshot` 같은 명시적 경로로 접근한다. 원본 모델 상속이나 모든 속성의 무조건 복제는 피한다.
5. 기존 Converter가 `gValues` 전체를 받는 곳은 이행 중 명시적으로 스냅샷을 전달한다. 문자열 또는 새 ViewModel을 그대로 넘겨 변환에 실패하지 않도록 한다.
6. 공통 NoteHost와 code-behind의 `DataContext is gValues` 검사를 함께 점검한다. 필요한 연결만 수정하고 비고 표시·애니메이션의 기존 동작을 보존한다.

갱신은 기존 스냅샷 교체 방식에 맞춰 처리한다. 첫 구현은 입력 데이터 또는 표시 설정이 바뀌면 새 표시 ViewModel을 만들고 DataContext/소유 속성 변경을 통지하는 방식으로 단순화한다. 동일 입력은 기존 동등 비교로 걸러 불필요하게 재생성하지 않는다.

표시 설정에는 사업장별 표시 조건도 포함한다. 데이터가 같더라도 관련 설정이 바뀌면 표시 값을 다시 계산한다. 이행 중 가변 속성을 사용하는 구간은 해당 표시 속성의 `PropertyChanged` 알림을 빠뜨리지 않는다.

## 4. 최저가·낙찰가·중량 표시 규칙

### 4.1 가격

공통 표시 속성은 `LowestPriceText`, `BidPriceText`를 기본으로 한다. 두 속성은 같은 숫자 서식 함수를 사용하고, 상태에 따른 문구 선택은 별도로 처리한다.

```xml
<!-- 공통 항목 표시 ViewModel을 DataContext로 사용하는 영역 -->
<TextBlock Text="{Binding LowestPriceText}" Foreground="Yellow" />
<TextBlock Text="{Binding BidPriceText}" Foreground="Orange" />
```

| 입력 또는 조건 | 목표 표시 |
| --- | --- |
| `1234` | `1,234` |
| `612` | `612` |
| `0` | `0` |
| `-` | `-` |
| 이미 구분자가 있는 정상 숫자 | 중복 쉼표 없이 기존과 같은 숫자 표시 |
| `결장` | 일반 경로는 원문 유지. 기존에 `-` 치환을 사용한 경로만 동일하게 치환 |
| null·빈 값·숫자가 아닌 문자열 | 현재 호출 지점별 결과를 기준으로 보존. 임의로 0으로 만들지 않음 |

- 가격 단위와 소수 표시 정책은 바꾸지 않는다. 기존 `#,##0`의 반올림 결과까지 기준 사례로 확보한다.
- 현재 Converter가 시스템 문화권에 의존하는 점을 기록하고, 파싱과 출력에 사용할 문화권을 공통 함수에서 명시한다. 기본은 현재 실행 문화권을 전달하여 호환성을 유지한다.
- 최저가·낙찰가의 예외 문구가 다른 경우, 숫자 서식 함수에 업무용 문자열 파라미터를 계속 추가하지 않고 표시 속성에서 결정한다.
- 현재 `LowestPrice`는 문자열이므로 `StringFormat`만 붙여 대체하지 않는다. 이미 숫자 타입이고 예외 문구가 없는 별도 바인딩은 `StringFormat` 유지가 가능하다.
- 가격이 변경될 때 표시 속성도 갱신되는 것이 완료 조건이다. 최초 화면에 쉼표가 보이는 것만으로 완료하지 않는다.

### 4.2 중량

- 숫자만 표시하는 화면과 `kg`를 붙이는 화면을 구분한다. 필요한 경우 `WeightText`, `WeightWithUnitText`로 제공한다.
- 중량의 빈 값·공백·`0`·`-`에 단위를 붙이는 경로는 `-`로 통일한다. 이는 기존 `WeightKGConverter`의 빈 값 처리와 달라지는 의도적인 수정이다.
- 숫자만 표시하던 중량 화면의 `0`까지 일괄적으로 `-`로 바꾸지 않는다.
- 모든 중량 화면에 새롭게 쉼표나 단위를 강제하지 않는다. 기존 화면별 서식을 공통 함수의 명시적 선택으로 보존한다.

## 5. Converter별 처리 방향

아래는 목표 방향이다. 실제 삭제는 전체 참조 조사와 대체 바인딩 검증을 마친 뒤 수행한다.

| 기존 클래스 | 처리 방향 |
| --- | --- |
| `NumberFormatConverter` | 가격·중량 사용처를 공통 표시 속성으로 이행. 남는 범용 숫자 서식 사용처가 있으면 상태 치환과 분리한 최소 구현 유지 |
| `WeightKGConverter`, `ChuncheonWeightConverter` | 공통 중량 표시 함수로 통합 |
| `SelectShowWeight_EPDString`, `SelectShowWeightEpdMultiConverter`, `MokpoEPD` | `ShowWeight`, `ShowEpd` 등으로 이동. Y/N·품목·사업장 프로필별 기존 조건표 보존 |
| `PregnancyValueConverter` | `ShowPregnancy`로 이동. `X`, `미정`, 양수 개월의 기존 의미 보존 |
| `NhLabelConverter` | `NhLabel` 표시 속성으로 이동 |
| `MotherLevelConverter` | 필요한 표시 문자열 속성으로 이동. 해남진도의 `미등 → 미등록` 예외 보존 |
| `EntityNumberVisibilityConverter` | 소·염소 영역 표시 조건을 공통 표시 속성으로 이동. 개체번호 텍스트 분할 컨트롤은 유지 |
| `NH_EPD_ShowToVisibilityConverter` | 값 존재 여부를 표시 속성으로 이동. 현재 빈 값·`-`·반전 조건 보존 |
| `NhExcellentToVisibilityConverter` | 사용 중이면 A/Y 판정을 표시 속성으로 이동. 미사용이면 삭제 |
| `ShowMatherBloodConverter` | 사용 중이면 모혈통/자기혈통 표시 조건으로 이동. 미사용이면 삭제 |
| `PaternityMatchToVisibilityConverter`, `PaternityMatchToMarginConverter` | 미사용 여부 우선 확인. 사용 중이면 결과 존재와 일치 판정을 분리하고, 여백은 XAML Trigger로 처리 |
| `CowTypeToStyleConverter_128_64`, `CowTypeToStyleConverter_160_64`, `CowTypeToStyleConverter_V3`, `CowTypeToStyleConverter_OutLine_V3` | 화면별 XAML Style·Trigger로 이행. 성별 별칭과 기본 스타일의 기존 차이를 먼저 기록 |
| `BloodFontStyle`, `CowCowDist_NumToString` | 각 화면의 Style·Trigger로 이행. 기존 정적 Style 재사용보다 무조건 빨라진다고 가정하지 않음 |
| `BorderOutlineConverter` | 사용 확인 후 미사용이면 삭제. 사용 중이면 경매 결과·성별 조건과 화면별 Border 스타일로 분리 |
| `BidderLeftPositionConverter` | 실제 너비 기반의 View 전용 계산이므로 우선 유지 |

중량·EPD 조건은 `ShowEpd = !ShowWeight`로 단순화하지 않는다. 빈 값이나 알 수 없는 코드에서는 둘 다 숨겨지는 기존 경로가 있다. 친자 정보도 `HasPaternityResult`와 `IsPaternityMatched`처럼 의미를 구분하되, 기존 `HasPaternityMatch`를 이름만 바꾸는 작업은 피한다.

## 6. 단계별 구현과 완료 기준

| 단계 | 작업 | 완료 기준 |
| --- | --- | --- |
| 1. 기준 확보와 미사용 정리 | 23개 클래스의 XAML·리소스 사전·C# 참조와 ConverterParameter를 조사한다. 사업장·상태·해상도별 조건표를 만들고 확인된 미사용 선언부터 정리한다. | 삭제 대상에 실제 참조가 없고 기존 바인딩 검증이 유지됨 |
| 2. 공통 표시 기반과 가격 | 표시 ViewModel과 공통 서식 함수를 추가한다. 대표 Running·Sold·UnSold에 연결하고 최저가·낙찰가를 이행한다. | 쉼표·예외 문구·SV/API 가격 변경·동일 데이터 재수신이 모두 예상대로 동작함 |
| 3. 공통 문구와 표시 조건 | 중량, 임신, 농협 문구, 혈통 문구, 품목·EPD 조건을 이행한다. | 사업장별 조건표와 결과가 일치하고 빈 값에서도 잘못된 영역이 표시되지 않음 |
| 4. 화면별 스타일 | 성별·혈통·품목·테두리 스타일을 XAML로 이행하고 동적 색상과 해상도별 배치를 유지한다. | 색상·굵기·여백·배율이 기준과 일치하고 변환마다 Style을 생성하던 경로가 제거됨 |
| 5. 전체 화면 적용과 잔여 정리 | 모든 대상 화면과 공통 컨트롤·code-behind 연결을 점검한다. 대체 완료한 Converter와 리소스만 제거한다. | Running·Sold·UnSold에서 같은 표시 규칙을 공유하고 잘못된 DataContext 타입 가정이 없음 |
| 6. 회귀 검증과 기록 | 오프라인 검증, 빌드와 대표 화면 비교, 필요 시 성능 측정을 수행한다. | 검증 결과와 미확인 범위를 보고하고 구현 완료 후 WORKLOG에 기록함 |

단계 2에서 공통 연결을 먼저 검증한 후 다른 화면으로 넓힌다. 한 화면에서 원본 모델 경로와 표시 ViewModel 경로가 임시로 섞이는 경우 이행 목록에 남기고 단계 5에서 정리한다. 각 단계의 실패는 해당 단계 변경을 기준으로 좁혀 해결하며, 기존 미커밋 작업을 일괄 되돌리지 않는다.

## 7. 주요 변경 예상 파일

| 파일·영역 | 목적 |
| --- | --- |
| 신규 `ViewModels/AuctionItemDisplayViewModel.cs` | 세 경매 상태에서 공유하는 표시 데이터 제공 |
| 신규 `Presentation/Display/AuctionDisplayFormatter.cs` | 숫자·중량 공통 표시 함수. 필요한 사업장별 순수 조건 함수도 같은 표시 계층에서 관리 |
| `ViewModels/AuctionContPanelViewModel.cs` | 원본 스냅샷 갱신과 공통 표시 객체 연결 |
| `Presentation/Display/DisplaySelect.cs` | Sold·UnSold의 표시 객체 연결과 기존 View 재사용 유지 |
| `Presentation/Converters/*.cs`, `Presentation/Converters/Mokpo/MokpoEPD.cs` | 대체·통합·미사용 제거 |
| `Views/Size128_64`, `Views/SIze_160_64`, `Views/Size128_128`, `Views/Size_320_64`의 관련 XAML | 가격·문구·표시 조건 바인딩과 화면별 Style 변경 |
| `Views/Common`, NoteHost와 관련 code-behind | DataContext 타입 의존과 공통 표시 연결 보정 |
| `Validation/ReferenceAdoptionChecks`, `Validation/OwnerHistoryChecks` | 기존 바인딩·화면 전환·비고 스크롤 회귀 검증 및 필요한 표시 규칙 검증 |

원본 `gValues`에 UI용 캐시·Brush·Style을 추가하지 않는다. 도메인 모델의 타입·직렬화·동등 비교 계약을 바꾸는 확장은 이 계획에 포함하지 않는다. 신규 패키지나 별도 스레드는 필요하지 않다.

## 8. 검증 계획

### 표시와 갱신

- 가격: `612`, `1234`, `0`, `-`, null, 빈 문자열, 공백, 이미 쉼표가 있는 값, 소수, 비숫자 문구와 `결장` 치환 경로.
- 갱신: SV 최저가 변경, API 새로고침, 같은 데이터 재수신, 다른 경매품으로 교체, 사업장 표시 설정 변경.
- 조건: Y/N/빈 값, 소·염소 및 기타 품목, 목포 품목 그룹, 임신 X/미정/0/양수, 친자 일치/불일치/빈 값.
- 상태: 대기·진행·낙찰·유찰 전환, 같은 View 재사용, 반복 진입·종료 시 이전 품목 표시 잔류 여부.
- 화면: 128×64, 160×64, 128×128, 320×64의 실제 존재하는 상태 조합과 사업장별 사용자 화면.
- 표현: 동적 색상, 개체번호 강조, 성별 별칭·긴 문구의 배율, 소유주 이력 색상·순서, 비고 스크롤 유지.

### 도구와 실행 범위

- 현재 검증 프로젝트의 `binding`, `--scroll-offline` 실행 경로를 활용한다. 단순 구현 복제 테스트 대신 실제 바인딩 해석과 입력 변경 후 결과를 검사한다.
- 표시 조건과 가격 서식은 실제 서버 없이 합성 데이터로 검증한다. 테스트 파일에 운영 로그·개인정보를 복사하지 않는다.
- 구현 후 필요한 Debug 빌드와 오프라인 검증을 수행하고, 전체 적용 완료 시 Release 빌드도 확인한다. 기존 복원 자산이 유효하면 `--no-restore`를 사용한다.
- 실행 중인 앱이나 배포본과 충돌하지 않도록 검증 빌드 출력은 별도 임시 경로를 사용한다. 운영 연결이 필요한 실화면 검증은 오프라인 결과와 구분한다.
- 전후 성능 비교가 필요하면 동일 화면 수·입력·설정으로 UI 반영 시간과 할당량을 측정한다. 표시 속성 사전 계산에도 비용이 있으므로 측정 없이 개선률을 보고하지 않는다.
- 문서 작성 시점의 빌드·테스트·실화면 검증은 미수행이다. 실제 구현 결과를 완료 후 별도로 기록한다.

## 9. 최종 완료 조건

- [ ] 기존 Converter와 리소스의 사용·대체·유지 목록이 확정되어 있다.
- [ ] 최저가·낙찰가 서식 함수가 공유되고, 갱신 후 화면 문자열이 원본 가격과 일치한다.
- [ ] 세 경매 상태가 같은 표시 ViewModel을 사용하며 화면별 색상·굵기는 독립적으로 지정할 수 있다.
- [ ] 사업장별 표시 예외와 주요 해상도 배치가 보존되어 있다.
- [ ] 원본 데이터·캐시 계약과 NOTE 원상복구 결과가 유지되어 있다.
- [ ] 바인딩·조건·화면 수명 주기 회귀와 빌드 결과를 기록했다.
- [ ] 기존 사용자 변경과 이번 구현을 구분하여 보고하고 WORKLOG를 갱신했다.

# XAML TextBlock 줄이기 계획 (테스트: StandardQQuri_Run1 / Standard_non_QQuri_Run1)

## 목표
- 디자인(레이아웃/색상/가독성)은 유지하면서 TextBlock 수를 최대한 줄인다.
- 컨버터 동작은 유지한다.
- 변경 범위는 두 파일로 한정한다.

## 원칙
- 색상이 다른 경우: 한 TextBlock 안에 `Run`을 여러 개 넣어 색만 분리한다.
- 색상이 동일한 경우: `StringFormat`/`MultiBinding`으로 하나의 TextBlock로 합친다.
- 위치가 다른 경우(명확히 떨어진 좌표): 그대로 분리한다.
- 컨버터는 `Run.Text`에 그대로 적용 가능하므로 유지한다.

---

## Standard_non_QQuri_Run1.xaml 계획

### 1) Location + OwnerName
- 현재: TextBlock 2개
  - Location (보라), OwnerName (시안)
- 변경: 1개 TextBlock + 2 Runs
  - Run1 = Location (보라)
  - Run2 = " " + OwnerName (시안)

### 2) "최저" 라인
- 현재: 라벨 2개 + 값 1개 (총 3개)
  - "최저", ":", LowestPrice
- 변경: 1개 TextBlock + 2 Runs
  - Run1 = "최저 : " (lime)
  - Run2 = LowestPrice (Converter 유지, Yellow)

### 3) "육종" 라인
- 현재: 라벨 2개 + 값 4개 (총 6개)
- 변경: 1개 TextBlock + 6 Runs
  - Run1 = "육종 : " (lime)
  - Run2 = bodyWeightInColdString (Silver Bold)
  - Run3 = " " + longestMuscleCrossSectionString
  - Run4 = " " + fatThicknessOnBackString
  - Run5 = " " + intramuscularFatContentString
- 결과: 같은 색/굵기 유지, 텍스트 수만 줄임

### 4) "임신" 라인 (Canvas 내부)
- 현재: 라벨 2개 + 값 1개 (총 3개)
- 변경: 1개 TextBlock + 2 Runs
  - Run1 = "임신 : " (lime)
  - Run2 = CowInfo.Pregnant (Yellow)

### 5) "중량" 라인
- 현재: 라벨 2개 + 값 1개 (총 3개)
- 변경: 1개 TextBlock + 2 Runs
  - Run1 = "중량 : " (lime)
  - Run2 = CowInfo.Weight (Yellow)

### 6) "계대" 라인
- 현재: 라벨 2개 + 값 1개 (총 3개)
- 변경: 1개 TextBlock + 2 Runs
  - Run1 = "계대 : " (lime)
  - Run2 = CowInfo.Blood (Orange)

### 7) "KPN" 라인
- 현재: 라벨 2개 + 값 1개 (총 3개)
- 변경: 1개 TextBlock + 2 Runs
  - Run1 = "KPN : " (lime)
  - Run2 = CowInfo.KPN (Orange)

### 8) "산차" 라인
- 현재: 라벨 2개 + 값 1개 (총 3개)
- 변경: 1개 TextBlock + 2 Runs
  - Run1 = "산차 : " (lime)
  - Run2 = CowInfo.CalvingNumber (Orange)

### 유지하는 영역
- CowDistinction/RegistrationCategory/EntityNumber 등은 좌표가 분리되어 있으므로 유지.
- 컨버터(예: MotherLevelConverter, BloolFontStyle)는 그대로 유지.

---

## StandardQQuri_Run1.xaml 계획

### 1) Location + OwnerName
- Standard_non_QQuri_Run1과 동일: 2개 → 1개 TextBlock + Runs

### 2) "최저" 라인
- 동일: 3개 → 1개 TextBlock + Runs (NumberFormatConverter 유지)

### 3) "육종" 라인 (Nh_ability_1_str 없을 때 영역)
- 현재: 라벨 2개 + 값 4개 (총 6개)
- 변경: 1개 TextBlock + Runs (색/굵기 동일)

### 4) "임신" 라인 (Canvas 내부)
- 동일: 3개 → 1개 TextBlock + Runs

### 5) "중량" 라인
- 동일: 3개 → 1개 TextBlock + Runs

### 6) "계대" 라인
- 동일: 3개 → 1개 TextBlock + Runs

### 7) "KPN" 라인
- 동일: 3개 → 1개 TextBlock + Runs

### 8) "산차" 라인
- 동일: 3개 → 1개 TextBlock + Runs

### 유지하는 영역
- Nh_ability_1~4 줄은 각 TextBlock마다 개별 마진/스타일 트리거가 있으므로 유지.
- 스트링/색/굵기 조합이 복잡한 영역은 Run 병합 대신 유지.

---

## 예상 감소량(대략)
- Standard_non_QQuri_Run1: 약 20~25개 → 약 10~12개 수준
- StandardQQuri_Run1: 약 22~28개 → 약 12~15개 수준
(정확한 수치는 적용 후 확인)

## 주의사항
- Run에 바인딩할 때 컨버터 사용 가능.
- Run 사이 간격은 " " 문자열로 조절.
- TextBlock 하나로 합칠 때는 Margin/정렬이 기존과 같도록 유지.


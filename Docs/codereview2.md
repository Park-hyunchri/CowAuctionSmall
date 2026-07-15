# 코드 리뷰 2 - GPU/RAM 사용량 원인 분석

## 검토 범위
- `Docs/codereview.md`
- `Docs/graphics-note-performance.md`
- `Docs/xamltext.md`
- 문서에 언급된 소스 일부 확인

## 주요 발견 사항 (심각도 순)

### 1) 보드당 TextBlock 수 과다로 Visual Tree가 비대 (Medium-High)
- 근거 문서: `Docs/xamltext.md:1`, `Docs/xamltext.md:111`
- 내용: 보드당 TextBlock이 20~28개 수준으로, 500+ 보드에서 1만+ UI 요소가 생성됩니다.
- 영향: 메모리 사용량 증가, Measure/Arrange/렌더 비용 상승으로 GPU 사용량도 동반 증가.
- 개선: Run 병합 및 MultiBinding으로 TextBlock 수를 절반 수준으로 축소.

### 2) UI 로그 문자열 무제한 누적 (Medium)
- 근거 문서: `Docs/codereview.md:111`, `Docs/codereview.md:112`
- 근거 코드: `ViewModels/MainWindowViewModel.cs:96`, `ViewModels/MainWindowViewModel.cs:417`, `ViewModels/MainWindowViewModel.cs:439`
- 내용: `MainWindowTextBox`에 계속 `+=`로 누적.
- 영향: 장시간 실행 시 RAM 사용량이 지속 증가하고 TextBox 렌더 비용 상승.
- 개선: 로그 라인 수 제한, 순환 버퍼 또는 파일 로그로 분리.

### 3) 가상화 적용 효과 제한 가능성 (Low-Medium)
- 근거 문서: `Docs/codereview.md:44`
- 근거 코드: `Services/DisplaySelect.cs:518`
- 내용: `VirtualizingPanel.SetIsVirtualizing`가 적용되더라도 컨테이너 구조가 ItemsControl 기반이 아니면 효과가 제한됩니다.
- 영향: 보드 전체가 시각 트리에 상주해 메모리 사용량이 증가.
- 개선: 컨테이너 구조 점검 후 실제 가상화 적용 여부 확인.

### 4) 페이지 전환마다 Children.Clear로 언로드/로드 반복 (Low-Medium)
- 근거 코드: `Services/DisplaySelect.cs:507`
- 내용: 페이지 전환 시 `panel.Children.Clear()` 후 다시 추가하여 Loaded/Unloaded가 반복됩니다.
- 영향: 페이지 전환이 잦을 때 레이아웃/초기화 비용 증가.
- 개선: `ContentPresenter`로 교체하거나 페이지를 유지한 채 `Visibility` 토글로 전환.

## 조치된 항목
- `Services/FlowTextAnimation.cs` 생성자 기본값을 `useRenderTransform = true`로 전환.
- `Services/FlowTextAnimation.cs`에 `IsVisibleChanged` 기반 Stop/Resume 추가.
- `Services/FlowTextAnimation.cs`에서 페이지별 위치 저장을 공유 키로 통합해 페이지 전환 시 동일 위치 이어짐.
- Spark 타이머와 `IsSparkOn` 바인딩 제거: `ViewModels/AuctionContPanelViewModel.cs`, `Views/Size128_128/Running/StandardQQuri_Run1.xaml`.
- `ViewModels/AuctionContPanelViewModel.cs`의 `NotePosition` 변경 알림 제거.
- `Services/FlowTextAnimation.cs`에서 짧은 텍스트는 Measure/Arrange 생략.
- `FLOWTEXT_DIAG=1` 환경 변수로 활성 애니메이션 수/평균 Tick 시간 로그 지원.

## 추가 확인 질문
- 페이지가 숨겨진 상태에서도 노트 스크롤이 계속 동작하나요?
- 스크롤을 끈 상태에서 GPU 3D 사용률이 유의미하게 내려가나요?

## 테스트/검증 제안
- 스크롤 OFF/RenderTransform ON/텍스트 병합 적용 후 GPU 3D 및 RAM 사용량 비교.
- 500+ 보드 로딩 시 시각 트리 요소 수와 GC/메모리 스냅샷 비교.

## 최적화 로드맵(현재 방향 유지)
- 기준 측정: 500+ 보드 기준 GPU 3D/RAM/UI 스레드 프레임 타임/활성 애니메이션 수 산출(FLOWTEXT_DIAG=1 + 작업 관리자).
- Visual Tree 축소: `Docs/xamltext.md` 기준으로 TextBlock 병합 적용.
- 페이지 전환 비용 감소: `Children.Clear()` 반복 제거, `ContentPresenter`+`Visibility` 전환 구조 검토.

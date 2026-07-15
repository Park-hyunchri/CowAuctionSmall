# NoteMargin 동작 분석 및 설명

## 1. 문제 요약

'비고' 내용이 수정될 때, '친자일치'(`borderPaternityMatch`)가 표시됨에도 불구하고 `NoteMargin`이 `32, 109, 0, 1`이 아닌 `2, 109, 0, 1`에서 시작하는 것으로 보이는 현상에 대한 분석입니다.

## 2. 분석 결과

결론부터 말하자면, **`NoteMargin`은 '비고' 내용과는 직접적인 관련이 없습니다.**

`NoteMargin` 값은 **'친자일치 여부'** 에 따라서만 결정됩니다. 사용자가 '비고' 내용이 변경될 때 마진이 바뀐다고 인지한 이유는, 새로운 개체 데이터가 화면에 표시될 때 '비고'와 '친자일치' 정보가 **동시에 업데이트**되기 때문입니다.

현재 코드는 의도된 대로 정상 동작하고 있습니다.

## 3. 상세 설명

### 가. 핵심 로직: `AuctionContPanelViewModel.cs`

모든 로직은 `ViewModels/AuctionContPanelViewModel.cs` 파일에 구현되어 있습니다. 별도의 `Converter`를 사용하지 않고 ViewModel에서 직접 UI 관련 값을 제어하는 깔끔한 MVVM 패턴을 따르고 있습니다.

- **`HasPaternityMatch` 속성:**
  현재 경매 개체(`CowInfo`)의 `PaternityMatch`(`친자검사결과여부`) 속성 값이 유효한지(`null`, `""`, `"-"` 이 아닌지) 검사하여 `true` 또는 `false`를 반환합니다.

  ```csharp
  // ViewModels/AuctionContPanelViewModel.cs
  public bool HasPaternityMatch
  {
      get
      {
          if (CowInfo == null || string.IsNullOrEmpty(CowInfo.PaternityMatch) || CowInfo.PaternityMatch.Trim() == "-")
          {
              return false;
          }
          return true;
      }
  }
  ```

- **`NoteMargin` 속성:**
  `HasPaternityMatch` 속성 값에 따라 `Thickness` 객체를 반환합니다.
  - **친자일치 O (`HasPaternityMatch = true`):** 왼쪽 마진 `32`
  - **친자일치 X (`HasPaternityMatch = false`):** 왼쪽 마진 `2`

  ```csharp
  // ViewModels/AuctionContPanelViewModel.cs
  public Thickness NoteMargin
  {
      get
      {
          if (HasPaternityMatch)
          {
              return new Thickness(32, 109, 0, 1);
          }
          else
          {
              return new Thickness(2, 109, 0, 1);
          }
      }
  }
  ```

### 나. XAML 바인딩

`Views/Size128_128/Running/AuctionRunning2.xaml`와 같은 XAML 파일에서는 아래와 같이 ViewModel의 속성을 바인딩하여 사용합니다.

- **친자일치 뱃지:** `borderPaternityMatch`의 `Visibility`는 `HasPaternityMatch`에 바인딩되어 있어 친자일치 여부에 따라 자동으로 보이거나 사라집니다.
- **비고란 마진:** 비고 `Canvas`의 `Margin`은 `NoteMargin` 속성에 바인딩되어 있어 친자일치 여부에 따라 왼쪽 간격이 동적으로 조절됩니다.

```xml
<!-- Views/Size128_128/Running/AuctionRunning2.xaml -->

<!-- 친자일치 뱃지 -->
<Border x:Name="borderPaternityMatch"
        Visibility="{Binding HasPaternityMatch, Converter={StaticResource BoolToVis}, FallbackValue=Collapsed}" ...>
    ...
</Border>

<!-- 비고 텍스트 -->
<Canvas Margin="{Binding NoteMargin, FallbackValue='2,109,0,1'}" ...>
    <TextBlock Text="{Binding CowInfo.Note}" ... />
</Canvas>
```

### 다. 데이터 모델: `gValues.cs`

`Models/Structures/gValues.cs` 파일의 `gValues` 클래스를 보면, `PaternityMatch`와 `Note`는 완전히 별개의 속성임을 확인할 수 있습니다.

```csharp
// Models/Structures/gValues.cs
public class gValues
{
    // ... 다른 속성들
    public string PaternityMatch { get; set; } // 친자검사결과여부
    public string Note { get; set; } // 비고
    // ...
}
```

## 4. 해결 방안

현재 시스템은 설계 의도대로 정상적으로 동작하고 있으므로 **별도의 코드 수정은 필요하지 않습니다.** `NoteMargin`은 '비고' 내용이 아닌 '친자일치' 여부에 따라 결정되는 것이 올바른 동작입니다.

# Note text performance (500+ boards)

## Context
- The note ("bigo") text is shown in many 128x128 boards.
- For long notes, each board starts a marquee using `FlowTextAnimation`.
- With 500+ boards on a Ryzen 2nd-gen APU (Vega 8), the note text looks choppy.

## Current behavior
- Each note TextBlock is inside a Canvas with `ClipToBounds="True"`.
- Code-behind measures and then starts `FlowTextAnimation` on load.
- `FlowTextAnimation` updates `Canvas.Left` every ~50ms for all active notes.
- Updating `Canvas.Left` triggers layout each frame. With many notes, layout churn is a bottleneck.

## Main cause of jank
- Per-frame layout invalidation (Canvas.Left) across many active note animations.
- Many notes can scroll at the same time, so the UI thread spends too much time in Arrange/Measure.

## Recommended change (smooth, low-risk)
### 1) Move the note with RenderTransform (no layout)
Update `FlowTextAnimation` to move the note with a `TranslateTransform` instead of `Canvas.SetLeft`.
This keeps layout stable and only re-renders the visual.

Example idea (concept only):
```csharp
// one-time setup per TextBlock
if (_textBlock.RenderTransform is not TranslateTransform tt)
{
    tt = new TranslateTransform();
    _textBlock.RenderTransform = tt;
}

// per frame
tt.X = _notePosition; // or viewModel.NotePosition
```
Notes:
- Keep Canvas for clipping (ClipToBounds stays).
- Set `HorizontalAlignment="Left"` on the note TextBlock so the transform is predictable.

### 2) Animate only when needed
- Keep the "note length" check, but skip `Measure/Arrange` if the text is short:
  - First check `note.Text.Length` and return early before measuring.
- Only start scrolling for:
  - Running boards, or
  - Boards on the current visible page.

### 3) Trim by default, scroll only on demand
For a more natural look and less motion:
- Use `TextTrimming="CharacterEllipsis"` and `TextWrapping="NoWrap"` on note TextBlocks.
- Show the full note only when:
  - The board is "active" (running/selected), or
  - On hover (tooltip or overlay).

This removes constant marquee motion and improves perceived smoothness.

## Optional low-end mode (Vega 8 safe)
Add a config flag to disable scrolling globally:
- When enabled, notes always use ellipsis.
- This makes the UI stable on low GPU/CPU setups.

## Pilot scope (test files)
Start with these 128x128 running boards only:
- `Views/Size128_128/Running/StandardQQuri_Run1.xaml`
- `Views/Size128_128/Running/StandardQQuri_Run1.xaml.cs`
- `Views/Size128_128/Running/Standard_non_QQuri_Run1.xaml`
- `Views/Size128_128/Running/Standard_non_QQuri_Run1.xaml.cs`
- `Views/Size128_128/Running/AuctionRunning2.xaml`
- `Views/Size128_128/Running/AuctionRunning2.xaml.cs`
- `Views/Size128_128/Running/AuctionRunning3.xaml`
- `Views/Size128_128/Running/AuctionRunning3.xaml.cs`

## Target files
- `Services/FlowTextAnimation.cs`
- `Views/.../*.xaml.cs` (note load/start logic)
- `Views/.../*.xaml` (note TextBlock settings)

## Quick checklist
- [ ] Use RenderTransform instead of Canvas.Left for note movement.
- [ ] Skip measure for short notes.
- [ ] Limit active scrolling notes.
- [ ] Add a no-scroll / ellipsis mode for low-end PCs.

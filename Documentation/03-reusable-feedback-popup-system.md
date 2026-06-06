# /goal 3 — Reusable Feedback Popup System

```text
/goal Add a reusable feedback popup system for money and life changes.

The game needs clear visual feedback when gold is spent and when lives are lost.

Focus only on creating a reusable popup system and connecting it to existing placement/life-loss events.

Tasks:
- Create a reusable floating popup system.
- Support money spent feedback such as “-50 gold” when placing a tower.
- Support life lost feedback such as “-1 life” when enemies escape.
- Allow popup colour, text, duration, and movement to be configurable.
- Make popups appear near the relevant world/UI event.
- Add optional simple pop animation/fade.
- Keep the system reusable for future score, damage, reward, or warning popups.
- Connect tower placement to money spent feedback.
- Connect end-of-path enemy escape to lives lost feedback.

Do not rebalance money, lives, waves, or enemy stats in this goal unless a tiny connection change is required.

Acceptance criteria:
- Spending gold shows a floating negative money popup.
- Losing lives shows a floating negative life popup.
- The system is reusable and not hardcoded to only one event.
- Existing HUD lives/gold displays still update correctly.
```

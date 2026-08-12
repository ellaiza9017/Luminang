# 🍢 Tusok-Tusok (Counting Minigame) Design & Architecture

## Overview
**Tusok-Tusok** is a counting and speech-recognition (STT) minigame designed for the Lesson 7 (Count) category in Luminang. The player assumes a first-person POV in front of a street food cart and must interact with various types of street food to satisfy customer/vendor orders.

## Core Gameplay Loop
1. **The Order:** Kuya Tindero asks for a specific combination of street food. For example: "Can you get *dua* a fishball, ket *tallo* a kwekkwek?" (2 fishballs, 3 kwek-kwek).
2. **The Wok (Selection):** A boiling wok UI contains different clickable food items: Fishballs, Kwek-kwek, Kikiam, and Red Hotdogs.
3. **The Stick (Stacking):** 
   - The player holds a barbecue stick on screen.
   - Every time the player taps a food item in the wok, a sprite of that food item spawns at the top of the stick.
   - As more items are tapped, the older items slide further down the stick to make room.
4. **Validation:** The player presses an "Enter" or "Submit" button when they think their stick is ready.
   - The game counts the total occurrences of each food type on the stick.
   - **Order Does Not Matter:** If they need 2 fishballs and 3 kwek-kweks, they can skewer them in any order (e.g., F-K-K-F-K).
5. **Win/Loss States:**
   - **Incorrect:** If the totals do not match the required order, Kuya shakes his head, the player loses a life, and they must try again.
   - **Correct:** The stick moves to the center of the screen and scales up. The target number words (e.g., "dua tallo") appear.
6. **STT Phase:** The microphone activates. The player must speak the number words exactly as prompted to complete the transaction.

---

## Technical Architecture

### 1. Prefabs Needed
*   **WokItem:** A simple button/clickable sprite representing the food floating in the oil. It needs an ID (e.g., `Type = Fishball`).
*   **StickSlot:** The visual representation of the food on the barbecue stick. 
*   **BarbecueStickUI:** A container (likely using a `VerticalLayoutGroup` with `Reverse Arrangement` or custom local moving logic) that visually stacks the `StickSlot` prefabs as they are added.

### 2. Core Scripts
*   **`TusokGameManager.cs`**: 
    - Manages the round index, lives, and checks win/loss conditions when the Enter button is pressed.
    - Validates if the count of items in `TusokStickManager` matches the current `RoundData`.
*   **`TusokStickManager.cs`**:
    - Handles the logic for spawning food sprites onto the stick UI.
    - Manages the visual sliding animation (pushing older items down).
    - Can calculate the current totals (`int currentFishballs, currentKwekKwek, etc.`).
*   **`TusokWokManager.cs`**:
    - Handles the spawning or static layout of the clickable food buttons in the wok.
*   **`TusokSTTManager.cs`**:
    - Triggers the STT listening phase once the stick validation is successful.

### 3. Data Structure (JSON)
The game will rely on a JSON file (e.g., `CountingRounds.json`) containing objects like:
```json
{
  "dialogueText": "Get dua a fishball, ket tallo a kwekkwek",
  "targetFishball": 2,
  "targetKwekKwek": 3,
  "targetKikiam": 0,
  "targetHotdog": 0,
  "sttTargetText": "dua tallo"
}
```

---

## Edge Cases & Rules
- **Over-skewering:** The stick should have a maximum capacity (e.g., 6 or 7 items). If the player tries to click more than the stick can hold, the game should prevent it or play an "error" sound.
- **Clearing the stick:** If a player makes a mistake while skewering, there should be a "Clear/Reset Stick" button so they can start over without losing a life, OR they can tap an item on the stick to remove it. (To be determined based on UX preference).

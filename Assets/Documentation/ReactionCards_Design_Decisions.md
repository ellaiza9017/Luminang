# 🃏 Reaction Cards (L2) — Design Decisions Log

> This document captures all design decisions for the Lesson 2 (Expressions of Gratitude) Minigame.
> Reference this before implementing or changing anything in the Reaction Cards game.

---

## 🎮 Game Concept: "Ano Ang Sasabihin Mo?"

**Theme:** A cozy Filipino desk setup (wooden table, capiz shell lamp, cup of kapeng barako). You are reviewing memories/situations via "Polaroid Cards" scattered on the desk.
**Goal:** Match the correct regional response to highly specific cultural situations, focusing on differentiating similar words (e.g., "thank you" vs "thank you very much").

---

## 🃏 Reaction Cards — Design Decisions

### Input Method
- A large **Situation Card** (looks like a Polaroid photo) appears in the center of the screen.
- 3 smaller **Vocabulary Cards** appear at the bottom.
- Player taps a Vocabulary Card to select their answer.
- The selected card slides up to pair with the Situation Card.

### Data Source
- **Offline / Local JSON** 
- Uses `Assets/Resources/LuminangPhrases.json` for word translations.
- Uses `Assets/Data/Minigames/ReactionCards/Gratitude.json` to define rounds.
  - Each round defines: 
    - `situationText`: "Someone gave you a very beautiful and expensive gift, you should express extreme gratitude."
    - `correctPhraseId`: "thank_you_very_much"
    - `distractors`: A list of wrong/partially correct phrases, each with custom feedback. 
      - *Example Distractor:* "thank_you" -> Feedback: "Hmm... you're almost correct, but we're expressing *extreme* gratitude. What do you think that is?"

### Lives System (5 Strikes)
- The player starts with **5 Hearts**.
- The game consists of **15 Rounds** (10 Gratitude situations + 5 Recall situations).
- If they tap a wrong Vocabulary Card, they lose 1 Heart (1 mistake).
- If they lose all 5 Hearts before 15 rounds are completed → game ends early.

### Wrong Answer Behavior
- Screen/Card **shakes**, red cross flashes.
- **1 Heart is lost** (HUD updates).
- **Specific Feedback Popup:** If the player picks a word that is "almost right" (like picking "Thank you" instead of "Thank you very much"), a gentle feedback bubble appears with the custom text from the JSON to guide them on *why* it wasn't the best fit.
- The wrong card grays out, player must pick from the remaining cards.

### Correct Answer Behavior
- The selected Vocabulary Card glows green and snaps perfectly under the Situation Card.
- A **mic button** appears.
- Player must say the word/phrase via STT before proceeding to the next round.

### STT (Speech-to-Text) Rules
- Player has **3 tries** to say the word correctly.
- Pass threshold: **80% accuracy** (from PhraseEvaluator).
- **Try 1 fail** → "Not quite! Try again."
- **Try 2 fail** → "Almost there! One more try."
- **Try 3 fail** → "Nice try! Keep practicing."
- After 3 fails: The player doesn't lose a heart, but they get 0 stars for that specific round, and the game moves to the next card.
- STT uses existing: `SpeechRecorder` → `GroqWhisperManager` → `PhraseEvaluator.EvaluateSpeech()`

### Recall Round Rule (L1)
- **5 of the 15 rounds** will randomly pull a **Greeting (L1)** situation instead of a Gratitude situation. 
- *Example:* "You wake up and see your lola. Greet her!" -> Player must pick "Good morning".

### Win Condition
- Complete all **15 rounds** without losing all 5 Hearts.
- Minimum to pass: **10/15** correct rounds.

---

## ⭐ Star & Coin Reward System

Stars are based on **how many rounds completed perfectly on the first try** (correct tap + STT passed):

| Correct Rounds | Stars | Coins Earned |
|---|---|---|
| 14–15 / 15 | ⭐⭐⭐⭐⭐ | 100% of L2 coins |
| 11–13 / 15 | ⭐⭐⭐⭐ | 80% |
| 8–10 / 15 | ⭐⭐⭐ | 60% |
| 5–7 / 15 | ⭐⭐ | 40% |
| 1–4 / 15 | ⭐ | 20% |
| 0 / 15 | — | 5% (consolation) |

Coin amounts come from `LessonsData.json` → `rewards.coins` for each lesson.

---

## 🗂️ File Structure (To Be Created)

```
Assets/
  Data/
    Minigames/ReactionCards/
      Gratitude.json                      ← Round data for L2
  Documentation/
    ReactionCards_Design_Decisions.md     ← this file
  Scripts/
    UI/Minigames/ReactionCards/
      ReactionCardsManager.cs             ← Core loop and lives
      SituationCardUI.cs                  ← Handles the polaroid UI
      VocabCardUI.cs                      ← Handles the 3 clickable cards
      ReactionSTTPanel.cs                 ← Handles mic popup
```

---

## 🛑 Pause Menu

A pause button (top-left corner of screen) opens an overlay with:
- ▶ **Resume** — close pause menu, continue game
- 🔁 **Restart** — restart from Round 1, reset Hearts to 3
- 🏠 **Quit to Map** — return to world map via `SceneNavigationManager`

---

## 👤 Player Character Rendering

Instead of a full 3D body, we use the same hybrid technique from the Fishing Game to keep the player connected to their customization:
- A **small ornate hand mirror** sits on the desk.
- The player's customized 3D head is rendered inside this mirror using a `RenderTexture` (from a secondary camera).
- When the player gets an answer right, the head in the mirror smiles!

---

## 📐 Scene Layout

```
┌─────────────────────────────────────────────┐
│ [Pause]                           [❤❤❤❤❤ Lives]│
├─────────────────────────────────────────────┤
│      ~~~ Wooden Desk Background ~~~         │
│                                             │
│       ┌───────────────────────────┐         │
│       │                           │         │
│       │  "You dropped your wallet │         │
│       │   and someone returned it.│         │
│       │   Express extreme         │         │
│       │   gratitude!"             │         │
│       └───────────────────────────┘         │
│                [🎙️ Mic]                    │
│                                             │
│  [Mirror]                                   │
│  [w/Head]  ┌───────┐  ┌───────┐  ┌───────┐  │
│            │ Vocab │  │ Vocab │  │ Vocab │  │
│            │Card 1 │  │Card 2 │  │Card 3 │  │
│            └───────┘  └───────┘  └───────┘  │
└─────────────────────────────────────────────┘
```

---

*Updated: August 2026*
*Next step: Build the scripts in `Assets/Scripts/UI/Minigames/ReactionCards/`*

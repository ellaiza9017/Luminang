# 🏛️ Final Assessment — Design Decisions

## 1. Core Concept
The final assessment is a culminating exam designed for Grade 6 students (approx. 11-12 years old) to test their mastery of all 90 contextual phrases across 11 categories. 

Instead of a typical test, it is framed as an immersive journey: **"Meet Tiptip at Magellan's Cross"** (Cebuano) or **"Meet Kalaw at Calle Crisologo"** (Ilokano).

## 2. Visual Theme & Skins
The assessment uses a **"One Scene, Two Skins"** approach. The scene layout, UI, and logic remain identical, but the visual assets swap based on the selected language (`SelectedLanguage` PlayerPref).

| Element | Ilokano Version | Cebuano Version |
|---|---|---|
| **Setting (Background)** | Calle Crisologo, Vigan | Magellan's Cross, Cebu |
| **NPC Character** | Kalaw | Tiptip |
| **Question Data** | FinalAssessment_Ilokano.json | FinalAssessment_Cebuano.json |

*Note: Background art and situational question images will be generated later.*

## 3. Narrative Intro (Story Tie-in)
Before the quiz begins, the player is greeted by the region's NPC. This ties the final assessment directly to the game's prologue.

- **The Great Fading Context:** The NPC reminds the player about their journey to restore the fading light of the Language Crystals using their anting-anting.
- **Example Dialog (Ilokano/Kalaw):** *"You have come a long way, traveler! The Great Fading tried to silence our voices, but your anting-anting glows brighter than ever. Let's see if you can restore the final light to the Ilokano Language Crystal!"*
- **Example Dialog (Cebuano/Tiptip):** *"Wow, look at how much you've learned! The Cebuano Language Crystal is almost fully restored. Show me what you know and let's keep our stories alive!"*

After a few short dialogue boxes, the assessment begins.

## 4. Game Flow & Structure
To avoid overwhelming the students, the assessment does not force them to answer all 90 questions in one sitting. Instead, each playthrough **randomly selects exactly 50 questions** from the total pool.

The 50 questions are divided into **3 Thematic Sections**:

1. **Conversational & Social:** 15 questions randomly pulled from the 22-question pool (Greetings, Gratitude, Responses, Identity)
2. **Functional & Navigational:** 15 questions randomly pulled from the 25-question pool (Requests, Directions, Count)
3. **Grammatical Foundations:** 20 questions randomly pulled from the 43-question pool (Action Verbs, Linking Verbs, Pronouns, Interrogatives)

### Flow Rules:
- **No Mid-Quiz Scoring:** The player does not see their score or a percentage during the quiz. This reduces anxiety.
- **Progress Tracking:** A simple progress indicator (e.g., "Question 23 of 50") is shown.
- **Section Transitions:** Between sections, a brief transition screen appears (e.g., "Section 1 Complete! Moving on...").
- **Live NPC Reactions:** The NPC (Kalaw or Tiptip) reacts immediately to answers (cheering for correct, encouraging for incorrect).

## 5. Question Types & Mechanics
Each of the 90 phrases will be tested using one of four question types. The specific type for each phrase is **randomly assigned at runtime** from a pool of compatible types based on the phrase's length and complexity. This ensures high replayability.

1. **Multiple Choice (MC):** Pick the correct phrase from 3 options based on a situational image or prompt. Best for short phrases or context-heavy situations.
2. **Fill in the Blank (FIB):** A sentence is shown with a missing word. The player selects the correct word from 4 tiles. Best for grammar and vocabulary.
3. **Speak to Text (STT):** The player uses the microphone to speak the phrase aloud. They get up to 3 attempts. Best for short phrases, numbers, and core verbs.
4. **Sentence Builder (SB):** Scrambled word tiles are presented at the bottom. The player drags and drops them into the correct order. Best for longer, multi-word phrases.

## 6. Scoring & Rewards
Since this is a final assessment, the scoring system is modeled after modern language apps (like Duolingo) rather than a strict pass/fail test.

- **No Lives/Hearts:** The player cannot "game over". They complete all 50 questions regardless of mistakes.
- **Scoring:** 
  - Correct Answer = 1 point
  - Incorrect Answer = 0 points
  - STT Failed (after 3 tries) = 0.5 points (partial credit for trying)
- **Final Results Screen:** Shown only at the end. It displays:
  - Total Score (Percentage)
  - Star Rating (1-5 stars based on percentage)
  - Category Breakdown (showing strengths and weaknesses)
  - Coin Reward Badge
- **Coin Rewards:** Distributed based on the final percentage:
  - 90–100%: 150 coins
  - 75–89%: 100 coins
  - 60–74%: 60 coins
  - 40–59%: 30 coins
  - Below 40%: 10 coins (consolation to encourage retrying)

## 7. Technical Implementation Plan
- **Single Scene:** `FinalAssessmentScene` handles both languages.
- **Manager Script:** `AssessmentManager.cs` orchestrates the flow, loads the correct JSON based on language, and manages the UI panels.
- **Question Panels:** Four distinct UI panels (MC, FIB, STT, SB) exist in the scene. Only the relevant one is activated for the current question.
- **Backend:** Uses the existing `SpeechRecorder` and `GroqWhisperManager` for STT questions. Scores are saved to Supabase upon completion.

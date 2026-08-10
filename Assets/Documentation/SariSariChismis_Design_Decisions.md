# Sari-Sari Chismis (Identity Expressions) Minigame Design Decisions

## 1. Core Gameplay & Mechanic
* **Theme:** **"POV Sari-Sari Store"**. You are looking at a traditional Sari-Sari store in a First-Person (POV) perspective. 
* **The "Tindera" (Vendor):** The Tindera is standing inside the store behind the counter (center of the screen). She serves as the main source of the prompt messages (e.g., asking "What is your name?").
* **The "Tambays" (Bystanders):** To make the scene feel alive and culturally accurate, "Tambay" NPCs will occasionally peep in or show up on the left and right sides of the screen to ask nosy questions (e.g., "Where are you from?").
* **Interaction:** **Drag and Drop Sentence Builder**. The player answers these questions by building a sentence. Empty dotted slots appear in a speech bubble in front of the player. The player must drag scattered `WordBlocks` (which look like cutout text or thought fragments) into the empty slots to construct the sentence grammatically.

## 2. Dynamic Data & Template Handling
* **Utilizing `LuminangPhrases.json`:** The game reads the `"ilokano_required_tokens"` array to dynamically generate the draggable `WordBlocks` and empty slots.
* **The Input Block (`{name}` / `{place}`):** The game spawns a special `[ Type Name... ]` block. The player drags this block into the slot. Once dropped, an `InputField` popup automatically appears allowing the player to type any name they want. This is **not** saved to PlayerPrefs, allowing them to practice with different names every time.

### 3. "Confirm STT" Mechanic & Dynamic Feedback
- **The Interaction:** Once all slots in the speech bubble are filled correctly, the STT Microphone activates.
- **The Microphone:** The player must **speak the complete sentence out loud** to officially reply to the Tindera or Tambay.
- **Dynamic Wrong Feedback:** If the player drops the wrong words or puts them in the wrong order, the game will read the blocks they placed and dynamically interpolate them into a feedback string. For example, if they build "bigat naimbag a", the UI will say: *"'bigat naimbag a'? I don't think that's it!"*
- **Backend Evaluation:** The final assembled sentence is evaluated by the NLP backend using the original template string.

## 4. Question & Data Structure (`SariSariChismis.json`)
The game pulls from `IdentityExpressions.json`.
**The JSON Structure (26 Total Rounds in Pool):**
* The JSON contains exactly 26 rounds covering all 22 phrases.
* **Identity (8 rounds):** 4 situational, 4 Tindera meta-quizzes.
* **Greetings (8 rounds):** 4 situational, 4 Tindera meta-quizzes.
* **Gratitude (5 rounds):** 3 situational, 2 Tindera meta-quizzes.
* **Responses (5 rounds):** 3 situational, 2 Tindera meta-quizzes.

## 5. Game Loop & Round Selection
A single session of the minigame consists of exactly **15 Rounds**. The player gets **5 Hearts** (lives). The `GameManager` will randomly select these 15 rounds from the JSON pool using the following distribution:
* **All 8** Identity rounds.
* **3 random** Responses recall rounds.
* **2 random** Gratitude recall rounds.
* **2 random** Greetings recall rounds.

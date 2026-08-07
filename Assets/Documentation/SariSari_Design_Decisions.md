# Sari-Sari Store Minigame Design Decisions

## 1. Core Gameplay & Mechanic
* **Theme:** A classic Filipino Sari-Sari Store. You are standing outside the metal grate window interacting with the store owner inside.
* **Topic:** Responses (Yes, No, Okay, I understand, I don't understand) + Recall (Greetings, Gratitude).
* **Interaction:** **Sliding the Answer through the Window**. The vendor asks a question. You have 3-4 "Response Cards" (or pieces of paper) at the bottom of the screen. You physically drag the correct card and slide it into the small window opening of the sari-sari store to hand it to the vendor!

## 2. Solving Subjectivity in Questions
To avoid ambiguity (where both "Yes" and "No" could be technically correct answers), the **Situation Prompt** will explicitly state the player's preference.

**Example:**
* **Vendor asks:** "I don't have exact change, is 3 pieces of candy okay?"
* **Situation Prompt:** "The vendor asks if candy is an okay replacement for change. **You don't want candy.** What do you say?"
* **Correct Answer:** No (Saan / Dili)

## 3. "Blind Surprise" STT Mechanic
To prevent the STT from feeling repetitive, the game will use the **Blind Surprise Challenge** mechanic:
* **60% of the game:** Purely fast-paced Drag & Drop (sliding paper through the window). No speaking required.
* **40% of the game (Randomly triggered):** A loud truck drives by or the vendor turns away and says, *"Wait, I couldn't hear you, what did you say?"*
* **The Challenge:** The Drag & Drop UI completely disappears, and the STT mic opens. The player must speak the correct response purely from memory!

## 4. Question & Data Structure (`Responses.json`)
Since this game takes place specifically at a Sari-Sari Store, the entire game will pull from a single `Responses.json` file. This file will contain custom-written situations tailored exactly to buying things at a neighborhood store!

**The JSON Pool:**
* **15 Main Response Situations:** (3 for Yes, 3 for No, 3 for Okay, 3 for I Understand, 3 for I Don't Understand)
* **13 Recall Situations:** (8 Custom Sari-Sari Greetings, 5 Custom Sari-Sari Gratitude)

**The Game Loop (20 Rounds Total):**
When the game starts, it will load ALL 15 Main Response situations, and then randomly select 5 Recall situations from the pool. This gives the player 20 unique rounds per game session!

## 5. Win Conditions & UI
* **Win Condition:** Player must survive all 20 rounds without losing all 3 (or 5) Hearts.
* **UI Elements:**
  * Top Bar: Health (Hearts), Current Round / Progress Bar
  * Center: Sari-Sari Store Window (Drop Zone) and Situation Prompt Box
  * Bottom: 3 to 4 Draggable Response Cards
  * Overlay: STT Slide-in Panel for the Blind Surprise Rounds

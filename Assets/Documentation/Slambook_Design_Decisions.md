
# Slambook (Identity Expressions) Minigame Design Decisions

## 1. Core Gameplay & Mechanic
* **Theme:** **"The Slambook"**. A massive part of Filipino school culture is passing around a notebook (Slambook) for friends to fill out their personal info (Name, Age, Favorites, etc.). This perfectly matches Identity!
* **Interaction:** **Drag and Drop Stickers/Cutouts**. The game displays a colorful Slambook page with a question prompt (e.g., "What is your name?"). The player is given empty dotted "slots" representing the sentence structure. 
* **The Goal:** Below the notebook, individual words (and distractors) look like cute scrapbook stickers or paper cutouts. The player must drag these `WordBlocks` into the correct dotted slots on the Slambook page to assemble the sentence.

## 2. Dynamic Data & Template Handling
* **Utilizing `LuminangPhrases.json`:** The Identity phrases in the database already contain arrays like `"ilokano_required_tokens": ["ti", "nagan", "ko", "ket"]`. We will use this exact array to dynamically generate the draggable `WordBlocks` and the exact number of empty slots required!
* **Template Substitution:** For `{name}` or `{place}`:
  * The game will read `PlayerPrefs.GetString("PlayerName")` and automatically generate a special `WordBlock` containing the player's actual name.
  * The player must drag their Name Block into the final slot to complete the sentence (e.g., `[ti] [nagan] [ko] [ket] [Irah]`).

## 3. "Confirm STT" Mechanic
This minigame enforces both grammatical syntax (building) and pronunciation (speaking).
* **The Interaction:** The player drags the blocks. Once all slots are filled correctly, a "Submit" button or microphone icon pulses.
* **The Microphone:** The player must **speak the complete sentence out loud** to officially lock it in.
* **Backend Evaluation:** Even though the player assembled it piecemeal, the game concatenates it back into the expected template string (`ti nagan ko ket {name}`) and sends it to the NLP backend for grading.

## 4. Question & Data Structure (`SentenceBuilder.json`)
The game will pull dynamically from a dedicated `SentenceBuilder.json` file.

**The JSON Structure:**
* **Question Prompt:** The exact string asking for the info (in English/Ilokano/Cebuano).
* **Target Phrase ID:** Maps to the phrase ID in `LuminangPhrases.json` (e.g., `identity_020`).
* **Distractor Words:** Extra words (like `ko`, `mo`, `na`) to throw off the player and test real grammatical knowledge.

**Recall Rules (L1–L3):**
* Out of the 6 total rounds, 3 will be Identity questions (long sentences).
* The remaining 3 will be Recall questions pulling from L1, L2, or L3. 
* For short Recalls (like "Yes"), it will just be a 1-slot or 2-slot build, providing a breather between the long Identity sentences!

# 🎮 Luminang — Mini-Game Design Guide

> **Last Updated:** August 2026  
> **Purpose:** Reference document for all planned lesson mini-games, their mechanics, STT integration, and spaced recall rules.

---

## 📌 About Luminang

**Luminang** (to journey/travel) is a Filipino regional language learning game set in a Philippine cultural world.  
Players travel through regions and learn real local languages (**Ilokano**, **Cebuano**) by interacting with NPCs, exploring a map, and completing lessons.

### Core Loop Per Lesson
```
NPC Teaching Phase
  └── NPC teaches a word/phrase
  └── Player REPEATS it using STT (Speech-to-Text)
  └── Meaning and usage of the word is shown

NPC Conversation Phase
  └── Player talks to different NPCs around the area
  └── Player picks the correct response from multiple choices
  └── Tests comprehension in a social context

Mini-Game Phase  ← THIS DOCUMENT COVERS THIS PART
  └── A fun, themed mini-game pops up
  └── Tests active recall of the lesson vocabulary
  └── Always includes an STT moment
  └── Includes a Recall Round using words from PREVIOUS lessons
```

---

## 🔁 The Recall Rule (Applies to ALL Mini-Games from L2 onwards)

Every mini-game from **Lesson 2 onwards** must include a **Recall Round** — a short 2–3 question segment (either at the start or embedded mid-game) that pulls vocabulary from **all previously completed lessons**.

**How it works in code:**
- When loading mini-game data for Lesson N, fetch vocab from **Lessons 1 through N**
- **New lesson words** → primary targets (correct answers, main game objects)
- **Old lesson words** → distractors, wrong choices, or bonus targets

Since `CurriculumManager` already fetches by `categoryKey`, extend it to accept a **list of categoryKeys** — the current lesson + all past ones.

---

## 📈 Progression & Difficulty Scaling
- **L1-L2:** Forgiving STT, unlimited time, obvious wrong choices.
- **L3-L5:** Introduction of time limits, more nuanced distractors.
- **L6+:** "Blind Surprise" STT moments increase in frequency. Faster timing.

---

## 🎙️ STT Integration Rule (Applies to ALL Mini-Games)

Every mini-game must have **at least one STT moment** built into it. This is Luminang's core differentiator.

**STT Moment Types:**

| Type | Description |
|---|---|
| **Confirm STT** | Player taps their answer first, then SPEAKS it to confirm. |
| **Answer STT** | Player must SPEAK the answer directly with no tap. |
| **Bonus STT** | Optional spoken bonus at the end for extra coins/XP. |

**STT Flow in a mini-game:**
```
Player sees question/situation
  → Player taps their answer (or game auto-triggers STT)
  → STT mic activates with a visual pulse indicator
  → Player speaks the word/phrase
  → Game compares spoken text to expected answer
  → Correct: celebrate + proceed
  → Wrong: gentle correction shown, player tries again (max 2 tries)
```

---

## 💡 Question Clarity Rule (For Similar Words)

Some words are very alike (e.g., "thank you" vs "thank you so much"). To prevent players from confusing them, the questions must be unmistakable.
- **Rule:** Questions must use clear descriptions, intended usages, AND highly specific situations combined.
- **Example Prompt:** "You are extremely grateful because someone saved your life. What do you say?" -> Answer: Thank you very much (Agyamanak unay).

---

## 📚 Lesson Mini-Game Breakdown

---

### CHAPTER 1 — Conversational & Social

---

#### L1 — Greetings
**🎣 Mini-Game: "Cast the Right Greeting" (Fishing Game)**

**Theme:** A riverside fishing scene. An old NPC sits on a bamboo dock.

**Gameplay:**
- Different fish swim by, each with a greeting written on them
- An NPC shouts a situation: "It's morning! What do you say?"
- Player casts their fishing line at the correct fish
- Miss it and it swims away — one more chance before it's gone
- 5 rounds, each with a different time-of-day situation

**STT Integration:**
- After catching the correct fish, the fish "talks" — player must SAY the greeting out loud (STT) to reel it in completely
- Wrong pronunciation = fish wiggles free, try again

**Recall Round:** None (first lesson)

**Win Condition:** Catch at least 4/5 fish correctly + speak them via STT

---

#### L2 — Expressions of Gratitude
**🃏 Mini-Game: "Ano Ang Sasabihin Mo?" (Reaction Cards)**

**Theme:** A deck of situational cards presenting diverse, real-world cultural moments.

**Gameplay:**
- A "Situation Card" appears in the center with a highly specific context and description (e.g., "A neighbor spent 5 hours fixing your roof. Express extreme gratitude.").
- 3 vocabulary options appear below it (e.g., "Thank you", "Thank you very much", "I am sorry").
- Player taps the vocabulary card that perfectly matches the intensity of the situation.
- The game tests nuances without locking them to a single environment.

**STT Integration:**
- Confirm STT: After tapping the correct card, a mic appears. The player must speak the phrase to officially log their reaction.

**Recall Round (L1):**
- 1 or 2 situation cards will present a greeting scenario (e.g., "You enter a shop in the morning. Greet the vendor!").

**Win Condition:** Correctly react to 8/10 situations + pass all STT checks.

---

#### L3 — Responses (Yes / No / Maybe)
**🩴 Mini-Game: "Tumbang Preso" (Slipper Toss)**

**Theme:** A classic Filipino street game where you knock down tin cans with a tsinelas (slipper).

**Gameplay:**
- A Situation Prompt appears at the top of the screen (e.g. "The vendor asks if you want spicy sauce. You hate spicy food.").
- 3 Tin Cans stand in the street, each with a signpost hovering above them containing your possible answers (Yes, No, Okay, or past lesson vocab).
- Player physically drags and swipes a Tsinelas from the bottom of the screen to throw it and knock over the correct can!

**STT Integration:**
- 40% Blind STT Surprise: Randomly, the Tin Cans spawn completely blank! The STT mic opens up, and you must shout the correct word from memory to make the Tsinelas fly and hit the invisible can.

**Recall Round (L1–L2):**
- 5 out of the 20 total rounds will randomly pull Greetings or Gratitude situations. The long phrases easily fit on the signposts above the cans.

**Win Condition:** Survive 20 total rounds without losing all your hearts (mistakes cost hearts).

---

#### L4 — Identity Expressions
**🗣️ Mini-Game: "POV Sari-Sari Store" (Drag-and-Drop Sentence Builder)**

**Theme:** A First-Person view (POV) of a traditional Sari-Sari store. You talk to the Tindera (Vendor) and various Tambays (Bystanders) who ask you nosy questions!

**Gameplay:**
- The game alternates between **Situational Roleplay** (a Tambay asks where you are from) and **Tindera Quizzes** (The Tindera acts as a guide, asking "How do you say X?").
- A speech bubble appears with empty dotted slots.
- A pile of scrambled individual `WordBlocks` sits at the bottom of the screen (combining the correct words with random distractor words from past vocabulary).
- Player drags and drops the individual words into the slots to build the correct sentence grammatically.

**STT Integration:**
- Confirm STT: Once all slots are filled correctly, the microphone activates. The player speaks the complete sentence to submit their answer to the NPC.

**Recall Round (L1–L3):**
- Massive Recall integration: The Tindera will quiz you on EVERY single phrase from Greetings, Gratitude, and Responses (e.g., "How do you say 'thank you'?"). The same drag-and-drop word mechanic applies.

**Win Condition:** Complete all 24 rounds of Identity and Recall questions.

---

### CHAPTER 2 — Functional & Navigational

---

#### L5 — Requests
**🛒 Mini-Game: "Palengke Match" (Line Matching)**

**Theme:** A busy wet market (Palengke). Vendors are shouting and customers have specific needs.

**Gameplay:**
- Left column shows 3 NPC portraits with thought bubbles detailing exactly what they need (e.g., "Needs to ask the price of the fish.").
- Right column shows 3 request phrases.
- Player draws a line connecting the specific need to the correct phrase.

**STT Integration:**
- After connecting a pair, the player must say the request out loud to officially serve the customer. 

**Recall Round (L1–L4):**
- One of the customers asks for your name instead of an item (L4 Recall).
- You accidentally bump into a vendor while walking (L2 Recall).

**Win Condition:** Connect and serve 3 sets of customers + all answered with STT.

---

#### L6 — Directions
**🚌 Mini-Game: "Tricycle Dash" (Lane Tap)**

**Theme:** Riding in the sidecar of a tricycle moving down a town road.

**Gameplay:**
- The road scrolls downwards. Arrows indicating turns (Left, Right, Straight, Stop) approach a "hit zone" at the bottom of the screen.
- A prompt at the top says: "Tell the driver to turn left!"

**STT Integration:**
- Answer STT: There are no buttons to tap! The player must SHOUT the correct directional word (via STT) right before the arrow hits the zone to make the tricycle turn.

**Recall Round (L1–L5):**
- You must ask the driver how much the fare is (L5 Recall).
- You greet the driver when you enter (L1 Recall).

**Win Condition:** Successfully navigate 10 turns via voice commands.

---

#### L7 — Count
**🍢 Mini-Game: "Tusok-Tusok" (Fishball Stand)**

**Theme:** A traditional Filipino street food cart (Fishballs, Kwek-Kwek, Kikiam) parked outside a school or plaza. You are buying snacks from "Manong Fishball".

**Gameplay:**
- Manong Fishball asks you how many pieces you want (e.g., "Ilan sayo?").
- A prompt tells you the target number (e.g., "Skewer 7 fishballs").
- You physically drag/swipe a barbecue stick to "tusok" (skewer) the exact number of fishballs from the boiling wok.
- If you skewer too many or too few, Manong shakes his head!

**STT Integration:**
- **Answer STT:** Once you have the correct number of fishballs on your stick, the STT mic activates. You must confidently state the number in the target language (e.g., "Pito" for 7) to pay and complete the order.

**Recall Round (L1–L6):**
- Manong Fishball will occasionally test your past knowledge (e.g., He asks your name -> L4 Recall).
- You must thank him after receiving your food (L2 Recall).
- He asks if you want spicy sauce, and you must answer Yes or No (L3 Recall).

**Win Condition:** Successfully skewer and verbally count 10 different orders.

---

### CHAPTER 3 — Grammatical Foundations

---

#### L8 — Action Verbs
**🏃 Mini-Game: "Simon Says" (Quick Tap)**

**Theme:** A lively barangay sports fest.

**Gameplay:**
- A large, clear icon of an action (e.g., a person eating) appears in the center of the screen with the prompt: "Command them to eat!"
- A timer ticks down extremely fast (3 seconds).

**STT Integration:**
- Answer STT: The player must instantly shout the correct verb via STT to make the character perform the action and fill a combo bar. No buttons to tap!

**Recall Round (L1–L7):**
- A number icon appears, requiring the player to shout the number (L7 Recall).
- A direction arrow appears, requiring a direction shout (L6 Recall).

**Win Condition:** Achieve a 10-action combo via STT before the timer runs out.

---

#### L9 — Linking Verbs
**🪞 Mini-Game: "Fill the Blank" (Swipe Selection)**

**Theme:** Constructing sentences logically on a chalkboard.

**Gameplay:**
- A sentence with a blank appears: "Siya ___ malipayon."
- A card with a linking word appears in the middle.
- Player swipes right if the word perfectly completes the sentence, or swipes left if it is grammatically incorrect.

**STT Integration:**
- Confirm STT: If swiped right, the player must speak the *entire* completed sentence to lock it in.

**Recall Round (L1–L8):**
- Some sentences will test Action Verbs from the previous lesson (L8 Recall).

**Win Condition:** Correctly sort 10 cards + speak 5 full sentences.

---

#### L10 — Pronouns
**👥 Mini-Game: "Photo Hunt" (Image Tap)**

**Theme:** Looking at a family photo album.

**Gameplay:**
- A flat illustration of a family gathering is shown. 
- A highly descriptive prompt appears: "Tap the group of people EXCLUDING yourself."
- Player taps the correct cluster of characters in the photo.

**STT Integration:**
- Confirm STT: After tapping, the player speaks the pronoun (e.g., "Sila" or "Kami") to confirm.

**Recall Round (L1–L9):**
- "Tap the person who is EATING" (L8 Recall).

**Win Condition:** 8/12 correct groups + 6 full-sentence STT.

---

#### L11 — Interrogatives
**🕵️ Mini-Game: "Secret Safe" (Dial Pad)**

**Theme:** A mystery in an old Bahay na Bato.

**Gameplay:**
- A UI safe dial with question words (Who, What, Where, When, Why, How) around it. 
- A highly specific clue is given: "Used when asking for a person's identity."
- Player spins the dial to the correct word and taps "Unlock".

**STT Integration:**
- Confirm STT: Speak the question word to fully turn the lock and open the safe.

**Recall Round (L1–L10):**
- The safe contains an item; you must describe who owns it using a pronoun (L10 Recall).

**Win Condition:** Unlock all 5 clues + all STT confirmed.

---

### CHAPTER 4 — Sentence Building

---

#### L12 — Sentence Building
**🧩 Mini-Game: "Magnet Poetry" (UI Drag & Drop)**

**Theme:** Organizing thoughts on a visual board.

**Gameplay:**
- Scattered word blocks on screen (subjects, verbs, objects).
- A prompt gives the exact English sentence to translate.
- Empty slots sit at the top.
- Player drags and drops the words into the slots in the correct grammatical order.

**STT Integration:**
- Confirm STT: Speak the full sentence to submit the answer for checking.

**Recall Round (All lessons):**
- The sentences built here pull vocabulary from every single past chapter.

**Win Condition:** Build 5 complete sentences + speak all of them via STT.

---

### CHAPTER 5 — Final Assessment

---

#### L13 — Final Assessment
**🏆 Mini-Game: "The Gauntlet" (Rapid Fire Quiz)**

**Theme:** The ultimate fiesta challenge.

**Gameplay:**
- A fast-paced compilation of all the UI mechanics:
  - Swipe Cards (from L3)
  - Line Matching (from L5)
  - Sentence Building (from L12)
- Randomly throws highly-specific questions from ALL previous chapters.

**STT Integration:**
- Every single answer in this mini-game requires STT confirmation. This is the ultimate test of active speaking.

**Win Condition:** Score 80%+ across all challenges to unlock the Completion Badge.

---

## 🛠️ Implementation Notes

### STT System Hook
All mini-games should call the existing STT system via `STTVoiceVisualizerAdapter` and validate using the expected phrase passed from the lesson vocabulary data.

```
Expected flow:
MinigameManager.StartMinigame()
  → Load vocab from CurriculumManager (current + past categoryKeys)
  → On STT trigger: STTManager.StartListening(expectedPhrase)
  → On result: compare to expectedPhrase (fuzzy match recommended)
  → Callback: OnSTTCorrect() / OnSTTWrong()
```

### Recall Data Loading (Pseudo-code)
```csharp
List<string> categoryKeysToLoad = GetAllPreviousCategories(currentLessonIndex);
categoryKeysToLoad.Add(currentCategoryKey);
var allVocab = await CurriculumManager.GetMatchingPairs(categoryKeysToLoad, languageId);

foreach (var word in allVocab)
    word.isRecall = word.categoryKey != currentCategoryKey;
```

### Mini-Game Difficulty Scaling

| Lesson | STT Strictness | Timer Pressure | Recall % |
|---|---|---|---|
| L1 | Loose (1–2 syllables) | None | 0% |
| L2–L4 | Medium | Low | 20% |
| L5–L7 | Medium | Medium | 30% |
| L8–L11 | Strict (full words) | Medium-High | 40% |
| L12 | Strict (full sentences) | High | 50% |
| L13 | Strict (full sentences) | High | 100% (all review) |

---

*This document should be updated as mini-games are finalized and built.*

---

## 🗄️ Backup / Generalized Minigames
These mechanics are highly reusable and not strictly tied to conversational situations, making them perfect fallbacks for future vocabulary categories (Colors, Numbers, Items, etc.).

### 1. "Pabitin" (Jump & Grab)
* **Theme:** A classic Filipino fiesta game where a bamboo lattice hangs from a tree, bouncing up and down.
* **Gameplay:** A Situation Prompt appears at the top. 3 prize bags hang from the lattice, each with an answer tag. The player must time their tap to make their character jump and grab the correct prize bag.
* **STT Integration:** Confirm STT. After grabbing the bag, the player must shout the word to officially "open" the prize. If they fail, the prize gets pulled back up!

### 2. "Palayok Smash" (Hit the Pot)
* **Theme:** A classic party game. 3 clay pots (*Palayok*) are hanging from a rope. Your character is blindfolded holding a bamboo stick.
* **Gameplay:** The prompt appears on screen. The 3 pots have the 3 text options written on them. The player taps or swipes towards the correct pot to swing their stick and smash it open.
* **STT Integration:** Blind Surprise. Just like Tumbang Preso, sometimes the pots spawn completely blank. Because the character is "blindfolded", the player must shout the answer from memory to smash the correct pot.

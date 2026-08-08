# Tumbang Preso Minigame Design Decisions

## 1. Core Gameplay & Mechanic
* **Theme:** A classic Filipino street game (Tumbang Preso) played in **First-Person POV (Landscape)**. You see the player's hand holding a Tsinelas (slipper) at the bottom of the screen.
* **The "Bantay" (NPC):** To make it culturally accurate, there is an NPC (the "it" or bantay) standing to the side of the cans. The NPC changes based on the situation, and a Chat Bubble appears next to them so you know exactly what they are saying. 
* **Interaction:** **The Tsinelas Toss**. The game gives you a Situation Prompt at the top of the screen. 3 Tin Cans stand in the street, each with a small signpost hovering above it containing an answer. You place your finger on the Tsinelas and **Swipe forward** to throw it!

## 2. 2D Physics & Visuals (Landscape)
Since the game is in landscape, there is plenty of horizontal room! It will not be cramped.
* **The Layout:** The NPC and their Chat Bubble sit comfortably on the left side of the street, while the 3 Tin Cans are spread out across the middle/right side.
* **The Throwing Mechanic:** Since it is a 2D game, we will simulate depth! When you swipe up, the Tsinelas Sprite will move upwards on the Y-axis and rapidly **scale down (shrink)** to look like it is flying away into the background.
* **The Collisions:** We will attach invisible 2D Colliders (`BoxCollider2D`) to the Tin Cans. When the flying Tsinelas hits the correct collider, it triggers an animation that knocks the can over!

## 3. "Blind Surprise" STT Mechanic
To prevent the STT from feeling repetitive, the game will use the **Blind Surprise Challenge** mechanic:
* **60% of the game:** Purely fast-paced swiping and throwing. No speaking required.
* **40% of the game (Randomly triggered):** A cinematic "Surprise" transition happens!
* **The Transition Animation:** Before any text appears on the cans, the 3 Tin Cans will suddenly fall over (knocked down)! Then, the STT Microphone icon will slide up from the bottom-center of the screen. 
* **The Challenge Prompt:** A highlighted text prompt will appear saying: *"No choices this time! Show us your memory power!"* The player must speak the correct response purely from memory. 
* **The "Learn It" Fallback (3 Tries):** We want them to learn, not get frustrated by the microphone! 
  * **Try 1 & 2:** The player speaks. If they get it wrong (or the mic mishears them), **NO Tsinelas is deducted**. The game simply gives them a hint by revealing the text on the Tin Cans so they can read it and try again.
  * **Try 3:** If they fail all 3 tries, that means they completely failed the STT round. **This is when 1 Tsinelas is deducted.**
  * **The Fallback:** After failing 3 times, the STT mic closes, and the player is allowed to physically swipe the Tsinelas to hit the can so they can move on to the next round without getting permanently stuck!

## 4. Question & Data Structure (`Responses.json`)
The game will pull dynamically from a single `Responses.json` file. Because Tumbang Preso is a generic physics game, it doesn't need to force everything to fit a specific scenario (like a store or a restaurant), allowing for a massive variety of everyday situations!

**The JSON Pool:**
* **15 Main Response Situations:** (3 for Yes, 3 for No, 3 for Okay, 3 for I Understand, 3 for I Don't Understand)
* **13 Recall Situations:** (8 Greetings, 5 Gratitude)

**The Game Loop (20 Rounds Total):**
When the game starts, it will load ALL 15 Main Response situations, and then randomly select 5 Recall situations from the pool. This gives the player 20 unique rounds per game session!

## 5. Win Conditions & UI
* **Win Condition:** The game consists of **20 Total Rounds** (15 Responses + 5 Recalls).
* **The "Ammo" System:** Just like the Fishing Game's bait system, the player starts with **25 Tsinelas Icons** (Ammo) at the top of the screen! 
* **The Math & Stars:** Since there are 20 rounds, the player is guaranteed to use 20 Tsinelas. This leaves a "buffer" of **5 Extra Tsinelas** for mistakes. If they make a mistake, they waste a Tsinelas. At the end of the game, computing the 3 Stars is incredibly easy based on how many of those 5 extra Tsinelas they have left! If they hit 0 Tsinelas before finishing 20 rounds, it's Game Over.
* **UI Elements:**
  * Top: Ammo (Number counter showing 25 Tsinelas), Current Round / Progress Bar, Situation Prompt Text Box
  * Middle: 3 Tin Cans with Hovering Signposts (Text Labels)
  * Bottom: The Draggable/Swipeable Tsinelas (Slipper)
  * Overlay: STT Slide-in Panel for the Surprise Rounds

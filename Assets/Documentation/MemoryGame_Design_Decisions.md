# Memory Match Minigame - Design Decisions

This document tracks the design rules, logic flow, and architectural decisions for the Action Verbs Memory Match Minigame.

## 1. Core Gameplay Loop
* **Phase 1 (Memory Match):** The board features a static 4x4 grid containing 16 cards (8 pairs of Action Verbs). The player flips cards to find *any* matching pair. Mismatched cards glow red and simply flip back over (no penalty).
* **Phase 2 (Verification Drag-and-Drop):** When a pair is matched, the cards glow green. One of the matched cards moves to the top center of the screen. The 8 action verb words appear at the bottom. The player must drag the correct word onto the card.
* **Phase 3 (Verification Speech):** Once the correct word is dropped, an STT panel pops up asking the player to "Say the word" into the microphone. They have 3 tries.
* **Phase 4 (Card Lock):** If STT is successful, the player returns to the memory board. The matched pair remains face-up and locked on the grid.
* **Random Recall Encounters:** At random points between memory matches, a surprise multiple-choice panel (Tusok-Tusok style) will pop up covering previous categories (Requests, Directions, Count). 
* **Game Over / Day Complete:** Once all 8 pairs and 5 recall questions are finished (13 rounds total), the game ends showing 5-star mastery for each verb, remaining hearts, and earned coins.

## 2. Heart & Penalty System (5 Hearts Total)
* **Memory Phase:** Finding a mismatched pair on the memory board does NOT cost a heart.
* **Drag-and-Drop Phase:** If the player drags the *wrong* word to the card, they lose **1 Heart** and must try dragging again.
* **STT Phase:** If the player speaks the word wrong, they lose **1 Try** (out of 3). If they lose all 3 tries, they do NOT lose a heart; instead, they are sent back to the Drag-and-Drop phase to redo the drag, and then get 3 fresh STT tries.
* **Recall Encounters:** Answering a recall multiple-choice question wrong costs **1 Heart**.

## 3. Card Flip Mechanics
* The cards use a **2D Scale Trick** for flipping. 
* To flip a card: Scale X shrinks from `1` to `0`, the sprite swaps from the Card Back to the Card Front (picture), and then Scale X expands from `0` back to `1`. This provides a smooth, pseudo-3D animation.

## 4. Data Source & Math
* **Total Rounds:** 13 (8 Action Verb Pairs + 5 Random Recall Questions)
* **Primary Source:** Action Verbs from `LuminangPhrases.json`
* **Recall Source:** Requests, Directions, Count from `LuminangPhrases.json`

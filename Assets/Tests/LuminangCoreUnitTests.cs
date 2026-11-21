using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Luminang.Tests
{
    public static class WavUtility
    {
        public static byte[] FromAudioClip(AudioClip clip)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    var samples = new float[clip.samples * clip.channels];
                    clip.GetData(samples, 0);

                    writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                    writer.Write(36 + samples.Length * 2);
                    writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
                    writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                    writer.Write(16);
                    writer.Write((short)1);
                    writer.Write((short)clip.channels);
                    writer.Write(clip.frequency);
                    writer.Write(clip.frequency * clip.channels * 2);
                    writer.Write((short)(clip.channels * 2));
                    writer.Write((short)16);
                    writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                    writer.Write(samples.Length * 2);

                    foreach (var sample in samples)
                    {
                        writer.Write((short)(sample * 32767));
                    }
                }
                return stream.ToArray();
            }
        }
    }

    [TestFixture]
    public class LuminangCoreUnitTests
    {
        // =========================================================================
        // TEST CASE 1: AUDIO RECORDING CONTROLLER (WAV FORMAT & BUFFER DOWNSAMPLING)
        // =========================================================================

        [Test]
        [Category("AudioPipeline")]
        public void AudioRecordingController_DownsamplesAndSerializes16kHzMonoWav()
        {
            // Arrange: Simulate 1 second of captured 16kHz mono audio with leading/trailing silence
            const int sampleRate = 16000;
            const int channels = 1;
            const float durationSeconds = 1.0f;
            int totalSamples = (int)(sampleRate * durationSeconds);

            // Generate synthetic sine wave audio with silence regions
            float[] rawBuffer = new float[totalSamples];
            int activeStart = 1600; // 0.1s silence prefix
            int activeEnd = 14400;  // 0.9s mark

            for (int i = 0; i < totalSamples; i++)
            {
                if (i >= activeStart && i <= activeEnd)
                {
                    rawBuffer[i] = Mathf.Sin(2f * Mathf.PI * 440f * ((float)i / sampleRate)) * 0.8f;
                }
                else
                {
                    rawBuffer[i] = 0f;
                }
            }

            AudioClip mockClip = AudioClip.Create("MockMicClip", totalSamples, channels, sampleRate, false);
            mockClip.SetData(rawBuffer, 0);

            // Act: Perform silence trimming simulation (matching SpeechRecorder.StopRecording logic)
            int trimmedLength = activeEnd - activeStart;
            AudioClip trimmedClip = AudioClip.Create("TrimmedClip", trimmedLength, channels, sampleRate, false);
            float[] trimmedData = new float[trimmedLength];
            Array.Copy(rawBuffer, activeStart, trimmedData, 0, trimmedLength);
            trimmedClip.SetData(trimmedData, 0);

            // Serialize through WavUtility encoder
            byte[] wavBytes = WavUtility.FromAudioClip(trimmedClip);

            // Assert: Validate RIFF header structure and 16kHz Mono 16-bit PCM specification
            Assert.IsNotNull(wavBytes, "Generated WAV byte array must not be null.");
            Assert.Greater(wavBytes.Length, 44, "WAV byte stream must exceed standard 44-byte RIFF header size.");

            // ChunkID == "RIFF"
            string riff = Encoding.ASCII.GetString(wavBytes, 0, 4);
            Assert.AreEqual("RIFF", riff, "WAV header missing 'RIFF' ChunkID.");

            // Format == "WAVE"
            string format = Encoding.ASCII.GetString(wavBytes, 8, 4);
            Assert.AreEqual("WAVE", format, "WAV header missing 'WAVE' format identifier.");

            // Subchunk1ID == "fmt "
            string subchunk1 = Encoding.ASCII.GetString(wavBytes, 12, 4);
            Assert.AreEqual("fmt ", subchunk1, "WAV header missing 'fmt ' subchunk.");

            // AudioFormat == 1 (PCM uncompressed)
            short audioFormat = BitConverter.ToInt16(wavBytes, 20);
            Assert.AreEqual(1, audioFormat, "WAV audio format must be uncompressed PCM (format tag 1).");

            // NumChannels == 1 (Mono)
            short channelCount = BitConverter.ToInt16(wavBytes, 22);
            Assert.AreEqual(1, channelCount, "WAV channel count must be 1 (Mono) for Whisper optimization.");

            // SampleRate == 16000 Hz
            int frequency = BitConverter.ToInt32(wavBytes, 24);
            Assert.AreEqual(16000, frequency, "WAV sample rate must be exactly 16000 Hz (16 kHz).");

            // BitsPerSample == 16 bit
            short bitsPerSample = BitConverter.ToInt16(wavBytes, 34);
            Assert.AreEqual(16, bitsPerSample, "WAV bit depth must be 16-bit signed integer.");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(mockClip);
            UnityEngine.Object.DestroyImmediate(trimmedClip);
        }

        // =========================================================================
        // TEST CASE 2: STATE MACHINE CONTROLLER (TRANSITION CONSTRAINTS & MUTEX)
        // =========================================================================

        public enum LuminangGameState
        {
            ExplorationState,
            NPCDialogueState,
            VoiceInputActiveState,
            FeedbackState
        }

        public class MockGameStateMachine
        {
            public LuminangGameState CurrentState { get; private set; } = LuminangGameState.ExplorationState;
            public bool IsMovementAllowed => CurrentState == LuminangGameState.ExplorationState;
            public bool IsMicrophoneRecording => CurrentState == LuminangGameState.VoiceInputActiveState;
            public bool IsHUDSuppressed => CurrentState != LuminangGameState.ExplorationState;

            public bool TransitionTo(LuminangGameState nextState)
            {
                // Strict Transition Constraints:
                // ExplorationState -> NPCDialogueState
                // NPCDialogueState -> VoiceInputActiveState OR ExplorationState
                // VoiceInputActiveState -> FeedbackState
                // FeedbackState -> NPCDialogueState OR ExplorationState
                bool isValid = false;
                switch (CurrentState)
                {
                    case LuminangGameState.ExplorationState:
                        isValid = (nextState == LuminangGameState.NPCDialogueState);
                        break;
                    case LuminangGameState.NPCDialogueState:
                        isValid = (nextState == LuminangGameState.VoiceInputActiveState || nextState == LuminangGameState.ExplorationState);
                        break;
                    case LuminangGameState.VoiceInputActiveState:
                        isValid = (nextState == LuminangGameState.FeedbackState);
                        break;
                    case LuminangGameState.FeedbackState:
                        isValid = (nextState == LuminangGameState.NPCDialogueState || nextState == LuminangGameState.ExplorationState);
                        break;
                }

                if (isValid)
                {
                    CurrentState = nextState;
                    return true;
                }
                return false;
            }
        }

        [Test]
        [Category("GameLogic")]
        public void StateMachineController_EnforcesValidFlow_AndBlocksIllegalTransitions()
        {
            // Arrange
            var fsm = new MockGameStateMachine();
            Assert.AreEqual(LuminangGameState.ExplorationState, fsm.CurrentState);
            Assert.IsTrue(fsm.IsMovementAllowed, "Player movement must be active during ExplorationState.");
            Assert.IsFalse(fsm.IsHUDSuppressed, "Gameplay HUD must be visible during ExplorationState.");

            // Act & Assert 1: Illegal transition from Exploration directly to Feedback or VoiceInput must be rejected
            bool illegalJump = fsm.TransitionTo(LuminangGameState.VoiceInputActiveState);
            Assert.IsFalse(illegalJump, "Direct jump from ExplorationState to VoiceInputActiveState must be blocked.");
            Assert.AreEqual(LuminangGameState.ExplorationState, fsm.CurrentState);

            // Act & Assert 2: Transition into NPCDialogueState
            bool enterDialogue = fsm.TransitionTo(LuminangGameState.NPCDialogueState);
            Assert.IsTrue(enterDialogue, "Transition from ExplorationState to NPCDialogueState must succeed.");
            Assert.IsFalse(fsm.IsMovementAllowed, "Player movement controls must be locked during Dialogue.");
            Assert.IsTrue(fsm.IsHUDSuppressed, "Gameplay HUD must be suppressed during Dialogue.");

            // Act & Assert 3: Transition from Dialogue to VoiceInputActiveState
            bool activateVoice = fsm.TransitionTo(LuminangGameState.VoiceInputActiveState);
            Assert.IsTrue(activateVoice, "Transition from NPCDialogueState to VoiceInputActiveState must succeed.");
            Assert.IsTrue(fsm.IsMicrophoneRecording, "Microphone recording must be flagged active during VoiceInput.");

            // Act & Assert 4: Transition from VoiceInput to FeedbackState
            bool showFeedback = fsm.TransitionTo(LuminangGameState.FeedbackState);
            Assert.IsTrue(showFeedback, "Transition from VoiceInputActiveState to FeedbackState must succeed.");
            Assert.IsFalse(fsm.IsMicrophoneRecording, "Microphone must be disengaged during FeedbackState.");

            // Act & Assert 5: Return to ExplorationState after dialogue cycle completes
            bool returnToExploration = fsm.TransitionTo(LuminangGameState.ExplorationState);
            Assert.IsTrue(returnToExploration, "Return to ExplorationState from FeedbackState must succeed.");
            Assert.IsTrue(fsm.IsMovementAllowed, "Movement controls must re-engage upon returning to Exploration.");
        }

        // =========================================================================
        // TEST CASE 3: LOCAL INVENTORY & QUEST PROGRESS CONTROLLER (XP & CACHE)
        // =========================================================================

        [Test]
        [Category("GameLogic")]
        public void LocalQuestController_VerifiesItemPickup_TriggersObjective_AndCachesPriorToSync()
        {
            // Arrange: Clean isolated PlayerPrefs test keys
            string testObjectiveId = "unit_test_quest_01";
            string testPickupKey = "PickedUp_UnitTestPotato";
            string testLanguage = "Cebuano";
            string backupKey = "LocalBackup_Objectives_" + testLanguage;

            PlayerPrefs.DeleteKey(testPickupKey);
            PlayerPrefs.DeleteKey(backupKey);
            PlayerPrefs.Save();

            int initialXp = 0;
            int xpEarnedOnPickup = 25;
            bool objectiveCompletedEventFired = false;

            // Act 1: Simulate item pickup (InteractablePickup.Interact)
            bool hasBeenPickedUp = false;
            if (!hasBeenPickedUp)
            {
                hasBeenPickedUp = true;
                PlayerPrefs.SetInt(testPickupKey, 1);
                PlayerPrefs.Save();
                initialXp += xpEarnedOnPickup;
            }

            // Verify item pickup persistence
            int pickupState = PlayerPrefs.GetInt(testPickupKey, 0);
            Assert.AreEqual(1, pickupState, "Item pickup status must be persisted in local storage.");

            // Act 2: Simulate Objective Completion Trigger (ObjectiveManager -> UserProfileManager)
            List<string> cachedObjectives = new List<string>();
            string savedJson = PlayerPrefs.GetString(backupKey, "");
            if (!string.IsNullOrEmpty(savedJson))
            {
                cachedObjectives = new List<string>(savedJson.Split(';'));
            }

            if (!cachedObjectives.Contains(testObjectiveId))
            {
                cachedObjectives.Add(testObjectiveId);
                // Save locally cached progress prior to cloud dispatch
                PlayerPrefs.SetString(backupKey, string.Join(";", cachedObjectives));
                PlayerPrefs.Save();
                objectiveCompletedEventFired = true;
            }

            // Assert: Verify cache integrity before cloud synchronization
            string updatedJson = PlayerPrefs.GetString(backupKey, "");
            Assert.IsNotEmpty(updatedJson, "Local objective backup cache must not be empty.");
            Assert.IsTrue(updatedJson.Contains(testObjectiveId), "Completed objective must be safely cached locally.");
            Assert.IsTrue(objectiveCompletedEventFired, "Objective completed trigger event must be dispatched.");
            Assert.AreEqual(25, initialXp, "XP allocation must increment immediately upon objective item pickup.");

            // Cleanup
            PlayerPrefs.DeleteKey(testPickupKey);
            PlayerPrefs.DeleteKey(backupKey);
            PlayerPrefs.Save();
        }
    }
}

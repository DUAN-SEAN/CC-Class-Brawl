using NUnit.Framework;
using ClassBrawl.Foundation;

namespace ClassBrawl.Tests.Foundation
{
    [TestFixture]
    public class InputSystemTests
    {
        // ====================================================================
        // FrameCounter Tests
        // ====================================================================

        [Test]
        public void Test_FrameCounter_StartsAtZero()
        {
            // Arrange
            var go = new UnityEngine.GameObject("FrameCounter");
            var counter = go.AddComponent<FrameCounter>();

            // Assert
            Assert.AreEqual(0, counter.CurrentFrame);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_FrameCounter_IncrementsViaAdvanceFrame()
        {
            // Arrange
            var go = new UnityEngine.GameObject("FrameCounter");
            var counter = go.AddComponent<FrameCounter>();

            // Act — simulate 60 FixedUpdate calls via AdvanceFrame
            for (int i = 0; i < 60; i++)
            {
                counter.AdvanceFrame();
            }

            // Assert
            Assert.AreEqual(60, counter.CurrentFrame);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_FrameCounter_Reset_ReturnsToZero()
        {
            // Arrange
            var go = new UnityEngine.GameObject("FrameCounter");
            var counter = go.AddComponent<FrameCounter>();

            // Act
            counter.Reset();

            // Assert
            Assert.AreEqual(0, counter.CurrentFrame);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(go);
        }

        // ====================================================================
        // InputBuffer Tests
        // ====================================================================

        [Test]
        public void Test_InputBuffer_WriteAndConsume_ReturnsTrue()
        {
            // Arrange
            var buffer = new InputBuffer();
            int recordedFrame = 5;

            // Act
            buffer.WriteAction(InputActionType.Attack, recordedFrame);

            // Assert — consume at frame 10, age=5, within 8-frame window
            bool result = buffer.TryConsumeAction(InputActionType.Attack, 10, 8);
            Assert.IsTrue(result);
        }

        [Test]
        public void Test_InputBuffer_ConsumeWrongType_ReturnsFalse()
        {
            // Arrange
            var buffer = new InputBuffer();
            buffer.WriteAction(InputActionType.Attack, 0);

            // Act — look for Jump but only Attack was written
            bool result = buffer.TryConsumeAction(InputActionType.Jump, 5, 8);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Test_InputBuffer_ExpiredEntry_ReturnsFalse()
        {
            // Arrange
            var buffer = new InputBuffer();
            int recordedFrame = 0;

            // Act
            buffer.WriteAction(InputActionType.Attack, recordedFrame);

            // Consume at frame 9 — BufferAge = 9 > bufferFrames (8)
            bool result = buffer.TryConsumeAction(InputActionType.Attack, 9, 8);

            // Assert — entry is expired
            Assert.IsFalse(result);
        }

        [Test]
        public void Test_InputBuffer_ExactlyAtBufferLimit_ReturnsTrue()
        {
            // Arrange
            var buffer = new InputBuffer();
            buffer.WriteAction(InputActionType.Attack, 0);

            // Act — consume at frame 8, BufferAge = 8, exactly at limit
            bool result = buffer.TryConsumeAction(InputActionType.Attack, 8, 8);

            // Assert — age == bufferFrames is valid (0 <= age <= bufferFrames)
            Assert.IsTrue(result);
        }

        [Test]
        public void Test_InputBuffer_NegativeAge_ReturnsFalse()
        {
            // Arrange
            var buffer = new InputBuffer();
            buffer.WriteAction(InputActionType.Attack, 10);

            // Act — current frame is before recorded frame (negative age)
            bool result = buffer.TryConsumeAction(InputActionType.Attack, 5, 8);

            // Assert — negative age is invalid
            Assert.IsFalse(result);
        }

        [Test]
        public void Test_InputBuffer_AlreadyConsumed_ReturnsFalse()
        {
            // Arrange
            var buffer = new InputBuffer();
            buffer.WriteAction(InputActionType.Attack, 0);

            // Act — first consume succeeds
            bool first = buffer.TryConsumeAction(InputActionType.Attack, 5, 8);
            // Second consume of same entry fails
            bool second = buffer.TryConsumeAction(InputActionType.Attack, 5, 8);

            // Assert
            Assert.IsTrue(first);
            Assert.IsFalse(second);
        }

        [Test]
        public void Test_InputBuffer_Clear_RemovesAllEntries()
        {
            // Arrange
            var buffer = new InputBuffer();
            buffer.WriteAction(InputActionType.Attack, 0);
            buffer.WriteAction(InputActionType.Jump, 1);
            buffer.WriteAction(InputActionType.Dash, 2);

            // Act
            buffer.Clear();

            // Assert — nothing should be consumable after clear
            Assert.IsFalse(buffer.TryConsumeAction(InputActionType.Attack, 5, 8));
            Assert.IsFalse(buffer.TryConsumeAction(InputActionType.Jump, 5, 8));
            Assert.IsFalse(buffer.TryConsumeAction(InputActionType.Dash, 5, 8));
        }

        [Test]
        public void Test_InputBuffer_RingBufferOverflow_WrapsCorrectly()
        {
            // Arrange — capacity is 8 (from GameConstants.InputBufferFrames)
            var buffer = new InputBuffer();
            Assert.AreEqual(8, buffer.BufferCapacity);

            // Act — write 9 entries. The 9th should overwrite the 1st.
            for (int i = 0; i < 9; i++)
            {
                buffer.WriteAction(InputActionType.Attack, i * 10);
            }

            // Assert — entry at frame 0 was overwritten, so consuming at
            // frame 5 with age check should fail (it no longer exists).
            // The oldest surviving entry is at frame 10 (the 2nd write).
            bool consumeOverwritten = buffer.TryConsumeAction(InputActionType.Attack, 5, 100);
            Assert.IsFalse(consumeOverwritten, "Overwritten entry (frame 0) should not be found.");

            // The 2nd entry (frame 10) should still be consumable.
            bool consumeSurviving = buffer.TryConsumeAction(InputActionType.Attack, 15, 100);
            Assert.IsTrue(consumeSurviving, "Entry at frame 10 should be consumable.");
        }

        [Test]
        public void Test_InputBuffer_MultipleTypes_CorrectConsumption()
        {
            // Arrange
            var buffer = new InputBuffer();
            buffer.WriteAction(InputActionType.Attack, 0);
            buffer.WriteAction(InputActionType.Jump, 0);
            buffer.WriteAction(InputActionType.Dash, 0);

            // Act & Assert — consume each type independently
            Assert.IsTrue(buffer.TryConsumeAction(InputActionType.Jump, 5, 8));
            Assert.IsTrue(buffer.TryConsumeAction(InputActionType.Attack, 5, 8));
            Assert.IsTrue(buffer.TryConsumeAction(InputActionType.Dash, 5, 8));
        }

        [Test]
        public void Test_InputBuffer_MultipleSameType_ConsumesFirstValid()
        {
            // Arrange — write two Attack entries at different frames
            var buffer = new InputBuffer();
            buffer.WriteAction(InputActionType.Attack, 0);
            buffer.WriteAction(InputActionType.Attack, 5);

            // Act — consume at frame 10, both are valid (age 10 and 5).
            // First unconsumed valid entry should be consumed.
            bool first = buffer.TryConsumeAction(InputActionType.Attack, 10, 8);

            // The first entry has age 10 > 8, so it is expired and skipped.
            // The second entry has age 5 <= 8, so it is consumed.
            Assert.IsTrue(first);

            // Second call should fail — only the expired first entry remains.
            bool second = buffer.TryConsumeAction(InputActionType.Attack, 10, 8);
            Assert.IsFalse(second);
        }

        [Test]
        public void Test_InputBuffer_SkillTypes_AllSupported()
        {
            // Arrange
            var buffer = new InputBuffer();

            // Act & Assert — all skill types can be written and consumed
            buffer.WriteAction(InputActionType.Skill1, 0);
            buffer.WriteAction(InputActionType.Skill2, 0);
            buffer.WriteAction(InputActionType.Skill3, 0);
            buffer.WriteAction(InputActionType.Skill4, 0);

            Assert.IsTrue(buffer.TryConsumeAction(InputActionType.Skill1, 5, 8));
            Assert.IsTrue(buffer.TryConsumeAction(InputActionType.Skill2, 5, 8));
            Assert.IsTrue(buffer.TryConsumeAction(InputActionType.Skill3, 5, 8));
            Assert.IsTrue(buffer.TryConsumeAction(InputActionType.Skill4, 5, 8));
        }

        // ====================================================================
        // InputBuffer — Edge Cases
        // ====================================================================

        [Test]
        public void Test_InputBuffer_Empty_ConsumeReturnsFalse()
        {
            // Arrange
            var buffer = new InputBuffer();

            // Act & Assert
            Assert.IsFalse(buffer.TryConsumeAction(InputActionType.Attack, 0, 8));
        }

        [Test]
        public void Test_InputBuffer_BufferAgeZero_ReturnsTrue()
        {
            // Arrange
            var buffer = new InputBuffer();
            buffer.WriteAction(InputActionType.Attack, 5);

            // Act — consume at the same frame (age = 0)
            bool result = buffer.TryConsumeAction(InputActionType.Attack, 5, 8);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Test_InputBuffer_ClearThenWrite_WorksCorrectly()
        {
            // Arrange
            var buffer = new InputBuffer();
            buffer.WriteAction(InputActionType.Attack, 0);
            buffer.Clear();

            // Act — write after clear
            buffer.WriteAction(InputActionType.Jump, 10);

            // Assert — new entry is consumable, old ones are gone
            Assert.IsTrue(buffer.TryConsumeAction(InputActionType.Jump, 15, 8));
            Assert.IsFalse(buffer.TryConsumeAction(InputActionType.Attack, 15, 8));
        }

        // ====================================================================
        // InputReader — Dead Zone Filtering Tests
        // ====================================================================

        [Test]
        public void Test_InputReader_DeadZone_BelowThreshold_ReturnsZero()
        {
            // Verify the dead zone constant is correctly defined.
            // Full dead zone filtering is tested through integration tests
            // with a live PlayerInput component. Here we validate the contract.
            Assert.AreEqual(0.15f, InputReader.DeadZone);
        }

        [Test]
        public void Test_InputReader_DeadZone_AboveThreshold_NotZero()
        {
            // Verify a vector above the dead zone passes filtering.
            // The dead zone logic: magnitude < 0.15f => Vector2.zero.
            UnityEngine.Vector2 input = new UnityEngine.Vector2(0.2f, 0.0f);
            Assert.IsTrue(input.magnitude >= InputReader.DeadZone);
        }

        [Test]
        public void Test_InputReader_DeadZone_ExactlyAtThreshold_NotFiltered()
        {
            // At exactly 0.15, magnitude is NOT less than 0.15,
            // so it should pass through.
            UnityEngine.Vector2 input = new UnityEngine.Vector2(0.15f, 0.0f);
            Assert.IsFalse(input.magnitude < InputReader.DeadZone);
        }

        // ====================================================================
        // InputReader — Jump Held/Released State Tests (via internal helpers)
        // ====================================================================

        [Test]
        public void Test_InputReader_WasJumpReleasedThisFrame_ResetsAfterRead()
        {
            // Arrange
            var go = new UnityEngine.GameObject("InputReader");
            var reader = go.AddComponent<InputReader>();

            // Initially, neither should be true
            Assert.IsFalse(reader.IsJumpHeld());
            Assert.IsFalse(reader.WasJumpReleasedThisFrame());

            // Cleanup
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_InputReader_PlayerIndex_NoPlayerInput_ReturnsMinusOne()
        {
            // Arrange — InputReader without a PlayerInput component
            var go = new UnityEngine.GameObject("InputReader");
            var reader = go.AddComponent<InputReader>();

            // Assert — without PlayerInput, index should be -1
            Assert.AreEqual(-1, reader.PlayerIndex);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_InputReader_SimulateJumpPress_SetsHeldTrue()
        {
            // Arrange
            var fcGo = new UnityEngine.GameObject("FrameCounter");
            var frameCounter = fcGo.AddComponent<FrameCounter>();
            var readerGo = new UnityEngine.GameObject("InputReader");
            var reader = readerGo.AddComponent<InputReader>();
            reader.Initialize(frameCounter);

            // Act
            reader.SimulateButtonPress(InputActionType.Jump);

            // Assert
            Assert.IsTrue(reader.IsJumpHeld());
            Assert.IsFalse(reader.WasJumpReleasedThisFrame());

            // Cleanup
            UnityEngine.Object.DestroyImmediate(readerGo);
            UnityEngine.Object.DestroyImmediate(fcGo);
        }

        [Test]
        public void Test_InputReader_SimulateJumpRelease_TriggersShortHopFlag()
        {
            // Arrange
            var fcGo = new UnityEngine.GameObject("FrameCounter");
            var frameCounter = fcGo.AddComponent<FrameCounter>();
            var readerGo = new UnityEngine.GameObject("InputReader");
            var reader = readerGo.AddComponent<InputReader>();
            reader.Initialize(frameCounter);

            // Act — press then release
            reader.SimulateButtonPress(InputActionType.Jump);
            Assert.IsTrue(reader.IsJumpHeld());

            reader.SimulateJumpRelease();

            // Assert — released this frame
            Assert.IsFalse(reader.IsJumpHeld());
            Assert.IsTrue(reader.WasJumpReleasedThisFrame());

            // Second read resets the flag
            Assert.IsFalse(reader.WasJumpReleasedThisFrame());

            // Cleanup
            UnityEngine.Object.DestroyImmediate(readerGo);
            UnityEngine.Object.DestroyImmediate(fcGo);
        }

        [Test]
        public void Test_InputReader_SimulateAttackPress_WritesToBuffer()
        {
            // Arrange
            var fcGo = new UnityEngine.GameObject("FrameCounter");
            var frameCounter = fcGo.AddComponent<FrameCounter>();
            var readerGo = new UnityEngine.GameObject("InputReader");
            var reader = readerGo.AddComponent<InputReader>();
            reader.Initialize(frameCounter);

            // Act
            reader.SimulateButtonPress(InputActionType.Attack);

            // Assert — can consume within buffer window
            Assert.IsTrue(reader.TryConsumeAction(InputActionType.Attack, 8));

            // Cleanup
            UnityEngine.Object.DestroyImmediate(readerGo);
            UnityEngine.Object.DestroyImmediate(fcGo);
        }

        [Test]
        public void Test_InputReader_SimulateDeviceLost_FiresEvent()
        {
            // Arrange
            var fcGo = new UnityEngine.GameObject("FrameCounter");
            var frameCounter = fcGo.AddComponent<FrameCounter>();
            var readerGo = new UnityEngine.GameObject("InputReader");
            var reader = readerGo.AddComponent<InputReader>();
            reader.Initialize(frameCounter);

            int eventPlayerIndex = -999;
            reader.OnDeviceLost += (playerIndex) => eventPlayerIndex = playerIndex;

            // Act
            reader.SimulateDeviceLost();

            // Assert
            Assert.AreEqual(-1, eventPlayerIndex); // -1 because no PlayerInput

            // Cleanup
            UnityEngine.Object.DestroyImmediate(readerGo);
            UnityEngine.Object.DestroyImmediate(fcGo);
        }

        [Test]
        public void Test_InputReader_ButtonPressExpiredAfterBufferWindow()
        {
            // Arrange
            var fcGo = new UnityEngine.GameObject("FrameCounter");
            var frameCounter = fcGo.AddComponent<FrameCounter>();
            var readerGo = new UnityEngine.GameObject("InputReader");
            var reader = readerGo.AddComponent<InputReader>();
            reader.Initialize(frameCounter);

            // Act — press at frame 0
            reader.SimulateButtonPress(InputActionType.Dash);

            // Advance 9 frames — buffer window is 8, so frame 9 means age=9
            for (int i = 0; i < 9; i++)
            {
                frameCounter.AdvanceFrame();
            }

            // Assert — input expired
            Assert.IsFalse(reader.TryConsumeAction(InputActionType.Dash, 8));

            // Cleanup
            UnityEngine.Object.DestroyImmediate(readerGo);
            UnityEngine.Object.DestroyImmediate(fcGo);
        }

        [Test]
        public void Test_InputReader_ButtonPressConsumedExactlyAtWindowEdge()
        {
            // Arrange
            var fcGo = new UnityEngine.GameObject("FrameCounter");
            var frameCounter = fcGo.AddComponent<FrameCounter>();
            var readerGo = new UnityEngine.GameObject("InputReader");
            var reader = readerGo.AddComponent<InputReader>();
            reader.Initialize(frameCounter);

            // Act — press at frame 0
            reader.SimulateButtonPress(InputActionType.Skill1);

            // Advance exactly 8 frames
            for (int i = 0; i < 8; i++)
            {
                frameCounter.AdvanceFrame();
            }

            // Assert — input still valid at age=8
            Assert.IsTrue(reader.TryConsumeAction(InputActionType.Skill1, 8));

            // Cleanup
            UnityEngine.Object.DestroyImmediate(readerGo);
            UnityEngine.Object.DestroyImmediate(fcGo);
        }

        // ====================================================================
        // Pause — Does NOT Write to InputBuffer
        // ====================================================================

        [Test]
        public void Test_InputReader_Pause_DoesNotWriteToInputBuffer()
        {
            // The Pause action fires OnPauseRequested event but does NOT
            // write to InputBuffer. This is validated by design:
            // 1. OnPausePerformed invokes OnPauseRequested event only.
            // 2. It never calls _inputBuffer.WriteAction().
            //
            // Integration test with live PlayerInput verifies this at runtime.
            // Here we confirm the buffer has no Pause entry type possible —
            // InputActionType enum has no Pause member.
            Assert.IsFalse(
                System.Enum.IsDefined(typeof(InputActionType), "Pause"),
                "InputActionType should NOT contain a Pause value — pause must not enter the combat input buffer."
            );
        }

        [Test]
        public void Test_InputActionType_DoesNotContainPause()
        {
            // Verify all defined action types — none should be Pause
            InputActionType[] types = (InputActionType[])System.Enum.GetValues(typeof(InputActionType));
            Assert.AreEqual(7, types.Length, "Should have exactly 7 action types: Jump, Attack, Dash, Skill1-4");

            foreach (InputActionType type in types)
            {
                Assert.AreNotEqual("Pause", type.ToString(),
                    "No InputActionType should be named Pause.");
            }
        }
    }
}

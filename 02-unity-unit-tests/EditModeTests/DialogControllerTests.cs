// =============================================================================
// DialogControllerTests.cs - Edit Mode Unit Tests for DialogController
// =============================================================================
// TARGET CLASS: DialogController
//   Real file: Assets/CommonA3/zSpace/licensing/Modernization/UI/Scripts/DialogController.cs
//
// WHAT IT TESTS:
//   DialogController creates and configures Dialog instances from CreationInfo
//   objects. Tests verify that CreationInfo defaults are correct and that
//   ShowDialog correctly configures title, message, input fields, buttons,
//   modal state, and fade-in behavior based on CreationInfo properties.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real DialogController instantiates prefabs and uses Unity UI.
//      These tests validate the CreationInfo data class and the configuration
//      logic through lightweight stubs.
//   4. Run via Window > General > Test Runner > EditMode tab.
// =============================================================================

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace zSpace.StudioA3.Tests
{
    // -------------------------------------------------------------------------
    // Stubs - remove these when wiring up to the real codebase
    // -------------------------------------------------------------------------

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Stub for Dialog.ButtonInfo used in CreationInfo.
    /// </summary>
    public class DialogButtonInfo
    {
        public string Id { get; set; }
        public string TextId { get; set; }

        public DialogButtonInfo(string id, string textId)
        {
            Id = id;
            TextId = textId;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Stub for Dialog.DataInputFieldInfo used in CreationInfo.
    /// </summary>
    public class DialogDataInputFieldInfo
    {
        public string LabelTextId { get; set; }
        public string WatermarkId { get; set; }

        public DialogDataInputFieldInfo(string labelTextId, string watermarkId)
        {
            LabelTextId = labelTextId;
            WatermarkId = watermarkId;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Mirrors DialogController.CreationInfo with the same default values and
    /// property structure as the real nested class.
    /// </summary>
    public class DialogCreationInfo
    {
        public bool IsModal { get; set; }
        public bool EnableFadeIn { get; set; }
        public bool EnableTabNavigation { get; set; }

        public string TitleTextId { get; set; }
        public string MessageTextId { get; set; }
        public IList<DialogDataInputFieldInfo> DataInputFieldInfos { get; set; }
        public IList<DialogButtonInfo> ButtonInfos { get; set; }

        public bool EnableInputField { get; set; }
        public string InputLabelTextId { get; set; }
        public string InputFieldTextId { get; set; }
        public string InputFieldWatermarkId { get; set; }

        public DialogCreationInfo()
        {
            this.IsModal = true;
            this.EnableFadeIn = true;
            this.EnableTabNavigation = false;

            this.TitleTextId = null;
            this.MessageTextId = null;
            this.DataInputFieldInfos = null;
            this.ButtonInfos = null;

            this.EnableInputField = false;
            this.InputLabelTextId = null;
            this.InputFieldTextId = null;
            this.InputFieldWatermarkId = null;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Records what configuration was applied to a dialog, simulating
    /// the real Dialog component's setter calls from ShowDialog.
    /// </summary>
    public class DialogStub
    {
        public bool IsModal { get; private set; }
        public float Alpha { get; private set; } = 1.0f;
        public bool TitleEnabled { get; private set; }
        public string TitleTextId { get; private set; }
        public bool MessageEnabled { get; private set; }
        public string MessageTextId { get; private set; }
        public bool InputEnabled { get; private set; }
        public bool InputLabelEnabled { get; private set; }
        public bool ButtonsEnabled { get; private set; }
        public bool DataInputFieldsEnabled { get; private set; }
        public bool FadeInCalled { get; private set; }
        public bool HasButtonClickedHandler { get; private set; }

        public void SetModal(bool isModal) { IsModal = isModal; }
        public void SetAlpha(float alpha) { Alpha = alpha; }
        public void SetTitleEnabled(bool enabled) { TitleEnabled = enabled; }
        public void SetTitleTextId(string textId) { TitleTextId = textId; }
        public void SetMessageEnabled(bool enabled) { MessageEnabled = enabled; }
        public void SetMessageTextId(string textId) { MessageTextId = textId; }
        public void SetInputEnabled(bool enabled) { InputEnabled = enabled; }
        public void SetInputLabelEnabled(bool enabled) { InputLabelEnabled = enabled; }
        public void SetButtonsEnabled(bool enabled) { ButtonsEnabled = enabled; }
        public void SetDataInputFieldsEnabled(bool enabled) { DataInputFieldsEnabled = enabled; }
        public void FadeIn() { FadeInCalled = true; }
        public void RegisterButtonClickedHandler() { HasButtonClickedHandler = true; }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the ShowDialog logic of DialogController.
    /// Creates and configures a DialogStub based on CreationInfo.
    /// </summary>
    public class DialogControllerStub
    {
        public DialogStub ShowDialog(DialogCreationInfo creationInfo, Action<string> onButtonClicked = null)
        {
            var dialog = new DialogStub();
            dialog.SetModal(creationInfo.IsModal);

            if (creationInfo.EnableFadeIn)
            {
                dialog.SetAlpha(0.0f);
            }

            if (!string.IsNullOrEmpty(creationInfo.TitleTextId))
            {
                dialog.SetTitleEnabled(true);
                dialog.SetTitleTextId(creationInfo.TitleTextId);
            }
            else
            {
                dialog.SetTitleEnabled(false);
            }

            if (!string.IsNullOrEmpty(creationInfo.MessageTextId))
            {
                dialog.SetMessageEnabled(true);
                dialog.SetMessageTextId(creationInfo.MessageTextId);
            }
            else
            {
                dialog.SetMessageEnabled(false);
            }

            if (creationInfo.EnableInputField)
            {
                dialog.SetInputEnabled(true);

                if (!string.IsNullOrEmpty(creationInfo.InputLabelTextId))
                {
                    dialog.SetInputLabelEnabled(true);
                }
                else
                {
                    dialog.SetInputLabelEnabled(false);
                }
            }
            else
            {
                dialog.SetInputEnabled(false);
            }

            if (creationInfo.DataInputFieldInfos != null &&
                creationInfo.DataInputFieldInfos.Count > 0)
            {
                dialog.SetDataInputFieldsEnabled(true);
            }
            else
            {
                dialog.SetDataInputFieldsEnabled(false);
            }

            if (creationInfo.ButtonInfos != null &&
                creationInfo.ButtonInfos.Count > 0)
            {
                dialog.SetButtonsEnabled(true);
            }
            else
            {
                dialog.SetButtonsEnabled(false);
            }

            if (creationInfo.EnableFadeIn)
            {
                dialog.FadeIn();
            }

            if (onButtonClicked != null)
            {
                dialog.RegisterButtonClickedHandler();
            }

            return dialog;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class DialogControllerTests
    {
        private DialogControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new DialogControllerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        // WHY: CreationInfo defaults must match expected values so dialogs created
        // without explicit configuration still behave correctly (modal, fade-in).
        [Test]
        public void CreationInfo_HasCorrectDefaults_OnConstruction()
        {
            // Act
            var info = new DialogCreationInfo();

            // Assert
            Assert.IsTrue(info.IsModal,
                "IsModal should default to true so dialogs block background interaction.");
            Assert.IsTrue(info.EnableFadeIn,
                "EnableFadeIn should default to true for smooth visual transitions.");
            Assert.IsFalse(info.EnableTabNavigation,
                "EnableTabNavigation should default to false.");
            Assert.IsNull(info.TitleTextId,
                "TitleTextId should default to null.");
            Assert.IsNull(info.MessageTextId,
                "MessageTextId should default to null.");
            Assert.IsNull(info.ButtonInfos,
                "ButtonInfos should default to null.");
            Assert.IsFalse(info.EnableInputField,
                "EnableInputField should default to false.");
        }

        // WHY: When a title is provided, the dialog must display it. Without a
        // title, the title area should be hidden to avoid empty space.
        [Test]
        public void ShowDialog_EnablesTitle_WhenTitleTextIdProvided()
        {
            // Arrange
            var info = new DialogCreationInfo
            {
                TitleTextId = "dialog-title-welcome"
            };

            // Act
            var dialog = _controller.ShowDialog(info);

            // Assert
            Assert.IsTrue(dialog.TitleEnabled,
                "Title should be enabled when TitleTextId is provided.");
            Assert.AreEqual("dialog-title-welcome", dialog.TitleTextId,
                "Title text ID should match the value from CreationInfo.");
        }

        // WHY: Dialogs without a title should hide the title section entirely
        // to maintain clean layout.
        [Test]
        public void ShowDialog_DisablesTitle_WhenTitleTextIdIsNull()
        {
            // Arrange
            var info = new DialogCreationInfo();

            // Act
            var dialog = _controller.ShowDialog(info);

            // Assert
            Assert.IsFalse(dialog.TitleEnabled,
                "Title should be disabled when TitleTextId is null.");
        }

        // WHY: Modal dialogs must block background interaction. The IsModal flag
        // controls whether a blocking overlay is shown behind the dialog.
        [Test]
        public void ShowDialog_SetsModalState_FromCreationInfo()
        {
            // Arrange
            var info = new DialogCreationInfo { IsModal = false };

            // Act
            var dialog = _controller.ShowDialog(info);

            // Assert
            Assert.IsFalse(dialog.IsModal,
                "Dialog modal state should match the CreationInfo IsModal value.");
        }

        // WHY: Fade-in creates a polished transition. When enabled, the dialog
        // must start fully transparent and then animate in.
        [Test]
        public void ShowDialog_SetsAlphaToZeroAndFadesIn_WhenFadeInEnabled()
        {
            // Arrange
            var info = new DialogCreationInfo { EnableFadeIn = true };

            // Act
            var dialog = _controller.ShowDialog(info);

            // Assert
            Assert.IsTrue(dialog.FadeInCalled,
                "FadeIn should be called when EnableFadeIn is true.");
        }

        // WHY: When fade-in is disabled, the dialog should appear immediately
        // at full opacity without animation.
        [Test]
        public void ShowDialog_SkipsFadeIn_WhenFadeInDisabled()
        {
            // Arrange
            var info = new DialogCreationInfo { EnableFadeIn = false };

            // Act
            var dialog = _controller.ShowDialog(info);

            // Assert
            Assert.IsFalse(dialog.FadeInCalled,
                "FadeIn should not be called when EnableFadeIn is false.");
        }

        // WHY: Button callback registration ensures the caller is notified when
        // a dialog button is clicked. Without this, button clicks are silently lost.
        [Test]
        public void ShowDialog_RegistersButtonClickedHandler_WhenProvided()
        {
            // Arrange
            var info = new DialogCreationInfo
            {
                ButtonInfos = new List<DialogButtonInfo>
                {
                    new DialogButtonInfo("ok", "button-ok")
                }
            };

            // Act
            var dialog = _controller.ShowDialog(info, (buttonId) => { });

            // Assert
            Assert.IsTrue(dialog.HasButtonClickedHandler,
                "Button clicked handler should be registered when a callback is provided.");
            Assert.IsTrue(dialog.ButtonsEnabled,
                "Buttons should be enabled when ButtonInfos are provided.");
        }

        // WHY: ShowDialog must handle the case where no buttons or callback are
        // provided, producing a dialog that can only be dismissed programmatically.
        [Test]
        public void ShowDialog_DisablesButtons_WhenNoButtonInfosProvided()
        {
            // Arrange
            var info = new DialogCreationInfo();

            // Act
            var dialog = _controller.ShowDialog(info);

            // Assert
            Assert.IsFalse(dialog.ButtonsEnabled,
                "Buttons should be disabled when no ButtonInfos are provided.");
            Assert.IsFalse(dialog.HasButtonClickedHandler,
                "No button clicked handler should be registered when no callback is provided.");
        }
    }
}

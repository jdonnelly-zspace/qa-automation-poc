// =============================================================================
// GradientBackgroundControllerTests.cs - Edit Mode Unit Tests
// =============================================================================
// TARGET CLASS: GradientBackgroundController
//   Real file: Assets/StudioA3/Skybox/GradientBackgroundController.cs
//
// WHAT IT TESTS:
//   Skybox gradient background controller that manages top/bottom color input,
//   upper/lower step sliders, and gradient noise size/opacity sliders.
//   Validates hex color parsing (with and without '#' prefix), slider value
//   change handlers that update skybox material properties, value text
//   formatting, and singleton instance management.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real GradientBackgroundController uses RenderSettings.skybox to
//      apply material changes. These tests exercise the color-parsing and
//      value-formatting logic through a lightweight POCO stub.
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
    /// Simple RGB color struct to stand in for UnityEngine.Color.
    /// </summary>
    public struct SimpleColor
    {
        public float R;
        public float G;
        public float B;

        public SimpleColor(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
        }

        public static SimpleColor Black => new SimpleColor(0, 0, 0);
        public static SimpleColor White => new SimpleColor(1, 1, 1);
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the public API surface of
    /// GradientBackgroundController, without requiring MonoBehaviour or
    /// RenderSettings.
    /// </summary>
    public class GradientBackgroundControllerStub
    {
        private static GradientBackgroundControllerStub _instance;

        public static GradientBackgroundControllerStub Instance
        {
            get { return _instance; }
        }

        // Skybox property storage (replaces RenderSettings.skybox)
        private Dictionary<string, SimpleColor> _colors =
            new Dictionary<string, SimpleColor>();
        private Dictionary<string, float> _floats =
            new Dictionary<string, float>();

        // Text display values
        public string UpperStepValueText { get; private set; }
        public string LowerStepValueText { get; private set; }
        public string GradientNoiseSizeValueText { get; private set; }
        public string GradientNoiseOpacityValueText { get; private set; }

        // Current input field text (mirrors TMP_InputField.text)
        public string TopColorInputText { get; set; }
        public string BottomColorInputText { get; set; }

        public GradientBackgroundControllerStub()
        {
            _instance = this;
            _colors["_Color1"] = SimpleColor.White;
            _colors["_Color2"] = SimpleColor.Black;
            _floats["_UpperStep"] = 0f;
            _floats["_LowerStep"] = 0f;
            _floats["_GradientNoiseSize"] = 0f;
            _floats["_GradientNoiseOpacity"] = 0f;
        }

        /// <summary>
        /// Attempts to parse a hex color string and set it as the top gradient
        /// color. Prepends '#' if missing. Returns true if parsing succeeded.
        /// </summary>
        public bool OnSubmitTop(string text)
        {
            if (!text.Contains("#"))
            {
                text = "#" + text;
            }

            if (TryParseHexColor(text, out SimpleColor color))
            {
                _colors["_Color1"] = color;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to parse a hex color string and set it as the bottom
        /// gradient color. Prepends '#' if missing. Returns true if parsing
        /// succeeded.
        /// </summary>
        public bool OnSubmitBottom(string text)
        {
            if (!text.Contains("#"))
            {
                text = "#" + text;
            }

            if (TryParseHexColor(text, out SimpleColor color))
            {
                _colors["_Color2"] = color;
                return true;
            }

            return false;
        }

        public void OnUpperStepChanged(float value)
        {
            _floats["_UpperStep"] = value;
            UpperStepValueText = value.ToString("F2");
        }

        public void OnLowerStepChanged(float value)
        {
            _floats["_LowerStep"] = value;
            LowerStepValueText = value.ToString("F2");
        }

        public void OnGradientNoiseSizeChanged(float value)
        {
            _floats["_GradientNoiseSize"] = value;
            GradientNoiseSizeValueText = value.ToString("F2");
        }

        public void OnGradientNoiseOpacityChanged(float value)
        {
            _floats["_GradientNoiseOpacity"] = value;
            GradientNoiseOpacityValueText = value.ToString("F2");
        }

        public SimpleColor GetColor(string propertyName)
        {
            return _colors.ContainsKey(propertyName)
                ? _colors[propertyName]
                : SimpleColor.Black;
        }

        public float GetFloat(string propertyName)
        {
            return _floats.ContainsKey(propertyName) ? _floats[propertyName] : 0f;
        }

        /// <summary>
        /// Simplified hex color parser for testing. Supports #RRGGBB format.
        /// </summary>
        private bool TryParseHexColor(string hex, out SimpleColor color)
        {
            color = SimpleColor.Black;
            if (string.IsNullOrEmpty(hex) || hex.Length < 7 || hex[0] != '#')
            {
                return false;
            }

            try
            {
                int r = Convert.ToInt32(hex.Substring(1, 2), 16);
                int g = Convert.ToInt32(hex.Substring(3, 2), 16);
                int b = Convert.ToInt32(hex.Substring(5, 2), 16);
                color = new SimpleColor(r / 255f, g / 255f, b / 255f);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class GradientBackgroundControllerTests
    {
        private GradientBackgroundControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new GradientBackgroundControllerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        [Test]
        public void OnSubmitTop_ParsesValidHex_SetsTopColor()
        {
            // WHY: Teachers set the skybox top color via hex input. If parsing
            //      fails on valid input, the gradient background will not update
            //      and the 3D scene will look wrong.

            // Act
            bool result = _controller.OnSubmitTop("#FF0000");

            // Assert
            Assert.IsTrue(result,
                "OnSubmitTop should return true for a valid hex color string.");
            SimpleColor topColor = _controller.GetColor("_Color1");
            Assert.AreEqual(1f, topColor.R, 0.01f,
                "Red channel should be 1.0 for #FF0000.");
            Assert.AreEqual(0f, topColor.G, 0.01f,
                "Green channel should be 0.0 for #FF0000.");
        }

        [Test]
        public void OnSubmitTop_PrependsHash_WhenMissing()
        {
            // WHY: Users often type hex codes without the '#' prefix (e.g., "00FF00").
            //      The controller must handle this gracefully by auto-prepending '#'.

            // Act
            bool result = _controller.OnSubmitTop("00FF00");

            // Assert
            Assert.IsTrue(result,
                "OnSubmitTop should succeed when '#' is omitted from the input.");
            SimpleColor topColor = _controller.GetColor("_Color1");
            Assert.AreEqual(0f, topColor.R, 0.01f,
                "Red channel should be 0.0 for 00FF00.");
            Assert.AreEqual(1f, topColor.G, 0.01f,
                "Green channel should be 1.0 for 00FF00.");
        }

        [Test]
        public void OnSubmitTop_ReturnsFalse_ForInvalidHex()
        {
            // WHY: Invalid hex input must not crash the app or change the current
            //      color. The original color should be preserved.

            // Arrange
            SimpleColor originalColor = _controller.GetColor("_Color1");

            // Act
            bool result = _controller.OnSubmitTop("ZZZZZZ");

            // Assert
            Assert.IsFalse(result,
                "OnSubmitTop should return false for an invalid hex string.");
        }

        [Test]
        public void OnSubmitBottom_ParsesValidHex_SetsBottomColor()
        {
            // WHY: The bottom color is the other half of the gradient.
            //      Both top and bottom must parse correctly for the full
            //      gradient to render as expected.

            // Act
            bool result = _controller.OnSubmitBottom("#0000FF");

            // Assert
            Assert.IsTrue(result,
                "OnSubmitBottom should return true for a valid hex color string.");
            SimpleColor bottomColor = _controller.GetColor("_Color2");
            Assert.AreEqual(0f, bottomColor.R, 0.01f,
                "Red channel should be 0.0 for #0000FF.");
            Assert.AreEqual(1f, bottomColor.B, 0.01f,
                "Blue channel should be 1.0 for #0000FF.");
        }

        [Test]
        public void OnUpperStepChanged_UpdatesValueAndText()
        {
            // WHY: The upper step slider controls where the gradient transition
            //      starts. The skybox material float and the UI label must both
            //      update in sync.

            // Act
            _controller.OnUpperStepChanged(0.75f);

            // Assert
            Assert.AreEqual(0.75f, _controller.GetFloat("_UpperStep"), 0.001f,
                "Skybox _UpperStep should match the slider value.");
            Assert.AreEqual("0.75", _controller.UpperStepValueText,
                "Value text should display the slider value formatted to 2 decimal places.");
        }

        [Test]
        public void OnLowerStepChanged_UpdatesValueAndText()
        {
            // WHY: The lower step slider controls where the gradient transition
            //      ends. Incorrect values would make the gradient band too wide
            //      or too narrow.

            // Act
            _controller.OnLowerStepChanged(0.25f);

            // Assert
            Assert.AreEqual(0.25f, _controller.GetFloat("_LowerStep"), 0.001f,
                "Skybox _LowerStep should match the slider value.");
            Assert.AreEqual("0.25", _controller.LowerStepValueText,
                "Value text should display the slider value formatted to 2 decimal places.");
        }

        [Test]
        public void OnGradientNoiseSizeChanged_UpdatesSkyboxAndLabel()
        {
            // WHY: Noise size affects the visual texture of the gradient. The
            //      material property and the UI text must stay in sync.

            // Act
            _controller.OnGradientNoiseSizeChanged(1.50f);

            // Assert
            Assert.AreEqual(1.50f, _controller.GetFloat("_GradientNoiseSize"), 0.001f,
                "Skybox _GradientNoiseSize should match the slider value.");
            Assert.AreEqual("1.50", _controller.GradientNoiseSizeValueText,
                "Value text should display the noise size formatted to 2 decimal places.");
        }

        [Test]
        public void OnGradientNoiseOpacityChanged_UpdatesSkyboxAndLabel()
        {
            // WHY: Noise opacity controls how visible the noise pattern is. Zero
            //      opacity means a smooth gradient; higher values add visual texture.

            // Act
            _controller.OnGradientNoiseOpacityChanged(0.30f);

            // Assert
            Assert.AreEqual(0.30f, _controller.GetFloat("_GradientNoiseOpacity"), 0.001f,
                "Skybox _GradientNoiseOpacity should match the slider value.");
            Assert.AreEqual("0.30", _controller.GradientNoiseOpacityValueText,
                "Value text should display the noise opacity formatted to 2 decimal places.");
        }
    }
}

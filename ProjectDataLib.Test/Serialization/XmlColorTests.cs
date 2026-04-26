using Xunit;
using System.Drawing;
using ProjectDataLib;

namespace ProjectDataLib.Test.Serialization
{
    public class XmlColorTests
    {
        [Fact]
        public void Constructor_DefaultCreatesBlackColor()
        {
            // Arrange & Act
            var xmlColor = new XmlColor();

            // Assert
            Assert.Equal(Color.Black, xmlColor.ToColor());
        }

        [Fact]
        public void Constructor_WithColor_StoresColor()
        {
            // Arrange
            var color = Color.Red;

            // Act
            var xmlColor = new XmlColor(color);

            // Assert
            Assert.Equal(color, xmlColor.ToColor());
        }

        [Fact]
        public void ToColor_ReturnsStoredColor()
        {
            // Arrange
            var color = Color.Blue;
            var xmlColor = new XmlColor(color);

            // Act
            var result = xmlColor.ToColor();

            // Assert
            Assert.Equal(color, result);
        }

        [Fact]
        public void FromColor_UpdatesStoredColor()
        {
            // Arrange
            var xmlColor = new XmlColor(Color.Black);
            var newColor = Color.Green;

            // Act
            xmlColor.FromColor(newColor);

            // Assert
            Assert.Equal(newColor, xmlColor.ToColor());
        }

        [Fact]
        public void Val_Property_ConvertsColorToHtml()
        {
            // Arrange
            var xmlColor = new XmlColor(Color.Red);

            // Act
            var val = xmlColor.Val;

            // Assert
            Assert.NotNull(val);
            Assert.NotEmpty(val);
        }

        [Fact]
        public void Val_PropertySetter_ConvertsHtmlToColor()
        {
            // Arrange
            var xmlColor = new XmlColor();

            // Act
            xmlColor.Val = "#FF0000"; // Red in HTML format

            // Assert
            var color = xmlColor.ToColor();
            Assert.Equal(255, color.R); // Red channel
            Assert.Equal(0, color.G);   // Green channel
            Assert.Equal(0, color.B);   // Blue channel
        }

        [Fact]
        public void Val_PropertySetter_WithInvalidValue_DefaultsToBlack()
        {
            // Arrange
            var xmlColor = new XmlColor(Color.Red);

            // Act
            xmlColor.Val = "InvalidColorValue";

            // Assert
            Assert.Equal(Color.Black, xmlColor.ToColor());
        }

        [Fact]
        public void Alpha_Property_ReturnsAlphaComponent()
        {
            // Arrange
            var color = Color.FromArgb(128, 255, 0, 0);
            var xmlColor = new XmlColor(color);

            // Act
            var alpha = xmlColor.Alpha;

            // Assert
            Assert.Equal(128, alpha);
        }

        [Fact]
        public void Alpha_PropertySetter_UpdatesAlphaComponent()
        {
            // Arrange
            var xmlColor = new XmlColor(Color.Red);

            // Act
            xmlColor.Alpha = 128;

            // Assert
            Assert.Equal(128, xmlColor.Alpha);
        }

        [Fact]
        public void ImplicitOperator_ColorToXmlColor()
        {
            // Arrange
            var color = Color.Blue;

            // Act
            XmlColor xmlColor = color;

            // Assert
            Assert.Equal(color, xmlColor.ToColor());
        }

        [Fact]
        public void ImplicitOperator_XmlColorToColor()
        {
            // Arrange
            var xmlColor = new XmlColor(Color.Green);

            // Act
            Color color = xmlColor;

            // Assert
            Assert.Equal(Color.Green, color);
        }

        [Fact]
        public void ShouldSerializeAlpha_ReturnsTrueWhenAlphaNotMaximum()
        {
            // Arrange
            var xmlColor = new XmlColor(Color.FromArgb(128, 255, 0, 0));

            // Act
            var shouldSerialize = xmlColor.ShouldSerializeAlpha();

            // Assert
            Assert.True(shouldSerialize);
        }

        [Fact]
        public void ShouldSerializeAlpha_ReturnsFalseWhenAlphaIsMaximum()
        {
            // Arrange
            var xmlColor = new XmlColor(Color.Red); // Default alpha is 0xFF

            // Act
            var shouldSerialize = xmlColor.ShouldSerializeAlpha();

            // Assert
            Assert.False(shouldSerialize);
        }
    }
}

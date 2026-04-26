using Xunit;
using ProjectDataLib;

namespace ProjectDataLib.Test.Helpers
{
    public class InformationTests
    {
        [Fact]
        public void Constructor_CreatesExceptionWithMessage()
        {
            // Arrange
            string message = "Test information message";

            // Act
            var info = new Information(message);

            // Assert
            Assert.Equal(message, info.Message);
        }

        [Fact]
        public void ToString_ReturnsHyphen()
        {
            // Arrange
            var info = new Information("Any message");

            // Act
            var result = info.ToString();

            // Assert
            Assert.Equal("-", result);
        }

        [Fact]
        public void InheritsFromException()
        {
            // Arrange & Act
            var info = new Information("Test");

            // Assert
            Assert.IsAssignableFrom<Exception>(info);
        }

        [Fact]
        public void Constructor_WithEmptyMessage()
        {
            // Arrange
            string message = "";

            // Act
            var info = new Information(message);

            // Assert
            Assert.Equal(message, info.Message);
            Assert.Equal("-", info.ToString());
        }
    }
}

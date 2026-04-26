using Xunit;
using ProjectDataLib;

namespace ProjectDataLib.Test.Helpers
{
    public class CustomExceptionTests
    {
        [Fact]
        public void Constructor_SetsSenderAndException()
        {
            // Arrange
            var sender = new object();
            var exception = new ArgumentException("Test error");

            // Act
            var customEx = new CustomException(sender, exception);

            // Assert
            Assert.Same(sender, customEx.Sender);
            Assert.Same(exception, customEx.Ex);
        }

        [Fact]
        public void Constructor_SetsCzasToCurrentTime()
        {
            // Arrange
            var before = DateTime.Now;
            var sender = new object();
            var exception = new Exception();

            // Act
            var customEx = new CustomException(sender, exception);
            var after = DateTime.Now;

            // Assert
            Assert.True(customEx.Czas >= before);
            Assert.True(customEx.Czas <= after);
        }

        [Fact]
        public void Constructor_WithNullSender()
        {
            // Arrange
            var exception = new Exception("Test");

            // Act
            var customEx = new CustomException(null, exception);

            // Assert
            Assert.Null(customEx.Sender);
            Assert.Same(exception, customEx.Ex);
        }

        [Fact]
        public void Constructor_WithNullException()
        {
            // Arrange
            var sender = new object();

            // Act
            var customEx = new CustomException(sender, null);

            // Assert
            Assert.Same(sender, customEx.Sender);
            Assert.Null(customEx.Ex);
        }

        [Fact]
        public void Ex_CanBeUpdatedAfterConstruction()
        {
            // Arrange
            var customEx = new CustomException(new object(), new Exception("Original"));
            var newException = new InvalidOperationException("New");

            // Act
            customEx.Ex = newException;

            // Assert
            Assert.Same(newException, customEx.Ex);
        }

        [Fact]
        public void Sender_CanBeUpdatedAfterConstruction()
        {
            // Arrange
            var originalSender = new object();
            var customEx = new CustomException(originalSender, new Exception());
            var newSender = new object();

            // Act
            customEx.Sender = newSender;

            // Assert
            Assert.Same(newSender, customEx.Sender);
        }
    }
}

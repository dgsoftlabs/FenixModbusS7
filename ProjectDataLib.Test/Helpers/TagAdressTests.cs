using Xunit;
using ProjectDataLib;

namespace ProjectDataLib.Test.Helpers
{
    public class TagAdressTests
    {
        [Fact]
        public void Constructor_SetsAddressAndSecondaryAddress()
        {
            // Arrange
            int address = 100;
            int secAddress = 5;

            // Act
            var tagAddress = new TagAdress(address, secAddress);

            // Assert
            Assert.Equal(address, tagAddress.adress);
            Assert.Equal(secAddress, tagAddress.secAdress);
        }

        [Fact]
        public void Constructor_WithZeroValues()
        {
            // Arrange & Act
            var tagAddress = new TagAdress(0, 0);

            // Assert
            Assert.Equal(0, tagAddress.adress);
            Assert.Equal(0, tagAddress.secAdress);
        }

        [Fact]
        public void Constructor_WithNegativeValues()
        {
            // Arrange
            int address = -100;
            int secAddress = -5;

            // Act
            var tagAddress = new TagAdress(address, secAddress);

            // Assert
            Assert.Equal(address, tagAddress.adress);
            Assert.Equal(secAddress, tagAddress.secAdress);
        }

        [Fact]
        public void Constructor_WithLargeValues()
        {
            // Arrange
            int address = int.MaxValue;
            int secAddress = int.MinValue;

            // Act
            var tagAddress = new TagAdress(address, secAddress);

            // Assert
            Assert.Equal(address, tagAddress.adress);
            Assert.Equal(secAddress, tagAddress.secAdress);
        }
    }
}

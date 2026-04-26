using Xunit;
using ProjectDataLib;

namespace ProjectDataLib.Test.Enums
{
    public class EnumTests
    {
        [Fact]
        public void TypeData_ContainsExpectedValues()
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(TypeData), TypeData.BIT));
            Assert.True(System.Enum.IsDefined(typeof(TypeData), TypeData.BYTE));
            Assert.True(System.Enum.IsDefined(typeof(TypeData), TypeData.SBYTE));
            Assert.True(System.Enum.IsDefined(typeof(TypeData), TypeData.CHAR));
            Assert.True(System.Enum.IsDefined(typeof(TypeData), TypeData.DOUBLE));
            Assert.True(System.Enum.IsDefined(typeof(TypeData), TypeData.FLOAT));
            Assert.True(System.Enum.IsDefined(typeof(TypeData), TypeData.INT));
            Assert.True(System.Enum.IsDefined(typeof(TypeData), TypeData.UINT));
            Assert.True(System.Enum.IsDefined(typeof(TypeData), TypeData.SHORT));
            Assert.True(System.Enum.IsDefined(typeof(TypeData), TypeData.USHORT));
            Assert.True(System.Enum.IsDefined(typeof(TypeData), TypeData.ShortToReal));
        }

        [Fact]
        public void BytesOrder_ContainsExpectedValues()
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(BytesOrder), BytesOrder.BADC));
            Assert.True(System.Enum.IsDefined(typeof(BytesOrder), BytesOrder.ABCD));
            Assert.True(System.Enum.IsDefined(typeof(BytesOrder), BytesOrder.DCBA));
        }

        [Theory]
        [InlineData(TypeData.BIT)]
        [InlineData(TypeData.BYTE)]
        [InlineData(TypeData.INT)]
        [InlineData(TypeData.FLOAT)]
        public void TypeData_CanBeCompared(TypeData type)
        {
            // Arrange
            var type1 = type;
            var type2 = type;

            // Act & Assert
            Assert.Equal(type1, type2);
        }

        [Theory]
        [InlineData(BytesOrder.ABCD)]
        [InlineData(BytesOrder.BADC)]
        [InlineData(BytesOrder.DCBA)]
        public void BytesOrder_CanBeCompared(BytesOrder order)
        {
            // Arrange
            var order1 = order;
            var order2 = order;

            // Act & Assert
            Assert.Equal(order1, order2);
        }
    }
}

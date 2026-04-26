using Xunit;
using ProjectDataLib;

namespace ProjectDataLib.Test.Models
{
    public class CustomTimerTests
    {
        [Fact]
        public void Constructor_Default_CreatesInstanceWithDefaults()
        {
            // Act
            var timer = new CustomTimer();

            // Assert
            Assert.NotNull(timer);
            Assert.Equal("Timer", timer.Name);
            Assert.Equal(1000, timer.Time);
            Assert.Equal(0, timer.Delay);
        }

        [Fact]
        public void Name_SetValue_UpdatesProperty()
        {
            // Arrange
            var timer = new CustomTimer();
            var expectedName = "CustomTimer1";

            // Act
            timer.Name = expectedName;

            // Assert
            Assert.Equal(expectedName, timer.Name);
        }

        [Theory]
        [InlineData("Timer1")]
        [InlineData("FastTimer")]
        [InlineData("")]
        [InlineData("A")]
        public void Name_SetVariousValues_UpdatesCorrectly(string name)
        {
            // Arrange
            var timer = new CustomTimer();

            // Act
            timer.Name = name;

            // Assert
            Assert.Equal(name, timer.Name);
        }

        [Fact]
        public void Time_SetValue_UpdatesProperty()
        {
            // Arrange
            var timer = new CustomTimer();
            int expectedTime = 5000;

            // Act
            timer.Time = expectedTime;

            // Assert
            Assert.Equal(expectedTime, timer.Time);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(int.MaxValue)]
        public void Time_SetVariousValues_UpdatesCorrectly(int time)
        {
            // Arrange
            var timer = new CustomTimer();

            // Act
            timer.Time = time;

            // Assert
            Assert.Equal(time, timer.Time);
        }

        [Fact]
        public void Delay_SetValue_UpdatesProperty()
        {
            // Arrange
            var timer = new CustomTimer();
            int expectedDelay = 500;

            // Act
            timer.Delay = expectedDelay;

            // Assert
            Assert.Equal(expectedDelay, timer.Delay);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(5000)]
        [InlineData(int.MaxValue)]
        public void Delay_SetVariousValues_UpdatesCorrectly(int delay)
        {
            // Arrange
            var timer = new CustomTimer();

            // Act
            timer.Delay = delay;

            // Assert
            Assert.Equal(delay, timer.Delay);
        }

        [Fact]
        public void IComparable_CompareTo_ComparesTimeProperty()
        {
            // Arrange
            var timer1 = new CustomTimer { Time = 1000 };
            var timer2 = new CustomTimer { Time = 2000 };
            var timer3 = new CustomTimer { Time = 1000 };

            // Act
            var comparison12 = ((IComparable<CustomTimer>)timer1).CompareTo(timer2);
            var comparison21 = ((IComparable<CustomTimer>)timer2).CompareTo(timer1);
            var comparison13 = ((IComparable<CustomTimer>)timer1).CompareTo(timer3);

            // Assert
            Assert.True(comparison12 < 0, "timer1 (1000ms) should be less than timer2 (2000ms)");
            Assert.True(comparison21 > 0, "timer2 (2000ms) should be greater than timer1 (1000ms)");
            Assert.Equal(0, comparison13);  // Equal times
        }

        [Theory]
        [InlineData(500, 1000, -1)]  // 500 < 1000
        [InlineData(1000, 500, 1)]   // 1000 > 500
        [InlineData(2000, 2000, 0)]  // 2000 == 2000
        [InlineData(0, 1, -1)]       // 0 < 1
        [InlineData(100, 100, 0)]    // 100 == 100
        public void IComparable_CompareTo_WithVariousTimes_ReturnsCorrectComparison(int time1, int time2, int expectedSign)
        {
            // Arrange
            var timer1 = new CustomTimer { Time = time1 };
            var timer2 = new CustomTimer { Time = time2 };

            // Act
            var result = ((IComparable<CustomTimer>)timer1).CompareTo(timer2);

            // Assert
            if (expectedSign == -1)
                Assert.True(result < 0);
            else if (expectedSign == 0)
                Assert.Equal(0, result);
            else
                Assert.True(result > 0);
        }

        [Fact]
        public void IsComparable_ImplementsInterface()
        {
            // Arrange
            var timer = new CustomTimer();

            // Act & Assert
            Assert.IsAssignableFrom<IComparable<CustomTimer>>(timer);
        }

        [Fact]
        public void IsSerializable_HasSerializableAttribute()
        {
            // Arrange & Act
            var timerType = typeof(CustomTimer);

            // Assert
            Assert.True(timerType.IsSerializable);
        }

        [Fact]
        public void MultipleProperties_CanBeSetIndependently()
        {
            // Arrange
            var timer = new CustomTimer();

            // Act
            timer.Name = "MyTimer";
            timer.Time = 3000;
            timer.Delay = 500;

            // Assert
            Assert.Equal("MyTimer", timer.Name);
            Assert.Equal(3000, timer.Time);
            Assert.Equal(500, timer.Delay);
        }

        [Fact]
        public void Delay_CanBeNegative()
        {
            // Arrange
            var timer = new CustomTimer();

            // Act
            timer.Delay = -100;

            // Assert
            Assert.Equal(-100, timer.Delay);
        }

        [Fact]
        public void Time_CanBeNegative()
        {
            // Arrange
            var timer = new CustomTimer();

            // Act
            timer.Time = -1000;

            // Assert
            Assert.Equal(-1000, timer.Time);
        }
    }
}

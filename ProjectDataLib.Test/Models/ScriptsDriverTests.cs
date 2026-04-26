using Xunit;
using ProjectDataLib;
using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace ProjectDataLib.Test.Models
{
    public class ScriptsDriverTests
    {
        [Fact]
        public void Constructor_Default_CreatesInstance()
        {
            // Act
            var driver = new ScriptsDriver();

            // Assert
            Assert.NotNull(driver);
        }

        [Fact]
        public void ObjId_CanBeSet()
        {
            // Arrange
            var driver = new ScriptsDriver();
            var newGuid = Guid.NewGuid();

            // Act
            driver.objId = newGuid;

            // Assert
            Assert.Equal(newGuid, driver.objId);
        }

        [Fact]
        public void ObjId_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var driver = new ScriptsDriver();
            bool eventRaised = false;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == "objId")
                    eventRaised = true;
            };

            var notifyingDriver = (INotifyPropertyChanged)driver;
            notifyingDriver.PropertyChanged += handler;

            // Act
            driver.objId = Guid.NewGuid();

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void Proj_CanBeSet()
        {
            // Arrange
            var driver = new ScriptsDriver();
            var project = new Project();

            // Act
            driver.Proj = project;

            // Assert
            Assert.Same(project, driver.Proj);
        }

        [Fact]
        public void Proj_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var driver = new ScriptsDriver();
            var project = new Project();
            bool eventRaised = false;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == "Proj")
                    eventRaised = true;
            };

            var notifyingDriver = (INotifyPropertyChanged)driver;
            notifyingDriver.PropertyChanged += handler;

            // Act
            driver.Proj = project;

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void Proj_CanBeSetNull()
        {
            // Arrange
            var driver = new ScriptsDriver();

            // Act
            driver.Proj = null;

            // Assert
            Assert.Null(driver.Proj);
        }

        [Fact]
        public void IsExpand_DefaultValue_IsFalse()
        {
            // Arrange & Act
            var driver = new ScriptsDriver();

            // Assert
            Assert.False(driver.isExpand);
        }

        [Fact]
        public void IsExpand_CanBeSetTrue()
        {
            // Arrange
            var driver = new ScriptsDriver();

            // Act
            driver.isExpand = true;

            // Assert
            Assert.True(driver.isExpand);
        }

        [Fact]
        public void IsExpand_CanBeSetFalse()
        {
            // Arrange
            var driver = new ScriptsDriver { isExpand = true };

            // Act
            driver.isExpand = false;

            // Assert
            Assert.False(driver.isExpand);
        }

        [Fact]
        public void IsExpand_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var driver = new ScriptsDriver();
            bool eventRaised = false;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == "isExpand")
                    eventRaised = true;
            };

            var notifyingDriver = (INotifyPropertyChanged)driver;
            notifyingDriver.PropertyChanged += handler;

            // Act
            driver.isExpand = true;

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void Timers_CanBeSet()
        {
            // Arrange
            var driver = new ScriptsDriver();
            var timers = new List<CustomTimer> { new CustomTimer { Name = "Timer1" } };

            // Act
            driver.Timers = timers;

            // Assert
            Assert.Same(timers, driver.Timers);
        }

        [Fact]
        public void Timers_CanBeSetNull()
        {
            // Arrange
            var driver = new ScriptsDriver();

            // Act
            driver.Timers = null;

            // Assert
            Assert.Null(driver.Timers);
        }

        [Fact]
        public void IDriverModel_ImplementsInterface()
        {
            // Arrange
            var driver = new ScriptsDriver();

            // Act & Assert
            Assert.IsAssignableFrom<IDriverModel>(driver);
        }

        [Fact]
        public void ITreeViewModel_ImplementsInterface()
        {
            // Arrange
            var driver = new ScriptsDriver();

            // Act & Assert
            Assert.IsAssignableFrom<ITreeViewModel>(driver);
        }

        [Fact]
        public void INotifyPropertyChanged_ImplementsInterface()
        {
            // Arrange
            var driver = new ScriptsDriver();

            // Act & Assert
            Assert.IsAssignableFrom<INotifyPropertyChanged>(driver);
        }

        [Fact]
        public void IsSerializable_HasSerializableAttribute()
        {
            // Arrange & Act
            var driverType = typeof(ScriptsDriver);

            // Assert
            Assert.True(driverType.IsSerializable);
        }

        [Fact]
        public void MultipleProperties_CanBeSetIndependently()
        {
            // Arrange
            var driver = new ScriptsDriver();
            var objGuid = Guid.NewGuid();
            var project = new Project();
            var timers = new List<CustomTimer>();

            // Act
            driver.objId = objGuid;
            driver.Proj = project;
            driver.isExpand = true;
            driver.Timers = timers;

            // Assert
            Assert.Equal(objGuid, driver.objId);
            Assert.Same(project, driver.Proj);
            Assert.True(driver.isExpand);
            Assert.Same(timers, driver.Timers);
        }

        [Fact]
        public void MultipleInstances_CanHaveIndependentState()
        {
            // Arrange
            var driver1 = new ScriptsDriver();
            var driver2 = new ScriptsDriver();
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();

            // Act
            driver1.objId = guid1;
            driver2.objId = guid2;

            // Assert
            Assert.Equal(guid1, driver1.objId);
            Assert.Equal(guid2, driver2.objId);
            Assert.NotEqual(driver1.objId, driver2.objId);
        }
    }
}

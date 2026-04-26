using Xunit;
using ProjectDataLib;
using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace ProjectDataLib.Test.Models
{
    public class InternalTagsDriverTests
    {
        [Fact]
        public void Constructor_Default_CreatesInstance()
        {
            // Act
            var driver = new InternalTagsDriver();

            // Assert
            Assert.NotNull(driver);
        }

        [Fact]
        public void ObjId_CanBeSet()
        {
            // Arrange
            var driver = new InternalTagsDriver();
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
            var driver = new InternalTagsDriver();
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
            var driver = new InternalTagsDriver();
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
            var driver = new InternalTagsDriver();
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
            var driver = new InternalTagsDriver();

            // Act
            driver.Proj = null;

            // Assert
            Assert.Null(driver.Proj);
        }

        [Fact]
        public void IsExpand_DefaultValue_IsFalse()
        {
            // Arrange & Act
            var driver = new InternalTagsDriver();

            // Assert
            Assert.False(driver.isExpand);
        }

        [Fact]
        public void IsExpand_CanBeSetTrue()
        {
            // Arrange
            var driver = new InternalTagsDriver();

            // Act
            driver.isExpand = true;

            // Assert
            Assert.True(driver.isExpand);
        }

        [Fact]
        public void IsExpand_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var driver = new InternalTagsDriver();
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
            var driver = new InternalTagsDriver();
            var timers = new List<CustomTimer> { new CustomTimer() };

            // Act
            driver.Timers = timers;

            // Assert
            Assert.Same(timers, driver.Timers);
        }

        [Fact]
        public void IDriverModel_ImplementsInterface()
        {
            // Arrange
            var driver = new InternalTagsDriver();

            // Act & Assert
            Assert.IsAssignableFrom<IDriverModel>(driver);
        }

        [Fact]
        public void ITreeViewModel_ImplementsInterface()
        {
            // Arrange
            var driver = new InternalTagsDriver();

            // Act & Assert
            Assert.IsAssignableFrom<ITreeViewModel>(driver);
        }

        [Fact]
        public void INotifyPropertyChanged_ImplementsInterface()
        {
            // Arrange
            var driver = new InternalTagsDriver();

            // Act & Assert
            Assert.IsAssignableFrom<INotifyPropertyChanged>(driver);
        }

        [Fact]
        public void IsSerializable_HasSerializableAttribute()
        {
            // Arrange & Act
            var driverType = typeof(InternalTagsDriver);

            // Assert
            Assert.True(driverType.IsSerializable);
        }

        [Fact]
        public void MultipleProperties_CanBeSetIndependently()
        {
            // Arrange
            var driver = new InternalTagsDriver();
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
    }
}

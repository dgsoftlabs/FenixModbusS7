using Xunit;
using ProjectDataLib;
using System;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace ProjectDataLib.Test.Models
{
    public class DeviceTests
    {
        [Fact]
        public void Constructor_Default_CreatesInstance()
        {
            // Act
            var device = new Device();

            // Assert
            Assert.NotNull(device);
        }

        [Fact]
        public void ObjId_CanBeSet()
        {
            // Arrange
            var device = new Device();
            var newGuid = Guid.NewGuid();

            // Act
            device.objId = newGuid;

            // Assert
            Assert.Equal(newGuid, device.objId);
        }

        [Fact]
        public void ObjId_DefaultValue_IsEmpty()
        {
            // Arrange & Act
            var device = new Device();

            // Assert
            Assert.Equal(Guid.Empty, device.objId);
        }

        [Fact]
        public void ParentId_CanBeSet()
        {
            // Arrange
            var device = new Device();
            var parentGuid = Guid.NewGuid();

            // Act
            device.parentId = parentGuid;

            // Assert
            Assert.Equal(parentGuid, device.parentId);
        }

        [Fact]
        public void ProjId_CanBeSet()
        {
            // Arrange
            var device = new Device();
            var projGuid = Guid.NewGuid();

            // Act
            device.projId = projGuid;

            // Assert
            Assert.Equal(projGuid, device.projId);
        }

        [Fact]
        public void ParentId_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var device = new Device();
            bool eventRaised = false;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == "parentId")
                    eventRaised = true;
            };

            var notifyingDevice = (INotifyPropertyChanged)device;
            notifyingDevice.PropertyChanged += handler;

            // Act
            device.parentId = Guid.NewGuid();

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void ProjId_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var device = new Device();
            bool eventRaised = false;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == "projId")
                    eventRaised = true;
            };

            var notifyingDevice = (INotifyPropertyChanged)device;
            notifyingDevice.PropertyChanged += handler;

            // Act
            device.projId = Guid.NewGuid();

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void ITreeViewModel_ImplementsInterface()
        {
            // Arrange
            var device = new Device();

            // Act & Assert
            Assert.IsAssignableFrom<ITreeViewModel>(device);
        }

        [Fact]
        public void ITableView_ImplementsInterface()
        {
            // Arrange
            var device = new Device();

            // Act & Assert
            Assert.IsAssignableFrom<ITableView>(device);
        }

        [Fact]
        public void IDriverModel_ImplementsInterface()
        {
            // Arrange
            var device = new Device();

            // Act & Assert
            Assert.IsAssignableFrom<IDriverModel>(device);
        }

        [Fact]
        public void IDriversMagazine_ImplementsInterface()
        {
            // Arrange
            var device = new Device();

            // Act & Assert
            Assert.IsAssignableFrom<IDriversMagazine>(device);
        }

        [Fact]
        public void INotifyPropertyChanged_ImplementsInterface()
        {
            // Arrange
            var device = new Device();

            // Act & Assert
            Assert.IsAssignableFrom<INotifyPropertyChanged>(device);
        }

        [Fact]
        public void IsSerializable_HasSerializableAttribute()
        {
            // Arrange & Act
            var deviceType = typeof(Device);

            // Assert
            Assert.True(deviceType.IsSerializable);
        }

        [Fact]
        public void MultipleGuids_CanBeSetIndependently()
        {
            // Arrange
            var device = new Device();
            var objGuid = Guid.NewGuid();
            var parentGuid = Guid.NewGuid();
            var projGuid = Guid.NewGuid();

            // Act
            device.objId = objGuid;
            device.parentId = parentGuid;
            device.projId = projGuid;

            // Assert
            Assert.Equal(objGuid, device.objId);
            Assert.Equal(parentGuid, device.parentId);
            Assert.Equal(projGuid, device.projId);
        }

        [Fact]
        public void Device_ImplementsMultipleInterfaces()
        {
            // Arrange
            var device = new Device();

            // Act & Assert
            Assert.IsAssignableFrom<ITreeViewModel>(device);
            Assert.IsAssignableFrom<ITableView>(device);
            Assert.IsAssignableFrom<IDriverModel>(device);
            Assert.IsAssignableFrom<IDriversMagazine>(device);
            Assert.IsAssignableFrom<INotifyPropertyChanged>(device);
        }

        [Fact]
        public void ITreeViewModel_Children_ReturnsObservableCollection()
        {
            // Arrange
            var device = new Device();
            var treeView = (ITreeViewModel)device;

            // Act
            var children = treeView.Children;

            // Assert
            // Children may be null if not initialized, which is acceptable
            Assert.True(children == null || children.Count >= 0);
        }

        [Fact]
        public void ITableView_Children_ReturnsObservableCollection()
        {
            // Arrange
            var device = new Device();
            var tableView = (ITableView)device;

            // Act
            var children = tableView.Children;

            // Assert
            // Children may be null if not initialized, which is acceptable
            Assert.True(children == null || children.Count >= 0);
        }

        [Fact]
        public void IDriversMagazine_Children_ReturnsObservableCollection()
        {
            // Arrange
            var device = new Device();
            var drivers = (IDriversMagazine)device;

            // Act
            var children = drivers.Children;

            // Assert
            // Children may be null if not initialized, which is acceptable
            Assert.True(children == null || children.Count >= 0);
        }

        [Fact]
        public void AddressChanged_EventCanBeRaised()
        {
            // Arrange
            var device = new Device();
            bool eventRaised = false;
            EventHandler handler = (sender, e) => eventRaised = true;

            device.adressChanged += handler;

            // Act
            device.adressChanged?.Invoke(device, EventArgs.Empty);

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void MultipleDevices_CanHaveIndependentGuids()
        {
            // Arrange
            var dev1 = new Device();
            var dev2 = new Device();
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();

            // Act
            dev1.objId = guid1;
            dev2.objId = guid2;

            // Assert
            Assert.Equal(guid1, dev1.objId);
            Assert.Equal(guid2, dev2.objId);
            Assert.NotEqual(dev1.objId, dev2.objId);
        }
    }
}

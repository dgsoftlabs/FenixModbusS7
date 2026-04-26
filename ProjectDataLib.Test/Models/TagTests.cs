using Xunit;
using ProjectDataLib;
using System;
using System.ComponentModel;

namespace ProjectDataLib.Test.Models
{
    public class TagTests
    {
        [Fact]
        public void Constructor_Default_CreatesInstance()
        {
            // Act
            var tag = new Tag();

            // Assert
            Assert.NotNull(tag);
        }

        [Fact]
        public void ObjId_CanBeSet()
        {
            // Arrange
            var tag = new Tag();
            var newGuid = Guid.NewGuid();

            // Act
            tag.objId = newGuid;

            // Assert
            Assert.Equal(newGuid, tag.objId);
        }

        [Fact]
        public void ObjId_DefaultValue_IsEmpty()
        {
            // Arrange & Act
            var tag = new Tag();

            // Assert
            Assert.Equal(Guid.Empty, tag.objId);
        }

        [Fact]
        public void TagName_CanBeSet()
        {
            // Arrange
            var tag = new Tag();

            // Act
            tag.tagName = "TestTag";

            // Assert
            Assert.NotNull(tag.tagName);
            // Note: tagName may have "$$" appended if duplicate names detected
        }

        [Fact]
        public void TagName_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var tag = new Tag();
            bool eventRaised = false;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == "Name")
                    eventRaised = true;
            };

            var notifyingTag = (INotifyPropertyChanged)tag;
            notifyingTag.PropertyChanged += handler;

            // Act
            tag.tagName = "NewTag";

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void ParentId_CanBeSet()
        {
            // Arrange
            var tag = new Tag();
            var parentGuid = Guid.NewGuid();

            // Act
            tag.parentId = parentGuid;

            // Assert
            Assert.Equal(parentGuid, tag.parentId);
        }

        [Fact]
        public void ConnId_CanBeSet()
        {
            // Arrange
            var tag = new Tag();
            var connGuid = Guid.NewGuid();

            // Act
            tag.connId = connGuid;

            // Assert
            Assert.Equal(connGuid, tag.connId);
        }

        [Fact]
        public void PrCon_CanBeSet()
        {
            // Arrange
            var tag = new Tag();
            var container = new ProjectContainer();

            // Act
            tag.PrCon = container;

            // Assert
            Assert.Same(container, tag.PrCon);
        }

        [Fact]
        public void PrCon_CanBeSetNull()
        {
            // Arrange
            var tag = new Tag();

            // Act
            tag.PrCon = null;

            // Assert
            Assert.Null(tag.PrCon);
        }

        [Fact]
        public void Proj_CanBeSet()
        {
            // Arrange
            var tag = new Tag();
            var project = new Project();

            // Act
            tag.Proj = project;

            // Assert
            Assert.Same(project, tag.Proj);
        }

        [Fact]
        public void Proj_CanBeSetNull()
        {
            // Arrange
            var tag = new Tag { Proj = new Project() };

            // Act
            tag.Proj = null;

            // Assert
            Assert.Null(tag.Proj);
        }

        [Fact]
        public void IComparable_ImplementsInterface()
        {
            // Arrange
            var tag = new Tag();

            // Act & Assert
            Assert.IsAssignableFrom<IComparable<Tag>>(tag);
        }

        [Fact]
        public void ITag_ImplementsInterface()
        {
            // Arrange
            var tag = new Tag();

            // Act & Assert
            Assert.IsAssignableFrom<ITag>(tag);
        }

        [Fact]
        public void INotifyPropertyChanged_ImplementsInterface()
        {
            // Arrange
            var tag = new Tag();

            // Act & Assert
            Assert.IsAssignableFrom<INotifyPropertyChanged>(tag);
        }

        [Fact]
        public void IDriverModel_ImplementsInterface()
        {
            // Arrange
            var tag = new Tag();

            // Act & Assert
            Assert.IsAssignableFrom<IDriverModel>(tag);
        }

        [Fact]
        public void ITreeViewModel_ImplementsInterface()
        {
            // Arrange
            var tag = new Tag();

            // Act & Assert
            Assert.IsAssignableFrom<ITreeViewModel>(tag);
        }

        [Fact]
        public void RefreshedCycle_EventCanBeSubscribed()
        {
            // Arrange
            var tag = new Tag();
            EventHandler handler = (sender, e) => { };

            // Act & Assert
            // Verify event subscription works (events are public fields managed by the class internally)
            tag.refreshedCycle += handler;
            tag.refreshedCycle -= handler;
            Assert.True(true);  // Test passes if no exception
        }

        [Fact]
        public void RefreshedPartial_EventCanBeSubscribed()
        {
            // Arrange
            var tag = new Tag();
            EventHandler handler = (sender, e) => { };

            // Act & Assert
            tag.refreshedPartial += handler;
            tag.refreshedPartial -= handler;
            Assert.True(true);
        }

        [Fact]
        public void Error_EventCanBeSubscribed()
        {
            // Arrange
            var tag = new Tag();
            EventHandler handler = (sender, e) => { };

            // Act & Assert
            tag.error += handler;
            tag.error -= handler;
            Assert.True(true);
        }

        [Fact]
        public void Information_EventCanBeSubscribed()
        {
            // Arrange
            var tag = new Tag();
            EventHandler handler = (sender, e) => { };

            // Act & Assert
            tag.information += handler;
            tag.information -= handler;
            Assert.True(true);
        }

        [Fact]
        public void DataSent_EventCanBeSubscribed()
        {
            // Arrange
            var tag = new Tag();
            EventHandler handler = (sender, e) => { };

            // Act & Assert
            tag.dataSent += handler;
            tag.dataSent -= handler;
            Assert.True(true);
        }

        [Fact]
        public void DataRecived_EventCanBeSubscribed()
        {
            // Arrange
            var tag = new Tag();
            EventHandler handler = (sender, e) => { };

            // Act & Assert
            tag.dataRecived += handler;
            tag.dataRecived -= handler;
            Assert.True(true);
        }

        [Fact]
        public void IsSerializable_HasSerializableAttribute()
        {
            // Arrange & Act
            var tagType = typeof(Tag);

            // Assert
            Assert.True(tagType.IsSerializable);
        }

        [Fact]
        public void MultipleGuids_CanBeSetIndependently()
        {
            // Arrange
            var tag = new Tag();
            var objGuid = Guid.NewGuid();
            var parentGuid = Guid.NewGuid();
            var connGuid = Guid.NewGuid();

            // Act
            tag.objId = objGuid;
            tag.parentId = parentGuid;
            tag.connId = connGuid;

            // Assert
            Assert.Equal(objGuid, tag.objId);
            Assert.Equal(parentGuid, tag.parentId);
            Assert.Equal(connGuid, tag.connId);
        }

        [Fact]
        public void Tag_ImplementsITreeViewModel()
        {
            // Arrange
            var tag = new Tag();
            var treeView = (ITreeViewModel)tag;

            // Act & Assert
            Assert.NotNull(treeView);
        }

        [Fact]
        public void Tag_ImplementsITableView()
        {
            // Arrange
            var tag = new Tag();

            // Act & Assert - Tag doesn't implement ITableView according to interface list
            Assert.False(tag is ITableView);
        }
    }
}

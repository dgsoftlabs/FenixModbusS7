using Xunit;
using ProjectDataLib;
using System;
using System.ComponentModel;

namespace ProjectDataLib.Test.Models
{
    public class InTagTests
    {
        [Fact]
        public void Constructor_Default_CreatesInstance()
        {
            // Act
            var inTag = new InTag();

            // Assert
            Assert.NotNull(inTag);
        }

        [Fact]
        public void ObjId_DefaultValue_IsEmpty()
        {
            // Arrange & Act
            var inTag = new InTag();

            // Assert
            Assert.Equal(Guid.Empty, inTag.objId);
        }

        [Fact]
        public void ObjId_CanBeSet()
        {
            // Arrange
            var inTag = new InTag();
            var newGuid = Guid.NewGuid();

            // Act
            inTag.objId = newGuid;

            // Assert
            Assert.Equal(newGuid, inTag.objId);
        }

        [Fact]
        public void ParentId_CanBeSet()
        {
            // Arrange
            var inTag = new InTag();
            var parentGuid = Guid.NewGuid();

            // Act
            inTag.parentId = parentGuid;

            // Assert
            Assert.Equal(parentGuid, inTag.parentId);
        }

        [Fact]
        public void PrCon_CanBeSet()
        {
            // Arrange
            var inTag = new InTag();
            var container = new ProjectContainer();

            // Act
            inTag.PrCon = container;

            // Assert
            Assert.Same(container, inTag.PrCon);
        }

        [Fact]
        public void PrCon_CanBeSetNull()
        {
            // Arrange
            var inTag = new InTag();

            // Act
            inTag.PrCon = null;

            // Assert
            Assert.Null(inTag.PrCon);
        }

        [Fact]
        public void Proj_CanBeSet()
        {
            // Arrange
            var inTag = new InTag();
            var project = new Project();

            // Act
            inTag.Proj = project;

            // Assert
            Assert.Same(project, inTag.Proj);
        }

        [Fact]
        public void Proj_CanBeSetNull()
        {
            // Arrange
            var inTag = new InTag();

            // Act
            inTag.Proj = null;

            // Assert
            Assert.Null(inTag.Proj);
        }

        [Fact]
        public void IComparable_ImplementsInterface()
        {
            // Arrange
            var inTag = new InTag();

            // Act & Assert
            Assert.IsAssignableFrom<IComparable<Tag>>(inTag);
        }

        [Fact]
        public void ITag_ImplementsInterface()
        {
            // Arrange
            var inTag = new InTag();

            // Act & Assert
            Assert.IsAssignableFrom<ITag>(inTag);
        }

        [Fact]
        public void INotifyPropertyChanged_ImplementsInterface()
        {
            // Arrange
            var inTag = new InTag();

            // Act & Assert
            Assert.IsAssignableFrom<INotifyPropertyChanged>(inTag);
        }

        [Fact]
        public void IDriverModel_ImplementsInterface()
        {
            // Arrange
            var inTag = new InTag();

            // Act & Assert
            Assert.IsAssignableFrom<IDriverModel>(inTag);
        }

        [Fact]
        public void ITreeViewModel_ImplementsInterface()
        {
            // Arrange
            var inTag = new InTag();

            // Act & Assert
            Assert.IsAssignableFrom<ITreeViewModel>(inTag);
        }

        [Fact]
        public void RefreshedCycle_EventCanBeSubscribed()
        {
            // Arrange
            var inTag = new InTag();
            EventHandler handler = (sender, e) => { };

            // Act & Assert
            // Verify event subscription works (events are public fields managed by the class internally)
            inTag.refreshedCycle += handler;
            inTag.refreshedCycle -= handler;
            Assert.True(true);  // Test passes if no exception
        }

        [Fact]
        public void RefreshedPartial_EventCanBeSubscribed()
        {
            // Arrange
            var inTag = new InTag();
            EventHandler handler = (sender, e) => { };

            // Act & Assert
            inTag.refreshedPartial += handler;
            inTag.refreshedPartial -= handler;
            Assert.True(true);
        }

        [Fact]
        public void Error_EventCanBeSubscribed()
        {
            // Arrange
            var inTag = new InTag();
            EventHandler handler = (sender, e) => { };

            // Act & Assert
            inTag.error += handler;
            inTag.error -= handler;
            Assert.True(true);
        }

        [Fact]
        public void Information_EventCanBeSubscribed()
        {
            // Arrange
            var inTag = new InTag();
            EventHandler handler = (sender, e) => { };

            // Act & Assert
            inTag.information += handler;
            inTag.information -= handler;
            Assert.True(true);
        }

        [Fact]
        public void DataSent_EventCanBeSubscribed()
        {
            // Arrange
            var inTag = new InTag();
            EventHandler handler = (sender, e) => { };

            // Act & Assert
            inTag.dataSent += handler;
            inTag.dataSent -= handler;
            Assert.True(true);
        }

        [Fact]
        public void DataRecived_EventCanBeSubscribed()
        {
            // Arrange
            var inTag = new InTag();
            EventHandler handler = (sender, e) => { };

            // Act & Assert
            inTag.dataRecived += handler;
            inTag.dataRecived -= handler;
            Assert.True(true);
        }

        [Fact]
        public void IsSerializable_HasSerializableAttribute()
        {
            // Arrange & Act
            var inTagType = typeof(InTag);

            // Assert
            Assert.True(inTagType.IsSerializable);
        }

        [Fact]
        public void MultipleProperties_CanBeSetIndependently()
        {
            // Arrange
            var inTag = new InTag();
            var objGuid = Guid.NewGuid();
            var parentGuid = Guid.NewGuid();
            var container = new ProjectContainer();

            // Act
            inTag.objId = objGuid;
            inTag.parentId = parentGuid;
            inTag.PrCon = container;

            // Assert
            Assert.Equal(objGuid, inTag.objId);
            Assert.Equal(parentGuid, inTag.parentId);
            Assert.Same(container, inTag.PrCon);
        }

        [Fact]
        public void InTag_ImplementsITreeViewModel()
        {
            // Arrange
            var inTag = new InTag();
            var treeView = (ITreeViewModel)inTag;

            // Act & Assert
            Assert.NotNull(treeView);
        }
    }
}

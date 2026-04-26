using Xunit;
using ProjectDataLib;
using System;
using System.ComponentModel;

namespace ProjectDataLib.Test.Models
{
    public class InFileTests
    {
        [Fact]
        public void Constructor_Default_CreatesInstance()
        {
            // Act
            var inFile = new InFile();

            // Assert
            Assert.NotNull(inFile);
        }

        [Fact]
        public void ObjId_GeneratesUniqueGuids()
        {
            // Arrange & Act
            var inFile1 = new InFile();
            var inFile2 = new InFile();

            // Assert
            Assert.NotEqual(Guid.Empty, inFile1.objId);
            Assert.NotEqual(Guid.Empty, inFile2.objId);
            Assert.NotEqual(inFile1.objId, inFile2.objId);
        }

        [Fact]
        public void ObjId_CanBeSet()
        {
            // Arrange
            var inFile = new InFile();
            var newGuid = Guid.NewGuid();

            // Act
            inFile.objId = newGuid;

            // Assert
            Assert.Equal(newGuid, inFile.objId);
        }

        [Fact]
        public void ObjId_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var inFile = new InFile();
            bool eventRaised = false;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == nameof(InFile.objId))
                    eventRaised = true;
            };

            var notifyingFile = (INotifyPropertyChanged)inFile;
            notifyingFile.PropertyChanged += handler;

            // Act
            inFile.objId = Guid.NewGuid();

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void Name_CanBeSet()
        {
            // Arrange
            var inFile = new InFile();
            var expectedName = "TestFile";

            // Act
            inFile.Name = expectedName;

            // Assert
            Assert.Equal(expectedName, inFile.Name);
        }

        [Fact]
        public void Name_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var inFile = new InFile();
            bool eventRaised = false;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == nameof(InFile.Name))
                    eventRaised = true;
            };

            var notifyingFile = (INotifyPropertyChanged)inFile;
            notifyingFile.PropertyChanged += handler;

            // Act
            inFile.Name = "NewName";

            // Assert
            Assert.True(eventRaised);
            Assert.Equal("NewName", inFile.Name);
        }

        [Theory]
        [InlineData("File1")]
        [InlineData("Data")]
        [InlineData("")]
        public void Name_SetVariousValues_UpdatesCorrectly(string name)
        {
            // Arrange
            var inFile = new InFile();

            // Act
            inFile.Name = name;

            // Assert
            Assert.Equal(name, inFile.Name);
        }

        [Fact]
        public void FilePath_CanBeSet()
        {
            // Arrange
            var inFile = new InFile();
            var expectedPath = @"C:\files\data.csv";

            // Act
            inFile.FilePath = expectedPath;

            // Assert
            Assert.Equal(expectedPath, inFile.FilePath);
        }

        [Fact]
        public void FilePath_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var inFile = new InFile();
            bool eventRaised = false;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == nameof(InFile.FilePath))
                    eventRaised = true;
            };

            var notifyingFile = (INotifyPropertyChanged)inFile;
            notifyingFile.PropertyChanged += handler;

            // Act
            inFile.FilePath = @"C:\test\file.csv";

            // Assert
            Assert.True(eventRaised);
        }

        [Theory]
        [InlineData(@"C:\files\data.csv")]
        [InlineData(@"D:\backup\config.xml")]
        [InlineData("")]
        public void FilePath_SetVariousPaths_UpdatesCorrectly(string path)
        {
            // Arrange
            var inFile = new InFile();

            // Act
            inFile.FilePath = path;

            // Assert
            Assert.Equal(path, inFile.FilePath);
        }

        [Fact]
        public void Enable_DefaultValue_IsTrue()
        {
            // Arrange & Act
            var inFile = new InFile();

            // Assert
            Assert.True(inFile.Enable);
        }

        [Fact]
        public void Enable_CanBeSetFalse()
        {
            // Arrange
            var inFile = new InFile();

            // Act
            inFile.Enable = false;

            // Assert
            Assert.False(inFile.Enable);
        }

        [Fact]
        public void Enable_CanBeSetTrue()
        {
            // Arrange
            var inFile = new InFile { Enable = false };

            // Act
            inFile.Enable = true;

            // Assert
            Assert.True(inFile.Enable);
        }

        [Fact]
        public void Enable_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var inFile = new InFile();
            int eventCount = 0;
            PropertyChangedEventHandler handler = (sender, e) => eventCount++;

            var notifyingFile = (INotifyPropertyChanged)inFile;
            notifyingFile.PropertyChanged += handler;

            // Act
            inFile.Enable = false;

            // Assert
            Assert.True(eventCount >= 2);  // Enable event + IsBlocked event
        }

        [Fact]
        public void IsBlocked_IsInverseOfEnable()
        {
            // Arrange
            var inFile = new InFile();

            // Act & Assert
            Assert.True(inFile.Enable);
            Assert.False(inFile.IsBlocked);

            inFile.Enable = false;
            Assert.False(inFile.Enable);
            Assert.True(inFile.IsBlocked);
        }

        [Fact]
        public void IsBlocked_CanBeSet()
        {
            // Arrange
            var inFile = new InFile();

            // Act
            inFile.IsBlocked = true;

            // Assert
            Assert.True(inFile.IsBlocked);
            Assert.False(inFile.Enable);
        }

        [Fact]
        public void PrCon_CanBeSet()
        {
            // Arrange
            var inFile = new InFile();
            var container = new ProjectContainer();

            // Act
            inFile.PrCon = container;

            // Assert
            Assert.Same(container, inFile.PrCon);
        }

        [Fact]
        public void PrCon_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var inFile = new InFile();
            var container = new ProjectContainer();
            bool eventRaised = false;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == nameof(InFile.PrCon))
                    eventRaised = true;
            };

            var notifyingFile = (INotifyPropertyChanged)inFile;
            notifyingFile.PropertyChanged += handler;

            // Act
            inFile.PrCon = container;

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void PrCon_CanBeSetNull()
        {
            // Arrange
            var inFile = new InFile { PrCon = new ProjectContainer() };

            // Act
            inFile.PrCon = null;

            // Assert
            Assert.Null(inFile.PrCon);
        }

        [Fact]
        public void Proj_CanBeSet()
        {
            // Arrange
            var inFile = new InFile();
            var project = new Project();

            // Act
            inFile.Proj = project;

            // Assert
            Assert.Same(project, inFile.Proj);
        }

        [Fact]
        public void Proj_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var inFile = new InFile();
            var project = new Project();
            bool eventRaised = false;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == nameof(InFile.Proj))
                    eventRaised = true;
            };

            var notifyingFile = (INotifyPropertyChanged)inFile;
            notifyingFile.PropertyChanged += handler;

            // Act
            inFile.Proj = project;

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void ITreeViewModel_Children_ReturnsEmptyCollection()
        {
            // Arrange
            var inFile = new InFile();
            var treeView = (ITreeViewModel)inFile;

            // Act
            var children = treeView.Children;

            // Assert
            Assert.Empty(children);
        }

        [Fact]
        public void ITreeViewModel_Name_ReturnsName()
        {
            // Arrange
            var inFile = new InFile { Name = "TestFile" };
            var treeView = (ITreeViewModel)inFile;

            // Act
            var name = treeView.Name;

            // Assert
            Assert.Equal("TestFile", name);
        }

        [Fact]
        public void ITreeViewModel_Name_CanBeSet()
        {
            // Arrange
            var inFile = new InFile();
            var treeView = (ITreeViewModel)inFile;

            // Act
            treeView.Name = "NewName";

            // Assert
            Assert.Equal("NewName", inFile.Name);
        }

        [Fact]
        public void ITreeViewModel_IsExpand_ReturnsFalse()
        {
            // Arrange
            var inFile = new InFile();
            var treeView = (ITreeViewModel)inFile;

            // Act & Assert
            Assert.False(treeView.IsExpand);
        }

        [Fact]
        public void ITreeViewModel_IsLive_ReturnsFalse()
        {
            // Arrange
            var inFile = new InFile();
            var treeView = (ITreeViewModel)inFile;

            // Act & Assert
            Assert.False(treeView.IsLive);
        }

        [Fact]
        public void ITreeViewModel_IsBlocked_ReflectsIsBlockedProperty()
        {
            // Arrange
            var inFile = new InFile();
            var treeView = (ITreeViewModel)inFile;

            // Act & Assert
            Assert.False(treeView.IsBlocked);

            inFile.IsBlocked = true;
            Assert.True(treeView.IsBlocked);
        }

        [Fact]
        public void INotifyPropertyChanged_ImplementsInterface()
        {
            // Arrange
            var inFile = new InFile();

            // Act & Assert
            Assert.IsAssignableFrom<INotifyPropertyChanged>(inFile);
        }

        [Fact]
        public void ITreeViewModel_ImplementsInterface()
        {
            // Arrange
            var inFile = new InFile();

            // Act & Assert
            Assert.IsAssignableFrom<ITreeViewModel>(inFile);
        }

        [Fact]
        public void IsSerializable_HasSerializableAttribute()
        {
            // Arrange & Act
            var inFileType = typeof(InFile);

            // Assert
            Assert.True(inFileType.IsSerializable);
        }

        [Fact]
        public void MultiplePropertyChanges_RaisesEventForEach()
        {
            // Arrange
            var inFile = new InFile();
            int eventCount = 0;
            PropertyChangedEventHandler handler = (sender, e) => eventCount++;

            var notifyingFile = (INotifyPropertyChanged)inFile;
            notifyingFile.PropertyChanged += handler;

            // Act
            inFile.Name = "File1";
            inFile.FilePath = @"C:\test\file.csv";
            inFile.Enable = false;

            // Assert
            Assert.True(eventCount > 0);
        }
    }
}

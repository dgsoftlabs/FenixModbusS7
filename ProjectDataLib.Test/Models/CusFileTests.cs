using System.ComponentModel;
using Xunit;

namespace ProjectDataLib.Test.Models
{
    public class CusFileTests
    {
        [Fact]
        public void Constructor_Default_CreatesInstanceWithDefaults()
        {
            // Act
            var cusFile = new CusFile();

            // Assert
            Assert.NotNull(cusFile);
            Assert.NotEqual(Guid.Empty, cusFile.ObjId);
            Assert.NotNull(cusFile.Children);
            Assert.Empty(cusFile.Children);
        }

        [Fact]
        public void ObjId_GeneratesUniqueGuids()
        {
            // Arrange & Act
            var cusFile1 = new CusFile();
            var cusFile2 = new CusFile();

            // Assert
            Assert.NotEqual(cusFile1.ObjId, cusFile2.ObjId);
        }

        [Fact]
        public void ObjId_CanBeSet()
        {
            // Arrange
            var cusFile = new CusFile();
            var newGuid = Guid.NewGuid();

            // Act
            cusFile.ObjId = newGuid;

            // Assert
            Assert.Equal(newGuid, cusFile.ObjId);
        }

        [Fact]
        public void Name_CanBeSet()
        {
            // Arrange
            var cusFile = new CusFile();
            var expectedName = "TestFile";

            // Act
            cusFile.Name = expectedName;

            // Assert
            Assert.Equal(expectedName, cusFile.Name);
        }

        [Theory]
        [InlineData("File1.txt")]
        [InlineData("Document")]
        [InlineData("")]
        [InlineData("A")]
        public void Name_SetVariousValues_UpdatesCorrectly(string name)
        {
            // Arrange
            var cusFile = new CusFile();

            // Act
            cusFile.Name = name;

            // Assert
            Assert.Equal(name, cusFile.Name);
        }

        [Fact]
        public void FullName_SetValue_UpdatesNameFromPath()
        {
            // Arrange
            var cusFile = new CusFile();

            // Act
            cusFile.FullName = @"C:\folder\file.txt";

            // Assert
            Assert.Equal(@"C:\folder\file.txt", cusFile.FullName);
            Assert.Equal("file.txt", cusFile.Name);
        }

        [Theory]
        [InlineData(@"C:\folder\subdir\", "subdir")]  // Directory
        [InlineData(@"C:\folder\file.txt", "file.txt")]  // File
        [InlineData("file.txt", "file.txt")]  // Relative path
        public void FullName_SetVariousPaths_ExtractsNameCorrectly(string fullPath, string expectedName)
        {
            // Arrange
            var cusFile = new CusFile();

            // Act
            cusFile.FullName = fullPath;

            // Assert
            Assert.Equal(expectedName, cusFile.Name);
        }

        [Fact]
        public void FullName_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var cusFile = new CusFile();
            var eventRaised = false;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == nameof(CusFile.FullName))
                    eventRaised = true;
            };

            var notifyingFile = (INotifyPropertyChanged)cusFile;
            notifyingFile.PropertyChanged += handler;

            // Act
            cusFile.FullName = @"C:\test\file.txt";

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void IsFile_CanBeSetTrue()
        {
            // Arrange
            var cusFile = new CusFile();

            // Act
            cusFile.IsFile = true;

            // Assert
            Assert.True(cusFile.IsFile);
        }

        [Fact]
        public void IsFile_CanBeSetFalse()
        {
            // Arrange
            var cusFile = new CusFile();

            // Act
            cusFile.IsFile = false;

            // Assert
            Assert.False(cusFile.IsFile);
        }

        [Fact]
        public void Children_CanAddItems()
        {
            // Arrange
            var cusFile = new CusFile();
            var item = new object();

            // Act
            cusFile.Children.Add(item);

            // Assert
            Assert.Single(cusFile.Children);
            Assert.Contains(item, cusFile.Children);
        }

        [Fact]
        public void Children_CanBeCleared()
        {
            // Arrange
            var cusFile = new CusFile();
            cusFile.Children.Add(new object());
            cusFile.Children.Add(new object());

            // Act
            cusFile.Children.Clear();

            // Assert
            Assert.Empty(cusFile.Children);
        }

        [Fact]
        public void ITreeViewModel_Children_ReturnsObservableCollection()
        {
            // Arrange
            var cusFile = new CusFile();
            var treeView = (ITreeViewModel)cusFile;

            // Act
            var children = treeView.Children;

            // Assert
            Assert.NotNull(children);
            Assert.Empty(children);
        }

        [Fact]
        public void ITreeViewModel_Name_ReturnsName()
        {
            // Arrange
            var cusFile = new CusFile();
            cusFile.Name = "TestName";
            var treeView = (ITreeViewModel)cusFile;

            // Act
            var name = treeView.Name;

            // Assert
            Assert.Equal("TestName", name);
        }

        [Fact]
        public void ITreeViewModel_Name_CanBeSet()
        {
            // Arrange
            var cusFile = new CusFile();
            var treeView = (ITreeViewModel)cusFile;

            // Act
            treeView.Name = "NewName";

            // Assert
            Assert.Equal("NewName", cusFile.Name);
        }

        [Fact]
        public void ITreeViewModel_IsExpand_AlwaysReturnsTrue()
        {
            // Arrange
            var cusFile = new CusFile();
            var treeView = (ITreeViewModel)cusFile;

            // Act & Assert
            Assert.True(treeView.IsExpand);
        }

        [Fact]
        public void ITreeViewModel_IsExpand_CanBeSet()
        {
            // Arrange
            var cusFile = new CusFile();
            var treeView = (ITreeViewModel)cusFile;

            // Act
            treeView.IsExpand = false;

            // Assert - No exception, behaves correctly
            Assert.True(treeView.IsExpand);  // Still returns true regardless
        }

        [Fact]
        public void INotifyPropertyChanged_ImplementsInterface()
        {
            // Arrange
            var cusFile = new CusFile();

            // Act & Assert
            Assert.IsAssignableFrom<INotifyPropertyChanged>(cusFile);
        }

        [Fact]
        public void IsSerializable_HasSerializableAttribute()
        {
            // Arrange & Act
            var cusFileType = typeof(CusFile);

            // Assert
            Assert.True(cusFileType.IsSerializable);
        }

        [Fact]
        public void FullName_OfDirectory_ExtractsDirectoryName()
        {
            // Arrange
            var cusFile = new CusFile { IsFile = false };

            // Act
            cusFile.FullName = @"C:\mydir\";

            // Assert
            Assert.Equal("mydir", cusFile.Name);
        }

        [Fact]
        public void MultipleChildren_CanBeAdded()
        {
            // Arrange
            var cusFile = new CusFile();
            var items = new object[] { new object(), new object(), new object() };

            // Act
            foreach (var item in items)
                cusFile.Children.Add(item);

            // Assert
            Assert.Equal(3, cusFile.Children.Count);
        }

        [Fact]
        public void Children_RemovedAfterAdding()
        {
            // Arrange
            var cusFile = new CusFile();
            var item = new object();
            cusFile.Children.Add(item);

            // Act
            cusFile.Children.Remove(item);

            // Assert
            Assert.Empty(cusFile.Children);
        }
    }
}

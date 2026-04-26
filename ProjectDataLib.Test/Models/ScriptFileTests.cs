using Xunit;
using ProjectDataLib;
using System;
using System.ComponentModel;

namespace ProjectDataLib.Test.Models
{
    public class ScriptFileTests
    {
        [Fact]
        public void Constructor_Default_CreatesInstance()
        {
            // Act
            var scriptFile = new ScriptFile();

            // Assert
            Assert.NotNull(scriptFile);
        }

        [Fact]
        public void ObjId_GeneratesUniqueGuids()
        {
            // Arrange & Act
            var scriptFile1 = new ScriptFile();
            var scriptFile2 = new ScriptFile();
            scriptFile1.objId = Guid.NewGuid();
            scriptFile2.objId = Guid.NewGuid();

            // Assert
            Assert.NotEqual(Guid.Empty, scriptFile1.objId);
            Assert.NotEqual(Guid.Empty, scriptFile2.objId);
            Assert.NotEqual(scriptFile1.objId, scriptFile2.objId);
        }

        [Fact]
        public void ObjId_CanBeSet()
        {
            // Arrange
            var scriptFile = new ScriptFile();
            var newGuid = Guid.NewGuid();

            // Act
            scriptFile.objId = newGuid;

            // Assert
            Assert.Equal(newGuid, scriptFile.objId);
        }

        [Fact]
        public void ObjId_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var scriptFile = new ScriptFile();
            bool eventRaised = false;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == "objId")
                    eventRaised = true;
            };

            var notifyingFile = (INotifyPropertyChanged)scriptFile;
            notifyingFile.PropertyChanged += handler;

            // Act
            scriptFile.objId = Guid.NewGuid();

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void Name_CanBeSet()
        {
            // Arrange
            var scriptFile = new ScriptFile();
            var expectedName = "TestScript";

            // Act
            scriptFile.Name = expectedName;

            // Assert
            Assert.Equal(expectedName, scriptFile.Name);
        }

        [Fact]
        public void Name_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var scriptFile = new ScriptFile();
            bool eventRaised = false;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == nameof(ScriptFile.Name))
                    eventRaised = true;
            };

            var notifyingFile = (INotifyPropertyChanged)scriptFile;
            notifyingFile.PropertyChanged += handler;

            // Act
            scriptFile.Name = "NewScript";

            // Assert
            Assert.True(eventRaised);
            Assert.Equal("NewScript", scriptFile.Name);
        }

        [Theory]
        [InlineData("Script1")]
        [InlineData("OnStartup")]
        [InlineData("")]
        public void Name_SetVariousValues_UpdatesCorrectly(string name)
        {
            // Arrange
            var scriptFile = new ScriptFile();

            // Act
            scriptFile.Name = name;

            // Assert
            Assert.Equal(name, scriptFile.Name);
        }

        [Fact]
        public void FilePath_CanBeSet()
        {
            // Arrange
            var scriptFile = new ScriptFile();
            var expectedPath = @"C:\scripts\main.cs";

            // Act
            scriptFile.FilePath = expectedPath;

            // Assert
            Assert.Equal(expectedPath, scriptFile.FilePath);
        }

        [Fact]
        public void FilePath_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var scriptFile = new ScriptFile();
            bool eventRaised = false;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == "FilePath")
                    eventRaised = true;
            };

            var notifyingFile = (INotifyPropertyChanged)scriptFile;
            notifyingFile.PropertyChanged += handler;

            // Act
            scriptFile.FilePath = @"C:\test\script.cs";

            // Assert
            Assert.True(eventRaised);
        }

        [Theory]
        [InlineData(@"C:\scripts\main.cs")]
        [InlineData(@"D:\backup\script.cs")]
        [InlineData("")]
        public void FilePath_SetVariousPaths_UpdatesCorrectly(string path)
        {
            // Arrange
            var scriptFile = new ScriptFile();

            // Act
            scriptFile.FilePath = path;

            // Assert
            Assert.Equal(path, scriptFile.FilePath);
        }

        [Fact]
        public void Enable_DefaultValue_IsFalse()
        {
            // Arrange & Act
            var scriptFile = new ScriptFile();

            // Assert
            Assert.False(scriptFile.Enable);
        }

        [Fact]
        public void Enable_CanBeSetTrue()
        {
            // Arrange
            var scriptFile = new ScriptFile();

            // Act
            scriptFile.Enable = true;

            // Assert
            Assert.True(scriptFile.Enable);
        }

        [Fact]
        public void Enable_CanBeSetFalse()
        {
            // Arrange
            var scriptFile = new ScriptFile { Enable = true };

            // Act
            scriptFile.Enable = false;

            // Assert
            Assert.False(scriptFile.Enable);
        }

        [Fact]
        public void Enable_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var scriptFile = new ScriptFile();
            bool eventRaised = false;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == "Enable")
                    eventRaised = true;
            };

            var notifyingFile = (INotifyPropertyChanged)scriptFile;
            notifyingFile.PropertyChanged += handler;

            // Act
            scriptFile.Enable = true;

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void IsBlocked_IsInverseOfEnable()
        {
            // Arrange
            var scriptFile = new ScriptFile();

            // Act & Assert
            Assert.False(scriptFile.Enable);
            Assert.True(scriptFile.IsBlocked);

            scriptFile.Enable = true;
            Assert.True(scriptFile.Enable);
            Assert.False(scriptFile.IsBlocked);
        }

        [Fact]
        public void IsBlocked_CanBeSet()
        {
            // Arrange
            var scriptFile = new ScriptFile();

            // Act
            scriptFile.IsBlocked = false;

            // Assert
            Assert.False(scriptFile.IsBlocked);
            Assert.True(scriptFile.Enable);
        }

        [Fact]
        public void TimerName_CanBeSet()
        {
            // Arrange
            var scriptFile = new ScriptFile();
            var timerName = "Timer1";

            // Act
            scriptFile.TimerName = timerName;

            // Assert
            Assert.Equal(timerName, scriptFile.TimerName);
        }

        [Fact]
        public void TimerName_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var scriptFile = new ScriptFile();
            bool eventRaised = false;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == "TimerName")
                    eventRaised = true;
            };

            var notifyingFile = (INotifyPropertyChanged)scriptFile;
            notifyingFile.PropertyChanged += handler;

            // Act
            scriptFile.TimerName = "MyTimer";

            // Assert
            Assert.True(eventRaised);
        }

        [Theory]
        [InlineData("Timer1")]
        [InlineData("OnStart")]
        [InlineData("")]
        [InlineData(null)]
        public void TimerName_SetVariousValues_UpdatesCorrectly(string timerName)
        {
            // Arrange
            var scriptFile = new ScriptFile();

            // Act
            scriptFile.TimerName = timerName;

            // Assert
            Assert.Equal(timerName, scriptFile.TimerName);
        }

        [Fact]
        public void PrCon_CanBeSet()
        {
            // Arrange
            var scriptFile = new ScriptFile();
            var container = new ProjectContainer();

            // Act
            scriptFile.PrCon = container;

            // Assert
            Assert.Same(container, scriptFile.PrCon);
        }

        [Fact]
        public void PrCon_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var scriptFile = new ScriptFile();
            var container = new ProjectContainer();
            bool eventRaised = false;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == "PrCon")
                    eventRaised = true;
            };

            var notifyingFile = (INotifyPropertyChanged)scriptFile;
            notifyingFile.PropertyChanged += handler;

            // Act
            scriptFile.PrCon = container;

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void Proj_CanBeSet()
        {
            // Arrange
            var scriptFile = new ScriptFile();
            var project = new Project();

            // Act
            scriptFile.Proj = project;

            // Assert
            Assert.Same(project, scriptFile.Proj);
        }

        [Fact]
        public void Proj_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var scriptFile = new ScriptFile();
            var project = new Project();
            bool eventRaised = false;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == "Proj")
                    eventRaised = true;
            };

            var notifyingFile = (INotifyPropertyChanged)scriptFile;
            notifyingFile.PropertyChanged += handler;

            // Act
            scriptFile.Proj = project;

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void ITreeViewModel_Children_ReturnsEmptyCollection()
        {
            // Arrange
            var scriptFile = new ScriptFile();
            var treeView = (ITreeViewModel)scriptFile;

            // Act
            var children = treeView.Children;

            // Assert
            Assert.Empty(children);
        }

        [Fact]
        public void ITreeViewModel_Name_ReturnsName()
        {
            // Arrange
            var scriptFile = new ScriptFile { Name = "TestScript" };
            var treeView = (ITreeViewModel)scriptFile;

            // Act
            var name = treeView.Name;

            // Assert
            Assert.Equal("TestScript", name);
        }

        [Fact]
        public void ITreeViewModel_Name_CanBeSet()
        {
            // Arrange
            var scriptFile = new ScriptFile();
            var treeView = (ITreeViewModel)scriptFile;

            // Act
            treeView.Name = "NewName";

            // Assert
            Assert.Equal("NewName", scriptFile.Name);
        }

        [Fact]
        public void ITreeViewModel_IsExpand_ReturnsFalse()
        {
            // Arrange
            var scriptFile = new ScriptFile();
            var treeView = (ITreeViewModel)scriptFile;

            // Act & Assert
            Assert.False(treeView.IsExpand);
        }

        [Fact]
        public void ITreeViewModel_IsBlocked_ReflectsIsBlockedProperty()
        {
            // Arrange
            var scriptFile = new ScriptFile();
            var treeView = (ITreeViewModel)scriptFile;

            // Act & Assert
            Assert.True(treeView.IsBlocked);  // Default Enable is false, so IsBlocked is true

            scriptFile.Enable = true;
            Assert.False(treeView.IsBlocked);
        }

        [Fact]
        public void INotifyPropertyChanged_ImplementsInterface()
        {
            // Arrange
            var scriptFile = new ScriptFile();

            // Act & Assert
            Assert.IsAssignableFrom<INotifyPropertyChanged>(scriptFile);
        }

        [Fact]
        public void ITreeViewModel_ImplementsInterface()
        {
            // Arrange
            var scriptFile = new ScriptFile();

            // Act & Assert
            Assert.IsAssignableFrom<ITreeViewModel>(scriptFile);
        }

        [Fact]
        public void IsSerializable_HasSerializableAttribute()
        {
            // Arrange & Act
            var scriptFileType = typeof(ScriptFile);

            // Assert
            Assert.True(scriptFileType.IsSerializable);
        }

        [Fact]
        public void MultiplePropertyChanges_RaisesEventForEach()
        {
            // Arrange
            var scriptFile = new ScriptFile();
            int eventCount = 0;
            PropertyChangedEventHandler handler = (sender, e) => eventCount++;

            var notifyingFile = (INotifyPropertyChanged)scriptFile;
            notifyingFile.PropertyChanged += handler;

            // Act
            scriptFile.Name = "Script1";
            scriptFile.FilePath = @"C:\test\script.cs";
            scriptFile.Enable = true;
            scriptFile.TimerName = "Timer1";

            // Assert
            Assert.True(eventCount >= 4);
        }
    }
}

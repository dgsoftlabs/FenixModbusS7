using Xunit;
using ProjectDataLib;
using System;
using System.Collections.ObjectModel;

namespace ProjectDataLib.Test.Models
{
    public class ProjectTests
    {
        [Fact]
        public void Constructor_Default_CreatesInstance()
        {
            // Act
            var project = new Project();

            // Assert
            Assert.NotNull(project);
        }

        [Fact]
        public void Constructor_WithParameters_SetsProjectProperties()
        {
            // Arrange
            var projectContainer = new ProjectContainer();
            string projectName = "Test Project";
            string autor = "Test Author";
            string company = "Test Company";
            string describe = "Test Description";

            // Act
            var project = new Project(projectContainer, projectName, autor, company, describe);

            // Assert
            Assert.Equal(projectName, project.projectName);
            Assert.Equal(autor, project.autor);
            Assert.Equal(company, project.company);
            Assert.Equal(describe, project.describe);
        }

        [Fact]
        public void Constructor_WithParameters_InitializesCollections()
        {
            // Arrange
            var projectContainer = new ProjectContainer();

            // Act
            var project = new Project(projectContainer, "Test", "Author", "Company", "Desc");

            // Assert
            Assert.NotNull(project.FileList);
            Assert.NotNull(project.ScriptFileList);
            Assert.NotNull(project.connectionList);
            Assert.NotNull(project.DevicesList);
            Assert.NotNull(project.tagsList);
            Assert.NotNull(project.InTagsList);
        }

        [Fact]
        public void Constructor_WithParameters_SetsCreationTime()
        {
            // Arrange
            var before = DateTime.Now;
            var projectContainer = new ProjectContainer();

            // Act
            var project = new Project(projectContainer, "Test", "Author", "Company", "Desc");
            var after = DateTime.Now;

            // Assert
            Assert.True(project.createTime >= before);
            Assert.True(project.createTime <= after);
        }

        [Fact]
        public void Constructor_WithParameters_SetsModificationTime()
        {
            // Arrange
            var before = DateTime.Now;
            var projectContainer = new ProjectContainer();

            // Act
            var project = new Project(projectContainer, "Test", "Author", "Company", "Desc");
            var after = DateTime.Now;

            // Assert
            Assert.True(project.modifeTime >= before);
            Assert.True(project.modifeTime <= after);
        }

        [Fact]
        public void Constructor_WithParameters_CreatesUniqueGuid()
        {
            // Arrange
            var projectContainer = new ProjectContainer();

            // Act
            var project1 = new Project(projectContainer, "Test1", "Author", "Company", "Desc");
            var project2 = new Project(projectContainer, "Test2", "Author", "Company", "Desc");

            // Assert
            Assert.NotEqual(Guid.Empty, project1.objId);
            Assert.NotEqual(Guid.Empty, project2.objId);
            Assert.NotEqual(project1.objId, project2.objId);
        }

        [Fact]
        public void ProjectName_SetValue_UpdatesModificationTime()
        {
            // Arrange
            var projectContainer = new ProjectContainer();
            var project = new Project(projectContainer, "Original", "Author", "Company", "Desc");
            var originalModTime = project.modifeTime;
            System.Threading.Thread.Sleep(10);

            // Act
            project.projectName = "Modified";

            // Assert
            Assert.Equal("Modified", project.projectName);
            Assert.True(project.modifeTime > originalModTime);
        }

        [Fact]
        public void Autor_SetValue_UpdatesModificationTime()
        {
            // Arrange
            var projectContainer = new ProjectContainer();
            var project = new Project(projectContainer, "Test", "Author", "Company", "Desc");
            var originalModTime = project.modifeTime;
            System.Threading.Thread.Sleep(10);

            // Act
            project.autor = "New Author";

            // Assert
            Assert.Equal("New Author", project.autor);
            Assert.True(project.modifeTime > originalModTime);
        }

        [Fact]
        public void Company_SetValue_UpdatesModificationTime()
        {
            // Arrange
            var projectContainer = new ProjectContainer();
            var project = new Project(projectContainer, "Test", "Author", "Company", "Desc");
            var originalModTime = project.modifeTime;
            System.Threading.Thread.Sleep(10);

            // Act
            project.company = "New Company";

            // Assert
            Assert.Equal("New Company", project.company);
            Assert.True(project.modifeTime > originalModTime);
        }

        [Fact]
        public void Describe_SetValue_UpdatesModificationTime()
        {
            // Arrange
            var projectContainer = new ProjectContainer();
            var project = new Project(projectContainer, "Test", "Author", "Company", "Desc");
            var originalModTime = project.modifeTime;
            System.Threading.Thread.Sleep(10);

            // Act
            project.describe = "New Description";

            // Assert
            Assert.Equal("New Description", project.describe);
            Assert.True(project.modifeTime > originalModTime);
        }

        [Fact]
        public void ModMarks_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var projectContainer = new ProjectContainer();
            var project = new Project(projectContainer, "Test", "Author", "Company", "Desc");
            bool propertyChanged = false;

            ((System.ComponentModel.INotifyPropertyChanged)project).PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "modMarks")
                    propertyChanged = true;
            };

            // Act
            project.modMarks = false;

            // Assert
            Assert.True(propertyChanged);
        }

        [Fact]
        public void Path_SetValue_UpdatesModificationTime()
        {
            // Arrange
            var projectContainer = new ProjectContainer();
            var project = new Project(projectContainer, "Test", "Author", "Company", "Desc");
            var originalModTime = project.modifeTime;
            System.Threading.Thread.Sleep(10);

            // Act
            project.path = "C:\\Projects\\Test";

            // Assert
            Assert.Equal("C:\\Projects\\Test", project.path);
            Assert.True(project.modifeTime > originalModTime);
        }

        [Fact]
        public void IsExpand_SetValue_UpdatesModificationTime()
        {
            // Arrange
            var projectContainer = new ProjectContainer();
            var project = new Project(projectContainer, "Test", "Author", "Company", "Desc");
            var originalModTime = project.modifeTime;
            System.Threading.Thread.Sleep(10);

            // Act
            project.IsExpand = false;

            // Assert
            Assert.False(project.IsExpand);
            Assert.True(project.modifeTime > originalModTime);
        }

        [Fact]
        public void Constructor_WithParameters_InitializesTreeViewChildren()
        {
            // Arrange
            var projectContainer = new ProjectContainer();

            // Act
            var project = new Project(projectContainer, "Test", "Author", "Company", "Desc");
            var treeViewChildren = ((ITreeViewModel)project).Children;

            // Assert
            Assert.NotNull(treeViewChildren);
            Assert.True(treeViewChildren.Count > 0);
        }

        [Fact]
        public void Constructor_WithParameters_InitializesDriverChildren()
        {
            // Arrange
            var projectContainer = new ProjectContainer();

            // Act
            var project = new Project(projectContainer, "Test", "Author", "Company", "Desc");
            var driverChildren = ((IDriversMagazine)project).Children;

            // Assert
            Assert.NotNull(driverChildren);
            Assert.True(driverChildren.Count >= 2); // ScriptEng and InternalTagsDrv
        }

        [Fact]
        public void ITreeViewModel_Name_ReturnsProjectName()
        {
            // Arrange
            var projectContainer = new ProjectContainer();
            var project = new Project(projectContainer, "TestName", "Author", "Company", "Desc");

            // Act
            var name = ((ITreeViewModel)project).Name;

            // Assert
            Assert.Equal("TestName", name);
        }

        [Fact]
        public void ITreeViewModel_IsLive_ReturnsFalse()
        {
            // Arrange
            var projectContainer = new ProjectContainer();
            var project = new Project(projectContainer, "Test", "Author", "Company", "Desc");

            // Act
            var isLive = ((ITreeViewModel)project).IsLive;

            // Assert
            Assert.False(isLive);
        }

        [Fact]
        public void ITreeViewModel_IsBlocked_ReturnsFalse()
        {
            // Arrange
            var projectContainer = new ProjectContainer();
            var project = new Project(projectContainer, "Test", "Author", "Company", "Desc");

            // Act
            var isBlocked = ((ITreeViewModel)project).IsBlocked;

            // Assert
            Assert.False(isBlocked);
        }

        [Fact]
        public void ITreeViewModel_Clr_ReturnsWhite()
        {
            // Arrange
            var projectContainer = new ProjectContainer();
            var project = new Project(projectContainer, "Test", "Author", "Company", "Desc");

            // Act
            var color = ((ITreeViewModel)project).Clr;

            // Assert
            Assert.Equal(System.Drawing.Color.White, color);
        }

        [Fact]
        public void Dispose_ImplementsIDisposable()
        {
            // Arrange
            var projectContainer = new ProjectContainer();
            var project = new Project(projectContainer, "Test", "Author", "Company", "Desc");

            // Act & Assert - should not throw
            project.Dispose();
        }

        [Fact]
        public void FileVer_SetValue_UpdatesModificationTime()
        {
            // Arrange
            var projectContainer = new ProjectContainer();
            var project = new Project(projectContainer, "Test", "Author", "Company", "Desc");
            var originalModTime = project.modifeTime;
            System.Threading.Thread.Sleep(10);

            // Act
            project.fileVer = new Version(2, 0, 0, 0);

            // Assert
            Assert.NotNull(project.fileVer);
            Assert.True(project.modifeTime > originalModTime);
        }

        [Fact]
        public void ObjId_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var projectContainer = new ProjectContainer();
            var project = new Project(projectContainer, "Test", "Author", "Company", "Desc");
            bool propertyChanged = false;

            ((System.ComponentModel.INotifyPropertyChanged)project).PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "objId")
                    propertyChanged = true;
            };

            // Act
            project.objId = Guid.NewGuid();

            // Assert
            Assert.True(propertyChanged);
        }

        [Fact]
        public void ModifeTime_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var projectContainer = new ProjectContainer();
            var project = new Project(projectContainer, "Test", "Author", "Company", "Desc");
            bool propertyChanged = false;

            ((System.ComponentModel.INotifyPropertyChanged)project).PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "modifeTime")
                    propertyChanged = true;
            };

            // Act
            project.modifeTime = DateTime.Now;

            // Assert
            Assert.True(propertyChanged);
        }

        [Fact]
        public void ChartConf_SetValue_UpdatesModificationTime()
        {
            // Arrange
            var projectContainer = new ProjectContainer();
            var project = new Project(projectContainer, "Test", "Author", "Company", "Desc");
            var originalModTime = project.modifeTime;
            System.Threading.Thread.Sleep(10);

            // Act
            project.ChartConf = new ChartViewConf();

            // Assert
            Assert.NotNull(project.ChartConf);
            Assert.True(project.modifeTime > originalModTime);
        }

        [Fact]
        public void LongDT_DefaultValue_IsSetCorrectly()
        {
            // Arrange
            var projectContainer = new ProjectContainer();

            // Act
            var project = new Project(projectContainer, "Test", "Author", "Company", "Desc");

            // Assert
            Assert.Equal("yyyy-MM-dd HH:mm:ss.fff", project.longDT);
        }

        [Fact]
        public void Constructor_WithParameters_MarksAsModified()
        {
            // Arrange & Act
            var projectContainer = new ProjectContainer();
            var project = new Project(projectContainer, "Test", "Author", "Company", "Desc");

            // Assert
            Assert.True(project.modMarks);
        }
    }
}

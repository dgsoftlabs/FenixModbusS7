using Xunit;
using ProjectDataLib;
using System;
using System.ComponentModel;
using System.Drawing;

namespace ProjectDataLib.Test.Models
{
    public class DatabaseModelTests
    {
        [Fact]
        public void Constructor_Default_CreatesInstance()
        {
            // Act
            var dbModel = new DatabaseModel();

            // Assert
            Assert.NotNull(dbModel);
        }

        [Fact]
        public void PrCon_CanBeSet()
        {
            // Arrange
            var dbModel = new DatabaseModel();
            var container = new ProjectContainer();

            // Act
            dbModel.PrCon = container;

            // Assert
            Assert.Same(container, dbModel.PrCon);
        }

        [Fact]
        public void PrCon_CanBeSetNull()
        {
            // Arrange
            var dbModel = new DatabaseModel();

            // Act
            dbModel.PrCon = null;

            // Assert
            Assert.Null(dbModel.PrCon);
        }

        [Fact]
        public void Pr_CanBeSet()
        {
            // Arrange
            var dbModel = new DatabaseModel();
            var project = new Project();

            // Act
            dbModel.Pr = project;

            // Assert
            Assert.Same(project, dbModel.Pr);
        }

        [Fact]
        public void Pr_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var dbModel = new DatabaseModel();
            var project = new Project();
            bool eventRaised = false;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == "Proj")
                    eventRaised = true;
            };

            var notifyingModel = (INotifyPropertyChanged)dbModel;
            notifyingModel.PropertyChanged += handler;

            // Act
            dbModel.Pr = project;

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void Pr_CanBeSetNull()
        {
            // Arrange
            var dbModel = new DatabaseModel();

            // Act
            dbModel.Pr = null;

            // Assert
            Assert.Null(dbModel.Pr);
        }

        [Fact]
        public void ITreeViewModel_ImplementsInterface()
        {
            // Arrange
            var dbModel = new DatabaseModel();

            // Act & Assert
            Assert.IsAssignableFrom<ITreeViewModel>(dbModel);
        }

        [Fact]
        public void INotifyPropertyChanged_ImplementsInterface()
        {
            // Arrange
            var dbModel = new DatabaseModel();

            // Act & Assert
            Assert.IsAssignableFrom<INotifyPropertyChanged>(dbModel);
        }

        [Fact]
        public void IsSerializable_HasSerializableAttribute()
        {
            // Arrange & Act
            var dbModelType = typeof(DatabaseModel);

            // Assert
            Assert.True(dbModelType.IsSerializable);
        }

        [Fact]
        public void ITreeViewModel_Children_ReturnsEmptyCollection()
        {
            // Arrange
            var dbModel = new DatabaseModel();
            var treeView = (ITreeViewModel)dbModel;

            // Act
            var children = treeView.Children;

            // Assert
            Assert.Empty(children);
        }

        [Fact]
        public void ITreeViewModel_Clr_ReturnsWhiteColor()
        {
            // Arrange
            var dbModel = new DatabaseModel();
            var treeView = (ITreeViewModel)dbModel;

            // Act
            var color = treeView.Clr;

            // Assert
            Assert.Equal(Color.White, color);
        }

        [Fact]
        public void ITreeViewModel_Clr_CanBeSet()
        {
            // Arrange
            var dbModel = new DatabaseModel();
            var treeView = (ITreeViewModel)dbModel;

            // Act
            treeView.Clr = Color.Red;

            // Assert - Color setter does nothing, should still be White
            Assert.Equal(Color.White, treeView.Clr);
        }

        [Fact]
        public void PrCon_And_Pr_CanBeSetIndependently()
        {
            // Arrange
            var dbModel = new DatabaseModel();
            var container = new ProjectContainer();
            var project = new Project();

            // Act
            dbModel.PrCon = container;
            dbModel.Pr = project;

            // Assert
            Assert.Same(container, dbModel.PrCon);
            Assert.Same(project, dbModel.Pr);
        }
    }
}

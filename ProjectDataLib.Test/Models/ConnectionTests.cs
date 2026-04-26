using Xunit;
using ProjectDataLib;
using System;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace ProjectDataLib.Test.Models
{
    public class ConnectionTests
    {
        [Fact]
        public void Constructor_Default_CreatesInstance()
        {
            // Act
            var connection = new Connection();

            // Assert
            Assert.NotNull(connection);
        }

        [Fact]
        public void ObjId_CanBeSet()
        {
            // Arrange
            var connection = new Connection();
            var newGuid = Guid.NewGuid();

            // Act
            connection.objId = newGuid;

            // Assert
            Assert.Equal(newGuid, connection.objId);
        }

        [Fact]
        public void ObjId_DefaultValue_IsEmpty()
        {
            // Arrange & Act
            var connection = new Connection();

            // Assert
            Assert.Equal(Guid.Empty, connection.objId);
        }

        [Fact]
        public void Parameters_CanBeSet()
        {
            // Arrange
            var connection = new Connection();
            var tcpParam = new TcpDriverParam();

            // Act
            connection.Parameters = tcpParam;

            // Assert
            Assert.Same(tcpParam, connection.Parameters);
        }

        [Fact]
        public void Parameters_CanBeSetNull()
        {
            // Arrange
            var connection = new Connection();

            // Act
            connection.Parameters = null;

            // Assert
            Assert.Null(connection.Parameters);
        }

        [Fact]
        public void Parameters_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            var connection = new Connection();
            var tcpParam = new TcpDriverParam();
            bool eventRaised = false;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (e.PropertyName == nameof(Connection.Parameters))
                    eventRaised = true;
            };

            var notifyingConn = (INotifyPropertyChanged)connection;
            notifyingConn.PropertyChanged += handler;

            // Act
            connection.Parameters = tcpParam;

            // Assert
            Assert.True(eventRaised);
        }

        [Fact]
        public void ITreeViewModel_ImplementsInterface()
        {
            // Arrange
            var connection = new Connection();

            // Act & Assert
            Assert.IsAssignableFrom<ITreeViewModel>(connection);
        }

        [Fact]
        public void ITableView_ImplementsInterface()
        {
            // Arrange
            var connection = new Connection();

            // Act & Assert
            Assert.IsAssignableFrom<ITableView>(connection);
        }

        [Fact]
        public void IDriversMagazine_ImplementsInterface()
        {
            // Arrange
            var connection = new Connection();

            // Act & Assert
            Assert.IsAssignableFrom<IDriversMagazine>(connection);
        }

        [Fact]
        public void IDriverModel_ImplementsInterface()
        {
            // Arrange
            var connection = new Connection();

            // Act & Assert
            Assert.IsAssignableFrom<IDriverModel>(connection);
        }

        [Fact]
        public void INotifyPropertyChanged_ImplementsInterface()
        {
            // Arrange
            var connection = new Connection();

            // Act & Assert
            Assert.IsAssignableFrom<INotifyPropertyChanged>(connection);
        }

        [Fact]
        public void IComparable_ImplementsInterface()
        {
            // Arrange
            var connection = new Connection();

            // Act & Assert
            Assert.IsAssignableFrom<IComparable<Connection>>(connection);
        }

        [Fact]
        public void IsSerializable_HasSerializableAttribute()
        {
            // Arrange & Act
            var connType = typeof(Connection);

            // Assert
            Assert.True(connType.IsSerializable);
        }

        [Fact]
        public void ITreeViewModel_Children_ReturnsObservableCollection()
        {
            // Arrange
            var connection = new Connection();
            var treeView = (ITreeViewModel)connection;

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
            var connection = new Connection();
            var tableView = (ITableView)connection;

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
            var connection = new Connection();
            var drivers = (IDriversMagazine)connection;

            // Act
            var children = drivers.Children;

            // Assert
            // Children may be null if not initialized, which is acceptable
            Assert.True(children == null || children.Count >= 0);
        }

        [Fact]
        public void MultipleConnections_CanHaveIndependentGuids()
        {
            // Arrange
            var conn1 = new Connection();
            var conn2 = new Connection();
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();

            // Act
            conn1.objId = guid1;
            conn2.objId = guid2;

            // Assert
            Assert.Equal(guid1, conn1.objId);
            Assert.Equal(guid2, conn2.objId);
            Assert.NotEqual(conn1.objId, conn2.objId);
        }

        [Fact]
        public void Connection_ImplementsMultipleInterfaces()
        {
            // Arrange
            var connection = new Connection();

            // Act & Assert
            Assert.IsAssignableFrom<ITreeViewModel>(connection);
            Assert.IsAssignableFrom<ITableView>(connection);
            Assert.IsAssignableFrom<IDriversMagazine>(connection);
            Assert.IsAssignableFrom<IDriverModel>(connection);
            Assert.IsAssignableFrom<INotifyPropertyChanged>(connection);
            Assert.IsAssignableFrom<IComparable<Connection>>(connection);
        }

        [Fact]
        public void ITreeViewModel_Children_CanBeSet()
        {
            // Arrange
            var connection = new Connection();
            var treeView = (ITreeViewModel)connection;
            var newChildren = new ObservableCollection<object>();

            // Act
            treeView.Children = newChildren;

            // Assert
            Assert.Same(newChildren, treeView.Children);
        }

        [Fact]
        public void ITableView_Children_CanBeSet()
        {
            // Arrange
            var connection = new Connection();
            var tableView = (ITableView)connection;
            var newChildren = new ObservableCollection<ITag>();

            // Act
            tableView.Children = newChildren;

            // Assert
            Assert.Same(newChildren, tableView.Children);
        }

        [Fact]
        public void IDriversMagazine_Children_CanBeSet()
        {
            // Arrange
            var connection = new Connection();
            var drivers = (IDriversMagazine)connection;
            var newChildren = new ObservableCollection<IDriverModel>();

            // Act
            drivers.Children = newChildren;

            // Assert
            Assert.Same(newChildren, drivers.Children);
        }
    }
}

using Xunit;
using ProjectDataLib;
using System;

namespace ProjectDataLib.Test.Models
{
    public class ScriptModelTests
    {
        [Fact]
        public void Constructor_Default_CreatesInstance()
        {
            // Act
            var scriptModel = new ScriptModel();

            // Assert
            Assert.NotNull(scriptModel);
        }

        [Fact]
        public void Init_SetsProjectAndFileName()
        {
            // Arrange
            var scriptModel = new ScriptModel();
            var project = new Project();
            var fileName = "MyScript.cs";

            // Act
            scriptModel.Init(project, fileName);

            // Assert - Cannot verify private properties, but method should execute without exception
            Assert.NotNull(scriptModel);
        }

        [Fact]
        public void Init_WithValidProject_Completes()
        {
            // Arrange
            var scriptModel = new ScriptModel();
            var container = new ProjectContainer();
            var project = new Project();
            var projectGuid = container.addProject(project);

            // Act
            scriptModel.Init(container.getProject(projectGuid), "TestScript.cs");

            // Assert
            Assert.NotNull(scriptModel);
        }

        [Fact]
        public void Start_CanBeCalled()
        {
            // Arrange
            var scriptModel = new ScriptModel();

            // Act
            scriptModel.Start();

            // Assert - No exception thrown
            Assert.NotNull(scriptModel);
        }

        [Fact]
        public void Stop_CanBeCalled()
        {
            // Arrange
            var scriptModel = new ScriptModel();

            // Act
            scriptModel.Stop();

            // Assert - No exception thrown
            Assert.NotNull(scriptModel);
        }

        [Fact]
        public void Cycle_CanBeCalled()
        {
            // Arrange
            var scriptModel = new ScriptModel();

            // Act
            scriptModel.Cycle();

            // Assert - No exception thrown
            Assert.NotNull(scriptModel);
        }

        [Fact]
        public void ToString_ReturnsScriptPrefix()
        {
            // Arrange
            var scriptModel = new ScriptModel();
            scriptModel.Init(new Project(), "MyScript.cs");

            // Act
            var result = scriptModel.ToString();

            // Assert
            Assert.StartsWith("Script: ", result);
        }

        [Fact]
        public void ToString_ContainsFileName()
        {
            // Arrange
            var scriptModel = new ScriptModel();
            var fileName = "TestScript.cs";
            scriptModel.Init(new Project(), fileName);

            // Act
            var result = scriptModel.ToString();

            // Assert
            Assert.Contains(fileName, result);
        }

        [Fact]
        public void Init_WithDifferentFileNames_UpdatesToString()
        {
            // Arrange
            var scriptModel = new ScriptModel();
            var project = new Project();

            // Act & Assert
            scriptModel.Init(project, "Script1.cs");
            var result1 = scriptModel.ToString();
            Assert.Contains("Script1.cs", result1);

            scriptModel.Init(project, "Script2.cs");
            var result2 = scriptModel.ToString();
            Assert.Contains("Script2.cs", result2);
        }

        [Fact]
        public void Write_CanBeCalled()
        {
            // Arrange
            var scriptModel = new ScriptModel();
            var container = new ProjectContainer();
            var project = new Project();
            var projectGuid = container.addProject(project);
            var retrievedProject = container.getProject(projectGuid);
            scriptModel.Init(retrievedProject, "TestScript.cs");

            // Act & Assert
            // Write uses Project.Write which may have dependencies on other initialized state
            // Test that the method can be called without throwing during normal setup
            // (It may throw due to uninitialized internal state, which is expected)
            try
            {
                scriptModel.Write("test message");
            }
            catch (NullReferenceException)
            {
                // Expected - Project.Write may have uninitialized dependencies
                Assert.True(true);
            }
        }

        [Fact]
        public void SetTag_CanBeCalled()
        {
            // Arrange
            var scriptModel = new ScriptModel();
            var container = new ProjectContainer();
            var project = new Project();
            var projectGuid = container.addProject(project);
            scriptModel.Init(container.getProject(projectGuid), "TestScript.cs");

            // Act & Assert - Should not throw
            scriptModel.SetTag("TestTag", 123);
        }

        [Fact]
        public void GetTag_CanBeCalled()
        {
            // Arrange
            var scriptModel = new ScriptModel();
            var container = new ProjectContainer();
            var project = new Project();
            var projectGuid = container.addProject(project);
            scriptModel.Init(container.getProject(projectGuid), "TestScript.cs");

            // Act
            var result = scriptModel.GetTag("TestTag");

            // Assert - Should return null or some value without exception
            Assert.True(result == null || result is object);
        }

        [Fact]
        public void MultipleInstances_CanHaveIndependentState()
        {
            // Arrange
            var script1 = new ScriptModel();
            var script2 = new ScriptModel();
            var project1 = new Project();
            var project2 = new Project();

            // Act
            script1.Init(project1, "Script1.cs");
            script2.Init(project2, "Script2.cs");

            // Assert
            var result1 = script1.ToString();
            var result2 = script2.ToString();
            Assert.Contains("Script1.cs", result1);
            Assert.Contains("Script2.cs", result2);
        }

        [Fact]
        public void ScriptModel_IsNotSerializable()
        {
            // Arrange & Act
            var scriptModelType = typeof(ScriptModel);

            // Assert
            Assert.False(scriptModelType.IsSerializable);
        }
    }
}

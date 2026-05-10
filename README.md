# Fenix Modbus

Wiki: https://github.com/DanielSan1000/Fenix-Modbus/wiki

DGSoft - Author Site : https://dgsoftlabs.com

# About FenixModbusS7



# History Summary
Version	Nature of Changes
v3.3.0 → v3.3.1	Cleanup and documentation
v3.3.2	UI refactoring (async), database, layout serialization
v3.3.3.0	CI/CD pipeline (GitHub Actions, Advanced Installer)
v3.3.4.0	Testing, NuGet, CI/CD improvements
v3.4.0.0	⚡ .NET 10 Upgrade, WPF styles, cleaning legacy code
v4.0.1.0	Bug fixes (database, CSV, installer)
v4.0.2.0	Bug fixes, testing
v4.0.3.0	🎯 Multi-axis, timers, shortcuts, many new features
v4.0.4.0	Stabilization, refactoring, chart and table improvements

# 📋 Change History — Fenix

## v4.0.4.0
Category	Description of Changes
📈 Trends	Protection against lack of points on the chart
📋 TableView	Fixes problems with TableView
🏷️ Properties	Locking root names for nested parameters
🔧 Initialization	Fixing several initialization issues
🧹 Refactoring	Moving converters, removing InFile references, correcting names, - - moving classes to appropriate locations

## v4.0.3.0 
📈 Charts — Multi-axis	Adding multi-axis functionality in trends
🎚️ Charts — Axis tier	Adding axis tier functionality
🔍 Charts — Zoom	Adding a clear text field for axis limits and zoom reset
⏱️ Timers	Managing timers from the tree, adding timer persistence
⌨️ Keyboard Shortcuts	Adding keyboard shortcuts
📡 Communication View	Adding an index, refactoring converters, improving button labels
📝 Scripts	Fixing CRUD management of script files, improved error messages
🏷️ Properties	Fixing column width change behavior, fixing bugs
🔌 Connections	Refactoring models and connections
📋 Output	Adding copy from output
🏷️ InTag	Fixing name change after click
📊 Tables	Fixing problems with charts and tables
Feature-rich version — many new features, especially in the chart area.

## v4.0.2.0
🧪 Testing	Generating tests
🐛 Bugs	Fixing several bugs in the application, fixing bugs in CommunicationView
🧹 Cleanup	Ignoring FenixModbusS7 cache

## v4.0.1.0
🗄️ Database	Fixing "show files" option, activating database access after project creation
📤 CSV Export	Adding pivot in the table when exporting CSV
🔌 Connections	Fixing layout in AddConnection
🔧 Installer	Correcting installation path
🔢 Version	Updating version to 4.0.1.0

## v3.4.0.0 
🚀 .NET 10	Upgrade application to .NET 10
🗑️ Removals	Removing WebServer node, old HTTP/web resources, App.config
🎨 UI — WPF	Adding implicit WPF styles, centralizing UI theming, changing icons to emojis
📊 Database	Adding snapshot saving and pivoted DataGrid in DBTableView, database migration
📝 Scripts	Refactoring script management, expanding code editor
⚙️ Properties	Refactoring PropertyGrid, centralizing access control
📂 Projects	Handling only .pse files, improved installer (EN fixes)
🏗️ CI/CD	Updating pipeline, new FenixModbusS7 installer
This is a groundbreaking version — migration to .NET 10, cleaning legacy code, major UI modernization.

## v3.3.4.0
🧪 Testing	Adding new test project, asynchronous version control
📦 NuGet	Updating NuGet packages and binding redirects
🏗️ CI/CD	Adding aiproj-demo job to GitHub Actions, improving path compatibility (Windows/cross-platform)
🔧 Refactoring	Removing FenixInstall project, refactoring FenixManager
🎨 UI	Refactoring project properties, updating icons
🔢 Version	Bump to 3.3.4, adding "Version" project with version.xml

## v3.3.3.0
🏗️ CI/CD — GitHub Actions	Full implementation of CI/CD pipeline — creating dotnet.yml, configuring MSBuild, NuGet, Visual Studio 2022, Advanced Installer, artifact upload
📦 SQLite	Adding SQLite.Interop.dll, SQLite components in FenixSetup
🔧 Building	Dynamic location finding of devenv.com, Out-of-Proc Build configuration, path and syntax improvements
📝 Documentation	Updating README.md, .gitignore
🔢 Version	Bumping version to 3.3.3.0
Note: This version was mainly about automating the build and CI/CD process.

## v3.3.2
🔄 UI Refactoring	Asynchronous data retrieval, refactoring ChartView for async/await, improving interaction handling
🗄️ Database	Expanding database interaction functionality
📐 Layout	Adding layout serialization, improving handling
🖥️ TreeViewManager	Expanding TreeViewManager, updating color and status text
🎨 UI	Updating icons, refactoring converters, removing DataGridExtensions
🔧 Configuration	Removing app.config from multiple projects, updating paths in ModbusMaster
📝 Documentation	Improving documentation, naming, error handling, adding XML documentation
➕ New driver	Adding new driver
🧹 Cleanup	Library cleanup, merging all projects, updating .gitignore
🔧 Setup	Updating installer project

## v3.3.1
🧹 Organization	Removing old/unnecessary files and unused libraries
📂 Reorganization	Reorganizing project structure
📚 Documentation	Adding Wiki link, updating help links
🔢 Version	Updating version number, adding new version
🧹 Code	Removing redundant comments

## v3.3.0 (Initial Release)
Starting point for the project history.
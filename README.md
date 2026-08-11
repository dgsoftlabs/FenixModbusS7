# Fenix Modbus S7

> Industrial protocol communication suite for Modbus and SIEMENS S7-300/400 devices

[![GitHub Release](https://img.shields.io/github/v/release/dgsoftlabs/Fenix?style=flat-square)](https://github.com/dgsoftlabs/Fenix/releases)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)](LICENSE)

**Links:**
- 📖 [Wiki](https://github.com/DanielSan1000/Fenix-Modbus/wiki)
- 🌐 [DGSoft Labs](https://dgsoftlabs.com)

---

## 📁 Projects Overview

This repository contains multiple interconnected applications and libraries for industrial protocol communication:

### 🖥️ Applications

| Project | Type | Purpose |
|---------|------|---------|
| **Fenix** | WPF Desktop App | Primary application for Modbus and S7 communication with real-time visualization, trend charts, database storage, and device management. Supports TCP, RTU, and ASCII protocols. |
| **FenixServer** | WPF Server Host App | Desktop host that starts, stops and monitors the Fenix Server API (ASP.NET Core). |
| **FenixServer.Api** | Console App (ASP.NET Core) | RESTful Web API backend ("Fenix Server API") for remote device communication and distributed endpoint data access. Fully console-hosted application. |

### 📚 Libraries

| Project | Purpose |
|---------|---------|
| **ProjectDataLib** | Core data models, serialization, and utilities. Supports .pse project format with legacy .psx conversion. |
| **ProjectDataLib.Test** | Comprehensive test suite for data library validation. |
| **ModbusMasterTCP** | TCP Modbus protocol implementation with socket management. |
| **ModbusMasterRTU** | RTU serial Modbus protocol with CRC validation. |
| **ModbusMasterASCII** | ASCII Modbus protocol implementation with LRC checksum. |
| **S7-300-400 Ethernet** | SIEMENS S7 PLC Ethernet driver for industrial communications. |

---

## ⚙️ Technology Stack

- **.NET 10.0** (LTS) - Latest framework targeting
- **WPF** - Modern Windows Presentation Foundation UI
- **ASP.NET Core** - RESTful web services
- **MVVM Architecture** - Separation of concerns design pattern
- **SQLite** - Local data storage
- **Entity Framework** - ORM for database operations

---

## 🚀 Quick Start

### Requirements
- Windows 7 or later
- .NET 10.0 Runtime
- Visual Studio 2022 (for development)

### Building
```bash
dotnet build
dotnet build --configuration Release
```

### Running
```bash
# Main application
dotnet run --project Fenix

# Server host (desktop)
dotnet run --project FenixServer

# Web API
dotnet run --project FenixServer.Api
```

---

## 📋 Version History

### v4.1.0
| Feature | Details |
|---------|---------|
| 🚀 .NET 10.0 | Full migration from .NET Framework to .NET 10.0 LTS |
| 🎨 WPF Migration | Complete conversion of FenixServer from WinForms to WPF with consistent styling |
| 🌐 ASP.NET Core Server | New server-hosted approach with improved web API functionality and isolation |
| 📡 Endpoint Refactoring | Better endpoint implementation and legacy endpoint compatibility management |
| 📂 Project Format | Support for .pse project format; legacy .psx files can be opened for conversion but only save in new format |
| 🧪 Testing Framework | Added comprehensive xUnit test suite and ProjectDataLib unit tests |
| 🔧 Build Pipeline | Enhanced CI/CD workflow with .NET 10 toolchain and automated MSI installer generation |
| 📊 UI Refinements | Property editor constraints and improved user feedback on project state |

### v4.0.4.0
| Feature | Details |
|---------|---------|
| 📈 Trends | Protection against lack of points on the chart |
| 📋 TableView | Fixes for TableView problems |
| 🏷️ Properties | Locking root names for nested parameters |
| 🔧 Initialization | Several initialization fixes |
| 🧹 Refactoring | Code organization and cleanup |

### v4.0.3.0
| Feature | Details |
|---------|---------|
| 📈 Charts | Multi-axis functionality in trends |
| 🎚️ Axis | Tier functionality for better visualization |
| 🔍 Zoom | Clear text field and zoom reset controls |
| ⏱️ Timers | Tree-based timer management with persistence |
| ⌨️ Shortcuts | Keyboard shortcuts for improved workflow |
| 📡 Communication | Enhanced view with indexing and converters |
| 📝 Scripts | CRUD management improvements |
| 🏷️ Properties | Column width behavior and bug fixes |
| 🔌 Connections | Model and connection refactoring |
| 📊 Tables | Chart and table compatibility improvements |

### v4.0.2.0
| Category | Details |
|----------|---------|
| 🧪 Testing | Test suite generation |
| 🐛 Bugs | Bug fixes in application and CommunicationView |
| 🧹 Cleanup | Cache management improvements |

### v4.0.1.0
| Category | Details |
|----------|---------|
| 🗄️ Database | File management and access activation |
| 📤 Export | Pivot support for CSV exports |
| 🔌 Connections | Layout improvements |
| 🔧 Installer | Installation path corrections |

### v3.4.0.0 - Major Release
| Category | Details |
|----------|---------|
| 🚀 Framework | Upgraded to .NET 10 |
| 🎨 UI | WPF styles and emoji icons |
| 📊 Database | Snapshot saving and pivoted DataGrid |
| 📝 Scripts | Enhanced script management |
| ⚙️ Properties | Centralized PropertyGrid refactoring |
| 📂 Projects | .pse format support (legacy .psx migration) |

### v3.3.4.0
- 🧪 Testing: New test project
- 📦 NuGet: Package updates and binding redirects
- 🏗️ CI/CD: GitHub Actions pipeline
- 🎨 UI: Property refinements

### v3.3.3.0
- 🏗️ GitHub Actions: Full CI/CD implementation
- 📦 SQLite: Interop integration
- 🔧 Build: Dynamic toolchain configuration

### v3.3.2
- 🔄 UI: Async/await refactoring
- 🗄️ Database: Enhanced interactions
- 📐 Layout: Serialization support
- 🎨 UI: Icon and converter updates

### v3.3.1
- 🧹 Code cleanup and organization
- 📚 Documentation improvements
- 🔢 Version updates

### v3.3.0
- ✨ Initial release with core functionality
- 🔧 Move to .NET Framework 4.8
- 📦 Update all libraries
- 🎨 Better colorizing selection in tags range

### v3.2.0
- 🔍 CommunicationView for Diff Time used "Integer greater than" filter
- 📦 Update DataGridExtension 1.0.33 → 1.0.44
- 📦 Update MahApps.Metro 1.3.0 → 1.4.0
- 📦 Update SQLite 1.0.102 → 1.0.103
- 📦 Update OxyPlot 1.0.0.2182 → 2.0.0.0933
- 📦 Update Newtonsoft.Json 9.0.1 → 9.0.2
- 📁 Added Logs folder for unhandled exception tracking

### v3.1.9
- 🐛 Fixed writing data to S7 DB blocks
- 🐛 Fixed default parameters when creating a connection
- 🌐 Replaced local Help file with Website help
- 📡 Siemens S7 driver — all events connected to CommunicationView
- 📋 CommunicationView: added driver name column
- 📋 CommunicationView: record count in GridView
- 📋 CommunicationView: save to CSV
- 📋 CommunicationView: save to clipboard
- 📋 CommunicationView: basic data filtering
- 🗄️ Database CSV export: use dot as decimal separator
- 📦 Update OxyPlot 1.0.0.2176 → 1.0.0.2182
- 📦 Update MahApps.Metro 1.3.0.166 → 1.3.0.188
- 📦 Update SC-Script.bin 3.13.2 → 3.14
- 📦 Update TaskScheduler 2.5.20 → 2.5.21

### v3.1.7
- 🐛 Fixed names in Tag and InTag windows
- 📄 XML file and start removing *.psf
- 🚫 Removed automatic save on close
- 🐛 Fixed bugs related to ChartView and saving parameters
- 🐛 TableView: fixed missing Tag value when row was selected
- 🎨 Added color rectangle in TreeView for easier identification
- 🐛 Fixed database issue with Tag script usage
- 📦 Update OxyPlot 1.0.0.2175 → 1.0.0.2176
- 📦 Update SC-Script.bin 3.12.2.1 → 3.13.2
- 📦 Update Newtonsoft.Json 9.0.1-beta → 9.0.1
- 📦 Update System.Data.Sql 1.0.101 → 1.0.102

### v3.1.6
- 🎨 Introduced Metro UI

### v3.1.2
- 🐛 Bug fixes
- 🗄️ Database for Chart

### v3.1.1
- 🖥️ Everything is WPF
- ✨ Lots of new features

### v3.1.0
- 💾 Added saving windows layout
- 🐛 Fixed problems reading data higher than 16,000
- 🐛 Repaired various bugs

### v3.0.9
- 🐛 Fixed bug: name not assigned when adding range tags
- 🔌 Added new Siemens S7-300/400 driver
- 🎨 New TableView design (WPF)
- 🧹 Simplified interface (removed some features)
- 🚀 Moved Start/Stop to Fenix Manager

### v3.0.8
- 🔧 Changed Framework to .NET 4.6
- 🏷️ Improved algorithm for detecting duplicate Tag names
- 🐛 Fixed sbyte type issues
- 📝 Code Editor: save selected text to clipboard as HTML
- 📝 Code Editor: auto-selects JavaScript highlighting on startup
- 🔤 ASCII formatting

### v3.0.7
- 🏷️ Renamed "Folder Name" to "Device Name" for Device Object
- 🔌 Driver changes (TCP/RTU/ASCII) for better request management
- 🪟 Changed Window Management (windows start at the bottom)
- 📝 Changed Code Editor to AvalonEdit
- 🔄 Possibility to start another editor during communication
- 🔧 Added reConfig() method to driver for online Tag parameter changes
- 🗑️ Removed Stack button from Output
- 📖 Added Help file

### v3.0.6
- 🌐 Work on the web server

### v3.0.5
- 🐛 Fixed errors
- 📝 Work on Scintilla editor
- ⚙️ Improved parameter work

### v3.0.4
- 📈 Added ZedGraph chart
- 🧹 Code refactoring

### v3.0.3
- ⚡ Optimization

### v3.0.2
- 🐛 Fixed problems with creating a new file
- 🔄 Added file renaming when changing data
- 🎨 Added appropriate icons for files
- ⚙️ Script engine

### v3.0.1
- 🏷️ Added ability to format tag values
- 🔌 Option to enable output driver
- 🐛 Fixed various errors

### v3.0.0
- 🪟 Output driver as independent window

### v2.6.9
- 🌐 Added file handling through WebServer
- 🏷️ InternalsTag

### v2.6.8
- 🐛 Fixed scripts
- 🏷️ Added internal Tags
- 🌐 Formatting displayed numbers for WebServer
- 🏷️ Tag: added linear scaling
- 🗂️ Removed folder structure from interface

### v2.6.7
- 🏷️ Introduced global names for tags; better selection for HttpServer
- 🐛 Fixed error when closing Properties Manager and double-clicking TreeView
- 🔧 Added option to reset assemblyPath in AutoSearchDriver
- 🎨 Changed Icon for ServerHttp

### v2.6.6
- 🪟 Added window docking
- 🗑️ Removed TrayIcon, Alignment Window
- 🔄 Replaced Forms (tableView, viewLogger, chartView) with new parent-type windows
- 🪟 Fenix Manager is an MDI window
- 🌐 WebServer renamed and moved to external WPF application
- 🗂️ Sub-window menu stack with smaller icons
- 🏷️ Added current version label to window

### v2.6.5
- 🐛 TableView: fixed cell selection jumping to first cell after value change
- 🐛 Fixed inability to change secAddress in TableView
- 📋 TableView: added device parameterization

### v2.6.4
- 🐛 Fixed communication stopping after SetValue

### v2.6.3
- 🔄 Fixed multi-window monitoring; better Tag management
- ⚡ Optimized communication for multi-window monitoring
- 🌐 Added handling of deleting project elements through WebServer
- 🔘 Added Boolean buttons in TableView
- ✏️ Editing possibility in TableView

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit Pull Requests.

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## ✉️ Support

For issues, questions, or suggestions, please open an [issue](https://github.com/dgsoftlabs/Fenix/issues) on GitHub.

---

**Made with ❤️ by DGSoft Labs**
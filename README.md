# Fenix Modbus S7

> Industrial protocol communication suite for Modbus and SIEMENS S7-300/400 devices

[![GitHub Release](https://img.shields.io/github/v/release/dgsoftlabs/FenixModbusS7?style=flat-square)](https://github.com/dgsoftlabs/FenixModbusS7/releases)
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
| **FenixModbusS7** | WPF Desktop App | Primary application for Modbus and S7 communication with real-time visualization, trend charts, database storage, and device management. Supports TCP, RTU, and ASCII protocols. |
| **FenixServerS7** | WPF Server App | Server-hosted S7 communication application with ASP.NET Core integration for enterprise deployments. |
| **FenixServer.Web** | ASP.NET Core | RESTful Web API backend for remote device communication and distributed endpoint data access. |

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
dotnet run --project FenixModbusS7

# Web API
dotnet run --project FenixServer.Web
```

---

## 📋 Version History

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

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit Pull Requests.

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## ✉️ Support

For issues, questions, or suggestions, please open an [issue](https://github.com/dgsoftlabs/FenixModbusS7/issues) on GitHub.

---

**Made with ❤️ by DGSoft Labs**
<p align="center">
  <img src="docs/logo.svg" alt="Messentra" width="480" />
</p>

<p align="center">
  <a href="https://github.com/kamil-czarnecki/Messentra/releases"><img src="https://img.shields.io/badge/version-0.1.0-2563EB?style=flat-square" alt="Version" /></a>
  <a href="https://dotnet.microsoft.com/en-us/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet" alt=".NET 10" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-GPL--3.0-22C55E?style=flat-square" alt="License: GPL-3.0" /></a>
  <img src="https://img.shields.io/badge/platform-macOS%20%7C%20Windows%20%7C%20Linux-38BDF8?style=flat-square" alt="Platform: macOS | Windows | Linux" />
</p>

<p align="center">
  A cross-platform desktop explorer for <strong>Azure Service Bus</strong> — built with Blazor and Electron.NET.
</p>

---

## About

Messentra is a cross-platform desktop application for exploring and managing Azure Service Bus namespaces. It provides a clean, modern UI for browsing resources, inspecting messages, and sending to queues and topics — without leaving your desktop. It supports both **Connection String** and **Entra ID** authentication, making it suitable for local development as well as enterprise environments.

Built with **Blazor Server**, **Electron.NET**, **MudBlazor**, and **Fluxor**.

---

## Features

### 🔌 Connection Management
- Add, edit, and delete named namespace connections
- **Connection String** — paste a standard Azure Service Bus connection string
- **Entra ID** — authenticate using Tenant ID + Client ID (Azure AD / Microsoft Entra)
- Multiple connections managed side by side in the Explorer

### 🗂️ Resource Explorer
- Browse **queues**, **topics**, and **subscriptions** in a collapsible tree
- Live message counts (active / dead-letter) shown inline in the tree
- Per-resource **Overview** tab: status, creation date, last updated date
- **Properties** tab: full resource configuration and storage & quota details
- **Dead-Letter** sub-queue accessible directly from the resource panel

### 📨 Message Fetching
- Fetch messages from queues and topic subscriptions
- Choose fetch mode: **Peek** (non-destructive) or **Receive** (destructive)
- Configurable fetch count
- Message list displays: Sequence #, Message ID, Label, Enqueued Time, Delivery Count
- Inline message viewer with **Body** (syntax-highlighted) and **Properties** tabs
- Properties include all broker properties (e.g. `ContentType`, `CorrelationId`, `TimeToLive`) and custom application properties

### 📤 Send Message
- Send a single message to any queue or topic
- More options coming soon

### 📋 Activity Log
- Persistent activity log panel at the bottom showing connection and fetch events across all namespaces

---

## Screenshots

### Welcome
![Welcome screen](docs/welcome.png)

### Resource Explorer
![Resource explorer with queue overview](docs/resources.png)

### Messages
![Message list with body and properties viewer](docs/messages.png)

### Connections
![Connection management](docs/connections.png)

---

## Alternative Tools

| Tool | Description |
|------|-------------|
| [ServiceBusExplorer](https://github.com/paolosalvatori/ServiceBusExplorer) | Feature-rich Windows desktop tool for Azure Service Bus by Paolo Salvatori |

---

## License

Messentra is licensed under the [GNU General Public License v3.0](LICENSE).

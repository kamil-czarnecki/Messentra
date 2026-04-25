<p align="center">
  <img src="docs/logo.svg" alt="Messentra - Azure Service Bus Desktop Explorer" width="480" />
</p>

# Messentra - Azure Service Bus Desktop Explorer

<p align="center">
  <a href="https://github.com/kamil-czarnecki/Messentra/releases"><img src="https://img.shields.io/github/v/release/kamil-czarnecki/Messentra?style=flat-square&color=2563EB" alt="Version" /></a>
  <a href="https://dotnet.microsoft.com/en-us/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet" alt=".NET 10" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-GPL--3.0-22C55E?style=flat-square" alt="License: GPL-3.0" /></a>
  <img src="https://img.shields.io/badge/platform-macOS%20%7C%20Windows%20%7C%20Linux-38BDF8?style=flat-square" alt="Platform: macOS | Windows | Linux" />
</p>

<p align="center">
  Cross-platform desktop app for <strong>Azure Service Bus</strong>: browse resources, inspect and operate on messages, manage dead-letter queues.
  Built with Blazor Server, Electron.NET, MudBlazor, and Fluxor.
</p>

---

## Features

### 🗂️ Resource Explorer
Browse **queues**, **topics**, and **subscriptions** in a collapsible tree with live active and dead-letter message counts. Each resource has an **Overview** tab (status, dates) and a **Properties** tab (full configuration, storage and quota details). The dead-letter sub-queue is accessible directly from the resource panel.

### 📨 Message Fetching
Fetch messages from queues and topic subscriptions with three modes:

- **Peek** - non-destructive; configurable start sequence number for offset-based peeking
- **Receive / PeekLock** - locks messages for explicit settlement; configurable wait time
- **Receive / ReceiveAndDelete** - removes messages immediately on receive; configurable wait time

The message list shows Sequence #, Message ID, Label, Enqueued Time, and Delivery Count. Dead-letter views add Reason and Error Description. Live search runs across all visible fields, and messages can be multi-selected for bulk operations.

The inline message viewer has a **Body** tab with syntax highlighting and a **Properties** tab covering all broker properties (`ContentType`, `CorrelationId`, `TimeToLive`, etc.) plus custom application properties.

### �️ Message Grid Views
Customize which columns appear in the message grid via a **right-click context menu** on any column header. Changes apply globally to all queues and subscriptions.

**Column management:**
- **Add column** — pick any broker property (e.g. `CorrelationId`, `SessionId`, `TimeToLive`) or enter a free-form application property key; optionally override the column title
- **Remove column** — removes the column from the current view (SEQ # is permanently locked)
- **Reorder columns** — drag-and-drop any column header to rearrange

**Named views:**
- **Switch view** — select a saved view from the context menu; a checkmark shows the active view
- **Save current view** — overwrite the active view with the current column layout (disabled for the built-in Default view)
- **Save view as…** — create a new named view from the current layout; the new view becomes active
- **Delete view** — delete the active user-defined view; falls back to Default (disabled for the built-in Default view)

The built-in **Default** view shows: SEQ #, MESSAGE ID, LABEL, ENQUEUED TIME, DELIVERY COUNT. Dead-letter views automatically append **REASON** and **ERROR DESCRIPTION** columns regardless of the active view. All user-defined views are persisted locally across sessions.

### �🛠️ Message Operations
Available actions depend on the fetch mode:

- **Resend** - re-sends selected message(s) to the queue or topic. In PeekLock mode, a successful resend completes the original. In Peek and ReceiveAndDelete modes this can create duplicates intentionally.
- **Complete** *(PeekLock only)* - settle and remove message(s)
- **Abandon** *(PeekLock only)* - release the lock so message(s) become available again
- **Dead-Letter** *(PeekLock only)* - move message(s) to the dead-letter sub-queue

### 📤 Send Message
Send a message to any queue or topic. Supports **JSON** (with one-click formatting) and **Plain Text** bodies, all standard broker properties (Label, Message ID, Correlation ID, Partition Key, Session ID, Scheduled Enqueue Time, TTL, and more), and custom application properties as key/value pairs.

### 🔎 Resource Search & Filtering
Smart search bar in the Explorer tree with **special-phrase autocomplete**:

- Plain text - filter by resource name
- `namespace:x` - limit the tree to a specific namespace; autocompletes connected namespace names
- `has:dlq` - show only resources with active dead-letter messages
- Phrases combine freely: `namespace:prod has:dlq`, `namespace:prod orders`

### 📁 Custom Folders
Virtual groups inside a namespace for organizing resources by workflow, team, or incident context, without changing anything in Azure. Create, rename, and delete folders per namespace; assign queues and subscriptions to one or more folders. Adding a topic to a folder automatically includes its subscriptions. Refreshing a folder refreshes all resources inside it.

### 📦 Import & Export
Export messages from a queue or subscription to a JSON file for backup, replay, or sharing between environments. Import messages from a JSON file into a queue or topic. Both run as background jobs with status visible on the Jobs page.

> **Note:** Import may introduce duplicate messages.

### 🔌 Connections
Add, edit, and delete named namespace connections. Two authentication methods are supported:

- **Connection String** - paste a standard Azure Service Bus connection string
- **Entra ID** - authenticate with Tenant ID + Client ID (Azure AD / Microsoft Entra)

Multiple connections are managed side by side in the Explorer.

### 🧪 Azure Service Bus Emulator
Connect using a connection string in this format:

```text
Endpoint=sb://localhost:5300;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;
```

The port in `Endpoint` (e.g. `:5300`) is required for emulator management operations. Supported: viewing resources, sending messages, fetching messages. Active and dead-letter message counts are not available with the emulator.

### Other
- **Activity Log** - persistent panel at the bottom showing connection and fetch events across all namespaces
- **Dark Mode** - built-in dark theme
- **Keyboard shortcuts** - `F5` refreshes the selected resource in the Explorer tree; `↑`/`↓` navigates the message list

---

## Screenshots

### Welcome
![Messentra welcome screen](docs/welcome.png)

### Dark Mode
![Messentra dark mode](docs/dark_mode.png)

### Resource Explorer
![Messentra resource explorer](docs/resources.png)

### Resource Search & Filtering
![Messentra resource search](docs/resources_search.png)

### Messages
![Messentra message inspector](docs/messages.png)

### Jobs
![Messentra jobs screen](docs/jobs.png)

### Connections
![Messentra connection manager](docs/connections.png)

---

## Alternative Tools

| Tool | Description |
|------|-------------|
| [ServiceBusExplorer](https://github.com/paolosalvatori/ServiceBusExplorer) | Feature-rich Windows desktop tool for Azure Service Bus by Paolo Salvatori |

---

## License

Messentra is licensed under the [GNU General Public License v3.0](LICENSE).
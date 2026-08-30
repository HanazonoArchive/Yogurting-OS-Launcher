# 🎓 Yogurting Online - Modern Server Engine (.NET 8)

> **The Definitive Open-Source Server Implementation for Yogurting Online (ヨーグルティング / 요구르팅)**

This repository houses the modern, high-performance, asynchronous server backend for **Yogurting Online**. Built with **C# and .NET 8**, it replaces the legacy Delphi server architecture with a modular, data-driven, and scalable distributed system.

Every opcode, data structure, math formula, and network handshake is directly reverse-engineered and verified against the original client binaries (`LuGameEngineDX9.dll`, `Yogurting.exe`), raw DMS network grammars (`dictionary/dms/`), and legacy server logic (`server_legacy/DELPHI_PROJECT/`).

---

## 📑 Table of Contents

- [Server Architecture](#-server-architecture)
- [Subsystems & Implemented Features](#-subsystems--implemented-features)
- [Directory Layout](#-directory-layout)
- [Solution & Project Structure](#-solution--project-structure)
- [Getting Started](#-getting-started)
  - [Prerequisites](#prerequisites)
  - [1. Creating an Account](#1-creating-an-account)
  - [2. Starting the Server](#2-starting-the-server)
  - [3. Interactive CLI Commands](#3-interactive-cli-commands)
  - [4. Operational Execution Modes](#4-operational-execution-modes)
- [Configuration Reference](#-configuration-reference)
- [Data-Driven Game Database](#-data-driven-game-database)
- [Protocol Documentation](#-protocol-documentation)

---

## 🏛️ Server Architecture

Yogurting Online utilizes a distributed multi-daemon topology running across dedicated TCP endpoints:

```
                                  +-------------------+
                                  |  Yogurting Client |
                                  +---------+---------+
                                            |
         +-------------------+--------------+--------------+-------------------+
         |                   |                             |                   |
         v                   v                             v                   v
+-----------------+ +-----------------+           +-----------------+ +-----------------+
|   LoginServer   | |   FieldServer   |           |  EpisodeServer  | |   CommServer    |
|   Port: 10000   | |   Port: 10002   |           |   Port: 10003   | |   Port: 10004   |
+-----------------+ +-----------------+           +-----------------+ +-----------------+
| • Authentication| | • 3D Campus Maps|           | • Lobby Rooms   | | • Friend Lists  |
| • World List    | | • Monster AI 4Hz|           | • Matchmaking   | | • Whisper Relay |
| • Chara Creator | | • Combat & Skills           | • Mission Logic | | • Club / Guild  |
| • Character Sel | | • Inventory/Shop|           | • Clear Ranking | | • Presence Sync |
+-----------------+ +-----------------+           +-----------------+ +-----------------+
```

### Server Daemon Breakdown

| Server Daemon | Default Port | Opcode Range | Primary Responsibilities |
| :--- | :--- | :--- | :--- |
| **LoginServer** | `10000` | `0x7595` – `0x75AE` | Account authentication, session issuance, character selection, 3D character creator & customization. |
| **FieldServer** | `10002` | `0x5200` – `0x5274`<br>`0x7900` – `0x79FF`<br>`0xA400` – `0xA415` | Campus maps (Estiva / Soyul), hunting zones, spatial grid tracking, NPC dialogue, player movement, combat, weapon DEX, shops, trade, inventory. |
| **EpisodeServer** | `10003` | `0x765E` – `0x7695`<br>`0x7990` – `0x79BF` | Episode lobby browser, waiting rooms, stage matchmaking, mission objectives, grade evaluations, reward distribution. |
| **CommServer** | `10004` | `0x7604`<br>`0x7720` – `0x7760` | Cross-server instant messaging, whispers, friendship management, player status notifications, clubs/guilds. |

---

## ⚡ Subsystems & Implemented Features

- **High-Performance Asynchronous I/O**: Built on `System.IO.Pipelines` and non-blocking asynchronous TCP server loops (`AsyncTcpServer`), ensuring zero-allocation packet slicing and high throughput.
- **Data-Driven Database Engine (`GameDatabase`)**: 1-to-1 recreation of original game database structures loaded dynamically from 31 tabular data files (`data/db/`) without hardcoded game values.
- **Account & Character Management**:
  - MD5 password verification and JSON account persistence (`data/save/`).
  - Full 3D Character Creation pipeline with name validation, phone number assignment, gender, hair, and face selection.
  - Automatic starter equipment and starter weapon configuration dynamically loaded from `config/starter_items.json`.
- **Campus & Spatial World (`WorldManager` & `FieldInstance`)**:
  - Multi-zone management for Estiva Academy, Soyul Middle School, and hunting grounds.
  - Spatial field movement synchronization and portal / warp gate transitions (`0x7965` / `0x7966` / `0x7967`).
  - Interactive kiosks, terminal objects, and campus NPCs with dialogue trees.
- **Real-Time Combat & AI Engine**:
  - Dedicated 4Hz (250ms tick) Monster AI loop handling aggro, pursuit, field patrol paths, and combat state machines.
  - Original Delphi formula parity for attack combos, critical hit chances, defense reductions, EXP scaling, and drop tables.
  - Full 4-Weapon DEX proficiency tracking (Blade, Glove, Blunt, Spirit).
- **Inventory, Equipment & Economy**:
  - 12-slot paperdoll visual synchronization.
  - Equipment (BeItem / ByulItem), Consumables (CoItem), Reinforcement stones, and Crystallization (`0x79A8` / `0x79AA`).
  - NPC Shop buy/sell transactions, vending machines, and player-to-player trade systems.
- **Lobby & Episode Matchmaking**:
  - Room listing, creation, team selection, and host start orchestration.
  - Mission status tracking, clear criteria, and reward box distribution.

---

## 📂 Directory Layout

```
server_modern/
├── Yogurting.sln                # Master Visual Studio / .NET solution
├── startServer.bat              # One-click standalone server launcher
├── createAccount.bat            # Interactive CLI account creator
├── README.md                    # This master documentation
│
├── config/                      # Server configuration files
│   ├── server.json              # Network bindings, server name, MOTD, directories
│   └── starter_items.json       # Starter kits per school/gender for new characters
│
├── data/                        # Dynamic game data and runtime storage
│   ├── db/                      # 31 parameter tables (Items, Mobs, Spawns, Dex, Skills)
│   ├── save/                    # Persistent player JSON save files
│   └── score/                   # Episode clear ranking records
│
├── dictionary/                  # The Yogurting Online Revival Encyclopedia
│   ├── README.md                # 9-volume comprehensive technical protocol manual
│   └── dms/                     # 270 raw AnaYg DMS packet definition scripts
│
├── logs/                        # Daily rolling log files
│
└── src/                         # C# Source Code
    ├── Yogurting.Core/          # Packets, Opcodes, Network Pipeline, Data Models
    ├── Yogurting.Data/          # UYgDB Engine, Table Loaders, Account Repositories
    └── Yogurting.Server/        # Server Daemons, Handlers, World Instances, AI Engine
```

---

## 🧩 Solution & Project Structure

The codebase is partitioned into three decoupled .NET 8 class libraries and executables:

### 1. `Yogurting.Core` (`src/Yogurting.Core/`)
- **Network Engine**: `AsyncTcpServer`, `PacketReader`, `PacketWriter`, `PacketDispatcher<TState>`, `PacketHandlerAttribute`.
- **Protocol Grammar**: Master opcode catalog (`PacketOpcodes.cs`) and binary packet builder methods (`YogurtingPackets.cs`).
- **Data Models**: `Player`, `PlayerSessionState`, `FieldMonster`, `ShopProduct`, `StarterConfig`.
- **Logging**: Thread-safe colored console and rolling file logger.

### 2. `Yogurting.Data` (`src/Yogurting.Data/`)
- **Game Database Engine (`GameDatabase.cs`)**: Reads and parses `data/db/*.txt` tables (Item types, NPC definitions, Episode parameters, ExpTable, DexTable, MonsterBasis, Spawns, Reinforcements, etc.).
- **Map Grid Manager (`MapGridManager.cs`)**: Binary collision and grid partitioning parsed from `data/db/map.db`.
- **Persistence (`JsonAccountRepository.cs`)**: Thread-safe asynchronous JSON file storage for player accounts.

### 3. `Yogurting.Server` (`src/Yogurting.Server/`)
- **Server Bootstrap (`Program.cs`)**: Main entry point, service orchestrator, port listeners, and interactive administrative console.
- **World Simulation (`World/`)**: `WorldManager` and `FieldInstance` managing field players, monsters, and spatial broadcasts.
- **Packet Handlers (`Handlers/`)**:
  - `Auth/AuthHandlers.cs`: Login handshake, world list, character creator, character selection.
  - `Field/MovementAndFieldHandlers.cs`: Campus movement, spatial broadcasts, field transitions, warp gates.
  - `Field/CombatHandlers.cs`: Auto-attacks, weapon skills, monster aggro/death, EXP and drops.
  - `Field/EquipmentHandlers.cs`: Equipping/unequipping, visual paperdoll sync, inventory manipulations.
  - `Field/NpcAndDialogueHandlers.cs`: Dialogue sequences, quests, terminal kiosks.
  - `Field/ShopHandlers.cs`: Merchant shops, vending machines, player trades.
  - `EpisodeServerHandler.cs`: Matchmaking rooms, waiting lobbies, episode objectives, clear evaluation.
  - `CommServerHandler.cs`: Friend list operations, instant whispers, presence pings.

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or higher.
- Windows OS (or Linux/macOS with .NET 8 runtime).

---

### 1. Creating an Account

Before launching the client, create an account using the interactive account creation script:

```bat
createAccount.bat
```

1. Enter your desired **Username (Account ID)**.
2. Enter your desired **Password**.
3. The script hashes the password with MD5 and initializes a new player record in `data/save/{username}.json` with `HasCharacter: false`.
4. Upon your first login, the client will immediately enter the **3D Character Creator**!

---

### 2. Starting the Server

To launch all 4 server services in standalone direct mode:

```bat
startServer.bat
```

Or via the .NET CLI:

```bash
dotnet run --project src/Yogurting.Server/Yogurting.Server.csproj
```

The server will initialize the database tables from `data/db/`, bind to all designated TCP ports (`10000`, `10002`, `10003`, `10004`), and open the administrative command prompt.

---

### 3. Interactive CLI Commands

While the server is running, you can manage the game world directly through the console:

| Command | Description |
| :--- | :--- |
| `help` | Displays a list of all available console commands. |
| `status` | Shows server health, active connections per port, and database item/mob counts. |
| `users` | Lists all currently connected students, their character IDs, schools, and current zones. |
| `fields` | Displays active campus field instances and player populations. |
| `broadcast <message>` | Broadcasts a global system announcement banner to all connected players. |
| `reload` | Dynamically reloads parameter tables from `data/db/` without restarting the server. |
| `clear` | Clears the console screen. |
| `stop` / `exit` / `quit` | Flushes all character states to disk and safely terminates all server listeners. |

---

### 4. Operational Execution Modes

The modern server supports auxiliary network configurations for development, packet analysis, and differential testing:

- **Standard Standalone Mode** (Default):
  ```bash
  dotnet run --project src/Yogurting.Server
  # Listens on ports: 10000, 10002, 10003, 10004
  ```

- **Sniffer / Proxy Mode** (`--sniffer` or `--proxy`):
  Runs the modern server behind a packet sniffer or reverse proxy bridge.
  ```bash
  dotnet run --project src/Yogurting.Server -- --sniffer
  # Listens on internal ports: 20000, 20002, 20003, 20004
  ```

- **Shadow / Differential Mode** (`--shadow` or `--diff`):
  Runs concurrently alongside the legacy Quartet server for live packet-by-packet comparison.
  ```bash
  dotnet run --project src/Yogurting.Server -- --shadow
  # Listens on shadow ports: 30000, 30002, 30003, 30004
  ```

---

## ⚙️ Configuration Reference

### `config/server.json`
Main configuration controlling network bindings, file paths, and gameplay rate multipliers:

```json
{
  "Server": {
    "Name": "Yogurting Online English Revival",
    "Motd": "Welcome to Yogurting Modern English Server! Enjoy your school life!",
    "Language": "en",
    "MaxPlayers": 500
  },
  "Network": {
    "BindAddress": "0.0.0.0",
    "LoginPort": 10000,
    "FieldPort": 10002,
    "EpisodePort": 10003,
    "CommPort": 10004,
    "AdminPort": 10010
  },
  "Paths": {
    "DbDirectory": "data/db",
    "ScoreDirectory": "data/score",
    "SaveDirectory": "data/save"
  },
  "Gameplay": {
    "DefaultExpRate": 1.0,
    "DefaultDropRate": 1.0,
    "DefaultMoneyRate": 1.0,
    "AutoSaveIntervalSeconds": 60
  }
}
```

### `config/starter_items.json`
Specifies the initial starter items, clothing, and weapons granted to new characters based on school (`Estiva` / `Soyul`) and gender (`Male` / `Female`).

---

## 📊 Data-Driven Game Database

All game mechanics, items, spawn coordinates, and dialog lines are dynamically parsed from `data/db/` at startup.

Key database tables include:
- **`BeItemType.txt` / `ByulItemType.txt` / `CoItemType.txt`**: Complete item parameters, equipment stat requirements, and consumable effects.
- **`BeItemSlot.txt`**: Mapping of visual equipment slots (Weapon, Head, Face, Top, Bottom, Shoes, Backpack, Accessories).
- **`Field.txt`**: Field metadata, campus names, music tracks, background ambient IDs, and monster spawn generators.
- **`MonsterBasis.txt` / `MON.txt` / `HuntMon.txt`**: Monster templates, stats, aggression types, AI behaviors, and drop tables.
- **`DexTable.txt` / `StatusTable.txt` / `ExpTable.txt`**: Level progression curves, stat caps, and weapon proficiency requirements.
- **`SkillDesc.txt` / `SkillWeapon.txt`**: Weapon skill definitions, cooldowns, and damage multipliers.
- **`Episode.txt` / `EpisodeDetail.txt` / `EpisodeMonster.txt`**: Episode stages, clear conditions, time limits, and enemy waves.
- **`map.db`**: Binary navigation and spatial grid definition.

---

## 📖 Protocol Documentation

For in-depth network packet specifications, raw DMS scripts, memory structures, and reverse-engineering findings, refer to the technical encyclopedia in the `dictionary/` folder:

👉 **[The Yogurting Online Revival Encyclopedia](dictionary/README.md)**

- **Volume 01**: Core Protocol & Framing
- **Volume 02**: Authentication & Login State Machine
- **Volume 03**: Campus World & Spatial Sync
- **Volume 04**: Inventory & Item Slot Architecture
- **Volume 05**: Combat Formulas & Monster AI
- **Volume 06**: Economy, Shops & Player Trade
- **Volume 07**: Episodes, Lobby & Matchmaking
- **Volume 08**: Social, Messenger & Clubs
- **Volume 09**: Master Opcode Catalog (270 Opcodes)
- **Raw DMS Scripts**: `dictionary/dms/*.dms`

# 💬 Multi-Client TCP Network Chat Hub

A C# WinForms desktop application featuring a multi-client TCP socket server, concurrent thread handling, custom GDI+ drawing pipelines, dynamic state synchronization, and a modern Discord-inspired dark theme.

---

## 🚀 Quick Start & How to Run

### Prerequisites
* **Windows OS**
* **Visual Studio 2022** (with the *.NET desktop development* workload installed)
* **.NET 6.0 / 7.0 / 8.0 SDK**

### Running in Visual Studio (Multi-Instance Debugging)
1. Clone or download this repository.
2. Open `MultiClientChat.sln` in Visual Studio.
3. Right-click the **Solution 'MultiClientChat'** in Solution Explorer $\rightarrow$ Select **Set Startup Projects...**
4. Choose **Multiple startup projects**:
   * Set `ChatServer` $\rightarrow$ **Start**
   * Set `ChatClient` $\rightarrow$ **Start**
5. Press **F5** (or click **Start**).
6. To test multiple clients simultaneously, right-click `ChatClient` in Solution Explorer $\rightarrow$ **Debug** $\rightarrow$ **Start New Instance** to launch secondary clients (e.g., Alice, Bob, Yawe).

---

## 1. Project Overview & Objectives

The **Multi-Client TCP Network Chat Hub** replaces standard command-line messaging interfaces with a responsive, modern desktop GUI. The project addresses key networking and UI challenges including asynchronous network I/O, multi-threaded GUI synchronization, real-time client roster management, and custom control rendering without relying on third-party libraries.

---

## 2. System Architecture & Topology

The application uses a two-tier Client-Server architecture separated into distinct Visual Studio projects:

```text
                               ┌──────────────────────────────────┐
                               │     ChatServer (Console App)     │
                               │  - TcpListener (Port 8765)      │
                               │  - Client Dictionary & Locks     │
                               └────────────────┬─────────────────┘
                                                │
                     ┌──────────────────────────┼──────────────────────────┐
                     │ TcpStream                │ TcpStream                │ TcpStream
                     ▼                          ▼                          ▼
      ┌─────────────────────────────┐ ┌───────────────────┐ ┌───────────────────┐
      │  ChatClient Instance 1      │ │ ChatClient Inst. 2│ │ ChatClient Inst. 3│
      │  (Alice - WinForms GUI)     │ │ (Bob - WinForms)  │ │ (Yawe - WinForms) │
      └─────────────────────────────┘ └───────────────────┘ └───────────────────┘

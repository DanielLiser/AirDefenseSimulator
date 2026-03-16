# 🛡️  Air Defense Simulator

A high-performance, real-time air defense simulation system. This project features a **C# backend** acting as the Physics & Battle Management Engine and a **Python frontend** serving as a Command & Control (C2) Dashboard. The two systems communicate over a low-latency **TCP Socket** using JSON-structured data.



## 🚀 System Architecture

### 1. Backend: Simulation Engine (C# / .NET)
The core logic is built with **multithreaded C#**, managing complex real-time operations:
* **Physics Engine:** Calculates ballistic trajectories and impact points at 10Hz.
* **Radar System:** Scans for threats and populates a thread-safe `ConcurrentDictionary`.
* **Battle Management Center (BMC):** Intelligently routes threats to appropriate defense batteries (Arrow vs. Iron Dome) based on altitude.
* **Interceptor Batteries:** Managed via **Semaphores** to handle ammunition inventory and reloading logistics.
* **Async Networking:** Uses `Task.Run` and `await Task.Delay` for non-blocking TCP broadcasting of system events.

### 2. Frontend: C2 Dashboard (Python / Tkinter)
A reactive dashboard designed for real-time situational awareness:
* **TCP Listener:** A dedicated **Daemon Thread** that asynchronously parses incoming telemetry.
* **Thread Safety:** Implements **Mutual Exclusion (Locks)** to ensure data integrity between the network listener and the GUI refresh cycle.
* **State Machine:** Dynamically manages threat statuses (Active, Locked, Destroyed, Impact) with visual color-coding.
* **Real-time Stats:** Calculates live Interception Success Rates and total threat counts.

[Image of a modern Command and Control (C2) dashboard interface with real-time data tables and status indicators]

## 🛠️ Tech Stack

* **Languages:** C#, Python 3.13
* **Communication:** TCP/IP Sockets (Raw JSON Payloads)
* **Concurrency:** * **C#:** Tasks (Async/Await), Semaphores, Concurrent Collections.
    * **Python:** Threading, Locks, Daemon Processes.
* **GUI:** Tkinter (Custom Styled ttk)

## 📊 Key Features

* **Logistics Simulation:** Automated battery refilling tasks every 15 seconds with safety checks to prevent buffer overflows.
* **Fault Tolerance:** The Python client handles out-of-order packets and race conditions (e.g., a "Fire" event arriving before a "Detect" event).
* **Dynamic Visuals:**
*   * 🟡 **Yellow:** Interceptor Locked.
    * 🟢 **Green:** Successful Interception (removes after 2s).
    * 🔴 **Red:** Ground Impact (removes after 3s).
    * 🔵 **Blue:** New Threat Detected.

## ⚙️ Setup & Installation

1.  **Backend:**
    * Navigate to the C# project folder.
    * Build and run using Visual Studio or `dotnet run`.
    * The server will start listening on `127.0.0.1:5000`.

2.  **Frontend:**
    * Install Python 3.x.
    * Run the dashboard: `python main.py`.

## 📜 Educational Concepts Implemented
* **Thread Starvation Prevention:** Optimized loops and proper synchronization to prevent CPU 100% spikes.
* **Resource Synchronization:** Using Locks and Semaphores to manage shared state.
* **Event-Driven Programming:** 

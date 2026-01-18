# Testcontainers AutoSetup Benchmarks

This repository benchmarks the performance of database restoration strategies using **Testcontainers for .NET**. It specifically measures the time required to reset a database to a known clean state (snapshot restoration) across different database engines and dataset sizes.

The goal is to validate the efficiency of `Testcontainers.AutoSetup` for integration testing scenarios where rapid database isolation is critical.

---

## 🚀 Methodology

To ensure realistic and high-performance benchmarking, we utilize the **Persistent Container Strategy**.

### The Problem: Cold Starts
Standard Testcontainers usage often involves `DisposeAsync()` after every test. This introduces significant overhead:
1.  **Container Startup:** Pulling/Creating/Starting the Docker container.
2.  **Engine Warmup:** Database engines require time to initialize authentication and disk subsystems.

### The Solution: Reusable Containers + Intelligent Reset
Instead of destroying the container, we keep it running between iterations using `WithReuse(true)` and the Ryuk sidecar pattern. The benchmark loop follows this flow:

1.  **Global Setup (Once per Parameter):**
    * Starts the Docker container (or reuses an existing one).
    * Seeds the database with a "Golden Dataset" (1 to 50,000 rows).
    * Creates a native backup/snapshot inside the container.
2.  **Iteration Setup:**
    * "Dirties" the database by inserting junk data to simulate a modified test state.
3.  **Benchmark Action (Measured):**
    * Executes the **Restore** command.
    * This drops the dirty data and restores the "Golden Snapshot" using native tools (`mongorestore`, `mysql`, `RESTORE DATABASE`).
4.  **Global Cleanup:**
    * Leaves the container running to eliminate boot time for the next run.

---

## 📊 Benchmark Results

**Environment:**
* **OS:** Windows 11 (AMD Ryzen 5 7430U, 12 Logical Cores)
* **Runtime:** .NET 10.0.2 (RyuJIT x64)
* **Docker:** WSL2 Backend (Ubuntu 24.04 LTS)

### 1. MongoDB (mongorestore)
*Mechanism: Archives gzip dump restoration*

| Seed Rows | Mean Time | StdDev |
|----------:|----------:|-------:|
| **1** | **130.1 ms** | 8.09 ms |
| **10** | **127.8 ms** | 8.62 ms | 
| **100** | **125.4 ms** | 8.05 ms |
| **1,000** | **146.5 ms** | 14.59 ms |
| **10,000** | **365.3 ms** | 75.94 ms | 
| **50,000** | **1,327.9 ms** | 344.99 ms | |

### 2. MySQL (Source SQL Dump)
*Mechanism: Golden State DB restoration*

| Seed Rows | Mean Time | StdDev | 
|----------:|----------:|-------:|
| **1** | **6.5 ms** | 0.58 ms | 
| **10** | **6.4 ms** | 1.08 ms |
| **100** | **7.8 ms** | 0.75 ms |
| **1,000** | **26.6 ms** | 1.71 ms |
| **10,000** | **122.2 ms** | 19.25 ms |
| **50,000** | **528.3 ms** | 31.19 ms |

### 3. MSSQL (Backup/Restore)
*Mechanism: `.bak` file restoration using `RESTORE DATABASE WITH REPLACE`*

| Seed Rows | Mean Time | StdDev |
|----------:|----------:|-------:|
| **1** | **406.6 ms** | 520.8 ms |
| **10** | **295.3 ms** | 23.45 ms |
| **100** | **309.1 ms** | 31.13 ms | 
| **1,000** | **317.9 ms** | 27.47 ms |
| **10,000** | **285.0 ms** | 23.18 ms |
| **50,000** | **346.1 ms** | 180.11 ms |

---

## 💡 Key Findings & Analysis

1.  **MySQL is the Speed King for Tests:**
    For typical integration test sizes (1-1000 rows), MySQL is nearly instant (~6ms). The overhead of the protocol is negligible compared to Mongo and MSSQL.

2.  **MSSQL Scales Best:**
    MSSQL shows **O(1)** (constant time) performance characteristics. Because it restores a binary `.bak` file physically on disk, restoring 50,000 rows takes roughly the same time (~300ms) as restoring 1 row. It is the best choice for tests involving massive seed datasets.

3.  **MongoDB Overhead:**
    MongoDB has a high "base cost" (~130ms) even for 1 row. This is likely due to the `mongorestore` tool's connection handshake, authentication (SCRAM-SHA-1), and the overhead of processing the GZIP stream.

---

## 🛠️ How to Run

1.  Ensure Docker Desktop is running (WSL2 mode recommended for Windows).
2.  Run benchmarks in Release mode to ensure compiler optimizations:

```bash
dotnet run -c Release --project Testcontainers.AutoSetup.Benchmarks
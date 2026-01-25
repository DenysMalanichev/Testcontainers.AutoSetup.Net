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
### Tpmfs vs non-Tmpfs
#### MS SQL
WSL2:
| SeedRowCount | UseTmpfs | Mean     | Error     | StdDev    | Allocated |
|------------- |--------- |---------:|----------:|----------:|----------:|
| 1            | False    | 414.0 ms |  21.04 ms |  31.49 ms |  62.77 KB |
| 1            | True     | 411.7 ms |  20.17 ms |  30.18 ms |  62.77 KB |
| 10           | False    | 413.4 ms |  21.50 ms |  32.18 ms |  62.77 KB |
| 10           | True     | 432.3 ms |  19.06 ms |  28.53 ms |  62.77 KB |
| 100          | False    | 409.4 ms |  20.04 ms |  30.00 ms |  62.77 KB |
| 100          | True     | 421.9 ms |  20.47 ms |  30.63 ms |  62.51 KB |
| 1000         | False    | 402.4 ms |  21.26 ms |  31.82 ms |  62.51 KB |
| 1000         | True     | 452.3 ms | 117.00 ms | 175.12 ms |  59.02 KB |
| 10000        | False    | 404.6 ms |  16.05 ms |  24.03 ms |  62.77 KB |
| 10000        | True     | 414.7 ms |  18.05 ms |  27.01 ms |  62.51 KB |
| 50000        | False    | 411.5 ms |  20.21 ms |  30.25 ms |  62.77 KB |
| 50000        | True     | 405.3 ms |  20.44 ms |  30.59 ms |  62.51 KB |

Docker Desktop:
| SeedRowCount | UseTmpfs | Mean     | Error     | StdDev    | Allocated |
|------------- |--------- |---------:|----------:|----------:|----------:|
| 1            | False    | 459.0 ms |  22.21 ms |  33.24 ms |  58.75 KB |
| 1            | True     | 363.7 ms | 130.89 ms | 195.91 ms |  58.75 KB |
| 10           | False    | 455.4 ms |  20.44 ms |  30.59 ms |  62.77 KB |
| 10           | True     | 319.1 ms |  20.39 ms |  30.52 ms |  58.49 KB |
| 100          | False    | 463.4 ms |  27.08 ms |  40.53 ms |  58.75 KB |
| 100          | True     | 321.2 ms |  19.84 ms |  29.69 ms |  58.49 KB |
| 1000         | False    | 477.6 ms |  25.78 ms |  38.59 ms |  58.75 KB |
| 1000         | True     | 357.2 ms | 126.39 ms | 189.17 ms |  58.49 KB |
| 10000        | False    | 463.9 ms |  23.37 ms |  34.98 ms |  58.75 KB |
| 10000        | True     | 329.4 ms |  24.81 ms |  37.13 ms |  58.49 KB |
| 50000        | False    | 465.7 ms |  18.82 ms |  28.18 ms |  58.75 KB |
| 50000        | True     | 325.8 ms |  23.34 ms |  34.93 ms |  58.49 KB |

#### MySQL
WSL2:
| SeedRowCount | UseTmpfs | Mean        | Error     | StdDev    | Allocated |
|------------- |--------- |------------:|----------:|----------:|----------:|
| 1            | False    |    29.21 ms |  1.709 ms |  2.558 ms |  92.45 KB |
| 1            | True     |    26.04 ms |  0.933 ms |  1.397 ms |  91.44 KB |
| 10           | False    |    28.27 ms |  2.077 ms |  3.108 ms |  91.73 KB |
| 10           | True     |    28.72 ms |  2.773 ms |  4.151 ms |  91.73 KB |
| 100          | False    |    58.44 ms |  9.170 ms | 13.725 ms |  91.73 KB |
| 100          | True     |    47.90 ms |  1.298 ms |  1.942 ms |  91.44 KB |
| 1000         | False    |   173.49 ms |  3.150 ms |  4.715 ms |  91.41 KB |
| 1000         | True     |   178.29 ms |  6.454 ms |  9.660 ms |  91.41 KB |
| 10000        | False    |   355.46 ms | 20.211 ms | 30.251 ms |  91.42 KB |
| 10000        | True     |   350.17 ms | 16.989 ms | 25.428 ms |  91.42 KB |
| 50000        | False    | 1,050.27 ms | 59.966 ms | 89.755 ms |  91.42 KB |
| 50000        | True     | 1,076.33 ms | 60.674 ms | 90.815 ms |  91.42 KB |

Docker Desktop:
| SeedRowCount | UseTmpfs | Mean       | Error      | StdDev     | Allocated |
|------------- |--------- |-----------:|-----------:|-----------:|----------:|
| 1            | False    |  23.109 ms |  0.6303 ms |  0.9435 ms |  91.68 KB |
| 1            | True     |   7.053 ms |  0.4383 ms |  0.6560 ms |  91.68 KB |
| 10           | False    |  23.429 ms |  0.6297 ms |  0.9425 ms |  91.68 KB |
| 10           | True     |   6.866 ms |  0.5083 ms |  0.7608 ms |   92.4 KB |
| 100          | False    |  44.477 ms |  1.9371 ms |  2.8994 ms |  91.68 KB |
| 100          | True     |   8.130 ms |  0.4806 ms |  0.7193 ms |  91.39 KB |
| 1000         | False    | 150.839 ms |  2.4320 ms |  3.6401 ms |  91.08 KB |
| 1000         | True     |  21.045 ms |  0.5574 ms |  0.8343 ms |  91.68 KB |
| 10000        | False    | 286.986 ms | 32.7745 ms | 49.0553 ms |  91.09 KB |
| 10000        | True     | 101.781 ms | 12.8498 ms | 19.2330 ms |  91.09 KB |
| 50000        | False    | 734.503 ms | 24.1943 ms | 36.2129 ms |  91.38 KB |
| 50000        | True     | 467.368 ms | 30.4232 ms | 45.5361 ms |  91.09 KB |

#### MongoDB
WSL2:
| SeedRowCount | UseTmpfs | Mean     | Error    | StdDev   | Allocated |
|------------- |--------- |---------:|---------:|---------:|----------:|
| 1            | False    | 144.7 ms |  4.80 ms |  7.19 ms |  65.95 KB |
| 1            | True     | 132.6 ms |  6.75 ms | 10.10 ms |  65.95 KB |
| 10           | False    | 144.1 ms |  5.39 ms |  8.07 ms |  65.95 KB |
| 10           | True     | 128.9 ms |  5.64 ms |  8.44 ms |  65.95 KB |
| 100          | False    | 143.0 ms |  6.45 ms |  9.66 ms |  66.27 KB |
| 100          | True     | 134.5 ms |  5.58 ms |  8.35 ms |  65.97 KB |
| 1000         | False    | 150.4 ms |  6.83 ms | 10.23 ms |  65.97 KB |
| 1000         | True     | 142.2 ms |  7.08 ms | 10.59 ms |  65.97 KB |
| 10000        | False    | 299.0 ms | 10.66 ms | 15.96 ms |  379.6 KB |
| 10000        | True     | 281.0 ms | 21.36 ms | 31.97 ms |  65.98 KB |
| 50000        | False    | 835.9 ms | 38.24 ms | 57.23 ms |  65.89 KB |
| 50000        | True     | 660.7 ms | 17.86 ms | 26.73 ms | 235.03 KB |

Docker Dektop:
| SeedRowCount | UseTmpfs | Mean     | Error    | StdDev   | Allocated |
|------------- |--------- |---------:|---------:|---------:|----------:|
| 1            | False    | 127.3 ms |  5.73 ms |  8.58 ms |   68.6 KB |
| 1            | True     | 110.9 ms |  4.18 ms |  6.26 ms |  69.09 KB |
| 10           | False    | 126.9 ms |  6.15 ms |  7.08 ms |        NA |
| 10           | True     | 117.4 ms |  7.57 ms | 11.33 ms |   69.1 KB |
| 100          | False    | 126.3 ms |  3.72 ms |  5.57 ms |  68.95 KB |
| 100          | True     | 108.8 ms |  3.62 ms |  5.42 ms |  69.08 KB |
| 1000         | False    | 130.5 ms |  3.40 ms |  5.09 ms |  69.29 KB |
| 1000         | True     | 112.0 ms |  4.58 ms |  6.86 ms |   68.8 KB |
| 10000        | False    | 250.0 ms |  5.41 ms |  8.10 ms |  69.21 KB |
| 10000        | True     | 206.1 ms |  6.99 ms | 10.46 ms |  69.21 KB |
| 50000        | False    | 756.0 ms | 15.28 ms | 22.86 ms |  69.04 KB |
| 50000        | True     | 582.7 ms |  8.91 ms | 13.33 ms |  69.17 KB |

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
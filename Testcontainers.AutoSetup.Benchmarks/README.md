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
| SeedRowCount | UseTmpfs | Mean        | Error     | StdDev     | Allocated |
|------------- |--------- |------------:|----------:|-----------:|----------:|
| 1            | False    |    38.73 ms |  1.264 ms |   1.891 ms | 115.25 KB |
| 1            | True     |    39.80 ms |  1.473 ms |   2.204 ms | 114.96 KB |
| 10           | False    |    38.93 ms |  0.910 ms |   1.362 ms |  115.7 KB |
| 10           | True     |    40.42 ms |  1.656 ms |   2.479 ms | 115.27 KB |
| 100          | False    |    66.47 ms |  2.617 ms |   3.917 ms |    116 KB |
| 100          | True     |    66.13 ms |  2.601 ms |   3.893 ms | 114.99 KB |
| 1000         | False    |   209.15 ms |  6.046 ms |   9.049 ms | 115.44 KB |
| 1000         | True     |   208.43 ms |  5.011 ms |   7.501 ms | 148.38 KB |
| 10000        | False    |   414.51 ms | 17.256 ms |  25.828 ms | 182.23 KB |
| 10000        | True     |   397.50 ms | 10.501 ms |  15.718 ms | 149.41 KB |
| 50000        | False    | 1,145.61 ms | 94.874 ms | 142.003 ms | 148.69 KB |
| 50000        | True     |   996.85 ms | 41.130 ms |  61.562 ms | 149.12 KB |

Docker Desktop:
| SeedRowCount | UseTmpfs | Mean      | Error     | StdDev    | Allocated |
|------------- |--------- |----------:|----------:|----------:|----------:|
| 1            | False    |  45.53 ms |  5.640 ms |  8.441 ms |  149.3 KB |
| 1            | True     |  19.29 ms |  1.146 ms |  1.715 ms | 148.87 KB |
| 10           | False    |  44.62 ms |  3.413 ms |  5.109 ms | 148.88 KB |
| 10           | True     |  21.53 ms |  1.274 ms |  1.906 ms | 148.88 KB |
| 100          | False    |  69.32 ms |  2.796 ms |  4.185 ms |  148.9 KB |
| 100          | True     |  21.47 ms |  1.690 ms |  2.529 ms | 150.34 KB |
| 1000         | False    | 196.03 ms |  5.020 ms |  7.514 ms | 148.63 KB |
| 1000         | True     |  37.24 ms |  2.598 ms |  3.889 ms | 148.94 KB |
| 10000        | False    | 353.55 ms | 13.133 ms | 19.657 ms | 149.36 KB |
| 10000        | True     | 118.76 ms |  9.554 ms | 14.300 ms | 149.36 KB |
| 50000        | False    | 826.95 ms | 18.916 ms | 28.313 ms | 149.36 KB |
| 50000        | True     | 468.33 ms | 14.195 ms | 21.247 ms | 114.26 KB |

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

Docker Desktop:
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

#### Kafka (topics creation)
WSL2:
| SeedTopicsCount | UseTmpfs | Mean       | Error       | StdDev      | Median     | Allocated |
|---------------- |--------- |-----------:|------------:|------------:|-----------:|----------:|
| 1               | False    |   323.9 ms |    13.37 ms |    20.01 ms |   322.4 ms |  22.09 KB |
| 1               | True     |   325.1 ms |     4.40 ms |     6.59 ms |   322.2 ms |  22.09 KB |
| 10              | False    |   321.7 ms |     1.92 ms |     2.88 ms |   321.4 ms |  43.25 KB |
| 10              | True     |   318.4 ms |    13.21 ms |    19.77 ms |   321.7 ms |  43.25 KB |
| 100             | False    | 1,073.5 ms |    73.79 ms |   110.44 ms | 1,084.5 ms | 494.49 KB |
| 100             | True     |   691.3 ms |   139.42 ms |   208.68 ms |   757.1 ms | 446.55 KB |
| 1000            | False    |   786.7 ms | 1,162.49 ms | 1,739.95 ms |   212.6 ms |  747.8 KB |
| 1000            | True     |   747.9 ms | 1,080.10 ms | 1,616.65 ms |   213.4 ms |  747.8 KB |

Docker Desktop:
| TopicsCount | UseTmpfs | Mean       | Error       | StdDev      | Median     | Allocated  |
|------------ |--------- |-----------:|------------:|------------:|-----------:|-----------:|
| 1           | False    |   331.8 ms |     3.82 ms |     5.72 ms |   333.9 ms |   22.03 KB |
| 1           | True     |   328.3 ms |     4.63 ms |     6.93 ms |   330.8 ms |   22.03 KB |
| 10          | False    |   327.2 ms |    13.65 ms |    20.43 ms |   333.2 ms |   43.13 KB |
| 10          | True     |   328.9 ms |     5.18 ms |     7.76 ms |   333.3 ms |   43.13 KB |
| 100         | False    | 1,056.3 ms |   129.97 ms |   194.53 ms | 1,097.9 ms |  493.06 KB |
| 100         | True     |   328.8 ms |     4.95 ms |     7.41 ms |   333.3 ms |  253.46 KB |
| 1000        | False    |   901.9 ms | 1,399.60 ms | 2,094.86 ms |   223.5 ms |  747.77 KB |
| 1000        | True     |   711.2 ms |    70.34 ms |   105.28 ms |   662.9 ms | 3921.78 KB |

#### Kafka (message seeding)
WSL2:
| MessagesCount | UseTmpfs | Mean        | Error       | StdDev      | Median      | Allocated |
|-------------- |--------- |------------:|------------:|------------:|------------:|----------:|
| 1             | False    |    323.0 ms |    13.04 ms |    19.52 ms |    322.7 ms |  31.23 KB |
| 1             | True     |    323.8 ms |     3.84 ms |     5.75 ms |    321.4 ms |  31.23 KB |
| 10            | False    |    586.8 ms |    36.26 ms |    54.27 ms |    553.4 ms |  38.42 KB |
| 10            | True     |    595.7 ms |    47.03 ms |    70.39 ms |    593.5 ms |  38.38 KB |
| 100           | False    |  3,152.0 ms |   243.46 ms |   364.40 ms |  3,271.4 ms | 110.59 KB |
| 100           | True     |  3,228.7 ms |   191.32 ms |   286.36 ms |  3,380.0 ms | 110.61 KB |
| 1000          | False    | 29,800.5 ms | 1,931.20 ms | 2,890.54 ms | 31,076.3 ms | 833.73 KB |
| 1000          | True     | 30,065.8 ms | 1,800.56 ms | 2,694.99 ms | 31,165.5 ms |  833.8 KB |

Docker Desktop:
| SeedMessagesCount | UseTmpfs | Mean        | Error     | StdDev    | Allocated |      
|------------------ |--------- |------------:|----------:|----------:|----------:|      
| 1                 | False    |    328.4 ms |  13.14 ms |  19.66 ms |  31.17 KB |      
| 1                 | True     |    332.7 ms |   1.89 ms |   2.84 ms |  31.17 KB |      
| 10                | False    |    651.4 ms |  13.64 ms |  20.42 ms |  38.37 KB |      
| 10                | True     |    638.8 ms |  33.34 ms |  49.91 ms |  38.37 KB |
| 100               | False    |  3,723.9 ms |  65.44 ms |  97.94 ms | 110.74 KB |      
| 100               | True     |  3,704.0 ms |  48.79 ms |  73.02 ms |  110.7 KB |      
| 1000              | False    | 35,315.6 ms | 494.09 ms | 739.53 ms | 836.07 KB |      
| 1000              | True     | 35,753.7 ms | 209.42 ms | 313.45 ms | 835.99 KB |

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
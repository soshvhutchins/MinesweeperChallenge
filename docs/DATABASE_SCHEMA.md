# Database Schema Documentation

## Overview

This document describes the complete database schema for the Minesweeper Game application. The schema is designed using **Entity Framework Core** with support for multiple database providers (SQLite for development, PostgreSQL for production) and follows **Domain-Driven Design** principles with rich domain models.

## Database Providers

- **Development**: SQLite (file-based, easy setup)
- **Production**: PostgreSQL (scalable, enterprise-grade)
- **Testing**: In-Memory Provider (fast unit tests)

## Schema Version

- **Current Version**: 1.0
- **Last Updated**: August 5, 2025
- **EF Core Version**: 9.0+

---

## Schema Architecture Overview

```mermaid
---
title: Minesweeper Game Database Architecture
config:
  erDiagram:
    direction: TB
---
erDiagram
    %% Core Game Entities
    Players ||--o{ Games : "plays"
    Games ||--|| GameBoards : "contains"
    
    %% Future Enhancement Entities (Planned)
    Players ||--o{ PlayerStatistics : "tracks"
    Games ||--o{ GameAuditLog : "audits"
    Players ||--o{ PlayerAchievements : "earns"
    Games ||--o{ GameReplay : "records"

    Players {
        Guid Id PK "Primary Key"
        string Username UK "Unique Username"
        string Email UK "Unique Email"
        string DisplayName "Display Name"
        int TotalGames "Games Played"
        int GamesWon "Games Won"
        int GamesLost "Games Lost"
        DateTime CreatedAt "Account Created"
        DateTime LastActiveAt "Last Activity"
    }

    Games {
        Guid Id PK "Primary Key"
        Guid PlayerId FK "Player Reference"
        string DifficultyName "Difficulty Level"
        int DifficultyWidth "Board Width"
        int DifficultyHeight "Board Height"
        int DifficultyMineCount "Mine Count"
        string Status "Game Status"
        DateTime StartTime "Game Started"
        DateTime EndTime "Game Ended"
        int MoveCount "Moves Made"
        int FlagCount "Flags Placed"
        bool IsPaused "Is Paused"
    }

    GameBoards {
        Guid GameId PK "Game Reference FK"
        int Width "Board Width"
        int Height "Board Height"
        int MineCount "Mine Count"
        bool MinesGenerated "Mines Placed"
        string CellsData "JSON Cell States"
        string MinePositions "JSON Mine Positions"
        int RevealedCount "Revealed Cells"
        int FlaggedCount "Flagged Cells"
    }

    %% Future Tables (Planned)
    PlayerStatistics {
        Guid Id PK "Primary Key"
        Guid PlayerId FK "Player Reference"
        string StatisticType "Statistic Type"
        decimal Value "Statistic Value"
        DateTime UpdatedAt "Last Updated"
    }

    GameAuditLog {
        Guid Id PK "Primary Key"
        Guid GameId FK "Game Reference"
        Guid PlayerId FK "Player Reference"
        string Action "Action Type"
        string Position "Cell Position JSON"
        DateTime Timestamp "Action Time"
        bigint Duration "Time Since Last Action"
    }

    PlayerAchievements {
        Guid Id PK "Primary Key"
        Guid PlayerId FK "Player Reference"
        string AchievementType "Achievement Type"
        DateTime EarnedAt "Earned Date"
        string Metadata "Achievement Data JSON"
    }

    GameReplay {
        Guid Id PK "Primary Key"
        Guid GameId FK "Game Reference"
        string ReplayData "Compressed Replay JSON"
        DateTime CreatedAt "Replay Created"
    }
```

---

## Tables

### Games

**Purpose**: Core game entity representing individual Minesweeper game sessions

| Column                | Type                      | Constraints                    | Description                                                    |
| --------------------- | ------------------------- | ------------------------------ | -------------------------------------------------------------- |
| `Id`                  | `uniqueidentifier` (GUID) | **PRIMARY KEY**, NOT NULL      | Unique game identifier                                         |
| `PlayerId`            | `uniqueidentifier` (GUID) | **FOREIGN KEY**, NOT NULL      | Reference to Players table                                     |
| `DifficultyName`      | `nvarchar(50)`            | NOT NULL                       | Difficulty level name (Beginner, Intermediate, Expert, Custom) |
| `DifficultyWidth`     | `int`                     | NOT NULL                       | Board width in cells                                           |
| `DifficultyHeight`    | `int`                     | NOT NULL                       | Board height in cells                                          |
| `DifficultyMineCount` | `int`                     | NOT NULL                       | Total number of mines                                          |
| `Status`              | `nvarchar(20)`            | NOT NULL                       | Game status (NotStarted, InProgress, Won, Lost, Paused)        |
| `StartTime`           | `datetime2`               | NOT NULL                       | Game creation timestamp                                        |
| `EndTime`             | `datetime2`               | NULL                           | Game completion timestamp                                      |
| `FirstClickTime`      | `datetime2`               | NULL                           | First cell reveal timestamp                                    |
| `MoveCount`           | `int`                     | NOT NULL, DEFAULT 0            | Number of moves made                                           |
| `FlagCount`           | `int`                     | NOT NULL, DEFAULT 0            | Number of flags placed                                         |
| `IsPaused`            | `bit`                     | NOT NULL, DEFAULT 0            | Whether game is currently paused                               |
| `PausedDuration`      | `bigint`                  | NOT NULL, DEFAULT 0            | Total paused time in ticks                                     |
| `CreatedAt`           | `datetime2`               | NOT NULL, DEFAULT GETUTCDATE() | Record creation timestamp                                      |
| `UpdatedAt`           | `datetime2`               | NOT NULL, DEFAULT GETUTCDATE() | Record last update timestamp                                   |

**Indexes:**

- `IX_Games_PlayerId` - Non-clustered index on PlayerId for player game queries
- `IX_Games_Status` - Non-clustered index on Status for game state filtering
- `IX_Games_StartTime` - Non-clustered index on StartTime for chronological queries

**Constraints:**

- `CHK_Games_Dimensions` - Width and Height must be between 1 and 100
- `CHK_Games_MineCount` - MineCount must be less than (Width × Height)
- `CHK_Games_MoveCount` - MoveCount must be non-negative
- `CHK_Games_FlagCount` - FlagCount must be non-negative and ≤ MineCount

### Players

**Purpose**: Player entity representing registered users

| Column                 | Type                      | Constraints                    | Description                                      |
| ---------------------- | ------------------------- | ------------------------------ | ------------------------------------------------ |
| `Id`                   | `uniqueidentifier` (GUID) | **PRIMARY KEY**, NOT NULL      | Unique player identifier                         |
| `Username`             | `nvarchar(50)`            | **UNIQUE**, NOT NULL           | Player username                                  |
| `Email`                | `nvarchar(256)`           | **UNIQUE**, NOT NULL           | Player email address                             |
| `DisplayName`          | `nvarchar(100)`           | NOT NULL                       | Player display name                              |
| `TotalGames`           | `int`                     | NOT NULL, DEFAULT 0            | Total games played                               |
| `GamesWon`             | `int`                     | NOT NULL, DEFAULT 0            | Total games won                                  |
| `GamesLost`            | `int`                     | NOT NULL, DEFAULT 0            | Total games lost                                 |
| `TotalPlayTime`        | `bigint`                  | NOT NULL, DEFAULT 0            | Total play time in ticks                         |
| `BestTimeEasy`         | `bigint`                  | NULL                           | Best completion time for easy difficulty         |
| `BestTimeIntermediate` | `bigint`                  | NULL                           | Best completion time for intermediate difficulty |
| `BestTimeExpert`       | `bigint`                  | NULL                           | Best completion time for expert difficulty       |
| `CurrentStreak`        | `int`                     | NOT NULL, DEFAULT 0            | Current winning streak                           |
| `LongestStreak`        | `int`                     | NOT NULL, DEFAULT 0            | Longest winning streak achieved                  |
| `CreatedAt`            | `datetime2`               | NOT NULL, DEFAULT GETUTCDATE() | Account creation timestamp                       |
| `LastActiveAt`         | `datetime2`               | NOT NULL, DEFAULT GETUTCDATE() | Last activity timestamp                          |

**Indexes:**

- `IX_Players_Username` - Unique index on Username for login queries
- `IX_Players_Email` - Unique index on Email for authentication
- `IX_Players_LastActiveAt` - Non-clustered index for activity tracking

**Constraints:**

- `CHK_Players_Username` - Username length between 3 and 50 characters
- `CHK_Players_Email` - Valid email format
- `CHK_Players_Games` - GamesWon + GamesLost ≤ TotalGames
- `CHK_Players_Streaks` - CurrentStreak ≤ LongestStreak

### GameBoards

**Purpose**: Game board state including cell data and mine positions

| Column           | Type                      | Constraints                      | Description                    |
| ---------------- | ------------------------- | -------------------------------- | ------------------------------ |
| `GameId`         | `uniqueidentifier` (GUID) | **PRIMARY KEY**, **FOREIGN KEY** | Reference to Games table       |
| `Width`          | `int`                     | NOT NULL                         | Board width in cells           |
| `Height`         | `int`                     | NOT NULL                         | Board height in cells          |
| `MineCount`      | `int`                     | NOT NULL                         | Total number of mines          |
| `MinesGenerated` | `bit`                     | NOT NULL, DEFAULT 0              | Whether mines have been placed |
| `CellsData`      | `nvarchar(MAX)`           | NOT NULL                         | JSON serialized cell states    |
| `MinePositions`  | `nvarchar(MAX)`           | NULL                             | JSON serialized mine positions |
| `RevealedCount`  | `int`                     | NOT NULL, DEFAULT 0              | Number of revealed cells       |
| `FlaggedCount`   | `int`                     | NOT NULL, DEFAULT 0              | Number of flagged cells        |

**Indexes:**

- `PK_GameBoards` - Primary key on GameId
- `IX_GameBoards_Dimensions` - Composite index on Width, Height for dimension queries

**Constraints:**

- `CHK_GameBoards_Dimensions` - Width and Height must be positive
- `CHK_GameBoards_MineCount` - MineCount must be less than (Width × Height)
- `CHK_GameBoards_Counts` - RevealedCount and FlaggedCount must be non-negative

---

## Value Objects

### GameDifficulty (Embedded in Games table)

Represents different difficulty levels with predefined and custom options.

**Predefined Difficulties:**

- **Beginner**: 9×9 grid, 10 mines
- **Intermediate**: 16×16 grid, 40 mines  
- **Expert**: 16×30 grid, 99 mines
- **Custom**: User-defined dimensions and mine count

### CellPosition (Used in JSON serialization)

Represents a position on the game board with row and column coordinates.

```json
{
  "Row": 0,
  "Column": 0
}
```

### CellState (Used in JSON serialization)

Represents the state of individual cells on the game board.

**States:**

- `Hidden` (0) - Cell not yet revealed
- `Revealed` (1) - Cell has been revealed
- `Flagged` (2) - Cell marked as potential mine
- `Questioned` (3) - Cell marked with question mark

---

## Relationships

### Core Game Entity Relationships

```mermaid
---
title: Core Minesweeper Game Entity Relationships
---
erDiagram
    Players ||--o{ Games : "creates"
    Games ||--|| GameBoards : "contains"

    Players {
        Guid Id PK "Primary Key"
        string Username UK "Unique Username"
        string Email UK "Unique Email"
        string DisplayName "Display Name"
        int TotalGames "Total Games Played"
        int GamesWon "Games Won"
        int GamesLost "Games Lost"
        bigint TotalPlayTime "Total Play Time"
        bigint BestTimeEasy "Best Easy Time"
        bigint BestTimeIntermediate "Best Intermediate Time"
        bigint BestTimeExpert "Best Expert Time"
        int CurrentStreak "Current Win Streak"
        int LongestStreak "Longest Win Streak"
        DateTime CreatedAt "Account Created"
        DateTime LastActiveAt "Last Activity"
    }

    Games {
        Guid Id PK "Primary Key"
        Guid PlayerId FK "Player Reference"
        string DifficultyName "Difficulty Level"
        int DifficultyWidth "Board Width"
        int DifficultyHeight "Board Height"
        int DifficultyMineCount "Mine Count"
        string Status "Game Status"
        DateTime StartTime "Game Started"
        DateTime EndTime "Game Ended"
        DateTime FirstClickTime "First Click"
        int MoveCount "Moves Made"
        int FlagCount "Flags Placed"
        bool IsPaused "Is Paused"
        bigint PausedDuration "Paused Time"
        DateTime CreatedAt "Record Created"
        DateTime UpdatedAt "Record Updated"
    }

    GameBoards {
        Guid GameId PK "Game Reference FK"
        int Width "Board Width"
        int Height "Board Height"
        int MineCount "Mine Count"
        bool MinesGenerated "Mines Placed"
        string CellsData "JSON Cell States"
        string MinePositions "JSON Mine Positions"
        int RevealedCount "Revealed Cells"
        int FlaggedCount "Flagged Cells"
    }
```

### Game Logic Flow Diagram

```mermaid
---
title: Minesweeper Game Logic and State Flow
---
flowchart TD
    A[Player Creates Game] --> B[Game Entity Created]
    B --> C[GameBoard Entity Created]
    C --> D[Game Status: NotStarted]
    
    D --> E[Player Makes First Click]
    E --> F[Generate Mines<br/>Avoiding First Click]
    F --> G[Game Status: InProgress]
    
    G --> H{Cell Action}
    H -->|Reveal| I[Reveal Cell]
    H -->|Flag| J[Toggle Flag]
    H -->|Question| K[Toggle Question Mark]
    
    I --> L{Hit Mine?}
    L -->|Yes| M[Game Status: Lost]
    L -->|No| N{All Safe Cells Revealed?}
    
    N -->|Yes| O[Game Status: Won]
    N -->|No| P[Continue Game]
    
    J --> P
    K --> P
    P --> H
    
    G --> Q{Pause Game?}
    Q -->|Yes| R[Game Status: Paused]
    R --> S{Resume Game?}
    S -->|Yes| G
    
    M --> T[End Game - Update Statistics]
    O --> T
    T --> U[Save Game Results]
```

### Game Difficulty Value Object Schema

```mermaid
---
title: Game Difficulty Configuration Schema
---
classDiagram
    class GameDifficulty {
        +string Name
        +int Width
        +int Height
        +int MineCount
        +static GameDifficulty Beginner
        +static GameDifficulty Intermediate
        +static GameDifficulty Expert
        +static GameDifficulty Custom(width, height, mines)
        +ValidateDimensions() bool
    }
    
    GameDifficulty --> Beginner : "9×9, 10 mines"
    GameDifficulty --> Intermediate : "16×16, 40 mines"
    GameDifficulty --> Expert : "16×30, 99 mines"
    
    note for GameDifficulty "Stored as JSON in Games table\nValidates mine count < total cells"
```

### Cell State Management Schema

```mermaid
---
title: Cell State and Position Management
---
stateDiagram-v2
    [*] --> Hidden : Cell Created
    Hidden --> Revealed : Player Reveals
    Hidden --> Flagged : Player Flags
    Hidden --> Questioned : Player Questions
    
    Flagged --> Hidden : Remove Flag
    Flagged --> Questioned : Cycle State
    
    Questioned --> Hidden : Remove Question
    Questioned --> Flagged : Cycle State
    
    Revealed --> [*] : Final State
    
    note right of Hidden : Default state when game starts
    note right of Flagged : Player suspects mine here
    note right of Questioned : Player unsure about cell
    note right of Revealed : Shows mine count or mine
```

### Database Performance Index Strategy

```mermaid
---
title: Database Index Strategy for Performance
---
graph TB
    subgraph "Player Queries"
        A[IX_Players_Username] --> B[Login Performance]
        C[IX_Players_Email] --> D[Authentication]
        E[IX_Players_LastActiveAt] --> F[Activity Tracking]
    end
    
    subgraph "Game Queries"
        G[IX_Games_PlayerId] --> H[Player's Games]
        I[IX_Games_Status] --> J[Active Games]
        K[IX_Games_StartTime] --> L[Recent Games]
        M[IX_Games_PlayerId_Status] --> N[Player Active Games]
    end
    
    subgraph "Board Queries"
        O[PK_GameBoards] --> P[Direct Board Access]
        Q[IX_GameBoards_Dimensions] --> R[Dimension Filtering]
    end
    
    B --> S[Fast User Experience]
    D --> S
    F --> T[Analytics & Monitoring]
    H --> U[Game History]
    J --> V[System Monitoring]
    L --> U
    N --> W[Active Session Management]
    P --> X[Game State Loading]
    R --> Y[Board Size Analytics]
```

### Games → Players

- **Type**: Many-to-One
- **Foreign Key**: `Games.PlayerId` → `Players.Id`
- **Delete Behavior**: Restrict (cannot delete player with existing games)
- **Description**: Each game belongs to exactly one player; players can have multiple games

### GameBoards → Games

- **Type**: One-to-One
- **Foreign Key**: `GameBoards.GameId` → `Games.Id`
- **Delete Behavior**: Cascade (delete board when game is deleted)
- **Description**: Each game has exactly one board; each board belongs to exactly one game

---

## Future Enhancements Schema

### Authentication & Security Schema (Planned)

```mermaid
---
title: Future Authentication and Security Schema
---
erDiagram
    Users ||--o{ UserRoles : "has"
    Roles ||--o{ UserRoles : "grants"
    Users ||--o{ UserClaims : "possesses"
    Users ||--|| Players : "maps-to"
    Users ||--o{ UserSessions : "creates"
    Users ||--o{ UserSecurityLog : "generates"

    Users {
        Guid Id PK "Primary Key"
        string Username UK "Username"
        string Email UK "Email"
        string PasswordHash "BCrypt Hash"
        string SecurityStamp "Security Token"
        bool EmailConfirmed "Email Verified"
        bool TwoFactorEnabled "2FA Enabled"
        DateTime CreatedAt "Account Created"
        DateTime LastLoginAt "Last Login"
        bool IsLocked "Account Locked"
    }

    Roles {
        Guid Id PK "Primary Key"
        string Name UK "Role Name"
        string Description "Role Description"
        DateTime CreatedAt "Role Created"
    }

    UserRoles {
        Guid UserId PK "User Reference FK"
        Guid RoleId PK "Role Reference FK"
        DateTime GrantedAt "Role Granted"
        DateTime ExpiresAt "Role Expires"
    }

    UserClaims {
        Guid Id PK "Primary Key"
        Guid UserId FK "User Reference"
        string ClaimType "Claim Type"
        string ClaimValue "Claim Value"
        DateTime CreatedAt "Claim Created"
    }

    UserSessions {
        Guid Id PK "Primary Key"
        Guid UserId FK "User Reference"
        string SessionToken "Session Token"
        DateTime CreatedAt "Session Created"
        DateTime ExpiresAt "Session Expires"
        string IpAddress "Client IP"
        string UserAgent "Client Agent"
    }

    UserSecurityLog {
        Guid Id PK "Primary Key"
        Guid UserId FK "User Reference"
        string EventType "Security Event"
        string Details "Event Details JSON"
        string IpAddress "Client IP"
        DateTime OccurredAt "Event Time"
    }
```

### Tournament & Competition Schema (Planned)

```mermaid
---
title: Future Tournament and Competition Schema
---
erDiagram
    Tournaments ||--o{ TournamentParticipants : "includes"
    Players ||--o{ TournamentParticipants : "joins"
    Tournaments ||--o{ TournamentRounds : "contains"
    TournamentRounds ||--o{ TournamentMatches : "includes"
    TournamentMatches ||--o{ Games : "plays"
    Tournaments ||--o{ TournamentLeaderboard : "ranks"

    Tournaments {
        Guid Id PK "Primary Key"
        string Name "Tournament Name"
        string Description "Description"
        string TournamentType "Type"
        DateTime StartDate "Start Date"
        DateTime EndDate "End Date"
        DateTime RegistrationDeadline "Registration Deadline"
        int MaxParticipants "Max Participants"
        string Rules "Rules JSON"
        string Status "Tournament Status"
        Guid WinnerId FK "Winner Reference"
        DateTime CreatedAt "Tournament Created"
    }

    TournamentParticipants {
        Guid Id PK "Primary Key"
        Guid TournamentId FK "Tournament Reference"
        Guid PlayerId FK "Player Reference"
        DateTime RegisteredAt "Registration Time"
        string Status "Participation Status"
        int Seed "Tournament Seed"
    }

    TournamentRounds {
        Guid Id PK "Primary Key"
        Guid TournamentId FK "Tournament Reference"
        int RoundNumber "Round Number"
        DateTime StartTime "Round Start"
        DateTime EndTime "Round End"
        string Status "Round Status"
    }

    TournamentMatches {
        Guid Id PK "Primary Key"
        Guid TournamentRoundId FK "Round Reference"
        Guid Player1Id FK "Player 1"
        Guid Player2Id FK "Player 2"
        Guid WinnerId FK "Match Winner"
        DateTime ScheduledTime "Scheduled Time"
        DateTime CompletedTime "Completed Time"
        string Status "Match Status"
    }

    TournamentLeaderboard {
        Guid Id PK "Primary Key"
        Guid TournamentId FK "Tournament Reference"
        Guid PlayerId FK "Player Reference"
        int Position "Leaderboard Position"
        int Points "Total Points"
        decimal WinRate "Win Percentage"
        TimeSpan BestTime "Best Completion Time"
        DateTime LastUpdated "Last Updated"
    }
```

### Advanced Analytics Schema (Planned)

```mermaid
---
title: Future Analytics and Monitoring Schema
---
erDiagram
    Players ||--o{ PlayerAnalytics : "generates"
    Games ||--o{ GameAnalytics : "produces"
    Players ||--o{ PlayerBehaviorMetrics : "tracks"
    Games ||--o{ PerformanceMetrics : "measures"
    PlayerAnalytics ||--o{ AnalyticsReports : "included-in"

    PlayerAnalytics {
        Guid Id PK "Primary Key"
        Guid PlayerId FK "Player Reference"
        string MetricType "Metric Type"
        decimal Value "Metric Value"
        string Period "Time Period"
        DateTime CalculatedAt "Calculation Time"
        string Metadata "Additional Data JSON"
    }

    GameAnalytics {
        Guid Id PK "Primary Key"
        Guid GameId FK "Game Reference"
        decimal ClickAccuracy "Click Accuracy"
        TimeSpan AverageClickTime "Avg Click Time"
        int FlagAccuracy "Flag Accuracy"
        string ClickPattern "Click Pattern JSON"
        string HeatmapData "Heatmap JSON"
        DateTime AnalyzedAt "Analysis Time"
    }

    PlayerBehaviorMetrics {
        Guid Id PK "Primary Key"
        Guid PlayerId FK "Player Reference"
        DateTime SessionStart "Session Start"
        DateTime SessionEnd "Session End"
        int GamesPlayed "Games in Session"
        TimeSpan TotalPlayTime "Total Session Time"
        string DeviceInfo "Device Info JSON"
        string BehaviorPatterns "Patterns JSON"
    }

    PerformanceMetrics {
        Guid Id PK "Primary Key"
        Guid GameId FK "Game Reference"
        TimeSpan FirstClickDelay "First Click Delay"
        TimeSpan AverageActionTime "Avg Action Time"
        int BacktrackingCount "Backtracking Count"
        decimal EfficiencyScore "Efficiency Score"
        string PerformanceData "Performance JSON"
        DateTime RecordedAt "Recorded Time"
    }

    AnalyticsReports {
        Guid Id PK "Primary Key"
        string ReportType "Report Type"
        string Title "Report Title"
        DateTime GeneratedAt "Generated Time"
        DateTime PeriodStart "Report Period Start"
        DateTime PeriodEnd "Report Period End"
        string ReportData "Report Data JSON"
        Guid GeneratedBy FK "Generated By User"
    }
```

### Achievement System Schema (Planned)

```mermaid
---
title: Future Achievement System Schema
---
erDiagram
    AchievementDefinitions ||--o{ PlayerAchievements : "earned-as"
    Players ||--o{ PlayerAchievements : "earns"
    AchievementDefinitions ||--o{ AchievementRequirements : "requires"
    PlayerAchievements ||--o{ AchievementProgress : "tracks"

    AchievementDefinitions {
        Guid Id PK "Primary Key"
        string Name "Achievement Name"
        string Description "Description"
        string Category "Category"
        int Points "Points Awarded"
        string IconUrl "Icon URL"
        string BadgeColor "Badge Color"
        bool IsSecret "Is Secret Achievement"
        string RequirementType "Requirement Type"
        string RequirementData "Requirement JSON"
        DateTime CreatedAt "Achievement Created"
    }

    PlayerAchievements {
        Guid Id PK "Primary Key"
        Guid PlayerId FK "Player Reference"
        Guid AchievementId FK "Achievement Reference"
        DateTime EarnedAt "Earned Date"
        string EarnedData "Earned Context JSON"
        bool IsDisplayed "Show on Profile"
    }

    AchievementRequirements {
        Guid Id PK "Primary Key"
        Guid AchievementId FK "Achievement Reference"
        string RequirementType "Requirement Type"
        string RequirementValue "Required Value"
        string Description "Requirement Description"
        int SortOrder "Display Order"
    }

    AchievementProgress {
        Guid Id PK "Primary Key"
        Guid PlayerAchievementId FK "Player Achievement Reference"
        string ProgressType "Progress Type"
        decimal CurrentValue "Current Progress"
        decimal TargetValue "Target Value"
        decimal ProgressPercentage "Progress Percentage"
        DateTime LastUpdated "Last Updated"
    }
```

---

## Entity Framework Configuration

### Connection Strings

**Development (SQLite):**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=minesweeper.db"
  }
}
```

**Production (PostgreSQL):**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=minesweeper;Username=app_user;Password=secure_password"
  }
}
```

### Value Converters

#### GameDifficulty Converter

Converts the `GameDifficulty` value object to/from JSON for database storage.

```csharp
public class GameDifficultyConverter : ValueConverter<GameDifficulty, string>
{
    public GameDifficultyConverter() : base(
        v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
        v => JsonSerializer.Deserialize<GameDifficulty>(v, JsonSerializerOptions.Default))
    { }
}
```

#### CellPosition Converter  

Converts `CellPosition` value objects to/from JSON for collections.

```csharp
public class CellPositionListConverter : ValueConverter<List<CellPosition>, string>
{
    public CellPositionListConverter() : base(
        v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
        v => JsonSerializer.Deserialize<List<CellPosition>>(v, JsonSerializerOptions.Default) ?? new())
    { }
}
```

### Entity Configurations

#### GameConfiguration

```csharp
public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasConversion(id => id.Value, value => new GameId(value));
        
        builder.Property(g => g.PlayerId).HasConversion(id => id.Value, value => new PlayerId(value));
        
        builder.Property(g => g.Difficulty)
               .HasConversion<GameDifficultyConverter>()
               .HasColumnType("nvarchar(max)");
               
        builder.Property(g => g.Status)
               .HasConversion<string>()
               .HasMaxLength(20);
    }
}
```

#### PlayerConfiguration

```csharp
public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasConversion(id => id.Value, value => new PlayerId(value));
        
        builder.Property(p => p.Username).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Email).HasMaxLength(256).IsRequired();
        builder.Property(p => p.DisplayName).HasMaxLength(100).IsRequired();
        
        builder.HasIndex(p => p.Username).IsUnique();
        builder.HasIndex(p => p.Email).IsUnique();
    }
}
```

#### GameBoardConfiguration

```csharp
public class GameBoardConfiguration : IEntityTypeConfiguration<GameBoard>
{
    public void Configure(EntityTypeBuilder<GameBoard> builder)
    {
        builder.HasKey(gb => gb.GameId);
        builder.Property(gb => gb.GameId).HasConversion(id => id.Value, value => new GameId(value));
        
        builder.Property(gb => gb.CellsData)
               .HasColumnType("nvarchar(max)")
               .IsRequired();
               
        builder.Property(gb => gb.MinePositions)
               .HasColumnType("nvarchar(max)");
    }
}
```

---

## Migration History

### Initial Migration (20250805_Initial)

- Created `Players` table with user management fields
- Created `Games` table with game state tracking
- Created `GameBoards` table with board data storage
- Added primary keys and foreign key relationships
- Added initial indexes for common query patterns

### Future Migrations (Planned)

- Authentication tables (Users, Roles, Claims)
- Audit trail tables for game actions
- Tournament and leaderboard tables
- Game replay and statistics tables

---

## Query Patterns

### Common Queries

#### Get Player's Recent Games

```sql
SELECT g.Id, g.Status, g.StartTime, g.EndTime, g.MoveCount
FROM Games g
WHERE g.PlayerId = @playerId
ORDER BY g.StartTime DESC
OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;
```

#### Get Player Statistics

```sql
SELECT 
    p.TotalGames,
    p.GamesWon,
    p.GamesLost,
    CAST(p.GamesWon AS FLOAT) / NULLIF(p.TotalGames, 0) * 100 AS WinPercentage,
    p.CurrentStreak,
    p.LongestStreak
FROM Players p
WHERE p.Id = @playerId;
```

#### Get Active Games Count

```sql
SELECT COUNT(*)
FROM Games g
WHERE g.Status IN ('NotStarted', 'InProgress', 'Paused');
```

### Performance Considerations

#### Indexes for Common Queries

- `IX_Games_PlayerId_Status` - Composite index for player's active games
- `IX_Games_StartTime_Status` - Composite index for recent games by status
- `IX_Players_LastActiveAt` - Index for finding inactive players

#### Query Optimization

- Use pagination for large result sets
- Implement proper caching for player statistics
- Consider read replicas for reporting queries

---

## Data Integrity

### Constraints Summary

1. **Business Rules**
   - Mine count must be less than total cells
   - Games won + lost cannot exceed total games
   - Current streak cannot exceed longest streak

2. **Data Validation**
   - Username and email uniqueness
   - Positive values for dimensions and counts
   - Valid email format enforcement

3. **Referential Integrity**
   - Games must reference valid players
   - GameBoards must reference valid games
   - Cascade deletes for dependent entities

### Audit Trail (Future Enhancement)

Planned audit table structure for tracking all game actions:

```sql
CREATE TABLE GameAuditLog (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    GameId UNIQUEIDENTIFIER NOT NULL,
    PlayerId UNIQUEIDENTIFIER NOT NULL,
    Action NVARCHAR(50) NOT NULL, -- 'CellRevealed', 'CellFlagged', etc.
    Position NVARCHAR(20), -- JSON: {"Row": 0, "Column": 0}
    Timestamp DATETIME2 NOT NULL,
    ClientTimestamp DATETIME2,
    Duration BIGINT, -- Time since last action
    FOREIGN KEY (GameId) REFERENCES Games(Id),
    FOREIGN KEY (PlayerId) REFERENCES Players(Id)
);
```

---

## Security Considerations

### Data Protection

- Player passwords will be hashed using BCrypt (when authentication is added)
- Sensitive player data encrypted at rest
- Connection strings secured in Azure Key Vault (production)

### Access Control

- Row-level security to ensure players can only access their own games
- Database user permissions follow principle of least privilege
- Regular security audits of database access patterns

### Backup Strategy

- Daily automated backups for production databases
- Point-in-time recovery capability
- Backup encryption and secure storage

---

## Monitoring and Maintenance

### Performance Monitoring

- Query execution time tracking
- Database connection pool monitoring
- Index usage statistics

### Maintenance Tasks

- Regular index maintenance and statistics updates
- Cleanup of old game data (retention policy)
- Database size monitoring and growth planning

---

## Development Guidelines

### Adding New Tables

1. Create entity class following DDD principles
2. Add Entity Framework configuration
3. Create and apply migration
4. Update this documentation
5. Add appropriate indexes and constraints

### Schema Changes

1. Use Entity Framework migrations for all schema changes
2. Test migrations on development database first
3. Plan downtime for production deployments
4. Keep this documentation synchronized with actual schema

### Performance Testing

1. Test queries with realistic data volumes
2. Monitor query execution plans
3. Validate index effectiveness
4. Load test database under concurrent access

---

*Last Updated: August 5, 2025*  
*Document Version: 1.0*  
*Schema Version: 1.0*

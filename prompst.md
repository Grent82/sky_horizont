# Prompst

## Project main Prompt

**Prompt (Optimized for ChatGPT):**

You are helping develop a C# project for a science fiction space simulation game, currently titled *Sky Horizont*. The game features a galaxy of planets, each of which belongs to a specific faction. Factions are controlled by NPC leaders and consist of various characters who can be assigned to planetary or faction-level tasks.
The player takes the role of a special character — a faction leader — while other characters (NPCs) can serve in roles such as fleet captains, planetary governors, or experts in research, economy, military, or espionage.
Key elements of the game world:
* **Characters**: Each character can be male or female, has a unique personality, a skillset, and a set of personal traits including age, sex, rank, merits, and family ties.
  * A character's *skills* influence how effectively they perform their assigned tasks.
  * A character's *personality* influences their behavior, decision-making, and interactions with other characters.
  * Characters gain *merits* through successful missions or service, and can be promoted in rank if they accumulate enough merit.
* **Factions**:
  * Factions can engage in diplomacy with each other, including peace treaties, trade agreements, and declarations of war.
  * Pirates exist as special factions with no planets, only fleets.
    * Pirates can be hired by other factions to attack enemy targets.
    * If pirates conquer a planet independently (without being hired), they transform into a new, full-fledged faction.
* **Planets and Systems**:
  * Planets are grouped into star systems.
  * Each system is under the control of a single faction.
  * Planetary control can change through conquest, diplomacy, or rebellion.
**Instructions for ChatGPT:**
Based on this setting, generate ideas, game mechanics, dialogue, or world-building content consistent with this sci-fi simulation concept. Maintain a serious, immersive tone suitable for a deep space strategy/simulation game.

## Project Structure

```
SkyHorizont.sln
│
├── Domain/
│   ├── Core/
│   ├── Enums/
│   ├── Exceptions/
│   ├── Interfaces/
│   ├── Services/
│
├── Application/
│   ├── UseCases/                    (e.g. HostFleet, AssignGovernor, StartEspionage)
│       └── ICommandHandler interfaces
│   ├── Interfaces/                  (IDiplomacyRepository, IMeritCalculator, ITimeService, etc.)
│   └── DTOs / Commands / Queries    (if you prefer strict CQRS-ish separation)
│
├── Infrastructure/
│   ├── Persistence/
│       ├── InMemory/
│       ├── JsonFile/                (e.g. parked state serialization)
│       └── EFCore/ Sqlite/          (if later pivot to DB)
│   ├── GameLoop/                    (time–tick scheduler, event bus)
│   ├── ExternalServices/            (e.g. logging plugins, AI over HTTP)
│   └── Mapping/                     (projections, mappers between Domain and persistence)
│
├── HostRunner/ or SkyHorizont.UI/
│   ├── Console/                     (command‑line runner version)
│   └── UnityAdapter/                (Unity _MonoBehaviour_ scripts invoking Application)
│
├── SimulationEngine/ *(optional)*   (if your simulation is heavy and you want own project)
│
└── Tests/
    ├── Domain.Tests/                (pure domain invariants)
    ├── Application.Tests/           (use‑case correctness with mocks)
    └── Integration.Tests/           (round‑trip testing through Infrastructure)
```

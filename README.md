# Working Title: Sky Horizont

## Project structure

- SkyHorizont.Domain   (Domain Layer)
  - Entities, Enums, ValueObjects
  - Interfaces for Repositories, Domain Services
  - Domain Logic

- SkyHorizont.Application   (Application Layer)
  - Use cases, Commands & Handlers
  - Interfaces for Infrastructure dependencies
  - TurnProcessor 

- SkyHorizont.Infrastructure   (Infrastructure Layer)
  - Concrete implementations
  - Database context, external APIs, etc.

- SkyHorizont.Web (Presentation Layer / Composition Root)
  - ASP.NET Core API controllers
  - Program.cs / Startup.cs—DI setup here

- SkyHorizont.Test
  - Unit Tests

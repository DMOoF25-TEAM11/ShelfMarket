# Shelf Market

## Clean Architecture Folder Structure
The following folder structure is designed to follow the principles of Clean Architecture, promoting separation of concerns and maintainability. Each layer has a specific responsibility, and dependencies flow inward.

```Plaintext
ShelfMarket/
│
├── src/
│   ├── ShelfMarket.Domain/
│   │   ├── Entities/                   # Core business entities
│   │   ├── ValueObjects/               # Immutable objects representing concepts
│   │   ├── Interfaces/                 # Repository and service interfaces
│   │   └── DomainServices/             # Business logic services
│   │
│   ├── ShelfMarket.Application/
│   │   ├── UseCases/                   # Application use cases
│   │   ├── DTOs/                       # Data Transfer Objects
│   │   ├── Interfaces/                 # Application service interfaces
│   │   └── Services/                   # Application services
│   │
│   ├── ShelfMarket.Infrastructure/
│   │   ├── Persistence/                # Database context and migrations
│   │   ├── Repositories/               # Repository implementations
│   │   ├── ExternalServices/           # Integrations with external systems
│   │   └── Configuration/              # Infrastructure configurations
│   │
│   ├── ShelfMarket.UI/
│   │   ├── Views/                      # UI views and pages
│   │   │   └── UserControls/           # Reusable UI components
│   │   ├── ViewModels/                 # UI logic and data binding
│   │   ├── Commands/                   # UI commands and actions
│   │   └── Helpers/                    # UI utility classes
│
├── tests/
│   ├── ShelfMarket.Domain.Tests/
│   ├── ShelfMarket.Application.Tests/
│   └── ShelfMarket.Infrastructure.Tests/
│
└── README.md
```

### Layer Responsibilities

#### Domain Layer
- Pure business logic
- No dependencies on other layers
- Contains entities, value objects, domain events, and domain services

#### Application Layer
- Coordinates use cases
- Depends on the Domain layer
- Defines interfaces for services and repositories
- Contains DTOs and application-level logic

#### Infrastructure Layer
- mplements interfaces from Application and Domain layers
- Handles persistence (e.g., EF Core), external APIs, file systems, etc.
- Should not contain business logic

#### UI Layer
- WPF or other front-end technology
- Implements MVVM pattern
- Talks to Application layer via ViewModels and Services

### Dependency Flow
- UI → Infrastructure → Application → Domain
- Infrastructure implements interfaces from Domain/Application
- No layer should depend on Infrastructure
Use Dependency Injection to wire everything together, typically in the UI layer's composition root.

# Reol marked projekt (Shelf Market project)
A WPF application for managing a shelf market, built with .NET 9 and following Clean Architecture principles.

## About
This project is a sample implementation of a shelf market management system using WPF for the user interface and .NET 9 for the backend.
It adheres to Clean Architecture principles, ensuring a clear separation of concerns and maintainability.

## Table of Contents
- [About](#about)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
- [Clean Architecture Folder Structure](#clean-architecture-folder-structure)
  - [Layer Responsibilities](#layer-responsibilities)
    - [Domain Layer](#domain-layer)
    - [Application Layer](#application-layer)
    - [Infrastructure Layer](#infrastructure-layer)
    - [UI Layer](#ui-layer)
  - [Dependency Flow](#dependency-flow)
- [Functionality](#functionality)
  - [Shelf Market Management](#shelf-market-management)
  - [EAN-13 Barcode Generator (Shelf + Price)](#ean-13-barcode-generator-shelf--price)


## Getting Started
### Prerequisites
- .NET 9 SDK
- MSSQL Server or SQL Server Express

### Installation


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
│   └── ShelfMarket.UI/
│       ├── Views/                      # UI views and pages
│       │   └── UserControls/           # Reusable UI components
│       ├── ViewModels/                 # UI logic and data binding
│       ├── Commands/                   # UI commands and actions
│       └── Helpers/                    # UI utility classes
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


## Functionality

### Shelf Market Management
- Manage Tenants.
- Manage Shelves.
- Manage Shelf Types
- Assign Shelves to Tenants.

### EAN‑13 Barcode Generator (Shelf + Price)
- Generate EAN-13 barcodes for products.
- Input Tenant shelf id and price.

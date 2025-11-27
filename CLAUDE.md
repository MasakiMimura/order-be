# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## Project Overview

**OrderBE** is a .NET 8.0 order management backend service using:
- Entity Framework Core 9.0 with PostgreSQL (Npgsql)
- xUnit for unit testing with InMemory database
- TDD (Test-Driven Development) methodology

**Current Status**: OrderRepository implementation complete with 7 passing unit tests.

---

## Essential Commands

### Building
```bash
dotnet build
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~OrderRepositoryTests"

# Run with detailed output
dotnet test --logger "console;verbosity=normal"
```

### Database Migrations
```bash
# Add migration
dotnet ef migrations add <MigrationName>

# Update database
dotnet ef database update
```

### Clean Build
```bash
dotnet clean
rm -rf obj bin
dotnet build
```

---

## Git Commit Process

When user says "コミット" or "commit", automatically create a commit following this format:

### Commit Message Format

```
<prefix>: <日本語サマリ（命令形・簡潔に）>

- 変更内容1（箇条書き）
- 変更内容2（箇条書き）

Refs: Task-XXX（タスクIDがある場合）
```

### Prefixes
- `feat`: New feature
- `fix`: Bug fix
- `refactor`: Code refactoring (no behavior change)
- `test`: Test additions/modifications
- `docs`: Documentation updates
- `build`: Build/dependency changes
- `chore`: Miscellaneous (tool config, scripts)

### Important Rules
- **NO AI-generated signatures** (no 🤖 Generated with Claude Code)
- Write summary in **Japanese** (not English only)
- Use **bullet points** for body (no long paragraphs)
- Include task documents in the same commit

### Commit Steps
1. `git status` and `git diff` to check changes
2. `git log -2 --format=full` to understand format
3. Create commit message following format
4. Execute commit with HEREDOC:
```bash
git add <files>
git commit -m "$(cat <<'EOF'
<commit message>
EOF
)"
```

---

## Architecture

### Project Structure

```
order_be/
├── Data/                   # DbContext (OrderDbContext)
├── Models/                 # Entities (Order, OrderItem)
├── Repository/             # Repository layer (OrderRepository)
├── Exceptions/             # Custom exceptions (EntityNotFoundException)
├── TestData/               # Test data Seeder/Cleaner
├── Test/                   # Test project (excluded from build)
│   └── Unit/
│       └── OrderManagement/
│           └── OrderRepositoryTests.cs
├── tasks/                  # Task documents (excluded from build)
│   └── OM-001/
│       └── Task-OM-001-P3-R-OrderRepository/
└── docs/
    └── studies/            # Learning documentation
```

### Key Components

**OrderRepository** (`Repository/OrderRepository.cs`)
- CRUD operations for Order entity
- Methods: CreateOrderAsync, GetOrderByIdAsync, GetOrdersByStatusAsync, UpdateOrderAsync, DeleteOrderAsync
- UpdateOrderAsync uses UpdateChildCollection pattern for OrderItem management
- Comprehensive exception handling (DbUpdateException, NpgsqlException, TimeoutException)
- All methods include detailed XML documentation comments

**OrderRepositoryTests** (`Test/Unit/OrderManagement/OrderRepositoryTests.cs`)
- 7 unit tests using xUnit and InMemory database
- AAA (Arrange-Act-Assert) pattern
- Tests both normal and error cases
- Uses Moq for ILogger mocking

### Database Schema

**order table**
- order_id (SERIAL PRIMARY KEY)
- created_at (TIMESTAMPTZ)
- member_card_no (VARCHAR, nullable)
- total (NUMERIC(10,2))
- status (VARCHAR - 'IN_ORDER', 'CONFIRMED', 'PAID')

**order_item table**
- order_item_id (SERIAL PRIMARY KEY)
- order_id (FK to order, CASCADE DELETE)
- product_id, product_name, product_price, product_discount_percent, quantity

---

## TDD Workflow

### Test Tasks (RT, ST, CT suffixes)
- Create **test code only** (no implementation)
- Tests should be in "Red" state (failing)
- Implementation comes in next task (R, S suffixes)
- Minimal exception classes (like EntityNotFoundException) are allowed

### Test Naming Conventions
- Test class: `{TargetClass}Tests` (e.g., OrderRepositoryTests)
- Test method: `{MethodName}_{Scenario}_{ExpectedBehavior}`
  - Example: `CreateOrderAsync_ValidOrder_ReturnsCreatedOrder`

---

## Build Configuration

### OrderBE.csproj Exclusions

```xml
<ItemGroup>
  <Compile Remove="tasks\**\*.cs" />
  <Compile Remove="Test\**\*.cs" />
</ItemGroup>

<ItemGroup>
  <Compile Include="Test\Unit\OrderManagement\OrderRepositoryTests.cs" />
</ItemGroup>
```

**Why**: Test and task files must be excluded from main project build but test files need to be explicitly included for test execution.

### Test Dependencies

Required packages for unit testing:
- xunit (2.9.3)
- xunit.runner.visualstudio (3.1.5)
- Microsoft.NET.Test.Sdk (18.0.0)
- Moq (4.20.72)
- Microsoft.EntityFrameworkCore.InMemory (9.0.9)

---

## Task Execution

When user says "tasks\XXX\XXX.mdを実施":
1. Read the task document
2. Follow instructions in the task document
3. For test tasks: create tests only (TDD Red state)
4. For implementation tasks: implement to make tests pass (TDD Green state)
5. Add detailed XML comments to all code (処理内容を日本語で説明)

---

## Common Issues

### Build Error: Test files being compiled
**Solution**: Ensure Test directory is excluded in .csproj (see Build Configuration)

### Test Error: EntityNotFoundException not found
**Solution**: Create Exceptions/EntityNotFoundException.cs with standard exception pattern

### Test Error: [Fact] attribute not recognized
**Solution**: Add `using Xunit;` to test file

---

## Reference Documents

- Git commit rules: `.claude/rules/git-commit-message-format.mdc`
- xUnit test structure: `docs/studies/xunit-test-code-structure.md`
- Test output guide: `docs/studies/dotnet-test-output-guide.md`

---

**Last Updated**: 2025-10-12
**Project**: OrderBE (order_be)
**Framework**: .NET 8.0
**Database**: PostgreSQL via Entity Framework Core 9.0
**Testing**: xUnit with InMemory database
**Test Status**: ✅ 7/7 tests passing (964ms)

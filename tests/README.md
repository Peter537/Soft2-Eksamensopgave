# MToGo Test Suites

This directory contains different types of tests for the MToGo platform.

## Test Types

### 1. Unit Tests (`MToGo/tests/*`)

- Run in CI/CD pipeline on every push/PR
- Test individual components in isolation
- Fast execution, no external dependencies

```bash
dotnet test MToGo/tests/MToGo.OrderService.Tests/
```

### 2. Integration Tests (`MToGo/tests/MToGo.IntegrationTests/`)

- Run against a deployed staging environment
- Test service-to-service communication
- Verify API contracts and routing

```bash
# Set the API URL first
export MTOGO_API_URL=http://staging-api-url
dotnet test MToGo/tests/MToGo.IntegrationTests/
```

### 3. Load Tests (`tests/load/`)

- Performance and stress testing
- Uses k6 for load generation
- Run manually or in staging workflow

```bash
# Install k6: https://k6.io/docs/getting-started/installation/
k6 run tests/load/k6-script.js --env API_URL=http://your-api-url
```

## Test Pyramid

```
        /\
       /  \     E2E Tests (few, slow, expensive)
      /----\
     /      \   Integration Tests (some, medium)
    /--------\
   /          \ Unit Tests (many, fast, cheap)
  /------------\
```

## Running Tests in CI/CD

| Test Type         | When           | Where              |
| ----------------- | -------------- | ------------------ |
| Unit Tests        | Every push/PR  | `cicd.yml`         |
| Integration Tests | Staging deploy | `staging-test.yml` |
| Load Tests        | Manual trigger | `staging-test.yml` |

## Adding New Tests

### Unit Test

Add to the appropriate `MToGo/tests/MToGo.<Service>.Tests/` project.

### Integration Test

Add to `MToGo/tests/MToGo.IntegrationTests/`.

### Load Test

Modify `tests/load/k6-script.js` or create new k6 scripts.

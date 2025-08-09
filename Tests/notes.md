```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report
# open coverage-report/index.html

dotnet test --collect:"XPlat Code Coverage"
```

```bash
dotnet add Tests package FluentAssertions
dotnet add Tests package Moq
```
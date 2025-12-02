using Testcontainers.PostgreSql;

namespace MToGo.Testing;

public static class PostgreSqlContainerHelper
{
    public static PostgreSqlContainer CreatePostgreSqlContainer()
    {
        return new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("mtogo")
            .WithUsername("mtogo")
            .WithPassword("mtogo_password")
            .WithPortBinding(5432, true)
            .Build();
    }
}

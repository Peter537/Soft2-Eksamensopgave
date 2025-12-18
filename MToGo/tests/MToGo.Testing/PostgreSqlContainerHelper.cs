using Testcontainers.PostgreSql;

namespace MToGo.Testing;

public static class PostgreSqlContainerHelper
{
    public static PostgreSqlContainer CreatePostgreSqlContainer()
    {
        return CreatePostgreSqlContainer("mtogo");
    }

    public static PostgreSqlContainer CreatePostgreSqlContainer(string database)
    {
        return new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase(database)
            .WithUsername("mtogo")
            .WithPassword("mtogo_password")
            .WithPortBinding(5432, true)
            .Build();
    }
}


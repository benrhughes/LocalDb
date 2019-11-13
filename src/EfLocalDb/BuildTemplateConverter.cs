using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

static class BuildTemplateConverter
{
    public static Func<SqlConnection, DbContextOptionsBuilder<TDbContext>, Task> Convert<TDbContext>(Func<TDbContext, Task>? buildTemplate)
        where TDbContext : DbContext
    {
        return async (connection, builder) =>
        {
            var activator = CreateActivator<TDbContext>(builder.Options);
            await using var dbContext = activator();
            if (buildTemplate == null)
            {
                await dbContext.Database.EnsureCreatedAsync();
            }
            else
            {
                await buildTemplate(dbContext);
            }
        };
    }
    static Func<TDbContext> CreateActivator<TDbContext>(DbContextOptions options)
        where TDbContext : DbContext
    {
        var constructors
            = typeof(TDbContext).GetTypeInfo().DeclaredConstructors
                .Where(c => !c.IsStatic && c.IsPublic)
                .ToArray();

        if (constructors.Length == 1)
        {
            var parameters = constructors[0].GetParameters();

            if (parameters.Length == 1
                && (parameters[0].ParameterType == typeof(DbContextOptions)
                    || parameters[0].ParameterType == typeof(DbContextOptions<TDbContext>)))
            {
                return
                    Expression.Lambda<Func<TDbContext>>(
                            Expression.New(constructors[0], Expression.Constant(options)))
                        .Compile();
            }
        }

        throw new Exception($"Expected {typeof(TDbContext).Name} to have a single constructor that takes a DbContextOptions.");
    }
}
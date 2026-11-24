using System.Transactions;
using FitnessCenter.Modules.Booking.Infrastructure;
using FitnessCenter.Modules.Members.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// FIT-36: proves the database layout the modules rely on. One PostgreSQL schema per module,
/// each with its own migrations history table (D-014), and one transaction that spans module
/// DbContexts through the request's shared connection (D-015). Settles open question DB8.
/// </summary>
[TestClass]
public sealed class ModuleSchemaTests
{
    /// <summary>
    /// Every DbContext the API registers has a default schema of its own, maps every table to
    /// it, and keeps its migrations history table there. A new module that forgets
    /// <c>HasDefaultSchema</c> or <c>MigrationsHistoryTable</c> fails here.
    /// </summary>
    [TestMethod]
    public async Task Every_module_context_keeps_its_tables_and_history_in_its_own_schema()
    {
        var contextTypes = new List<Type>();
        await using var factory = new ApiFactory(services => contextTypes.AddRange(RegisteredDbContexts(services)));
        await using var scope = factory.Services.CreateAsyncScope();

        Assert.IsGreaterThanOrEqualTo(4, contextTypes.Count, "expected the Identity, Members, Booking and Access contexts");
        var schemas = new HashSet<string>();
        foreach (var type in contextTypes)
        {
            var db = (DbContext)scope.ServiceProvider.GetRequiredService(type);
            var schema = db.Model.GetDefaultSchema();

            Assert.IsNotNull(schema, $"{type.Name} has no default schema");
            Assert.AreNotEqual("public", schema, $"{type.Name} uses the public schema");
            Assert.IsTrue(schemas.Add(schema), $"{type.Name} shares the schema '{schema}' with another context");

            foreach (var entity in db.Model.GetEntityTypes().Where(e => e.GetTableName() is not null))
            {
                Assert.AreEqual(schema, entity.GetSchema(), $"{type.Name}: {entity.DisplayName()} is outside '{schema}'");
            }

            var relational = db.GetService<IDbContextOptions>().Extensions.OfType<RelationalOptionsExtension>().Single();
            Assert.AreEqual(schema, relational.MigrationsHistoryTableSchema, $"{type.Name}: history table outside '{schema}'");
        }

        // The migrated database agrees: nothing in public, one history table per module schema.
        var anyDb = (DbContext)scope.ServiceProvider.GetRequiredService(contextTypes[0]);
        var publicTables = await anyDb.Database
            .SqlQuery<string>($"SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'public'")
            .ToListAsync();
        Assert.IsEmpty(publicTables, $"tables in public: {string.Join(", ", publicTables)}");

        var historySchemas = await anyDb.Database
            .SqlQuery<string>($"SELECT table_schema AS \"Value\" FROM information_schema.tables WHERE table_name = '__EFMigrationsHistory'")
            .ToListAsync();
        CollectionAssert.AreEquivalent(schemas.ToList(), historySchemas);
    }

    /// <summary>
    /// EF Core writes the schema into every table name, so queries work without the module
    /// schemas on the connection's <c>search_path</c>.
    /// </summary>
    [TestMethod]
    public async Task Queries_name_the_schema_and_do_not_depend_on_the_search_path()
    {
        await using var factory = new ApiFactory();
        await using var scope = factory.Services.CreateAsyncScope();
        var booking = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var members = scope.ServiceProvider.GetRequiredService<MembersDbContext>();

        var sql = booking.GetService<ISqlGenerationHelper>();
        StringAssert.Contains(booking.ClassTypes.ToQueryString(), sql.DelimitIdentifier("class_types", "booking"));
        StringAssert.Contains(members.MembershipTypes.ToQueryString(), sql.DelimitIdentifier("membership_types", "members"));

        var searchPath = await booking.Database.SqlQuery<string>($"SHOW search_path").ToListAsync();
        Assert.DoesNotContain("booking", searchPath.Single());
        Assert.DoesNotContain("members", searchPath.Single());

        // Both contexts use the request's shared connection, so this runs with that search_path.
        Assert.IsNotEmpty(await booking.ClassTypes.ToListAsync());
        Assert.IsNotEmpty(await members.MembershipTypes.ToListAsync());
    }

    /// <summary>
    /// D-015: writes from two module DbContexts in one <see cref="TransactionScope"/> (the
    /// pattern <c>SignupHandler</c> uses) are one local transaction on the shared connection.
    /// It does not escalate to a distributed transaction, and rolling it back leaves both
    /// schemas untouched.
    /// </summary>
    [TestMethod]
    public async Task A_rolled_back_transaction_across_two_modules_leaves_both_schemas_untouched()
    {
        await using var factory = new ApiFactory();
        var classTypeId = Guid.NewGuid();
        var membershipTypeId = Guid.NewGuid();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var booking = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            var members = scope.ServiceProvider.GetRequiredService<MembersDbContext>();

            using (new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                TransactionScopeAsyncFlowOption.Enabled))
            {
                await booking.Database.ExecuteSqlAsync(
                    $"INSERT INTO booking.class_types (\"Id\", \"Name\") VALUES ({classTypeId}, 'FIT-36 rollback test')");
                await members.Database.ExecuteSqlAsync(
                    $"INSERT INTO members.membership_types (\"Id\", \"Name\", \"MonthlyPriceOre\") VALUES ({membershipTypeId}, 'FIT-36 rollback test', 0)");

                // Both rows are written, in one local transaction.
                Assert.AreEqual(1, await CountClassTypesAsync(members, classTypeId));
                Assert.AreEqual(1, await CountMembershipTypesAsync(booking, membershipTypeId));
                Assert.AreEqual(Guid.Empty, Transaction.Current!.TransactionInformation.DistributedIdentifier, "escalated");

                // Leaving the scope without Complete() rolls back, as a failing handler would.
            }
        }

        await using var check = factory.Services.CreateAsyncScope();
        var db = check.ServiceProvider.GetRequiredService<BookingDbContext>();
        Assert.AreEqual(0, await CountClassTypesAsync(db, classTypeId), "the Booking write was kept");
        Assert.AreEqual(0, await CountMembershipTypesAsync(db, membershipTypeId), "the Members write was kept");
    }

    private static IEnumerable<Type> RegisteredDbContexts(IServiceCollection services) =>
        services.Select(d => d.ServiceType).Where(t => t.IsSubclassOf(typeof(DbContext))).Distinct();

    // Counts through the other module's context on purpose: the point is that both see one transaction.
    private static Task<int> CountClassTypesAsync(DbContext db, Guid id) =>
        db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM booking.class_types WHERE \"Id\" = {id}").SingleAsync();

    private static Task<int> CountMembershipTypesAsync(DbContext db, Guid id) =>
        db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM members.membership_types WHERE \"Id\" = {id}").SingleAsync();
}

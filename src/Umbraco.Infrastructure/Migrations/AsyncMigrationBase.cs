using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Infrastructure.Migrations.Expressions.Alter;
using Umbraco.Cms.Infrastructure.Migrations.Expressions.Create;
using Umbraco.Cms.Infrastructure.Migrations.Expressions.Delete;
using Umbraco.Cms.Infrastructure.Migrations.Expressions.Execute;
using Umbraco.Cms.Infrastructure.Migrations.Expressions.Insert;
using Umbraco.Cms.Infrastructure.Migrations.Expressions.Rename;
using Umbraco.Cms.Infrastructure.Migrations.Expressions.Update;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Persistence.SqlSyntax;

namespace Umbraco.Cms.Infrastructure.Migrations;

/// <summary>
/// Provides a base class to all migrations.
/// </summary>
public abstract partial class AsyncMigrationBase : IDiscoverable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncMigrationBase" /> class.
    /// </summary>
    /// <param name="context">A migration context.</param>
    protected AsyncMigrationBase(IMigrationContext context)
        => Context = context;

    /// <summary>
    /// Builds an Alter expression.
    /// </summary>
    public IAlterBuilder Alter => BeginBuild(new AlterBuilder(Context));

    /// <summary>
    /// Gets the migration context.
    /// </summary>
    protected IMigrationContext Context { get; }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected ILogger Logger => Context.Logger;

    /// <summary>
    /// Gets the SQL syntax.
    /// </summary>
    protected ISqlSyntaxProvider SqlSyntax => Context.SqlContext.SqlSyntax;

    /// <summary>
    /// Gets the database instance.
    /// </summary>
    protected IUmbracoDatabase Database => Context.Database;

    /// <summary>
    /// Gets the database type.
    /// </summary>
    protected DatabaseType DatabaseType => Context.Database.DatabaseType;

    /// <summary>
    /// Builds a Create expression.
    /// </summary>
    public ICreateBuilder Create => BeginBuild(new CreateBuilder(Context));

    /// <summary>
    /// Builds a Delete expression.
    /// </summary>
    public IDeleteBuilder Delete => BeginBuild(new DeleteBuilder(Context));

    /// <summary>
    /// Builds an Execute expression.
    /// </summary>
    public IExecuteBuilder Execute => BeginBuild(new ExecuteBuilder(Context));

    /// <summary>
    /// Builds an Insert expression.
    /// </summary>
    public IInsertBuilder Insert => BeginBuild(new InsertBuilder(Context));

    /// <summary>
    /// Builds a Rename expression.
    /// </summary>
    public IRenameBuilder Rename => BeginBuild(new RenameBuilder(Context));

    /// <summary>
    /// Builds an Update expression.
    /// </summary>
    public IUpdateBuilder Update => BeginBuild(new UpdateBuilder(Context));

    /// <summary>
    /// If this is set to true, the published cache will be rebuild upon successful completion of the migration.
    /// </summary>
    public bool RebuildCache { get; set; }

    /// <summary>
    /// If this is set to true, all back-office client tokens will be revoked upon successful completion of the migration.
    /// </summary>
    public bool InvalidateBackofficeUserAccess { get; set; }

    /// <summary>
    /// Runs the migration.
    /// </summary>
    public async Task RunAsync()
    {
        await MigrateAsync().ConfigureAwait(false);

        // ensure there is no building expression
        // ie we did not forget to .Do() an expression
        if (Context.BuildingExpression)
        {
            throw new IncompleteMigrationExpressionException("The migration has run, but leaves an expression that has not run.");
        }
    }

    /// <summary>
    /// Creates a new Sql statement.
    /// </summary>
    protected Sql<ISqlContext> Sql() => Context.SqlContext.Sql();

    /// <summary>
    /// Creates a new Sql statement with arguments.
    /// </summary>
    protected Sql<ISqlContext> Sql(string sql, params object[] args) => Context.SqlContext.Sql(sql, args);

    /// <summary>
    /// Executes the migration.
    /// </summary>
    protected abstract Task MigrateAsync();

    // ensures we are not already building,
    // ie we did not forget to .Do() an expression
    private protected T BeginBuild<T>(T builder)
    {
        if (Context.BuildingExpression)
        {
            throw new IncompleteMigrationExpressionException("Cannot create a new expression: the previous expression has not run.");
        }

        Context.BuildingExpression = true;
        return builder;
    }
}

using System.Linq.Expressions;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Persistence.Querying;

namespace Umbraco.Cms.Core.Persistence.Repositories;

public interface IUserRepository : IReadWriteQueryRepository<Guid, IUser>
{
    /// <summary>
    ///     Gets the count of items based on a complex query
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    int GetCountByQuery(IQuery<IUser>? query);

    /// <summary>
    ///     Checks if a user with the username exists
    /// </summary>
    /// <param name="username"></param>
    /// <returns></returns>
    bool ExistsByUserName(string username);

    /// <summary>
    ///     Returns a user by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns>
    ///     A cached <see cref="IUser" /> instance
    /// </returns>
    IUser? Get(int id);

    /// <summary>
    ///     Checks if a user with the login exists
    /// </summary>
    /// <param name="login"></param>
    /// <returns></returns>
    bool ExistsByLogin(string login);

    /// <summary>
    ///     Gets a list of <see cref="IUser" /> objects associated with a given group
    /// </summary>
    /// <param name="groupId">Id of group</param>
    IEnumerable<IUser> GetAllInGroup(int groupId);

    /// <summary>
    ///     Gets a list of <see cref="IUser" /> objects not associated with a given group
    /// </summary>
    /// <param name="groupId">Id of group</param>
    IEnumerable<IUser> GetAllNotInGroup(int groupId);

    /// <summary>
    ///     Gets paged user results
    /// </summary>
    /// <param name="query"></param>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    /// <param name="totalRecords"></param>
    /// <param name="orderBy"></param>
    /// <param name="orderDirection"></param>
    /// <param name="includeUserGroups">
    ///     A filter to only include user that belong to these user groups
    /// </param>
    /// <param name="excludeUserGroups">
    ///     A filter to only include users that do not belong to these user groups
    /// </param>
    /// <param name="userState">Optional parameter to filter by specified user state</param>
    /// <param name="filter"></param>
    /// <returns></returns>
    IEnumerable<IUser> GetPagedResultsByQuery(
        IQuery<IUser>? query,
        long pageIndex,
        int pageSize,
        out long totalRecords,
        Expression<Func<IUser, object?>> orderBy,
        Direction orderDirection = Direction.Ascending,
        string[]? includeUserGroups = null,
        string[]? excludeUserGroups = null,
        UserState[]? userState = null,
        IQuery<IUser>? filter = null);

    /// <summary>
    ///     Returns a user by username
    /// </summary>
    /// <param name="username"></param>
    /// <param name="includeSecurityData">
    ///     This is only used for a shim in order to upgrade to 7.7
    /// </param>
    /// <returns>
    ///     A non cached <see cref="IUser" /> instance
    /// </returns>
    IUser? GetByUsername(string username, bool includeSecurityData);

    /// <summary>
    /// Gets a user by username for upgrade purposes, this will only return a result if the current runtime state is upgrade.
    /// </summary>
    /// <remarks>
    /// This only resolves the minimum amount of fields required to authorize for an upgrade.
    /// We need this to be able to add new columns to the user table.
    /// </remarks>
    /// <param name="username">The username to find the user by.</param>
    /// <returns>An uncached <see cref="IUser"/> instance.</returns>
    IUser? GetForUpgradeByUsername(string username) => GetByUsername(username, false);

    /// <summary>
    /// Gets a user by email for upgrade purposes, this will only return a result if the current runtime state is upgrade.
    /// </summary>
    /// <remarks>
    /// This only resolves the minimum amount of fields required to authorize for an upgrade.
    /// We need this to be able to add new columns to the user table.
    /// </remarks>
    /// <param name="email">The email to find the user by.</param>
    /// <returns>An uncached <see cref="IUser"/> instance.</returns>
    IUser? GetForUpgradeByEmail(string email) => GetMany().FirstOrDefault(x=>x.Email == email);

    /// <summary>
    /// Gets a user for upgrade purposes, this will only return a result if the current runtime state is upgrade.
    /// </summary>
    /// <remarks>
    /// This only resolves the minimum amount of fields required to authorize for an upgrade.
    /// We need this to be able to add new columns to the user table.
    /// </remarks>
    /// <param name="id">The id to find the user by.</param>
    /// <returns>An uncached <see cref="IUser"/> instance.</returns>
    IUser? GetForUpgrade(int id) => Get(id, false);

    /// <summary>
    ///     Returns a user by id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="includeSecurityData">
    ///     This is only used for a shim in order to upgrade to 7.7
    /// </param>
    /// <returns>
    ///     A non cached <see cref="IUser" /> instance
    /// </returns>
    IUser? Get(int? id, bool includeSecurityData);

    IProfile? GetProfile(string username);

    IProfile? GetProfile(int id);

    IDictionary<UserState, int> GetUserStates();

    Guid CreateLoginSession(int? userId, string requestingIpAddress, bool cleanStaleSessions = true);

    bool ValidateLoginSession(int userId, Guid sessionId);

    int ClearLoginSessions(int userId);

    int ClearLoginSessions(TimeSpan timespan);

    void ClearLoginSession(Guid sessionId);

    IEnumerable<string> GetAllClientIds();

    IEnumerable<string> GetClientIds(int id);

    void AddClientId(int id, string clientId);

    bool RemoveClientId(int id, string clientId);

    IUser? GetByClientId(string clientId);

    /// <summary>
    ///     Invalidates sessions for users that aren't associated with the current collection of providers.
    /// </summary>
    /// <param name="currentLoginProviders">The names of the currently configured providers.</param>
    void InvalidateSessionsForRemovedProviders(IEnumerable<string> currentLoginProviders) { }
}

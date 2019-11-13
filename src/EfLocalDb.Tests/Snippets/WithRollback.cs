using System.Threading.Tasks;
using EfLocalDb;

class WithRollback
{
    async Task Usage()
    {
        #region EfWithRollback
        var sqlInstance = new SqlInstance<TheDbContext>();

        await using var sqlDatabase = await sqlInstance.BuildWithRollback();
        var sqlConnection = sqlDatabase.Connection;
        var dbContext = sqlDatabase.Context;
        //Use the SqlConnection or TheDbContext
        #endregion
    }
}
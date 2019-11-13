using EfLocalDb;

class EfExplicitName
{
    EfExplicitName()
    {
        #region EfExplicitName
        var sqlInstance = new SqlInstance<TheDbContext>(
            name: "theInstanceName",
            directory: @"C:\LocalDb\theInstance");
        #endregion
    }
}
using System.Data;
using Microsoft.EntityFrameworkCore;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Domain.Entities;
using ShelfMarket.Infrastructure.Persistence;

namespace ShelfMarket.Infrastructure.Repositories;

public class SalesRepository : Repository<Sales>, ISalesRepository
{
    public SalesRepository(ShelfMarketDbContext context) : base(context)
    {
    }

    public async Task<decimal> GetCashSalesAsync(DateTime date)
    {
        // Uses stored procedure: uspGetDailyCashSalesSum
        // Assumptions:
        // - The procedure returns a single scalar (DECIMAL / NUMERIC) = total cash sales for the given date.
        // - It accepts a single parameter named @Date (adjust if your parameter name differs).
        var connection = _context.Database.GetDbConnection();

        await using var command = connection.CreateCommand();
        command.CommandText = "uspGetDailyCashSalesSum";
        command.CommandType = CommandType.StoredProcedure;

        var dateParam = command.CreateParameter();
        dateParam.ParameterName = "@SalesDate";
        dateParam.Value = date.Date;
        dateParam.DbType = DbType.Date;
        command.Parameters.Add(dateParam);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        var result = await command.ExecuteScalarAsync();
        return (result == null || result == DBNull.Value) ? 0m : Convert.ToDecimal(result);
    }
}

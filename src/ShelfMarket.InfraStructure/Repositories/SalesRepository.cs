using System.Data;
using Microsoft.Data.SqlClient; // <-- Use Microsoft.Data.SqlClient instead of System.Data.SqlClient
using Microsoft.EntityFrameworkCore;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Application.DTOs;
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
        // - It accepts a single parameter named @IssuedAt (adjust if your parameter name differs).
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

    public async Task<SalesReceiptWithTotalAmountDto> SetSaleAsync(IEnumerable<SalesLine> lines, bool paidByCash, bool paidByMobile)
    {
        var connection = _context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "uspCreateSalesReceipt";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@PaidByCash", paidByCash));
        command.Parameters.Add(new SqlParameter("@PaidByMobile", paidByMobile));
        command.Parameters.Add(new SqlParameter("@IssuedAt", DateTime.Now));

        var linesParam = new SqlParameter("@Lines", SqlDbType.Structured)
        {
            TypeName = "dbo.SalesReceiptLineInput",
            Value = ToDataTable(lines)
        };
        command.Parameters.Add(linesParam);

        var receiptId = new SqlParameter("@ReceiptId", SqlDbType.UniqueIdentifier)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(receiptId);

        var receiptNumber = new SqlParameter("@ReceiptNumber", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(receiptNumber);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        await command.ExecuteNonQueryAsync();

        // Create and return the DTO
        return new SalesReceiptWithTotalAmountDto
        {
            Id = receiptId.Value != DBNull.Value ? (Guid?)receiptId.Value : null,
            ReceiptNumber = receiptNumber.Value != DBNull.Value ? (uint?)(int)receiptNumber.Value : null,
            IssuedAt = DateTime.Now,
            PaidByCash = paidByCash,
            PaidByMobile = paidByMobile
            // VatAmount and TotalAmount can be set if you have output parameters for them
        };
    }

    private DataTable ToDataTable(IEnumerable<SalesLine> lines)
    {
        var table = new DataTable();
        table.Columns.Add("ShelfNumber", typeof(int));
        table.Columns.Add("UnitPrice", typeof(decimal));
        foreach (var line in lines)
            table.Rows.Add((int)line.ShelfNumber, line.Price);
        return table;
    }
}

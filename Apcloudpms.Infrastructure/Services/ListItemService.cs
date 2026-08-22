using System.Data;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Apcloudpms.Infrastructure.Services;

public sealed class ListItemService : IListItemService
{
    private readonly AppDbContext _context;

    public ListItemService(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<ListItemDto>> GetByCategoryAsync(
        string categoryName,
        CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();
        var shouldCloseConnection = connection.State == ConnectionState.Closed;

        if (shouldCloseConnection)
            await _context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.SpGetListItemsByCategory";
            command.CommandType = CommandType.StoredProcedure;

            var categoryParameter = command.CreateParameter();
            categoryParameter.ParameterName = "@CategoryName";
            categoryParameter.DbType = DbType.String;
            categoryParameter.Size = 100;
            categoryParameter.Value = categoryName.Trim();
            command.Parameters.Add(categoryParameter);

            var listItems = new List<ListItemDto>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                listItems.Add(new ListItemDto(
                    reader.GetInt32(reader.GetOrdinal("ListItemId")),
                    reader.GetInt32(reader.GetOrdinal("ListItemCategoryId")),
                    reader.GetString(reader.GetOrdinal("Code")),
                    reader.GetString(reader.GetOrdinal("Name")),
                    reader.IsDBNull(reader.GetOrdinal("Description"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("Description")),
                    reader.GetInt32(reader.GetOrdinal("DisplayOrder"))));
            }

            return listItems;
        }
        finally
        {
            if (shouldCloseConnection)
                await _context.Database.CloseConnectionAsync();
        }
    }
}

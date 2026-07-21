using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.Shared.Helpers;

public static class SqlBulkHelper
{
    public static List<string> GetTableColumns(SqlConnection conn, string tableName)
    {
        var columns = new List<string>();
        const string query = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName";

        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@tableName", tableName);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            columns.Add(reader.GetString(0));
        }
        return columns;
    }

    public static void MapColumnsCaseInsensitive(SqlBulkCopy bcp, DataTable dataTable, List<string> dbColumns)
    {
        foreach (DataColumn sourceCol in dataTable.Columns)
        {
            // Cari kolom di DB yang namanya sama (abaikan besar/kecil huruf)
            var destColName = dbColumns.FirstOrDefault(c =>
                c.Equals(sourceCol.ColumnName, StringComparison.OrdinalIgnoreCase));

            if (destColName != null)
            {
                // Gunakan nama kolom PERSIS seperti yang ada di Database
                bcp.ColumnMappings.Add(sourceCol.ColumnName, destColName);
            }
        }
    }
}

using System.Data.SqlClient;

namespace Bilreg.Infrastructure.Shared.Helpers;

public static class FullTextSearch
{
    public static void CheckAvailability(SqlConnection conn)
    {
        const string query = "SELECT SERVERPROPERTY('IsFullTextInstalled') AS Result";

        using var command = new SqlCommand(query, conn);
        conn.Open();
        var result = (int)command.ExecuteScalar();
        if (result != 1)
            throw new ArgumentException("FullTextSearch not available");
        conn.Close();
    }
    public static string GenKeywordContain(string keyword)
    {
        return string.Join(" AND ",
            keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                   .Select(x => $"\"{x.Trim()}*\""));
    }
}

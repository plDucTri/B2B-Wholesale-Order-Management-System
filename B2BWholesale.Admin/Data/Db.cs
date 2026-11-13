using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace B2BWholesale.Admin.Data;

/// <summary>Small ADO.NET helper. Every query MUST use parameters (no string concatenation).</summary>
public static class Db
{
    public static readonly string ConnectionString = LoadConnectionString();

    private static string LoadConnectionString()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(path))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("ConnectionString", out var cs) &&
                !string.IsNullOrWhiteSpace(cs.GetString()))
                return cs.GetString()!;
        }
        return "Server=localhost;Database=WholesaleManagementDB;Trusted_Connection=True;TrustServerCertificate=True;";
    }

    public static SqlConnection Open()
    {
        var con = new SqlConnection(ConnectionString);
        con.Open();
        return con;
    }

    public static SqlParameter P(string name, object? value) => new(name, value ?? DBNull.Value);

    public static SqlCommand Cmd(SqlConnection con, SqlTransaction? tx, string sql, params SqlParameter[] ps)
    {
        var cmd = new SqlCommand(sql, con, tx);
        cmd.Parameters.AddRange(ps);
        return cmd;
    }

    public static DataTable Query(string sql, params SqlParameter[] ps)
    {
        using var con = Open();
        using var cmd = Cmd(con, null, sql, ps);
        using var da = new SqlDataAdapter(cmd);
        var dt = new DataTable();
        da.Fill(dt);
        return dt;
    }

    public static int Execute(string sql, params SqlParameter[] ps)
    {
        using var con = Open();
        using var cmd = Cmd(con, null, sql, ps);
        return cmd.ExecuteNonQuery();
    }

    public static object? Scalar(string sql, params SqlParameter[] ps)
    {
        using var con = Open();
        using var cmd = Cmd(con, null, sql, ps);
        var v = cmd.ExecuteScalar();
        return v == DBNull.Value ? null : v;
    }
}

public static class Session
{
    public static int UserId { get; set; }
    public static string UserName { get; set; } = "";
}


using Microsoft.Data.SqlClient;

namespace AgriStream.Web.Services;

public class SqlUserService(IConfiguration config)
{
	private string ConnectionString =>
		config.GetConnectionString("AgriStreamSql")
		?? throw new InvalidOperationException(
			"Set ConnectionStrings:AgriStreamSql.");

	public async Task<(bool ok, string role)> ValidateAsync(
		string username, string password)
	{
		const string sql = @"
            SELECT SystemRole
            FROM dbo.Users
            WHERE Username = @u
              AND IsActive = 1";

		await using var c = new SqlConnection(ConnectionString);
		await c.OpenAsync();

		await using var cmd = new SqlCommand(sql, c);
		cmd.Parameters.AddWithValue("@u", username);

		var result = await cmd.ExecuteScalarAsync();

		if (result == null || result == DBNull.Value)
			return (false, "");

		// Demonstration login credentials
		if (username == "admin" && password == "Admin123")
			return (true, result.ToString()!);

		return (false, "");
	}
}

using System;
using System.Linq;
using Npgsql;

class Program
{
    static void Main()
    {
        string connStr = "Host=localhost;Port=5432;Database=ocpp_db;Username=postgres;Password=password";
        using var conn = new NpgsqlConnection(connStr);
        conn.Open();

        using var cmd = new NpgsqlCommand("SELECT \"Id\", \"Username\", \"PasswordHash\" FROM \"Users\"", conn);
        using var reader = cmd.ExecuteReader();
        
        Console.WriteLine("--- Users Table ---");
        while (reader.Read())
        {
            Console.WriteLine($"ID: {reader.GetInt32(0)}, Username: {reader.GetString(1)}, PwdHash: {reader.GetValue(2)}");
        }
    }
}

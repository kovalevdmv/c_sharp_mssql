using System;
using System.Data.SqlClient;

string connectionString = @"Server=192.168.14.128;Database=test1;User Id=sa;Password=123;";
using (SqlConnection connection = new SqlConnection(connectionString))

{

    connection.Open();
    SqlTransaction transaction = connection.BeginTransaction();

    SqlCommand command = connection.CreateCommand();
    command.Transaction = transaction;

    try
    {
        // выполняем две отдельные команды
        command.CommandText = "INSERT INTO Users (Name, Age) VALUES('Tim', 34)";
        command.ExecuteNonQuery();
        command.CommandText = "INSERT INTO Users (Name, Age) VALUES('Kat', 31)";
        command.ExecuteNonQuery();

        // подтверждаем транзакцию
        transaction.Commit();
        Console.WriteLine("Данные добавлены в базу данных");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        transaction.Rollback();
    }
}
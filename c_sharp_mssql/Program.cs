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
        /*
        command.CommandText = "create table Myfiles (id INTEGER PRIMARY KEY, pach TEXT, hash TEXT);";
        command.ExecuteNonQuery();
        */

        command.CommandText = "delete from Myfiles;";
        command.ExecuteNonQuery();

        int count = 0;
        for (int i = 1; i < 100; i++)
        {
            command.CommandText = @"insert into Myfiles (id,pach,hash) values (@id,@pach,@hash);";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@id", i);
            command.Parameters.AddWithValue("@pach", "123");
            command.Parameters.AddWithValue("@hash", "1234");
            command.ExecuteNonQuery();
            count++;
            if (count == 1000)
            {
                count = 0;
                Console.WriteLine(i);
            }
        }

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
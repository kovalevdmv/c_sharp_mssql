using System.Data.SqlClient;
using System;
using System.IO;
using System.Collections;

namespace c_sharp_mssql
{
    class Program
    {

        public enum LINE
        {
            time = 0,
            event1s = 1,
            SessionID = 10,
            Usr = 11
        }

        static string getProp(string line, string name)
        {
            string[] line_arr = line.Split(",");
            foreach (string i in line_arr)
            {
                if (i.StartsWith(name))
                    return i;
            }

            return "";
        }

        static void Main(string[] args)
        {

            string connectionString = @"Server=192.168.14.128;Database=test1;User Id=sa;Password=123;";

            using (SqlConnection connection = new SqlConnection(connectionString))

            {

                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();
                SqlCommand command = connection.CreateCommand();
                command.Transaction = transaction;

                string[] line_arr;

                try
                {

                    command.CommandText = @"if not EXISTS (
                                            SELECT *
                                            FROM sys.objects
                                            WHERE object_id = OBJECT_ID(N'dbo.MyFiles')
	                                        )
	                                        create table Myfiles (id INTEGER PRIMARY KEY, 
                                            time TEXT, 
                                            event1s TEXT, 
                                            SessionID TEXT,
                                            Usr TEXT,
                                            Regions TEXT,
                                            Locks TEXT,
                                            WaitConnections TEXT,
                                            Context TEXT);";
                    command.ExecuteNonQuery();


                    command.CommandText = "delete from Myfiles;";
                    command.ExecuteNonQuery();

                    try
                    {
                        using (var sr = new StreamReader("1s.log"))
                        {
                            string line;
                            int count = 0;
                            int ind = -1;
                            string line_all = "";
                                
                            string[] buf = new string[2]; // буфер на две строки, чтобы можно было узнать что в следующей строке
                            while ((line = sr.ReadLine()) != null)
                            {
                                ind++;

                                if (ind > 0) { // для последующий, сместить строки в буфере вверх и поместить в него новую строку
                                    if (ind > 1)
                                        buf[0] = buf[1];
                                    buf[1] = line;
                                }
                                else
                                { // если чтрока одна или первая/
                                    buf[0] = line;
                                    if (!sr.EndOfStream)
                                        continue;
                                }

                                line_all += buf[0];
                                if ((buf[1].ToString().Split(",").Length < 2)) continue;

                                line_arr = line_all.Split(",");
                                if (!line_all.Contains("TLOCK")) { line_all = ""; continue; }
                                
                                
                                command.CommandText = @"insert into Myfiles (id,time,event1s,SessionID,Usr,Regions,Locks,WaitConnections,Context) values (@id,@time,@event1s,@SessionID,@Usr,@Regions,@Locks,@WaitConnections,@Context);";
                                command.Parameters.Clear();
                                command.Parameters.AddWithValue("@id", ind);
                                command.Parameters.AddWithValue("@time", line_arr[(int)LINE.time]);
                                command.Parameters.AddWithValue("@event1s", line_arr[(int)LINE.event1s]);
                                command.Parameters.AddWithValue("@SessionID", line_arr[(int)LINE.SessionID]);
                                command.Parameters.AddWithValue("@Usr", line_arr[(int)LINE.Usr]);
                                command.Parameters.AddWithValue("@Regions", getProp(line_all, "Regions"));
                                command.Parameters.AddWithValue("@Locks", getProp(line_all, "Locks"));
                                command.Parameters.AddWithValue("@WaitConnections", getProp(line_all, "WaitConnections"));
                                command.Parameters.AddWithValue("@Context", getProp(line_all,"Context"));
                                line_all = "";
                                command.ExecuteNonQuery();

                              count++;
                                
                                if (count == 1000)
                                {
                                    count = 0;
                                    Console.WriteLine(ind);
                                }

                            }
                        }
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine("The file could not be read:");
                        Console.WriteLine(e.Message);
                    }


                    transaction.Commit();
                    Console.WriteLine("Данные добавлены в базу данных");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    transaction.Rollback();
                }


            }
        }
    }
}

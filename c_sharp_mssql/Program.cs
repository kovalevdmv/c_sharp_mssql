using System.Data.SqlClient;
using System;
using System.IO;
using System.Collections;

namespace c_sharp_mssql
{
    class Program
    {
        public enum TLOCK
        {
            time = 0,
            SessionID = 10,
            Usr = 11,
            Regions = 15,
            Locks = 16,
            WaitConnections = 17,
            Context = 18
        }

        public enum LINE
        {
            time = 0,
            event_ = 1
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

                string[] _line;

                try
                {

                    command.CommandText = @"if not EXISTS (
                                            SELECT *
                                            FROM sys.objects
                                            WHERE object_id = OBJECT_ID(N'dbo.MyFiles')
	                                        )
	                                        create table Myfiles (id INTEGER PRIMARY KEY, 
                                            time TEXT, 
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
                                
                            string[] buf = new string[2];
                            while ((line = sr.ReadLine()) != null)
                            {
                                ind++;

                                if (ind > 0) {
                                    if (ind > 1)
                                        buf[0] = buf[1];
                                    buf[1] = line;
                                }
                                else
                                {
                                    buf[0] = line;
                                    if (!sr.EndOfStream)
                                        continue;
                                }

                                line_all += buf[0];
                                if ((buf[1].ToString().Split(",").Length < 5)) continue;

                                _line = line_all.Split(",");
                                if (line_all[(int)LINE.event_] != "TLOCK") continue;
                                line_all = "";
                                
                                command.CommandText = @"insert into Myfiles (id,time,SessionID,Usr,Regions,Locks,WaitConnections,Context) values (@id,@time,@SessionID,@Usr,@Regions,@Locks,@WaitConnections,@Context);";
                                command.Parameters.Clear();
                                command.Parameters.AddWithValue("@id", ind);
                                command.Parameters.AddWithValue("@time", );
                                command.Parameters.AddWithValue("@SessionID", _line[(int)TLOCK.SessionID]);
                                command.Parameters.AddWithValue("@Usr", _line[(int)TLOCK.Usr]);
                                command.Parameters.AddWithValue("@Regions", _line[(int)TLOCK.Regions]);
                                command.Parameters.AddWithValue("@Locks", _line[(int)TLOCK.Locks]);
                                command.Parameters.AddWithValue("@WaitConnections", _line[(int)TLOCK.WaitConnections]);
                                if (line_all.Contains(WithValue("@Context", _line[(int)TLOCK.Context]);
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

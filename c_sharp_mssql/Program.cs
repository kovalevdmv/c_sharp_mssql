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
            event1s = 1
        }

        // получить из строки лога значение указаного свойства
        static string getProp(string line, string name)
        {

            int sim_equals;
            string[] line_arr = line.Split(",");
            foreach (string i in line_arr)
            {
                if (i.StartsWith(name))
                {
                    sim_equals = i.IndexOf("=");
                    if (sim_equals != 0)
                        return i.Substring(sim_equals + 1);
                    else
                        return i;
                }
            }

            return "";
        }

        static string getDuration(string val)
        {
            return Convert.ToString( Convert.ToInt32(val.Split("-")[1]) / 1000000 );
        }

        static string line_proc(string line)
        {
            
            string newLine = "";
            int open = 0;
            for (int i = 0; i < line.Length; i++)
            {

                if (line[i].Equals('\''))
                    open++;
                if ((line[i].Equals(',')) && (open % 2 != 0)) // если запятая внутри ' ' заменить за дугой символ, чтобы отличать перечисления внутри строк от глобального перечисления
                    newLine += " | ";
                else newLine += line[i];
            }

            return newLine;
        }

        static void Main(string[] args)
        {

            //string s = line_proc("p1='v',p2='v1,v2'");

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
                                            WHERE object_id = OBJECT_ID(N'dbo.MyFiles'))
	                                        create table Myfiles (id INTEGER PRIMARY KEY, 
                                            time varchar(max), 
                                            Duration int,
                                            event1s varchar(max), 
                                            SessionID varchar(max),
                                            connectID  varchar(max),
                                            Usr varchar(max),
                                            Regions varchar(max),
                                            Locks varchar(max),
                                            WaitConnections varchar(max),
                                            Context varchar(max));";
                    command.ExecuteNonQuery();


                    command.CommandText = "delete from Myfiles;";
                    command.ExecuteNonQuery();

                    try
                    {
                        using (var sr = new StreamReader("1s_big.log"))
                        {
                            string line;
                            int count = 0;
                            int ind = -1;
                            string line_all = "";

                            string[] buf = new string[2]; // буфер на две строки, чтобы можно было узнать что в следующей строке
                            int count_line_commnd = 1;
                            while ((line = sr.ReadLine()) != null)
                            {
                                ind++;

                                if (ind > 0)
                                { // для последующий, сместить строки в буфере вверх и поместить в него новую строку
                                    if (ind > 1)
                                        buf[0] = buf[1];
                                    buf[1] = line;
                                }
                                else
                                { // если строка одна или первая/
                                    buf[0] = line;
                                    if (!sr.EndOfStream)
                                        continue;
                                }

                                line_all += buf[0]; // из буфера всегда брать первую строку (вторая только для проверки)
                                if ((buf[1].ToString().Split(",").Length < 2)) continue; // если след. строка не начало строки лога, а продолжение, пропустить (она соберется в общую сторку)

                                line_all = line_proc(line_all);
                                line_arr = line_all.Split(",");

                                if (!line_all.Contains("TLOCK")) { line_all = ""; continue; } // пропустить все кроме события TLOCK


                                command.CommandText += String.Format("insert into Myfiles (id,time,Duration,event1s,SessionID,connectID,Usr,Regions,Locks,WaitConnections,Context) " +
                                    "values (@id{0},@time{0},@Duration{0},@event1s{0},@SessionID{0},@connectID{0},@Usr{0},@Regions{0},@Locks{0},@WaitConnections{0},@Context{0});", count_line_commnd);

                                command.Parameters.AddWithValue(String.Format("@id{0}", count_line_commnd), ind);
                                command.Parameters.AddWithValue(String.Format("@time{0}", count_line_commnd), line_arr[(int)LINE.time]);
                                command.Parameters.AddWithValue(String.Format("@Duration{0}", count_line_commnd), getDuration(line_arr[(int)LINE.time]));
                                command.Parameters.AddWithValue(String.Format("@event1s{0}", count_line_commnd), line_arr[(int)LINE.event1s]);
                                command.Parameters.AddWithValue(String.Format("@SessionID{0}", count_line_commnd), getProp(line_all, "SessionID"));
                                command.Parameters.AddWithValue(String.Format("@connectID{0}", count_line_commnd), getProp(line_all, "t:connectID"));
                                command.Parameters.AddWithValue(String.Format("@Usr{0}", count_line_commnd), getProp(line_all, "Usr"));
                                command.Parameters.AddWithValue(String.Format("@Regions{0}", count_line_commnd), getProp(line_all, "Regions"));
                                command.Parameters.AddWithValue(String.Format("@Locks{0}", count_line_commnd), getProp(line_all, "Locks"));
                                command.Parameters.AddWithValue(String.Format("@WaitConnections{0}", count_line_commnd), getProp(line_all, "WaitConnections"));
                                command.Parameters.AddWithValue(String.Format("@Context{0}", count_line_commnd), getProp(line_all, "Context"));
                                if ((count_line_commnd == 50) || sr.EndOfStream) // выполнять запросы пакетами по 50
                                {
                                    command.ExecuteNonQuery();
                                    command.Parameters.Clear();
                                    command.CommandText = "";
                                    count_line_commnd = 1;
                                }
                                count_line_commnd++;
                                line_all = "";

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

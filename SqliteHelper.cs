using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace wxreader
{
    internal class SqliteHelper
    {
        private static string _connectionString = GloableVars.connecsString;

        public class SqliteConnection
        {
            public string connectingName { get; set; }
            public SQLiteConnection connection { get; set; }
        }

        // 该属性用于获取或设置数据库连接字符串。
        public static string ConnectionString
        {
            get
            {
                return _connectionString;
            }
            set
            {
                _connectionString = value;
            }
        }
           
        // 该方法用于获取数据库连接。
        public static SQLiteConnection GetConnection(string connectionString)
        {
            SQLiteConnection connection = new SQLiteConnection(connectionString);
            connection.Open();
            return connection;
        }

        // 该方法用于关闭数据库连接。
        public static void CloseConnection(SQLiteConnection connection)
        {
            if (connection!= null)
            {
                connection.Close();
            }
        }

        //清理数据库连接解除文件占用
        public static void ClearConnection()
        {
            SQLiteConnection.ClearAllPools();
        }

        // 该方法用于执行指定的SQL语句。
        public static void ExecuteNonQuery(string sql, SQLiteConnection connection)
        {
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                connection.Close();
                throw ex;
            }
        }

        // 该方法用于执行指定的SQL语句并返回执行结果的IDataReader。
        public static SQLiteDataReader ExecuteReader(string sql, SQLiteConnection connection)
        {
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            return command.ExecuteReader();
        }

        // 该方法用于执行指定的SQL语句并返回执行结果的第一行第一列的值。
        public static object ExecuteScalar(string sql, SQLiteConnection connection)
        {
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            return command.ExecuteScalar();
        }

        // 该方法用于创建指定名称的表，并指定列名和列类型。
        // 参数：
        // - tableName: 表名
        // - columnNames: 列名数组
        // - columnTypes: 列类型数组
        public static void CreateTable(string tableName, string[] columnNames, string[] columnTypes)
        {
            string sql = "CREATE TABLE IF NOT EXISTS " + tableName + " (";
            for (int i = 0; i < columnNames.Length; i++)
            {
                sql += columnNames[i] + " " + columnTypes[i];
                if (i < columnNames.Length - 1)
                {
                    sql += ", ";
                }
            }
            sql += ")";
            ExecuteNonQuery(sql, GetConnection(ConnectionString));
        }

        // 该方法用于插入数据到指定表中。
        // 参数：
        // - tableName: 表名
        // - columnNames: 列名数组
        // - values: 要插入的值数组
        public static void Insert(string tableName, string[] columnNames, object[] values)
        {
            string sql = "INSERT INTO " + tableName + " (";
            for (int i = 0; i < columnNames.Length; i++)
            {
                sql += columnNames[i];
                if (i < columnNames.Length - 1)
                {
                    sql += ", ";
                }
            }
            sql += ") VALUES (";
            for (int i = 0; i < values.Length; i++)
            {
                sql += "@" + i;
                if (i < values.Length - 1)
                {
                    sql += ", ";
                }
            }
            sql += ")";
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            for (int i = 0; i < values.Length; i++)
            {
                command.Parameters.AddWithValue("@" + i, values[i]);
            }
            command.ExecuteNonQuery();
            CloseConnection(connection);
        }

        // 该方法用于更新指定表中的数据。
        // 参数：
        // - tableName: 表名
        // - columnNames: 要更新的列名数组
        // - values: 要更新的值数组
        // - whereClause: 查询条件
        public static void Update(string tableName, string[] columnNames, object[] values, string whereClause)
        {
            string sql = "UPDATE " + tableName + " SET ";
            for (int i = 0; i < columnNames.Length; i++)
            {
                sql += columnNames[i] + " = @" + i;
                if (i < columnNames.Length - 1)
                {
                    sql += ", ";
                }
            }
            sql += " WHERE " + whereClause;
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            for (int i = 0; i < values.Length; i++)
            {
                command.Parameters.AddWithValue("@" + i, values[i]);
            }
            command.ExecuteNonQuery();
            CloseConnection(connection);
        }

        public static void Delete(string tableName, string whereClause)
        {
            string sql = "DELETE FROM " + tableName + " WHERE " + whereClause;
            ExecuteNonQuery(sql, GetConnection(ConnectionString));
        }

        // 该方法用于从指定的数据库表中选择数据，并返回一个包含结果的列表。
        // 参数：
        // - tableName: 表名
        // - columnNames: 要选择的列名数组
        // - whereClause: 查询条件
        public static List<Dictionary<string, object>> Select(string tableName, string[] columnNames, string whereClause)
        {
            string sql = "SELECT ";
            for (int i = 0; i < columnNames.Length; i++)
            {
                sql += columnNames[i];
                if (i < columnNames.Length - 1)
                {
                    sql += ", ";
                }
            }
            sql += " FROM " + tableName + " WHERE " + whereClause;
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
            while (reader.Read())
            {
                Dictionary<string, object> row = new Dictionary<string, object>();
                for (int i = 0; i < columnNames.Length; i++)
                {
                    row.Add(columnNames[i], reader[i]);
                }
                result.Add(row);
            }
            CloseConnection(connection);
            return result;
        }


        public static void DropTable(string tableName)
        {
            string sql = "DROP TABLE IF EXISTS " + tableName;
            ExecuteNonQuery(sql, GetConnection(ConnectionString));
        }



        // 创建索引的方法，接受表名、索引名和列名数组作为参数，生成相应的SQL语句并执行。
        public static void CreateIndex(string tableName, string indexName, string[] columnNames)
        {
            string sql = "CREATE INDEX IF NOT EXISTS " + indexName + " ON " + tableName + " (";
            for (int i = 0; i < columnNames.Length; i++)
            {
                sql += columnNames[i];
                if (i < columnNames.Length - 1)
                {
                    sql += ", ";
                }
            }
            sql += ")";
            ExecuteNonQuery(sql, GetConnection(ConnectionString));
        }


        // 删除索引的方法，接受表名和索引名作为参数，生成相应的SQL语句并执行。
        public static void DropIndex(string tableName, string indexName)
        {
            string sql = "DROP INDEX IF EXISTS " + indexName;
            ExecuteNonQuery(sql, GetConnection(ConnectionString)); // 执行SQL语句
        }

        // 该方法用于清空指定表中的所有数据。
        public static void TruncateTable(string tableName)
        {
            string sql = "DELETE FROM " + tableName;
            ExecuteNonQuery(sql, GetConnection(ConnectionString));
        }

        // 该方法用于检查指定表是否存在。
        public static bool CheckTableExists(string tableName)
        {
            string sql = "SELECT name FROM sqlite_master WHERE type='table' AND name='" + tableName + "'";
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            bool exists = reader.HasRows;
            CloseConnection(connection);
            return exists;
        }

        /// <summary>
        /// 该方法用于检查指定表是否存在。
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static bool CheckTableExists(SQLiteConnection conn, string tableName)
        {
            string sql = "SELECT name FROM sqlite_master WHERE type='table' AND name='" + tableName + "'";
            //SQLiteConnection connection = GetConnection(connectionString);
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            SQLiteDataReader reader = command.ExecuteReader();
            bool exists = reader.HasRows;
            //CloseConnection(connection);
            return exists;
        }

        public static bool NewCheckTableExists(SQLiteConnection conn, string tableName)
        {
            string sql = $"SELECT name FROM {tableName.Substring(0, tableName.IndexOf("."))}.sqlite_master WHERE type='table' AND name='{tableName.Substring(tableName.IndexOf(".") + 1)}'";
            //SQLiteConnection connection = GetConnection(connectionString);
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            SQLiteDataReader reader = command.ExecuteReader();
            bool exists = reader.HasRows;
            //CloseConnection(connection);
            return exists;
        }

        // 该方法用于检查指定索引是否存在。
        public static bool CheckIndexExists(string tableName, string indexName)
        {
            string sql = "SELECT name FROM sqlite_master WHERE type='index' AND name='" + indexName + "'";
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            bool exists = reader.HasRows;
            CloseConnection(connection);
            return exists;
        }

        // 该方法用于获取指定表的列名数组。
        public static string[] GetColumnNames(string tableName)
        {
            string sql = "PRAGMA table_info(" + tableName + ")";
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            List<string> columnNames = new List<string>();
            while (reader.Read())
            {
                columnNames.Add(reader["name"].ToString());
            }
            CloseConnection(connection);
            return columnNames.ToArray(); // 返回列名数组
        }

        // 该方法用于获取指定表的索引名数组。
        public static string[] GetIndexNames(string tableName)
        {
            string sql = "SELECT name FROM sqlite_master WHERE type='index' AND tbl_name='" + tableName + "'";
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            List<string> indexNames = new List<string>();
            while (reader.Read())
            {
                indexNames.Add(reader["name"].ToString());
            }
            CloseConnection(connection);
            return indexNames.ToArray(); // 返回索引名数组
        }

        // 该方法用于获取指定表的主键名。
        public static string GetPrimaryKey(string tableName)
        {
            string sql = "PRAGMA table_info(" + tableName + ")";
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            string primaryKey = "";
            while (reader.Read())
            {
                if (reader["pk"].ToString() == "1")
                {
                    primaryKey = reader["name"].ToString();
                    break;
                }
            }
            CloseConnection(connection);
            return primaryKey; // 返回主键名
        }

        // 该方法用于获取指定表的行数。
        public static int GetRowCount(string tableName)
        {
            string sql = "SELECT COUNT(*) FROM " + tableName;
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            int count = (int)command.ExecuteScalar();
            CloseConnection(connection);
            return count; // 返回行数
        }

        // 该方法用于获取指定表的列数。
        public static int GetColumnCount(string tableName)
        {
            string sql = "PRAGMA table_info(" + tableName + ")";
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            int count = 0;
            while (reader.Read())
            {
                count++;
            }
            CloseConnection(connection);
            return count; // 返回列数
        }

        // 该方法用于获取指定表的表结构。
        public static string GetTableStructure(string tableName)
        {
            string sql = "SELECT sql FROM sqlite_master WHERE type='table' AND name='" + tableName + "'";
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            string structure = command.ExecuteScalar().ToString();
            CloseConnection(connection);
            return structure; // 返回表结构
        }

        // 该方法用于获取指定索引的结构。
        public static string GetIndexStructure(string tableName, string indexName)
        {
            string sql = "SELECT sql FROM sqlite_master WHERE type='index' AND name='" + indexName + "' AND tbl_name='" + tableName + "'";
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            string structure = command.ExecuteScalar().ToString();
            CloseConnection(connection);
            return structure; // 返回索引结构
        }

        // 该方法用于获取指定表的全部数据。
        public static List<Dictionary<string, object>> GetTableData(string tableName)
        {
            string sql = "SELECT * FROM " + tableName;
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            List<Dictionary<string, object>> data = new List<Dictionary<string, object>>();
            while (reader.Read())
            {
                Dictionary<string, object> row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row.Add(reader.GetName(i), reader[i]);
                }
                data.Add(row);
            }
            CloseConnection(connection);
            return data; // 返回表数据
        }

        // 该方法用于获取指定表的指定列的数据。
        public static List<object> GetColumnData(string tableName, string columnName)
        {
            string sql = "SELECT " + columnName + " FROM " + tableName;
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            List<object> data = new List<object>();
            while (reader.Read())
            {
                data.Add(reader[0]);
            }
            CloseConnection(connection);
            return data; // 返回指定列的数据
        }

        // 该方法用于获取指定表的指定行的数据。
        public static Dictionary<string, object> GetRowData(string tableName, int rowNumber)
        {
            string sql = "SELECT * FROM " + tableName + " LIMIT 1 OFFSET " + (rowNumber - 1);
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            Dictionary<string, object> data = new Dictionary<string, object>();
            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    data.Add(reader.GetName(i), reader[i]);
                }
            }
            CloseConnection(connection);
            return data; // 返回指定行的数据
        }

        // 该方法用于获取指定表的指定范围的数据。
        public static List<Dictionary<string, object>> GetRangeData(string tableName, int startRow, int endRow)
        {
            string sql = "SELECT * FROM " + tableName + " LIMIT " + (endRow - startRow + 1) + " OFFSET " + (startRow - 1);
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            List<Dictionary<string, object>> data = new List<Dictionary<string, object>>();
            while (reader.Read())
            {
                Dictionary<string, object> row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row.Add(reader.GetName(i), reader[i]);
                }
                data.Add(row);
            }
            CloseConnection(connection);
            return data; // 返回指定范围的数据
        }

        // 该方法用于获取指定表的指定列的最大值。
        public static object GetMax(string tableName, string columnName)
        {
            string sql = "SELECT MAX(" + columnName + ") FROM " + tableName;
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            object max = command.ExecuteScalar();
            CloseConnection(connection);
            return max; // 返回指定列的最大值
        }

        // 该方法用于获取指定表的指定列的最小值。
        public static object GetMin(string tableName, string columnName)
        {
            string sql = "SELECT MIN(" + columnName + ") FROM " + tableName;
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            object min = command.ExecuteScalar();
            CloseConnection(connection);
            return min; // 返回指定列的最小值
        }

        // 该方法用于获取指定表的指定列的平均值。
        public static object GetAvg(string tableName, string columnName)
        {
            string sql = "SELECT AVG(" + columnName + ") FROM " + tableName;
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            object avg = command.ExecuteScalar();
            CloseConnection(connection);
            return avg; // 返回指定列的平均值
        }

        // 该方法用于获取指定表的指定列的总和。
        public static object GetSum(string tableName, string columnName)
        {
            string sql = "SELECT SUM(" + columnName + ") FROM " + tableName;
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            object sum = command.ExecuteScalar();
            CloseConnection(connection);
            return sum; // 返回指定列的总和
        }

        // 该方法用于获取指定表的指定列的标准差。
        public static object GetStdDev(string tableName, string columnName)
        {
            string sql = "SELECT STDDEV(" + columnName + ") FROM " + tableName;
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            object stdDev = command.ExecuteScalar();
            CloseConnection(connection);
            return stdDev; // 返回指定列的标准差
        }

        // 该方法用于获取指定表的指定列的方差。
        public static object GetVariance(string tableName, string columnName)
        {
            string sql = "SELECT VARIANCE(" + columnName + ") FROM " + tableName;
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            object variance = command.ExecuteScalar();
            CloseConnection(connection);
            return variance; // 返回指定列的方差
        }

        // 该方法用于获取指定表的指定列的模式。
        public static object GetMode(string tableName, string columnName)
        {
            string sql = "SELECT " + columnName + ", COUNT(*) AS count FROM " + tableName + " GROUP BY " + columnName + " ORDER BY count DESC LIMIT 1";
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            object mode = command.ExecuteScalar();
            CloseConnection(connection);
            return mode; // 返回指定列的模式
        }

        // 该方法用于获取指定表的指定列的唯一值数量。
        public static int GetUniqueCount(string tableName, string columnName)
        {
            string sql = "SELECT COUNT(DISTINCT " + columnName + ") FROM " + tableName;
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            int count = (int)command.ExecuteScalar();
            CloseConnection(connection);
            return count; // 返回指定列的唯一值数量
        }

        // 该方法用于获取指定表的指定列的空值数量。
        public static int GetNullCount(string tableName, string columnName)
        {
            string sql = "SELECT COUNT(*) FROM " + tableName + " WHERE " + columnName + " IS NULL";
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            int count = (int)command.ExecuteScalar();
            CloseConnection(connection);
            return count; // 返回指定列的空值数量
        }

        // 该方法用于获取指定表的指定列的重复值数量。
        public static int GetDuplicateCount(string tableName, string columnName)
        {
            string sql = "SELECT COUNT(*) FROM " + tableName + " GROUP BY " + columnName + " HAVING COUNT(*) > 1";
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            int count = (int)command.ExecuteScalar();
            CloseConnection(connection);
            return count; // 返回指定列的重复值数量
        }

        // 该方法用于获取指定表的指定列的分布情况。
        public static List<object> GetDistribution(string tableName, string columnName)
        {
            string sql = "SELECT " + columnName + ", COUNT(*) AS count FROM " + tableName + " GROUP BY " + columnName + " ORDER BY count DESC";
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            List<object> distribution = new List<object>();
            while (reader.Read())
            {
                distribution.Add(new { Value = reader[0], Count = reader[1] });
            }
            CloseConnection(connection);
            return distribution; // 返回指定列的分布情况
        }

        // 该方法用于获取指定表的指定列的最小值和最大值。
        public static object[] GetMinMax(string tableName, string columnName)
        {
            string sql = "SELECT MIN(" + columnName + "), MAX(" + columnName + ") FROM " + tableName;
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            object[] minMax = new object[2];
            while (reader.Read())
            {
                minMax[0] = reader[0];
                minMax[1] = reader[1];
            }
            CloseConnection(connection);
            return minMax; // 返回指定列的最小值和最大值
        }

        // 该方法用于获取指定表的指定列的分位数。
        public static object[] GetQuantile(string tableName, string columnName, double[] quantiles)
        {
            string sql = "SELECT " + columnName + ", COUNT(*) AS count FROM " + tableName + " GROUP BY " + columnName + " ORDER BY " + columnName;
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            List<object> quantileValues = new List<object>();
            while (reader.Read())
            {
                double value = reader.GetDouble(0);
                int count = reader.GetInt32(1);
                for (int i = 0; i < quantiles.Length; i++)
                {
                    if (count / (double)reader.FieldCount <= quantiles[i])
                    {
                        quantileValues.Add(value);
                        break;
                    }
                }
            }
            CloseConnection(connection);
            return quantileValues.ToArray(); // 返回指定列的分位数
        }

        // 该方法用于获取指定表的指定列的直方图。
        public static List<object> GetHistogram(string tableName, string columnName, int bucketCount)
        {
            string sql = "SELECT " + columnName + ", COUNT(*) AS count FROM " + tableName + " GROUP BY " + columnName + " ORDER BY " + columnName;
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            List<object> histogram = new List<object>();
            double min = double.MaxValue;
            double max = double.MinValue;
            while (reader.Read())
            {
                double value = reader.GetDouble(0);
                if (value < min)
                {
                    min = value;
                }
                if (value > max)
                {
                    max = value;
                }
            }
            double bucketSize = (max - min) / bucketCount;
            for (int i = 0; i < bucketCount; i++)
            {
                double lowerBound = min + i * bucketSize;
                double upperBound = min + (i + 1) * bucketSize;
                string bucketSql = "SELECT COUNT(*) AS count FROM " + tableName + " WHERE " + columnName + " BETWEEN " + lowerBound + " AND " + upperBound;
                SQLiteCommand bucketCommand = new SQLiteCommand(bucketSql, connection);
                int count = (int)bucketCommand.ExecuteScalar();
                histogram.Add(new { LowerBound = lowerBound, UpperBound = upperBound, Count = count });
            }
            CloseConnection(connection);
            return histogram; // 返回指定列的直方图
        }

        // 该方法用于获取指定表的指定列的频率分布。
        public static List<object> GetFrequencyDistribution(string tableName, string columnName)
        {
            string sql = "SELECT " + columnName + ", COUNT(*) AS count FROM " + tableName + " GROUP BY " + columnName + " ORDER BY count DESC";
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            List<object> frequencyDistribution = new List<object>();
            while (reader.Read())
            {
                frequencyDistribution.Add(new { Value = reader[0], Count = reader[1] });
            }
            CloseConnection(connection);
            return frequencyDistribution; // 返回指定列的频率分布
        }

        // 该方法用于获取指定表的指定列的箱线图。
        public static List<object> GetBoxplot(string tableName, string columnName)
        {
            string sql = "SELECT " + columnName + ", COUNT(*) AS count FROM " + tableName + " GROUP BY " + columnName + " ORDER BY " + columnName;
            SQLiteConnection connection = GetConnection(ConnectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            SQLiteDataReader reader = command.ExecuteReader();
            List<object> boxplot = new List<object>();
            double min = double.MaxValue;
            double max = double.MinValue;
            double lowerQuartile = 0;
            double upperQuartile = 0;
            double lowerWhisker = 0;
            double upperWhisker = 0;
            while (reader.Read())
            {
                double value = reader.GetDouble(0);
                if (value < min)
                {
                    min = value;
                }
                if (value > max)
                {
                    max = value;
                }
            }
            double[] quantiles = { 0.25, 0.5, 0.75 };
            object[] quantile = GetQuantile(tableName, columnName, quantiles);
            lowerQuartile = (double)quantile[0];
            upperQuartile = (double)quantile[2];
            double iqr = upperQuartile - lowerQuartile;
            double lowerFence = lowerQuartile - 1.5 * iqr;
            double upperFence = upperQuartile + 1.5 * iqr;
            double[] whiskerQuantiles = { 0.0, 0.25, 0.5, 0.75, 1.0 };
            object[] whiskerQuantile = GetQuantile(tableName, columnName, whiskerQuantiles);
            lowerWhisker = (double)whiskerQuantile[1];
            upperWhisker = (double)whiskerQuantile[3];
            boxplot.Add(new { LowerWhisker = lowerWhisker, LowerFence = lowerFence, LowerQuartile = lowerQuartile, Median = (double)quantile[1], UpperQuartile = upperQuartile, UpperFence = upperFence, UpperWhisker = upperWhisker });
            CloseConnection(connection);
            return boxplot; // 返回指定列的箱线图
        }

        internal static void InitSqlite()
        {
            GloableVars.connecsString = $"Data Source={GloableVars.filePath}\\de_MicroMsg.db;Version=3;";
            _connectionString = GloableVars.connecsString;
        }
    }
}

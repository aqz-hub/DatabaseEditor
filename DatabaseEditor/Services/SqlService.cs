using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DatabaseEditor.Models;
using System.Linq;
using System.Collections.ObjectModel;
using System.Data;

namespace DatabaseEditor.Services
{
    internal class SqlService
    {
        private SqlConnection connection;
        private string connectionString = $"Server=(localdb)\\mssqllocaldb;Database=";
        public string CurrentDatabase { get; set; }

        private static SqlService instance;
        private SqlService() { }

        public static async Task<SqlService> GetInstance()
        {
            if (instance == null)
            {
                instance = new SqlService();
            }
            return instance;
        }

        public async Task OpenConnection()
        {
            connection = new SqlConnection(connectionString + CurrentDatabase + ";");
            try
            {
                await connection.OpenAsync();
            }
            catch (Exception ex)
            {
                MainWindow.Log.Add("Не удалось открыть подключение к базе данных! Ошибка: " + ex.Message);
            }
        }

        public async Task CloseConnection()
        {
            if (connection != null && connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }

        public async Task<ObservableCollection<string>> GetDatabases()
        {
            var list = new ObservableCollection<string>();

            string connectionString = "Server=(localdb)\\mssqllocaldb;";

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                var command = new SqlCommand("SELECT name FROM sys.databases", connection);
                SqlDataReader reader;
                try
                {
                    reader = await command.ExecuteReaderAsync();
                }
                catch(Exception ex)
                {
                    MainWindow.Log.Add("Не удалось получить базы данных! Ошибка: " + ex.Message);
                    return null;
                }

                if (!reader.HasRows)
                {
                    MainWindow.Log.Add("Не верный запрос! Возвращено NULL.");
                    return null;
                }

                while (await reader.ReadAsync())
                {
                    list.Add(reader[0].ToString());
                }
            }
            return list;
        }

        public async Task<bool> DeleteTable(string tableName)
        {
            if(connection.State != ConnectionState.Open)
            {
                MainWindow.Log.Add("Отсутствует подключение к базе данных!");
                return false;
            }

            var command = new SqlCommand($"DROP TABLE {tableName}", connection);
            try
            {
                await command.ExecuteNonQueryAsync();
                MainWindow.Log.Add("Таблица успешно удалена!");
                return true;
            }
            catch (Exception ex)
            {
                MainWindow.Log.Add("Ошибка при выполнении запроса! " + ex.Message);
                return false;
            }
        }

        public async Task<bool> RenameTable(string oldName, string newName)
        {
            if (connection.State != ConnectionState.Open)
            {
                MainWindow.Log.Add("Отсутствует подключение к базе данных!");
                return false;
            }

            var command = new SqlCommand($"EXEC sp_rename '{oldName}', '{newName}'", connection);
            try
            {
                await command.ExecuteNonQueryAsync();
                MainWindow.Log.Add("Наименование таблицы успешно изменено!");
                return true;
            }
            catch (Exception ex)
            {
                MainWindow.Log.Add("Ошибка при выполнении запроса! " + ex.Message);
                return false;
            }
        }

        public async Task<List<Field>?> GetTableFields(string tableName)
        {
            var list = new List<Field>();
            if (connection.State != ConnectionState.Open)
            {
                MainWindow.Log.Add("Отсутствует подключение к базе данных!");
                return null;
            }

            var command = new SqlCommand($"SELECT COLUMN_NAME, DATA_TYPE FROM {CurrentDatabase}.INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='{tableName}'", connection);
            SqlDataReader reader;
            try
            {
                reader = await command.ExecuteReaderAsync();
            }
            catch (Exception ex)
            {
                MainWindow.Log.Add("Ошибка при выполнении запроса! " + ex.Message);
                return null;
            }

            if (!reader.HasRows)
            {
                MainWindow.Log.Add("Не верный запрос! Возвращено NULL.");
                return null;
            }

            while (await reader.ReadAsync())
            {
                var fieldName = reader.GetValue(0);
                var fieldType = reader.GetValue(1);

                list.Add(new Field() { Name = fieldName.ToString(), Type = Field.ParseType(fieldType.ToString()) });
            }
            await reader.CloseAsync();

            command = new SqlCommand("SELECT KU.table_name as TABLENAME ,column_name as PRIMARYKEYCOLUMN " +
            $"FROM {CurrentDatabase}.INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC " +
            "INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KU ON TC.CONSTRAINT_TYPE = 'PRIMARY KEY' AND " +
            $"TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME AND KU.table_name='{tableName}'", connection);
            try
            {
                reader = await command.ExecuteReaderAsync();
            }
            catch (Exception ex)
            {
                MainWindow.Log.Add("Ошибка при выполнении запроса! " + ex.Message);
                return null;
            }

            if(!reader.HasRows)
            {
                MainWindow.Log.Add("Не верный запрос! Возвращено NULL.");
                return null;
            }
            await reader.ReadAsync();
            var primaryKeyName = reader.GetValue(1);
            var field = list.Single(x => x.Name == primaryKeyName.ToString());
            field.PrimaryKey = true;

            await reader.CloseAsync();
            MainWindow.Log.Add("Поля таблицы успешно получены!");
            return list;
        }

        public async Task<ObservableCollection<string>?> GetTables()
        {
            var list = new ObservableCollection<string>();
            if (connection.State != ConnectionState.Open)
            {
                MainWindow.Log.Add("Отсутствует подключение к базе данных!");
                return null;
            }
            var command = new SqlCommand($"SELECT TABLE_NAME FROM {CurrentDatabase}.INFORMATION_SCHEMA.TABLES", connection);
            SqlDataReader reader;
            try
            {
                reader = await command.ExecuteReaderAsync();
            }
            catch (Exception ex)
            {
                MainWindow.Log.Add("Ошибка при выполнении запроса! " + ex.Message);
                return null;
            }

            if (!reader.HasRows)
            {
                MainWindow.Log.Add("Не верный запрос! Возвращено NULL.");
                return null;
            }

            while (await reader.ReadAsync())
            {
                var tableName = reader.GetValue(0);
                if (tableName != null)
                    list.Add(tableName.ToString());
            }

            MainWindow.Log.Add("Таблицы успешно получены!");
            await reader.CloseAsync();
            return list;
        }

        private List<string> BuildChangeTypeQuery(string tableName, List<SourceField> sourceFields, List<Field> changedFields)
        {
            List<string> query = new List<string>();
            for (int i = 0; i < changedFields.Count; i++)
            {
                if (sourceFields[i].Type != changedFields[i].Type)
                    query.Add($"ALTER TABLE {tableName} ALTER COLUMN {changedFields[i].Name} {Field.ParseType(changedFields[i].Type)}");
            }
            return query;
        }
        private List<string> BuildRenameQuery(string tableName, List<SourceField> sourceFields, List<Field> changedFields)
        {
            List<string> query = new List<string>();
            for (int i = 0; i < sourceFields.Count; i++)
            {
                if (sourceFields[i].Name != changedFields[i].Name)
                    query.Add($"EXEC sp_rename '{tableName}.{sourceFields[i].Name}', '{changedFields[i].Name}', 'COLUMN';");
            }
            return query;
        }
        private string BuildChangePrimaryKeyQuery(string tableName, string oldPrimaryKey, Field newPrimaryKey)
        {
            string query = $"ALTER TABLE {tableName} ALTER COLUMN {newPrimaryKey.Name} {Field.ParseType(newPrimaryKey.Type)} NOT NULL;";
            query += $"ALTER TABLE {tableName} DROP CONSTRAINT {oldPrimaryKey};";
            query += $"ALTER TABLE {tableName} ADD CONSTRAINT PK_{tableName} PRIMARY KEY CLUSTERED ({newPrimaryKey.Name});";
            return query;
        }

        private bool PrimaryKeyChanged(SourceField oldPrimaryKey, Field newPrimaryKey)
        {
            if (oldPrimaryKey.Name == newPrimaryKey.Name)
                return false;
            else
                return true;
        }

        private async Task<string> GetOldPrimaryKey(string tableName)
        {
            string defaultPrimaryKey = $"PK_{tableName}";
            string query = $"SELECT name FROM sys.key_constraints WHERE type = 'PK' AND OBJECT_NAME(parent_object_id) = N'{tableName}'";
            if (connection.State != ConnectionState.Open)
            {
                return defaultPrimaryKey;
            }

            var command = new SqlCommand(query, connection);
            SqlDataReader reader;
            try
            {
                reader = await command.ExecuteReaderAsync();
            }
            catch (Exception ex)
            {
                MainWindow.Log.Add($"Не удалось получить название первичного ключа для таблицы {tableName}! Ошибка: " + ex.Message);
                return defaultPrimaryKey;
            }
            if (!reader.HasRows)
            {
                return defaultPrimaryKey;
            }
            await reader.ReadAsync();

            var keyName = reader.GetValue(0);
            await reader.CloseAsync();
            if (keyName != null)
                return keyName.ToString();

            return defaultPrimaryKey;
        }

        public async Task<bool> SaveFields(string tableName, List<SourceField> sourceFields, List<Field> changedFields)
        {
            if (connection.State != ConnectionState.Open)
            {
                MainWindow.Log.Add("Отсутствует подключение к базе данных!");
                return false;
            }

            SqlCommand command;
            var renameQuery = BuildRenameQuery(tableName, sourceFields, changedFields);
            foreach (var query in renameQuery)
            {
                command = new SqlCommand(query, connection);
                try
                {
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    MainWindow.Log.Add("Ошибка при выполнении запроса! " + ex.Message);
                    return false;
                }
            }

            var changeTypeQuery = BuildChangeTypeQuery(tableName, sourceFields, changedFields);
            foreach (var query in changeTypeQuery)
            {
                command = new SqlCommand(query, connection);
                try
                {
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    MainWindow.Log.Add("Ошибка при выполнении запроса! " + ex.Message);
                    return false;
                }
            }

            if(PrimaryKeyChanged(sourceFields.Single(x => x.PrimaryKey), changedFields.Single(x => x.PrimaryKey)))
            {
                var oldPrimaryKey = await GetOldPrimaryKey(tableName);

                var changePrimaryKeyQuery = BuildChangePrimaryKeyQuery(tableName, oldPrimaryKey.ToString(), changedFields.Single(x => x.PrimaryKey));
                command = new SqlCommand(changePrimaryKeyQuery, connection);
                try
                {
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    MainWindow.Log.Add("Ошибка при выполнении запроса! " + ex.Message);
                    return false;
                }
            }   

            MainWindow.Log.Add("Сохранения успешно применены!");
            return true;
        }

        private string BuildCreateTableQuery(string tableName, List<Field> fields)
        {
            string fieldsQuery = "";
            foreach (var field in fields)
            {
                fieldsQuery += $"{field.Name} {Field.ParseType(field.Type)} NOT NULL,";
            }
            var primaryKey = fields.Single(x => x.PrimaryKey);
            string query = $"CREATE TABLE {tableName} ({fieldsQuery.TrimEnd(',')});";
            query += $"ALTER TABLE {tableName} ALTER COLUMN {primaryKey.Name} {Field.ParseType(primaryKey.Type)} NOT NULL;";
            query += $"ALTER TABLE {tableName} ADD CONSTRAINT PK_{tableName} PRIMARY KEY CLUSTERED ({primaryKey.Name});";
            return query;
        }

        public async Task<bool> CreateTable(string tableName, List<Field> fields)
        {
            if (connection.State == ConnectionState.Open)
            {
                MainWindow.Log.Add("Отсутствует подключение к базе данных!");
                return false;
            }

            var query = BuildCreateTableQuery(tableName, fields);
            var command = new SqlCommand(query, connection);
            try
            {
                var executer = await command.ExecuteNonQueryAsync();
                MainWindow.Log.Add("Таблица успешно создана!");
                return true;
            }
            catch (Exception ex)
            {
                MainWindow.Log.Add("Ошибка при выполнении запроса! " + ex.Message);
                return false;
            }
        }
    }
}

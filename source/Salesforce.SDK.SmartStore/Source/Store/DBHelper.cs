/*
 * Copyright (c) 2014, salesforce.com, inc.
 * All rights reserved.
 * Redistribution and use of this software in source and binary forms, with or
 * without modification, are permitted provided that the following conditions
 * are met:
 * - Redistributions of source code must retain the above copyright notice, this
 * list of conditions and the following disclaimer.
 * - Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 * - Neither the name of salesforce.com, inc. nor the names of its contributors
 * may be used to endorse or promote products derived from this software without
 * specific prior written permission of salesforce.com, inc.
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SQLite;
using Salesforce.SDK.SmartStore.Source.SQLiteExtensions;

namespace Salesforce.SDK.SmartStore.Store
{
    public sealed class DBHelper : IDisposable
    {
        #region Statics

        // Some queries
        private const string CountSelect = "SELECT count(*) FROM {0} {1}";
        private const string SeqSelect = "SELECT seq FROM SQLITE_SEQUENCE WHERE name = ?";
        private const string LimitSelect = "SELECT * FROM ({0}) LIMIT {1}";
        private const string QueryStatement = "SELECT {0} FROM {1} {2} {3}{4}{5}";
        private const string InsertStatement = "INSERT INTO {0} ({1}) VALUES ({2});";
        private const string UpdateStatement = "UPDATE {0} SET {1} WHERE {2}";
        private const string DeleteStatement = "DELETE FROM {0} WHERE {1}";
        private const string DeleteAllStatement = "DELETE FROM {0}";
        private static Dictionary<string, DBHelper> _instances;
        private static Type _sqliteConnectionType = typeof(SQLiteConnection);

        #endregion

        #region DBHelper properties

        private readonly string DatabasePath;

        /// <summary>
        /// </summary>
        private readonly Dictionary<string, IndexSpec[]> SoupNameToIndexSpecsMap;

        // Cache of soup name to boolean indicating if soup uses FTS
        private readonly Dictionary<string, bool> SoupNameToHasFTS;

        /// <summary>
        ///     Cache of soup name to soup table names
        /// </summary>
        private readonly Dictionary<string, string> SoupNameToTableNamesMap;

        private SQLiteConnection _sqlConnection;

        #endregion

        private DBHelper(string sqliteDb)
        {
            SoupNameToTableNamesMap = new Dictionary<string, string>();
            SoupNameToIndexSpecsMap = new Dictionary<string, IndexSpec[]>();
            SoupNameToHasFTS = new Dictionary<string, bool>();
            DatabasePath = sqliteDb;

            _sqlConnection = (SQLiteConnection)Activator.CreateInstance(_sqliteConnectionType, sqliteDb, true);

        }

        /// <summary>
        /// This static method exists if you need to extend SQLiteConnection to add more functionality.
        /// 
        /// This method will throw SmartStoreException if type isn't derived from SQLiteConnection.
        /// </summary>
        /// <param name="sqlConnectionType"></param>
        public static void SetSqliteConnectionClass(Type sqlConnectionType)
        {
            if (sqlConnectionType == null || (!sqlConnectionType.GetTypeInfo().IsSubclassOf(typeof(SQLiteConnection)) &&
                sqlConnectionType != typeof(SQLiteConnection)))
            {
                throw new SmartStoreException("sqlConnectionType must be SQLiteConnection or derived from SQLiteConnection");
            }
            _sqliteConnectionType = sqlConnectionType;
        }

        public static DBHelper GetInstance(string sqliteDbFile)
        {
            if (_instances == null)
            {
                _instances = new Dictionary<string, DBHelper>();
            }
            DBHelper instance;
            if (!_instances.TryGetValue(sqliteDbFile, out instance))
            {
                instance = new DBHelper(sqliteDbFile);
                _instances.Add(sqliteDbFile, instance);
            }
            return instance;
        }

        public void CacheTableName(string soupName, string tableName)
        {
            SoupNameToTableNamesMap.Add(soupName, tableName);
        }

        public string GetCachedTableName(string soupName)
        {
            string value;
            if (!SoupNameToTableNamesMap.TryGetValue(soupName, out value))
            {
                return null;
            }
            return value;
        }

        public void CacheIndexSpecs(string soupName, IndexSpec[] indexSpecs)
        {
            if (SoupNameToIndexSpecsMap.ContainsKey(soupName) == false)
            {
                SoupNameToIndexSpecsMap.Add(soupName, indexSpecs);
            }
            if (SoupNameToHasFTS.ContainsKey(soupName) == false)
            {
                SoupNameToHasFTS.Add(soupName, IndexSpec.HasFTS(indexSpecs));
            }
        }

        public IndexSpec[] GetCachedIndexSpecs(string soupName)
        {
            IndexSpec[] value;
            if (!SoupNameToIndexSpecsMap.TryGetValue(soupName, out value))
            {
                return null;
            }
            return value;
        }

        public void RemoveFromCache(string soupName)
        {
            SoupNameToTableNamesMap.Remove(soupName);
            SoupNameToIndexSpecsMap.Remove(soupName);
            SoupNameToHasFTS.Remove(soupName);
        }

        public long GetNextId(string tableName)
        {
            long result = _sqlConnection.ExecuteScalar<long>(SeqSelect, tableName);
            result++;
            return result;
        }

        public SqliteStatement LimitRawQuery(string sql, string limit, params string[] args)
        {
            string limitSql = String.Format(LimitSelect, sql, limit);
            return new SqliteStatement(_sqlConnection, limitSql, NotNull(args));
        }

        public long CountRawCountQuery(string countSql, params string[] args)
        {
            return _sqlConnection.ExecuteScalar<long>(countSql, NotNull(args));
        }

        public long CountRawQuery(String sql, params string[] args)
        {
            string countSql = String.Format(CountSelect, "", "(" + sql + ")");
            return CountRawCountQuery(countSql, args);
        }

        public SQLiteCommand Query(string table, string[] columns, string orderBy, string limit, string whereClause,
            params string[] args)
        {
            if (String.IsNullOrWhiteSpace(table) || columns == null || columns.Length == 0)
            {
                throw new InvalidOperationException("Must specify a table and columns to query");
            }
            if (String.IsNullOrWhiteSpace(whereClause))
            {
                whereClause = String.Empty;
            }
            else
            {
                whereClause = "WHERE " + whereClause;
            }
            string sql = String.Format(QueryStatement,
                String.Join(", ", columns),
                table,
                whereClause,
                orderBy,
                limit,
                String.Empty);
            return _sqlConnection.CreateCommand(sql, NotNull(args));
        }

        public long Insert(string table, Dictionary<string, object> contentValues)
        {
            if (String.IsNullOrWhiteSpace(table) || contentValues == null || contentValues.Keys.Count == 0)
            {
                throw new InvalidOperationException("Must specify a table and provide content to insert");
            }
            string columns = String.Join(", ", contentValues.Keys);
            var valueBindingString = new StringBuilder();
            for (int i = 0, max = contentValues.Count; i < max; i++)
            {
                valueBindingString.Append("?");
                if ((i + 1) < max)
                {
                    valueBindingString.Append(", ");
                }
            }
            string sql = String.Format(InsertStatement,
                table,
                columns,
                valueBindingString);
            var rowsModified = _sqlConnection.Execute(sql, contentValues.Values.ToArray());
            return LastInsertRowId();
        }

        public bool Update(string table, Dictionary<string, object> contentValues, string whereClause,
            params string[] args)
        {
            if (String.IsNullOrWhiteSpace(table) || contentValues == null || contentValues.Keys.Count == 0)
            {
                throw new InvalidOperationException("Must specify a table and provide content to update");
            }
            if (String.IsNullOrWhiteSpace(whereClause))
            {
                whereClause = String.Empty;
            }
            string entries = String.Join("= ?, ", contentValues.Keys);
            if (contentValues.Keys.Count > 0)
            {
                entries += " = ?";
            }
            string sql = String.Format(UpdateStatement,
                table,
                entries,
                whereClause);

            var sqlArgs = contentValues.Values.ToArray().Concat(NotNull(args)).ToArray();

            _sqlConnection.Execute(sql, sqlArgs);
            return true;
        }


        public bool Delete(string table, Dictionary<string, object> contentValues)
        {
            if (String.IsNullOrWhiteSpace(table) || contentValues == null || contentValues.Keys.Count == 0)
            {
                throw new InvalidOperationException("Must specify a table and provide content to delete");
            }
            string values = String.Join(" = ?, ", contentValues.Keys);
            if (contentValues.Keys.Count > 0)
            {
                values += " = ?";
            }
            string sql = String.Format(DeleteStatement,
                table,
                values);
            var args = contentValues.Values.ToArray();
            _sqlConnection.Execute(sql, args);
            return true;
        }

        public bool Delete(string table, string whereClause)
        {
            if (String.IsNullOrWhiteSpace(table) || String.IsNullOrWhiteSpace(whereClause))
            {
                throw new InvalidOperationException("Must specify a table and provide where clause to delete");
            }
            string sql = String.Format(DeleteStatement,
                table,
                whereClause);
            _sqlConnection.Execute(sql);
            return true;
        }

        public bool Delete(string table)
        {
            string sql = String.Format(DeleteAllStatement, table);
            _sqlConnection.Execute(sql);
            return true;
        }

        public string GetSoupTableName(string soupName)
        {
            string soupTableName = GetCachedTableName(soupName);
            if (String.IsNullOrWhiteSpace(soupTableName))
            {
                soupTableName = GetSoupTableNameFromDb(soupName);
                if (!String.IsNullOrWhiteSpace(soupTableName))
                {
                    CacheTableName(soupName, soupTableName);
                }
            }
            return soupTableName;
        }

        private string GetSoupTableNameFromDb(string soupName)
        {
            var cmd = Query(SmartStore.SoupNamesTable, new[] { SmartStore.IdCol }, String.Empty,
                    String.Empty,
                    SmartStore.SoupNamePredicate, soupName);
            var soupId = cmd.ExecuteScalar<int>();
            if (soupId != default(int))
            {
                return SmartStore.GetSoupTableName(soupId);
            }
            return null;
        }

        internal void ResetConnection()
        {
            _sqlConnection.Dispose();
            _sqlConnection = new SQLiteConnection(DatabasePath);
        }

        public int Execute(string sql)
        {
            return _sqlConnection.Execute(sql);
        }

        public string GetColumnNameForPath(String soupName, String path)
        {
            IndexSpec[] indexSpecs = GetIndexSpecs(soupName);
            foreach (IndexSpec indexSpec in indexSpecs.Where(indexSpec => indexSpec.Path.Equals(path)))
            {
                return indexSpec.ColumnName;
            }
            return String.Empty;
        }

        public IndexSpec[] GetIndexSpecs(String soupName)
        {
            IndexSpec[] indexSpecs = GetCachedIndexSpecs(soupName);
            if (indexSpecs == null || indexSpecs.Length == 0)
            {
                indexSpecs = GetIndexSpecsFromDb(soupName);
                CacheIndexSpecs(soupName, indexSpecs);
            }
            return indexSpecs;
        }

        private class GetIndexSpecsResult
        {
            public string path { get; set; }
            public string columnName { get; set; }
            public string columnType { get; set; }
        }

        private IndexSpec[] GetIndexSpecsFromDb(String soupName)
        {
            var cmd = Query(SmartStore.SoupIndexMapTable,
                new[] { SmartStore.PathCol, SmartStore.ColumnNameCol, SmartStore.ColumnTypeCol }, null,
                null, SmartStore.SoupNamePredicate, soupName);

            var list = cmd.ExecuteQuery<GetIndexSpecsResult>();

            var indexSpecs = new List<IndexSpec>();

            foreach (var item in list)
            {
                String path = item.path;
                String columnName = item.columnName;
                var columnType = new SmartStoreType(item.columnType);
                indexSpecs.Add(new IndexSpec(path, columnType, columnName));
            }

            return indexSpecs.ToArray();
        }

        /**
	    * @param db
	    * @param soupName
	    * @return true if soup has full-text-search index
	    */
        public bool HasFTS(String soupName)
        {
            GetIndexSpecs(soupName); // will populate cache if needed
            return GetCachedHasFTS(soupName);
        }

        public Boolean GetCachedHasFTS(String soupName)
        {
            bool result;
            SoupNameToHasFTS.TryGetValue(soupName, out result);
            return result;
        }

        public bool BeginTransaction()
        {
            try
            {
                _sqlConnection.BeginTransaction();
            }
            catch (InvalidOperationException) { return false; }
            return true;
        }

        public bool CommitTransaction()
        {
            try
            {
                _sqlConnection.Commit();
            }
            catch (InvalidOperationException) { return false; }
            return true;
        }

        public bool RollbackTransaction()
        {
            try
            {
                _sqlConnection.Rollback();
            }
            catch (InvalidOperationException) { return false; }
            return true;
        }

        public void Dispose()
        {
            if (_sqlConnection != null)
            {
                _sqlConnection.Dispose();
            }
            _instances.Remove(DatabasePath);
            SoupNameToIndexSpecsMap.Clear();
            SoupNameToTableNamesMap.Clear();
        }

        private long LastInsertRowId()
        {
            return _sqlConnection.ExecuteScalar<long>("SELECT last_insert_rowid()");
        }

        private object[] NotNull(object[] args)
        {
            return args ?? new object[] { };
        }
    }
}
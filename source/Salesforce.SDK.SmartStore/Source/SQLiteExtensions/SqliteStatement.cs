using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sqlite3Statement = SQLitePCL.sqlite3_stmt;
using SQLite;
using System.Reflection;

namespace Salesforce.SDK.SmartStore.Source.SQLiteExtensions
{
    public class SqliteStatement
    {
        private readonly SQLiteConnection _conn;

        public string CommandText { get; private set; }
        public object[] Args { get; private set; }
        private List<Binding> _bindings;

        internal SqliteStatement(SQLiteConnection connection, string query, params object[] args)
        {
            this._conn = connection;
            this.CommandText = query;
            this.Args = args ?? new object[] { };
            this._bindings = new List<Binding>();
            foreach (var arg in this.Args)
            {
                Bind(arg);
            }
        }

        public void Execute(Action<SqliteRow> action)
        {
            var stmt = Prepare();
            try
            {
                var cols = new string[SQLite3.ColumnCount(stmt)];

                for (int i = 0; i < cols.Length; i++)
                {
                    var name = SQLite3.ColumnName16(stmt, i);
                    cols[i] = name;
                }

                while (SQLite3.Step(stmt) == SQLite3.Result.Row)
                {
                    var row = new SqliteRow(stmt, cols);
                    action(row);
                }
            }
            finally
            {
                SQLite3.Finalize(stmt);
            }
        }

        Sqlite3Statement Prepare()
        {
            var stmt = SQLite3.Prepare2(_conn.Handle, CommandText);
            BindAll(stmt);
            return stmt;
        }


        public void Bind(string name, object val)
        {
            _bindings.Add(new Binding
            {
                Name = name,
                Value = val
            });
        }

        public void Bind(object val)
        {
            Bind(null, val);
        }

        void BindAll(Sqlite3Statement stmt)
        {
            int nextIdx = 1;
            foreach (var b in _bindings)
            {
                if (b.Name != null)
                {
                    b.Index = SQLite3.BindParameterIndex(stmt, b.Name);
                }
                else
                {
                    b.Index = nextIdx++;
                }

                BindParameter(stmt, b.Index, b.Value, _conn.StoreDateTimeAsTicks);
            }
        }

        internal static IntPtr NegativePointer = new IntPtr(-1);

        const string DateTimeExactStoreFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff";

        internal static void BindParameter(Sqlite3Statement stmt, int index, object value, bool storeDateTimeAsTicks)
        {
            if (value == null)
            {
                SQLite3.BindNull(stmt, index);
            }
            else
            {
                if (value is Int32)
                {
                    SQLite3.BindInt(stmt, index, (int)value);
                }
                else if (value is String)
                {
                    SQLite3.BindText(stmt, index, (string)value, -1, NegativePointer);
                }
                else if (value is Byte || value is UInt16 || value is SByte || value is Int16)
                {
                    SQLite3.BindInt(stmt, index, Convert.ToInt32(value));
                }
                else if (value is Boolean)
                {
                    SQLite3.BindInt(stmt, index, (bool)value ? 1 : 0);
                }
                else if (value is UInt32 || value is Int64)
                {
                    SQLite3.BindInt64(stmt, index, Convert.ToInt64(value));
                }
                else if (value is Single || value is Double || value is Decimal)
                {
                    SQLite3.BindDouble(stmt, index, Convert.ToDouble(value));
                }
                else if (value is TimeSpan)
                {
                    SQLite3.BindInt64(stmt, index, ((TimeSpan)value).Ticks);
                }
                else if (value is DateTime)
                {
                    if (storeDateTimeAsTicks)
                    {
                        SQLite3.BindInt64(stmt, index, ((DateTime)value).Ticks);
                    }
                    else
                    {
                        SQLite3.BindText(stmt, index, ((DateTime)value).ToString(DateTimeExactStoreFormat, System.Globalization.CultureInfo.InvariantCulture), -1, NegativePointer);
                    }
                }
                else if (value is DateTimeOffset)
                {
                    SQLite3.BindInt64(stmt, index, ((DateTimeOffset)value).UtcTicks);
                }
                else if (value is byte[])
                {
                    SQLite3.BindBlob(stmt, index, (byte[])value, ((byte[])value).Length, NegativePointer);
                }
                else if (value is Guid)
                {
                    SQLite3.BindText(stmt, index, ((Guid)value).ToString(), 72, NegativePointer);
                }
                else
                {
                    // Now we could possibly get an enum, retrieve cached info
                    var valueType = value.GetType();
                    var enumInfo = EnumCache.GetInfo(valueType);
                    if (enumInfo.IsEnum)
                    {
                        var enumIntValue = Convert.ToInt32(value);
                        if (enumInfo.StoreAsText)
                            SQLite3.BindText(stmt, index, enumInfo.EnumValues[enumIntValue], -1, NegativePointer);
                        else
                            SQLite3.BindInt(stmt, index, enumIntValue);
                    }
                    else
                    {
                        throw new NotSupportedException("Cannot store type: " + Orm.GetType(value));
                    }
                }
            }
        }

        class Binding
        {
            public string Name { get; set; }

            public object Value { get; set; }

            public int Index { get; set; }
        }

        class EnumCacheInfo
        {
            public EnumCacheInfo(Type type)
            {
                var typeInfo = type.GetTypeInfo();

                IsEnum = typeInfo.IsEnum;

                if (IsEnum)
                {
                    StoreAsText = typeInfo.CustomAttributes.Any(x => x.AttributeType == typeof(StoreAsTextAttribute));

                    if (StoreAsText)
                    {
                        EnumValues = Enum.GetValues(type).Cast<object>().ToDictionary(Convert.ToInt32, x => x.ToString());
                    }
                    else
                    {
                        EnumValues = Enum.GetValues(type).Cast<object>().ToDictionary(Convert.ToInt32, x => Convert.ToInt32(x).ToString());
                    }
                }
            }

            public bool IsEnum { get; private set; }

            public bool StoreAsText { get; private set; }

            public Dictionary<int, string> EnumValues { get; private set; }
        }

        static class EnumCache
        {
            static readonly Dictionary<Type, EnumCacheInfo> Cache = new Dictionary<Type, EnumCacheInfo>();

            public static EnumCacheInfo GetInfo<T>()
            {
                return GetInfo(typeof(T));
            }

            public static EnumCacheInfo GetInfo(Type type)
            {
                lock (Cache)
                {
                    EnumCacheInfo info = null;
                    if (!Cache.TryGetValue(type, out info))
                    {
                        info = new EnumCacheInfo(type);
                        Cache[type] = info;
                    }

                    return info;
                }
            }
        }

        public class SqliteRow
        {
            private readonly Sqlite3Statement _statement;
            private readonly List<string> _columns;

            internal SqliteRow(Sqlite3Statement statement, string[] columns)
            {
                this._statement = statement;
                this._columns = columns.ToList();
            }

            public int ColumnCount { get { return _columns.Count; } }

            public string GetText(int position)
            {
                return SQLite3.ColumnText(_statement, position);
            }

            internal string ColumnName(int position)
            {
                return _columns[position];
            }

            internal int GetInteger(int position)
            {
                return SQLite3.ColumnInt(_statement, position);
            }

            internal float GetFloat(int position)
            {
                return (float)SQLite3.ColumnDouble(_statement, position);
            }

            internal string GetText(string columnName)
            {
                return GetText(GetColumnIndex(columnName));
            }

            internal int GetColumnIndex(string columnName)
            {
                return _columns.IndexOf(columnName);
            }

            internal string GetColumnName(int position)
            {
                return _columns[position];
            }

            internal SQLite3.ColType GetColType(int position)
            {
                return SQLite3.ColumnType(_statement, position);
            }

            internal byte[] GetBlob(int position)
            {
                return SQLite3.ColumnBlob(_statement, position);
            }
        }
    }
}

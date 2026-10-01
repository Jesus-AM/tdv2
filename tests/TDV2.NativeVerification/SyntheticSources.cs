using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Tdv2.Synchronization;
namespace Tdv2.NativeVerification;

// Synthetic ADO.NET peer: production SELECT/mapping/limits run, no SQL Server/MySQL is contacted.
internal sealed class SyntheticSources : ISourceConnections
{
    internal DataTable Sii = Units();
    internal DataTable Ilda = Inventory();
    internal readonly ConcurrentQueue<string> Queries = new();
    internal string? Failure;
    internal string? Block;
    internal TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Continue = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal Func<Task>? OnRead;
    internal int FailAfter = -1;
    public DbConnection Create(string source) => new SyntheticConnection(this,source);
    internal static DataTable Table(string[] columns, params object?[][] rows)
    {
        var table = new DataTable(); foreach (var c in columns) table.Columns.Add(c,typeof(object));
        foreach (var row in rows) table.Rows.Add(row.Select(v => v ?? DBNull.Value).ToArray()); return table;
    }
    internal static DataTable Units() => Table(CatalogSource.SiiColumns.Select(c => c.ToUpperInvariant()).ToArray(),
        ["A",2026,"100","Área sintética A","0001","Encargada sintética",null,"A",2,"Activo"],
        ["A3",2026,"110","Área sintética A3","0002",null,"A","A",3,"Activo"],
        ["A4",2026,"111","Área sintética A4",null,null,"A3","A",4,"Activo"],
        ["B",2026,"200","Área sintética B","0003",null,null,"A",2,"Activo"]);
    internal static DataTable Inventory() => Table(["id","ur2","informacion_generada","extra","vacio"],
        [1,"100","Constancias sintéticas","Valor íntegro áéíóú",""], [2," 110 ","Registro subordinado",null,""],
        [3,"200","Registro de otra rama",null,""], [4,null,null,"Sin área",null], [5,"100","Seguimiento sintético",null,""]);
    private sealed class SyntheticConnection(SyntheticSources owner,string name) : DbConnection
    {
        [AllowNull] public override string ConnectionString { get; set; } = "";
        public override string Database => name;
        public override string DataSource => "synthetic";
        public override string ServerVersion => "synthetic";
        public override ConnectionState State => ConnectionState.Open;
        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => new SyntheticCommand(owner,name,this);
    }
    private sealed class SyntheticCommand(SyntheticSources owner,string name,DbConnection connection) : DbCommand
    {
        private readonly Npgsql.NpgsqlCommand parameters = new();
        [AllowNull] public override string CommandText { get; set; } = "";
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; } = connection;
        protected override DbTransaction? DbTransaction { get; set; }
        protected override DbParameterCollection DbParameterCollection => parameters.Parameters;
        public override void Cancel() { }
        public override void Prepare() => throw new NotSupportedException();
        protected override DbParameter CreateDbParameter() => new Npgsql.NpgsqlParameter();
        public override int ExecuteNonQuery()
        {
            if (name != "sii" || CommandText != "SET TRANSACTION ISOLATION LEVEL READ COMMITTED") throw new InvalidOperationException("Unexpected remote mutation.");
            owner.Queries.Enqueue(CommandText); return 0;
        }
        public override object? ExecuteScalar() => throw new NotSupportedException();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => ExecuteDbDataReaderAsync(behavior,CancellationToken.None).GetAwaiter().GetResult();
        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior,CancellationToken ct)
        {
            if (CommandText != CatalogSource.IldaSelect && CommandText != CatalogSource.SiiSelect + CatalogSource.SiiLatest && CommandText != CatalogSource.SiiSelect)
                throw new InvalidOperationException("Unexpected remote SQL.");
            if (name == "ilda" && (int)parameters.Parameters[0].Value! <= 0) throw new InvalidOperationException("Expected bound row sentinel.");
            owner.Queries.Enqueue(CommandText);
            if (owner.Failure == name) throw new IOException("password=SYNTHETIC_SOURCE_SECRET; source contents must never escape");
            if (owner.OnRead is not null) await owner.OnRead();
            if (owner.Block == name) { owner.Entered.TrySetResult(); await owner.Continue.Task.WaitAsync(ct); }
            var data = name == "sii" ? owner.Sii : owner.Ilda;
            return new InterruptibleReader(data.CreateDataReader(),owner.FailAfter);
        }
    }
    private sealed class InterruptibleReader(DbDataReader inner,int failAfter) : DbDataReader
    {
        private int count;
        public override bool Read() { if (failAfter >= 0 && count++ == failAfter) throw new IOException("SYNTHETIC_PARTIAL_STREAM_SECRET"); return inner.Read(); }
        public override Task<bool> ReadAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(Read()); }
        public override int FieldCount => inner.FieldCount;
        public override object this[int ordinal] => inner[ordinal];
        public override object this[string name] => inner[name];
        public override int Depth => inner.Depth;
        public override bool HasRows => inner.HasRows;
        public override bool IsClosed => inner.IsClosed;
        public override int RecordsAffected => inner.RecordsAffected;
        public override void Close() => inner.Close();
        public override bool NextResult() => inner.NextResult();
        public override string GetName(int i) => inner.GetName(i);
        public override string GetDataTypeName(int i) => inner.GetDataTypeName(i);
        public override Type GetFieldType(int i) => inner.GetFieldType(i);
        public override object GetValue(int i) => inner.GetValue(i);
        public override int GetValues(object[] values) => inner.GetValues(values);
        public override int GetOrdinal(string name) => inner.GetOrdinal(name);
        public override bool GetBoolean(int i) => inner.GetBoolean(i);
        public override byte GetByte(int i) => inner.GetByte(i);
        public override long GetBytes(int i,long offset,byte[]? b,int start,int length) => inner.GetBytes(i,offset,b,start,length);
        public override char GetChar(int i) => inner.GetChar(i);
        public override long GetChars(int i,long offset,char[]? b,int start,int length) => inner.GetChars(i,offset,b,start,length);
        public override Guid GetGuid(int i) => inner.GetGuid(i);
        public override short GetInt16(int i) => inner.GetInt16(i);
        public override int GetInt32(int i) => inner.GetInt32(i);
        public override long GetInt64(int i) => inner.GetInt64(i);
        public override float GetFloat(int i) => inner.GetFloat(i);
        public override double GetDouble(int i) => inner.GetDouble(i);
        public override string GetString(int i) => inner.GetString(i);
        public override decimal GetDecimal(int i) => inner.GetDecimal(i);
        public override DateTime GetDateTime(int i) => inner.GetDateTime(i);
        public override bool IsDBNull(int i) => inner.IsDBNull(i);
        public override IEnumerator GetEnumerator() => ((IEnumerable)inner).GetEnumerator();
        protected override void Dispose(bool disposing) { if (disposing) inner.Dispose(); base.Dispose(disposing); }
    }
}

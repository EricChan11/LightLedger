using System;
using System.IO;
using System.Text;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public sealed class LedgerDb : IDisposable {
    IntPtr db;
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Row(IntPtr arg,int n,IntPtr values,IntPtr names);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_open(byte[] file,out IntPtr db);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_close(IntPtr db);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_exec(IntPtr db,byte[] sql,Row callback,IntPtr arg,out IntPtr error);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern void sqlite3_free(IntPtr p);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern IntPtr sqlite3_backup_init(IntPtr dest,byte[] destName,IntPtr source,byte[] sourceName);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_backup_step(IntPtr backup,int pages);
    [DllImport("winsqlite3.dll", CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_backup_finish(IntPtr backup);
    static byte[] Utf(string s) { return Encoding.UTF8.GetBytes(s+"\0"); }
    static string Read(IntPtr p) { if(p==IntPtr.Zero)return ""; int n=0; while(Marshal.ReadByte(p,n)!=0)n++; byte[] b=new byte[n]; Marshal.Copy(p,b,0,n); return Encoding.UTF8.GetString(b); }
    public static string Q(string s) { return "'"+s.Replace("'","''")+"'"; }
    public LedgerDb(string path) {
        if(sqlite3_open(Utf(path),out db)!=0) { if(db!=IntPtr.Zero)sqlite3_close(db); throw new Exception(L.T("无法打开数据库：")+path); }
        Run("PRAGMA busy_timeout=5000; PRAGMA journal_mode=DELETE; CREATE TABLE IF NOT EXISTS entries(id INTEGER PRIMARY KEY, day TEXT NOT NULL, kind TEXT NOT NULL CHECK(kind IN ('收入','支出')), amount INTEGER NOT NULL CHECK(amount>0), currency TEXT NOT NULL, category TEXT NOT NULL, note TEXT NOT NULL, created_at TEXT NOT NULL DEFAULT '', updated_at TEXT NOT NULL DEFAULT '');");
        EnsureTimestampColumns();
    }
    void EnsureTimestampColumns() {
        DataTable columns=Run("PRAGMA table_info(entries)"); bool hasCreated=false,hasUpdated=false;
        foreach(DataRow row in columns.Rows) { string name=row[1].ToString(); if(name=="created_at")hasCreated=true; if(name=="updated_at")hasUpdated=true; }
        string migration="";
        if(!hasCreated)migration+="ALTER TABLE entries ADD COLUMN created_at TEXT NOT NULL DEFAULT '';";
        if(!hasUpdated)migration+="ALTER TABLE entries ADD COLUMN updated_at TEXT NOT NULL DEFAULT '';";
        if(migration!="")Run("BEGIN IMMEDIATE;"+migration+"COMMIT;");
    }
    public DataTable Run(string sql) {
        DataTable t=new DataTable(); IntPtr err;
        Row callback=delegate(IntPtr a,int n,IntPtr vals,IntPtr names) {
            if(t.Columns.Count==0)for(int i=0;i<n;i++)t.Columns.Add(Read(Marshal.ReadIntPtr(names,i*IntPtr.Size)));
            object[] row=new object[n]; for(int i=0;i<n;i++)row[i]=Read(Marshal.ReadIntPtr(vals,i*IntPtr.Size)); t.Rows.Add(row); return 0;
        };
        int rc=sqlite3_exec(db,Utf(sql),callback,IntPtr.Zero,out err);
        if(rc!=0) { string message=Read(err); sqlite3_free(err); throw new Exception(message); } return t;
    }
    public void Backup(string path) {
        IntPtr dest; if(sqlite3_open(Utf(path),out dest)!=0) { if(dest!=IntPtr.Zero)sqlite3_close(dest); throw new Exception(L.T("无法创建备份文件。")); }
        try { IntPtr b=sqlite3_backup_init(dest,Utf("main"),db,Utf("main")); if(b==IntPtr.Zero)throw new Exception(L.T("备份初始化失败。")); int result=sqlite3_backup_step(b,-1); int end=sqlite3_backup_finish(b); if(result!=101 || end!=0)throw new Exception(L.T("备份未完成，请选择其他路径后重试。")); }
        finally { sqlite3_close(dest); }
    }
    public void Dispose() { if(db!=IntPtr.Zero) { sqlite3_close(db); db=IntPtr.Zero; } }
}

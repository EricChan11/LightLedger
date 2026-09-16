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
        Run("PRAGMA busy_timeout=5000; PRAGMA journal_mode=DELETE; CREATE TABLE IF NOT EXISTS entries(id INTEGER PRIMARY KEY, day TEXT NOT NULL, kind TEXT NOT NULL CHECK(kind IN ('收入','支出')), amount INTEGER NOT NULL CHECK(amount>0), currency TEXT NOT NULL, category TEXT NOT NULL, note TEXT NOT NULL);");
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

public class LedgerForm : Form {
    public static readonly string[] Currencies={"CNY","USD","EUR","JPY","GBP","HKD","AUD","CAD","SGD","KRW","CHF","TWD"};
    public static int Digits(string c) { return c=="JPY"||c=="KRW" ? 0 : 2; }
    public static long Minor(decimal amount,string c) {
        int digits=Digits(c); if(amount<=0 || amount>1000000000000m)throw new Exception(L.T("金额必须大于 0，且不超过一万亿。"));
        if(decimal.Round(amount,digits)!=amount)throw new Exception(c+L.T(" 金额最多允许 ")+digits+L.T(" 位小数。"));
        return (long)(amount*(digits==0?1m:100m));
    }
    static string Money(long n,string c) { return ((decimal)n/(Digits(c)==0?1m:100m)).ToString("N"+Digits(c),CultureInfo.CurrentCulture); }
    LedgerDb db; string dbPath; long editing=0;
    DateTimePicker day=new DateTimePicker(), month=new DateTimePicker();
    ComboBox language=new ComboBox(); bool switching=false;
    ComboBox kind=new ComboBox(),currency=new ComboBox(),category=new ComboBox(),filter=new ComboBox();
    TextBox amount=new TextBox(),note=new TextBox(),search=new TextBox(); CheckBox all=new CheckBox();
    DataGridView grid=new DataGridView(); TextBox totals=new TextBox(); Label status=new Label(),editorTitle=new Label(); Button save;
    public LedgerForm() {
        L.Load(); Text=L.T("轻记账 · 本地多币种账本"); Size=new Size(1380,850); MinimumSize=new Size(1260,790); StartPosition=FormStartPosition.CenterScreen; Font=new Font("Microsoft YaHei UI",10); BackColor=Color.FromArgb(244,246,249);
        dbPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data","ledger.db"); Directory.CreateDirectory(Path.GetDirectoryName(dbPath)); db=new LedgerDb(dbPath);
        var root=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Padding=new Padding(22) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,67)); root.RowStyles.Add(new RowStyle(SizeType.Absolute,48)); root.RowStyles.Add(new RowStyle(SizeType.Absolute,96)); root.RowStyles.Add(new RowStyle(SizeType.Percent,100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute,34)); Controls.Add(root);
        var head=new FlowLayoutPanel { Dock=DockStyle.Fill }; head.Controls.Add(new Label { Text=L.T("轻记账"),Font=new Font(Font.FontFamily,24,FontStyle.Bold),AutoSize=true,Margin=new Padding(0,0,20,0) }); head.Controls.Add(new Label { Text=L.T("本地保存  /  多币种  /  离线可用"),AutoSize=true,ForeColor=Color.DimGray,Margin=new Padding(0,19,0,0) }); language.DropDownStyle=ComboBoxStyle.DropDownList; language.Items.AddRange(new string[]{"中文","日本語","English"}); language.SelectedIndex=L.Index; language.Width=130; language.Margin=new Padding(25,17,0,0); head.Controls.Add(language); root.Controls.Add(head,0,0);
        var filters=new FlowLayoutPanel { Dock=DockStyle.Fill }; month.Format=DateTimePickerFormat.Custom; month.CustomFormat="yyyy-MM"; month.ShowUpDown=true; month.Width=115; all.Text=L.T("全部月份"); all.AutoSize=true;
        filter.DropDownStyle=ComboBoxStyle.DropDownList; filter.Items.Add(L.T("全部币种")); foreach(string code in Currencies)filter.Items.Add(new CurrencyItem(code)); filter.SelectedIndex=0; filter.Width=215; search.Width=140;
        filters.Controls.AddRange(new Control[]{month,all,filter,new Label{Text=L.T("搜索分类 / 备注"),AutoSize=true,Margin=new Padding(14,6,4,0)},search});
        filters.Controls.Add(Btn(L.T("导出 CSV"),Export)); filters.Controls.Add(Btn(L.T("备份数据库"),Backup)); root.Controls.Add(filters,0,1);
        totals.Dock=DockStyle.Fill; totals.BackColor=Color.White; totals.Multiline=true; totals.ReadOnly=true; totals.ScrollBars=ScrollBars.Vertical; totals.BorderStyle=BorderStyle.None; totals.Text=L.T("暂无记录"); root.Controls.Add(totals,0,2);
        var body=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(0,14,0,0)}; body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,278)); root.Controls.Add(body,0,3);
        grid.Dock=DockStyle.Fill; grid.ReadOnly=true; grid.AllowUserToAddRows=false; grid.AllowUserToDeleteRows=false; grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect; grid.MultiSelect=false; grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill; grid.RowHeadersVisible=false; grid.BackgroundColor=Color.White; grid.BorderStyle=BorderStyle.None; grid.AutoGenerateColumns=true; grid.RowTemplate.Height=32; grid.ColumnHeadersHeight=38; grid.CellDoubleClick+=delegate(object s,DataGridViewCellEventArgs e){if(e.RowIndex>=0) { try{Edit();}catch(Exception ex){MessageBox.Show(this,ex.Message,L.T("操作未完成"));} }}; body.Controls.Add(grid,0,0);
        var editor=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(16,0,0,0)}; body.Controls.Add(editor,1,0);
        editorTitle.Text=L.T("记一笔"); editorTitle.Font=new Font(Font.FontFamily,14,FontStyle.Bold); editorTitle.Height=34; editorTitle.Width=230; editor.Controls.Add(editorTitle);
        day.Format=DateTimePickerFormat.Custom; day.CustomFormat="yyyy-MM-dd"; AddField(editor,L.T("日期"),day);
        kind.DropDownStyle=ComboBoxStyle.DropDownList; kind.Items.AddRange(new string[]{L.T("支出"),L.T("收入")}); kind.SelectedIndex=0; AddField(editor,L.T("类型"),kind);
        currency.DropDownStyle=ComboBoxStyle.DropDownList; foreach(string code in Currencies)currency.Items.Add(new CurrencyItem(code)); currency.SelectedIndex=0; AddField(editor,L.T("币种（CNY 人民币 / JPY 日元）"),currency);
        AddField(editor,L.T("金额（填写正数）"),amount); category.Items.AddRange(new string[]{L.T("餐饮"),L.T("交通"),L.T("购物"),L.T("住房"),L.T("娱乐"),L.T("医疗"),L.T("工资"),L.T("奖金"),L.T("其他")}); category.Text=L.T("餐饮"); AddField(editor,L.T("分类"),category);
        note.MaxLength=1000; AddField(editor,L.T("备注"),note); save=Btn(L.T("保存这笔账"),Save); save.Width=235; save.BackColor=Color.FromArgb(25,111,95); save.ForeColor=Color.White; editor.Controls.Add(save);
        var actions=new FlowLayoutPanel{Width=240,Height=78}; actions.Controls.Add(Btn(L.T("编辑所选"),delegate{Edit();})); actions.Controls.Add(Btn(L.T("删除所选"),Delete)); editor.Controls.Add(actions);
        var googleCalendar=Btn(L.T("添加到 Google 日历"),AddToGoogleCalendar); googleCalendar.Width=235; editor.Controls.Add(googleCalendar);
        editor.Controls.Add(Btn(L.T("取消编辑 / 清空"),delegate{Reset();}));
        status.Dock=DockStyle.Fill; status.TextAlign=ContentAlignment.BottomLeft; status.ForeColor=Color.DimGray; status.AutoEllipsis=true; status.Text=L.T("数据库：")+dbPath; root.Controls.Add(status,0,4);
        month.ValueChanged+=delegate{RefreshData();}; all.CheckedChanged+=delegate{month.Enabled=!all.Checked;RefreshData();}; filter.SelectedIndexChanged+=delegate{RefreshData();}; search.TextChanged+=delegate{RefreshData();}; FormClosed+=delegate{db.Dispose();}; RegisterText(this); language.SelectedIndexChanged+=delegate{ChangeLanguage();}; RefreshData();
    }

    string CurrencyCode { get { return ((CurrencyItem)currency.SelectedItem).Code; } }
    string KindCode { get { return kind.SelectedIndex==0?"支出":"收入"; } }
    void RegisterText(Control parent) {
        if(parent is Label || parent is Button || parent is CheckBox || parent is Form)parent.Tag=L.Key(parent.Text);
        foreach(Control c in parent.Controls)RegisterText(c);
    }
    void TranslateText(Control parent) {
        if(parent.Tag is string)parent.Text=L.T((string)parent.Tag);
        foreach(Control c in parent.Controls)TranslateText(c);
    }
    void ChangeLanguage() {
        if(switching)return;
        int k=kind.SelectedIndex,c=currency.SelectedIndex,f=filter.SelectedIndex;
        string cat=L.CategoryKey(category.Text); long selected=grid.SelectedRows.Count==0?0:long.Parse(grid.SelectedRows[0].Cells[0].Value.ToString());
        switching=true;
        try {
            L.Index=language.SelectedIndex; TranslateText(this);
            kind.Items.Clear();kind.Items.AddRange(new string[]{L.T("支出"),L.T("收入")});kind.SelectedIndex=k;
            currency.Items.Clear();filter.Items.Clear();filter.Items.Add(L.T("全部币种"));
            foreach(string code in Currencies){currency.Items.Add(new CurrencyItem(code));filter.Items.Add(new CurrencyItem(code));}
            currency.SelectedIndex=c;filter.SelectedIndex=f;
            category.Items.Clear();foreach(string key in L.Categories)category.Items.Add(L.T(key));category.Text=L.CategoryText(cat);
            editorTitle.Text=editing==0?L.T("记一笔"):L.T("编辑账目 #")+editing;save.Text=L.T(editing==0?"保存这笔账":"保存修改");status.Text=L.T("数据库：")+dbPath;
        } finally { switching=false; }
        RefreshData();
        foreach(DataGridViewRow row in grid.Rows)if(long.Parse(row.Cells[0].Value.ToString())==selected)row.Selected=true;
        try {L.Save();}catch(Exception ex){MessageBox.Show(this,ex.Message,L.T("操作未完成"));}
    }
    Button Btn(string text,Action action) { var b=new Button{Text=text,AutoSize=true,Height=32,FlatStyle=FlatStyle.Flat,BackColor=Color.White,Margin=new Padding(3,0,5,4)}; b.Click+=delegate{try{action();}catch(Exception ex){MessageBox.Show(this,ex.Message,L.T("操作未完成"),MessageBoxButtons.OK,MessageBoxIcon.Warning);}};return b; }
    void AddField(FlowLayoutPanel p,string label,Control c) { p.Controls.Add(new Label{Text=label,AutoSize=true,Margin=new Padding(3,4,0,2)}); c.Width=235;p.Controls.Add(c); }
    string Where() {
        string w=" WHERE 1=1"; if(!all.Checked)w+=" AND substr(day,1,7)="+LedgerDb.Q(month.Value.ToString("yyyy-MM")); if(filter.SelectedIndex>0)w+=" AND currency="+LedgerDb.Q(((CurrencyItem)filter.SelectedItem).Code);
        if(search.Text.Trim()!="")w+=" AND (instr(lower(category),lower("+LedgerDb.Q(L.CategoryKey(search.Text.Trim()))+"))>0 OR instr(lower(note),lower("+LedgerDb.Q(search.Text.Trim())+"))>0)"; return w;
    }
    void RefreshData() {
        if(switching)return;
        try {
            DataTable raw=db.Run("SELECT id,day,kind,amount,currency,category,note FROM entries"+Where()+" ORDER BY day DESC,id DESC");
            DataTable view=new DataTable(); foreach(string col in new string[]{L.T("编号"),L.T("日期"),L.T("类型"),L.T("金额"),L.T("币种"),L.T("分类"),L.T("备注")})view.Columns.Add(col);
            foreach(DataRow r in raw.Rows)view.Rows.Add(r[0],r[1],L.T(r[2].ToString()),Money(long.Parse(r[3].ToString()),r[4].ToString()),L.Currency(r[4].ToString()),L.CategoryText(r[5].ToString()),r[6]);
            grid.DataSource=view; grid.Columns[0].Visible=false; grid.Columns[6].FillWeight=180; grid.ClearSelection();
            DataTable sums=db.Run("SELECT currency,SUM(CASE WHEN kind='收入' THEN amount ELSE 0 END),SUM(CASE WHEN kind='支出' THEN amount ELSE 0 END) FROM entries"+Where()+" GROUP BY currency ORDER BY currency");
            StringBuilder b=new StringBuilder(); b.AppendFormat(L.T("当前筛选 · {0} 笔（各币种独立统计；不做汇率换算）"),raw.Rows.Count).AppendLine();
            foreach(DataRow r in sums.Rows) { string c=r[0].ToString(); long inc=long.Parse(r[1].ToString()),expense=long.Parse(r[2].ToString()); b.Append(L.Currency(c)+"   "+L.T("收入")+" "+Money(inc,c)+"   "+L.T("支出")+" "+Money(expense,c)+"   "+L.T("结余")+" "+Money(inc-expense,c)+Environment.NewLine); }
            totals.Text=b.ToString();
        } catch(Exception ex){MessageBox.Show(this,ex.Message,L.T("读取失败"));}
    }
    void Save() {
        decimal value; if(!decimal.TryParse(amount.Text.Trim(),NumberStyles.AllowDecimalPoint,CultureInfo.CurrentCulture,out value))throw new Exception(L.T("请输入有效金额，例如 25.50，不要填写千位分隔符。"));
        long minor=Minor(value,CurrencyCode); string cat=L.CategoryKey(category.Text.Trim()); if(cat.Length==0 || cat.Length>50)throw new Exception(L.T("分类必须为 1–50 个字符。"));
        string fields="day="+LedgerDb.Q(day.Value.ToString("yyyy-MM-dd"))+",kind="+LedgerDb.Q(KindCode)+",amount="+minor+",currency="+LedgerDb.Q(CurrencyCode)+",category="+LedgerDb.Q(cat)+",note="+LedgerDb.Q(note.Text.Trim());
        if(editing==0)db.Run("INSERT INTO entries(day,kind,amount,currency,category,note) VALUES("+LedgerDb.Q(day.Value.ToString("yyyy-MM-dd"))+","+LedgerDb.Q(KindCode)+","+minor+","+LedgerDb.Q(CurrencyCode)+","+LedgerDb.Q(cat)+","+LedgerDb.Q(note.Text.Trim())+")"); else db.Run("UPDATE entries SET "+fields+" WHERE id="+editing);
        Reset(); RefreshData(); status.Text=L.T("已保存 · 数据库：")+dbPath;
    }
    long Selected() { if(grid.SelectedRows.Count==0)throw new Exception(L.T("请先选择一条账目。"));return long.Parse(grid.SelectedRows[0].Cells[0].Value.ToString()); }
    void Edit() {
        long id=Selected(); DataTable t=db.Run("SELECT day,kind,amount,currency,category,note FROM entries WHERE id="+id); if(t.Rows.Count==0)throw new Exception(L.T("这条记录已不存在。")); DataRow r=t.Rows[0]; editing=id;
        day.Value=DateTime.ParseExact(r[0].ToString(),"yyyy-MM-dd",CultureInfo.InvariantCulture); kind.SelectedIndex=r[1].ToString()=="支出"?0:1; currency.SelectedIndex=Array.IndexOf(Currencies,r[3].ToString()); amount.Text=((decimal)long.Parse(r[2].ToString())/(Digits(CurrencyCode)==0?1:100)).ToString(CultureInfo.CurrentCulture); category.Text=L.CategoryText(r[4].ToString()); note.Text=r[5].ToString(); editorTitle.Text=L.T("编辑账目 #")+id;save.Text=L.T("保存修改");
    }
    void Delete() { long id=Selected(); if(MessageBox.Show(this,L.T("确定删除所选账目？此操作无法撤销。"),L.T("删除账目"),MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return; db.Run("DELETE FROM entries WHERE id="+id); if(editing==id)Reset();RefreshData(); }
    void Reset() { editing=0; editorTitle.Text=L.T("记一笔");save.Text=L.T("保存这笔账"); amount.Clear();note.Clear();day.Value=DateTime.Today; }
    public static string GoogleCalendarUrl(string entryDay,string entryKind,long entryAmount,string entryCurrency,string entryCategory,string entryNote) {
        DateTime start=DateTime.ParseExact(entryDay,"yyyy-MM-dd",CultureInfo.InvariantCulture);
        string dates=start.ToString("yyyyMMdd",CultureInfo.InvariantCulture)+"/"+start.AddDays(1).ToString("yyyyMMdd",CultureInfo.InvariantCulture);
        string title=L.T(entryKind)+" · "+L.CategoryText(entryCategory)+" · "+L.Currency(entryCurrency)+" "+Money(entryAmount,entryCurrency);
        StringBuilder details=new StringBuilder();
        details.Append(L.T("类型")+": "+L.T(entryKind)+Environment.NewLine);
        details.Append(L.T("金额")+": "+L.Currency(entryCurrency)+" "+Money(entryAmount,entryCurrency)+Environment.NewLine);
        details.Append(L.T("分类")+": "+L.CategoryText(entryCategory));
        if(!String.IsNullOrWhiteSpace(entryNote))details.Append(Environment.NewLine+L.T("备注")+": "+entryNote.Trim());
        details.Append(Environment.NewLine+L.T("来源：轻记账"));
        return "https://calendar.google.com/calendar/render?action=TEMPLATE&text="+Uri.EscapeDataString(title)+"&dates="+dates+"&details="+Uri.EscapeDataString(details.ToString());
    }
    void AddToGoogleCalendar() {
        long id=Selected();
        DataTable t=db.Run("SELECT day,kind,amount,currency,category,note FROM entries WHERE id="+id);
        if(t.Rows.Count==0)throw new Exception(L.T("这条记录已不存在。"));
        DataRow r=t.Rows[0];
        string url=GoogleCalendarUrl(r[0].ToString(),r[1].ToString(),long.Parse(r[2].ToString()),r[3].ToString(),r[4].ToString(),r[5].ToString());
        Process.Start(url);
        status.Text=L.T("已打开 Google 日历，请在浏览器中确认保存。")+" · "+L.T("数据库：")+dbPath;
    }
    void Backup() {
        using(var d=new SaveFileDialog{Filter=L.T("SQLite 数据库|*.db"),FileName=L.T("账本备份-")+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".db"})if(d.ShowDialog(this)==DialogResult.OK){if(string.Equals(Path.GetFullPath(d.FileName),Path.GetFullPath(dbPath),StringComparison.OrdinalIgnoreCase))throw new Exception(L.T("请选择与当前数据库不同的备份路径。")); db.Backup(d.FileName);MessageBox.Show(this,L.T("备份已保存：\n")+d.FileName,L.T("备份完成"));}
    }
    static string Csv(string s) { if(s.Length>0 && "=+-@\t\r".IndexOf(s[0])>=0)s="'"+s;return "\""+s.Replace("\"","\"\"")+"\""; }
    void Export() {
        using(var d=new SaveFileDialog{Filter=L.T("CSV 表格|*.csv"),FileName=L.T("账目-")+DateTime.Now.ToString("yyyyMMdd")+".csv"})if(d.ShowDialog(this)==DialogResult.OK){DataTable t=(DataTable)grid.DataSource;using(var w=new StreamWriter(d.FileName,false,new UTF8Encoding(true))){string[] headers=new string[6]; for(int j=0;j<6;j++)headers[j]=Csv(t.Columns[j+1].ColumnName); w.WriteLine(string.Join(",",headers));foreach(DataRow r in t.Rows){string[] fields=new string[6];for(int i=0;i<6;i++)fields[i]=Csv(r[i+1].ToString());w.WriteLine(string.Join(",",fields));}}MessageBox.Show(this,L.T("已导出当前筛选结果。"),L.T("导出完成"));}
    }
    [STAThread] public static void Main(string[] args) {
        if(args.Length==2 && args[0]=="--self-test") { SelfTest(args[1]);return; }
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        try { if(args.Length==2 && args[0]=="--render") { using(var f=new LedgerForm()) { f.Show(); Application.DoEvents(); using(var bmp=new Bitmap(f.Width,f.Height)) { f.DrawToBitmap(bmp,new Rectangle(0,0,f.Width,f.Height)); bmp.Save(args[1]); } f.Close(); } } else Application.Run(new LedgerForm()); }catch(Exception ex){MessageBox.Show(L.T("启动失败：")+ex.Message,L.T("轻记账"));}
    }
    static void SelfTest(string folder) {
        Directory.CreateDirectory(folder);string path=Path.Combine(folder,"test-"+Guid.NewGuid().ToString("N")+".db"),backup=path+".backup";
        using(var d=new LedgerDb(path)) {
            if(Minor(12.34m,"CNY")!=1234 || Minor(123m,"JPY")!=123)throw new Exception("金额精度失败");
            bool rejected=false;try{Minor(1.1m,"JPY");}catch{rejected=true;}if(!rejected)throw new Exception("日元小数校验失败");
            d.Run("INSERT INTO entries(day,kind,amount,currency,category,note) VALUES('2026-09-06','支出',1234,'CNY','餐饮',"+LedgerDb.Q("中文 ' quote\n多行")+"); INSERT INTO entries(day,kind,amount,currency,category,note) VALUES('2026-09-06','收入',500,'JPY','工资','测试')");
            if(d.Run("SELECT currency FROM entries GROUP BY currency").Rows.Count!=2)throw new Exception("币种分组失败");
            d.Run("UPDATE entries SET amount=2345 WHERE currency='CNY'"); d.Backup(backup);
        }
        using(var d=new LedgerDb(path)) {if(d.Run("SELECT amount FROM entries WHERE currency='CNY'").Rows[0][0].ToString()!="2345")throw new Exception("持久化失败");d.Run("DELETE FROM entries");}
        using(var d=new LedgerDb(backup)) {if(d.Run("SELECT * FROM entries").Rows.Count!=2)throw new Exception("备份失败");}
        File.Delete(path);File.Delete(backup);File.WriteAllText(Path.Combine(folder,"test-result.txt"),"PASS: amount precision, JPY validation, Unicode, SQL quoting, currency grouping, update, persistence, delete, SQLite backup.");
    }
}


public sealed class CurrencyItem {
    public readonly string Code;
    public CurrencyItem(string code) { Code=code; }
    public override string ToString() { return L.Currency(Code); }
}
public static class L {
    public static int Index=0;
    public static readonly string[] Categories={"餐饮","交通","购物","住房","娱乐","医疗","工资","奖金","其他"};
    static readonly System.Collections.Generic.Dictionary<string,string[]> Texts=new System.Collections.Generic.Dictionary<string,string[]> {
        {"轻记账", new string[]{"轻记账","かんたん家計簿","Light Ledger"}},
        {"轻记账 · 本地多币种账本", new string[]{"轻记账 · 本地多币种账本","かんたん家計簿 · ローカル多通貨帳簿","Light Ledger · Local Multi-currency Ledger"}},
        {"本地保存  /  多币种  /  离线可用", new string[]{"本地保存  /  多币种  /  离线可用","ローカル保存 / 多通貨 / オフライン","Local storage / Multi-currency / Offline"}},
        {"全部月份", new string[]{"全部月份","すべての月","All months"}},
        {"全部币种", new string[]{"全部币种","すべての通貨","All currencies"}},
        {"搜索分类 / 备注", new string[]{"搜索分类 / 备注","分類 / メモを検索","Search category / note"}},
        {"导出 CSV", new string[]{"导出 CSV","CSV 出力","Export CSV"}},
        {"备份数据库", new string[]{"备份数据库","DB バックアップ","Back up database"}},
        {"暂无记录", new string[]{"暂无记录","記録なし","No entries"}},
        {"记一笔", new string[]{"记一笔","新規記録","New entry"}},
        {"日期", new string[]{"日期","日付","Date"}},
        {"类型", new string[]{"类型","種類","Type"}},
        {"支出", new string[]{"支出","支出","Expense"}},
        {"收入", new string[]{"收入","収入","Income"}},
        {"币种（CNY 人民币 / JPY 日元）", new string[]{"币种","通貨","Currency"}},
        {"金额（填写正数）", new string[]{"金额（填写正数）","金額（正の数）","Amount (positive number)"}},
        {"分类", new string[]{"分类","分類","Category"}},
        {"备注", new string[]{"备注","メモ","Note"}},
        {"保存这笔账", new string[]{"保存这笔账","記録を保存","Save entry"}},
        {"编辑所选", new string[]{"编辑所选","選択項目を編集","Edit selected"}},
        {"删除所选", new string[]{"删除所选","選択項目を削除","Delete selected"}},
        {"添加到 Google 日历", new string[]{"添加到 Google 日历","Google カレンダーに追加","Add to Google Calendar"}},
        {"取消编辑 / 清空", new string[]{"取消编辑 / 清空","編集取消 / クリア","Cancel / Clear"}},
        {"数据库：", new string[]{"数据库：","データベース：","Database: "}},
        {"操作未完成", new string[]{"操作未完成","操作失敗","Action failed"}},
        {"编号", new string[]{"编号","ID","ID"}},
        {"金额", new string[]{"金额","金額","Amount"}},
        {"币种", new string[]{"币种","通貨","Currency"}},
        {"读取失败", new string[]{"读取失败","読み込み失敗","Read failed"}},
        {"来源：轻记账", new string[]{"来源：轻记账","作成元：かんたん家計簿","Source: Light Ledger"}},
        {"已打开 Google 日历，请在浏览器中确认保存。", new string[]{"已打开 Google 日历，请在浏览器中确认保存。","Google カレンダーを開きました。ブラウザで保存を確認してください。","Google Calendar is open. Confirm and save the event in your browser."}},
        {"已保存 · 数据库：", new string[]{"已保存 · 数据库：","保存済み · データベース：","Saved · Database: "}},
        {"编辑账目 #", new string[]{"编辑账目 #","記録を編集 #","Edit entry #"}},
        {"保存修改", new string[]{"保存修改","変更を保存","Save changes"}},
        {"删除账目", new string[]{"删除账目","記録の削除","Delete entry"}},
        {"确定删除所选账目？此操作无法撤销。", new string[]{"确定删除所选账目？此操作无法撤销。","選択した記録を削除しますか？元に戻せません。","Delete the selected entry? This cannot be undone."}},
        {"餐饮", new string[]{"餐饮","食費","Food"}},
        {"交通", new string[]{"交通","交通費","Transport"}},
        {"购物", new string[]{"购物","買い物","Shopping"}},
        {"住房", new string[]{"住房","住居費","Housing"}},
        {"娱乐", new string[]{"娱乐","娯楽","Entertainment"}},
        {"医疗", new string[]{"医疗","医療","Medical"}},
        {"工资", new string[]{"工资","給与","Salary"}},
        {"奖金", new string[]{"奖金","賞与","Bonus"}},
        {"其他", new string[]{"其他","その他","Other"}},
        {"无法打开数据库：", new string[]{"无法打开数据库：","データベースを開けません：","Cannot open database: "}},
        {"无法创建备份文件。", new string[]{"无法创建备份文件。","バックアップファイルを作成できません。","Cannot create backup file."}},
        {"备份初始化失败。", new string[]{"备份初始化失败。","バックアップを開始できません。","Could not initialize backup."}},
        {"备份未完成，请选择其他路径后重试。", new string[]{"备份未完成，请选择其他路径后重试。","バックアップが完了しませんでした。別の保存先で再試行してください。","Backup did not complete. Try another destination."}},
        {"金额必须大于 0，且不超过一万亿。", new string[]{"金额必须大于 0，且不超过一万亿。","金額は 0 より大きく、1 兆以下にしてください。","Amount must be greater than zero and at most one trillion."}},
        {" 金额最多允许 ", new string[]{" 金额最多允许 "," の小数点以下の最大桁数："," maximum decimal places: "}},
        {" 位小数。", new string[]{" 位小数。"," 桁。","."}},
        {"请输入有效金额，例如 25.50，不要填写千位分隔符。", new string[]{"请输入有效金额，例如 25.50，不要填写千位分隔符。","25.50 のような金額を入力してください。桁区切りは使用しないでください。","Enter an amount such as 25.50, without thousands separators."}},
        {"分类必须为 1–50 个字符。", new string[]{"分类必须为 1–50 个字符。","分類は 1～50 文字で入力してください。","Category must contain 1–50 characters."}},
        {"请先选择一条账目。", new string[]{"请先选择一条账目。","記録を選択してください。","Select an entry first."}},
        {"这条记录已不存在。", new string[]{"这条记录已不存在。","この記録は存在しません。","This entry no longer exists."}},
        {"SQLite 数据库|*.db", new string[]{"SQLite 数据库|*.db","SQLite データベース|*.db","SQLite database|*.db"}},
        {"账本备份-", new string[]{"账本备份-","家計簿バックアップ-","ledger-backup-"}},
        {"请选择与当前数据库不同的备份路径。", new string[]{"请选择与当前数据库不同的备份路径。","現在のデータベースとは別の保存先を選択してください。","Choose a backup path different from the current database."}},
        {"备份已保存：\n", new string[]{"备份已保存：\n","バックアップ保存先：\n","Backup saved:\n"}},
        {"备份完成", new string[]{"备份完成","バックアップ完了","Backup complete"}},
        {"CSV 表格|*.csv", new string[]{"CSV 表格|*.csv","CSV ファイル|*.csv","CSV file|*.csv"}},
        {"账目-", new string[]{"账目-","記録-","entries-"}},
        {"已导出当前筛选结果。", new string[]{"已导出当前筛选结果。","現在の絞り込み結果を出力しました。","Exported the current filtered results."}},
        {"导出完成", new string[]{"导出完成","出力完了","Export complete"}},
        {"启动失败：", new string[]{"启动失败：","起動失敗：","Startup failed: "}},
        {"当前筛选 · {0} 笔（各币种独立统计；不做汇率换算）", new string[]{"当前筛选 · {0} 笔（各币种独立统计；不做汇率换算）","絞り込み結果 · {0} 件（通貨別集計・為替換算なし）","Current filter · {0} entries (separate totals per currency; no conversion)"}},
        {"结余", new string[]{"结余","差引残高","Balance"}}
    };
    static readonly string[][] Names={new string[]{"人民币","人民元","Chinese Yuan"},new string[]{"美元","米ドル","US Dollar"},new string[]{"欧元","ユーロ","Euro"},new string[]{"日元","日本円","Japanese Yen"},new string[]{"英镑","英ポンド","British Pound"},new string[]{"港币","香港ドル","Hong Kong Dollar"},new string[]{"澳元","豪ドル","Australian Dollar"},new string[]{"加元","カナダドル","Canadian Dollar"},new string[]{"新加坡元","シンガポールドル","Singapore Dollar"},new string[]{"韩元","韓国ウォン","South Korean Won"},new string[]{"瑞士法郎","スイスフラン","Swiss Franc"},new string[]{"新台币","台湾ドル","New Taiwan Dollar"}};
    public static string T(string key) { string[] values; return Texts.TryGetValue(key,out values)?values[Index]:key; }
    public static string Key(string text) { foreach(var p in Texts)if(p.Value[Index]==text)return p.Key; return text; }
    public static string CategoryKey(string text) { foreach(string key in Categories)if(T(key)==text)return key;return text; }
    public static string CategoryText(string key) { return Array.IndexOf(Categories,key)>=0?T(key):key; }
    public static string Currency(string code) { int i=Array.IndexOf(LedgerForm.Currencies,code);return i<0?code:code+" "+Names[i][Index]; }
    static string SettingsPath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data","language.txt"); } }
    public static void Load() { try {int n;if(File.Exists(SettingsPath)&&int.TryParse(File.ReadAllText(SettingsPath),out n)&&n>=0&&n<=2)Index=n;}catch(IOException){}catch(UnauthorizedAccessException){} }
    public static void Save() { Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));File.WriteAllText(SettingsPath,Index.ToString(CultureInfo.InvariantCulture)); }
}

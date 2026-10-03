using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

public sealed class CurrencyDefinition {
    public string Code,NameZh,NameJa,NameEn;
    public int Digits,Order;
    public bool Active;
    public string Name(int language) { return language==1?NameJa:(language==2?NameEn:NameZh); }
}

public static class CurrencyCatalog {
    static readonly Dictionary<string,CurrencyDefinition> items=new Dictionary<string,CurrencyDefinition>(StringComparer.OrdinalIgnoreCase);
    static readonly List<string> activeCodes=new List<string>();
    static readonly string[][] BuiltIns={
        new string[]{"JPY","日元","日本円","Japanese Yen","0"},
        new string[]{"CNY","人民币","人民元","Chinese Yuan","2"},
        new string[]{"USD","美元","米ドル","US Dollar","2"},
        new string[]{"EUR","欧元","ユーロ","Euro","2"},
        new string[]{"GBP","英镑","英ポンド","British Pound","2"},
        new string[]{"HKD","港币","香港ドル","Hong Kong Dollar","2"},
        new string[]{"AUD","澳元","豪ドル","Australian Dollar","2"},
        new string[]{"CAD","加元","カナダドル","Canadian Dollar","2"},
        new string[]{"SGD","新加坡元","シンガポールドル","Singapore Dollar","2"},
        new string[]{"KRW","韩元","韓国ウォン","South Korean Won","0"},
        new string[]{"CHF","瑞士法郎","スイスフラン","Swiss Franc","2"},
        new string[]{"TWD","新台币","台湾ドル","New Taiwan Dollar","2"}
    };

    public static void Initialize(LedgerDb db) {
        db.Run("CREATE TABLE IF NOT EXISTS currencies(code TEXT PRIMARY KEY COLLATE NOCASE,name_zh TEXT NOT NULL,name_ja TEXT NOT NULL,name_en TEXT NOT NULL,digits INTEGER NOT NULL CHECK(digits BETWEEN 0 AND 4),sort_order INTEGER NOT NULL,active INTEGER NOT NULL DEFAULT 1 CHECK(active IN (0,1)));");
        for(int i=0;i<BuiltIns.Length;i++) {
            string[] value=BuiltIns[i];
            db.Run("INSERT OR IGNORE INTO currencies(code,name_zh,name_ja,name_en,digits,sort_order,active) VALUES("+LedgerDb.Q(value[0])+","+LedgerDb.Q(value[1])+","+LedgerDb.Q(value[2])+","+LedgerDb.Q(value[3])+","+value[4]+","+i+",1)");
        }
        DataTable unknown=db.Run("SELECT DISTINCT currency FROM entries WHERE currency NOT IN (SELECT code FROM currencies) ORDER BY currency");
        int unknownOrder=1000;
        foreach(DataRow row in unknown.Rows) {
            string code=row[0].ToString().ToUpperInvariant();int order=unknownOrder++;
            db.Run("INSERT OR IGNORE INTO currencies(code,name_zh,name_ja,name_en,digits,sort_order,active) VALUES("+LedgerDb.Q(code)+","+LedgerDb.Q(code)+","+LedgerDb.Q(code)+","+LedgerDb.Q(code)+",2,"+order+",1)");
        }
        Reload(db);
    }

    public static void Reload(LedgerDb db) {
        items.Clear();activeCodes.Clear();
        DataTable table=db.Run("SELECT code,name_zh,name_ja,name_en,digits,sort_order,active FROM currencies ORDER BY active DESC,sort_order,code");
        foreach(DataRow row in table.Rows) {
            var item=new CurrencyDefinition{Code=row[0].ToString(),NameZh=row[1].ToString(),NameJa=row[2].ToString(),NameEn=row[3].ToString(),Digits=Int32.Parse(row[4].ToString(),CultureInfo.InvariantCulture),Order=Int32.Parse(row[5].ToString(),CultureInfo.InvariantCulture),Active=row[6].ToString()=="1"};
            items[item.Code]=item;if(item.Active)activeCodes.Add(item.Code);
        }
    }

    public static string[] ActiveCodes { get { return activeCodes.ToArray(); } }
    public static CurrencyDefinition Get(string code) { CurrencyDefinition value;return code!=null&&items.TryGetValue(code,out value)?value:null; }
    public static int Digits(string code) { CurrencyDefinition value=Get(code);return value==null?2:value.Digits; }
    public static string Display(string code,int language) { CurrencyDefinition value=Get(code);return value==null?code:code+" "+value.Name(language); }
    public static bool IsActive(string code) { CurrencyDefinition value=Get(code);return value!=null&&value.Active; }

    public static void SaveOrder(LedgerDb db,IList<string> codes) {
        StringBuilderCompat sql=new StringBuilderCompat();
        for(int i=0;i<codes.Count;i++)sql.Append("UPDATE currencies SET sort_order="+i+" WHERE code="+LedgerDb.Q(codes[i])+";");
        db.Run(sql.ToString());Reload(db);
    }

    public static void Move(LedgerDb db,string code,int direction) {
        var codes=new List<string>(activeCodes);int index=codes.FindIndex(delegate(string value){return String.Equals(value,code,StringComparison.OrdinalIgnoreCase);});int target=index+direction;
        if(index<0||target<0||target>=codes.Count)return;string swap=codes[target];codes[target]=codes[index];codes[index]=swap;SaveOrder(db,codes);
    }

    public static void SetDefault(LedgerDb db,string code) {
        var codes=new List<string>(activeCodes);codes.RemoveAll(delegate(string value){return String.Equals(value,code,StringComparison.OrdinalIgnoreCase);});codes.Insert(0,code);SaveOrder(db,codes);
    }

    public static void Deactivate(LedgerDb db,string code) {
        if(activeCodes.Count<=1)throw new Exception(L.T("至少需要保留一个币种。"));
        db.Run("UPDATE currencies SET active=0 WHERE code="+LedgerDb.Q(code));Reload(db);SaveOrder(db,new List<string>(activeCodes));
    }

    public static bool AddOrReactivate(LedgerDb db,string code,string zh,string ja,string en,int digits) {
        code=code.ToUpperInvariant();CurrencyDefinition existing=Get(code);
        if(existing!=null&&existing.Active)throw new Exception(L.T("该币种代码已存在。"));
        if(existing!=null)db.Run("UPDATE currencies SET active=1,sort_order="+activeCodes.Count+" WHERE code="+LedgerDb.Q(code));
        else db.Run("INSERT INTO currencies(code,name_zh,name_ja,name_en,digits,sort_order,active) VALUES("+LedgerDb.Q(code)+","+LedgerDb.Q(zh)+","+LedgerDb.Q(ja)+","+LedgerDb.Q(en)+","+digits+","+activeCodes.Count+",1)");
        Reload(db);return existing!=null;
    }
}

// Keeps the project compatible with the older compiler without exposing StringBuilder in the public API.
sealed class StringBuilderCompat {
    readonly System.Text.StringBuilder value=new System.Text.StringBuilder();
    public void Append(string text) { value.Append(text); }
    public override string ToString() { return value.ToString(); }
}

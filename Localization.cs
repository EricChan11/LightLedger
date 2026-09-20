using System;
using System.IO;
using System.Globalization;

public static class L {
    public static int Index=0;
    public static readonly string[] Categories={"餐饮","交通","购物","住房","娱乐","医疗","工资","奖金","其他"};
    static readonly System.Collections.Generic.Dictionary<string,string[]> Texts=new System.Collections.Generic.Dictionary<string,string[]> {
        {"轻记账", new string[]{"轻记账","かんたん家計簿","Light Ledger"}},
        {"轻记账 · 本地多币种账本", new string[]{"轻记账 · 本地多币种账本","かんたん家計簿 · ローカル多通貨帳簿","Light Ledger · Local Multi-currency Ledger"}},
        {"本地保存  /  多币种  /  离线可用", new string[]{"本地保存  /  多币种  /  离线可用","ローカル保存 / 多通貨 / オフライン","Local storage / Multi-currency / Offline"}},
        {"全部月份", new string[]{"全部月份","すべての月","All months"}},
        {"显示记录时间", new string[]{"显示记录时间","記録時刻を表示","Show record times"}},
        {"全部币种", new string[]{"全部币种","すべての通貨","All currencies"}},
        {"搜索分类 / 备注", new string[]{"搜索分类 / 备注","分類 / メモを検索","Search category / note"}},
        {"导出 CSV", new string[]{"导出 CSV","CSV 出力","Export CSV"}},
        {"备份数据库", new string[]{"备份数据库","DB バックアップ","Back up database"}},
        {"备份到 Google Drive", new string[]{"备份到 Google Drive","Google Drive にバックアップ","Back up to Google Drive"}},
        {"请选择 Google Drive 桌面版正在同步的文件夹。", new string[]{"请选择 Google Drive 桌面版正在同步的文件夹。","Google Drive デスクトップ版が同期しているフォルダーを選択してください。","Select a folder synced by Google Drive for desktop."}},
        {"找不到已设置的 Google Drive 文件夹。", new string[]{"找不到已设置的 Google Drive 文件夹。","設定された Google Drive フォルダーが見つかりません。","The configured Google Drive folder could not be found."}},
        {"轻记账备份-", new string[]{"轻记账备份-","かんたん家計簿バックアップ-","light-ledger-backup-"}},
        {"备份到以下 Google Drive 文件夹？\n", new string[]{"备份到以下 Google Drive 文件夹？\n","次の Google Drive フォルダーにバックアップしますか？\n","Back up to this Google Drive folder?\n"}},
        {"选择“否”可以更换文件夹。", new string[]{"选择“否”可以更换文件夹。","「いいえ」を選ぶとフォルダーを変更できます。","Choose No to select a different folder."}},
        {"Google Drive 备份已创建：\n", new string[]{"Google Drive 备份已创建：\n","Google Drive バックアップを作成しました：\n","Google Drive backup created:\n"}},
        {"Google Drive 桌面版会负责将此文件同步到云端。", new string[]{"Google Drive 桌面版会负责将此文件同步到云端。","Google Drive デスクトップ版がこのファイルをクラウドに同期します。","Google Drive for desktop will sync this file to the cloud."}},
        {"Google Drive 备份已创建：", new string[]{"Google Drive 备份已创建：","Google Drive バックアップ作成済み：","Google Drive backup created: "}},
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
        {"创建时间", new string[]{"创建时间","作成日時","Created"}},
        {"修改时间", new string[]{"修改时间","更新日時","Updated"}},
        {"未知", new string[]{"未知","不明","Unknown"}},
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

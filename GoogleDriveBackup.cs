using System;
using System.IO;
using System.Globalization;
using System.Windows.Forms;

public static class GoogleDriveBackup {
    static string SettingsPath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data","google-drive-folder.txt"); } }

    public static string LoadFolder() {
        try { return File.Exists(SettingsPath)?File.ReadAllText(SettingsPath).Trim():""; }
        catch(IOException) { return ""; }
        catch(UnauthorizedAccessException) { return ""; }
    }

    public static void SaveFolder(string folder) {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
        File.WriteAllText(SettingsPath,Path.GetFullPath(folder));
    }

    public static string ChooseFolder(IWin32Window owner,string currentFolder) {
        using(var dialog=new FolderBrowserDialog()) {
            dialog.Description=L.T("请选择 Google Drive 桌面版正在同步的文件夹。");
            dialog.ShowNewFolderButton=true;
            if(Directory.Exists(currentFolder))dialog.SelectedPath=currentFolder;
            return dialog.ShowDialog(owner)==DialogResult.OK?dialog.SelectedPath:"";
        }
    }

    public static string CreateBackup(LedgerDb db,string folder,DateTime now) {
        if(String.IsNullOrWhiteSpace(folder)||!Directory.Exists(folder))throw new DirectoryNotFoundException(L.T("找不到已设置的 Google Drive 文件夹。"));
        string baseName=L.T("轻记账备份-")+now.ToString("yyyyMMdd-HHmmss",CultureInfo.InvariantCulture);
        string path=Path.Combine(folder,baseName+".db");
        int suffix=2;
        while(File.Exists(path))path=Path.Combine(folder,baseName+"-"+(suffix++)+".db");
        db.Backup(path);
        return path;
    }
}

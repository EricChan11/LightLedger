using System;
using System.Data;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

public sealed class CurrencyManagerDialog : Form {
    readonly LedgerDb db;
    readonly DataGridView grid=new DataGridView();
    public bool Changed { get; private set; }

    public CurrencyManagerDialog(LedgerDb database) {
        db=database;Text=L.T("管理币种");Size=new Size(900,560);MinimumSize=new Size(760,430);StartPosition=FormStartPosition.CenterParent;Font=new Font("Microsoft YaHei UI",10);BackColor=Color.FromArgb(244,246,249);
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(16)};root.RowStyles.Add(new RowStyle(SizeType.Absolute,50));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,88));Controls.Add(root);
        root.Controls.Add(new Label{Text=L.T("第一行是新建账目时的默认币种。删除只会从选择列表隐藏，不会删除已有账目。"),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=Color.DimGray},0,0);
        ConfigureGrid();root.Controls.Add(grid,0,1);
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(0,10,0,0)};
        buttons.Controls.Add(Button(L.T("添加币种"),AddCurrency));buttons.Controls.Add(Button(L.T("删除币种"),DeleteCurrency));buttons.Controls.Add(Button(L.T("上移"),delegate{MoveSelection(-1);}));buttons.Controls.Add(Button(L.T("下移"),delegate{MoveSelection(1);}));
        var makeDefault=Button(L.T("设为默认"),SetDefault);makeDefault.BackColor=Color.FromArgb(25,111,95);makeDefault.ForeColor=Color.White;buttons.Controls.Add(makeDefault);buttons.Controls.Add(Button(L.T("关闭"),delegate{Close();}));root.Controls.Add(buttons,0,2);
        RefreshGrid("");
    }

    Button Button(string text,Action action) { var value=new Button{Text=text,AutoSize=true,Height=32,FlatStyle=FlatStyle.Flat,BackColor=Color.White,Margin=new Padding(0,0,8,0)};value.Click+=delegate{try{action();}catch(Exception ex){MessageBox.Show(this,ex.Message,L.T("操作未完成"),MessageBoxButtons.OK,MessageBoxIcon.Warning);}};return value; }
    void ConfigureGrid() {
        grid.Dock=DockStyle.Fill;grid.ReadOnly=true;grid.AllowUserToAddRows=false;grid.AllowUserToDeleteRows=false;grid.RowHeadersVisible=false;grid.BackgroundColor=Color.White;grid.BorderStyle=BorderStyle.None;grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;grid.MultiSelect=false;grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;grid.RowTemplate.Height=32;grid.ColumnHeadersHeight=38;
        foreach(string name in new string[]{L.T("默认"),L.T("币种代码"),L.T("中文名称"),L.T("日文名称"),L.T("英文名称"),L.T("小数位数")})grid.Columns.Add(name,name);
        grid.Columns[0].FillWeight=45;grid.Columns[1].FillWeight=60;grid.Columns[2].FillWeight=90;grid.Columns[3].FillWeight=100;grid.Columns[4].FillWeight=120;grid.Columns[5].FillWeight=55;
    }
    void RefreshGrid(string selectedCode) {
        grid.Rows.Clear();string[] codes=CurrencyCatalog.ActiveCodes;
        for(int i=0;i<codes.Length;i++) { CurrencyDefinition item=CurrencyCatalog.Get(codes[i]);int row=grid.Rows.Add(i==0?"✓":"",item.Code,item.NameZh,item.NameJa,item.NameEn,item.Digits);if(String.Equals(item.Code,selectedCode,StringComparison.OrdinalIgnoreCase))grid.Rows[row].Selected=true; }
        if(grid.SelectedRows.Count==0&&grid.Rows.Count>0)grid.Rows[0].Selected=true;
    }
    string SelectedCode() { if(grid.SelectedRows.Count==0)throw new Exception(L.T("请先选择一个币种。"));return grid.SelectedRows[0].Cells[1].Value.ToString(); }
    void AddCurrency() {
        using(var dialog=new AddCurrencyDialog())if(dialog.ShowDialog(this)==DialogResult.OK) {
            bool restored=CurrencyCatalog.AddOrReactivate(db,dialog.Code,dialog.NameZh,dialog.NameJa,dialog.NameEn,dialog.Digits);Changed=true;RefreshGrid(dialog.Code);
            if(restored)MessageBox.Show(this,L.T("已恢复之前删除的币种及其原有设置。"),L.T("币种已恢复"),MessageBoxButtons.OK,MessageBoxIcon.Information);
        }
    }
    void DeleteCurrency() {
        string code=SelectedCode();string message=L.T("从选择列表中删除这个币种？")+Environment.NewLine+L.Currency(code)+Environment.NewLine+Environment.NewLine+L.T("已有账目会保留，并继续显示原币种。以后可通过添加相同代码恢复。");
        if(MessageBox.Show(this,message,L.T("删除币种"),MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
        CurrencyCatalog.Deactivate(db,code);Changed=true;RefreshGrid("");
    }
    void MoveSelection(int direction) { string code=SelectedCode();CurrencyCatalog.Move(db,code,direction);Changed=true;RefreshGrid(code); }
    void SetDefault() { string code=SelectedCode();CurrencyCatalog.SetDefault(db,code);Changed=true;RefreshGrid(code); }
}

public sealed class AddCurrencyDialog : Form {
    readonly TextBox code=new TextBox(),zh=new TextBox(),ja=new TextBox(),en=new TextBox();readonly NumericUpDown digits=new NumericUpDown();
    public string Code { get { return code.Text.Trim().ToUpperInvariant(); } }
    public string NameZh { get { return zh.Text.Trim(); } }
    public string NameJa { get { return ja.Text.Trim(); } }
    public string NameEn { get { return en.Text.Trim(); } }
    public int Digits { get { return Decimal.ToInt32(digits.Value); } }

    public AddCurrencyDialog() {
        Text=L.T("添加币种");Size=new Size(460,390);MinimumSize=MaximumSize=Size;StartPosition=FormStartPosition.CenterParent;Font=new Font("Microsoft YaHei UI",10);BackColor=Color.FromArgb(244,246,249);MaximizeBox=false;MinimizeBox=false;
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=7,Padding=new Padding(18)};root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,125));root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));Controls.Add(root);
        AddRow(root,0,L.T("币种代码"),code);AddRow(root,1,L.T("中文名称"),zh);AddRow(root,2,L.T("日文名称"),ja);AddRow(root,3,L.T("英文名称"),en);digits.Minimum=0;digits.Maximum=4;digits.Value=2;AddRow(root,4,L.T("小数位数"),digits);
        root.Controls.Add(new Label{Text=L.T("代码使用三个英文字母，例如 NOK。"),AutoSize=true,ForeColor=Color.DimGray,Margin=new Padding(3,8,3,3)},1,5);
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill};var add=new Button{Text=L.T("添加"),AutoSize=true,Height=32,BackColor=Color.FromArgb(25,111,95),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};add.Click+=delegate{ValidateAndClose();};buttons.Controls.Add(add);var cancel=new Button{Text=L.T("取消"),AutoSize=true,Height=32,FlatStyle=FlatStyle.Flat};cancel.Click+=delegate{DialogResult=DialogResult.Cancel;Close();};buttons.Controls.Add(cancel);root.Controls.Add(buttons,1,6);
        AcceptButton=add;CancelButton=cancel;
    }
    static void AddRow(TableLayoutPanel root,int row,string label,Control control) { root.RowStyles.Add(new RowStyle(SizeType.Absolute,43));root.Controls.Add(new Label{Text=label,AutoSize=true,Margin=new Padding(3,7,3,3)},0,row);control.Dock=DockStyle.Top;root.Controls.Add(control,1,row); }
    void ValidateAndClose() {
        if(!Regex.IsMatch(Code,"^[A-Z]{3}$")) { MessageBox.Show(this,L.T("币种代码必须是三个英文字母。"),L.T("输入无效"));return; }
        if(NameZh==""||NameJa==""||NameEn=="") { MessageBox.Show(this,L.T("请填写三种语言的币种名称。"),L.T("输入无效"));return; }
        DialogResult=DialogResult.OK;Close();
    }
}

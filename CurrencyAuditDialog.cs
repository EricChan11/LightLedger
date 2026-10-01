using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

public static class CurrencyAuditService {
    public static DataTable FindCandidates(LedgerDb db) {
        return db.Run("SELECT id,day,kind,amount,currency,category,note FROM entries WHERE currency IN ('CNY','HKD') AND amount>10000 ORDER BY day DESC,id DESC");
    }

    public static int ConvertToJpy(LedgerDb db,List<long> ids,string updatedAt) {
        if(ids.Count==0)return 0;
        StringBuilder idList=new StringBuilder();
        foreach(long id in ids) { if(idList.Length>0)idList.Append(','); idList.Append(id.ToString(CultureInfo.InvariantCulture)); }
        db.Run("UPDATE entries SET amount=amount/100,currency='JPY',updated_at="+LedgerDb.Q(updatedAt)+" WHERE id IN ("+idList+") AND currency IN ('CNY','HKD') AND amount>10000 AND amount%100=0");
        DataTable changed=db.Run("SELECT changes()");
        return changed.Rows.Count==0?0:Int32.Parse(changed.Rows[0][0].ToString(),CultureInfo.InvariantCulture);
    }
}

public sealed class CurrencyAuditDialog : Form {
    readonly LedgerDb db;
    readonly DataTable candidates;
    readonly DataGridView grid=new DataGridView();
    public bool Changed { get; private set; }
    public bool HasCandidates { get { return candidates.Rows.Count>0; } }

    public CurrencyAuditDialog(LedgerDb database) {
        db=database; candidates=CurrencyAuditService.FindCandidates(db);
        Text=L.T("疑似日元记录"); Size=new Size(1060,600); MinimumSize=new Size(850,450); StartPosition=FormStartPosition.CenterParent; Font=new Font("Microsoft YaHei UI",10); BackColor=Color.FromArgb(244,246,249);
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(16)};
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,58));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,52));Controls.Add(root);
        root.Controls.Add(new Label{Text=L.T("检测规则：人民币或港币单笔金额超过 100。含小数的记录需要手动编辑。"),Dock=DockStyle.Fill,AutoSize=false,ForeColor=Color.DimGray,TextAlign=ContentAlignment.MiddleLeft},0,0);
        ConfigureGrid(); root.Controls.Add(grid,0,1);
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight,Padding=new Padding(0,10,0,0)};
        buttons.Controls.Add(MakeButton(L.T("全选"),SelectAll));buttons.Controls.Add(MakeButton(L.T("取消全选"),ClearAll));
        var convert=MakeButton(L.T("一键改为 JPY 日元"),ConvertSelected);convert.BackColor=Color.FromArgb(25,111,95);convert.ForeColor=Color.White;buttons.Controls.Add(convert);
        buttons.Controls.Add(MakeButton(L.T("关闭"),delegate{Close();}));root.Controls.Add(buttons,0,2);
        Populate();
    }

    Button MakeButton(string text,Action action) {
        var button=new Button{Text=text,AutoSize=true,Height=32,FlatStyle=FlatStyle.Flat,BackColor=Color.White,Margin=new Padding(0,0,8,0)};
        button.Click+=delegate{try{action();}catch(Exception ex){MessageBox.Show(this,ex.Message,L.T("操作未完成"),MessageBoxButtons.OK,MessageBoxIcon.Warning);}};return button;
    }

    void ConfigureGrid() {
        grid.Dock=DockStyle.Fill;grid.AllowUserToAddRows=false;grid.AllowUserToDeleteRows=false;grid.RowHeadersVisible=false;grid.BackgroundColor=Color.White;grid.BorderStyle=BorderStyle.None;grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;grid.MultiSelect=false;grid.RowTemplate.Height=32;grid.ColumnHeadersHeight=38;
        grid.Columns.Add(new DataGridViewCheckBoxColumn{Name="pick",HeaderText=L.T("选择"),Width=55,FillWeight=45});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="id",HeaderText="ID",Visible=false});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="day",HeaderText=L.T("日期"),FillWeight=75,ReadOnly=true});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="kind",HeaderText=L.T("类型"),FillWeight=65,ReadOnly=true});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="amount",HeaderText=L.T("金额"),FillWeight=70,ReadOnly=true});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="currency",HeaderText=L.T("当前币种"),FillWeight=105,ReadOnly=true});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="category",HeaderText=L.T("分类"),FillWeight=75,ReadOnly=true});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="note",HeaderText=L.T("备注"),FillWeight=150,ReadOnly=true});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="result",HeaderText=L.T("检查结果"),FillWeight=130,ReadOnly=true});
        grid.Columns.Add(new DataGridViewTextBoxColumn{Name="minor",HeaderText="minor",Visible=false});
    }

    void Populate() {
        foreach(DataRow row in candidates.Rows) {
            long minor=Int64.Parse(row[3].ToString(),CultureInfo.InvariantCulture);bool eligible=minor%100==0;
            int index=grid.Rows.Add(false,row[0],row[1],L.T(row[2].ToString()),((decimal)minor/100m).ToString("N2",CultureInfo.CurrentCulture),L.Currency(row[4].ToString()),L.CategoryText(row[5].ToString()),row[6],L.T(eligible?"可批量转换":"含小数，请手动编辑"),minor);
            if(!eligible) { grid.Rows[index].Cells[0].ReadOnly=true;grid.Rows[index].DefaultCellStyle.ForeColor=Color.DimGray;grid.Rows[index].DefaultCellStyle.BackColor=Color.FromArgb(245,245,245); }
        }
    }

    void SelectAll() { foreach(DataGridViewRow row in grid.Rows)if(!row.Cells[0].ReadOnly)row.Cells[0].Value=true; }
    void ClearAll() { foreach(DataGridViewRow row in grid.Rows)row.Cells[0].Value=false; }

    void ConvertSelected() {
        grid.EndEdit(); List<long> ids=new List<long>();
        foreach(DataGridViewRow row in grid.Rows)if(Convert.ToBoolean(row.Cells[0].Value))ids.Add(Int64.Parse(row.Cells[1].Value.ToString(),CultureInfo.InvariantCulture));
        if(ids.Count==0)throw new Exception(L.T("请至少选择一条可转换的记录。"));
        string message=String.Format(CultureInfo.CurrentCulture,L.T("将选中的 {0} 笔记录改为 JPY 日元？"),ids.Count)+Environment.NewLine+L.T("金额数字保持不变，只修改币种；此操作会更新修改时间。");
        if(MessageBox.Show(this,message,L.T("确认批量修改"),MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
        string now=DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture);int changed=CurrencyAuditService.ConvertToJpy(db,ids,now);
        if(changed!=ids.Count)throw new Exception(L.T("部分记录已发生变化，请重新检查后再试。"));
        Changed=true;MessageBox.Show(this,String.Format(CultureInfo.CurrentCulture,L.T("已将 {0} 笔记录改为 JPY 日元。"),changed),L.T("修改完成"),MessageBoxButtons.OK,MessageBoxIcon.Information);DialogResult=DialogResult.OK;Close();
    }
}

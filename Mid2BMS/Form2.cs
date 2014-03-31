using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Mid2BMS
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            RedoRequired = false;
            InitializeComponent();
        }

        DataSet data_set;
        DataTable data_table;

        public String TrackName_csv { get; set; }  // 親フォーム(Form1)から値を受け取る
        public List<String> TrackNames { get; set; }
        public List<String> InstrumentNames { get; set; }
        public List<bool> IsDrumsList { get; set; }
        public List<bool> IgnoreList { get; set; }
        public List<bool> IsChordList { get; set; }
        public bool RedoRequired { get; private set; }  // 初期値はfalseかな？
        bool changeEnabled = false;

        private void button1_Click(object sender, EventArgs e)
        {
            if (changeEnabled == false)
            {
                // 1回目のクリック「終了」
                this.Close();
            }
            else
            {
                RedoRequired = true;

                TrackNames = new List<String>(TrackNames);

                IsDrumsList = new List<bool>(TrackNames.Select(x => false));  // この書き方良くないけど良い(?)
                IgnoreList = new List<bool>(TrackNames.Select(x => false));  // この書き方良くないけど良い(?)
                IsChordList = new List<bool>(TrackNames.Select(x => false));  // この書き方良くないけど良い(?)
                // というかtrueでもfalseでもいいんですよ
                // こういう点を見ると、scilabのzeros(x)とかones(x)って便利だなって思う

                for (int i = 0; i < data_table.Rows.Count; i++)
                {
                    int tracknumber = (System.Int32)(data_table.Rows[i][0]);
                    TrackNames[tracknumber] = data_table.Rows[i][5].ToString();
                    IsDrumsList[tracknumber] = (bool)data_table.Rows[i][6];
                    IgnoreList[tracknumber] = (bool)data_table.Rows[i][7];
                    IsChordList[tracknumber] = (bool)data_table.Rows[i][8];
                }

                //MessageBox.Show("ねこ");

                this.Close();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (changeEnabled == false)
            {
                // 1回目のクリック「変更する」
                button1.Text = "Apply Changes (適用)";
                button2.Text = "Cancel (キャンセル)";
                changeEnabled = true;
                SetTable(true);
            }
            else
            {
                // 2回目のクリック「キャンセル」
                if (MessageBox.Show(this, "変更を取り消して操作を完了しますか？", "Cancel and Close", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {
                    this.Close();
                }
            }

            return;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            SetTable(false);
        }
        private void SetTable(bool showDetail) {
            // ん、datagridとdatagridviewって違うのか
            // http://msdn.microsoft.com/ja-jp/library/ms171628(v=vs.110).aspx


            //ヘッダーとすべてのセルの内容に合わせて、列の幅を自動調整する
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            //ヘッダーとすべてのセルの内容に合わせて、行の高さを自動調整する
            //dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;


            // データセットの作成
            data_set = new DataSet("default_set");

            // データテーブルの作成
            data_table = new DataTable("default_table");

            // データテーブルをデータセットに登録
            data_set.Tables.Add(data_table);

            // データテーブルにカラムを作成・登録
            data_table.Columns.Add("Tr", Type.GetType("System.Int32"));  // 数字の順にソート出来るようにする
            data_table.Columns.Add("wavs", Type.GetType("System.Int32"));
            data_table.Columns.Add("notes", Type.GetType("System.Int32"));
            if (showDetail)
            {
                data_table.Columns.Add("TrackName", Type.GetType("System.String"));
                data_table.Columns.Add("InstName", Type.GetType("System.String"));
                data_table.Columns.Add("New TrackName (Edit This Col)", Type.GetType("System.String"));
                data_table.Columns.Add("Drums?", Type.GetType("System.Boolean"));
                data_table.Columns.Add("Ignore?", Type.GetType("System.Boolean"));
                data_table.Columns.Add("Chord?", Type.GetType("System.Boolean"));
                /*{
                    var cs = new DataGridBoolColumn();
                    cs.MappingName = "Drums?";
                    cs.AllowNull = false;
                    data_table.Columns.Add(cs);//, Type.GetType("System.Boolean"));
                }*/
            }
            else
            {
                data_table.Columns.Add("TrackName", Type.GetType("System.String"));
            }

            // データテーブルのプライマリーキー（主キー）を設定
            data_table.PrimaryKey = new DataColumn[] { data_table.Columns[0] };

            String[] myrows = (TrackName_csv ?? "").Split(new String[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            DataRow data_row;

            for (int rowi = 0; rowi < myrows.Length - 2; rowi++)
            {
                String[] mycells = myrows[rowi + 2].Split(new String[] { "\t" }, StringSplitOptions.None);

                if (Convert.ToInt32(mycells[2]) != 0)  // ノート数が0なら無視する。従ってコンダクタートラックも無視され、曲名の変更は出来ません
                {
                    // データ行の作成とテーブルへの登録　その１
                    data_row = data_table.NewRow();
                    data_row[0] = Convert.ToInt32(mycells[0]);  // Convert.ToInt32は無くてもおｋ？
                    data_row[1] = Convert.ToInt32(mycells[1]);
                    data_row[2] = Convert.ToInt32(mycells[2]);
                    if (showDetail)
                    {
                        data_row[3] = TrackNames[rowi];
                        data_row[4] = InstrumentNames[rowi];
                        data_row[5] = mycells[3];
                        data_row[6] = false;
                        data_row[7] = false;
                        data_row[8] = false;
                    }
                    else
                    {
                        data_row[3] = mycells[3];
                    }
                    data_table.Rows.Add(data_row);
                }
            }

            // DataGridViewにデータセットを設定
            dataGridView1.DataMember = data_set.Tables[0].TableName;
            dataGridView1.DataSource = data_set;

            // ファイル名だけ編集可能にする
            dataGridView1.Columns[0].ReadOnly = true;
            dataGridView1.Columns[1].ReadOnly = true;
            dataGridView1.Columns[2].ReadOnly = true;

            if (showDetail)
            {
                dataGridView1.Columns[3].ReadOnly = true;
                dataGridView1.Columns[4].ReadOnly = true;
                dataGridView1.Columns[5].ReadOnly = false;
                dataGridView1.Columns[6].ReadOnly = false;
                dataGridView1.Columns[7].ReadOnly = false;
                dataGridView1.Columns[8].ReadOnly = false;
            }
            else
            {
                dataGridView1.Columns[3].ReadOnly = true;
            }
            
            
        }
    }
}

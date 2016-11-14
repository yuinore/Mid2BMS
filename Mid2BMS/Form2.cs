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

        //**************************************************
        //********** 無効なチェックの組み合わせ ************
        // Chord + Drums は不可 (意味がないため)
        // Ignore + その他 は不可 (Ignoreが優先されるため)
        // XChain + その他 は不可 (サイドチェイン以外は無視されるため)
        // Purple + Chord は不可 (ポルタメントを適用する順序が一意でないため)
        // XChain は RedMode かつシーケンスレイヤーの場合のみ可
        // RedMode + OneShot は意味が無いので一応不可
        //**************************************************

        //################ 入出力パラメータ ################
        public String TrackName_csv { get; set; }  // 親フォーム(Form1)から値を受け取る
        public List<String> TrackNames { get; set; }
        public List<String> InstrumentNames { get; set; }
        public List<bool> IsDrumsList { get; set; }
        public List<bool> IgnoreList { get; set; }
        public List<bool> IsChordList { get; set; }
        public List<bool> IsXChainList { get; set; }  // RedModeとシーケンスレイヤーの両方が（？）選択されている場合に、サイドチェイントリガノーツとして扱う
        public List<bool> IsOneShotList { get; set; }  // Midiノーツの長さを無視して処理する

        //################# 出力パラメータ #################
        public bool RedoRequired { get; private set; }  // 初期値はfalseかな？

        //################# 入力パラメータ #################
        private bool IsSequenceLayer;
        private bool IsRedMode;
        private bool IsPurpleMode;

        //##################################################

        bool changeEnabled = false;

        readonly int COLUMN_DRUMS = 6;  // 順序変更有り、また、この数値のみを変更しないこと
        readonly int COLUMN_ONESHOT = 7;
        readonly int COLUMN_CHORD = 8;
        readonly int COLUMN_IGNORE = 9;
        readonly int COLUMN_XCHAIN = 10;
        
        public void SetMode(bool isSequenceLayer, bool isRedMode, bool isPurpleMode)
        {
            // private get; set; は許されるのか！？ いや、許されない気がする・・・
            IsSequenceLayer = isSequenceLayer;
            IsRedMode = isRedMode;
            IsPurpleMode = isPurpleMode;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (changeEnabled == false)
            {
                // 1回目のクリック「終了」
                this.Close();
            }
            else
            {
                //######## パラメータの妥当性のチェック ########
                for (int i = 0; i < data_table.Rows.Count; i++)
                {
                    bool isDrums = (bool)data_table.Rows[i][COLUMN_DRUMS];
                    bool isOneShot = (bool)data_table.Rows[i][COLUMN_ONESHOT];
                    bool isChord = (bool)data_table.Rows[i][COLUMN_CHORD];
                    bool ignore = (bool)data_table.Rows[i][COLUMN_IGNORE];
                    bool isXChain = (bool)data_table.Rows[i][COLUMN_XCHAIN];

                    if (isChord && isDrums)
                    {
                        MessageBox.Show(this, "Chord? と Drums? を同時にチェックすることはできません。設定を確認してください。", "Invalid Parameter");
                        return;
                    }

                    if (ignore && (isOneShot || isChord || isDrums || isXChain))
                    {
                        MessageBox.Show(this, "Ignore? がチェックされる場合、これは単独でチェックされなければなりません。設定を確認してください。", "Invalid Parameter");
                        return;
                    }

                    if (isXChain && (isOneShot || isChord || isDrums))
                    {
                        MessageBox.Show(this, "XChain? がチェックされる場合、これは単独でチェックされなければなりません。設定を確認してください。", "Invalid Parameter");
                        return;
                    }
                }

                //######## 呼び出し元(Form1)への戻り値の設定 ########
                RedoRequired = true;

                TrackNames = new List<String>(TrackNames);

                // http://stackoverflow.com/questions/3363940/fill-listint-with-default-values
                // Enumerable.Repeatよりもこの方が僅かに速いらしい

                IsDrumsList = new List<bool>(new bool[TrackNames.Count]);
                IgnoreList = new List<bool>(new bool[TrackNames.Count]);
                IsChordList = new List<bool>(new bool[TrackNames.Count]);
                IsXChainList = new List<bool>(new bool[TrackNames.Count]);
                IsOneShotList = new List<bool>(new bool[TrackNames.Count]);

                for (int i = 0; i < data_table.Rows.Count; i++)
                {
                    int tracknumber = (System.Int32)(data_table.Rows[i][0]);
                    TrackNames[tracknumber] = data_table.Rows[i][5].ToString();
                    IsDrumsList[tracknumber] = (bool)data_table.Rows[i][6];
                    IsOneShotList[tracknumber] = (bool)data_table.Rows[i][7];  // 順序変更有り
                    IsChordList[tracknumber] = (bool)data_table.Rows[i][8];
                    IgnoreList[tracknumber] = (bool)data_table.Rows[i][9];
                    IsXChainList[tracknumber] = (bool)data_table.Rows[i][10];
                }

                //######## フォームを閉じる ########
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
                data_table.Columns.Add("OheShot?", Type.GetType("System.Boolean"));  // 順序変更有り
                data_table.Columns.Add("Chord?", Type.GetType("System.Boolean"));
                data_table.Columns.Add("Ignore?", Type.GetType("System.Boolean"));
                data_table.Columns.Add("XChain?", Type.GetType("System.Boolean"));
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
                        data_row[7] = false;  // 順序変更有り
                        data_row[8] = false;
                        data_row[9] = false;
                        data_row[10] = false;
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
                dataGridView1.Columns[7].ReadOnly = false;  // 順序変更有り
                dataGridView1.Columns[8].ReadOnly = false;  // Chord?
                dataGridView1.Columns[9].ReadOnly = false;
                dataGridView1.Columns[10].ReadOnly = false;

                dataGridView1.Columns[COLUMN_CHORD].Visible = !IsPurpleMode;
                dataGridView1.Columns[COLUMN_XCHAIN].Visible = IsRedMode && IsSequenceLayer;
                dataGridView1.Columns[COLUMN_ONESHOT].Visible = !IsRedMode;
            }
            else
            {
                dataGridView1.Columns[3].ReadOnly = true;
            }
            
            
        }
    }
}

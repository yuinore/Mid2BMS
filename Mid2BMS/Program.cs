using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Mid2BMS
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            {
                // 選択されているカルチャ情報を取得する
                String culturetext = "default";
                if (File.Exists("culture.ini"))
                {
                    culturetext = File.ReadAllText("culture.ini");
                }

                if (culturetext != "default")
                {
                    CultureInfo culture = (CultureInfo)CultureInfo.GetCultureInfo(culturetext);

                    try
                    {
                        // Windowsフォームのリソースに対応したUIカルチャを設定する
                        //Thread.CurrentThread.CurrentCulture = culture;
                        Thread.CurrentThread.CurrentUICulture = culture;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return;
                    }
                }
            }

            Application.Run(new Form1());
        }
    }
}

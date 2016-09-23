using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Mid2BMS
{
    static class ControlProperty
    {
        // フォームの内容を保存する
        //
        // [C#]コントロールの値をずばっとまるごと保存、展開する。
        // http://kimux.net/?p=360
        //
        // このコードではDynamicJsonを使用しています
        // http://dynamicjson.codeplex.com/

        /// <summary>
        /// Control's property.
        /// </summary>
        public struct Property
        {
            public string Name { get; set; }
            public dynamic Value { get; set; }
        }

        /// <summary>
        /// Tuple
        /// </summary>
        public struct DynamicTuple
        {
            public dynamic Item1 { get; set; }
            public dynamic Item2 { get; set; }
        }

        /// <summary>
        /// GetValue() Value is get.
        /// SetValue() Value is set;
        /// </summary>
        public struct GetSet
        {
            public Func<dynamic, dynamic> GetValue;
            public Action<dynamic, dynamic> SetValue;
        }

        public static Dictionary<Type, GetSet> GetTypeList()
        {
            /// Register Dictionary.
            /// SetValue for control type.
            /// GetValue for control type.

            /// ex)
            /// dic.Add( typeof(Control type), 
            ///     new GetSet() { GetValue = (p) => (Control setter),
            ///                    SetValue = (p) => (Control getter) });

            var dic = new Dictionary<Type, GetSet>();

            dic.Add(typeof(TextBox), new GetSet() { GetValue = (p) => p.Text, SetValue = (p, v) => p.Text = v });
            dic.Add(typeof(RadioButton), new GetSet() { GetValue = (p) => p.Checked, SetValue = (p, v) => p.Checked = v });
            // 変更
            // dic.Add(typeof(ComboBox), new GetSet() { GetValue = (p) => p.SelectedIndex, SetValue = (p, v) => p.SelectedIndex = (int)v });
            //dic.Add(typeof(ComboBox), new GetSet() { GetValue = (p) => p.Text, SetValue = (p, v) => p.Text = (string)v });
            dic.Add(typeof(ComboBox), new GetSet()
            {
                GetValue = (p) => ((p.DropDownStyle == ComboBoxStyle.DropDownList) ? p.SelectedIndex : p.Text),
                //SetValue = (p, v) => ((p.DropDownStyle == ComboBoxStyle.DropDownList) ? (p.Text = (int)v).ToString() : (p.Text = (string)v)).ToString()
                SetValue = (p, v) => ((p.DropDownStyle == ComboBoxStyle.DropDownList) ? (p.SelectedIndex = ((int)v)).ToString() : (p.Text = (string)v)).ToString()
            });
            dic.Add(typeof(DateTimePicker), new GetSet() { GetValue = (p) => p.Value, SetValue = (p, v) => p.Value = DateTime.Parse(v) });
            dic.Add(typeof(CheckBox), new GetSet() { GetValue = (p) => p.Checked, SetValue = (p, v) => p.Checked = v });
            // 削除
            //dic.Add(typeof(ListBox), new GetSet() { GetValue = (p) => p.SelectedIndex, SetValue = (p, v) => p.SelectedIndex = (int)v });
            dic.Add(typeof(TrackBar), new GetSet() { GetValue = (p) => p.Value, SetValue = (p, v) => p.Value = (int)v });
            dic.Add(typeof(VScrollBar), new GetSet() { GetValue = (p) => p.Value, SetValue = (p, v) => p.Value = (int)v });
            // 最近追加された項目
            dic.Add(typeof(TabControl), new GetSet() { GetValue = (p) => p.SelectedIndex, SetValue = (p, v) => p.SelectedIndex = (int)v });

            return dic;
        }

        /// <summary>
        /// Control value is get
        /// </summary>
        /// <param name="coll">Control collection</param>
        /// <returns>Control value</returns>
        public static Property[] Get(System.Windows.Forms.Control.ControlCollection coll)
        {
            var dic = GetTypeList();
            var ret = new List<Property>();

            /// Control scanning
            foreach (Control ctrl in coll)
            {
                /// Child control recurisve call
                if (ctrl.Controls.Count > 0)
                {
                    var c_ctrl = Get(ctrl.Controls);

                    ret.AddRange(c_ctrl);
                }
                /// know control, value is get
                //else  // 最近変更された箇所
                {
                    //if (dic.ContainsKey(ctrl.GetType()))
                    if (dic.ContainsKey(ctrl.GetType()) && (!(ctrl is TextBox) || (ctrl as TextBox).ReadOnly == false))
                    {
                        ret.Add(new Property() { Name = ctrl.Name, Value = dic[ctrl.GetType()].GetValue(ctrl) });
                    }
                }
            }

            return ret.ToArray();
        }

        /// <summary>
        /// Control value is set
        /// </summary>
        /// <param name="coll">Control collection</param>
        /// <param name="val">Configuration file of a value</param>
        public static void Set(System.Windows.Forms.Control.ControlCollection coll, Property[] val)
        {
            var dic = GetTypeList();
            var ret = new List<Property>();

            /// Control scanning 
            foreach (Control ctrl in coll)
            {
                /// Child control recursive call
                if (ctrl.Controls.Count > 0)
                {
                    Set(ctrl.Controls, val);
                }
                /// know control, value is set
                //else  // 最近変更された箇所
                {
                    //if (dic.ContainsKey(ctrl.GetType()))
                    if (dic.ContainsKey(ctrl.GetType()) && (!(ctrl is TextBox) || (ctrl as TextBox).ReadOnly == false))
                    {
                        var currentValue = val.Where(n => ((n.Name.Any()) && (n.Name == ctrl.Name)));
                        if (currentValue.Any())
                        {
                            dic[ctrl.GetType()].SetValue(ctrl, currentValue.First().Value);
                        }
                    }
                }
            }
        }
    }
}

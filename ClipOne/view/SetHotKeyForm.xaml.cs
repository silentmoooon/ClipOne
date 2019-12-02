using ClipOne.util;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClipOne.view
{
    /// <summary>
    /// SetRecords.xaml 的交互逻辑
    /// </summary>
    public partial class SetHotKeyForm : Window
    {

        /// <summary>
        /// 修饰键
        /// </summary>
        public int HotkeyModifier { get; set; }
        /// <summary>
        /// 按键
        /// </summary>
        public int HotkeyKey { get; set; }

        /// <summary>
        /// 当前应用句柄
        /// </summary>
        public IntPtr WpfHwnd { get; set; }

        /// <summary>
        /// 按键唯一因子
        /// </summary>
        public int HotkeyAtom { get; set; }

        private static Dictionary<int,List<CheckBox>> hotkeyCboMap=new Dictionary<int,List<CheckBox>>();

        public SetHotKeyForm()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
           
            if (cboKey.SelectedIndex == -1)
            {
                MessageBox.Show("请选择按键");
                return;
            }

            bool hasCheckBoxChecked = false;
            int tmpModifier=0;
            foreach (UIElement element in MainGrid.Children)
            {
                if (element is CheckBox)
                {
                    CheckBox cbo = (CheckBox)element;
                    if (cbo.IsChecked==true) { 
                        hasCheckBoxChecked = true;
                        tmpModifier = int.Parse(cbo.Tag.ToString()) | tmpModifier;
                    }
                }
            }
            if (!hasCheckBoxChecked)
            {
                MessageBox.Show("请选择修饰键");
                return;
            }
            ComboBoxItem item = cboKey.SelectedItem as ComboBoxItem;
            int   tmpHotkeyKey = (int)item.Tag;

            HotKeyManager.UnregisterHotKey(WpfHwnd, HotkeyAtom);
            bool status = HotKeyManager.RegisterHotKey(WpfHwnd, HotkeyAtom, tmpModifier, tmpHotkeyKey);
            if (!status)
            {
                //如果注册新热键失败，则重新注册旧热键
                HotKeyManager.RegisterHotKey(WpfHwnd, HotkeyAtom, HotkeyModifier, HotkeyKey);
                MessageBox.Show("热键注册失败，请重新设置！");
                return;
            }
            HotkeyModifier = tmpModifier;
            HotkeyKey = tmpHotkeyKey;
            this.DialogResult = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            int selectedIndex = -1;
            int i = 0;

            //初始化修饰键
            hotkeyCboMap[1]=new List<CheckBox>() { cboAlt};
            hotkeyCboMap[3]=new List<CheckBox>() { cboAlt,cboCtrl };
            hotkeyCboMap[5]=new List<CheckBox>() { cboAlt,cboShift };
            hotkeyCboMap[9]=new List<CheckBox>() { cboAlt,cboWin };
            hotkeyCboMap[7]=new List<CheckBox>() { cboAlt,cboCtrl,cboShift };
            hotkeyCboMap[11]=new List<CheckBox>() { cboAlt ,cboCtrl,cboWin};
            hotkeyCboMap[13]=new List<CheckBox>() { cboAlt,cboShift,cboWin };
            hotkeyCboMap[15]=new List<CheckBox>() { cboAlt ,cboCtrl,cboShift,cboWin};
            hotkeyCboMap[2]=new List<CheckBox>() { cboCtrl };
            hotkeyCboMap[6]=new List<CheckBox>() { cboCtrl, cboShift };
            hotkeyCboMap[10]=new List<CheckBox>() { cboCtrl, cboWin };
            hotkeyCboMap[14]=new List<CheckBox>() { cboCtrl, cboShift,cboWin};
            hotkeyCboMap[4]=new List<CheckBox>() { cboShift};
            hotkeyCboMap[12]=new List<CheckBox>() { cboShift,cboWin };
            hotkeyCboMap[8]=new List<CheckBox>() { cboWin };

            if (HotkeyModifier != 0)
            {
                foreach(CheckBox cb in hotkeyCboMap[HotkeyModifier])
                {
                    cb.IsChecked = true;
                }
            }
            //初始化按键

            foreach (Key key in Enum.GetValues(typeof( Key)))
            {
                
                if (KeyInterop.VirtualKeyFromKey(key) >= 65 && KeyInterop.VirtualKeyFromKey(key) <= 90) {
                    ComboBoxItem cbi = new ComboBoxItem();
                    
                    cbi.Content = key.ToString();
                    cbi.Tag = KeyInterop.VirtualKeyFromKey(key);
                    cboKey.Items.Add(cbi);
                    if (KeyInterop.VirtualKeyFromKey(key) == HotkeyKey)
                    {
                        selectedIndex = i;
                    }
                    i++;
                }
            }
            if (selectedIndex >= 0)
            {
                cboKey.SelectedIndex = selectedIndex;
            }

             
             
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

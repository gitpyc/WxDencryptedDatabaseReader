using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace wxreader
{
    public partial class Contract : Form
    {
        public List<GloableVars.wxuser> wxusers { get; set; }
        
        public Contract(List<GloableVars.wxuser> wxuserlist)
        {
            InitializeComponent();
            wxusers = wxuserlist;

            GloableVars.MonitoredVariable.Value= "Contract is open";

            GloableVars.NoCondition.ValueChanged += MonitoredVariable_ValueChanged;// 监听变量变化

            // 设置窗大小随listview大小变化
            this.Size = new Size(450, 650);
            this.CenterToScreen();
            // 添加列标题
            //listView1.Columns.Add("Image", 100);
            listView1.View = View.Details;
            listView1.GridLines = true; // 显示网格线
            listView1.MultiSelect = false; // 禁用多选，如果需要多选可以修改为true
            listView1.MouseClick += new MouseEventHandler(listView1_MouseClick);
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listView1.Columns.Add("昵称", 150, HorizontalAlignment.Center);
            //宽度根据内容自动调整
            
            listView1.Columns.Add("备注", 150, HorizontalAlignment.Center);
            listView1.Columns.Add("消息总数", 100, HorizontalAlignment.Center);
            //listView1.Columns.Add("Remark", 200);

            imageList1.ImageSize=new Size(50, 50);

            listView1.SmallImageList = imageList1;

            // 添加项
            int i = 0;
            foreach (var wxuser in wxuserlist)
            {
                string img = File.Exists($"{GloableVars.filePath}\\headimage\\{wxuser.username}.jpg") == true ? $"{GloableVars.filePath}\\headimage\\{wxuser.username}.jpg" : "E:\\mhp\\mhp\\headimage\\default.jpg";
                wxuser.headimgurl = img;
                imageList1.Images.Add(wxuser.username, Image.FromFile(img));
                AddContact(wxuser.nickname, wxuser.remark, wxuser.msgcount,$"{GloableVars.filePath}\\headimage\\{wxuser.username}.jpg", i);
                i++;
            }
        }

        private void MonitoredVariable_ValueChanged(object sender, EventArgs e)
        {
            //this.Close();
            GloableVars.isForm5Show = false;
        }

        Form4 form4 = new Form4();
        private void ShowForm4()
        {
            form4 = new Form4(); // 创建 Form4 实例
            form4.TopMost = true; // 置顶显示
            Application.Run(form4); // 确保在新线程中显示表单
        }

        private void CloseForm4()
        {
            if (form4 != null && form4.IsHandleCreated)
            {
                form4.Invoke(new Action(() => form4.Close()));
            }
        }
        
        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            if(GloableVars.isForm5Show)
            {
                return;
            }
            // 获取鼠标点击的位置
            var hitTestInfo = listView1.HitTest(e.Location);
            //多线程显示form4
            /*GloableVars.LoadingForm = new Form6();
            GloableVars.LoadingForm.UpdateLoadingText("正在进行二次语音数据核对，请稍候...");
            GloableVars.LoadingForm.Show();
            Thread thread = new Thread(new ThreadStart(GloableVars.ShowLoadingForm));
            thread.Start();*/

            Thread thread = new Thread(new ThreadStart(ShowForm4));
            thread.Start();

            // 检查是否点击了某个项
            if (hitTestInfo.Item != null)
            {
                // 获取被点击的 ListViewItem
                var selectedItem = hitTestInfo.Item;

                // 在这里可以处理点击事件，比如显示信息
                //MessageBox.Show($"你点击了项: {selectedItem.Text}");
                ChatHistory form5 = new ChatHistory(wxusers[hitTestInfo.Item.Index].username);
                //Application.Run(form5);
                form5.Show();
                GloableVars.isForm5Show = true;
            }
            CloseForm4();
            //GloableVars.LoadingForm.Close();
        }

        private void AddContact(string name, string remark,Int64  count,string imagePath,int index)
        {
            var listViewItem = new ListViewItem(name);
            //添加列表项的点击事件
            
            listViewItem.Text = name;
            listViewItem.ImageIndex = index;
            listViewItem.SubItems.Add(remark);
            listViewItem.SubItems.Add(count.ToString());
            listView1.Items.Add(listViewItem);

        }

        private void Contract_FormClosed(object sender, FormClosedEventArgs e)
        {
            GloableVars.MonitoredVariable.Value = "Contract is closed";
        }
    }
}

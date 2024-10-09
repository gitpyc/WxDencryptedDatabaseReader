using LibVLCSharp.Shared;
using System;
using System.IO;
using System.Windows.Forms;
using Vlc.DotNet.Core;
using Vlc.DotNet.Forms;
using Xabe.FFmpeg;
using LibVLCSharp.Shared;

namespace wxreader
{
    public class VideoPlayerForm : Form
    {
        private VlcMediaPlayer mediaPlayer;
        private VlcControl vlcControl2;
        private string videoPath;
        DirectoryInfo currentDirectory = null;
        public int videoWidth;
        private TrackBar trackBar;
        private VlcControl vlcControl1;
        public int videoHeight;

        public VideoPlayerForm(string videoPath)
        {
            this.videoPath = videoPath;

            AutoSize = true;
            // 设置窗体属性
            this.Text = "视频播放器";
            this.FormClosing += VideoPlayerForm_FormClosing;

            // 设置 VLC 库文件路径
            currentDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            var vlcLibDirectory = new System.IO.DirectoryInfo(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libvlc"));

            // 检查库路径是否存在
            if (!vlcLibDirectory.Exists)
            {
                MessageBox.Show("VLC library directory not found. Please ensure the 'libvlc' folder is present in the application directory.");
                this.Close();
            }
            GloableVars.GetMovWidthAndHeight(videoPath, ref videoWidth, ref videoHeight);
            /*// 调整窗体大小以适应视频--已经在初始化时设置了窗体大小
            this.Width = videoWidth + 40;  // 加上窗体的边框、菜单等的宽度
            this.Height = videoHeight + 60; // 加上窗体的标题栏和边框的高度
            this.Invalidate();*/
            InitializeComponent();

            //https://github.com/ZeBobo5/Vlc.DotNet/blob/master/src/Samples/Samples.WinForms.MultiplePlayers/Form1.cs
            // If any of the following 2 elements is true, then the vlc player itself will capture input from the user.
            // and then, the mouse click event won't fire.
            vlcControl1.Video.IsMouseInputEnabled = false;
            vlcControl1.Video.IsKeyInputEnabled = false;
            
            // 创建 VLC 媒体播放器
            mediaPlayer = vlcControl1.VlcMediaPlayer;

            // 绑定媒体加载完成事件
            mediaPlayer.Play(new Uri(videoPath)); // 播放视频

            vlcControl1.MouseClick += (s, e) =>
            {
                if (mediaPlayer.IsPlaying())
                {
                    mediaPlayer.Pause();
        }
                else
                {
                    mediaPlayer.Play();
                }
            };

            // 设置进度条
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000; // 每秒更新一次
            timer.Tick += (s, e) =>
            {
                if (mediaPlayer.IsPlaying())
                {
                    // 更新进度条
                    trackBar.Value = (int)(mediaPlayer.Time / mediaPlayer.Length * trackBar.Maximum);
                }
            };
            timer.Start();

           /* vlcControl1.Click += (s, e) =>
            {
                if (mediaPlayer.IsPlaying())
                {
                    mediaPlayer.Pause();
                }
                else
                {
                    mediaPlayer.Play();
                }
            };*/
            this.Text = "视频播放器 - " + videoPath;
        }
        /*
         Vlc使用方法：
         1. 创建VlcControl控件，设置控件的位置、大小、背景色等属性。
         2. 创建VlcMediaPlayer对象，并设置VlcControl控件的VlcMediaPlayer属性。
         3. 设置VlcMediaPlayer的VlcMediaplayerOptions属性，设置播放器的各种参数。
         4. 设置VlcMediaPlayer的Play方法，传入要播放的视频的路径或Uri。
         5. 调用VlcMediaPlayer的Play方法，开始播放视频。
         6. 调用VlcMediaPlayer的Stop方法，停止播放视频。
         7. 调用VlcMediaPlayer的Dispose方法，释放资源。
         
         注意：
         1. VlcControl控件的VlcLibDirectory属性，设置VLC库的路径。
         2. 注意是使用的X64还是X86的VLC库，需要根据自己系统的位数来选择。
         3. vlclib.dll 和 libvlc.dll 都需要放到应用程序的运行目录下。
         */



        private void VideoPlayerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 清理资源
            mediaPlayer.Stop(); // 停止播放
            mediaPlayer.Dispose(); // 释放资源
        }

        private void InitializeComponent()
        {
            // 
            // vlcControl1
            // 
            this.vlcControl1 = new Vlc.DotNet.Forms.VlcControl();
            ((System.ComponentModel.ISupportInitialize)(this.vlcControl1)).BeginInit();
            this.vlcControl1.BackColor = System.Drawing.Color.Black;
            this.vlcControl1.Location = new System.Drawing.Point(0, 0);
            this.vlcControl1.Name = "vlcControl1";
            this.vlcControl1.Size = new System.Drawing.Size(videoWidth, videoHeight);
            this.vlcControl1.Spu = -1;
            this.vlcControl1.TabIndex = 1;
            this.vlcControl1.Text = "vlcControl1";
            this.vlcControl1.VlcLibDirectory = new System.IO.DirectoryInfo(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libvlc",IntPtr.Size == 8 ? "x64" : "x86"));
            this.vlcControl1.VlcMediaplayerOptions = null;

            this.trackBar = new System.Windows.Forms.TrackBar();
            
            ((System.ComponentModel.ISupportInitialize)(this.trackBar)).BeginInit();
            
            this.SuspendLayout();
            // 
            // trackBar
            //位置跟随视频大小变化
            this.trackBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.trackBar.Location = new System.Drawing.Point(videoWidth / 5, videoHeight / 5);
            this.trackBar.Name = "trackBar";
            this.trackBar.Size = new System.Drawing.Size(videoWidth / 2, 45);
            this.trackBar.TabIndex = 0;
            // 
            // VideoPlayerForm
            // 
            this.ClientSize = new System.Drawing.Size(videoWidth, videoHeight);
            this.Controls.Add(this.vlcControl1);
            this.Controls.Add(this.trackBar);
            this.Name = "VideoPlayerForm";
            ((System.ComponentModel.ISupportInitialize)(this.trackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.vlcControl1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}

using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Net;
using static wxreader.GloableVars;

namespace wxreader
{
    public partial class Form1 : Form, IProgressReporter
    {
        //定义全局变量
        List<GloableVars.fileinfo> filelist = new List<GloableVars.fileinfo>();
        List<GloableVars.fileinfo> extfiles = new List<GloableVars.fileinfo>();
        SqliteHelper sqlitehelper = new SqliteHelper();
        List<SqliteHelper.SqliteConnection> connetions = new List<SqliteHelper.SqliteConnection>();

        private BackgroundWorker worker = new BackgroundWorker();
        private BackgroundWorker worker2 = new BackgroundWorker();
        public Form1()
        {
            InitializeComponent();
            //this.TopMost = true;
            textBox1.Enabled = false;

            //所有按钮不可用
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = false;

            GloableVars.MonitoredVariable.ValueChanged += MonitoredVariable_ValueChanged; // 订阅事件

            GloableVars.ProcessingVariable.ValueChanged += UpdataProcessingVariable; // 订阅事件

            GloableVars.NoCondition.ValueChanged += NoCondition_ValueChanged; // 订阅事件

            //设置后台线程
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += DoWork;
            worker.ProgressChanged += ProgressChanged;
            worker.RunWorkerCompleted += RunWorkerCompleted;

            worker2.WorkerReportsProgress = true;
            worker2.WorkerSupportsCancellation = true;
            worker2.DoWork += DoWork2;
            worker2.ProgressChanged += ProgressChanged2;
            worker2.RunWorkerCompleted += RunWorkerCompleted2;


            //设置进度条初始值
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;
            progressBar1.Value = 0;

            progressBar2.Minimum = 0;
            progressBar2.Maximum = 100;
            progressBar2.Value = 0;

            progressBar3.Minimum = 0;
            progressBar3.Maximum = 100;
            progressBar3.Value = 0;
        }

        private async void NoCondition_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                GloableVars.MonitoredVariable.Value = "default.jpg not found!";

                // 确保所有事件处理完成
                // 延迟一段时间，确保 UI 可以进行处理
                await Task.Delay(100); // 使用异步延迟

                // 关闭窗体而不是直接退出应用
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发生错误: {ex.Message}");
            }
        }

        private void MonitoredVariable_ValueChanged(object sender, EventArgs e)
        {
            button7.Enabled = !button7.Enabled;
        }

        private void UpdataProcessingVariable(object sender, EventArgs e)
        {
            progressBar2.Value = int.Parse(GloableVars.ProcessingVariable.Value) / GloableVars.DatImageCount;
        }

        private void RunWorkerCompleted2(object? sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("解码完成！");
        }

        private void ProgressChanged2(object? sender, ProgressChangedEventArgs e)
        {
            progressBar3.Value = e.ProgressPercentage;
        }

        private void DoWork2(object? sender, DoWorkEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void ReportProgress(int percent)
        {
            if (progressBar3.InvokeRequired)
            {
                progressBar3.Invoke(new Action(() => ReportProgress(percent)));
            }
            else
            {
                progressBar3.Value = percent;
            }
        }

        private void RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("合并完成,相关信息已经存储在TRUEMSG表中！");
            button2.Enabled = true;
            //关闭数据库连接
            //foreach (SqliteHelper.SqliteConnection connection in connetions)
            //{
            //    connection.connection.Close();
            //}
            connetions[0].connection.Close();
        }

        private void CleanConnOfSqlite()
        {
            foreach (SqliteHelper.SqliteConnection connection in connetions)
            {
                connection.connection.Close();
            }
            SQLiteConnection.ClearAllPools();//清理数据库连接池
            //清理数据库临时文件
            foreach (GloableVars.fileinfo file in extfiles)
            {
                if (File.Exists(file.filepath + "-wal"))
                {
                    //检测文件是否被其他进程占用
                    if (FolderFileSearcher.IsFileLocked(file.filepath + "-wal"))
                    {
                        MessageBox.Show("文件" + file.filepath + "-wal" + "被占用！");
                    }
                    else
                    {
                        FolderFileSearcher.ReleaseSqliteFile(file.filepath + "-wal");
                        File.Delete(file.filepath + "-wal");
                    }
                }
                if (File.Exists(file.filepath + "-shm"))
                {
                    if (FolderFileSearcher.IsFileLocked(file.filepath + "-shm"))
                    {

                        MessageBox.Show("文件" + file.filepath + "-shm" + "被占用！");
                    }
                    else
                    {
                        FolderFileSearcher.ReleaseSqliteFile(file.filepath + "-shm");
                        File.Delete(file.filepath + "-shm");
                    }
                }
            }
        }
        private void ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void DoWork(object? sender, DoWorkEventArgs e)
        {
            int countmediat = 0, msgcoun = 0, progress = 0;

            SqliteHelper.SqliteConnection connection = new SqliteHelper.SqliteConnection();

            //string connstr = null;
            //遍历文件列表，获取连接
            connection.connectingName = extfiles[0].filename;
            foreach (GloableVars.fileinfo file in extfiles)
            {

                //connection.connectingName = file.filename;
                if (extfiles.IndexOf(file) == 0)
                {
                    //connection.connectingName = file.filename;
                    //connstr = @"Data Source=" + file.filepath + " ";
                    connection.connection = SqliteHelper.GetConnection(@"Data Source=" + file.filepath + ";Version=3;");
                    //该行代码已经包含打开数据库连接操作无需再执行打开操作
                }
                else
                {
                    SqliteHelper.ExecuteNonQuery(@"ATTACH DATABASE '" + file.filepath + "' AS " + Path.GetFileNameWithoutExtension(file.filename) + ";", connection.connection);
                    //附加数据库到当前连接
                }
            }
            connetions.Add(connection);//添加到连接列表

            string sql = "CREATE TABLE MEGEDMSG AS ";
            //遍历文件列表，获取表结构
            foreach (GloableVars.fileinfo file in extfiles)
            {
                if (extfiles.IndexOf(file) != 0)
                {
                    if (file.filename.Contains("de_MediaMSG"))
                    {
                        countmediat++;
                    }
                    else if (file.filename.Contains("de_MSG"))
                    {
                        msgcoun++;
                    }
                }
                progress += 1;
                worker.ReportProgress(progress);
            }
            //合并消息数据库
            foreach (GloableVars.fileinfo file in extfiles)
            {
                if (extfiles.IndexOf(file) != 0 && file.filename.Contains("de_MSG"))
                {
                    if (msgcoun > 1)
                    {
                        sql += "SELECT * FROM " + Path.GetFileNameWithoutExtension(file.filename) + ".MSG UNION ";
                        msgcoun--;
                    }
                    else if (msgcoun == 1)
                    {
                        sql += "SELECT * FROM " + Path.GetFileNameWithoutExtension(file.filename) + ".MSG";
                    }
                }
            }
            //多线程查询同步执行
            if (!SqliteHelper.CheckTableExists(connetions[0].connection, "MEGEDMSG"))
            {
                SqliteHelper.ExecuteNonQuery(sql, connetions[0].connection);
            }
            else
            {
                MessageBox.Show("MEGEDMSG表已存在！");
            }
            progress += 50;
            worker.ReportProgress(progress);
            //MessageBox.Show("消息数据库合并完成！");

            sql = "CREATE TABLE MEGEDMEDIA AS ";
            //合并媒体数据库
            foreach (GloableVars.fileinfo file in extfiles)
            {
                if (extfiles.IndexOf(file) != 0 && file.filename.Contains("de_MediaMSG"))
                {
                    if (countmediat > 1)
                    {
                        if (SqliteHelper.NewCheckTableExists(connetions[0].connection, Path.GetFileNameWithoutExtension(file.filename) + ".Media"))
                        { sql += "SELECT * FROM " + Path.GetFileNameWithoutExtension(file.filename) + ".Media UNION "; }
                        countmediat--;
                    }
                    else if (countmediat == 1)
                    {
                        if (SqliteHelper.NewCheckTableExists(connetions[0].connection, Path.GetFileNameWithoutExtension(file.filename) + ".Media"))
                        {
                            sql += "SELECT * FROM " + Path.GetFileNameWithoutExtension(file.filename) + ".Media";
                        }
                        else
                        {
                            sql = sql.Substring(0, sql.Length - 6);//去掉最后的UNION
                        }
                    }
                }
                progress += 1;
                worker.ReportProgress(progress);
            }
            //合并媒体数据库
            if (!SqliteHelper.CheckTableExists(connetions[0].connection, "MEGEDMEDIA"))
            {
                SqliteHelper.ExecuteNonQuery(sql, connetions[0].connection);
            }
            else
            {
                MessageBox.Show("MEGEDMEDIA表已存在！");
            }
            worker.ReportProgress(66);

            //合并消息和媒体数据库
            sql = "CREATE TABLE ALLMESG AS SELECT MEGEDMSG.*,MEGEDMEDIA.Buf FROM MEGEDMSG LEFT JOIN MEGEDMEDIA ON MEGEDMSG.MsgSvrID = MEGEDMEDIA.Reserved0";
            if (!SqliteHelper.CheckTableExists(connetions[0].connection, "ALLMESG"))
            {
                try
                {
                    SqliteHelper.ExecuteNonQuery(sql, connetions[0].connection);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show("ALLMESG表已存在！");
            }
            worker.ReportProgress(80);

            sql = "CREATE TABLE TRUEMSG as SELECT ALLMESG.*,Contact.bigHeadImgUrl,Contact.SmallHeadImgUrl,Contact.Remark,Contact.ExtraBuf as ContactEBuf,Contact.NickName FROM ALLMESG LEFT JOIN Contact on ALLMESG.StrTalker = Contact.UserName";
            try
            {
                if (!SqliteHelper.CheckTableExists(connetions[0].connection, "TRUEMSG"))
                {
                    SqliteHelper.ExecuteNonQuery(sql, connetions[0].connection);
                }
                else
                {
                    MessageBox.Show("TRUEMSG表已存在！");
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
            worker.ReportProgress(100);
            connetions[0].connection.Close();

            //新建数据库连接
            connetions[0].connection.Open();
            SqliteHelper.ExecuteNonQuery("DROP TABLE IF EXISTS MEGEDMSG", connetions[0].connection);
            SqliteHelper.ExecuteNonQuery("DROP TABLE IF EXISTS MEGEDMEDIA", connetions[0].connection);
            SqliteHelper.ExecuteNonQuery("DROP TABLE IF EXISTS ALLMESG", connetions[0].connection);
            SqliteHelper.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS ProgramInfo (id INTEGER PRIMARY KEY AUTOINCREMENT,wxid TEXT NOT NULL,TotalMessageCount INTEGER,VoiceMessageCount INTEGER,VaildVoiceMessageCount INTEGER);", connetions[0].connection);
            connetions[0].connection.Close();

            MessageBox.Show("中间表已删除！");
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }


        private void button1_Click(object sender, EventArgs e)
        {
            //打开文件夹选择对话框
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            DialogResult result = folderBrowserDialog1.ShowDialog();
            FolderFileSearcher ffsearcher = new FolderFileSearcher();
            if (result == DialogResult.OK)
            {
                //获取选择的文件夹路径
                GloableVars.filePath = folderBrowserDialog1.SelectedPath;
                //设置文本框显示路径
                textBox1.Text = GloableVars.filePath;
                //设置按钮可用
                button1.Enabled = true;

                //遍历文件夹及子文件夹下所有文件
                filelist = ffsearcher.SearchFiles(GloableVars.filePath);

                List<string> defaultdbtext = new List<string>();

                //添加默认列表数据
                defaultdbtext.Add("de_MicroMsg");
                defaultdbtext.Add("de_MediaMSG");
                defaultdbtext.Add("de_MSG");

                //检查存在指定数据库文件
                extfiles = ffsearcher.CheckFileInfoFromFilnameList(filelist, defaultdbtext);

                //设置文件列表显示
                listView1.Items.Clear();
                foreach (GloableVars.fileinfo file in extfiles)
                {
                    ListViewItem item = new ListViewItem(file.filename);
                    item.SubItems.Add(file.filepath);
                    listView1.Items.Add(item);
                }
            }
            else
            {
                //显示打开失败原因
                MessageBox.Show("路径打开失败！" + result.ToString());
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // 启动后台操作
            button2.Enabled = false;
            worker.RunWorkerAsync();
        }

        //解码数据库中的buf字段
        public static async Task DecodeBufAsync(SQLiteConnection connection, string strTalker)
        {
            // 创建silkdecoder实例并传入数据库连接和最大并发数
            var decoder = new silkdecoder(connection, maxConcurrency: 10); // 设置最大并发数为10

            // 定义多个音频ID
            List<GloableVars.voiceinfo> audioIds = GetAudioIds(connection, strTalker); // 假设这是一个包含几万条音频ID的列表
            GloableVars.voiceList = audioIds;

            if (audioIds.Count == 0)
            {
                Console.WriteLine($"{strTalker}没有音频数据!");
                //silkdecoder.WriteLog($"{GloableVars.filePath}\\contract\\{strTalker}\\silk", $"{strTalker}没有音频数据!");
                return;
            }
            // 启动多个转换任务
            var tasks = new List<Task>();

            int i = 0;
            GloableVars.successDecodeVoiceCount = 0;
            GloableVars.failedDecodeVoiceCount = 0;
            foreach (var audioId in audioIds)
            {
                //int percent = (int)((i + 1) / (double)audioIds.Count * 100);
                //worker2.ReportProgress(percent);
                string tempSilkFilePath = $"{GloableVars.filePath}\\contract\\{strTalker}\\silk\\{audioId.audoid}.silk";
                string outputMp3FilePath = $"{GloableVars.filePath}\\contract\\{strTalker}\\mp3\\{audioId.audoid}.mp3";
                tasks.Add(decoder.DecodeAudioAsync(audioId.audoid, audioIds, tempSilkFilePath, outputMp3FilePath));
                i++;
                //当任务数达到最大并发数时，等待任务完成
                if (tasks.Count == 10)
                {
                    await Task.WhenAny(tasks);
                    tasks.Remove(tasks.FirstOrDefault(t => t.IsCompleted));
                }
            }

            // 等待所有任务完成
            await Task.WhenAll(tasks);

            Console.WriteLine("所有转换任务已完成");

        }

        public static List<GloableVars.voiceinfo> GetAudioIds(SQLiteConnection connection, string strTalker)
        {
            if (connection.State == System.Data.ConnectionState.Closed)
            {
                connection.Open(); 
            }
            try
            {
                SQLiteDataReader reader = SqliteHelper.ExecuteReader("SELECT MsgSvrID,Buf FROM TRUEMSG where StrTalker = '" + strTalker + "' and Type=34", connection);//假设这是一个包含MsgSvrID的表名
                List<string> audioIds = new List<string>();
                List<GloableVars.voiceinfo> voiceinfos = new List<GloableVars.voiceinfo>();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        //判断msgsvrID是否为空
                        string audioId = reader["MsgSvrID"] == DBNull.Value ? "" : reader.GetInt64(0).ToString();
                        //判断buf是否为空
                        byte[] buf = reader["Buf"] == DBNull.Value ? new byte[0] : (byte[])reader["Buf"];
                        if (!string.IsNullOrEmpty(audioId) && buf.Length > 0)
                        {
                            audioIds.Add(audioId);
                            GloableVars.voiceinfo voiceinfo = new GloableVars.voiceinfo
                            {
                                audoid = audioId,
                                voice = buf
                            };
                            voiceinfos.Add(voiceinfo);
                        }
                    }
                }
                return voiceinfos;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            finally
            {
                connection.Close();
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            CleanConnOfSqlite();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Golangdecoder.Fuckit();
        }

        /// <summary>
        /// 创建strtalker目录
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        private bool createStrtaklerDir(SQLiteConnection connection)
        {
            if (connection.State == System.Data.ConnectionState.Closed)
            {
                connection.Open();
            }
            try
            {
                string sql = "SELECT * FROM Contact";
                SQLiteDataReader reader = SqliteHelper.ExecuteReader(sql, connection);
                GloableVars.talkerlist = new List<GloableVars.wxtalker>();
                while (reader.Read())
                {
                    GloableVars.wxtalker talker = new GloableVars.wxtalker();
                    talker.username = (string)reader["UserName"];
                    talker.nickname = (string)reader["NickName"];
                    talker.remark = (string)reader["Remark"];
                    GloableVars.talkerlist.Add(talker);
                    string strTalkerDir = Path.Combine(GloableVars.filePath, "contract", talker.username);
                    if (!Directory.Exists(strTalkerDir))
                    {
                        Directory.CreateDirectory(strTalkerDir);
                        string silkDir = Path.Combine(strTalkerDir, "silk");
                        Directory.CreateDirectory(silkDir);
                        string mp3Dir = Path.Combine(strTalkerDir, "mp3");
                        Directory.CreateDirectory(mp3Dir);
                    }
                }
                return true;
            }
            catch
            {
                connection.Close();
                return false;
            }
            finally
            {
                connection.Close();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //GloableVars.connecsString == null ? GloableVars.connecsString = @"Data Source=" + GloableVars.filePath + @"\\de_MicroMsg.db ;Version=3;" : "";
            if (GloableVars.filePath == null)
            {
                //打开文件夹选择对话框
                FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
                DialogResult result = folderBrowserDialog1.ShowDialog();
                if (result == DialogResult.OK)
                {
                    GloableVars.filePath = folderBrowserDialog1.SelectedPath;
                    //设置文本框显示路径
                    textBox1.Text = GloableVars.filePath;
                    //设置按钮可用
                    button1.Enabled = true;
                }
                else
                {
                    //显示打开失败原因
                    MessageBox.Show("路径打开失败！" + result.ToString());
                    return;
                }
            }
            if (GloableVars.connecsString == null)
            {
                GloableVars.connecsString = @"Data Source=" + GloableVars.filePath + @"\de_MicroMsg.db ;Version=3;";
            }
            connetions.Add(new SqliteHelper.SqliteConnection() { connectingName = "globalconnect", connection = new SQLiteConnection(GloableVars.connecsString) });
            if (SqliteHelper.CheckTableExists("TRUEMSG"))
            {
                if (connetions.Count > 0)
                {
                    if (createStrtaklerDir(connetions[0].connection))
                    {
                        foreach (GloableVars.wxtalker talker in GloableVars.talkerlist)
                        {
                            DecodeBufAsync(connetions[0].connection, talker.username);
                        }
                    }
                }
                else
                {
                    SQLiteConnection connection = new SQLiteConnection();
                    connection.ConnectionString = @"Data Source=" + GloableVars.filePath + @"\de_MicroMsg.db ;Version=3;";
                    connetions.Add(new SqliteHelper.SqliteConnection() { connection = connection });
                    connetions[0].connection.Open();
                    if (createStrtaklerDir(connetions[0].connection))
                    {
                        foreach (GloableVars.wxtalker talker in GloableVars.talkerlist)
                        {
                            DecodeBufAsync(connetions[0].connection, talker.username);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("TRUEMSG表不存在,请先合并数据库！");
            }
            //关闭所有数据库连接
            CleanConnOfSqlite();
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            button5.Enabled = false;
            string connectionString = null;
            SQLiteConnection connection = new SQLiteConnection();
            if (GloableVars.filePath != null)
            {
                connectionString = @"Data Source=" + GloableVars.filePath + @"\de_MicroMsg.db ;Version=3;";
                connection = SqliteHelper.GetConnection(connectionString);
            }
            else
            {
                //打开文件夹选择对话框
                FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
                DialogResult result = folderBrowserDialog1.ShowDialog();
                if (result == DialogResult.OK)
                {
                    GloableVars.filePath = folderBrowserDialog1.SelectedPath;
                    //设置文本框显示路径
                    textBox1.Text = GloableVars.filePath;
                    //设置按钮可用
                    button1.Enabled = true;
                }
                else
                {
                    //显示打开失败原因
                    MessageBox.Show("路径打开失败！" + result.ToString());
                    return;
                }
                connectionString = @"Data Source=" + GloableVars.filePath + @"\de_MicroMsg.db ;Version=3;";
                connection = SqliteHelper.GetConnection(connectionString);
            }
            await DownloadHeadImage(connection, Path.Combine(GloableVars.filePath, "headimage"), UpdateProgressBar);
            connection.Close();
        }

        private void UpdateProgressBar(int progress)
        {
            if (progressBar4.InvokeRequired)
            {
                progressBar4.Invoke(new Action<int>(UpdateProgressBar), progress);
            }
            else
            {
                progressBar4.Value = progress;
            }
        }

        private async Task DownloadHeadImage(SQLiteConnection connection, string imageDirectory, Action<int> progressCallback)
        {
            if (!Directory.Exists(imageDirectory))
            {
                Directory.CreateDirectory(imageDirectory);
            }
            //读取数据库
            string sql = "SELECT * FROM ContactHeadImgUrl";
            SQLiteDataReader reader = SqliteHelper.ExecuteReader(sql, connection);
            List<GloableVars.HeadImageInfo> headimageinfos = new List<GloableVars.HeadImageInfo>();
            int count = 0, total = 0;
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    GloableVars.HeadImageInfo headimageinfo = new GloableVars.HeadImageInfo();
                    headimageinfo.UserName = reader["usrName"] == DBNull.Value ? "" : (string)reader["usrName"];
                    headimageinfo.smallImageUrl = reader["smallHeadImgUrl"] == DBNull.Value ? "" : (string)reader["smallHeadImgUrl"];
                    headimageinfo.bigImageUrl = reader["bigHeadImgUrl"] == DBNull.Value ? "" : (string)reader["bigHeadImgUrl"];
                    headimageinfo.HeadImageMd5 = reader["headImgMd5"] == DBNull.Value ? "" : (string)reader["headImgMd5"];
                    headimageinfos.Add(headimageinfo);
                    count++;
                }
            }
            foreach (var item in headimageinfos)
            {
                if (!string.IsNullOrEmpty(item.smallImageUrl) && !string.IsNullOrEmpty(item.bigImageUrl))
                {
                    string smallImagePath = Path.Combine(imageDirectory, item.UserName + ".jpg");
                    string bigImagePath = Path.Combine(imageDirectory, item.UserName + "_big.jpg");
                    if (!File.Exists(smallImagePath) || !File.Exists(bigImagePath))
                    {
                        try
                        {
                            await Task.Run(() =>
                            {
                                WebClient webClient = new WebClient();
                                webClient.DownloadFile(item.smallImageUrl, smallImagePath);
                                webClient.DownloadFile(item.bigImageUrl, bigImagePath);
                                webClient.Dispose();
                                total++;
                                progressCallback((int)(total / (double)headimageinfos.Count * 100));
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
            progressBar4.Value = 100;
            MessageBox.Show($"{count}个头像下载完成，请在{GloableVars.filePath+@"\headimage"}文件夹下放default.jpg作为默认头像！");
            button5.Enabled = true;

        }

        private void button6_Click(object sender, EventArgs e)
        {
            button6.Enabled = false;

            SQLiteConnection connection = new SQLiteConnection();
            string[] encryptedFiles = null;
            if (GloableVars.filePath != null)
            {
                connection = SqliteHelper.GetConnection(@"Data Source=" + GloableVars.filePath + @"\de_Emotion.db ;Version=3;");
                encryptedFiles = Directory.GetFiles($"{GloableVars.filePath}\\CustomEmotion", "*", SearchOption.AllDirectories);
                if (encryptedFiles.Length == 0)
                {
                    MessageBox.Show("CustomEmotion文件下没有文件或文件夹不存在！");
                    return;
                }
            }
            else
            {
                //打开文件夹选择对话框
                FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
                DialogResult result = folderBrowserDialog1.ShowDialog();
                if (result == DialogResult.OK)
                {
                    GloableVars.filePath = folderBrowserDialog1.SelectedPath;
                    //设置文本框显示路径
                    textBox1.Text = GloableVars.filePath;
                    //获取文件夹下所有文件
                    encryptedFiles = Directory.GetFiles($"{GloableVars.filePath}\\CustomEmotion", "*", SearchOption.AllDirectories);
                    if (encryptedFiles.Length == 0)
                    {
                        MessageBox.Show("请选择包含CustomEmotion文件夹的目录！");
                        return;
                    }
                }
                else
                {
                    //显示打开失败原因
                    MessageBox.Show("路径打开失败！" + result.ToString());
                    return;
                }
                connection = SqliteHelper.GetConnection(@"Data Source=" + GloableVars.filePath + @"\de_Emotion.db ;Version=3;");
            }
            if (encryptedFiles == null)
            {
                return;
            }

            List<GloableVars.emotioninfo> emotioninfos = new List<GloableVars.emotioninfo>();
            string sql = "SELECT * FROM CustomEmotion";
            SQLiteDataReader reader = SqliteHelper.ExecuteReader(sql, connection);
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    GloableVars.emotioninfo emotioninfo = new GloableVars.emotioninfo();
                    emotioninfo.emotionname = reader["MD5"] == DBNull.Value ? "" : (string)reader["MD5"];
                    emotioninfo.emotion_aeskey = reader["aeskey"] == DBNull.Value ? "" : (string)reader["aeskey"];
                    emotioninfo.emotion_aesiv = reader["Reserved3"] == DBNull.Value ? "" : (string)reader["Reserved3"];
                    emotioninfo.emotion_url = reader["CDNUrl"] == DBNull.Value ? "" : (string)reader["CDNUrl"];
                    emotioninfos.Add(emotioninfo);
                }
            }
            else
            {
                MessageBox.Show("CustomEmotion表不存在！");
                connection.Close();
                return;
            }
            connection.Close();
            MessageBox.Show("成功获取到" + emotioninfos.Count + "条表情数据,正在下载中...");

            //单任务解密
            int count = 0;
            foreach (var item in emotioninfos)
            {
                foreach (var file in encryptedFiles)
                {
                    if (file.Contains(item.emotionname.ToUpper()) && !file.EndsWith(".gif"))
                    {
                        //aesdencrpyt aesdencrpyt = new aesdencrpyt(item.emotion_aeskey, item.emotion_aesiv);
                        try
                        {
                            //解密失败，aeskey是从数据库中读取的，这个不会错，只是iv也是从数据库中另外一个字段读取的，无法确认是否正确，还有
                            //逻辑是否正确也无从考证，还有最后的异或密钥也是从网上的描述中来做的，所以不知道是否正确
                            //aesdencrpyt.DecryptImage(file, $"{Path.GetDirectoryName(file)}\\{item.emotionname}.gif");

                            //走另外一种路线，就是从网上下载图片，然后保存到本地
                            string url = item.emotion_url;
                            string fileName = Path.GetFileName(file);
                            string filePath = Path.Combine(Path.GetDirectoryName(file), item.emotionname + ".gif");
                            WebClient webClient = new WebClient();
                            webClient.DownloadFile(url, filePath);
                            webClient.Dispose();
                            progressBar5.Value = (int)(count / (double)emotioninfos.Count * 100);
                            count++;
                        }
                        catch (Exception ex)
                        {
                            //silkdecoder.WriteLog($"{Path.GetDirectoryName(file)}.log", ex.Message);
                            throw;
                        }
                    }
                }
            }
            button6.Enabled = true;
            progressBar5.Value = 100;
            MessageBox.Show("表情已下载完成！");

        }

        private void button7_Click(object sender, EventArgs e)
        {
            //Form4 form4 = GloableVars.startWatingWindow();
            Thread thread = new Thread(new ThreadStart(ShowForm4));
            thread.Start();

            List<GloableVars.wxuser> wxusers = GetWxUsers(SqliteHelper.GetConnection($"Data Source={GloableVars.filePath}\\de_MicroMsg.db ;Version=3;"));
            if (wxusers.Count == 0)
            {
                MessageBox.Show("没有找到联系人信息！");
                return;
            }
            var form2 = new Contract(wxusers);
            form2.wxusers = wxusers;
            form2.Show();
            //GloableVars.CloseFormIfHandleCreated(form4);
            CloseForm4();
        }

        private List<GloableVars.wxuser> GetWxUsers(SQLiteConnection connection)
        {
            List<GloableVars.wxuser> wxusers = new List<GloableVars.wxuser>();
            string sql = "SELECT count(strContent) as msgcount,StrTalker,Remark,nickname FROM TRUEMSG GROUP by StrTalker order by msgcount Desc";
            SQLiteDataReader reader = SqliteHelper.ExecuteReader(sql, connection);
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    GloableVars.wxuser wxuser = new GloableVars.wxuser();
                    wxuser.username = reader["StrTalker"] == DBNull.Value ? "" : (string)reader["StrTalker"];
                    wxuser.nickname = reader["NickName"] == DBNull.Value ? "" : (string)reader["NickName"];
                    wxuser.remark = reader["Remark"] == DBNull.Value ? "" : (string)reader["Remark"];
                    wxuser.msgcount = reader["msgcount"] == DBNull.Value ? 0 : (Int64)reader["msgcount"];
                    wxusers.Add(wxuser);
                }
            }
            GloableVars.userlist = wxusers;
            return wxusers;
        }

        //检测GloableVars.filePath是否存在,存在则不操作，不存在则打开文件夹选择对话框
        private bool CheckKeyDBFiles()
        {
            if (GloableVars.filePath == null)
            {
                //打开文件夹选择对话框
                FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
                DialogResult result = folderBrowserDialog1.ShowDialog();
                if (result == DialogResult.OK)
                {
                    //多线程显示form4
                    Thread thread = new Thread(new ThreadStart(ShowForm4));
                    thread.Start();
                    GloableVars.filePath = folderBrowserDialog1.SelectedPath;
                    //设置文本框显示路径
                    textBox1.Text = GloableVars.filePath;
                    //设置按钮可用
                    if (textBox1.Text != null)
                    {
                        button1.Enabled = false;
                    }
                    var dbfiles = Directory.GetFiles(GloableVars.filePath, "de_MicroMsg.db", SearchOption.TopDirectoryOnly);
                    if (dbfiles.Length == 0)
                    {
                        CloseForm4();
                        MessageBox.Show("没有找到微信数据库文件，请确认路径是否正确！或者数据库是否解密！");
                        //退出程序
                        Application.Exit();
                        return false;
                    }
                    GloableVars.selfwxid = GetSelfID();
                    if (GloableVars.selfwxid == "")
                    {
                        CloseForm4();
                        MessageBox.Show("获取自己的微信ID失败！");
                        Application.Exit();
                        return false;
                    }
                }
                else
                {
                    CloseForm4();
                    //显示打开失败原因
                    MessageBox.Show("路径打开失败，退出程序！");
                    this.Close();
                    Application.Exit();
                    return false;
                }
            }

            //遍历文件夹及子文件夹下所有文件
            FolderFileSearcher ffsearcher = new FolderFileSearcher();
            filelist = ffsearcher.SearchFiles(GloableVars.filePath);

            List<string> defaultdbtext = new List<string>();

            //添加默认列表数据
            defaultdbtext.Add("de_MicroMsg");
            defaultdbtext.Add("de_MediaMSG");
            defaultdbtext.Add("de_MSG");

            //检查存在指定数据库文件
            extfiles = ffsearcher.CheckFileInfoFromFilnameList(filelist, defaultdbtext);

            //设置文件列表显示
            listView1.Items.Clear();
            foreach (GloableVars.fileinfo file in extfiles)
            {
                ListViewItem item = new ListViewItem(file.filename);
                item.SubItems.Add(file.filepath);
                listView1.Items.Add(item);
            }

            //所有按钮不可用
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
            button5.Enabled = true;
            button6.Enabled = true;
            button7.Enabled = true;
            return true;
        }

        Form4 form4 = new Form4();

        private string GetSelfID()
        {
            string sql = "select * FROM Contact LIMIT 1";
            SQLiteDataReader reader = SqliteHelper.ExecuteReader(sql, SqliteHelper.GetConnection($"Data Source={GloableVars.filePath}\\de_MicroMsg.db ;Version=3;"));
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    return reader["UserName"] == DBNull.Value ? "" : (string)reader["UserName"];
                }
            }
            return "";
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            MessageBox.Show("请先选择微信解密数据库根目录！");

            //thread.Join();

            if (GloableVars.selfwxid == "")
            {
                MessageBox.Show("获取自己的微信ID失败！");
                Application.Exit();
                return;
            }

            if (!CheckKeyDBFiles())
            {
                Application.Exit();
                return;
            }

            SqliteHelper.InitSqlite(); // 初始化数据库

            // 检测表TRUEMSG是否存在
            button2.Enabled = !SqliteHelper.CheckTableExists("TRUEMSG");

            // 检测Decode文件夹是否存在
            if (!Directory.Exists(Path.Combine(GloableVars.filePath, "MsgAttach")))
            {
                MessageBox.Show("MsgAttach文件夹不存在,请从微信安装目录下拷贝该文件夹！");
                Application.Exit();
                return;
            }
            var decodefiles = Directory.GetFiles($"{GloableVars.filePath}\\MsgAttach", "*.jpeg", SearchOption.AllDirectories);
            button3.Enabled = decodefiles.Length == 0;


            // 检测MP3文件
            if (!Directory.Exists(Path.Combine(GloableVars.filePath, "contract")))
            {
                //确认对话框
                DialogResult result = MessageBox.Show("contract文件夹不存在,是否创建该文件夹？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(Path.Combine(GloableVars.filePath, "contract"));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("创建contract文件夹失败！" + ex.Message);
                        Application.Exit();
                        return;
                    }
                }
                else
                {
                    Application.Exit();
                    return;
                }
            }
            var mp3files = Directory.GetFiles($"{GloableVars.filePath}\\contract", "*.mp3", SearchOption.AllDirectories);
            button4.Enabled = mp3files.Length == 0;


            // 检测表情文件
            if (!Directory.Exists(Path.Combine(GloableVars.filePath, "CustomEmotion")))
            {
                MessageBox.Show("CustomEmotion文件夹不存在,请从微信安装目录下拷贝该文件夹！");
                Application.Exit();
                return;
            }
            var emotionfiles = Directory.GetFiles($"{GloableVars.filePath}\\CustomEmotion", "*.gif", SearchOption.AllDirectories);
            button6.Enabled = emotionfiles.Length == 0;


            // 检测头像文件夹
            if (!Directory.Exists(Path.Combine(GloableVars.filePath, "headimage")))
            {
                //确认对话框
                DialogResult result = MessageBox.Show("headimage文件夹不存在,是否创建该文件夹？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(Path.Combine(GloableVars.filePath, "headimage"));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("创建headimage文件夹失败！" + ex.Message);
                        Application.Exit();
                        return;
                    }
                }
                else
                {
                    Application.Exit();
                    return;
                }
            }
            var headimagefiles = Directory.GetFiles(Path.Combine(GloableVars.filePath, "headimage"), "*.jpg", SearchOption.AllDirectories);
            button5.Enabled = headimagefiles.Length == 0;
            

            this.Activate();

            // 启动一个新的线程来执行耗时操作
            // 使用线程使得 UI 不会被阻塞
            //var thread = new Thread(new ThreadStart(LoadData));
            //thread.Start();
            //thread.Join();
            /*if (form4 != null && form4.IsHandleCreated)
            {
                form4.Invoke(new Action(() => form4.Close()));
            }*/
            CloseForm4();
        }

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

        private void LoadData()
        {
            SqliteHelper.InitSqlite(); // 初始化数据库

            // 在主线程更新UI
            this.Invoke(new Action(() =>
            {
                // 检测表TRUEMSG是否存在
                button2.Enabled = !SqliteHelper.CheckTableExists("TRUEMSG");

                // 检测Decode文件夹是否存在
                var decodefiles = Directory.GetFiles($"{GloableVars.filePath}\\MsgAttach", "*.jpeg", SearchOption.AllDirectories);
                button3.Enabled = decodefiles.Length == 0;

                // 检测MP3文件
                var mp3files = Directory.GetFiles($"{GloableVars.filePath}\\contract", "*.mp3", SearchOption.AllDirectories);
                button4.Enabled = mp3files.Length == 0;

                // 检测表情文件
                var emotionfiles = Directory.GetFiles($"{GloableVars.filePath}\\CustomEmotion", "*.gif", SearchOption.AllDirectories);
                button6.Enabled = emotionfiles.Length == 0;

                // 检测头像文件夹
                var headimagefiles = Directory.GetFiles(Path.Combine(GloableVars.filePath, "headimage"), "*.jpg", SearchOption.AllDirectories);
                button5.Enabled = headimagefiles.Length == 0;

            }));
        }
    }
}

using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Net;
using static wxreader.GloableVars;

namespace wxreader
{
    public partial class Form1 : Form, IProgressReporter
    {
        //����ȫ�ֱ���
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

            //���а�ť������
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = false;

            GloableVars.MonitoredVariable.ValueChanged += MonitoredVariable_ValueChanged; // �����¼�

            GloableVars.ProcessingVariable.ValueChanged += UpdataProcessingVariable; // �����¼�

            GloableVars.NoCondition.ValueChanged += NoCondition_ValueChanged; // �����¼�

            //���ú�̨�߳�
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


            //���ý�������ʼֵ
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

                // ȷ�������¼��������
                // �ӳ�һ��ʱ�䣬ȷ�� UI ���Խ��д���
                await Task.Delay(100); // ʹ���첽�ӳ�

                // �رմ��������ֱ���˳�Ӧ��
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"��������: {ex.Message}");
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
            MessageBox.Show("������ɣ�");
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
            MessageBox.Show("�ϲ����,�����Ϣ�Ѿ��洢��TRUEMSG���У�");
            button2.Enabled = true;
            //�ر����ݿ�����
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
            SQLiteConnection.ClearAllPools();//�������ݿ����ӳ�
            //�������ݿ���ʱ�ļ�
            foreach (GloableVars.fileinfo file in extfiles)
            {
                if (File.Exists(file.filepath + "-wal"))
                {
                    //����ļ��Ƿ���������ռ��
                    if (FolderFileSearcher.IsFileLocked(file.filepath + "-wal"))
                    {
                        MessageBox.Show("�ļ�" + file.filepath + "-wal" + "��ռ�ã�");
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

                        MessageBox.Show("�ļ�" + file.filepath + "-shm" + "��ռ�ã�");
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
            //�����ļ��б���ȡ����
            connection.connectingName = extfiles[0].filename;
            foreach (GloableVars.fileinfo file in extfiles)
            {

                //connection.connectingName = file.filename;
                if (extfiles.IndexOf(file) == 0)
                {
                    //connection.connectingName = file.filename;
                    //connstr = @"Data Source=" + file.filepath + " ";
                    connection.connection = SqliteHelper.GetConnection(@"Data Source=" + file.filepath + ";Version=3;");
                    //���д����Ѿ����������ݿ����Ӳ���������ִ�д򿪲���
                }
                else
                {
                    SqliteHelper.ExecuteNonQuery(@"ATTACH DATABASE '" + file.filepath + "' AS " + Path.GetFileNameWithoutExtension(file.filename) + ";", connection.connection);
                    //�������ݿ⵽��ǰ����
                }
            }
            connetions.Add(connection);//��ӵ������б�

            string sql = "CREATE TABLE MEGEDMSG AS ";
            //�����ļ��б���ȡ��ṹ
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
            //�ϲ���Ϣ���ݿ�
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
            //���̲߳�ѯͬ��ִ��
            if (!SqliteHelper.CheckTableExists(connetions[0].connection, "MEGEDMSG"))
            {
                SqliteHelper.ExecuteNonQuery(sql, connetions[0].connection);
            }
            else
            {
                MessageBox.Show("MEGEDMSG���Ѵ��ڣ�");
            }
            progress += 50;
            worker.ReportProgress(progress);
            //MessageBox.Show("��Ϣ���ݿ�ϲ���ɣ�");

            sql = "CREATE TABLE MEGEDMEDIA AS ";
            //�ϲ�ý�����ݿ�
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
                            sql = sql.Substring(0, sql.Length - 6);//ȥ������UNION
                        }
                    }
                }
                progress += 1;
                worker.ReportProgress(progress);
            }
            //�ϲ�ý�����ݿ�
            if (!SqliteHelper.CheckTableExists(connetions[0].connection, "MEGEDMEDIA"))
            {
                SqliteHelper.ExecuteNonQuery(sql, connetions[0].connection);
            }
            else
            {
                MessageBox.Show("MEGEDMEDIA���Ѵ��ڣ�");
            }
            worker.ReportProgress(66);

            //�ϲ���Ϣ��ý�����ݿ�
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
                MessageBox.Show("ALLMESG���Ѵ��ڣ�");
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
                    MessageBox.Show("TRUEMSG���Ѵ��ڣ�");
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
            worker.ReportProgress(100);
            connetions[0].connection.Close();

            //�½����ݿ�����
            connetions[0].connection.Open();
            SqliteHelper.ExecuteNonQuery("DROP TABLE IF EXISTS MEGEDMSG", connetions[0].connection);
            SqliteHelper.ExecuteNonQuery("DROP TABLE IF EXISTS MEGEDMEDIA", connetions[0].connection);
            SqliteHelper.ExecuteNonQuery("DROP TABLE IF EXISTS ALLMESG", connetions[0].connection);
            SqliteHelper.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS ProgramInfo (id INTEGER PRIMARY KEY AUTOINCREMENT,wxid TEXT NOT NULL,TotalMessageCount INTEGER,VoiceMessageCount INTEGER,VaildVoiceMessageCount INTEGER);", connetions[0].connection);
            connetions[0].connection.Close();

            MessageBox.Show("�м����ɾ����");
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }


        private void button1_Click(object sender, EventArgs e)
        {
            //���ļ���ѡ��Ի���
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            DialogResult result = folderBrowserDialog1.ShowDialog();
            FolderFileSearcher ffsearcher = new FolderFileSearcher();
            if (result == DialogResult.OK)
            {
                //��ȡѡ����ļ���·��
                GloableVars.filePath = folderBrowserDialog1.SelectedPath;
                //�����ı�����ʾ·��
                textBox1.Text = GloableVars.filePath;
                //���ð�ť����
                button1.Enabled = true;

                //�����ļ��м����ļ����������ļ�
                filelist = ffsearcher.SearchFiles(GloableVars.filePath);

                List<string> defaultdbtext = new List<string>();

                //���Ĭ���б�����
                defaultdbtext.Add("de_MicroMsg");
                defaultdbtext.Add("de_MediaMSG");
                defaultdbtext.Add("de_MSG");

                //������ָ�����ݿ��ļ�
                extfiles = ffsearcher.CheckFileInfoFromFilnameList(filelist, defaultdbtext);

                //�����ļ��б���ʾ
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
                //��ʾ��ʧ��ԭ��
                MessageBox.Show("·����ʧ�ܣ�" + result.ToString());
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // ������̨����
            button2.Enabled = false;
            worker.RunWorkerAsync();
        }

        //�������ݿ��е�buf�ֶ�
        public static async Task DecodeBufAsync(SQLiteConnection connection, string strTalker)
        {
            // ����silkdecoderʵ�����������ݿ����Ӻ���󲢷���
            var decoder = new silkdecoder(connection, maxConcurrency: 10); // ������󲢷���Ϊ10

            // ��������ƵID
            List<GloableVars.voiceinfo> audioIds = GetAudioIds(connection, strTalker); // ��������һ��������������ƵID���б�
            GloableVars.voiceList = audioIds;

            if (audioIds.Count == 0)
            {
                Console.WriteLine($"{strTalker}û����Ƶ����!");
                //silkdecoder.WriteLog($"{GloableVars.filePath}\\contract\\{strTalker}\\silk", $"{strTalker}û����Ƶ����!");
                return;
            }
            // �������ת������
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
                //���������ﵽ��󲢷���ʱ���ȴ��������
                if (tasks.Count == 10)
                {
                    await Task.WhenAny(tasks);
                    tasks.Remove(tasks.FirstOrDefault(t => t.IsCompleted));
                }
            }

            // �ȴ������������
            await Task.WhenAll(tasks);

            Console.WriteLine("����ת�����������");

        }

        public static List<GloableVars.voiceinfo> GetAudioIds(SQLiteConnection connection, string strTalker)
        {
            if (connection.State == System.Data.ConnectionState.Closed)
            {
                connection.Open(); 
            }
            try
            {
                SQLiteDataReader reader = SqliteHelper.ExecuteReader("SELECT MsgSvrID,Buf FROM TRUEMSG where StrTalker = '" + strTalker + "' and Type=34", connection);//��������һ������MsgSvrID�ı���
                List<string> audioIds = new List<string>();
                List<GloableVars.voiceinfo> voiceinfos = new List<GloableVars.voiceinfo>();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        //�ж�msgsvrID�Ƿ�Ϊ��
                        string audioId = reader["MsgSvrID"] == DBNull.Value ? "" : reader.GetInt64(0).ToString();
                        //�ж�buf�Ƿ�Ϊ��
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
        /// ����strtalkerĿ¼
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
                //���ļ���ѡ��Ի���
                FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
                DialogResult result = folderBrowserDialog1.ShowDialog();
                if (result == DialogResult.OK)
                {
                    GloableVars.filePath = folderBrowserDialog1.SelectedPath;
                    //�����ı�����ʾ·��
                    textBox1.Text = GloableVars.filePath;
                    //���ð�ť����
                    button1.Enabled = true;
                }
                else
                {
                    //��ʾ��ʧ��ԭ��
                    MessageBox.Show("·����ʧ�ܣ�" + result.ToString());
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
                MessageBox.Show("TRUEMSG������,���Ⱥϲ����ݿ⣡");
            }
            //�ر��������ݿ�����
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
                //���ļ���ѡ��Ի���
                FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
                DialogResult result = folderBrowserDialog1.ShowDialog();
                if (result == DialogResult.OK)
                {
                    GloableVars.filePath = folderBrowserDialog1.SelectedPath;
                    //�����ı�����ʾ·��
                    textBox1.Text = GloableVars.filePath;
                    //���ð�ť����
                    button1.Enabled = true;
                }
                else
                {
                    //��ʾ��ʧ��ԭ��
                    MessageBox.Show("·����ʧ�ܣ�" + result.ToString());
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
            //��ȡ���ݿ�
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
            MessageBox.Show($"{count}��ͷ��������ɣ�����{GloableVars.filePath+@"\headimage"}�ļ����·�default.jpg��ΪĬ��ͷ��");
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
                    MessageBox.Show("CustomEmotion�ļ���û���ļ����ļ��в����ڣ�");
                    return;
                }
            }
            else
            {
                //���ļ���ѡ��Ի���
                FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
                DialogResult result = folderBrowserDialog1.ShowDialog();
                if (result == DialogResult.OK)
                {
                    GloableVars.filePath = folderBrowserDialog1.SelectedPath;
                    //�����ı�����ʾ·��
                    textBox1.Text = GloableVars.filePath;
                    //��ȡ�ļ����������ļ�
                    encryptedFiles = Directory.GetFiles($"{GloableVars.filePath}\\CustomEmotion", "*", SearchOption.AllDirectories);
                    if (encryptedFiles.Length == 0)
                    {
                        MessageBox.Show("��ѡ�����CustomEmotion�ļ��е�Ŀ¼��");
                        return;
                    }
                }
                else
                {
                    //��ʾ��ʧ��ԭ��
                    MessageBox.Show("·����ʧ�ܣ�" + result.ToString());
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
                MessageBox.Show("CustomEmotion�����ڣ�");
                connection.Close();
                return;
            }
            connection.Close();
            MessageBox.Show("�ɹ���ȡ��" + emotioninfos.Count + "����������,����������...");

            //���������
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
                            //����ʧ�ܣ�aeskey�Ǵ����ݿ��ж�ȡ�ģ���������ֻ��ivҲ�Ǵ����ݿ�������һ���ֶζ�ȡ�ģ��޷�ȷ���Ƿ���ȷ������
                            //�߼��Ƿ���ȷҲ�޴ӿ�֤���������������ԿҲ�Ǵ����ϵ������������ģ����Բ�֪���Ƿ���ȷ
                            //aesdencrpyt.DecryptImage(file, $"{Path.GetDirectoryName(file)}\\{item.emotionname}.gif");

                            //������һ��·�ߣ����Ǵ���������ͼƬ��Ȼ�󱣴浽����
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
            MessageBox.Show("������������ɣ�");

        }

        private void button7_Click(object sender, EventArgs e)
        {
            //Form4 form4 = GloableVars.startWatingWindow();
            Thread thread = new Thread(new ThreadStart(ShowForm4));
            thread.Start();

            List<GloableVars.wxuser> wxusers = GetWxUsers(SqliteHelper.GetConnection($"Data Source={GloableVars.filePath}\\de_MicroMsg.db ;Version=3;"));
            if (wxusers.Count == 0)
            {
                MessageBox.Show("û���ҵ���ϵ����Ϣ��");
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

        //���GloableVars.filePath�Ƿ����,�����򲻲���������������ļ���ѡ��Ի���
        private bool CheckKeyDBFiles()
        {
            if (GloableVars.filePath == null)
            {
                //���ļ���ѡ��Ի���
                FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
                DialogResult result = folderBrowserDialog1.ShowDialog();
                if (result == DialogResult.OK)
                {
                    //���߳���ʾform4
                    Thread thread = new Thread(new ThreadStart(ShowForm4));
                    thread.Start();
                    GloableVars.filePath = folderBrowserDialog1.SelectedPath;
                    //�����ı�����ʾ·��
                    textBox1.Text = GloableVars.filePath;
                    //���ð�ť����
                    if (textBox1.Text != null)
                    {
                        button1.Enabled = false;
                    }
                    var dbfiles = Directory.GetFiles(GloableVars.filePath, "de_MicroMsg.db", SearchOption.TopDirectoryOnly);
                    if (dbfiles.Length == 0)
                    {
                        CloseForm4();
                        MessageBox.Show("û���ҵ�΢�����ݿ��ļ�����ȷ��·���Ƿ���ȷ���������ݿ��Ƿ���ܣ�");
                        //�˳�����
                        Application.Exit();
                        return false;
                    }
                    GloableVars.selfwxid = GetSelfID();
                    if (GloableVars.selfwxid == "")
                    {
                        CloseForm4();
                        MessageBox.Show("��ȡ�Լ���΢��IDʧ�ܣ�");
                        Application.Exit();
                        return false;
                    }
                }
                else
                {
                    CloseForm4();
                    //��ʾ��ʧ��ԭ��
                    MessageBox.Show("·����ʧ�ܣ��˳�����");
                    this.Close();
                    Application.Exit();
                    return false;
                }
            }

            //�����ļ��м����ļ����������ļ�
            FolderFileSearcher ffsearcher = new FolderFileSearcher();
            filelist = ffsearcher.SearchFiles(GloableVars.filePath);

            List<string> defaultdbtext = new List<string>();

            //���Ĭ���б�����
            defaultdbtext.Add("de_MicroMsg");
            defaultdbtext.Add("de_MediaMSG");
            defaultdbtext.Add("de_MSG");

            //������ָ�����ݿ��ļ�
            extfiles = ffsearcher.CheckFileInfoFromFilnameList(filelist, defaultdbtext);

            //�����ļ��б���ʾ
            listView1.Items.Clear();
            foreach (GloableVars.fileinfo file in extfiles)
            {
                ListViewItem item = new ListViewItem(file.filename);
                item.SubItems.Add(file.filepath);
                listView1.Items.Add(item);
            }

            //���а�ť������
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
            MessageBox.Show("����ѡ��΢�Ž������ݿ��Ŀ¼��");

            //thread.Join();

            if (GloableVars.selfwxid == "")
            {
                MessageBox.Show("��ȡ�Լ���΢��IDʧ�ܣ�");
                Application.Exit();
                return;
            }

            if (!CheckKeyDBFiles())
            {
                Application.Exit();
                return;
            }

            SqliteHelper.InitSqlite(); // ��ʼ�����ݿ�

            // ����TRUEMSG�Ƿ����
            button2.Enabled = !SqliteHelper.CheckTableExists("TRUEMSG");

            // ���Decode�ļ����Ƿ����
            if (!Directory.Exists(Path.Combine(GloableVars.filePath, "MsgAttach")))
            {
                MessageBox.Show("MsgAttach�ļ��в�����,���΢�Ű�װĿ¼�¿������ļ��У�");
                Application.Exit();
                return;
            }
            var decodefiles = Directory.GetFiles($"{GloableVars.filePath}\\MsgAttach", "*.jpeg", SearchOption.AllDirectories);
            button3.Enabled = decodefiles.Length == 0;


            // ���MP3�ļ�
            if (!Directory.Exists(Path.Combine(GloableVars.filePath, "contract")))
            {
                //ȷ�϶Ի���
                DialogResult result = MessageBox.Show("contract�ļ��в�����,�Ƿ񴴽����ļ��У�", "��ʾ", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(Path.Combine(GloableVars.filePath, "contract"));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("����contract�ļ���ʧ�ܣ�" + ex.Message);
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


            // �������ļ�
            if (!Directory.Exists(Path.Combine(GloableVars.filePath, "CustomEmotion")))
            {
                MessageBox.Show("CustomEmotion�ļ��в�����,���΢�Ű�װĿ¼�¿������ļ��У�");
                Application.Exit();
                return;
            }
            var emotionfiles = Directory.GetFiles($"{GloableVars.filePath}\\CustomEmotion", "*.gif", SearchOption.AllDirectories);
            button6.Enabled = emotionfiles.Length == 0;


            // ���ͷ���ļ���
            if (!Directory.Exists(Path.Combine(GloableVars.filePath, "headimage")))
            {
                //ȷ�϶Ի���
                DialogResult result = MessageBox.Show("headimage�ļ��в�����,�Ƿ񴴽����ļ��У�", "��ʾ", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(Path.Combine(GloableVars.filePath, "headimage"));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("����headimage�ļ���ʧ�ܣ�" + ex.Message);
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

            // ����һ���µ��߳���ִ�к�ʱ����
            // ʹ���߳�ʹ�� UI ���ᱻ����
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
            form4 = new Form4(); // ���� Form4 ʵ��
            form4.TopMost = true; // �ö���ʾ
            Application.Run(form4); // ȷ�������߳�����ʾ��
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
            SqliteHelper.InitSqlite(); // ��ʼ�����ݿ�

            // �����̸߳���UI
            this.Invoke(new Action(() =>
            {
                // ����TRUEMSG�Ƿ����
                button2.Enabled = !SqliteHelper.CheckTableExists("TRUEMSG");

                // ���Decode�ļ����Ƿ����
                var decodefiles = Directory.GetFiles($"{GloableVars.filePath}\\MsgAttach", "*.jpeg", SearchOption.AllDirectories);
                button3.Enabled = decodefiles.Length == 0;

                // ���MP3�ļ�
                var mp3files = Directory.GetFiles($"{GloableVars.filePath}\\contract", "*.mp3", SearchOption.AllDirectories);
                button4.Enabled = mp3files.Length == 0;

                // �������ļ�
                var emotionfiles = Directory.GetFiles($"{GloableVars.filePath}\\CustomEmotion", "*.gif", SearchOption.AllDirectories);
                button6.Enabled = emotionfiles.Length == 0;

                // ���ͷ���ļ���
                var headimagefiles = Directory.GetFiles(Path.Combine(GloableVars.filePath, "headimage"), "*.jpg", SearchOption.AllDirectories);
                button5.Enabled = headimagefiles.Length == 0;

            }));
        }
    }
}

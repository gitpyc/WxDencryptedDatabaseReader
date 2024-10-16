using ESFramework.Boost;
using ESFramework.Extensions.ChatRendering;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using System.Threading;
using static wxreader.GloableVars;
using System.Data.SQLite;
using System.Numerics;
using System.Web;
using System.Security;

namespace wxreader
{
    public partial class ChatHistory : Form
    {
        //private int offsetY => this.chatBox1.VerticalScroll.Value;
        //private IChatRender chatRender;
        //private RenderDataProvider renderDataProvider;
        private string wxid;
        public static Dictionary<string, Image> imageFileList = new Dictionary<string, Image>();
        public static DateTime lastMessageTime;
        private const int PageSize = 100; // 每次加载的消息条数
        private int currentPage = 0; // 当前页数
        private List<GloableVars.TansMsg> transmsglist;
        private const int MaxMessages = 200; // 定义最大消息数量

        private System.Windows.Forms.Timer animationTimer;
        private float animationProgress = 0f;

        private TaskCompletionSource<bool> _decodeCompletionSource;//语音文件解码完成事件

        private Dictionary<string, Image> textEmotionDict = new Dictionary<string, Image>();

        public ChatHistory(string wxid)

        {
            InitializeComponent();

            //检测对应目录下是否有语音文件,如果没有则进行语音文件解码
            _decodeCompletionSource = new TaskCompletionSource<bool>();

            // 创建一个新的任务来调用异步方法
            Task.Run(async () => await DecodeOnRun(wxid));

            // 等待 DecodeOnRun 完成
            _decodeCompletionSource.Task.Wait();

            LoadImageSource();
            headstring=this.Text = loadStrNameOrNick(wxid);
            panel1.AutoScroll = true;
            panel1.Size = new Size(400, 1000);
            panel1.VerticalScroll.Enabled = true;


            /*this.renderDataProvider = new RenderDataProvider();
            this.chatRender = ChatRenderFactory.CreateChatRender(renderDataProvider, chatBox1, GloableVars.selfwxid, wxid, false);
            this.chatBox1.Initialize(this.chatRender);
            this.chatRender.AudioMessageClicked += ChatRender_AudioMessageClicked;
            this.chatRender.ContextMenuCalled += ChatRender_ContextMenuCalled;*/
            this.wxid = wxid;
            //GloableVars.selfwxid = "wxid_huv1d35ubf9k22";
            //获取聊天记录
            var sqlconn = SqliteHelper.GetConnection($"Data Source={GloableVars.filePath}\\de_MicroMsg.db ;Version=3;");
            var chatHistory = SqliteHelper.ExecuteReader($"SELECT * FROM TRUEMSG where StrTalker='{wxid}' ORDER by Sequence desc", sqlconn);
            GloableVars.truemsglist = new List<GloableVars.truemsg>();
            GloableVars.truemsg truemsg = null;
            if (chatHistory.HasRows)
            {
                while (chatHistory.Read())
                {
                    truemsg = new GloableVars.truemsg();
                    truemsg.Sequence = chatHistory["Sequence"] == DBNull.Value ? "" : chatHistory.GetInt64(chatHistory.GetOrdinal("Sequence")).ToString();
                    truemsg.StrTalker = chatHistory["StrTalker"] == DBNull.Value ? "" : chatHistory["StrTalker"].ToString();
                    truemsg.CreateTime = chatHistory["CreateTime"] == DBNull.Value ? "" : chatHistory.GetInt64(chatHistory.GetOrdinal("CreateTime")).ToString(); ;
                    truemsg.IsSender = chatHistory["IsSender"] == DBNull.Value ? 0 : Convert.ToInt32(chatHistory["IsSender"]);
                    truemsg.StrContent = chatHistory["StrContent"] == DBNull.Value ? "" : chatHistory["StrContent"].ToString();
                    truemsg.Type = chatHistory["Type"] == DBNull.Value ? 0 : Convert.ToInt32(chatHistory["Type"]);
                    truemsg.SubType = chatHistory["SubType"] == DBNull.Value ? 0 : Convert.ToInt32(chatHistory["SubType"]);
                    truemsg.BytesExtra = chatHistory["BytesExtra"] == DBNull.Value ? null : (byte[])chatHistory["BytesExtra"];
                    truemsg.MsgSvrId = chatHistory["MsgSvrId"] == DBNull.Value ? "" : chatHistory.GetInt64(chatHistory.GetOrdinal("MsgSvrId")).ToString();
                    truemsg.DisplayContent = chatHistory["DisplayContent"] == DBNull.Value ? "" : chatHistory["DisplayContent"].ToString();
                    GloableVars.truemsglist.Add(truemsg);
                }
            }
            else
            {
                MessageBox.Show("该联系人没有找到聊天记录！");
            }
            LoadMessage(wxid);
            ShowMessage();
        }

        private string loadStrNameOrNick(string wxid)
        {
            string strNameOrNick = "", userName = "";
            var sqlconn = SqliteHelper.GetConnection($"Data Source={GloableVars.filePath}\\de_MicroMsg.db ;Version=3;");
            var strNameOrNickResult = SqliteHelper.ExecuteReader($"SELECT * FROM Contact where UserName='{wxid}'", sqlconn);
            if (strNameOrNickResult.HasRows)
            {
                while (strNameOrNickResult.Read())
                {
                    userName = strNameOrNickResult["Remark"] == DBNull.Value ? "" : strNameOrNickResult["Remark"].ToString();
                    strNameOrNick = strNameOrNickResult["NickName"] == DBNull.Value ? "" : strNameOrNickResult["NickName"].ToString();
                }
            }
            return $"与\"{userName}\"/\"{strNameOrNick}\"的聊天记录";
        }

        private async Task DecodeOnRun(string xid)
        {
            bool isgroup = xid.Contains("@chatroom");
            SQLiteConnection conn = new SQLiteConnection($"Data Source={GloableVars.filePath}\\de_MicroMsg.db ;Version=3;");
            conn.Open();
            if(!GloableVars.IsDirExist(Path.Combine(GloableVars.filePath, "contract", xid)))
            {
                Directory.CreateDirectory(Path.Combine(GloableVars.filePath, "contract", xid));
            }
            string[] mp3Files = Directory.GetFiles(GloableVars.filePath + "\\contract\\" + xid, "*.mp3", SearchOption.AllDirectories);
            //检查语音文件数量是否正确
            SQLiteCommand command = new($"select count(*) as voicecount FROM TRUEMSG where StrTalker='{xid}' AND Type = 34 AND Buf NOT NULL", conn);
            SQLiteDataReader reader;
            try
            {
                reader = command.ExecuteReader();
            }
            catch (Exception e)
            {
                silkdecoder.WriteLog(Path.Combine(GloableVars.filePath, "contract", xid, "log.txt"), e.Message);
                _decodeCompletionSource.SetResult(true);
                return;
            }
            int voicecount = 0;
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    voicecount = reader.GetInt32(reader.GetOrdinal("voicecount"));
                }
            }
            reader.Close();
            conn.Close();
            while (voicecount != 0)//数量正确则退出循环
            {
                if (mp3Files.Length == 0 || mp3Files.Length != voicecount)
                {
                    if (Directory.GetFiles(GloableVars.filePath + "\\contract\\" + xid, "*.silk", SearchOption.AllDirectories).Length == 0)
                    {
                        if (!Directory.Exists(GloableVars.filePath + "\\contract\\" + xid + "\\silk") && !Directory.Exists(GloableVars.filePath + "\\contract\\" + xid + "\\mp3"))
                        {
                            var silkDir = Directory.CreateDirectory(GloableVars.filePath + "\\contract\\" + xid + "\\silk");
                            var mp3Dir = Directory.CreateDirectory(GloableVars.filePath + "\\contract\\" + xid + "\\mp3");
                        }
                        else if (!Directory.Exists(GloableVars.filePath + "\\contract\\" + xid + "\\silk"))
                        {
                            var silkDir = Directory.CreateDirectory(GloableVars.filePath + "\\contract\\" + xid + "\\silk");
                        }
                        else if (!Directory.Exists(GloableVars.filePath + "\\contract\\" + xid + "\\mp3"))
                        {
                            var mp3Dir = Directory.CreateDirectory(GloableVars.filePath + "\\contract\\" + xid + "\\mp3");
                        }
                    }
                    await Task.Run(() => GloableVars.DecodeVoice(conn, xid));
                    /*else
                    {
                        await Task.Run(() => GloableVars.DecodeVoice(conn, xid));
                        // 完成任务
                        //_decodeCompletionSource.SetResult(true);
                    }*/
                }
                else
                {
                    break;
                }
            }
            _decodeCompletionSource.SetResult(true);
        }
        private void LoadMessage(string wxid)
        {
            //获取聊天记录
            if (GloableVars.transmsglist == null || GloableVars.transmsglist.Count != GloableVars.truemsglist.Count)
            {
                GloableVars.transmsglist = TransToLocalfile(GloableVars.truemsglist);
            }
        }
        private void LoadImageSource()
        {
            if (imageFileList.Count == 0)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "FileIcon\\");
                FileInfo[] files = directoryInfo.GetFiles("*.png");
                for (int i = 0; i < files.Length; i++)
                {
                    string headFilePath = AppDomain.CurrentDomain.BaseDirectory + $"FileIcon\\{files[i]}";
                    string fileToken = Path.GetFileNameWithoutExtension(headFilePath).Replace("file_type_", "");
                    imageFileList.Add(fileToken, Image.FromFile(headFilePath));
                }
            }
            //获取wxemoji目录下所有文件
            string[] wxemojifiles = Directory.GetFiles("wxemoji", "*.png", SearchOption.TopDirectoryOnly);
            foreach (var item in wxemojifiles)
            {
                textEmotionDict.Add($"[{Path.GetFileNameWithoutExtension(item)}]", Image.FromFile(item));
            }
        }

        private void Form5_Load(object sender, EventArgs e)
        {
            this.Activate();
        }

        private void ChatRender_AudioMessageClicked(string arg1, object arg2)
        {
        }

        private void ChatRender_ContextMenuCalled(Point point, ChatMessageType type, string arg3)
        {
        }

        public static List<GloableVars.TansMsg> TransToLocalfile(List<GloableVars.truemsg> truemsglist)
        {
            List<GloableVars.TansMsg> transmsglist = new List<GloableVars.TansMsg>();
            int count = 0;
            if (truemsglist.Count > 0)
            {
                foreach (var truemsg in truemsglist)
                {
                    GloableVars.TansMsg transmsg = new GloableVars.TansMsg();
                    //判断是否是第一条消息
                    if (count == 0)
                    {
                        lastMessageTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(truemsg.CreateTime)).DateTime;
                    }
                    switch (truemsg.Type)
                    {
                        case 1://文字消息
                            transmsg.StrContent = truemsg.StrContent;
                            transmsg.StrTalker = truemsg.StrTalker;
                            transmsg.Type = truemsg.Type;
                            transmsg.IsSender = truemsg.IsSender;
                            transmsg.CreateTime = truemsg.CreateTime;
                            transmsg.Sequence = truemsg.Sequence;
                            transmsg.MsgSvrId = truemsg.MsgSvrId;
                            transmsglist.Add(transmsg);
                            break;
                        case 3://图片消息
                            transmsg.StrContent = truemsg.StrContent;
                            transmsg.StrTalker = truemsg.StrTalker;
                            transmsg.Type = truemsg.Type;
                            transmsg.IsSender = truemsg.IsSender;
                            transmsg.CreateTime = truemsg.CreateTime;
                            transmsg.Sequence = truemsg.Sequence;
                            transmsg.MsgSvrId = truemsg.MsgSvrId;
                            if (truemsg.BytesExtra != null)
                            {
                                //byte转字符串
                                string imagepath = Encoding.UTF8.GetString(truemsg.BytesExtra);

                                //查找msgattach出现的次数
                                int msgattachcount = Regex.Matches(imagepath, "MsgAttach").Count;
                                bool isfullthumbname = false;

                                if (msgattachcount == 2)//检测内容是否是有高清图和模糊图
                                {
                                    //将imagepath根据wxid分割为两部分
                                    //string[] bigimagepath = imagepath.Split(GloableVars.selfwxid.ToCharArray());
                                    string bigimagepath, tempsmallimagepath, smallimagepath, splitempath;

                                    if (imagepath.IndexOf("Thumb") > imagepath.IndexOf("Image"))//判断字符串顺序
                                    {
                                        if (imagepath.IndexOf("_thumb.dat") > 0 && imagepath.IndexOf("_thumb.dat") < imagepath.IndexOf("Image"))//判断字符串顺序
                                        {
                                            smallimagepath = imagepath.Substring(imagepath.IndexOf(GloableVars.selfwxid), imagepath.IndexOf("_thumb.dat") - imagepath.IndexOf(GloableVars.selfwxid));
                                            smallimagepath = smallimagepath.Substring(smallimagepath.IndexOf("MsgAttach"), smallimagepath.Length - smallimagepath.IndexOf("MsgAttach"));
                                            bigimagepath = imagepath.Substring(imagepath.IndexOf(smallimagepath) + 10, imagepath.Length - (imagepath.IndexOf(smallimagepath) + 10));
                                            bigimagepath = bigimagepath.Substring(bigimagepath.IndexOf("MsgAttach"), bigimagepath.IndexOf(".dat") - bigimagepath.IndexOf("MsgAttach"));
                                            isfullthumbname = true;
                                        }
                                        else
                                        {
                                            bigimagepath = imagepath.Substring(imagepath.IndexOf(GloableVars.selfwxid), imagepath.IndexOf(".dat") - imagepath.IndexOf(GloableVars.selfwxid));
                                            tempsmallimagepath = imagepath.Substring(imagepath.IndexOf(bigimagepath) + bigimagepath.Length, imagepath.IndexOf("_t.dat") - imagepath.IndexOf(bigimagepath) - bigimagepath.Length);
                                            smallimagepath = tempsmallimagepath.Substring(tempsmallimagepath.IndexOf("MsgAttach"), tempsmallimagepath.Length - tempsmallimagepath.IndexOf("MsgAttach"));
                                            bigimagepath = bigimagepath.Substring(bigimagepath.IndexOf("MsgAttach"), bigimagepath.Length - bigimagepath.IndexOf("MsgAttach"));
                                        }
                                    }
                                    else
                                    {
                                        if (imagepath.Contains("_thumb.dat"))
                                        {
                                            smallimagepath = imagepath.Substring(imagepath.IndexOf(GloableVars.selfwxid), imagepath.IndexOf("_thumb.dat") - imagepath.IndexOf(GloableVars.selfwxid));
                                            smallimagepath = smallimagepath.Substring(smallimagepath.IndexOf("MsgAttach"), smallimagepath.Length - smallimagepath.IndexOf("MsgAttach"));
                                            bigimagepath = imagepath.Substring(imagepath.IndexOf(smallimagepath) + smallimagepath.Length + 10, imagepath.Length - (imagepath.IndexOf(smallimagepath) + smallimagepath.Length + 10));
                                            bigimagepath = bigimagepath.Substring(bigimagepath.IndexOf("MsgAttach"), bigimagepath.IndexOf(".dat") - bigimagepath.IndexOf("MsgAttach"));
                                            isfullthumbname = true;
                                        }
                                        else
                                        {
                                            smallimagepath = imagepath.Substring(imagepath.IndexOf(GloableVars.selfwxid), imagepath.IndexOf("_t.dat") - imagepath.IndexOf(GloableVars.selfwxid));
                                            //string[] temp = Regex.Split(imagepath, smallimagepath+"_t.dat");
                                            splitempath = imagepath.Substring(imagepath.IndexOf(smallimagepath + "_t.dat") + smallimagepath.Length + 6, imagepath.Length - (imagepath.IndexOf(smallimagepath + "_t.dat") + smallimagepath.Length + 6));
                                            //tempsmallimagepath = imagepath.Substring(imagepath.IndexOf(smallimagepath) + smallimagepath.Length, imagepath.IndexOf(".dat") - imagepath.IndexOf(smallimagepath) - smallimagepath.Length);
                                            bigimagepath = splitempath.Substring(splitempath.IndexOf("MsgAttach"), splitempath.IndexOf(".dat") - splitempath.IndexOf("MsgAttach"));
                                            smallimagepath = smallimagepath.Substring(smallimagepath.IndexOf("MsgAttach"), smallimagepath.Length - smallimagepath.IndexOf("MsgAttach"));
                                        }
                                    }

                                    string bigimagefilepath = bigimagepath + ".jpg";
                                    bigimagefilepath = Path.Combine(GloableVars.filePath, bigimagefilepath);
                                    string[] bigfiles = null;
                                    if (Directory.Exists(Path.GetDirectoryName(bigimagefilepath) + "\\Decode"))
                                    {
                                        bigfiles = Directory.GetFiles(Path.GetDirectoryName(bigimagefilepath) + "\\Decode", "*", SearchOption.TopDirectoryOnly);
                                        bigimagefilepath = getContainNameFile(Path.GetFileNameWithoutExtension(bigimagefilepath), bigfiles);//获取包含文件名的高清图片
                                    }
                                    string smallimagefilepath = isfullthumbname ? smallimagepath + "_thumb.jpg" : smallimagepath + "_t.jpg";
                                    smallimagefilepath = Path.Combine(GloableVars.filePath, smallimagefilepath);
                                    string[] smalfiles = null;
                                    if (Directory.Exists(Path.GetDirectoryName(smallimagefilepath) + "\\Decode"))
                                    {
                                        smalfiles = Directory.GetFiles(Path.GetDirectoryName(smallimagefilepath) + "\\Decode", "*", SearchOption.TopDirectoryOnly);
                                        smallimagefilepath = getContainNameFile(Path.GetFileNameWithoutExtension(smallimagefilepath), smalfiles);//获取包含文件名的模糊图片
                                    }
                                    transmsg.ImgLocalPath = bigimagefilepath;
                                    transmsg.thumbImgLocalPath = smallimagefilepath;
                                    transmsglist.Add(transmsg);
                                }
                                else
                                {
                                    string smallimagepath = imagepath.Substring(imagepath.IndexOf("MsgAttach"), imagepath.IndexOf("_t.dat") - imagepath.IndexOf("MsgAttach"));
                                    string smallimagefilepath = smallimagepath + "_t.jpg";
                                    //获取路径下的所有文件
                                    string[] afiles = null;
                                    if (Directory.Exists(Path.GetDirectoryName(smallimagefilepath) + "\\Decode"))
                                    {
                                        afiles = Directory.GetFiles(Path.GetDirectoryName(smallimagefilepath) + "\\Decode", "*", SearchOption.TopDirectoryOnly);
                                        smallimagefilepath = getContainNameFile(Path.GetFileNameWithoutExtension(smallimagefilepath), afiles);
                                    }
                                    transmsg.thumbImgLocalPath = smallimagefilepath;
                                    transmsglist.Add(transmsg);
                                }
                            }
                            break;
                        case 34://语音消息
                            transmsg.StrContent = truemsg.StrContent;
                            transmsg.StrTalker = truemsg.StrTalker;
                            transmsg.Type = truemsg.Type;
                            transmsg.IsSender = truemsg.IsSender;
                            transmsg.CreateTime = truemsg.CreateTime;
                            transmsg.Sequence = truemsg.Sequence;
                            transmsg.MsgSvrId = truemsg.MsgSvrId;
                            string voicepath = Path.Combine(GloableVars.filePath, "contract", truemsg.StrTalker, "mp3", truemsg.MsgSvrId + ".mp3");
                            transmsg.VoiceLocalPath = File.Exists(voicepath) ? voicepath : "";
                            transmsglist.Add(transmsg);
                            break;
                        case 43://视频消息
                            transmsg.StrContent = truemsg.StrContent;
                            transmsg.StrTalker = truemsg.StrTalker;
                            transmsg.Type = truemsg.Type;
                            transmsg.IsSender = truemsg.IsSender;
                            transmsg.CreateTime = truemsg.CreateTime;
                            transmsg.Sequence = truemsg.Sequence;
                            transmsg.MsgSvrId = truemsg.MsgSvrId;
                            if (truemsg.BytesExtra != null)
                            {
                                //byte转字符串
                                string videopath = Encoding.UTF8.GetString(truemsg.BytesExtra);
                                if (Regex.Matches(videopath, "Video").Count >= 2)
                                {
                                    if (videopath.IndexOf(".jpg") > videopath.IndexOf(".mp4"))
                                    {
                                        string temp = videopath.Substring(videopath.IndexOf("Video"), videopath.IndexOf(".jpg") - videopath.IndexOf("Video") + 4);
                                        string videotemp = temp.Contains("_raw.mp4") ? temp.Substring(temp.IndexOf("_raw.mp4") + 8, temp.Length - temp.IndexOf("_raw.mp4") - 8) : temp;//丢弃_raw.mp4
                                        string a = videotemp.Substring(videotemp.IndexOf("Video"), videotemp.IndexOf(".mp4") - videotemp.IndexOf("Video") + 4);
                                        temp = videotemp.Substring(videotemp.IndexOf(".mp4") + 4, videotemp.Length - videotemp.IndexOf(".mp4") - 4);//丢弃.mp4
                                        temp = temp.Substring(temp.IndexOf("Video"), temp.IndexOf(".jpg") - temp.IndexOf("Video") + 4);
                                        transmsg.VideoPreviewImgLocalPath = File.Exists(Path.Combine(GloableVars.filePath, temp)) ? Path.Combine(GloableVars.filePath, temp) : "";
                                        transmsg.VideoLocalPath = Path.Combine(GloableVars.filePath, a);
                                        transmsg.VideoLocalPath = File.Exists(transmsg.VideoLocalPath) ? transmsg.VideoLocalPath : "";
                                        transmsglist.Add(transmsg);
                                    }
                                    else
                                    {
                                        if (videopath.Contains("_raw.mp4"))
                                        {
                                            string temp = videopath.Substring(videopath.IndexOf("raw.mp4") + 7, videopath.Length - videopath.IndexOf("raw.mp4") - 7);//丢弃raw.mp4
                                            string a = temp.Substring(temp.IndexOf("Video"), temp.IndexOf(".jpg") + 4);
                                            string b = temp.Substring(temp.IndexOf(a), temp.IndexOf(".mp4") + 4);
                                            transmsg.VideoPreviewImgLocalPath = File.Exists(Path.Combine(GloableVars.filePath, a)) ? Path.Combine(GloableVars.filePath, a) : "";
                                            transmsg.VideoLocalPath = Path.Combine(GloableVars.filePath, b);
                                            transmsg.VideoLocalPath = File.Exists(transmsg.VideoLocalPath) ? transmsg.VideoLocalPath : "";
                                            transmsglist.Add(transmsg);
                                        }
                                        else
                                        {
                                            string temp = videopath.Substring(videopath.IndexOf("Video"), videopath.IndexOf(".jpg") - videopath.IndexOf("Video") + 4);
                                            transmsg.VideoPreviewImgLocalPath = File.Exists(Path.Combine(GloableVars.filePath, temp)) ? Path.Combine(GloableVars.filePath, temp) : "";
                                            string a = videopath.Substring(videopath.IndexOf(".jpg") + 4, videopath.Length - videopath.IndexOf(".jpg") - 4);
                                            string b = a.Substring(a.IndexOf("Video"), a.IndexOf(".mp4") - a.IndexOf("Video") + 4);
                                            transmsg.VideoLocalPath = Path.Combine(GloableVars.filePath, b);
                                            transmsg.VideoLocalPath = File.Exists(transmsg.VideoLocalPath) ? transmsg.VideoLocalPath : "";
                                            transmsglist.Add(transmsg);
                                        }
                                    }
                                }
                                else
                                {
                                    if (videopath.Contains("Video"))
                                    {
                                        string temp = videopath.Substring(videopath.IndexOf("Video"), videopath.IndexOf(".jpg") - videopath.IndexOf("Video") + 4);
                                        transmsg.VideoPreviewImgLocalPath = File.Exists(Path.Combine(GloableVars.filePath, temp)) ? Path.Combine(GloableVars.filePath, temp) : "";
                                        transmsglist.Add(transmsg);
                                    }
                                }
                            }
                            break;
                        case 47://动画表情
                            transmsg.StrContent = truemsg.StrContent;
                            transmsg.StrTalker = truemsg.StrTalker;
                            transmsg.Type = truemsg.Type;
                            transmsg.IsSender = truemsg.IsSender;
                            transmsg.CreateTime = truemsg.CreateTime;
                            transmsg.Sequence = truemsg.Sequence;
                            transmsg.MsgSvrId = truemsg.MsgSvrId;



                            // 解析xml文件，处理url中的特殊字符
                            string xmlpath = HttpUtility.HtmlDecode(truemsg.StrContent); // 使用 HtmlDecode 解码

                            // 替换http之后到引号之前的所有=号，替换为"等于"
                            xmlpath = Regex.Replace(xmlpath, @"(?<=http[s]?://[^""']*?)(?<=\w*)=(?=[^""']*?(?=[""']{1}))", "等于");

                            // 替换http之后到引号之前的所有&符号，替换为"和"
                            xmlpath = Regex.Replace(xmlpath, @"(?<=http[s]?://[^""']*?)(?<=\w*)&(?=[^""']*?(?=[""']{1}))", "和");

                            XDocument document = XDocument.Parse(xmlpath);
                            var emoji = document.Descendants("emoji");

                            // 尝试获取 md5 属性
                            var emotionname = emoji.First().Attribute("md5");

                            // 如果 md5 属性不存在，则尝试获取 androidmd5 属性
                            if (emotionname == null)
                            {
                                emotionname = emoji.First().Attribute("androidmd5");
                            }

                            if (emotionname != null)
                            {
                                //获取路径下的所有文件
                                string[] files = Directory.GetFiles(Path.Combine(GloableVars.filePath, "CustomEmotion"), "*.gif", SearchOption.AllDirectories);
                                string emotionfilepath = getContainNameFile(emotionname.Value, files);
                                transmsg.EmotionLocalPath = File.Exists(emotionfilepath) ? emotionfilepath : "";
                                transmsglist.Add(transmsg);
                            }

                            break;
                        case 49://名片消息
                            break;
                        case 50://语音或视频电话
                            transmsg.StrContent = truemsg.StrContent;
                            transmsg.StrTalker = truemsg.StrTalker;
                            transmsg.Type = truemsg.Type;
                            transmsg.IsSender = truemsg.IsSender;
                            transmsg.CreateTime = truemsg.CreateTime;
                            transmsg.Sequence = truemsg.Sequence;
                            transmsg.MsgSvrId = truemsg.MsgSvrId;
                            transmsg.DisplayContent = truemsg.DisplayContent;

                            //解析xml文件，查看roomtype
                            if (!string.IsNullOrEmpty(truemsg.StrContent) && !string.IsNullOrEmpty(truemsg.DisplayContent))
                            {
                                string xml = HttpUtility.HtmlDecode(truemsg.StrContent); // 使用 HtmlDecode 解码
                                XDocument doc = XDocument.Parse(xml);
                                int roomtype = int.Parse(doc.Descendants("room_type").First().Value);
                                transmsg.IsVoipOrVideoip = roomtype; 
                            }
                            transmsglist.Add(transmsg);
                            break;
                        case 10000://系统消息
                            if (truemsg.StrContent.Contains("revokemsg"))
                            {
                                if (!string.IsNullOrEmpty(truemsg.StrContent))
                                {
                                    string xml = HttpUtility.HtmlDecode(truemsg.StrContent); // 使用 HtmlDecode 解码
                                    XDocument doc = XDocument.Parse(xml);
                                    transmsg.StrContent = doc.Descendants("revokemsg").First().Value;
                                }
                            }
                            else
                            {
                                transmsg.StrContent = truemsg.StrContent;
                            }
                            transmsg.StrTalker = truemsg.StrTalker;
                            transmsg.Type = truemsg.Type;
                            transmsg.IsSender = truemsg.IsSender;
                            transmsg.CreateTime = truemsg.CreateTime;
                            transmsg.Sequence = truemsg.Sequence;
                            transmsg.MsgSvrId = truemsg.MsgSvrId;
                            transmsglist.Add(transmsg);
                            break;
                        defalut:
                            break;
                    }
                    count++;
                }
            }
            return transmsglist;
        }
        public static string getContainNameFile(string name, string[] files)
        {
            if (files != null && files.Length > 0)
            {
                foreach (var file in files)
                {
                    if (file.Contains(name))
                    {
                        return file;
                    }
                }
            }
            else
            {
                return "";
            }
            return "";
        }

        private void AddChatBubble(GloableVars.TansMsg message)
        {
            // 创建气泡面板
            FlowLayoutPanel bubblePanel = new FlowLayoutPanel
            {
                FlowDirection = message.IsSender == 1 ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                AutoSize = true,
                MaximumSize = new Size(panel1.Width - 20, 0),
                Padding = new Padding(5),
                Margin = new Padding(0, 5, 0, 0),
                WrapContents = true,  // 关闭换行
                Dock = DockStyle.Top
            };

            // 添加头像
            PictureBox avatar = new PictureBox
            {
                Width = 40,
                Height = 40,
                Image = Image.FromFile(message.IsSender == 1 ?
                    Path.Combine(GloableVars.filePath, "headimage", GloableVars.selfwxid + ".jpg") :
                    GloableVars.userlist.Find(x => x.username == message.StrTalker).headimgurl),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Margin = new Padding(message.IsSender == 1 ? 0 : 5, 0, message.IsSender == 1 ? 5 : 0, 0) // 控制头像的边距
            };

            bubblePanel.Controls.Add(avatar); // 添加头像到气泡

            // 创建消息文字气泡
            RounderCornerBubble messagePanel = new RounderCornerBubble
            {
                // 设置背景颜色
                BackColor = message.IsSender == 1 ? Color.LightGreen : Color.White,
                AutoSize = true,
                MaximumSize = new Size(300, 0),
                Padding = new Padding(5,20,5,5),
                CornerRadius = 15, // 设置圆角半径
                BorderColor = message.IsSender == 1 ? Color.LightGreen : Color.White, // 设置边框颜色
            };


            // 创建消息标签--最古老版本，不能显示表情，但是文字没有毛病
            Label messageLabel = new Label
            {
                Text = message.StrContent,
                AutoSize = true,
                MaximumSize = new Size(280, 0),
                Padding = new Padding(10, 10, 5, 5),
                Font = new Font("Segoe UI Emoji", 9)
            };

            //让文本可以选中复制
            messageLabel.Cursor = Cursors.Default;
            messageLabel.MouseClick += (sender, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    Clipboard.SetText(messageLabel.Text);
                    MessageBox.Show("已复制到剪切板");
                }
            };


            /*//用的自定义的Label，可以显示表情，但是控制方式有点让人强迫症犯，使用padding来控制，😮，效果不好
            CustomLabel messageLabel = new CustomLabel
            {
                //Text = message.StrContent,
                imageMap = textEmotionDict,
                LabelText = message.StrContent,
                AutoSize = true,
                MaximumSize = new Size(280, 0),
                Padding = message.IsSender != 1 ?
                new Padding(TextRenderer.MeasureText(message.StrContent, new Font("Segoe UI Emoji", 9)).Width + 10, FontHeight, 0, 0) :
                new Padding(0, FontHeight, TextRenderer.MeasureText(message.StrContent, new Font("Segoe UI Emoji", 9)).Width + 10, 0),
                Font = new Font("Segoe UI Emoji", 9),
                Margin = new Padding(10, 10, 5, 5)
            };*/

            //效果还不如上面的自定义呢
            /*CustomRichTextBox messageLabel = new CustomRichTextBox
            {
                MaximumSize = new Size(280, 0),
                Font = new Font("Segoe UI Emoji", 9),
            };
            var regex = new Regex(@"\[.+?\]"); // 匹配 "[占位符]"
            var matches = regex.Matches(message.StrContent);
            foreach (Match match in matches)
            {
                string key = match.Value;
                messageLabel.AddImage(key, textEmotionDict[key]);
            }
            messageLabel.RichTextContent = message.StrContent;*/


            messagePanel.Controls.Add(messageLabel); // 添加标签到文字气泡
            bubblePanel.Controls.Add(messagePanel); // 添加消息面板到气泡面板
            // 调用 AddTimestampLabel 函数显示时间
            AddTimestampLabel(Convert.ToInt64(message.CreateTime));

            // 将气泡面板添加到主容器
            panel1.Controls.Add(bubblePanel);

            // 确保当前控制面板的内容可以滚动
            panel1.ScrollControlIntoView(bubblePanel); // 滚动到最新消息
        }

        private void AddImageBubble(GloableVars.TansMsg message)
        {
            // 创建气泡面板
            FlowLayoutPanel imageBubblePanel = new FlowLayoutPanel
            {
                FlowDirection = message.IsSender == 1 ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                AutoSize = true,
                MaximumSize = new Size(panel1.Width - 20, 0),
                Padding = new Padding(5),
                Margin = new Padding(0, 5, 0, 0),
                WrapContents = false, // 关闭换行,
                Dock = DockStyle.Top
            };

            // 添加头像
            PictureBox avatar = new PictureBox
            {
                Width = 40,
                Height = 40,
                Image = Image.FromFile(message.IsSender == 1 ?
                    Path.Combine(GloableVars.filePath, "headimage", GloableVars.selfwxid + ".jpg") :
                    GloableVars.userlist.Find(x => x.username == message.StrTalker).headimgurl),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Margin = new Padding(message.IsSender == 1 ? 0 : 5, 0, message.IsSender == 1 ? 5 : 0, 0) // 控制头像的边距
            };

            imageBubblePanel.Controls.Add(avatar); // 添加头像到气泡

            Image image = null;
            // 创建图片显示面板
            if (!String.IsNullOrEmpty(message.ImgLocalPath) && File.Exists(message.ImgLocalPath))
            {
                image = Image.FromFile(message.ImgLocalPath);
            }
            else if (!String.IsNullOrEmpty(message.thumbImgLocalPath) && File.Exists(message.thumbImgLocalPath))
            {
                image = Image.FromFile(message.thumbImgLocalPath);
            }
            else
            {
                image = Image.FromFile(Path.Combine(GloableVars.filePath, "headimage", "default.jpg"));
            }
            /*image = Image.FromFile(
                    !String.IsNullOrEmpty(message.ImgLocalPath) ? message.ImgLocalPath: // 假设消息中有一个 ImgLocalPath 属性来指定图片路径
                    !String.IsNullOrEmpty(message.thumbImgLocalPath) ? message.thumbImgLocalPath: Path.Combine(GloableVars.filePath, "headimage", "default.jpg") // 假设消息中有一个 thumbImgLocalPath 属性来指定缩略图路径
                    );*/
            float rate = (float)image.Width / (float)image.Height;//限制图片宽度为300，高度按比例缩放
            PictureBox imageDisplay = new PictureBox
            {
                Image = image,// 假设消息中有一个 ImgLocalPath 属性来指定图片路径
                SizeMode = PictureBoxSizeMode.StretchImage,
                Width = image.Width > 300 ? 300 : image.Width, // 可根据需要设置
                Height = image.Height > 300 ? (int)(300 / rate) : image.Height, // 可根据需要设置高度
                Margin = new Padding(2) // 设置图片周围的边距
            };


            // 添加点击事件以显示原始图像
            imageDisplay.Click += (sender, e) =>
            {
                // 显示原始图像的窗口
                ImageViewForm imageViewForm = new ImageViewForm(image);
                imageViewForm.ShowDialog(); // 使用 ShowDialog 以模式方式显示
            };

            imageBubblePanel.Controls.Add(imageDisplay); // 添加图片到气泡面板

            // 调用 AddTimestampLabel 函数显示时间
            AddTimestampLabel(Convert.ToInt64(message.CreateTime));

            // 将气泡面板添加到主容器
            panel1.Controls.Add(imageBubblePanel);

            // 确保当前控制面板的内容可以滚动
            panel1.ScrollControlIntoView(imageBubblePanel); // 滚动到最新消息
        }

        private void AddVoiceBubble(GloableVars.TansMsg message)
        {
            // 创建气泡面板
            FlowLayoutPanel voiceBubblePanel = new FlowLayoutPanel
            {
                FlowDirection = message.IsSender == 1 ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                AutoSize = true,
                MaximumSize = new Size(panel1.Width - 20, 0),
                Padding = new Padding(5),
                Margin = new Padding(0, 5, 0, 0),
                WrapContents = false, // 关闭换行
                Dock = DockStyle.Top
            };

            // 添加头像
            PictureBox avatar = new PictureBox
            {
                Width = 40,
                Height = 40,
                Image = Image.FromFile(message.IsSender == 1 ?
                    Path.Combine(GloableVars.filePath, "headimage", GloableVars.selfwxid + ".jpg") :
                    GloableVars.userlist.Find(x => x.username == message.StrTalker).headimgurl),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Margin = new Padding(message.IsSender == 1 ? 0 : 5, 0, message.IsSender == 1 ? 5 : 0, 0) // 控制头像的边距
            };
            voiceBubblePanel.Controls.Add(avatar); // 添加头像到气泡

            //添加语音图标
            // 假设 voiceBubblePanel 是语音气泡的父容器
            string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", message.IsSender == 1 ? "voicelogo.png" : "voicelogoleft.png");
            if(message.VoiceLocalPath == null || !File.Exists(message.VoiceLocalPath))
            {
                imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "voicelogonodata.png");// 语音文件不存在时显示默认图标
            }
            Image voiceLogo = Image.FromFile(imagePath);
            PictureBox voiceIcon = new PictureBox
            {
                Image = voiceLogo,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Width = 16,
                Height = 40,
                // 获取气泡面板的高度
                Margin = new Padding(message.IsSender == 1 ? 0 : 5, 0, message.IsSender == 1 ? 5 : 0, 0) // 控制图标的边距
            };

            /*// 计算垂直居中的 Y 坐标--无效
            int bubbleHeight = 40;
            int iconVerticalCenter = (bubbleHeight - voiceIcon.Height) / 2;
            voiceIcon.Location = new Point(message.IsSender == 1 ? voiceLogo.Width : 0, iconVerticalCenter); // 根据发送者定位*/

            voiceBubblePanel.Controls.Add(voiceIcon); // 添加语音图标

            int voiceLength = 0;
            if (message.VoiceLocalPath != null && File.Exists(message.VoiceLocalPath))
            {
                //计算语音时长
                using (var audioFile = new AudioFileReader(message.VoiceLocalPath))
                {
                    voiceLength = (int)audioFile.TotalTime.TotalSeconds;
                }
            }
            // 创建语音显示面板
            Color backColor = message.IsSender == 1 ? Color.LightGreen : Color.White;
            if(message.VoiceLocalPath == null || !File.Exists(message.VoiceLocalPath))
            {
                backColor = Color.Gray; // 语音文件不存在时显示灰色背景
            }
            Color fillColor = message.IsSender == 1 ? Color.Orange : Color.Orange;
            VoicePlayRounderPanel voicePanel = new VoicePlayRounderPanel
            {
                BackColor = backColor,
                //AutoSize = true,
                Size = new Size(5 * voiceLength + 15 * 2, 35),
                Padding = new Padding(10),
                CornerRadius = 15, // 设置圆角半径
                BorderColor = backColor, // 设置边框颜色
                fillColor = fillColor, // 设置播放语音时的填充颜色
            };
            if (message.VoiceLocalPath != null && File.Exists(message.VoiceLocalPath))
            {
                voicePanel.Tag = new GloableVars.VoiceMessageData(message.VoiceLocalPath, voiceLength, message.IsSender == 1 ? true : false);
                voicePanel.Click += VoicePanel_Click;
            }
            //语音面板添加到气泡面板
            voiceBubblePanel.Controls.Add(voicePanel);

            /*// 创建播放按钮
            Button playButton = new Button
            {
                Text = message.VoiceLocalPath != null ? "播放" : "语音文件不存在", // 可以根据需要更改按钮文本
                AutoSize = true,
                Tag = message.VoiceLocalPath // 语音文件路径存储在 Tag 中
            };
            playButton.Click += PlayButton_Click; // 绑定点击事件处理器

            // 将播放按钮添加到气泡面板
            voiceBubblePanel.Controls.Add(playButton);*/


            // 调用 AddTimestampLabel 函数显示时间
            AddTimestampLabel(Convert.ToInt64(message.CreateTime));

            // 将气泡面板添加到主容器
            panel1.Controls.Add(voiceBubblePanel);

            // 确保当前控制面板的内容可以滚动
            panel1.ScrollControlIntoView(voiceBubblePanel); // 滚动到最新消息
        }

        private void AddVideoBubble(GloableVars.TansMsg message)
        {
            // 创建气泡面板
            FlowLayoutPanel videoBubblePanel = new FlowLayoutPanel
            {
                FlowDirection = message.IsSender == 1 ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                AutoSize = true,
                MaximumSize = new Size(panel1.Width - 20, 0),
                Padding = new Padding(5),
                Margin = new Padding(0, 5, 0, 0),
                WrapContents = false, // 关闭换行
                Dock = DockStyle.Top
            };

            // 添加头像
            PictureBox avatar = new PictureBox
            {
                Width = 40,
                Height = 40,
                Image = Image.FromFile(message.IsSender == 1 ?
                    Path.Combine(GloableVars.filePath, "headimage", GloableVars.selfwxid + ".jpg") :
                    GloableVars.userlist.Find(x => x.username == message.StrTalker).headimgurl),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Margin = new Padding(message.IsSender == 1 ? 0 : 5, 0, message.IsSender == 1 ? 5 : 0, 0) // 控制头像的边距
            };

            videoBubblePanel.Controls.Add(avatar); // 添加头像到气泡

            // 创建视频显示面板
            VideoPlayRounderPanel videoPanel = new VideoPlayRounderPanel
            {
                BackColor = message.IsSender == 1 ? Color.LightGreen : Color.White,
                AutoSize = true,
                Padding = new Padding(0),
                CornerRadius = 0, // 设置圆角半径
                BorderColor = message.IsSender == 1 ? Color.LightGreen : Color.White, // 设置边框颜色
                //fillColor = message.IsSender == 1 ? Color.DarkGray : Color.DarkGray, // 设置播放视频时的填充颜色
            };
            /*videoPanel.Tag = new GloableVars.VideoMessageData(message.VideoPreviewImgLocalPath, message.VideoLocalPath, message.IsSender == 1 ? true : false);
            videoPanel.Click += VideoPanel_Click;*/


            Image image = null;
            // 创建图片显示面板
            if (!String.IsNullOrEmpty(message.VideoPreviewImgLocalPath) && File.Exists(message.VideoPreviewImgLocalPath))
            {
                image = Image.FromFile(message.VideoPreviewImgLocalPath);
            }
            else
            {
                image = Image.FromFile(Path.Combine(GloableVars.filePath, "headimage", "default.jpg"));
            }
            /*image = Image.FromFile(
                    !String.IsNullOrEmpty(message.ImgLocalPath) ? message.ImgLocalPath: // 假设消息中有一个 ImgLocalPath 属性来指定图片路径
                    !String.IsNullOrEmpty(message.thumbImgLocalPath) ? message.thumbImgLocalPath: Path.Combine(GloableVars.filePath, "headimage", "default.jpg") // 假设消息中有一个 thumbImgLocalPath 属性来指定缩略图路径
                    );*/
            float rate = (float)image.Width / (float)image.Height;
            // 显示视频预览图片
            PictureBox videoPreviewPictureBox = new PictureBox
            {
                //Image = Image.FromFile(message.VideoPreviewImgLocalPath == null ? Path.Combine(GloableVars.filePath, "headimage", "default.jpg") : message.VideoPreviewImgLocalPath), // 加载视频预览图片
                Image = image, // 假设消息中有一个 ImgLocalPath 属性来指定图片路径
                Width = image.Width > 300 ? 300 : image.Width, // 可根据需要设置
                Height = image.Height > 300 ? (int)(300 / rate) : image.Height, // 可根据需要设置高度
                SizeMode = PictureBoxSizeMode.StretchImage,
                Margin = new Padding(2)
            };
            videoPreviewPictureBox.Tag = new GloableVars.VideoMessageData(message.VideoPreviewImgLocalPath, message.VideoLocalPath, message.IsSender == 1 ? true : false);
            videoPreviewPictureBox.Click += VideoPanel_Click;
            videoPanel.Controls.Add(videoPreviewPictureBox);


            // 在预览图片上绘制播放按钮
            float buttonSizeFactor = 0.1f; // 播放按钮相对于图像高度的比例（可以调整此值）
            int buttonSize = (int)(image.Height * buttonSizeFactor); // 根据图像高度计算按钮大小
            buttonSize = Math.Max(buttonSize, 30); // 确保按钮的最小大小为30
            Rectangle rect = new Rectangle(image.Width / 2 - buttonSize / 2, image.Height / 2 - buttonSize / 2, buttonSize, buttonSize);
            Graphics g = Graphics.FromImage(image);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            // 绘制圆形边框
            g.DrawEllipse(new Pen(Color.White, 2), rect);

            // 根据按钮大小设置字体大小
            float fontSizeFactor = 0.5f; // 字体大小相对于按钮大小的比例（可以调整此值）
            float fontSize = buttonSize * fontSizeFactor; // 计算字体大小
            Font font = new Font("微软雅黑", fontSize); // 创建字体对象
            string playSymbol = "▶";
            SizeF textSize = g.MeasureString(playSymbol, font);


            // 计算文本位置，使其居中
            float x = rect.X + (rect.Width - textSize.Width) / 2;
            float y = rect.Y + (rect.Height - textSize.Height) / 2;

            // 绘制播放图标
            g.DrawString(playSymbol, font, new SolidBrush(Color.Gainsboro), new PointF(x, y));

            g.Dispose();
            videoBubblePanel.Controls.Add(videoPanel);

            // 调用 AddTimestampLabel 函数显示时间
            AddTimestampLabel(Convert.ToInt64(message.CreateTime));

            // 将气泡面板添加到主容器
            panel1.Controls.Add(videoBubblePanel);

            // 确保当前控制面板的内容可以滚动
            panel1.ScrollControlIntoView(videoBubblePanel); // 滚动到最新消息
        }

        private void AddGifBubble(GloableVars.TansMsg message)
        {
            // 创建气泡面板
            FlowLayoutPanel gifBubblePanel = new FlowLayoutPanel
            {
                FlowDirection = message.IsSender == 1 ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                AutoSize = true,
                MaximumSize = new Size(panel1.Width - 20, 0),
                Padding = new Padding(5),
                Margin = new Padding(0, 5, 0, 0),
                WrapContents = false, // 关闭换行
                Dock = DockStyle.Top
            };

            // 添加头像
            PictureBox avatar = new PictureBox
            {
                Width = 40,
                Height = 40,
                Image = Image.FromFile(message.IsSender == 1 ?
                    Path.Combine(GloableVars.filePath, "headimage", GloableVars.selfwxid + ".jpg") :
                    GloableVars.userlist.Find(x => x.username == message.StrTalker).headimgurl),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Margin = new Padding(message.IsSender == 1 ? 0 : 5, 0, message.IsSender == 1 ? 5 : 0, 0) // 控制头像的边距
            };

            gifBubblePanel.Controls.Add(avatar); // 添加头像到气泡

            // 创建 GIF 动画显示面板
            Image gifImage = null;
            if (!String.IsNullOrEmpty(message.EmotionLocalPath) && File.Exists(message.EmotionLocalPath))
            {
                gifImage = Image.FromFile(message.EmotionLocalPath); // 加载 GIF 动画
            }
            else
            {
                gifImage = Image.FromFile(Path.Combine(GloableVars.filePath, "headimage", "default.jpg")); // 默认 GIF
            }

            // 创建 PictureBox 用于显示 GIF 动画
            PictureBox gifDisplay = new PictureBox
            {
                Image = gifImage,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Width = gifImage.Width > 150 ? 150 : gifImage.Width,
                Height = gifImage.Height > 150 ? (int)(150 * ((float)gifImage.Height / gifImage.Width)) : gifImage.Height,
                Margin = new Padding(2) // 设置 GIF 周围的边距
            };

            gifBubblePanel.Controls.Add(gifDisplay); // 添加 GIF 动画到气泡面板

            // 调用 AddTimestampLabel 函数显示时间
            AddTimestampLabel(Convert.ToInt64(message.CreateTime));

            // 将气泡面板添加到主容器
            panel1.Controls.Add(gifBubblePanel);

            // 确保当前控制面板的内容可以滚动
            panel1.ScrollControlIntoView(gifBubblePanel); // 滚动到最新消息
        }

        /*private void VideoPanel_Click(object sender, EventArgs e)
        {
            // 获取点击的面板
            PictureBox clickedPanel = sender as PictureBox;

            // 从 Tag 属性中获取视频数据
            VideoMessageData data = clickedPanel.Tag as VideoMessageData;

            VideoPlayerForm vidoplayerForm = null; 

            // 检查路径是否有效并播放视频
            if (data != null && File.Exists(data.videoLocalPath))
            {
                //调用本地vlc播放器进行播放

                *//*string pluginPath = Environment.CurrentDirectory + "\\vlclib\\plugins\\";  //插件目录
                VlcPlayerBase player = new VlcPlayerBase(pluginPath);
                player.SetRenderWindow((int)vidoplayerForm.Handle);//panel
                player.LoadFile(data.videoLocalPath);//视频文件路径
                vidoplayerForm = new VideoPlayerForm(data.videoLocalPath);
                vidoplayerForm.Show(); // 使用 ShowDialog 以模式方式显示
                player.Play(); // 播放视频*//*
            }else
            {
                MessageBox.Show("视频文件不存在！");
            }
        }*/
        private void AddVoicePhoneBubble(TansMsg message)
        {
            // 创建气泡面板
            FlowLayoutPanel voiceBubblePanel = new FlowLayoutPanel
            {
                FlowDirection = message.IsSender == 1 ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                AutoSize = true,
                MaximumSize = new Size(panel1.Width - 20, 0),
                Padding = new Padding(5),
                Margin = new Padding(0, 5, 0, 0),
                WrapContents = false, // 关闭换行
                Dock = DockStyle.Top
            };

            // 添加头像
            PictureBox avatar = new PictureBox
            {
                Width = 40,
                Height = 40,
                Image = Image.FromFile(message.IsSender == 1 ?
                    Path.Combine(GloableVars.filePath, "headimage", GloableVars.selfwxid + ".jpg") :
                    GloableVars.userlist.Find(x => x.username == message.StrTalker).headimgurl),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Margin = new Padding(message.IsSender == 1 ? 0 : 5, 0, message.IsSender == 1 ? 5 : 0, 0) // 控制头像的边距
            };
            voiceBubblePanel.Controls.Add(avatar); // 添加头像到气泡

            
            //添加图标圆角面板
            RounderCornerBubble roundCornerBubble = new RounderCornerBubble
            {
                //重新设置内部参考点
                BackColor = message.IsSender == 1 ? Color.LightGreen : Color.White,
                AutoSize = true,
                //MaximumSize = new Size(300, 0),
                Padding = new Padding(0),
                CornerRadius = 15, // 设置圆角半径
                BorderColor = message.IsSender == 1 ? Color.LightGreen : Color.White, // 设置边框颜色
            };

            FlowLayoutPanel iconPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                MaximumSize = new Size(150,0),
                Padding = new Padding(0),
                FlowDirection = message.IsSender == 1 ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, // // 左对齐
            };
            

            //添加图标
            Image image = null;
            if(message.IsVoipOrVideoip != 0)
            {
                image = Image.FromFile(AppDomain.CurrentDomain.BaseDirectory + "\\Resources\\audioCall.png");
            }
            else
            {
                image = Image.FromFile(AppDomain.CurrentDomain.BaseDirectory + "\\Resources\\videoCall.png");
            }

            if (message.IsSender == 1)
            {
                image.RotateFlip(RotateFlipType.Rotate180FlipY); //水品翻转图片
            }

            PictureBox icon = new PictureBox
            {
                Image = image,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Width = 30,
                Height = 30,
                //Margin = message.IsSender != 1 ? new Padding(10, 15, 5, 0) : new Padding(5, 15, 10, 0), // 设置图标的边距
            };

            iconPanel.Controls.Add(icon); // 添加图标到圆角面板

            //添加文字
            Label textLabel = new Label
            {
                Text = message.DisplayContent,
                AutoSize = true,
                MaximumSize = new Size(120, 0),
                Font = new Font("Segoe UI Emoji", 8),
                Padding = new Padding(0, 10, 0, 0), // 设置文字的边距
                //Padding = message.IsSender != 1 ? new Padding(35, 8, 0, 0): new Padding(0,8,35,0), // 设置文字的边距
            };
            iconPanel.Controls.Add(textLabel); // 添加文字到圆角面板
            roundCornerBubble.Controls.Add(iconPanel); // 添加圆角面板到气泡面板
            voiceBubblePanel.Controls.Add(roundCornerBubble); // 添加圆角面板到气泡面板

            // 调用 AddTimestampLabel 函数显示时间
            AddTimestampLabel(Convert.ToInt64(message.CreateTime));

            // 将气泡面板添加到主容器
            panel1.Controls.Add(voiceBubblePanel);

            // 确保当前控制面板的内容可以滚动
            panel1.ScrollControlIntoView(voiceBubblePanel); // 滚动到最新消息
        }

        private void AddSystemMsgBubble(TansMsg message)
        {
            // 使用TextRenderer来获取消息文本的实际宽度
            int messageWidth = TextRenderer.MeasureText(message.StrContent, new Font("Segoe UI Emoji", 8)).Width;

            // 根据消息宽度动态设置Padding
            int leftPadding = (this.Width - 30 - messageWidth) / 2; // 居中对齐的左侧填充
            int rightPadding = leftPadding; // 右侧填充与左侧相同
            FlowLayoutPanel systemmsgBubblePanel = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Top, // 确保它在最上面
                Margin = new Padding(0, 30, 0, 30), // 设置上下间距
                MaximumSize = new Size(panel1.Width - 20, 0),
                //根据消息长度自动设置左右间距
                //Padding = message.StrContent.Length > 100 ? new Padding(100, 10, 0, 10) : new Padding(170, 10, 0, 10),
                // 根据消息长度自动设置左右Padding以中心对齐
                //Padding = new Padding(100, 10, 0, 10),
                Padding = new Padding(leftPadding, 10, rightPadding, 10), // 使用动态计算的填充
                FlowDirection = FlowDirection.LeftToRight // 左对齐
            };

            Label systemMsgLabel = new Label
            {
                Text = message.StrContent,
                Width = messageWidth,
                Font = new Font("Segoe UI Emoji", 8),
                ForeColor = Color.White,
                BackColor = Color.LightGray,
                TextAlign = ContentAlignment.MiddleCenter,
            };
            systemmsgBubblePanel.Controls.Add(systemMsgLabel); // 添加系统消息到气泡面板

            // 调用 AddTimestampLabel 函数显示时间
            AddTimestampLabel(Convert.ToInt64(message.CreateTime));

            // 将气泡面板添加到主容器
            panel1.Controls.Add(systemmsgBubblePanel);

            // 确保当前控制面板的内容可以滚动
            panel1.ScrollControlIntoView(systemmsgBubblePanel); // 滚动到最新消息
        }

        private void VideoPanel_Click(object sender, EventArgs e)
        {
            // 获取点击的面板
            PictureBox clickedPanel = sender as PictureBox;

            // 从 Tag 属性中获取视频数据
            VideoMessageData data = clickedPanel.Tag as VideoMessageData;

            // 检查路径是否有效并播放视频
            if (data != null && File.Exists(data.videoLocalPath))
            {
                // 创建 VideoPlayerForm 实例
                VideoPlayerForm vidoplayerForm = new VideoPlayerForm(data.videoLocalPath);
                vidoplayerForm.ShowDialog(); // 显示视频播放器窗口

                /*// 获取插件路径
                string pluginPath = Environment.CurrentDirectory + "\\vlclib\\plugins\\";

                // 创建 VLC 播放器实例
                VlcPlayerBase player = new VlcPlayerBase(pluginPath);
                player.SetRenderWindow((int)vidoplayerForm.Handle); // 设置渲染窗口
                player.LoadFile(data.videoLocalPath); // 加载视频文件
                player.Play(); // 播放视频*/
            }
            else
            {
                MessageBox.Show("视频文件不存在或无法访问！");
            }
        }


        private void VoicePanel_Click(object sender, EventArgs e)
        {
            // 获取点击的面板
            VoicePlayRounderPanel clickedPanel = sender as VoicePlayRounderPanel;

            // 从 Tag 属性中获取声音数据
            VoiceMessageData data = clickedPanel.Tag as VoiceMessageData;

            // 启动动画
            //StartVoiceAnimation(clickedPanel);

            // 检查路径是否有效并播放音频
            if (data != null)
            {
                PlayAudioAsync(data.VoicePath, clickedPanel, data.IsSend);
                //窗口关闭按钮不可用
                //this.CloseButton.Enabled = false;
            }
        }

        private bool isPlaying = false; // 播放状态标志
        private async Task PlayAudioAsync(string audioPath, VoicePlayRounderPanel voicePanel, bool isSender)
        {
            if (!string.IsNullOrEmpty(audioPath) && File.Exists(audioPath))
            {
                isPlaying = true; // 设置播放状态为正在播放
                await Task.Run(() =>
                {
                    using (var audioFile = new AudioFileReader(audioPath))
                    using (var outputDevice = new WaveOutEvent())
                    {
                        outputDevice.Init(audioFile);
                        outputDevice.Play();

                        // 获取音频总时长
                        var totalDuration = audioFile.TotalTime.TotalMilliseconds;

                        // 循环直至播放结束或窗口关闭
                        while (outputDevice.PlaybackState == PlaybackState.Playing && isPlaying)
                        {
                            // 更新动画进度
                            var currentPosition = audioFile.CurrentTime.TotalMilliseconds;
                            var progress = currentPosition / totalDuration; // 计算进度（0到1之间）

                            // 发送进度更新到UI线程，以更新动画
                            voicePanel.Invoke(new Action(() =>
                            {
                                // 根据发送者确定动画填充的方向
                                voicePanel.UpdateAnimationProgress((float)progress, isSender);
                            }));

                            // 适当睡眠，避免阻塞线程
                            Thread.Sleep(100);
                        }

                        // 停止音频播放
                        if (outputDevice.PlaybackState == PlaybackState.Playing)
                        {
                            outputDevice.Stop();
                        }
                    }
                });
            }
            else
            {
                MessageBox.Show("语音文件不存在！");
            }
        }

        private void StartVoiceAnimation(VoicePlayRounderPanel voicePanel)
        {
            /*// 初始化 Timer
            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 100; // 每100毫秒更新时间
            animationTimer.Tick += (s, e) =>
            {
                animationProgress += 0.05f; // 进度每次增加5%
                if (animationProgress > 1f) animationProgress = 1f; // 限制在1之内
                voicePanel.Invalidate(); // 触发重绘

                if (animationProgress == 1f)
                {
                    animationTimer.Stop(); // 停止动画
                }
            };
            animationTimer.Start();*/
            voicePanel.StartAnimation();
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;
            string voicePath = button.Tag.ToString(); // 获取语音路径

            if (!string.IsNullOrEmpty(voicePath) && File.Exists(voicePath))
            {
                using (var audioFile = new AudioFileReader(voicePath))
                using (var outputDevice = new WaveOutEvent())
                {
                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                    while (outputDevice.PlaybackState == PlaybackState.Playing)
                    {
                        Thread.Sleep(100); // 避免阻塞UI线程
                    }
                }
            }
            else
            {
                button.Text = "语音文件未找到"; // 显示失败信息
                button.Enabled = false; // 禁用按钮
            }
        }

        public static DateTime ConvertUnixTimestampToDateTime(long timestamp)
        {
            // Unix时间戳是从1970年1月1日00:00:00开始的秒数或毫秒数
            // 此处以秒为单位，如果是毫秒则除以1000
            System.DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(timestamp).ToLocalTime(); // 转换为本地时间
            return dateTime;
        }
        //设置时间显示
        private void AddTimestampLabel(long messageTimestamp)
        {
            //DateTime messageTime = DateTimeOffset.FromUnixTimeSeconds(messageTimestamp).DateTime;//此法不准确

            DateTime messageTime = ConvertUnixTimestampToDateTime(messageTimestamp);//此法准确
            // 判断时间差异
            if ((lastMessageTime != DateTime.MinValue) && (lastMessageTime - messageTime).TotalMinutes > 15)
            {
                // 创建一个单独的面板用于显示时间标签
                //新建一个flowLayoutPanel
                FlowLayoutPanel timestampPanel = new FlowLayoutPanel
                {
                    AutoSize = true,
                    Dock = DockStyle.Top, // 确保它在最上面
                    Margin = new Padding(0, 30, 0, 30), // 设置上下间距
                    //设置左右间距
                    Padding = new Padding(170, 10, 0, 10),
                    FlowDirection = FlowDirection.LeftToRight // 左对齐
                };

                // 创建时间标签
                Label timeLabel = new Label
                {
                    Text = messageTime.ToString("yyyy-MM-dd HH:mm:ss"), // 格式化时间显示
                    Size = new Size(150, 10), // 设置大小
                    Font = new Font("黑体", 8),
                    //Dock = DockStyle.Bottom, // 设置为顶部停靠
                    //设置居中显示
                    TextAlign = ContentAlignment.MiddleCenter
                };

                timestampPanel.Controls.Add(timeLabel); // 将时间标签添加到时间面板
                panel1.Controls.Add(timestampPanel); // 将时间面板添加到气泡面板
            }

            lastMessageTime = messageTime; // 更新最后消息时间戳
        }
        private string headstring;
        private void ShowMessage()
        {
            if (GloableVars.transmsglist.Count > 0)
            {
                if (currentPage * PageSize < GloableVars.transmsglist.Count)
                {
                    var messagesToLoad = GloableVars.transmsglist.Skip(currentPage * PageSize).Take(PageSize).ToList();
                    Text =headstring+ $",当前查看: {currentPage * PageSize + 1}-{Math.Min((currentPage + 1) * PageSize, GloableVars.transmsglist.Count)} / 总条数: {GloableVars.transmsglist.Count}";

                    foreach (var truemsg in messagesToLoad)
                    {
                        Label messageLabel = new Label();
                        messageLabel.Width = panel1.Width - 20; // 减去边距
                        messageLabel.AutoSize = true;
                        switch (truemsg.Type)
                        {
                            case 1://文字消息
                                /*if (truemsg.IsSender ==1)//使用esframework失败，暂时不用
                                {
                                    this.chatRender.AddChatItemText(truemsg.Sequence.ToString(), GloableVars.selfwxid, truemsg.StrContent);
                                    //时间戳转时间
                                    this.chatRender.AddChatItemTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Convert.ToDouble(truemsg.CreateTime)));
                                }
                                else
                                {
                                    this.chatRender.AddChatItemText(truemsg.Sequence.ToString(), truemsg.StrTalker, truemsg.StrContent);
                                    this.chatRender.AddChatItemTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Convert.ToDouble(truemsg.CreateTime)));
                                }*/
                                AddChatBubble(truemsg);
                                break;
                            case 3://图片消息
                                AddImageBubble(truemsg);
                                break;
                            case 34://语音消息
                                AddVoiceBubble(truemsg);
                                break;
                            case 43://视频消息
                                AddVideoBubble(truemsg);
                                break;
                            case 47://动画表情
                                AddGifBubble(truemsg);
                                break;
                            case 49://名片消息
                                break;
                            case 50://语音电话
                                AddVoicePhoneBubble(truemsg);
                                break;
                            case 10000://系统消息
                                AddSystemMsgBubble(truemsg);
                                break;
                            defalut:
                                break;
                        }
                    }
                    currentPage++;
                    // 回收旧消息
                    if (panel1.Controls.Count > MaxMessages)
                    {
                        int excessMessages = panel1.Controls.Count - MaxMessages;

                        for (int i = 0; i < excessMessages; i++)
                        {
                            panel1.Controls.RemoveAt(0); // 移除最旧的消息 (顶部的消息)
                        }
                    }
                }
            }
        }

        

        private void panel1_Scroll(object sender, ScrollEventArgs e)
        {
            // 当滚动到顶部时，加载更多消息
            if (e.NewValue == 0)
            {
                ShowMessage();
            }
        }

        private void ChatHistory_FormClosing(object sender, FormClosingEventArgs e)
        {
            isPlaying = false; // 设置播放状态为停止
            GloableVars.isForm5Show = false; // 设置窗体状态为关闭
        }
    }
}

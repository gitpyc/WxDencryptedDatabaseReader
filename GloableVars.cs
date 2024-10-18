using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace wxreader
{
    public static class GloableVars
    {
        public static string filePath { get; set; }
        public static string fileName { get; set; }
        public static string fileExt { get; set; }

        public static string connecsString { get; set; }

        public static List<string> imagesDir { get; set; }

        public static List<string> processedDir { get; set; }

        public class fileinfo
        {
            public string filename { get; set; }
            public string filepath { get; set; }
        }

        public class voiceinfo
        {
            public string audoid { get; set; }
            public byte[] voice { get; set; }
        }

        public static List<voiceinfo> voiceList { get; set; }

        public class wxtalker
        {
            public string username { get; set; }
            public string nickname { get; set; }
            public string remark { get; set; }
        }
        public static List<wxtalker> talkerlist { get; set; }

        public class emotioninfo
        {
            public string emotionname { get; set; }
            public string emotion_aeskey { get; set; }

            public string emotion_aesiv { get; set; }

            public string emotion_url { get; set; }
        }

        public class HeadImageInfo
        {
            public string UserName { get; set; }
            public string smallImageUrl { get; set; }
            public string bigImageUrl { get; set; }
            public string HeadImageMd5 { get; set; }
        }

        public class wxuser
        {
            public string username { get; set; }
            public string nickname { get; set; }
            public string remark { get; set; }
            public string headimgurl { get; set; }
            public string smallheadimgurl { get; set; }
            public string bigheadimgurl { get; set; }
            public string headimgmd5 { get; set; }

            public Int64 msgcount { get; set; }
        }

        public static List<wxuser> userlist { get; set; }

        public class truemsg
        {
            public string localId { get; set; }
            public string TalkerId { get; set; }
            public string MsgSvrId { get; set; }
            public int Type { get; set; }
            public int SubType { get; set; }
            public int IsSender { get; set; }
            public string CreateTime { get; set; }
            public string Sequence { get; set; }
            public string StatusEx { get; set; }
            public string MsgServerSeq { get; set; }
            public string MsgSequence { get; set; }
            public string StrTalker { get; set; }
            public string StrContent { get; set; }
            public string DisplayContent { get; set; }
            public byte[] CompressContent { get; set; }
            public byte[] BytesExtra { get; set; }
            public byte[] Buf { get; set; }
            public string Remark { get; set; }
            public string NickName { get; set; }
        }

        public class TansMsg
        {
            public Int64 localId { get; set; }
            public Int64 TalkerId { get; set; }
            public string MsgSvrId { get; set; }
            public int Type { get; set; }
            public int SubType { get; set; }
            public int IsSender { get; set; }
            public string CreateTime { get; set; }
            public string Sequence { get; set; }
            public Int64 StatusEx { get; set; }
            public string MsgServerSeq { get; set; }
            public string MsgSequence { get; set; }
            public string StrTalker { get; set; }
            public string StrContent { get; set; }
            public string DisplayContent { get; set; }
            public string Remark { get; set; }
            public string NickName { get; set; }
            public string ImgLocalPath { get; set; }
            public string thumbImgLocalPath { get; set; }
            public string EmotionLocalPath { get; set; }
            public string VoiceLocalPath { get; set; }

            public string VideoLocalPath { get; set; }
            public string VideoPreviewImgLocalPath { get; set; }

            public int IsVoipOrVideoip { get; set; }
        }

        public static List<truemsg> truemsglist { get; set; }

        public static List<TansMsg> transmsglist { get; set; }

        public static string selfwxid { get; set; }

        public class VoiceMessageData
        {
            public string VoicePath { get; set; }
            public int VoiceLength { get; set; }

            public bool IsSend { get; set; }

            public VoiceMessageData(string voicePath, int voiceLength, bool isSend)
            {
                VoicePath = voicePath;
                VoiceLength = voiceLength;
                IsSend = isSend;
            }
        }
        public static bool IsDirExist(string dir)
        {
            return System.IO.Directory.Exists(dir);
        }
        public static bool IsFileExist(string file)
        {
            return System.IO.File.Exists(file);
        }

        public static int DecodeVoice(SQLiteConnection connection, string strTalker)
        {
            Form1.DecodeBufAsync(connection, strTalker);
            return voiceList.Count;
        }

        public class VideoMessageData
        {
            public string videoPreviewImgLocalPath;
            public string videoLocalPath;
            public bool v;

            public VideoMessageData(string videoPreviewImgLocalPath, string videoLocalPath, bool v)
            {
                this.videoPreviewImgLocalPath = videoPreviewImgLocalPath;
                this.videoLocalPath = videoLocalPath;
                this.v = v;
            }
        }

        /// <summary>
        /// 执行一条command命令
        /// </summary>
        /// <param name="command">需要执行的Command</param>
        /// <param name="output">输出</param>
        /// <param name="error">错误</param>
        public static void ExecuteCommand(string command, out string output, out string error)
        {
            try
            {
                //创建一个进程
                Process pc = new Process();
                pc.StartInfo.FileName = command;
                pc.StartInfo.UseShellExecute = false;
                pc.StartInfo.RedirectStandardOutput = true;
                pc.StartInfo.RedirectStandardError = true;
                pc.StartInfo.CreateNoWindow = true;

                //启动进程
                pc.Start();

                //准备读出输出流和错误流
                string outputData = string.Empty;
                string errorData = string.Empty;
                pc.BeginOutputReadLine();
                pc.BeginErrorReadLine();

                pc.OutputDataReceived += (ss, ee) =>
                {
                    outputData += ee.Data;
                };

                pc.ErrorDataReceived += (ss, ee) =>
                {
                    errorData += ee.Data;
                };

                //等待退出
                pc.WaitForExit();

                //关闭进程
                pc.Close();

                //返回流结果
                output = outputData;
                error = errorData;
            }
            catch (Exception)
            {
                output = null;
                error = null;
            }
        }

        /// <summary>
        /// 获取视频的帧宽度和帧高度
        /// </summary>
        /// <param name="videoFilePath">mov文件的路径</param>
        /// <returns>null表示获取宽度或高度失败</returns>
        public static void GetMovWidthAndHeight(string videoFilePath, ref int width, ref int height)
        {
            try
            {
                /*//判断文件是否存在
                if (!File.Exists(videoFilePath))
                {
                    width = null;
                    height = null;
                }*/

                //执行命令获取该文件的一些信息 
                // 获取应用程序的基目录
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                // 构建ffmpeg.exe的完整路径
                string ffmpegPath = Path.Combine(baseDirectory, "silk-v3-decoder-master", "windows", "ffmpeg.exe");

                string output;
                string error;
                ExecuteCommand("\"" + ffmpegPath + "\"" + " -i " + "\"" + videoFilePath + "\"", out output, out error);
                /*if (string.IsNullOrEmpty(error))
                {
                    width = null;
                    height = null;
                }*/

                //通过正则表达式获取信息里面的宽度信息
                Regex regex = new Regex("(\\d{2,4})x(\\d{2,4})", RegexOptions.Compiled);
                Match m = regex.Match(error);
                if (m.Success)
                {
                    width = int.Parse(m.Groups[1].Value);
                    height = int.Parse(m.Groups[2].Value);
                }
                else
                {
                    width = 0;
                    height = 0;
                }
            }
            catch (Exception)
            {
                width = 0;
                height = 0;
            }
        }

        public static bool isForm5Show = false;

        public static ObservableString MonitoredVariable { get; set; } = new ObservableString();
        public static ObservableString ProcessingVariable { get; set; } = new ObservableString();
        public static int DatImageCount { get; set; } = 0;
    }
}

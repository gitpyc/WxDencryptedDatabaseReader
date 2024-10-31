using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace wxreader
{
    public class silkdecoder
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        private static readonly log4net.ILog loginfo = log4net.LogManager.GetLogger("loginfo");
        private static readonly log4net.ILog logerror = log4net.LogManager.GetLogger("logerror");
        private readonly SQLiteConnection _connection;
        private readonly SemaphoreSlim _semaphore;

        public silkdecoder(SQLiteConnection connection, int maxConcurrency)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _semaphore = new SemaphoreSlim(maxConcurrency);
        }

        public async Task DecodeAudioAsync(string audioId, List<GloableVars.voiceinfo> voiceList, string tempSilkFilePath, string outputMp3FilePath)
        {
            log4net.GlobalContext.Properties["LogFilePath"] = outputMp3FilePath;
            XmlConfigurator.Configure();
            await _semaphore.WaitAsync();// 控制并发数
            try
            {
                // 从数据库中读取Silk数据
                //byte[] audioData = await Task.Run(() => ReadAudioDataFromDatabase(audioId));

                //查找列表
                byte[] audioData = voiceList.Find(x => x.audoid == audioId).voice;

                if (!File.Exists(tempSilkFilePath)) // 文件存在则不进行操作
                {
                    // 保存Silk数据为临时文件
                    using (var fileStream = new FileStream(tempSilkFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                    {
                        try
                        {
                            await fileStream.WriteAsync(audioData, 0, audioData.Length);
                            await fileStream.FlushAsync(); // 确保所有数据都写入文件
                        }
                        catch (Exception ee)
                        {
                            //GloableVars.failedDecodeVoiceCount++;
                            //WriteLog(Path.Combine(Path.GetDirectoryName(outputMp3FilePath), "decodesilk.log"), $"{Path.GetFileName(outputMp3FilePath)}临时文件保存失败：{ee.Message}");
                            //logerror.Error($"临时文件保存失败：{ee.Message}");
                        }
                    }
                }

                // 使用silk-v3-decoder进行转换
                if (File.Exists(tempSilkFilePath) && !File.Exists(outputMp3FilePath))
                {
                    // 确保临时文件存储完成后再进行转换
                    try
                    {
                        await ConvertSilkToMp3Async(tempSilkFilePath, outputMp3FilePath);
                    }
                    catch (Exception ex)
                    {
                        GloableVars.failedDecodeVoiceCount++;
                        //WriteLog(Path.Combine(Path.GetDirectoryName(outputMp3FilePath), "decodesilk.log"), $"{Path.GetFileName(outputMp3FilePath)}mp3文件转换失败：{ex.Message}");
                        //logerror.Error($"mp3文件转换失败：{ex.Message}");
                    }
                }
                else if(File.Exists(outputMp3FilePath))
                {
                    Console.WriteLine($"MP3文件已存在，无需转换");
                    GloableVars.successDecodeVoiceCount++;
                    //WriteLog(Path.Combine(Path.GetDirectoryName(outputMp3FilePath), "decodesilk.log"), $"{Path.GetFileName(outputMp3FilePath)}文件已存在，无需转换");
                }
                // 删除临时文件
                //File.Delete(tempSilkFilePath);

                //WriteLog(Path.Combine(Path.GetDirectoryName(outputMp3FilePath), "decodesilk.log"), $"{Path.GetFileName(outputMp3FilePath)}文件转换成功");
            }
            catch (Exception ex)
            {
                //调试控制台输出
                Console.WriteLine($"转换失败：{ex.Message}");
                //WriteLog(Path.Combine(Path.GetDirectoryName(outputMp3FilePath), "decodesilk.log"), $"{Path.GetFileName(outputMp3FilePath)}文件转换失败：{ex.Message}");
                //logerror.Error($"文件转换失败：{ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        //写入日志文件
        
        public static void WriteLog(string logfilepath, string log)
        {
            using (FileStream fs = new FileStream(logfilepath, FileMode.Append, FileAccess.Write, FileShare.None))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff")}] {log}");//毫秒级别
                sw.Close();
            }
        }


        private byte[] ReadAudioDataFromDatabase(string msgSvrId)
        {
            using (var command = new SQLiteCommand("SELECT Buf FROM TRUEMSG WHERE MsgSvrID = @id", _connection))
            {
                //检测数据库连接是否正常
                if (_connection.State != System.Data.ConnectionState.Open)
                {
                    _connection.Open();
                }
                command.Parameters.AddWithValue("@id", msgSvrId);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return (byte[])reader["Buf"];
                    }
                }
            }
            throw new Exception("未找到指定ID的silk数据");
        }

        private static async Task ConvertSilkToMp3Async(string inputFilePath, string outputFilePath)
        {
            // 获取应用程序的基目录
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // 构建silk_v3_decoder.exe和ffmpeg.exe的完整路径
            string silkDecoderPath = Path.Combine(baseDirectory, "silk-v3-decoder-master","windows", "silk_v3_decoder.exe");
            string ffmpegPath = Path.Combine(baseDirectory, "silk-v3-decoder-master", "windows", "ffmpeg.exe");

            // 检查silk_v3_decoder.exe和ffmpeg.exe是否存在
            if (!File.Exists(silkDecoderPath))
            {
                //WriteLog(Path.Combine(Path.GetDirectoryName(outputFilePath), "decodesilk.log"), $"未找到ffmpeg.exe");
                throw new FileNotFoundException("未找到silk_v3_decoder.exe,请查看说明");
            }
            if (!File.Exists(ffmpegPath))
            {
                //WriteLog(Path.Combine(Path.GetDirectoryName(outputFilePath), "decodesilk.log"), $"未找到ffmpeg.exe");
                throw new FileNotFoundException("未找到ffmpeg.exe,请查看说明");
            }

            // 临时输出文件路径
            string tempPcmFilePath = Path.ChangeExtension(outputFilePath, ".pcm");
            string tempWavFilePath = Path.ChangeExtension(outputFilePath, ".mp3");

            //检查输入和输出文件是否存在
            //if (!File.Exists(tempPcmFilePath))
            //{
            //    WriteLog(Path.Combine(Path.GetDirectoryName(outputFilePath), "decodesilk.log"), $"未找到输入文件：{inputFilePath}");
            //    throw new FileNotFoundException("未找到输入文件");
            //}
            if (File.Exists(tempWavFilePath))
            {
                //WriteLog(Path.Combine(Path.GetDirectoryName(outputFilePath), "decodesilk.log"), $"输出文件已存在：{outputFilePath}");
                return;
            }

            // 构建silk_v3_decoder.exe命令
            string silkCommand = $"{silkDecoderPath} {inputFilePath} {tempPcmFilePath} -quiet";

            // 构建ffmpeg.exe命令
            string ffmpegCommand = $"{ffmpegPath} -y -f s16le -ar 24000 -ac 1 -i {tempPcmFilePath} {outputFilePath}";

            // 执行silk_v3_decoder.exe命令
            if (!File.Exists(tempPcmFilePath))
            {
                int silkExitCode =  ExecuteCommandAsync(silkCommand);
                if (silkExitCode != 0)
                {
                    //WriteLog(Path.Combine(Path.GetDirectoryName(outputFilePath), "decodesilk.log"), $"silk_v3_decoder.exe 执行失败，退出代码：{silkExitCode}");
                    GloableVars.failedDecodeVoiceCount++;
                    return;
                } 
            }

            // 执行ffmpeg.exe命令
            if (!File.Exists(tempWavFilePath) && File.Exists(tempPcmFilePath))
            {
                int ffmpegExitCode = ExecuteCommandAsync(ffmpegCommand);
                if (ffmpegExitCode != 0)
                {
                    GloableVars.failedDecodeVoiceCount++;
                    //WriteLog(Path.Combine(Path.GetDirectoryName(outputFilePath), "decodesilk.log"), $"ffmpeg.exe 执行失败，退出代码：{ffmpegExitCode}");
                    return;
                } 
            }

            // 删除临时pcm文件
            try
            {
                if (File.Exists(tempPcmFilePath))
                {
                    File.Delete(tempPcmFilePath);
                    //WriteLog(Path.Combine(Path.GetDirectoryName(outputFilePath), "decodesilk.log"), $"临时pcm文件已成功删除：{tempPcmFilePath}");
                }
                else
                {
                    //WriteLog(Path.Combine(Path.GetDirectoryName(outputFilePath), "decodesilk.log"), $"临时pcm文件不存在，无需删除：{tempPcmFilePath}");
                }
            }
            catch (Exception ex)
            {
                //WriteLog(Path.Combine(Path.GetDirectoryName(outputFilePath), "decodesilk.log"), $"删除临时pcm文件失败：{tempPcmFilePath}，错误信息：{ex.Message}");
            }

        }

        /// <summary>
        /// 执行命令行命令
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private static int ExecuteCommandAsync(string command)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C {command}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                //process.Start();
                process.Start();
                process.WaitForExit(); // 替代 WaitForExitAsync
                //process.WaitForExitAsync();
                return process.ExitCode;
            }
        }



        private static string cmdconverter(string command,string inputFilePath, string outputFilePath)
        {
            // 使用Process启动cmd.exe
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                // 将命令写入cmd.exe
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(process.StandardInput.BaseStream))
                {
                    sw.WriteLine(command);
                    sw.WriteLine("exit");
                }

                // 获取cmd的输出
                string result = process.StandardOutput.ReadToEnd();

                process.WaitForExit();

                // 打印输出结果
                Console.WriteLine(result);
                return result;
            }
        }

        internal async Task DecodeEmotionAsync(GloableVars.emotioninfo emotioninfo, string tempSilkFilePath, string outputMp3FilePath)
        {
            throw new NotImplementedException();
        }
    }
}
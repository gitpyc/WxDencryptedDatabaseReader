using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wxreader
{
    internal class ImageDecoder
    {
        static int coder = 0xde;  // Need to change it by using the image file of top2 bytes HEX_code XOR 0xFF

        static void ImageDecode(string filePath)
        {
            if (!filePath.EndsWith(".dat"))
            {
                return;
            }

            using (FileStream datRead = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                string outFile = Path.ChangeExtension(filePath, ".jpg");
                using (FileStream pngWrite = new FileStream(outFile, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[datRead.Length];
                    datRead.Read(buffer, 0, buffer.Length);
                    byte[] outputBuffer = new byte[buffer.Length];

                    for (int i = 0; i < buffer.Length; i++)
                    {
                        outputBuffer[i] = (byte)(buffer[i] ^ coder);
                    }

                    pngWrite.Write(outputBuffer, 0, outputBuffer.Length);
                }
            }
        }

        static void FindFlist(string directory)
        {
            string[] fsinfo = Directory.GetFiles(directory);

            foreach (string file in fsinfo)
            {
                Console.WriteLine($"文件路径: {file}");
                Console.WriteLine(Path.GetFileName(file));
                ImageDecode(file);
            }

            string[] subdirectories = Directory.GetDirectories(directory);
            foreach (string subdirectory in subdirectories)
            {
                FindFlist(subdirectory);
            }
        }

        static void GetImage(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("运行时请设定转换照片文件夹路径 (注意本工具会把子文件夹内的微信图片一起转换)");
            }
            else
            {
                FindFlist(args[0]);  // Need to fill with folder path
            }
        }
    }
}

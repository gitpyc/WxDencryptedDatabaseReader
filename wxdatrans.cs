using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wxreader
{
    public class wxdatrans
    {
        static byte[] buf = new byte[32 * 1024 * 1024];
        static int flag;
        static ulong coder, coder_extend;

        public static int CheckCoder(byte a, byte b)
        {
            if ((a ^ 0xff) == (b ^ 0xd8))
            {
                return a ^ 0xff;
            }
            else if ((a ^ 0x89) == (b ^ 0x50))
            {
                return a ^ 0x89;
            }
            else if ((a ^ 0x42) == (b ^ 0x4d))
            {
                return a ^ 0x42;
            }
            else if ((a ^ 0x47) == (b ^ 0x49))
            {
                return a ^ 0x47;
            }
            else if ((a ^ 0x50) == (b ^ 0x4b))
            {
                return a ^ 0x50;
            }
            else if ((a ^ 0x52) == (b ^ 0x61))
            {
                return a ^ 0x52;
            }
            else if ((a ^ 0x41) == (b ^ 0x56))
            {
                return a ^ 0x41;
            }
            return 0;
        }

        public static void Process(string[] args)
        {
            for (int times = 0; times < args.Length - 1; times++)
            {
                using (FileStream p = new FileStream(args[times + 1], FileMode.Open, FileAccess.Read))
                {
                    int length = p.Read(buf, 0, buf.Length);
                    if (flag == 0)
                    {
                        coder = (ulong)CheckCoder(buf[0], buf[1]);
                        coder_extend =
                            (coder << 56) | (coder << 48) | (coder << 40) | (coder << 32) | (coder << 24) | (coder << 16) |
                            (coder << 8) | coder;
                        flag = 1;
                    }

                    for (int i = 0; i < length; i += 8)
                    {
                        ulong value = BitConverter.ToUInt64(buf, i);
                        value ^= coder_extend;
                        Buffer.BlockCopy(BitConverter.GetBytes(value), 0, buf, i, 8);
                    }

                    string outputFileName = Path.ChangeExtension(args[times + 1], ".jpg");
                    using (FileStream o = new FileStream(outputFileName, FileMode.Create, FileAccess.Write))
                    {
                        o.Write(buf, 0, length);
                    }
                }
            }
        }
    }


}

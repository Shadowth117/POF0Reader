using Reloaded.Memory.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace POF0Reader
{
    class Program
    {
        static void Main(string[] args)
        {
            List<uint> pof = new List<uint>();
            int mask = 0xC0;
            int sectionLength = 0;
            int pofId = 0;

            using (Stream stream = (Stream)new FileStream(args[0], FileMode.Open))
            using (var streamReader = new BufferedStreamReader(stream, 8192))
            {
                POF0SEARCH:
                while (true)
                {
                    byte test = streamReader.Read<byte>();
                    if (test == 0x50)
                    {
                        
                        if(streamReader.CanRead(3))
                        {
                            byte[] pofTest = streamReader.ReadBytes(streamReader.Position(), 3);
                            streamReader.Seek(streamReader.Position() + 3, SeekOrigin.Begin);
                            if(pofTest[0] == 0x4F && pofTest[1] == 0x46 && pofTest[2] == 0x30)
                            {
                                break;
                            }
                        }
                    }
                }


                int size = streamReader.Read<int>();
                sectionLength = size;

                while(size > 0 && streamReader.CanRead(1))
                {
                    uint pointer = streamReader.Read<byte>();

                    switch(pointer & mask)
                    {
                        case 0x40:
                            pointer -= 0x40;
                            pof.Add(4 * pointer);
                            break;
                        case 0x80:
                            pointer -= 0x80;
                            pointer *= 0x100;
                            pointer += streamReader.Read<byte>();
                            pof.Add(4 * pointer);
                            size -= 1;
                            break;
                        case 0xC0:
                            pointer -= 0xC0;
                            pointer *= 0x1000000;
                            pointer += streamReader.Read<byte>() * (uint)0x10000;
                            pointer += streamReader.Read<byte>() * (uint)0x100;
                            pointer += streamReader.Read<byte>();
                            pof.Add(4 * pointer);
                            size -= 3;
                            break;
                    }
                    size -= 1;
                }


                using (StreamWriter file = new StreamWriter(args[0] + "_pof0_" + pofId + "_real.txt"))
                {
                    uint prev = 0;
                    file.WriteLine("POF0 Data:");
                    file.WriteLine("Size:     " + sectionLength.ToString("X8") + " " + sectionLength);
                    file.WriteLine("**Actual**");
                    for (int i = 0; i < pof.Count; i++)
                    {
                        uint cur = pof[i];
                        cur += prev;
                        prev = cur;
                        file.WriteLine($"Offset {i.ToString("D4")}: {cur.ToString("X8")} {cur}");
                    }

                    file.WriteLine(" ");
                    file.WriteLine("**Relative**");
                    for (int i = 0; i < pof.Count; i++)
                    {
                        file.WriteLine($"Offset {i.ToString("D4")}: {pof[i].ToString("X8")} {pof[i]}");
                    }
                }

                if (streamReader.CanRead(1))
                {
                    pofId++;
                    pof = new List<uint>();
                    goto POF0SEARCH;
                }
            }

        }
    }
}

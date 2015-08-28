using System.Text;
using System.IO;

namespace DOA5Tool
{
    class UTF16
    {
        public static string GetString(byte[] Buff, EndianBinary.Endian Endian)
        {
            StringBuilder Output = new StringBuilder();

            for (int i = 0; i < Buff.Length; i += 2)
            {
                ushort Value = 0;

                if (Endian == EndianBinary.Endian.Little)
                    Value = (ushort)(Buff[i] | Buff[i + 1] << 8);
                else
                    Value = (ushort)(Buff[i + 1] | Buff[i] << 8);

                Output.Append(Encoding.Unicode.GetString(new byte[] { (byte)(Value & 0xff), (byte)(Value >> 8) }));
            }

            return Output.ToString();
        }

        public static byte[] GetBytes(string Text, EndianBinary.Endian Endian)
        {
            using (MemoryStream Output = new MemoryStream())
            { 
                for (int i = 0; i < Text.Length; i++)
                {
                    string Character = Text.Substring(i, 1);
                    byte[] Buff = Encoding.Unicode.GetBytes(Character);

                    if (Endian == EndianBinary.Endian.Little)
                    {
                        Output.WriteByte(Buff[0]);
                        Output.WriteByte(Buff[1]);
                    }
                    else
                    {
                        Output.WriteByte(Buff[1]);
                        Output.WriteByte(Buff[0]);
                    }
                }

                return Output.ToArray();
            }
        }
    }
}

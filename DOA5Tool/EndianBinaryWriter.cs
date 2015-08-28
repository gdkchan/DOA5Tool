using System;
using System.IO;

namespace DOA5Tool
{
    class EndianBinaryWriter
    {
        public Stream BaseStream { get; set; }
        private EndianBinary.Endian Endian;

        public EndianBinaryWriter(Stream Input, EndianBinary.Endian Endianess)
        {
            BaseStream = Input;
            Endian = Endianess;
        }

        public void Write(byte Value)
        {
            BaseStream.WriteByte(Value);
        }

        public void Write(ushort Value)
        {
            if (Endian == EndianBinary.Endian.Little)
            {
                BaseStream.WriteByte((byte)(Value & 0xff));
                BaseStream.WriteByte((byte)(Value >> 8));
            }
            else
            {
                BaseStream.WriteByte((byte)(Value >> 8));
                BaseStream.WriteByte((byte)(Value & 0xff));
            }
        }

        public void Write(short Value)
        {
            Write((ushort)Value);
        }

        public void Write(uint Value)
        {
            if (Endian == EndianBinary.Endian.Little)
                WriteLE(Value);
            else
            {
                BaseStream.WriteByte((byte)(Value >> 24));
                BaseStream.WriteByte((byte)(Value >> 16));
                BaseStream.WriteByte((byte)(Value >> 8));
                BaseStream.WriteByte((byte)(Value & 0xff));
            }
        }

        public void WriteLE(uint Value)
        {
            BaseStream.WriteByte((byte)(Value & 0xff));
            BaseStream.WriteByte((byte)(Value >> 8));
            BaseStream.WriteByte((byte)(Value >> 16));
            BaseStream.WriteByte((byte)(Value >> 24));
        }

        public void Write(int Value)
        {
            Write((uint)Value);
        }

        public void Write(float Value)
        {
            Write(BitConverter.ToUInt32(BitConverter.GetBytes(Value), 0));
        }

        public void Write(ulong Value)
        {
            if (Endian == EndianBinary.Endian.Little)
            {
                BaseStream.WriteByte((byte)(Value & 0xff));
                BaseStream.WriteByte((byte)(Value >> 8));
                BaseStream.WriteByte((byte)(Value >> 16));
                BaseStream.WriteByte((byte)(Value >> 24));
                BaseStream.WriteByte((byte)(Value >> 32));
                BaseStream.WriteByte((byte)(Value >> 40));
                BaseStream.WriteByte((byte)(Value >> 48));
                BaseStream.WriteByte((byte)(Value >> 56));
            }
            else
            {
                BaseStream.WriteByte((byte)(Value >> 56));
                BaseStream.WriteByte((byte)(Value >> 48));
                BaseStream.WriteByte((byte)(Value >> 40));
                BaseStream.WriteByte((byte)(Value >> 32));
                BaseStream.WriteByte((byte)(Value >> 24));
                BaseStream.WriteByte((byte)(Value >> 16));
                BaseStream.WriteByte((byte)(Value >> 8));
                BaseStream.WriteByte((byte)(Value & 0xff));
            }
        }

        public void Write(byte[] Buff)
        {
            BaseStream.Write(Buff, 0, Buff.Length);
        }

        public void Write(byte[] Buff, int Index, int Length)
        {
            BaseStream.Write(Buff, Index, Length);
        }

        public void Seek(long Offset, SeekOrigin Origin)
        {
            BaseStream.Seek(Offset, Origin);
        }

        public void Close()
        {
            BaseStream.Close();
        }
    }
}

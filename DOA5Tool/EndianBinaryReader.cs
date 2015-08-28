using System;
using System.IO;

namespace DOA5Tool
{
    class EndianBinaryReader
    {
        public Stream BaseStream { get; set; }
        private EndianBinary.Endian Endian;

        public EndianBinaryReader(Stream Input, EndianBinary.Endian Endianess)
        {
            BaseStream = Input;
            Endian = Endianess;
        }

        public byte ReadByte()
        {
            return (byte)BaseStream.ReadByte();
        }

        public ushort ReadUInt16()
        {
            if (Endian == EndianBinary.Endian.Little)
                return (ushort)(BaseStream.ReadByte() | (BaseStream.ReadByte() << 8));
            else
                return (ushort)((BaseStream.ReadByte() << 8) | BaseStream.ReadByte());
        }

        public short ReadInt16()
        {
            return (short)ReadUInt16();
        }

        public uint ReadUInt32()
        {
            if (Endian == EndianBinary.Endian.Little)
                return (uint)(BaseStream.ReadByte() |
                    (BaseStream.ReadByte() << 8) |
                    (BaseStream.ReadByte() << 16) |
                    (BaseStream.ReadByte() << 24));
            else
                return (uint)((BaseStream.ReadByte() << 24) |
                    (BaseStream.ReadByte() << 16) |
                    (BaseStream.ReadByte() << 8) |
                    BaseStream.ReadByte());
        }

        public int ReadInt32()
        {
            return (int)ReadUInt32();
        }

        public float ReadSingle()
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(ReadUInt32()), 0);
        }

        public ulong ReadUInt64()
        {
            if (Endian == EndianBinary.Endian.Little)
                return (ulong)(BaseStream.ReadByte() |
                    (BaseStream.ReadByte() << 8) |
                    (BaseStream.ReadByte() << 16) |
                    (BaseStream.ReadByte() << 24) |
                    (BaseStream.ReadByte() << 32) |
                    (BaseStream.ReadByte() << 40) |
                    (BaseStream.ReadByte() << 48) | 
                    (BaseStream.ReadByte() << 56));
            else
                return (ulong)((BaseStream.ReadByte() << 56) |
                    (BaseStream.ReadByte() << 48) |
                    (BaseStream.ReadByte() << 40) |
                    (BaseStream.ReadByte() << 32) |
                    (BaseStream.ReadByte() << 24) |
                    (BaseStream.ReadByte() << 16) |
                    (BaseStream.ReadByte() << 8) |
                    BaseStream.ReadByte());
        }

        public void Read(byte[] Buff, int Index, int Length)
        {
            BaseStream.Read(Buff, Index, Length);
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

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace DOA5Tool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Dead or Alive 5 LANG Dumper/Creator by gdkchan");
            Console.WriteLine("Version 0.2.1");
            Console.CursorTop++;
            Console.ResetColor();

            if (args.Length == 0)
            {
                PrintUsage();
            }
            else
            {
                string Argument = args[0];
                string Platform = args[1];
                string FileName = null;
                for (int i = 2; i < args.Length; i++) FileName += args[i];

                EndianBinary.Endian Endian = EndianBinary.Endian.Little;
                switch (Platform)
                {
                    case "-pc": Endian = EndianBinary.Endian.Little; break;
                    case "-console": Endian = EndianBinary.Endian.Big; break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid platform argument specified!");
                        Console.CursorTop++;
                        PrintUsage();
                        return;
                }

                if (((Argument == "-p" && !Directory.Exists(FileName)) || (Argument != "-p" && !File.Exists(FileName))) && FileName != "-all")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("File not found!");
                    Console.ResetColor();
                    return;
                }

                switch (Argument)
                {
                    case "-x":
                        if (FileName == "-all")
                        {
                            string[] Files = Directory.GetFiles(Environment.CurrentDirectory);
                            foreach (string File in Files) if (Path.GetExtension(File).ToLower() == ".lnk") Extract(File, Endian);
                        }
                        else
                            Extract(FileName, Endian);

                        break;
                    case "-p":
                        if (FileName == "-all")
                        {
                            string[] Folders = Directory.GetDirectories(Environment.CurrentDirectory);
                            foreach (string Folder in Folders) Pack(Folder, Endian);
                        }
                        else
                            Pack(FileName, Endian);

                        break;
                    case "-d":
                        if (FileName == "-all")
                        {
                            string[] Files = Directory.GetFiles(Environment.CurrentDirectory);
                            foreach (string File in Files) if (Path.GetExtension(File).ToLower() == ".lang") Dump(File, Endian);
                        }
                        else
                            Dump(FileName, Endian);

                        break;
                    case "-c":
                        if (FileName == "-all")
                        {
                            string[] Files = Directory.GetFiles(Environment.CurrentDirectory);
                            foreach (string File in Files) if (Path.GetExtension(File).ToLower() == ".txt") Create(File, Endian);
                        }
                        else
                            Create(FileName, Endian);

                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid operation argument specified!");
                        Console.CursorTop++;
                        PrintUsage();
                        break;
                }
            }
        }

        static void PrintUsage()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Usage:");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("DOA5Tool.exe [operation] [platform] [file|-all]");
            Console.CursorTop++;
            Console.WriteLine("[operation]");
            Console.WriteLine("-x  Extract a *.lnk file to a folder");
            Console.WriteLine("-p  Pack a *.lnk file from a folder");
            Console.WriteLine("-d  Dumps a *.lang file to a *.txt file");
            Console.WriteLine("-c  Creates a *.lang file from a *.txt file");
            Console.CursorTop++;
            Console.WriteLine("[platform]");
            Console.WriteLine("-pc  For the PC version of the game");
            Console.WriteLine("-console  For the Xbox/Playstation version of the game");
            Console.CursorTop++;
            Console.WriteLine("-all  Manipulate all the files on the work directory");
            Console.CursorTop++;
            Console.WriteLine("Note: -p file name format must be file_[N][.bin|.lang]");
            Console.ResetColor();
        }

        private struct Section
        {
            public string Signature;
            public uint BaseOffset;
            public uint SectionType;
            public uint HeaderLength;
            public uint SectionLength;
            public uint PointerCount;
            public uint LengthCount;
            public uint PackPointersOffset;
            public uint PackLengthsOffset;
        }

        private struct PackEntry
        {
            public uint Offset;
            public uint Length;
        }

        static void Dump(string FileName, EndianBinary.Endian Endian)
        {
            string OutFile = Path.GetFileNameWithoutExtension(FileName) + ".txt";
            StringBuilder Output = new StringBuilder();

            FileStream Input = new FileStream(FileName, FileMode.Open);
            EndianBinaryReader Reader = new EndianBinaryReader(Input, Endian);

            Section Lang = ParseSection(Reader);
            if (Lang.Signature != "LANG")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid file!");
                Console.ResetColor();
            }

            Input.Seek(ParsePack(Reader, Lang)[0].Offset, SeekOrigin.Begin);
            Section Category = ParseSection(Reader);
            Input.Seek(ParsePack(Reader, Category)[0].Offset, SeekOrigin.Begin);
            Section String = ParseSection(Reader);
            List<PackEntry> StringPack = ParsePack(Reader, String);

            foreach (PackEntry Entry in StringPack)
            {
                Input.Seek(Entry.Offset, SeekOrigin.Begin);

                byte[] Buffer = new byte[Entry.Length - 2];
                Reader.Read(Buffer, 0, Buffer.Length);
                Output.Append(UTF16.GetString(Buffer, Endian));
                Output.AppendLine(null);
                Output.AppendLine(null);
            }

            File.WriteAllText(OutFile, Output.ToString().TrimEnd());

            Reader.Close();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Dumped " + Path.GetFileName(FileName) + "!");
            Console.ResetColor();
        }

        static Section ParseSection(EndianBinaryReader Reader)
        {
            uint InitialPosition = (uint)Reader.BaseStream.Position;
            Section Output = new Section();

            byte b = Reader.ReadByte();
            MemoryStream SignatureBuffer = new MemoryStream();
            while (b != 0)
            {
                SignatureBuffer.WriteByte(b);
                b = Reader.ReadByte();
            }
            Output.Signature = Encoding.ASCII.GetString(SignatureBuffer.ToArray());
            Output.BaseOffset = InitialPosition;
            SignatureBuffer.Dispose();

            Reader.BaseStream.Seek(InitialPosition + 8, SeekOrigin.Begin); //Pula parte da assinatura
            Output.SectionType = Reader.ReadUInt32();
            Output.HeaderLength = Reader.ReadUInt32();
            Output.SectionLength = Reader.ReadUInt32();
            Output.PointerCount = Reader.ReadUInt32();
            Output.LengthCount = Reader.ReadUInt32();
            Reader.ReadUInt32(); //Padding
            Output.PackPointersOffset = Reader.ReadUInt32() + InitialPosition;
            Output.PackLengthsOffset = Reader.ReadUInt32() + InitialPosition;
            Reader.ReadUInt32(); //Padding
            Reader.ReadUInt32(); //Padding

            return Output;
        }

        static List<PackEntry> ParsePack(EndianBinaryReader Reader, Section PackSection)
        {
            List<PackEntry> Output = new List<PackEntry>();

            for (int i = 0; i < PackSection.PointerCount; i++)
            {
                PackEntry Entry = new PackEntry();

                Reader.BaseStream.Seek(PackSection.PackPointersOffset + i * 4, SeekOrigin.Begin);
                Entry.Offset = Reader.ReadUInt32() + PackSection.BaseOffset;

                Reader.BaseStream.Seek(PackSection.PackLengthsOffset + i * 4, SeekOrigin.Begin);
                Entry.Length = Reader.ReadUInt32();

                Output.Add(Entry);
            }

            return Output;
        }

        static void Create(string FileName, EndianBinary.Endian Endian)
        {
            string OutFile = Path.GetFileNameWithoutExtension(FileName) + ".lang";
            FileStream Output = new FileStream(OutFile, FileMode.Create);
            EndianBinaryWriter Writer = new EndianBinaryWriter(Output, Endian);

            /*
             * Seção String
             */

            MemoryStream String = new MemoryStream();
            EndianBinaryWriter StringWriter = new EndianBinaryWriter(String, Endian);

            string[] Texts = File.ReadAllText(FileName).Split(new string[] { Environment.NewLine + Environment.NewLine }, StringSplitOptions.None);

            List<byte[]> Buffers = new List<byte[]>();
            foreach (string Text in Texts) Buffers.Add(UTF16.GetBytes(Text, Endian));

            MemoryStream TextBlock = new MemoryStream();
            foreach (byte[] Buffer in Buffers)
            {
                TextBlock.Write(Buffer, 0, Buffer.Length);
                TextBlock.WriteByte(0);
                TextBlock.WriteByte(0); //0x00 00 Null Terminator
                while ((TextBlock.Position & 0xf) != 0) TextBlock.WriteByte(0); //Alinha
            }

            StringWriter.Write(Encoding.ASCII.GetBytes("STRPACK"));
            StringWriter.Write(0);
            StringWriter.WriteLE((uint)(0x10000 | (Endian == EndianBinary.Endian.Little ? 0 : 0xff)));
            StringWriter.Write((uint)0x30);
            StringWriter.Write((uint)(0x60 + Align(Texts.Length * 4) * 2 + TextBlock.Length)); //Tamanho da seção
            StringWriter.Write((uint)Texts.Length); //Total de ponteiros
            StringWriter.Write((uint)Texts.Length); //Total de tamanhos
            StringWriter.Write((uint)0); //Alinha em 16 bytes
            StringWriter.Write((uint)0x60); //Offset relativo de inicio dos ponteiros
            StringWriter.Write(0x60 + Align(Texts.Length * 4)); //Offset relativo de inicio dos tamanhos
            StringWriter.Write((ulong)0); //Alinha em 16 bytes

            StringWriter.Write(Encoding.ASCII.GetBytes("string_pack"));
            while ((String.Position & 0xf) != 0) String.WriteByte(0);
            StringWriter.Write((uint)1); //Número de seções
            StringWriter.Write((uint)0x30); //Offset dos dados
            StringWriter.Write((ulong)0); //Alinha em 16 bytes
            StringWriter.Write((uint)0x54); //???
            String.Seek(0xc, SeekOrigin.Current);

            uint DataOffset = 0x60 + Align(Texts.Length * 4) * 2;
            foreach (byte[] Buffer in Buffers)
            {
                StringWriter.Write(DataOffset);
                DataOffset += Align(Buffer.Length + 2);
            }
            while ((String.Position & 0xf) != 0) String.WriteByte(0);

            foreach (byte[] Buffer in Buffers) StringWriter.Write((uint)Buffer.Length + 2);
            while ((String.Position & 0xf) != 0) String.WriteByte(0);

            StringWriter.Write(TextBlock.ToArray());
            TextBlock.Close();

            /*
             * Seção Category
             */

            MemoryStream Category = new MemoryStream();
            EndianBinaryWriter CategoryWriter = new EndianBinaryWriter(Category, Endian);

            CategoryWriter.Write(Encoding.ASCII.GetBytes("CTGPACK"));
            CategoryWriter.Write(0);
            CategoryWriter.WriteLE((uint)(0x10000 | (Endian == EndianBinary.Endian.Little ? 0 : 0xff)));
            CategoryWriter.Write((uint)0x30);
            CategoryWriter.Write((uint)String.Length + 0x80); //Tamanho da seção
            CategoryWriter.Write((uint)1); //Total de ponteiros
            CategoryWriter.Write((uint)1); //Total de tamanhos
            CategoryWriter.Write((uint)0); //Alinha em 16 bytes
            CategoryWriter.Write((uint)0x60); //Offset relativo de inicio dos ponteiros
            CategoryWriter.Write((uint)0x70); //Offset relativo de inicio dos tamanhos
            CategoryWriter.Write((ulong)0); //Alinha em 16 bytes

            CategoryWriter.Write(Encoding.ASCII.GetBytes("category_pack"));
            while ((Category.Position & 0xf) != 0) Category.WriteByte(0);
            CategoryWriter.Write((uint)1); //Número de seções
            CategoryWriter.Write((uint)0x30); //Offset dos dados
            CategoryWriter.Write((ulong)0); //Alinha em 16 bytes
            Category.Seek(0xc, SeekOrigin.Current);
            if (Endian == EndianBinary.Endian.Little)
                CategoryWriter.Write(0x18fc2c); //???
            else
                CategoryWriter.Write((uint)0);

            CategoryWriter.Write((uint)0x80); //Offset relativo onde se inicia a outra seção
            Category.Seek(0xc, SeekOrigin.Current);
            CategoryWriter.Write((uint)String.Length); //Tamanho da outra seção
            Category.Seek(0xc, SeekOrigin.Current);

            CategoryWriter.Write(String.ToArray());
            String.Close();

            /*
             * Seção Lang
             */

            Writer.Write(Encoding.ASCII.GetBytes("LANG"));
            Writer.Write((uint)0);
            Writer.WriteLE((uint)(0x10000 | (Endian == EndianBinary.Endian.Little ? 0 : 0xff)));
            Writer.Write((uint)0x30);
            Writer.Write((uint)Category.Length + 0x80); //Tamanho da seção
            Writer.Write((uint)1); //Total de ponteiros
            Writer.Write((uint)1); //Total de tamanhos
            Writer.Write((uint)0); //Alinha em 16 bytes
            Writer.Write((uint)0x60); //Offset relativo de inicio dos ponteiros
            Writer.Write((uint)0x70); //Offset relativo de inicio dos tamanhos
            Writer.Write((ulong)0); //Alinha em 16 bytes

            Writer.Write(Encoding.ASCII.GetBytes("language_pack"));
            while ((Output.Position & 0xf) != 0) Output.WriteByte(0);
            Writer.Write((uint)1); //Número de seções
            Writer.Write((uint)0x30); //Offset dos dados
            Writer.Write((ulong)0); //Alinha em 16 bytes
            Output.Seek(0x10, SeekOrigin.Current);

            Writer.Write((uint)0x80); //Offset relativo onde se inicia a outra seção
            Output.Seek(0xc, SeekOrigin.Current);
            Writer.Write((uint)Category.Length); //Tamanho da outra seção
            Output.Seek(0xc, SeekOrigin.Current);

            Writer.Write(Category.ToArray());
            Category.Close();

            Output.Close();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Created " + Path.GetFileName(OutFile) + "!");
            Console.ResetColor();
        }

        static uint Align(int Value)
        {
            while ((Value & 0xf) != 0) Value++; //Alinha em 16 bytes
            return (uint)Value;
        }


        static void Extract(string FileName, EndianBinary.Endian Endian)
        {
            FileStream Input = new FileStream(FileName, FileMode.Open);
            EndianBinaryReader Reader = new EndianBinaryReader(Input, Endian);

            byte b = Reader.ReadByte();
            MemoryStream SignatureBuffer = new MemoryStream();
            while (b != 0)
            {
                SignatureBuffer.WriteByte(b);
                b = Reader.ReadByte();
            }
            string Signature = Encoding.ASCII.GetString(SignatureBuffer.ToArray());
            SignatureBuffer.Dispose();
            Reader.BaseStream.Seek(8, SeekOrigin.Begin); //Pula parte da assinatura

            if (Signature != "MSSG")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid file!");
                Console.ResetColor();
            }

            string OutDir = Path.GetFileNameWithoutExtension(FileName);
            Directory.CreateDirectory(OutDir);

            ulong FileCount = Reader.ReadUInt64();
            ulong FileLength = Reader.ReadUInt64();
            Reader.ReadUInt64(); //??? 0x800

            for (ulong Entry = 0; Entry < FileCount; Entry++)
            {
                Input.Seek((long)(0x20 + Entry * 0x20), SeekOrigin.Begin);

                ulong Offset = Reader.ReadUInt64();
                ulong Length1 = Reader.ReadUInt64();
                ulong Length2 = Reader.ReadUInt64();
                Reader.ReadUInt64(); //Padding

                Input.Seek((long)Offset, SeekOrigin.Begin);
                byte[] Buffer = new byte[Length1];
                Reader.Read(Buffer, 0, Buffer.Length);

                string SubFileSignature = Encoding.ASCII.GetString(Buffer, 0, 4);
                bool IsLangFile = SubFileSignature == "LANG";
                File.WriteAllBytes(Path.Combine(OutDir, "file_" + Entry.ToString() + (IsLangFile ? ".lang" : ".bin")), Buffer);
            }

            Reader.Close();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Extracted all files from " + Path.GetFileName(FileName) + "!");
            Console.ResetColor();
        }

        static void Pack(string Folder, EndianBinary.Endian Endian)
        {
            int FileCount = Directory.GetFiles(Folder).Length;

            int RealCount = 0;
            for (int i = 0; i < FileCount; i++)
            {
                string FileName = Path.Combine(Folder, "file_" + i.ToString() + ".lang");
                if (!File.Exists(FileName)) FileName = Path.GetFileNameWithoutExtension(FileName) + ".bin";
                if (File.Exists(FileName)) RealCount++;
            }

            FileStream Output = new FileStream(Folder + ".lnk", FileMode.Create);
            EndianBinaryWriter Writer = new EndianBinaryWriter(Output, Endian);
            Writer.Write(Encoding.ASCII.GetBytes("MSSG"));
            Writer.Write((uint)0);
            Writer.Write((ulong)RealCount);

            long TableOffset = 0x20;
            long DataOffset = TableOffset + RealCount * 0x20;
            while ((DataOffset & 0x7ff) != 0) DataOffset++;

            for (int i = 0; i < FileCount; i++)
            {
                string FileName = Path.Combine(Folder, "file_" + i.ToString() + ".lang");
                if (!File.Exists(FileName)) FileName = Path.GetFileNameWithoutExtension(FileName) + ".bin";

                if (File.Exists(FileName))
                {
                    byte[] Buffer = File.ReadAllBytes(FileName);

                    Writer.Seek(TableOffset, SeekOrigin.Begin);
                    Writer.Write((ulong)DataOffset);
                    Writer.Write((ulong)Buffer.Length);
                    Writer.Write((ulong)Buffer.Length);
                    TableOffset += 0x20;

                    Writer.Seek(DataOffset, SeekOrigin.Begin);
                    Writer.Write(Buffer);
                    while ((Output.Position & 0x7ff) != 0) Output.WriteByte(0);
                    DataOffset = Output.Position;
                }
            }

            Writer.Seek(0x10, SeekOrigin.Begin);
            Writer.Write((ulong)Output.Length);
            Writer.Write((ulong)0x800); //???

            Writer.Close();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Packed all files from \"" + Path.GetFileName(Folder) + "\"!");
            Console.ResetColor();
        }
    }
}

using System;
using System.IO;
using ZLink.Core;

namespace ZLink
{
    class ZLink
    {
        SimpleMemory _memory = null;
        Linker _linker = null;

        int memsz = 1024 * 1024;
        int offset = 0;
        int _limit = 0;
        bool _readable = false;
        bool _writable = false;
        bool _executable = false;
        int align = 16;
        bool _finished = false;

        public void Finish()
        {
            if (!_finished)
            {
                GetLinker().link(false);
                _finished = true;
            }
        }

        public int GetLimit()
        {
            if (_limit <= 0 || _limit > memsz)
            {
                return memsz;
            }
            else
            {
                return _limit;
            }
        }

        public SimpleMemory GetMemory()
        {
            if (_memory == null)
            {
                _memory = new SimpleMemory(memsz);
            }
            return _memory;
        }

        public Linker GetLinker()
        {
            if (_linker == null)
            {
                _linker = new Linker(GetMemory());
            }
            return _linker;
        }

        static void Usage(string msg = null)
        {
            Console.WriteLine("USAGE:\n TODO!");
        }

        string StringArg(string[] args, int n)
        {
            if (n >= args.Length)
            {
                Usage("Expecting additional (string/name) argument");
                throw new Exception("Failed.");
            }

            return args[n];
        }

        ulong NumberArg(string[] args, int n)
        {
            if (n >= args.Length)
            {
                Usage("Expecting additional (number) argument");
                throw new Exception("Failed.");
            }
            ulong mul = 1;
            string s = args[n];
            if (s.EndsWith("GB"))
            {
                mul = 1024 * 1024 * 1024;
                s = s.Substring(0, s.Length - 2);
            }
            else if (s.EndsWith("MB"))
            {
                mul = 1024 * 1024;
                s = s.Substring(0, s.Length - 2);
            }
            else if (s.EndsWith("KB"))
            {
                mul = 1024;
                s = s.Substring(0, s.Length - 2);
            }

            return mul * UInt64.Parse(s);
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Zak's Linker (early version...)");

            ZLink link = new ZLink();

            int argi = 0;
            while (argi < args.Length)
            {
                switch (args[argi])
                {
                    case "--memory":
                        link.memsz = (int)link.NumberArg(args, ++argi);
                        break;
                    case "--offset":
                        link.offset = (int)link.NumberArg(args, ++argi);
                        break;
                    case "--limit":
                        link._limit = (int)link.NumberArg(args, ++argi);
                        break;
                    case "--dump":
                        link.Finish();
                        byte[] data = new byte[link.GetLimit() - link.offset];
                        for (int i = 0; i < data.Length; i++)
                        {
                            data[i] = link.GetMemory().getU8((ulong)(link.offset + i), false);
                        }
                        File.WriteAllBytes(link.StringArg(args, ++argi), data);
                        break;
                    case "--readable":
                        link._readable = true;
                        break;
                    case "--writable":
                        link._writable = true;
                        break;
                    case "--executable":
                        link._executable = true;
                        break;
                    case "--align":
                        link.align = (int)link.NumberArg(args, ++argi);
                        break;
                    case "--section":
                        link.GetLinker().setup(link.StringArg(args, ++argi), (ulong)link.offset, link._readable, link._writable, link._executable, (uint)link.align);
                        link._readable = link._writable = link._executable = false;
                        break;

                    case "--help":
                    case "--usage":
                    case "-h":
                    case "-u":
                        Usage();
                        return;
                    default:
                        byte[] fbytes = File.ReadAllBytes(args[argi]);
                        SimpleMemory fmem = new SimpleMemory(fbytes);
                        LibraryFile f = new LibraryFile(args[argi], fmem);
                        //Dump(f);
                        link.GetLinker().add(f);
                        break;
                }
                argi++;
            }
        }
    }
}

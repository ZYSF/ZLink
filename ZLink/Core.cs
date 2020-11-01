using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Text;

namespace ZLink.Core
{
    public class MemoryException : Exception
    {
        Memory memory;
        ulong address;

        public MemoryException(Memory memory, ulong address, string message) : base(message)
        {
            this.memory = memory;
            this.address = address;
        }

        public MemoryException()
        {
        }

        public MemoryException(string message) : base(message)
        {
        }

        public MemoryException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MemoryException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
    public abstract class Memory
    {
        public abstract byte getU8(ulong address, bool executing);
        public abstract void setU8(ulong address, byte value);

        public byte[] copyBytes(ulong address, uint nbytes, bool executing = false)
        {
            byte[] result = new byte[nbytes];
            for (uint i = 0; i < nbytes; i++)
            {
                result[i] = getU8(address + i, executing);
            }
            return result;
        }
        public sbyte[] copySignedBytes(ulong address, uint nbytes, bool executing = false)
        {
            sbyte[] result = new sbyte[nbytes];
            for (uint i = 0; i < nbytes; i++)
            {
                result[i] = (sbyte)getU8(address + i, executing);
            }
            return result;
        }
        public char[] copyASCII(ulong address, uint nbytes, bool executing = false)
        {
            char[] result = new char[nbytes];
            for (uint i = 0; i < nbytes; i++)
            {
                result[i] = (char)getU8(address + i, executing);
                //Console.WriteLine("Got char " + (int)result[i]);
            }
            return result;
        }

        public uint strlen(ulong address)
        {
            uint l = 0;
            while (getU8(address + l, false) != 0)
            {
                l++;
            }
            return l;
        }

        public string copyString(ulong address)
        {
            char[] b = copyASCII(address, strlen(address));
            return new string(b); // TODO: Work out how to create a string from UTF-8 data in C# (it's not as easy as Java apparently...)
        }

        public ushort getU16(ulong address, bool executing, bool bigEndian = false)
        {
            ushort a = getU8(address, executing);
            ushort b = getU8(address + 1, executing);
            if (bigEndian)
            {
                return (ushort)(b | (a << 8));
            }
            else
            {
                return (ushort)(a | (b << 8));
            }
        }

        public void setU16(ulong address, ushort value, bool bigEndian = false)
        {
            if (bigEndian)
            {
                setU8(address, (byte)(value >> 8));
                setU8(address + 1, (byte)value);
            }
            else
            {
                setU8(address, (byte)value);
                setU8(address + 1, (byte)(value >> 8));
            }
        }
        public uint getU32(ulong address, bool executing, bool bigEndian = false)
        {
            uint a = getU16(address, executing);
            uint b = getU16(address + 2, executing);
            if (bigEndian)
            {
                return (uint)(b | (a << 16));
            }
            else
            {
                return (uint)(a | (b << 16));
            }
        }

        public void setU32(ulong address, uint value, bool bigEndian = false)
        {
            if (bigEndian)
            {
                setU16(address, (ushort)(value >> 16));
                setU16(address + 2, (ushort)value);
            }
            else
            {
                setU16(address, (ushort)value);
                setU16(address + 2, (ushort)(value >> 16));
            }
        }
        public ulong getU64(ulong address, bool executing, bool bigEndian = false)
        {
            ulong a = getU32(address, executing);
            ulong b = getU32(address + 4, executing);
            if (bigEndian)
            {
                return (ulong)(b | (a << 32));
            }
            else
            {
                return (ulong)(a | (b << 32));
            }
        }

        public void setU64(ulong address, ulong value, bool bigEndian = false)
        {
            if (bigEndian)
            {
                setU32(address, (byte)(value >> 32));
                setU32(address + 4, (byte)value);
            }
            else
            {
                setU32(address, (byte)value);
                setU32(address + 4, (byte)(value >> 32));
            }
        }
    }

    public class SimpleMemory : Memory
    {
        byte[] bytes;

        public SimpleMemory(byte[] bytes)
        {
            this.bytes = bytes;
        }

        public SimpleMemory(int nbytes)
        {
            this.bytes = new byte[nbytes];
        }

        public int intAddress(ulong address)
        {
            int iaddress = (int)address;
            if (address != (ulong)iaddress || iaddress >= bytes.Length)
            {
                throw new MemoryException(this, address, "This address is outside of this memory");
            }
            return iaddress;
        }

        public override byte getU8(ulong address, bool executing)
        {
            return bytes[intAddress(address)];
        }

        public override void setU8(ulong address, byte value)
        {
            bytes[intAddress(address)] = value;
        }
    }

    public class MemoryWindow : Memory
    {
        public Memory backing;
        public ulong vstart;
        public ulong pstart;
        public ulong size;

        public MemoryWindow(Memory backing, ulong vstart, ulong pstart, ulong size)
        {
            this.backing = backing;
            this.vstart = vstart;
            this.pstart = pstart;
            this.size = size;
        }

        public ulong physicalAddress(ulong address)
        {
            ulong offset = address - vstart;
            if (offset >= size)
            {
                throw new MemoryException(this, address, "Address is outside of memory window");
            }
            return pstart + offset;
        }

        public override byte getU8(ulong address, bool executing)
        {
            return backing.getU8(physicalAddress(address), executing);
        }

        public override void setU8(ulong address, byte value)
        {
            backing.setU8(physicalAddress(address), value);
        }
    }

    public class LinkerException : Exception
    {
        Linker linker;
        LibraryFile library;
        public LinkerException(LibraryFile library, string message) : base(message)
        {
            this.library = library;
        }
        public LinkerException(LibraryFile library, string message, Exception inner) : base(message, inner)
        {
            this.library = library;
        }
        public LinkerException(Linker linker, string message) : base(message)
        {
            this.linker = linker;
        }
        public LinkerException(Linker linker, string message, Exception inner) : base(message, inner)
        {
            this.linker = linker;
        }
    }
    public class LibraryFile
    {
        public static string EXPECTED_MAGIC = "ASMDATA1";
        public static uint EXPECTED_VERSION = 1;

        public static ulong OFFSET_MAGIC = 0;
        public static ulong OFFSET_VERSION = 8;
        public static ulong OFFSET_PAGESIZE = 12;

        public static ulong OFFSET_HINT1 = 16;
        public static ulong OFFSET_HINT2 = 24;
        public static ulong OFFSET_INTHINT = 32;

        public static ulong OFFSET_STRINDEX = 36;
        public static ulong OFFSET_HDRINDEX = 40;
        public static ulong OFFSET_NSECTIONS = 44;

        public static ulong OFFSET_SECTIONSTART = 48;
        public static ulong OFFSET_SECTIONSIZE = 64;

        public static ulong OFFSET_SECTIONEFLAGS = 0;
        public static ulong OFFSET_SECTIONCFLAGS = 4;
        public static ulong OFFSET_SECTIONFOFFSET = 8;
        public static ulong OFFSET_SECTIONVOFFSET = 16;
        public static ulong OFFSET_SECTIONFSIZE = 24;
        public static ulong OFFSET_SECTIONVSIZE = 32;
        public static ulong OFFSET_SECTIONRESERVED = 40;
        public static ulong OFFSET_SECTIONNAMESTR = 48;
        public static ulong OFFSET_SECTIONHASH = 56;

        public static ulong OFFSET_SYMBOLSIZE = 40;// without sub-expressions was 24;
        public static ulong OFFSET_SYMBOLFLAGS = 0;
        public static ulong OFFSET_SYMBOLSECTION = 4;
        public static ulong OFFSET_SYMBOLOFFSET = 8;
        public static ulong OFFSET_SYMBOLNAMESTR = 16;
        public static ulong OFFSET_SYMBOLXLHS = 24;
        public static ulong OFFSET_SYMBOLXOP = 28;
        public static ulong OFFSET_SYMBOLXRHS = 32;
        public static ulong OFFSET_SYMBOLRESERVED = 36;

        public static ulong OFFSET_REFERENCESIZE = 24;
        public static ulong OFFSET_REFERENCEBASEFLAGS = 0;
        public static ulong OFFSET_REFERENCEWIDTH = 1;
        public static ulong OFFSET_REFERENCEEXTFLAGS = 2;
        public static ulong OFFSET_REFERENCESECTION = 4;
        public static ulong OFFSET_REFERENCEOFFSET = 8;
        public static ulong OFFSET_REFERENCESYMBOL = 16;
        public static ulong OFFSET_REFERENCEEXTDATA = 20;

        public string identifier;
        public Memory memory;
        public bool bigEndian;
        public uint version;

        public uint strindex;
        public uint hdrindex;
        public uint nsections;

        public ulong stroffset;
        public ulong strsize;

        public uint nsymbols;
        public uint symindex;
        public ulong symoffset;
        public ulong symsize;

        public uint nreferences;
        public uint refindex;
        public ulong refoffset;
        public ulong refsize;

        public LibraryFile(string identifier, Memory memory)
        {
            this.identifier = identifier;
            this.memory = memory;
            string magic = new string(memory.copyASCII(OFFSET_MAGIC, (uint)EXPECTED_MAGIC.Length));
            if (!magic.Equals(EXPECTED_MAGIC))
            {
                //Console.WriteLine("Got magic '" + magic + "'");
                throw new LinkerException(this, "Bad magic string (file should start with '" + EXPECTED_MAGIC + "')");
            }
            this.bigEndian = checkBigEndian();
            this.version = checkVersion();
            this.strindex = memory.getU32(OFFSET_STRINDEX, false, bigEndian);
            this.hdrindex = memory.getU32(OFFSET_HDRINDEX, false, bigEndian);
            this.nsections = memory.getU32(OFFSET_NSECTIONS, false, bigEndian);

            stroffset = sectionFileOffset(strindex);
            strsize = sectionFileSize(strindex);

            if (hasSection("asmdata.symbols"))
            {
                symindex = findSection("asmdata.symbols");
                symoffset = sectionFileOffset(symindex);
                symsize = sectionFileSize(symindex);
                nsymbols = (uint)(symsize / OFFSET_SYMBOLSIZE);
            }
            if (hasSection("asmdata.references"))
            {
                refindex = findSection("asmdata.references");
                refoffset = sectionFileOffset(refindex);
                refsize = sectionFileSize(refindex);
                nreferences = (uint)(refsize / OFFSET_REFERENCESIZE);
            }
        }

        public bool checkBigEndian()
        {
            return memory.getU32(OFFSET_VERSION, false, true) < 1000; // If the version number is "low" when read in big-endian, it's a big-endian file
        }

        public uint checkVersion()
        {
            uint v = memory.getU32(OFFSET_VERSION, false, bigEndian);
            if (v != EXPECTED_VERSION)
            {
                throw new LinkerException(this, "Bad version number");
            }
            return v;
        }

        public string str(ulong address)
        {
            if (address == 0)
            {
                return null;
            }
            return memory.copyString(stroffset + address);
        }

        public ulong sectionHeaderOffset(uint sectionNumber)
        {
            return OFFSET_SECTIONSTART + (sectionNumber * OFFSET_SECTIONSIZE);
        }

        public uint sectionEncodingFlags(uint sectionNumber)
        {
            return memory.getU32(sectionHeaderOffset(sectionNumber) + OFFSET_SECTIONEFLAGS, false, bigEndian);
        }
        public uint sectionContentFlags(uint sectionNumber)
        {
            return memory.getU32(sectionHeaderOffset(sectionNumber) + OFFSET_SECTIONCFLAGS, false, bigEndian);
        }

        public ulong sectionFileOffset(uint sectionNumber)
        {
            return memory.getU64(sectionHeaderOffset(sectionNumber) + OFFSET_SECTIONFOFFSET, false, bigEndian);
        }
        public ulong sectionVirtualOffset(uint sectionNumber)
        {
            return memory.getU64(sectionHeaderOffset(sectionNumber) + OFFSET_SECTIONVOFFSET, false, bigEndian);
        }

        public ulong sectionFileSize(uint sectionNumber)
        {
            return memory.getU64(sectionHeaderOffset(sectionNumber) + OFFSET_SECTIONFSIZE, false, bigEndian);
        }
        public ulong sectionVirtualSize(uint sectionNumber)
        {
            return memory.getU64(sectionHeaderOffset(sectionNumber) + OFFSET_SECTIONVSIZE, false, bigEndian);
        }

        public ulong sectionNameIndex(uint sectionNumber)
        {
            return memory.getU64(sectionHeaderOffset(sectionNumber) + OFFSET_SECTIONNAMESTR, false, bigEndian);
        }

        public string sectionName(uint sectionNumber)
        {
            return str(sectionNameIndex(sectionNumber));
        }

        public ulong sectionHash(uint sectionNumber)
        {
            return memory.getU64(sectionHeaderOffset(sectionNumber) + OFFSET_SECTIONHASH, false, bigEndian);
        }

        public bool hasSection(string name)
        {
            for (uint i = 0; i < nsections; i++)
            {
                if (sectionName(i).Equals(name))
                {
                    return true;
                }
            }
            return false;
        }

        public uint findSection(string name)
        {
            for (uint i = 0; i < nsections; i++)
            {
                if (sectionName(i).Equals(name))
                {
                    return i;
                }
            }
            return 0xFFFFFFFF;
        }

        /*
        
        public static ulong OFFSET_SYMBOLSIZE = 24;
        public static ulong OFFSET_SYMBOLFLAGS = 0;
        public static ulong OFFSET_SYMBOLSECTION = 4;
        public static ulong OFFSET_SYMBOLOFFSET = 8;
        public static ulong OFFSET_SYMBOLNAMESTR = 16;
        */
        public uint symbolFlags(uint symbolNumber)
        {
            return memory.getU32(symoffset + (symbolNumber * OFFSET_SYMBOLSIZE) + OFFSET_SYMBOLFLAGS, false, bigEndian);
        }
        public uint symbolSection(uint symbolNumber)
        {
            return memory.getU32(symoffset + (symbolNumber * OFFSET_SYMBOLSIZE) + OFFSET_SYMBOLSECTION, false, bigEndian);
        }
        public ulong symbolOffset(uint symbolNumber)
        {
            return memory.getU64(symoffset + (symbolNumber * OFFSET_SYMBOLSIZE) + OFFSET_SYMBOLOFFSET, false, bigEndian);
        }
        public ulong symbolNameIndex(uint symbolNumber)
        {
            return memory.getU64(symoffset + (symbolNumber * OFFSET_SYMBOLSIZE) + OFFSET_SYMBOLNAMESTR, false, bigEndian);
        }

        public string symbolName(uint symbolNumber)
        {
            return str(symbolNameIndex(symbolNumber));
        }

        public uint symbolXLHS(uint symbolNumber)
        {
            return memory.getU32(symoffset + (symbolNumber * OFFSET_SYMBOLSIZE) + OFFSET_SYMBOLXLHS, false, bigEndian);
        }

        public uint symbolXOp(uint symbolNumber)
        {
            return memory.getU32(symoffset + (symbolNumber * OFFSET_SYMBOLSIZE) + OFFSET_SYMBOLXOP, false, bigEndian);
        }

        public uint symbolXRHS(uint symbolNumber)
        {
            return memory.getU32(symoffset + (symbolNumber * OFFSET_SYMBOLSIZE) + OFFSET_SYMBOLXRHS, false, bigEndian);
        }

        public uint symbolReserved(uint symbolNumber)
        {
            return memory.getU32(symoffset + (symbolNumber * OFFSET_SYMBOLSIZE) + OFFSET_SYMBOLRESERVED, false, bigEndian);
        }

        /*
        public static ulong OFFSET_REFERENCESIZE = 24;
        public static ulong OFFSET_REFERENCEBASEFLAGS = 0;
        public static ulong OFFSET_REFERENCEWIDTH = 1;
        public static ulong OFFSET_REFERENCEEXTFLAGS = 2;
        public static ulong OFFSET_REFERENCESECTION = 4;
        public static ulong OFFSET_REFERENCEOFFSET = 8;
        public static ulong OFFSET_REFERENCESYMBOL = 16;
        public static ulong OFFSET_REFERENCEEXTDATA = 20;
        */

        public byte referenceBaseFlags(uint referenceNumber)
        {
            return memory.getU8(refoffset + (referenceNumber * OFFSET_REFERENCESIZE) + OFFSET_REFERENCEBASEFLAGS, false);
        }


        public byte referenceWidth(uint referenceNumber)
        {
            return memory.getU8(refoffset + (referenceNumber * OFFSET_REFERENCESIZE) + OFFSET_REFERENCEWIDTH, false);
        }

        public ushort referenceExtFlags(uint referenceNumber)
        {
            return memory.getU16(refoffset + (referenceNumber * OFFSET_REFERENCESIZE) + OFFSET_REFERENCEEXTFLAGS, false, bigEndian);
        }
        public uint referenceSection(uint referenceNumber)
        {
            return memory.getU32(refoffset + (referenceNumber * OFFSET_REFERENCESIZE) + OFFSET_REFERENCESECTION, false, bigEndian);
        }
        public ulong referenceOffset(uint referenceNumber)
        {
            return memory.getU64(refoffset + (referenceNumber * OFFSET_REFERENCESIZE) + OFFSET_REFERENCEOFFSET, false, bigEndian);
        }
        public uint referenceSymbol(uint referenceNumber)
        {
            return memory.getU32(refoffset + (referenceNumber * OFFSET_REFERENCESIZE) + OFFSET_REFERENCESYMBOL, false, bigEndian);
        }
        public uint referenceExtData(uint referenceNumber)
        {
            return memory.getU32(refoffset + (referenceNumber * OFFSET_REFERENCESIZE) + OFFSET_REFERENCEEXTDATA, false, bigEndian);
        }
    }

    public class Linker
    {
        public Memory target;
        public List<LinkedLibrary> libraries = new List<LinkedLibrary>();
        public Dictionary<string, LinkedSection> sections = new Dictionary<string, LinkedSection>();
        public Dictionary<string, LinkedSymbol> globals = new Dictionary<string, LinkedSymbol>();

        public Linker(Memory target)
        {
            this.target = target;
        }

        public LinkedSection setup(string name, ulong offset, bool readable, bool writable, bool executable, uint alignment = 16)
        {
            if (sections.ContainsKey(name))
            {
                throw new LinkerException(this, "Section '" + name + "' is already setup");
            }

            LinkedSection s = new LinkedSection();
            s.linker = this;
            s.name = name;
            s.offset = offset;
            s.size = 0;
            s.readable = readable;
            s.writable = writable;
            s.executable = executable;
            s.alignment = alignment;

            sections.Add(name, s);

            return s;
        }

        public LinkedLibrary add(LibraryFile library)
        {
            LinkedLibrary result = new LinkedLibrary();
            result.linker = this;
            result.file = library;
            result.symbols = new LinkedSymbol[library.nsymbols];
            for (uint i = 0; i < library.nsymbols; i++)
            {
                result.symbols[i] = new LinkedSymbol();
                result.symbols[i].library = result;
                result.symbols[i].index = i;
            }
            libraries.Add(result);

            for (uint i = 0; i < library.nsections; i++)
            {
                if (sections.ContainsKey(library.sectionName(i)))
                {
                    LinkedSection s = null;
                    sections.TryGetValue(library.sectionName(i), out s); // WTF? Why doesn't "Get"/"GetValue" work?
                    result.add(s.add(result, i));
                }
            }

            return result;
        }

        public void link(bool allowMissing = false)
        {
            foreach (LinkedLibrary l in libraries)
            {
                l.linkStage1();
            }
            foreach (LinkedLibrary l in libraries)
            {
                l.linkStage2(allowMissing);
            }
            foreach (LinkedLibrary l in libraries)
            {
                l.linkStage3(allowMissing);
            }
        }
    }

    public class LinkedLibrary
    {
        public Linker linker;
        public LibraryFile file;
        public List<LinkedSectionPart> parts = new List<LinkedSectionPart>();
        public LinkedSymbol[] symbols;

        public LinkedSectionPart add(LinkedSectionPart s)
        {
            parts.Add(s);
            return s;
        }

        public void linkStage1()
        {
            foreach (LinkedSymbol s in symbols)
            {
                LinkedSectionPart p = findPart(file.symbolSection(s.index));
                if (p != null) // We only process symbols which are actually defined (with a location)
                {
                    s.part = p;
                    s.address = p.startAddress + file.symbolOffset(s.index);

                    // TODO: Only do this if actually flagged as global!
                    // TODO: Check if already listed?

                    if (linker.globals.ContainsKey(s.name()))
                    {
                        Console.WriteLine("Symbol '" + s.name() + "' has already been defined (elsewhere?)");
                        //throw new LinkerException(file, "Symbol '" + s.name() + "' has already been defined (elsewhere?)");
                    }
                    else
                    {

                        Console.WriteLine("Symbol '" + s.name() + "' is a new one though");
                        linker.globals.Add(s.name(), s);
                    }
                }
            }
        }
        public void linkStage2(bool allowMissing)
        {
            foreach (LinkedSymbol s in symbols)
            {
                if (s.part == null && !s.isDummy())
                {
                    LinkedSymbol target;
                    if (linker.globals.TryGetValue(s.name(), out target))
                    {
                        s.part = target.part;
                        s.address = target.address;
                    }
                    else
                    {
                        if (!allowMissing)
                        {
                            throw new LinkerException(file, "Can't find a definition of symbol '" + s.name() + "'");
                        }
                    }
                }
            }
        }

        public void linkStage3(bool allowMissing)
        {
            foreach (LinkedSectionPart p in parts)
            {
                p.linkStage3(allowMissing);
            }
        }

        public LinkedSectionPart findPart(uint sectionNumber)
        {
            foreach (LinkedSectionPart p in parts)
            {
                if (p.librarySection == sectionNumber)
                {
                    return p;
                }
            }

            return null;
        }
    }

    public class LinkedSymbol
    {
        public LinkedLibrary library;
        public uint index;
        public LinkedSectionPart part;
        public ulong address;

        public ulong doexpr(ulong lhs, string op, ulong rhs)
        {
            long slhs = (long)lhs;
            long srhs = (long)rhs;
            switch (op)
            {
                case "+":
                    return lhs + rhs;
                case "-":
                    return lhs - rhs;
                case "*":
                    return (ulong)(slhs * srhs);
                case "/":
                    return (ulong)(slhs / srhs);
                case "%":
                    return (ulong)(slhs % srhs);
                case "<<":
                    return lhs << (int)rhs;
                case ">>":
                    return lhs >> (int)rhs;
                case "&":
                    return lhs & rhs;
                case "|":
                    return lhs | rhs;
                default:
                    throw new LinkerException(library.file, "Bad subexpression (" + lhs + " " + op + " " + rhs + ")");
            }
        }

        public ulong finalValue()
        {
            if (isDummy())
            {
                if (isConst())
                {
                    return library.file.symbolOffset(index);
                }
                else if (isExpr())
                {
                    LinkedSymbol l = library.symbols[library.file.symbolXLHS(index)];
                    LinkedSymbol o = library.symbols[library.file.symbolXOp(index)];
                    LinkedSymbol r = library.symbols[library.file.symbolXRHS(index)];
                    return doexpr(l.finalValue(), o.name(), r.finalValue());
                }
                else
                {
                    throw new LinkerException(library.file, "Bad reference (this symbol can't be evaluated)");
                }
            }
            else
            {
                return address;
            }
        }

        public string name()
        {
            return library.file.symbolName(index);
        }

        public bool isDummy()
        {
            return (library.file.symbolFlags(index) & (1 << 8)) != 0;
        }
        public bool isExpr()
        {
            return (library.file.symbolFlags(index) & (1 << 9)) != 0;
        }
        public bool isConst()
        {
            return (library.file.symbolFlags(index) & (1 << 10)) != 0;
        }
        public bool isOp()
        {
            return (library.file.symbolFlags(index) & (1 << 11)) != 0;
        }
    }

    public class LinkedSection
    {
        public Linker linker;
        public string name;
        public ulong offset;
        public ulong size;
        public bool readable;
        public bool writable;
        public bool executable;
        public uint alignment;
        List<LinkedSectionPart> parts = new List<LinkedSectionPart>();

        public LinkedSectionPart add(LinkedLibrary library, uint sectionNumber)
        {
            LinkedSectionPart s = new LinkedSectionPart();
            s.section = this;
            s.library = library;
            s.librarySection = sectionNumber;
            while ((size % alignment) != 0)
            {
                size++;
            }
            s.startAddress = offset + size;
            s.size = library.file.sectionVirtualSize(sectionNumber);
            size += s.size;
            parts.Add(s);

            ulong foffset = library.file.sectionFileOffset(sectionNumber);
            ulong fsize = library.file.sectionFileSize(sectionNumber);
            for (ulong i = 0; i < fsize; i++)
            {
                linker.target.setU8(s.startAddress + i, library.file.memory.getU8(foffset + i, false));
            }
            for (ulong j = fsize; j < s.size; j++)
            {
                linker.target.setU8(s.startAddress + j, 0); // Clear any remaining "virtual" (reserved) memory with zeroes
            }
            return s;
        }
    }

    public class LinkedSectionPart
    {
        public LinkedSection section;
        public LinkedLibrary library;
        public uint librarySection;
        public ulong startAddress;
        public ulong size;

        public void linkStage3(bool allowMissing)
        {
            for (uint i = 0; i < library.file.nreferences; i++)
            {
                if (library.file.referenceSection(i) == librarySection)
                {
                    LinkedSymbol sym = library.symbols[library.file.referenceSymbol(i)];
                    ulong addr = startAddress + library.file.referenceOffset(i);
                    Console.WriteLine("Fixing address at " + addr + " (offset " + library.file.referenceOffset(i) + ") to point to '" + sym.name() + "' (at " + sym.address + ")");
                    byte sz = library.file.referenceWidth(i);
                    switch (sz)
                    {
                        case 0: // 1<<0 = 1 bytes (8 bits)
                            library.linker.target.setU8(addr, (byte)sym.finalValue());
                            break;
                        case 1: // 1<<1 = 2 bytes (16 bits)
                            library.linker.target.setU16(addr, (ushort)sym.finalValue(), library.file.bigEndian);
                            break;
                        case 2: // 1<<2 = 4 bytes (32 bits)
                            library.linker.target.setU32(addr, (uint)sym.finalValue(), library.file.bigEndian);
                            break;
                        case 3: // 1<<3 = 8 bytes (64 bits)
                            library.linker.target.setU64(addr, sym.finalValue(), library.file.bigEndian);
                            break;
                        default:
                            throw new LinkerException(library.file, "Bad reference size: " + sz);
                    }
                }
            }
        }
    }


}

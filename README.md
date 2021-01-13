# ZLink
A simple linker to build binaries from the output of my assembler.

## Usage

The linker is expected to be run with one or more output files from [ZAsm](https://github.com/ZYSF/ZAsm/). Internally, it will link the data in these files into a flat binary structure, which you can then pull parts out of to create ROM images or other immediately-loadable programs.

### Setting The Memory Limit

Start with something like `--memory 100MB` to be on the safe side!

### Configuring Sections

Sections can be configured with `--offset` and `--section`:

    ... --offset 1MB --section code ...

Would imply that the code section should start at 1MB.

### Dumping data

Then we can dump, say, up to 1MB from that code section into `my_rom_image.bin`:

    ... --offset 1MB --limit 2MB --dump my_rom_image.bin ...

### Summary

So, assuming we want to just extract the code section at 1MB for a ROM image (but making sure we stop at/before the 2MB mark), and we have three input files:

    zlink --memory 2MB --offset 1MB --section code my_startup.obj my_graphics.obj my_app.obj --offset 1MB --limit 2MB --dump my_rom_image.bin

## TODO

 * Better documentation & testing
 * Probably add some more operators

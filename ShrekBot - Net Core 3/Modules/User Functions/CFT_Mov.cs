using FileTypeChecker;
using FileTypeChecker.Abstracts;

namespace ShrekBot
{
    //Custom File Type = CFT
    internal class CFT_Mov : FileType
    {
        //https://en.wikipedia.org/wiki/QuickTime_File_Format

        //private static readonly
        //    MagicSequence[] _magicBytes
        //    = { new MagicSequence
        //        (new byte[] { 0x00, 0x00, 0x00, 0x14, 0x66, 0x74, 0x79, 0x70, 0x71, 0x74 } )};

        public CFT_Mov()
            : base("Quick Time File Format",
                  "video/quicktime",
                  "mov",
                  new MagicSequence
                    (new byte[] { 0x00, 0x00, 0x00, 0x14, 0x66, 0x74, 0x79, 0x70, 0x71, 0x74 })) { }
    }
}

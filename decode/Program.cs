using System;

namespace decode
{
    class Program
    {
        static void Main(string[] args)
        {

            if (args.Length < 1)
                Usage();

            ParameterHandler(args);

        }

        static void ParameterHandler(string[] args)
        {

            switch (args[1])
            {
                case "md5":
                    Md5Calculate();
                    break;
                case "base64":
                    switch (args[2])
                    {
                        case "decode":
                            Base64Decode(args[3]);
                            break;
                        case "encode":
                            Base64Encode(args[3]);
                            break;
                        default:
                            Usage();
                            break;
                    }
                    break;
                default:
                    Usage();
                    break;
            }
        }

        private static void Base64Encode(string v)
        {
            throw new NotImplementedException();
        }

        private static void Base64Decode(string v)
        {
            throw new NotImplementedException();
        }

        private static void Md5Calculate()
        {
            throw new NotImplementedException();
        }

        static void Usage()
        {
            Console.WriteLine("Usage: ");
            Console.WriteLine("----------------");
            Console.WriteLine("\r\n--md5 ile [filename] - generate MD5 hash from file stream");
            Console.WriteLine("--base64 --decode - decode base64 encoded file");
            Console.WriteLine("--base64 --encode - encode file as base64, outfile is required");
            Console.WriteLine("");
            Console.WriteLine("Input options:");
            Console.WriteLine("----------------");
            Console.WriteLine("--infile - input file");
            Console.WriteLine("--infolder - input folder");
            Console.WriteLine("--s - include subfolders");
            Console.WriteLine("--ext - file extension to limit folder search such as txt, pdf, docx. If multiple extensions will be specified the list should be closed in quotations and comma seperated. ");
            Console.WriteLine("");
            Console.WriteLine("Output options:");
            Console.WriteLine("----------------");
            Console.WriteLine("--outfile - ");


        }
    }
}

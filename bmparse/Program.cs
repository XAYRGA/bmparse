
using System.IO;
using bmparse.debug;
using xayrga;
using xayrga.byteglider;
using xayrga.cmdl;
using Newtonsoft.Json;

namespace bmparse {
    public static class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("bmparse by XAYRGA");
            Console.WriteLine("Source Code: https://github.com/xayrga/bmparse");
            Console.WriteLine("Donate: https://ko-fi.com/xayrga");
            Console.WriteLine();
        

            cmdarg.cmdargs = args;
            var command = cmdarg.assertArg(0, "Operation");
            command = command.ToLower();

            switch (command)
            {
                case "disassemble":
                    {
                        var bmsFile = cmdarg.assertArg(1, "BMS File");
                        var projectOut = cmdarg.assertArg(2, "Project Folder");

                        cmdarg.assert(File.Exists(bmsFile), $"Cannot locate BMSFile {bmsFile}");

                        var nameFilePath = cmdarg.findDynamicStringArgument("-namefile", "NONE");
                        var nameContainer = new SEBSNameFile();
                        if (nameFilePath!="NONE")
                        {
                            Console.WriteLine($"Loading NAM file {nameFilePath}");
                            cmdarg.assert(File.Exists(nameFilePath), $"Cannot locate specified NAM file {nameFilePath}");

                            var fHnd = File.OpenRead(nameFilePath);
                            try {
                       
                                var reader = new bgReader(fHnd);
                                nameContainer.Read(reader);
                                reader.Close();
                            } catch (Exception E)
                            {
                                cmdarg.assert($"NAMFile is corrupted!\n{E}");
                            }
                        }

                        var bmsHandle = File.OpenRead(bmsFile);
                        var bmsReader = new bgReader(bmsHandle);

                        Console.Write("Analyzing link structure....");
                        var LinkAnalyzer = new BMSLinkageAnalyzer(bmsReader);
                        bmsReader.PushAnchor();
                        var LinkageInfo =  LinkAnalyzer.Analyze(0, 0, ReferenceType.ROOT); // Important! Reference 0x00 only here! 
                        bmsReader.PopAnchor();
                        Console.WriteLine($" OK! {LinkageInfo.Count} Link references in assembly.");

                        var Disassembler = new SEBMSDisassembler(bmsReader, LinkageInfo)
                        {
                            SoundNames = nameContainer.SoundNames,
                            CategoryNames = nameContainer.CategoryNames,
                            CodePageMapping = LinkAnalyzer.CodePageMapping // Need to clean this up. Oversight.
                        };


                        Disassembler.Disassemble(projectOut);
                        break;
                    }
                case "assemble":
                    {
                        var projectFolder = cmdarg.assertArg(1, "ProjectFolder");
                        var outFile = cmdarg.assertArg(2, "BMSFile");
                        cmdarg.assert(Directory.Exists(projectFolder), $"Project Folder {projectFolder} doesn't exist.");
                        cmdarg.assert(File.Exists($"{projectFolder}/project.json"),$"Project file at {projectFolder} doesn't contain a project.json! (Not a SEBS project?)");
                        var projectBuffer = File.ReadAllText($"{projectFolder}/project.json");
                        SEBMSProject Project = null;
                        try
                        {
                             Project = JsonConvert.DeserializeObject<SEBMSProject>(projectBuffer);
                        }catch (Exception E)
                        {
                            cmdarg.assert($"{E.ToString()}\n==============\nProblem loading project file! Check your project.json!");
                            return;
                        }
                        SEBMSAssembler Assembler = new SEBMSAssembler();
                        Assembler.BuildProject(Project,projectFolder, outFile);
                        break;
                    }
                default:
                    Console.WriteLine("Welcome to SEBS / BMPARSE!");
                    Console.WriteLine("Usage: \n[] 's indicate optional arguments!");
                    Console.WriteLine("bmparse <command> <command arguments>\n");
                    Console.WriteLine("");
                    Console.WriteLine("bmparse disassemble <se.bms file> <output folder> [-namefile <file.nam>] [-hintfile <file.hin>]\n");
                    Console.WriteLine("bmparse assemble <project foler> <output file> ");
                    Console.WriteLine("\nIssues?\nhttps://www.github.com/xayrga/bmparse");
                    break;

            }

        }
    }
}
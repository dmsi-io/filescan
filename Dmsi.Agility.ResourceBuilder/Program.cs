using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Dmsi.Agility.Resource.ResourceBuilder
{
    class Program
    {
        private static TextWriter _writer;

        static void Main(string[] args)
        {
            if (args.Length == 2)
            {
                if (File.Exists(args[0]))
                {
                    try
                    {
                        Console.WriteLine("> Generating resx file . . .");
                        
                        ResourceDefinition def = new ResourceDefinition(args[0]);

                        _writer = File.CreateText("agilresx.log");

                        def.LoadSucceeded += def_LoadSucceeded;
                        def.FileProcessed += def_FileProcessed;
                        def.LoadFailed += def_LoadFailed;

                        def.ParseFiles(args[1]);

                        _writer.Flush();
                        _writer.Close();
                        _writer.Dispose();

                        Console.WriteLine("> Done . . .");
                        Console.WriteLine("> Output file: " + Path.GetFullPath(args[1]));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("> " + e.Message);
                    }
                }
                else
                    Console.WriteLine("> '" + args[0] + "' file not found.");
            }
            else if (args.Length == 3)
            {
                if (Directory.Exists(args[1]) &&
                    args[0] == "-d")
                {
                    try
                    {
                        Console.WriteLine("> Generating resx file . . .");

                        ResourceDefinition def = new ResourceDefinition();
                        def.AddDirectory(new DirectoryInfo(args[1]));

                        _writer = File.CreateText("agilresx.log");

                        def.LoadSucceeded += def_LoadSucceeded;
                        def.FileProcessed += def_FileProcessed;
                        def.LoadFailed += def_LoadFailed;
                        
                        def.ParseFiles(args[2]);

                        _writer.Flush();
                        _writer.Close();
                        _writer.Dispose();

                        Console.WriteLine("> Done . . .");
                        Console.WriteLine("> Output file: " + Path.GetFullPath(args[2]));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("> " + e.Message);
                    }
                }
                else if (args[0] != "-d")
                {
                    Console.WriteLine("> Unknown parameter '" + args[0] + "'.");
                }
                else
                    Console.WriteLine("> '" + args[1]  + "' not found.");
            }
            else
                Console.WriteLine("> Wrong number of arguments passed. ex. agilresx [-d <directory>|<inputfile>] <outputfile>");
        }

        private static void def_FileProcessed(object sender, FileProcessedEventArgs e)
        {
            Console.WriteLine($"> {e.Name} - {e.Source}" );
        }

        static void def_LoadFailed(object sender, LoadFailedEventArgs e)
        {
            _writer.WriteLine(e.Name + " - Failed: " + e.Error);
            Console.WriteLine("> " + e.Name + " - Failed: " + e.Error);
        }

        static void def_LoadSucceeded(object sender, LoadSucceededEventArgs e)
        {
            Console.WriteLine("> " + e.Name + " - Successful");
        }
    }
}

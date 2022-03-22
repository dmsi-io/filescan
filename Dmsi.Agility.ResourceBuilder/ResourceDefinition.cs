using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Dmsi.Agility.Resource.ResourceBuilder
{
    [XmlRootAttribute("ResourceDefinition", Namespace = "", IsNullable = false)]
    public class ResourceDefinition
    {
        [XmlArray("Nodes"), XmlArrayItem("ResourceNode", typeof(ResourceNode))]
        public ArrayList Nodes
        {
            get;
            set;
        }

        [XmlIgnoreAttribute]
        public bool IsDirty
        {
            get;
            set;
        }

        [XmlIgnoreAttribute]
        public string FileName
        {
            get;
            private set;
        }

        [XmlIgnoreAttribute]
        public bool Cancelled
        {
            get;
            set;
        }

        [XmlIgnoreAttribute]
        public ProcessType Type
        {
            get;
            set;
        }

        public event EventHandler<LoadFailedEventArgs> LoadFailed;
        public event EventHandler<FileProcessedEventArgs> FileProcessed;
        public event EventHandler<LoadSucceededEventArgs> LoadSucceeded;
        public event EventHandler<MessageGeneratedEventArgs> MessageGenerated;

        private Dictionary<string, object> _nameValuePairs = new Dictionary<string,object>();
        private List<string> Literal = new List<string>();
        
        /// <summary>
        /// Comma seaparated list of desired file extension
        /// </summary>
        public string Extensions
        {
            get{return "cls,w,p,i,t";}
        }

        public ResourceDefinition()
        {
            IsDirty = false;
            FileName = "untitled.agil";
            Nodes = new ArrayList();
        }

        public ResourceDefinition(string fileName)
        {
            Load(fileName);
        }

        public void AddDirectory(DirectoryInfo directoryInfo)
        {
            Nodes.Add(new ResourceNode(Path.GetFileName(directoryInfo.FullName), directoryInfo.FullName));
        }

        public void AddFile(FileInfo fileInfo)
        {
            Nodes.Add(new ResourceNode(Path.GetFileNameWithoutExtension(fileInfo.FullName), fileInfo.FullName));
        }

        private void Load(string fileName)
        {
            try
            {
                IsDirty = false;
                FileName = fileName;
                ObjectXMLSerializer<ResourceDefinition> serializer = new ObjectXMLSerializer<ResourceDefinition>();
                ResourceDefinition def = serializer.Load(fileName);
                Nodes = def.Nodes;
            }
            catch { }
        }

        public static ResourceDefinition LoadFromFile(string fileName)
        {
            ObjectXMLSerializer<ResourceDefinition> serializer = new ObjectXMLSerializer<ResourceDefinition>();
            ResourceDefinition def = serializer.Load(fileName);
            def.FileName = fileName;
            def.IsDirty = false;

            return def;
        }

        public void Save()
        {
            Save(FileName);
        }

        public void Save(string fileName)
        {
            if (fileName == null || fileName == "")
                throw new Exception("Filename must be specified.");

            ObjectXMLSerializer<ResourceDefinition> serializer = new ObjectXMLSerializer<ResourceDefinition>();
            serializer.Save(this, fileName);
            IsDirty = false;
            FileName = Path.GetFileName(fileName);
        }

        private int _numScanned, _numMatches = 0;

        public bool Contains(string name)
        {
            bool found = false;

            foreach (ResourceNode node in Nodes)
            {
                if (node.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            return found;
        }

        public bool ContainsValue(string value)
        {
            bool found = false;

            foreach (ResourceNode node in Nodes)
            {
                if (node.Value.Equals(value, StringComparison.CurrentCultureIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            return found;
        }

        private bool NameValuePairContains(string name)
        {
            bool found = false;

            foreach (KeyValuePair<string, object> entry in _nameValuePairs)
            {
                if(entry.Key.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            return found;
        }

        /// <summary>
        /// Create an resx file using the current FileName value as base. File is created in the current folder.
        /// </summary>
        /// <returns></returns>
        public string ParseFiles()
        {
            return ParseFiles(FileName);
        }

        /// <summary>
        /// Creates/Saves resx using the given path.
        /// </summary>
        /// <param name="outputFileName"></param>
        /// <returns></returns>
        public string ParseFiles(string outputFileName)
        {
            Literal.Clear();

            if (outputFileName == null || outputFileName == "")
                throw new Exception("Filename must be specified.");

            string outputPath = "";
            outputPath = Path.ChangeExtension(outputFileName, "txt");
            _nameValuePairs.Clear();

            // Generate the listing
            foreach (ResourceNode node in Nodes)
            {
                if (Cancelled)
                    break;

                if (node.IsFolder)
                    GetFiles(node.Name, node.Value);
                else
                    CheckForLiteral(node.Name, node.Value);
            }

            MessageGeneratedEventArgs args = new MessageGeneratedEventArgs();
            args.Text = $"SCANNED: {_numScanned} files";
            MessageGenerated?.Invoke(this, args);
            args.Text = $"WITH LITERAL: {_numMatches} files";
            MessageGenerated?.Invoke(this, args);

            if (!Cancelled)
            {
                StreamWriter writer = File.CreateText(outputPath);

                foreach (KeyValuePair<string, object> entry in _nameValuePairs)
                {
                    writer.WriteLine($"{entry.Key}");
                    // List<string> result = (List<string>)entry.Value;
                    
                    // for(int i=0; i<result.Count; i++)
                    // {
                    //    writer.WriteLine($"  {result[i]}");
                    // }
                }

                writer.Flush();
                writer.Close();
                writer.Dispose();

                string file = Path.GetFileNameWithoutExtension(outputPath);
                outputPath = outputPath.Replace(file, file + "-literals");
                writer = File.CreateText(outputPath);

                foreach (string s in Literal)
                {
                    writer.WriteLine(s);
                }

                writer.Flush();
                writer.Close();
                writer.Dispose();
            }

            return Path.GetFullPath(outputPath);
        }

        private void GetFiles(string key, string path)
        {
            foreach (string file in Directory.GetFiles(path))
            {
                string ext = Path.GetExtension(file).Replace(".", "");
                if (!Extensions.Split(',').Contains(ext))
                    continue;

                if (Cancelled)
                    break;

                CheckForLiteral(Path.GetFileNameWithoutExtension(file), file);
            }

            foreach (string dir in Directory.GetDirectories(path))
            {
                GetFiles(key, dir);
            }
        }

        private void CheckForLiteral(string name, string fileName)
        {
            if (!NameValuePairContains(name) && File.Exists(fileName))
            {
                try
                {
                    List<string> result = HasFunctionCallInQuery(fileName);

                    if (result.Count > 0)
                    { 
                        FileProcessedEventArgs args = new FileProcessedEventArgs();
                        args.Name = name;
                        args.Source = fileName;
                        args.Matches = result;
                        FileProcessed?.Invoke(this, args);

                        _nameValuePairs.Add(fileName, result);
                        _numMatches++;
                    }

                    LoadSucceededEventArgs args2 = new LoadSucceededEventArgs();
                    args2.Name = name;
                    args2.Source = fileName;
                    LoadSucceeded?.Invoke(this, args2);

                    _numScanned++;
                }
                catch (Exception e)
                {
                    LoadFailedEventArgs args = new LoadFailedEventArgs();
                    args.Name = name;
                    args.Error = e.Message;
                    args.Source = fileName;
                    LoadFailed?.Invoke(this, args);
                }
            }
            else if (NameValuePairContains(name))
            {
                LoadFailedEventArgs args = new LoadFailedEventArgs();
                args.Name = name;
                args.Error = "Duplicate key.";
                args.Source = fileName;
                LoadFailed?.Invoke(this, args);
            }
            else if (!File.Exists(fileName))
            {
                LoadFailedEventArgs args = new LoadFailedEventArgs();
                args.Name = name;
                args.Error = "File not found.";
                args.Source = fileName;
                LoadFailed?.Invoke(this, args);
            }
        }

        private List<string> HasFunctionCallInQuery(string fileName)
        {
            var name = Path.GetFileName(fileName);
            List<string> result = new List<string>();
            string allText = File.ReadAllText(fileName);

            Regex rx2 = new Regex(@"(FOR FIRST|FOR EACH|FIND FIRST|FIND LAST) +((?!:|NO-LOCK\.|NO-ERROR\.|CASE|RUN|DO|END|FOR|EACH|FIND|FIRST|LAST|VARIABLE|DEFINE|ASSIGN).|\n)*?(INT|INTEGER|DEC|DECIMAL|LOG|LOGICAL) *\((.|\n)*?(\.|:)", RegexOptions.IgnoreCase);
            MatchCollection matches = rx2.Matches(allText);

            if (matches.Count > 0)
            {
                result.Add($"{name}");

                foreach (Match match in matches)
                {
                    var s = match.Value;
                    Literal.Add($"{name},{s}");
                    break;
                }
            }


            return result;
        }
    }

    public class LoadFailedEventArgs: FileNameEventArgs
    {
        public string Error
        {
            get;
            set;
        }
    }

    public class LoadSucceededEventArgs: FileNameEventArgs
    {

    }

    public class FileProcessedEventArgs : FileNameEventArgs
    {
        public List<string> Matches
        {
            get;
            set;
        }
    }

    public class MessageGeneratedEventArgs : EventArgs
    {
        public string Text
        {
            get;
            set;
        }
    }

    public class FileNameEventArgs : EventArgs
    {
        public string Name
        {
            get;
            set;
        }

        public string Source
        {
            get;
            set;
        }
    }
    public enum ProcessType
    {
        All,
        String,
        Numeric
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Resources;
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

        public event EventHandler<LoadFailedEventArgs> LoadFailed;
        public event EventHandler<FileProcessedEventArgs> FileProcessed;
        public event EventHandler<LoadSucceededEventArgs> LoadSucceeded;

        private Dictionary<string, object> _nameValuePairs = new Dictionary<string,object>();
            
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
            IsDirty = false;
            FileName = fileName;
            ObjectXMLSerializer<ResourceDefinition> serializer = new ObjectXMLSerializer<ResourceDefinition>();
            ResourceDefinition def = serializer.Load(fileName);
            Nodes = def.Nodes;
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
        }
        

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

            if (!Cancelled)
            {
                StreamWriter writer = File.CreateText(outputPath);

                foreach (KeyValuePair<string, object> entry in _nameValuePairs)
                {
                    writer.WriteLine($"{entry.Value}");
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
                    FileProcessedEventArgs args = new FileProcessedEventArgs();
                    args.Name = name;
                    args.Source = fileName;

                    FileProcessed?.Invoke(this, args);

                    _nameValuePairs.Add(name, fileName);

                    LoadSucceededEventArgs args2 = new LoadSucceededEventArgs();
                    args2.Name = name;
                    args2.Source = fileName;

                    LoadSucceeded?.Invoke(this, args2);
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
    }

    public class LoadFailedEventArgs: EventArgs
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

        public string Error
        {
            get;
            set;
        }
    }

    public class LoadSucceededEventArgs: EventArgs
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

    public class FileProcessedEventArgs : EventArgs
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
}

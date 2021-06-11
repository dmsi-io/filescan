using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Collections;
using System.IO;
using System.Resources;
using System.Drawing;

namespace Dmsi.Agility.Resource
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
            set;
        }

        private Dictionary<string, object> _nameValuePairs = new Dictionary<string,object>();
            
        /// <summary>
        /// Comma seaparated list of desired image extension
        /// </summary>
        public string Extensions
        {
            get{return "png,ico";}
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

        public void Load(string fileName)
        {
            IsDirty = false;
            FileName = fileName;
            ObjectXMLSerializer<ResourceDefinition> serializer = new ObjectXMLSerializer<ResourceDefinition>();
            ResourceDefinition def = serializer.Load(fileName);
            Nodes = def.Nodes;
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

        public string GenerateResx()
        {
            if (FileName == null || FileName == "")
                throw new Exception("Filename must be specified.");

            string resxPath = "";
            resxPath = Path.ChangeExtension(FileName, "resx");
            
            // Gerate the listing
            foreach (ResourceNode node in Nodes)
            {
                if (node.IsFolder)
                    GetFiles(node.Name, node.Value);
                else
                    AddNode(node.Name, node.Value);
            }


            ResXResourceWriter writer = new ResXResourceWriter(resxPath);

            foreach(KeyValuePair<string, object> entry in _nameValuePairs)
            {
                ResXDataNode resxNode = new ResXDataNode(entry.Key, entry.Value);
                writer.AddResource(resxNode);
            }
            
            writer.Generate();
            writer.Close();
            writer.Dispose();

            return Path.GetFullPath(resxPath);
        }

        private void GetFiles(string key, string path)
        {
            try
            {
                foreach (string file in Directory.GetFiles(path, "*.ico"))
                {
                    AddNode(Path.GetFileNameWithoutExtension(file), file);
                }
            }
            catch { }

            try
            {
                foreach (string file in Directory.GetFiles(path, "*.png"))
                {
                    AddNode(Path.GetFileNameWithoutExtension(file), file);
                }
            }
            catch { }
        }

        private void AddNode(string name, string fileName)
        {
            if (!NameValuePairContains(name) && File.Exists(fileName))
            {
                if (Path.GetExtension(fileName).Trim('.').Equals("ico", StringComparison.CurrentCultureIgnoreCase))
                {
                    try
                    {
                        _nameValuePairs.Add(name, new Icon(fileName));
                    }
                    catch { }
                }
                else if (Path.GetExtension(fileName).Trim('.').Equals("png", StringComparison.CurrentCultureIgnoreCase))
                    _nameValuePairs.Add(name, Image.FromFile(fileName));
            }
        }
    }
}

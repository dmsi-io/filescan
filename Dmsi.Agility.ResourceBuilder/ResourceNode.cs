using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;
using System.Drawing;

namespace Dmsi.Agility.Resource.MatchBuilder
{
    public class ResourceNode
    {
        [Category("Node")]
        [DescriptionAttribute("Name of the resource entry that will serve as the key.")]
        public string Name
        {
            get;
            set;
        }

        [Category("Node")]
        [DescriptionAttribute("The fullpath of the resource entry.")]
        [ReadOnly(true)]
        public string Value
        {
            get;
            set;
        }

        [Category("Node")]
        [DescriptionAttribute("Folder otherwise a file.")]
        [ReadOnly(true)]
        public bool IsFolder
        {
            get;
            set;
        }

        [Category("Node")]
        [DescriptionAttribute("File extension")]
        [ReadOnly(true)]
        public string Extension
        {
            get;
            set;
        }

        private Image _image;

        [Category("Linked Data")]
        [DescriptionAttribute("Image the value is pointed to.")]
        [ReadOnly(true)]
        public Image Image
        {
            get
            {
                try
                {
                    if (_image == null)
                    {
                        try
                        {
                            _image = Image.FromFile(Value);
                        }
                        catch { }
                    }
                    return _image;
                }
                catch
                {
                    return null;
                }
            }
        }

        [Category("Linked Data")]
        [DescriptionAttribute("String value of the link data.")]
        [ReadOnly(true)]
        public string String
        {
            get
            {
                if (Image == null)
                    return Value.ToString();
                else
                    return null;
            }
        }

        [Category("Misc")]
        [DescriptionAttribute("Tag")]
        public string Tag
        {
            get;
            set;
        }

        public ResourceNode()
        {
        }

        public ResourceNode(string name, string value)
        {
            Name = name;
            Value = value;

            if (Directory.Exists(value))
                IsFolder = true;

            if (File.Exists(value))
                Extension = Path.GetFileNameWithoutExtension(value);
        }
    }
}

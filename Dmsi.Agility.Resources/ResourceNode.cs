using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Dmsi.Agility.Resource
{
    public class ResourceNode
    {
        [DescriptionAttribute("Name of the resource entry tat will serve as the key.")]
        public string Name
        {
            get;
            set;
        }

        [DescriptionAttribute("The fullpath of the resource entry.")]
        [ReadOnly(true)]
        public string Value
        {
            get;
            set;
        }

        [DescriptionAttribute("Folder otherwise a file.")]
        [ReadOnly(true)]
        public bool IsFolder
        {
            get;
            set;
        }

        [DescriptionAttribute("File extension")]
        [ReadOnly(true)]
        public string Extension
        {
            get;
            set;
        }
    }
}

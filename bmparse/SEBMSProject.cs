using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bmparse
{


    public class SEBMSProjectCategory {

        public string Name;
        public string[] Sounds;
    };
    public class SEBMSProject : SEBSProject
    {

        public string InitSection;
        public string[] CategoryLogics;
        public string[] CommonLib;
        public SEBMSProjectCategory[] SoundLists;

        public SEBMSProject()
        {
            this.Type = "SEBMS";
        }
    }
}

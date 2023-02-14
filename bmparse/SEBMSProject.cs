using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bmparse
{
    public class SEBMSProject : SEBSProject
    {

        public string InitSection;
        public string[] CategoryLogics;
        public string[] CommonLib;
        public string[,] SoundLists;

        public SEBMSProject()
        {
            this.Type = "SEBMS";
        }
    }
}

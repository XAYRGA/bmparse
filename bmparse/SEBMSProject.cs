using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bmparse
{


    public class SEBMSProjectCategory {

        public string Name;
        public string LogicFile; 
        public string[] Sounds;
    };

    public class SEBMSProject : SEBSProject
    {

        public string InitSection;
        public string[] CommonLib;
        public SEBMSProjectCategory[] Categories;

        public SEBMSProject()
        {
            this.Type = "SEBMS";
        }
    }
}

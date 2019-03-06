using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing
{
    public class Shade
    {
        public string Name { get; set; }
        public Dictionary<int, int> Shades { get; set; }
        public StringBuilder StringBuilder { get; set; }

        public Shade(string Name, int width, int height)
        {
            this.Name = Name;
        }
    }
}

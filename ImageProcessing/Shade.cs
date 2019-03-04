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
            Shades = Initialize(width, height);
        }

        private Dictionary<int, int> Initialize(int width, int height)
        {
            Dictionary<int, int> dictionary = new Dictionary<int, int>();

            const int limit = 256;

            for (int i = 0; i < limit; i++)
            {
                dictionary[i] = 0;
            }

            return dictionary;
        }
    }
}

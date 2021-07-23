using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Celluar_Automaton_CPU
{
    public class Element
    {
        public string texname;
        public int index;
        public Texture2D tex;
        public Element(string texname, int index)
        {
            this.texname = texname;
            this.index = index;
            tex = Game1.contmanager.Load<Texture2D>(texname);
        }
    }
}

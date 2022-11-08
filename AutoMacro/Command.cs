using Dapplo.Windows.Input.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoMacro
{
    public class Command
    {
        private List<VirtualKeyCode> keys;
        private bool enabled;
        private int xpos;
        private int ypos;
        
        public Command(bool enabled, int xpos, int ypos)
        {
            Enabled = enabled;
            XPos = xpos;
            YPos = ypos;
        }

        public Command(bool enabled, List<VirtualKeyCode> keys)
        {
            Enabled = enabled;
            Keys = keys;
        }   

        public bool Enabled 
        {
            get { return enabled; }
            set { enabled = value; }
        }
        public List<VirtualKeyCode> Keys
        {
            get { return keys; }
            set { keys = value; }
        }

        public int XPos 
        {
            get { return xpos; }
            set { xpos = value; }
        }
        public int YPos 
        {
            get { return ypos; }
            set { ypos = value; }

        }
        public string MacroListToString()
        {
            string macro = "";
            if (keys != null)
            {
                foreach (VirtualKeyCode key in keys)
                {
                    if (keys.Last() != key)
                        macro = key.ToString() + " + ";
                    else
                        macro += key.ToString();

                }
                return macro;
            }
            return macro;
        }

    }
}

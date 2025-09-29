using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using game1.Properties;

namespace game1
{
    internal class game1
    {
    }
}
 
class Sample2
{
    public static void Main()
    {
        Form fm = new Form();
        fm.Text = "샘플";

        PictureBox pb = new PictureBox();
        pb.Image = Resources.car;

        fm.Controls.Add(pb);
        Application.Run(fm);
    }
}


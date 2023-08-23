using SimpleBot.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleBot
{
  public partial class SongRequestView : UserControl
  {
    public SongRequestView()
    {
      InitializeComponent();
    }

    private void btnShowYoutubeForm_Click(object sender, EventArgs e)
    {
      SongRequest._yt?.Show();
    }
  }
}

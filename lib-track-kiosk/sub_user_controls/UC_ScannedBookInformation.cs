using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace lib_track_kiosk.user_control_forms
{
    public partial class UC_ScannedBookInformation : UserControl
    {
        private readonly int _bookId;
        private readonly string _bookNumber;

        public UC_ScannedBookInformation(int bookId, string bookNumber)
        {
            InitializeComponent();
            _bookId = bookId;
            _bookNumber = bookNumber;
        }
    }
}

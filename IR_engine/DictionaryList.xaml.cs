using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace IR_engine
{
    /// <summary>
    /// Interaction logic for DictionaryList.xaml
    /// </summary>
    public partial class DictionaryList : Window
    {
        public DictionaryList(Dictionary<string, indexTerm> list)
        {
            InitializeComponent();
            StringBuilder sb = new StringBuilder();
            foreach(KeyValuePair<string, indexTerm> entery in list)
            {
                sb.Append(entery.Value.ToString());
                sb.Append("\n");
            }
            termsList.Text = sb.ToString();

        }


    }
}

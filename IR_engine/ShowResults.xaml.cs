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
    /// Interaction logic for ShowResults.xaml
    /// </summary>
    public partial class ShowResults : Window
    {
        Dictionary<int, string> elements;
        public ShowResults(Dictionary<int, List<string>> results, Dictionary<int, string> elements)
        {
            InitializeComponent();
            this.elements = elements;
            if(results.Count == 0)
            {
                System.Windows.MessageBox.Show("No results found!");
                return;
            }
            foreach (KeyValuePair<int, List<string>> queryResult in results)
            {
                TabItem t = new TabItem();
                StackPanel sp = new StackPanel();
                ScrollViewer sv = new ScrollViewer();
                sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                if(queryResult.Key == 0)
                    t.Header = "Written Q";
                else
                    t.Header = "Q: " + queryResult.Key;
                foreach (string doc in queryResult.Value)
                {
                    Label lb = new Label();
                    lb.Content = String.Format("Doc:{0,20}\t\t{1,20}", Searcher.Index2Doc[int.Parse(doc)], "Click Here for doc info");
                    //lb.Content = "Doc: " + Searcher.Index2Doc[int.Parse(doc)] +"\t\t\t\t\t\t Click Here for doc info";
                    lb.Name = "d"+ doc;
                    lb.MouseDown += click;
                    sp.Children.Add(lb);
                }
                sv.Content = sp;
                t.Content = sv;
                resultView.Items.Add(t);
            }
        }

        private void click(object sender, RoutedEventArgs e)
        {
            Label l = (Label)sender;
            string content = (string)l.Content;
            if (!content.Contains("Click Here for doc info")) return;
            content = content.Replace("Click Here for doc info", "");
            string doc = l.Name.TrimStart(new char[] { 'd' });
            if (elements.ContainsKey(int.Parse(doc)))
                content += "Elements: "+elements[int.Parse(doc)];
            else
                content += "---No Elements in this document---";
            l.Content = content;
        }
    }
}

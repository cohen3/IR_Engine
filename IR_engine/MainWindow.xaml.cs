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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls;

namespace IR_engine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string path = "";
        string IndexPath = "";
        string modelPath = "";
        string qryPath = "";
        string outFilePath = "";
        static int filenum = 1;
        Model m;
        Searcher search;
        bool isDictionaryStemmed;
        bool semantics;

        public MainWindow()
        {
            InitializeComponent();
            semantics = false;
            m = new Model();
            DataContext = m;
            m.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                if(e.PropertyName.Equals("done"))
                {
                    string a = null;
                    var arrayOfAllKeys = ReadFile.Langs.Keys.ToArray();
                    foreach (string x in arrayOfAllKeys)
                    {
                        a = Model.cleanAll(x);
                        if (!a.Equals("") && !a.Equals(" "))
                            Language.Dispatcher.BeginInvoke((Action)(() => Language.Items.Add(x)));
                    }
                }
                else
                    progBar.Dispatcher.BeginInvoke((Action)(() => progBar.Value = (m.Progress)));
            };
            path = "";
            IndexPath = "";
            isDictionaryStemmed = false;
            test.Content = "Welcome to BarvazBarvazGo!\nPlease make sure you have internet connection.";
            model_CB.SelectedIndex = 0;
            string[] models = Directory.GetFiles(@"MODELS\", "*.bin", SearchOption.AllDirectories);
            foreach(string model in models)
            {
                model_CB.Items.Add(System.IO.Path.GetFileNameWithoutExtension(model));
            }
            model_CB.Items.Add("Costumize");
            //how to fill the scrollview with checkboxes

            //StackPanel s = new StackPanel();
            //for(int i = 0; i < 100; i++)
            //{
            //    System.Windows.Controls.CheckBox c = new System.Windows.Controls.CheckBox();
            //    c.Content = i;
            //    s.Children.Add(c);
            //}
            //scrollLocations.Content = s;   
        }



        /// <summary>
        /// mouse enter event for the RUN button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Run_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Run.Width = 110;
            Run.Height = 110;
            Run.Foreground = new SolidColorBrush(Colors.Red);
            Run.FontSize = 38;
        }

        /// <summary>
        /// mouse leave event for the RUN button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Run_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Run.Width = 100;
            Run.Height = 100;
            Run.Foreground = new SolidColorBrush(Colors.Black);
            Run.FontSize = 36;
        }
        private void load_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Load_Index.Width = 100;
            Load_Index.Height = 100;
            Load_Index.Foreground = new SolidColorBrush(Colors.Black);
            Load_Index.FontSize = 12;
        }
        private void load_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Load_Index.Width = 110;
            Load_Index.Height = 110;
            Load_Index.Foreground = new SolidColorBrush(Colors.Red);
            Load_Index.FontSize = 13;
        }

        private void Search_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SearchBtn.Width = 100;
            SearchBtn.Height = 100;
            SearchBtn.Foreground = new SolidColorBrush(Colors.Red);
            SearchBtn.FontSize = 22;
        }

        private void SearchBtn_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SearchBtn.Width = 90;
            SearchBtn.Height = 90;
            SearchBtn.Foreground = new SolidColorBrush(Colors.Black);
            SearchBtn.FontSize = 20;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            if (Model.isWorking)
            {
                test.Content = "Engine is working, please wait for a completion message to pop up";
                return;
            }
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog()) { if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    path = dialog.SelectedPath;
                }
            }
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            if(Model.isWorking)
            {
                test.Content = "Engine is working, please wait for a completion message to pop up";
                return;
            }
            test.Content = "";
            if (path.Equals(""))
            {
                path = pathText.Text;
            }
            if (IndexPath.Equals(""))
            {
                IndexPath = IndexPathText.Text;
            }
            if (!path.Equals(""))
            {
                if (!Directory.Exists(IndexPath)) { test.Content = "Index path not a directory"; IndexPath = ""; path = ""; }
                else
                {
                    progBar.Value = 0;
                    test.Content = "Working, please wait...";
                    isDictionaryStemmed = stem.IsChecked.Value;
                    m.IndexPath1 = IndexPath;
                    m.Path = path;
                    m.toStem = stem.IsChecked.Value;
                    Task.Factory.StartNew(()=>m.index());
                }
            }
            else
            {
                test.Content = "path not provided.";
                IndexPath = ""; path = "";
            }
        }

        private void showDic_Click(object sender, RoutedEventArgs e)
        {
            if (m == null)
            {
                test.Content = "You need to run the engine first";
                return;
            }
            if (Model.isWorking)
            {
                test.Content = "Engine is working, please wait for a completion message to pop up";
                return;
            }
            Dictionary<string, indexTerm> index = m.getDictionary();
            if (index.Count == 0)
            {
                test.Content = "No index was loaded.\nPlease load the index first";
                return;
            }
            if (isDictionaryStemmed != stem.IsChecked.Value)
            {
                string s = isDictionaryStemmed ? "is" : "isn't";
                test.Content = "The current loaded index " + s + " stemmed.\nin order to load the desired index,\nplease load it first by pressing the 'load index' button.";
                return;
            }
            Window dictionary;
            dictionary = new DictionaryList(index);
            dictionary.Show();
        }

        private void BrowseIndex_Click(object sender, RoutedEventArgs e)
        {
            if (Model.isWorking)
            {
                test.Content = "Engine is working, please wait for a completion message to pop up";
                return;
            }
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    IndexPath = dialog.SelectedPath;
                }
            }
        }

        private void Load_Index_Click(object sender, RoutedEventArgs e)
        {
            test.Content = "this may take a few minutes";
            if (Model.isWorking)
            {
                test.Content = "Engine is working, please wait for a completion message to pop up";
                return;
            }
            if((IndexPathText.Text == "" && IndexPath.Equals("")))
            {
                test.Content = "No index path provided.\nPlease input a path to the index.txt file";
                return;
            }
            string ipt=null;
            if (stem.IsChecked.Value) {ipt = IndexPath.Equals("")? IndexPathText.Text + "\\EnableStem" : IndexPath + "\\EnableStem"; }
            else {ipt = IndexPath.Equals("") ? IndexPathText.Text + "\\DisableStem" : IndexPath + "\\DisableStem"; }
            if (!Directory.Exists(ipt) || !File.Exists(ipt+"\\index.txt"))
                test.Content = "No Index in path";
            else
            {
                isDictionaryStemmed = stem.IsChecked.Value;
                m.load_index(ipt);
                m.loadElements(ipt);
                StackPanel s = new StackPanel();
                foreach(string location in m.load_location(ipt))
                {
                    System.Windows.Controls.CheckBox c = new System.Windows.Controls.CheckBox();
                    string l = Model.cleanAll(location);
                    if (l.Equals("") || l.Equals("for")) continue;
                    c.Content = l;
                    s.Children.Add(c);
                }
                scrollLocations.Content = s;
                string a = null;
                var arrayOfAllKeys = m.load_langs(ipt);
                if (arrayOfAllKeys == null) return;
                foreach (string x in arrayOfAllKeys)
                {
                    a = Model.cleanAll(x);
                    if (!a.Equals("") && !a.Equals(" "))
                        Language.Items.Add(a);
                }
                test.Content = "Index was succesfully loaded from the file:\n" + ipt + "\\index.txt";
            }
        }

        private void reset_Click(object sender, RoutedEventArgs e)
        {
            if(m != null)
                m.Memorydump();
            if (IndexPath.Equals(""))
            {
                IndexPath = IndexPathText.Text;
            }
            if(IndexPath.Equals(""))
            {
                test.Content = "Memory cleared but posting and index directories\ndid not because there is no path to the directory.\nPlease insert an index directory path for the posting\nfiles to be cleaned.";
                return;
            }
            if (Directory.Exists(IndexPath + "\\DisableStem"))
            {
                Directory.Delete(IndexPath + "\\DisableStem", true);
            }
            if (Directory.Exists(IndexPath + "\\EnableStem"))
            {
                Directory.Delete(IndexPath + "\\EnableStem", true);
            }
            //File.Delete(IndexPath + "\\city_dictionary.txt");
            //File.Delete(IndexPath + "\\documents.txt");
        }

        private void browseQry_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Title = "Browse Query Files";
            openFileDialog1.InitialDirectory = @"C:\";
            openFileDialog1.Filter = "Query txt files (*.txt)|*.txt";
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;
            DialogResult result = openFileDialog1.ShowDialog();
            qryPath = openFileDialog1.FileName;
            qryTextBox.Text = qryPath;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            semantics = semanticsCheckBox.IsChecked.Value;
            if (semantics)
                model_CB.IsEnabled = true;
            else
            {
                model_CB.IsEnabled = false;
                createModel.IsEnabled = false;
            }
        }

        private void model_CB_DropDownClosed(object sender, EventArgs e)
        {
            test.Content = model_CB.SelectedValue;
            if (model_CB.SelectedValue.Equals("Costumize"))
                createModel.IsEnabled = true;
            else
                createModel.IsEnabled = false;
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Model.isWorking)
            {
                test.Content = "Engine is working, please wait for a completion message to pop up";
                return;
            }
            if(m.getDictionary().Count == 0)
            {
                test.Content = "No index was loaded, please load an index in order to search";
                return;
            }
            if(outFile.Text.Equals(""))
            {
                test.Content = "No result output file selected.\nplease press the browse button and choose where to save it.";
                return;
            }
            if(!Directory.Exists(System.IO.Path.GetDirectoryName(outFile.Text)))
            {
                test.Content = "Directory does not exist.\nplease press the browse button and choose where to save it.";
                return;
            }
            string oFile = outFile.Text;
            progBar.Value = 0;
            string ipt = null;
            if (stem.IsChecked.Value) { ipt = IndexPath.Equals("") ? IndexPathText.Text + "\\EnableStem" : IndexPath + "\\EnableStem"; }
            else { ipt = IndexPath.Equals("") ? IndexPathText.Text + "\\DisableStem" : IndexPath + "\\DisableStem"; }
            if (!Directory.Exists(ipt) || !File.Exists(ipt + "\\index.txt"))
                test.Content = "No Index in path";
            if (semanticsCheckBox.IsChecked.Value)
            {
                if(!model_CB.SelectedValue.Equals("Costumize"))
                    modelPath = @"MODELS\"+model_CB.SelectedValue + ".bin";
            }
            if (textQuery.Text.Equals("") && qryTextBox.Text.Equals(""))
            {
                test.Content = "No query provided";
                return;
            }
            HashSet<string> locations = new HashSet<string>();
            bool allLocs = true;
            StackPanel s = (StackPanel)scrollLocations.Content;
            foreach (System.Windows.Controls.CheckBox c in s.Children)
            {
                if (c.IsChecked.Value)
                    locations.Add(c.Content.ToString());
            }
            allLocs = locations.Count == 0 ? true: false;
            string query = "";
            if(!textQuery.Text.Equals(""))
            {
                using (StreamWriter sr1 = new StreamWriter("qryTemp.txt"))
                {
                    sr1.WriteLine("<top>");
                    sr1.WriteLine("<num> Number: 0 ");
                    sr1.WriteLine("<title> " + textQuery.Text);
                    sr1.WriteLine("<desc>");
                    sr1.WriteLine("<narr>");
                    sr1.WriteLine("</top>");
                }
                query = "qryTemp.txt";
            }
            if(!qryTextBox.Text.Equals(""))
                query = qryTextBox.Text;
            if (!textQuery.Text.Equals("") && !qryTextBox.Text.Equals(""))
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("We detected that we entered a query and a file.\nWould you like to search the query anyway?\n(pressing no will search the file)",
                    "Duplicated input",
                    MessageBoxButton.YesNo);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        {
                            query = "qryTemp.txt";
                            break;
                        }
                    case MessageBoxResult.No:
                        query = qryTextBox.Text;
                        break;
                }
            }
            progBar.Value = 0;
            Dictionary<int, List<string>> RelevantDocs = null;
            search = new Searcher(ipt, query, semanticsCheckBox.IsChecked.Value, modelPath, stem.IsChecked.Value, oFile);
            search.PropertyChanged += delegate (object sender1, PropertyChangedEventArgs e1)
            {
                if(e1.PropertyName.Equals("done"))
                {
                    if (query.Equals("qryTemp.txt"))
                        File.Delete("qryTemp.txt");
                    test.Dispatcher.BeginInvoke((Action)(() => test.Content = "Done!"));
                    if (search.rdocs.Count != 0)
                    {
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ShowResults sr = new ShowResults(search.rdocs, m.elements);
                            sr.Show();
                        }));
                    }
                    else
                        System.Windows.MessageBox.Show("No Results", "no results");
                    return;
                }
                progBar.Dispatcher.BeginInvoke((Action)(() => progBar.Value = (search.Progress) * 100));
            };
            //Dictionary<int, List<string>> RelevantDocs = search.Search(locations);

            Task t = new Task(() =>
            {
               RelevantDocs = search.Search(locations, allLocs);
            });
            t.Start();
            test.Content = "Retreiving Data...";
            //t.Wait();
            //how to read from the checkboxes from the scrollview

            //StackPanel s = (StackPanel)scrollLocations.Content;
            //foreach(System.Windows.Controls.CheckBox c in s.Children)
            //{
            //    if (c.IsChecked.Value)
            //        Console.WriteLine("Location: " + c.Content);
            //}
        }

        private void createModel_Click(object sender, RoutedEventArgs e)
        {
            CreateModel cm = new CreateModel();
            cm.Show();
        }

        private void refreshbtn_Click(object sender, RoutedEventArgs e)
        {
            string a = null;
            var arrayOfAllKeys = ReadFile.Langs.Keys.ToArray();
            if (arrayOfAllKeys == null) return;
            foreach (string x in arrayOfAllKeys)
            {
                a = Model.cleanAll(x);
                if (!a.Equals("") && !a.Equals(" "))
                    Language.Items.Add(x);
            }
            var locs = Model.locationsList;
            if (locs == null) return;
            StackPanel s = new StackPanel();
            foreach (string location in locs)
            {
                System.Windows.Controls.CheckBox c = new System.Windows.Controls.CheckBox();
                string l = Model.cleanAll(location);
                if (l.Equals("") || l.Equals("for")) continue;
                c.Content = l;
                s.Children.Add(c);
            }
            scrollLocations.Content = s;
        }

        private void outFileBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (Model.isWorking)
            {
                test.Content = "Engine is working, please wait for a completion message to pop up";
                return;
            }
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string f = "\\res";
                    outFilePath = dialog.SelectedPath;
                    while (File.Exists(outFilePath + f + filenum + ".txt")) filenum++;
                    outFilePath = outFilePath + f + filenum + ".txt";
                    outFile.Text = outFilePath;
                }
            }
        }
    }
}

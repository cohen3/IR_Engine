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
using Word2Vec.Net;

namespace IR_engine
{
    /// <summary>
    /// Interaction logic for CreateModel.xaml
    /// </summary>
    public partial class CreateModel : Window
    {
        Task t;
        public CreateModel()
        {
            InitializeComponent();
            Label l = new Label();
            l.Content = "- Please note that training a model takes a much longer than the engine.\n"
                + "- a reminder that we included a pre-trained models, you may use them.\n"
                + "- Default values are recommended, it may take time even so.\n"
                + "- If you wish to train a model note that efficient training requires values that\n"
                + "  may cause the training proccess to take too long";
            gb.Content = l;
            int cores = Environment.ProcessorCount;
            for (int i = 0; i < cores-1; i++)
                threadsCB.Items.Add(i + 1);
            modelCB.Items.Add("SkipGram");
            modelCB.Items.Add("CBOW");
            modelCB.SelectedIndex = 1;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(t != null && !t.IsCompleted)
            {
                warnings.Content = "Training in progress, please wait for it to finish";
                return;
            }
            Word2Vec.Net.Word2Vec word2Vec = null;
            try
            {
                word2Vec = Word2VecBuilder.Create()
                                .WithTrainFile(trainSet.Text)// Use text data to train the model;
                                .WithOutputFile(@"MODELS\"+outputName.Text+".bin")//Use to save the resulting word vectors / word clusters
                                .WithSize(int.Parse(vectorSizeTB.Text))//Set size of word vectors; default is 100
                                //.WithSaveVocubFile()//The vocabulary will be saved to <file>
                                .WithDebug(2)//Set the debug mode (default = 2 = more info during training)
                                .WithBinary(1)//Save the resulting vectors in binary moded; default is 0 (off)
                                .WithCBow(modelCB.SelectedIndex)//Use the continuous bag of words model; default is 1 (use 0 for skip-gram model)
                                .WithAlpha(float.Parse(learningRateTB.Text))//Set the starting learning rate; default is 0.025 for skip-gram and 0.05 for CBOW
                                .WithWindow(7)//Set max skip length between words; default is 5
                                .WithSample((float)1e-3)//Set threshold for occurrence of words. Those that appear with higher frequency in the training data twill be randomly down-sampled; default is 1e-3, useful range is (0, 1e-5)
                                .WithHs(0)//Use Hierarchical Softmax; default is 0 (not used)
                                .WithNegative(int.Parse(negtb.Text))//Number of negative examples; default is 5, common values are 3 - 10 (0 = not used)
                                .WithThreads(int.Parse(threadsCB.Text))//Use <int> threads (default 12)
                                .WithIter(int.Parse(iterTB.Text))//Run more training iterations (default 5)
                                .WithMinCount(30)//This will discard words that appear less than <int> times; default is 5
                                .WithClasses(0)//Output word classes rather than word vectors; default number of classes is 0 (vectors are written)
                                .Build();
            }
            catch(Exception e1)
            {
                warnings.Content = "Wrong input";
                return;
            }
            warnings.Content = "Training started.";
            t = new Task(word2Vec.TrainModel);
            t.Start();
        }
    }
}

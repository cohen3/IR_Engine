using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IR_engine
{
    /// <summary>
    /// this class implements a "document" object that separetes between the different fields of the document
    /// </summary>
    public class document
    {
        private string doc;
        private string docID;
        private string docDate;
        private string docHead;
        public double maxTF;
        public double uniqueTerms;
        private string docCity;
        private double docSize;
        private int DocIndex;

        /// <summary>
        /// this is the empty (default) constructor
        /// </summary>
        public document()
        {
            this.doc = "";
            this.docID = "";
            this.docDate = "";
            this.docHead = "";
            maxTF = 0;
            uniqueTerms = 0;
            this.docCity = "";
        }
        /// <summary>
        /// this is another constructor that takes all the fields
        /// </summary>
        /// <param name="doc"> the text part of the document</param>
        /// <param name="docId">the ID of the document</param>
        /// <param name="docDate"> the date that the document was created</param>
        /// <param name="docHead">the header of the document</param>
        public document(string doc, string docId, string docDate, string docHead, string docCity)
        {
            this.doc = doc;
            this.docID = docId;
            this.docDate = docDate;
            this.docHead = docHead;
            this.docCity = docCity;
            maxTF = 0;
            uniqueTerms = 0;
        }

        public string Doc
        {
            get { return doc; }
            set { doc = value; }
        }
        public string DocID
        {
            get { return docID; }
            set { docID = value; }
        }
        public string Docdate
        {
            get { return docDate; }
            set { docDate = value; }
        }
        public string DocHead
        {
            get { return docHead; }
            set { docHead = value; }
        }
        public string DocCity
        {
            get { return docCity; }
            set { docCity = value; }
        }

        public double DocSize { get => docSize; set => docSize = value; }
        public int DocIndex1 { get => DocIndex; set => DocIndex = value; }

        public override string ToString()
        {
            return docID +"\t"+ docDate +"\t"+ maxTF+"\t"+uniqueTerms+"\t"+docCity+"\t"+docSize;
        }
    }
}

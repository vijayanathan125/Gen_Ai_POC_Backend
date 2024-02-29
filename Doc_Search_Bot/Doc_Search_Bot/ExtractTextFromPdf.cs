
//using iTextSharp.text.pdf;
//using iTextSharp.text.pdf.parser;

//using System.Text;
//namespace Doc_Search_Bot
//{
//    public class ExtractTextFromPdf
//    {
//        public string Extract_Text_FromPdf(Stream pdfStream)
//        {
//            StringBuilder text = new StringBuilder();

//            try
//            {
//                //pdfStream.Position = 1;
//                //using (var pdfReader = new iTextSharp.text.pdf.PdfReader(pdfStream))
//                using (PdfReader pdfReader = new PdfReader(pdfStream))
//                {
//                    for (int i = 1; i <= pdfReader.NumberOfPages; i++)
//                    {
//                        var strategy = new iTextSharp.text.pdf.parser.SimpleTextExtractionStrategy();
//                        var currentText = iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(pdfReader, i, strategy);
//                        text.Append(currentText);
//                    }
//                }

//                // Reset stream position
//                //pdfStream.Seek(0, SeekOrigin.Begin);
//                return text.ToString();
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error extracting text from PDF: {ex.Message}");
//                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
//                throw;
//            }
//        }
//    }
//}

using PdfSharp.Pdf;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.Content.Objects;
using PdfSharp.Pdf.IO;

using System.IO;
using System.Text;

namespace Doc_Search_Bot
{
    public class ExtractTextFromPdf
    {
        public string Extract_Text_FromPdf(Stream pdfStream)
        {
            StringBuilder text = new StringBuilder();

            try
            {
                // Load the PDF document
                PdfDocument pdfDocument = PdfReader.Open(pdfStream, PdfDocumentOpenMode.Import);

                // Iterate through pages
                foreach (PdfPage page in pdfDocument.Pages)
                {
                    // Extract text from the page
                    CObject content = ContentReader.ReadContent(page);
                    if (content is CArray array)
                    {
                        text.Append(ExtractTextFromContentArray(array));

                    }
                }

                return text.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting text from PDF: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        private string ExtractTextFromContentArray(CArray array)
        {
            StringBuilder text = new StringBuilder();
            double textX = 0;
            double textY = 0;

            foreach (var item in array)
            {
                if (item is CString cString)
                {
                    // Log the extracted string
                    Console.WriteLine($"Extracted String: {cString.Value}");

                    text.Append(cString.Value);
                }
                else if (item is COperator cOperator)
                {
                    // Log the operator
                    Console.WriteLine($"Operator: {cOperator.OpCode}");

                    // Handle Td operator for text positioning
                    if (cOperator.OpCode.OpCodeName == OpCodeName.Td)
                    {
                        // Td operator adjusts the text position
                        if (cOperator.Operands.Count >= 2 && cOperator.Operands[0] is CReal realX && cOperator.Operands[1] is CReal realY)
                        {
                            textX += realX.Value;
                            textY += realY.Value;
                        }
                    }
                    // Handle TD operator (shorthand for setting leading and using Td)
                    else if (cOperator.OpCode.OpCodeName == OpCodeName.TD)
                    {
                        // TD operator adjusts the text position with leading
                        if (cOperator.Operands.Count >= 2 && cOperator.Operands[0] is CReal realX && cOperator.Operands[1] is CReal realY)
                        {
                            textX += realX.Value;
                            textY += realY.Value;
                        }
                    }
                }
                else if (item is CArray nestedArray)
                {
                    text.Append(ExtractTextFromContentArray(nestedArray));
                }
            }

            return text.ToString();
        }

    }
}



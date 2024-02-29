
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;
using System.Text;
using Xceed.Words.NET;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExcelCell = DocumentFormat.OpenXml.Spreadsheet.Cell;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Doc_Search_Bot;
using Python.Runtime;
using System.Diagnostics;




[Route("api/[controller]")]
[ApiController]
public class ChatController : ControllerBase
{
    private readonly CloudBlobContainer _blobContainer;
    private readonly string _openAiApiKey;
    private readonly ILogger<ChatController> _logger;
    private readonly ExtractTextFromPdf _pdfExtractor;

    public ChatController(ILogger<ChatController> logger, ExtractTextFromPdf pdfExtractor)
    {
        _logger = logger;
        _pdfExtractor = pdfExtractor;
        

        // Azure Storage Account Connection String
        var connectionString = "DefaultEndpointsProtocol=https;AccountName=genaipocstoragevj;AccountKey=de21VUFgKmt8u0ULDqX+D5EzjLwSw4NI6QLDVtPy1Uj82qNmiqvCnu68swIjSG0ySH8F4XPGkPgC+AStAICusw==;EndpointSuffix=core.windows.net";

        // OpenAI API Key
        _openAiApiKey = "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";  // Replace with your actual OpenAI API key

        var storageAccount = CloudStorageAccount.Parse(connectionString);
        var blobClient = storageAccount.CreateCloudBlobClient();
        _blobContainer = blobClient.GetContainerReference("genaipocdocuments1");
    }




    [HttpPost("ask")]
    public async Task<ActionResult<string>> AskQuestion([FromBody] string userQuery)
    {
        try
        {
            _logger.LogInformation($"Received user query: {userQuery}");

            string content = await SearchBlobStorage();

            if (!string.IsNullOrEmpty(content))
            {
                string response = await GenerateOpenAIResponse(userQuery, content);

                _logger.LogInformation($"OpenAI response: {response}");

                // Extracting only the assistant's content
                string assistantContent = ExtractContentFromResponse(response);

                _logger.LogInformation($"Assistant content: {assistantContent}");
                Console.WriteLine( assistantContent );

                // Return the content in the response
                return Ok(new { Content = assistantContent });
            }

            _logger.LogWarning("Content not found");
            return Ok("Content not found");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Internal server error: {ex.Message}");
            Console.WriteLine($"Error processing PDF file: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            return StatusCode(500, $"Internal server error: {ex.Message}");
            Process process = Process.GetCurrentProcess();
            Console.WriteLine($"Memory Usage: {process.WorkingSet64} bytes");

        }
    }

    private string ExtractContentFromResponse(string jsonResponse)
    {
        dynamic responseObject = JsonConvert.DeserializeObject(jsonResponse);
        string assistantContent = responseObject["choices"][0]["message"]["content"];
        return assistantContent;
    }



    private async Task<string> SearchBlobStorage()
    {
        var blobItems = await ListBlobItemsAsync();
        StringBuilder combinedContent = new StringBuilder();

        foreach (var blobItem in blobItems)
        {
            if (blobItem is CloudBlockBlob blockBlob)
            {
                var content = await DownloadBlobContentAsync(blockBlob);

                // Log the content for debugging
                _logger.LogInformation($"Content for {blockBlob.Name}: {content}");

                // Append the content to the combinedContent
                combinedContent.Append(content);

                // Add two lines of hyphens as separation
                combinedContent.AppendLine();
                combinedContent.AppendLine("----------------------------------------");
                combinedContent.AppendLine();
            }
        }

        string mergedContent = combinedContent.ToString();

        if (!string.IsNullOrEmpty(mergedContent))
        {
            _logger.LogInformation("Merged content found in storage");
            _logger.LogInformation(mergedContent);
            return mergedContent;
        }

        _logger.LogInformation("No content found in storage");
        return null; // Content not found
    }



    private async Task<IEnumerable<IListBlobItem>> ListBlobItemsAsync()
    {
        var blobItems = new List<IListBlobItem>();
        BlobContinuationToken continuationToken = null;

        do
        {
            var results = await _blobContainer.ListBlobsSegmentedAsync(continuationToken);
            continuationToken = results.ContinuationToken;
            blobItems.AddRange(results.Results);
        } while (continuationToken != null);

        return blobItems;
    }


    private async Task<string> DownloadBlobContentAsync(CloudBlockBlob blob)
    {
        using (var memoryStream = new MemoryStream())
        {
            await blob.DownloadToStreamAsync(memoryStream);
            //****
            string extension = Path.GetExtension(blob.Name).ToLower();
            System.Console.WriteLine(blob.Name + " " + extension);
            
            string text;

            if (extension == ".docx" || extension == ".doc")
            {
                text = ExtractTextFromDocx(memoryStream);
            }
            else if (extension == ".pptx" || extension == ".ppt")
            {
                text = ConvertPptxToDocxAndExtractText(memoryStream);
            }
            else if (extension == ".xls" || extension == ".xlsx")
            {
                text = ExtractTextFromXlsx(memoryStream);
            }
            else if (extension == ".pdf")
            {
              text=_pdfExtractor.Extract_Text_FromPdf(memoryStream);

            }
            else
            {
                text = "Unsupported file type";
            }

            memoryStream.Seek(0, SeekOrigin.Begin);

            return text;
        }
    }

    private string ExtractTextFromDocx(Stream stream)
    {
        // Logic to extract text from .docx file using external library (e.g., Aspose.Words)
        using (var document = DocX.Load(stream))
        {
            return document.Text;
        }
    }




    private string ConvertPptxToDocxAndExtractText(Stream pptxStream)
    {
        using (MemoryStream docxStream = new MemoryStream())
        {
            // Create a Word document
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(docxStream, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                mainPart.Document.Body = new Body(); // Initialize the Body element

                // Iterate through slides in the PowerPoint presentation
                using (PresentationDocument presentationDocument = PresentationDocument.Open(pptxStream, false))
                {
                    foreach (SlidePart slidePart in presentationDocument.PresentationPart.SlideParts)
                    {
                        // Create a new paragraph for each slide
                        DocumentFormat.OpenXml.Wordprocessing.Paragraph slideParagraph = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();

                        // Extract text from each text element in the slide
                        foreach (var textElement in slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
                        {
                            // Append the text to the slide's paragraph
                            slideParagraph.Append(textElement.CloneNode(true));
                        }

                        // Check if mainPart.Document.Body is null
                        if (mainPart.Document.Body != null)
                        {
                            // Add the slide's paragraph to the Word document
                            mainPart.Document.Body.Append(slideParagraph.CloneNode(true));
                        }
                        else
                        {
                            // Handle the case where Body is null (optional based on your application logic)
                            Console.WriteLine("Error: Body is null");
                        }
                    }
                }
            }

            // Extract text from the Word document
            return ExtractTextFromWord(docxStream);
        }
    }

    private string ExtractTextFromWord(Stream wordStream)
    {
        StringBuilder text = new StringBuilder();

        using (WordprocessingDocument wordDocument = WordprocessingDocument.Open(wordStream, false))
        {
            // Iterate through paragraphs in the Word document
            foreach (var paragraph in wordDocument.MainDocumentPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
            {
                text.AppendLine(paragraph.InnerText);
            }
        }

        return text.ToString();
    }


    private string ExtractTextFromXlsx(Stream stream)
    {
        var text = new StringBuilder();

        using (var spreadsheetDocument = SpreadsheetDocument.Open(stream, false))
        {
            var workbookPart = spreadsheetDocument.WorkbookPart;
            var sharedStringTablePart = workbookPart.SharedStringTablePart;

            foreach (var sheet in workbookPart.Workbook.Descendants<Sheet>())
            {
                var worksheetPart = workbookPart.GetPartById(sheet.Id) as WorksheetPart;

                if (worksheetPart != null)
                {
                    var sharedStringTable = sharedStringTablePart.SharedStringTable;

                    foreach (var cell in worksheetPart.Worksheet.Descendants<ExcelCell>())
                    {
                        if (cell.DataType != null && cell.DataType == CellValues.SharedString)
                        {
                            int sharedStringIndex = int.Parse(cell.InnerText);
                            text.Append(sharedStringTable.ElementAt(sharedStringIndex).InnerText).Append('\t');
                        }
                        else
                        {
                            text.Append(cell.InnerText).Append('\t');
                        }
                    }

                    text.AppendLine();
                }
            }
        }

        return text.ToString();
    }
    //private string ExtractTextFromTxt(Stream stream)
    //{
    //    stream.Seek(0, SeekOrigin.Begin); // Set the stream position to the beginning

    //    using (var reader = new StreamReader(stream, Encoding.UTF8))
    //    {
    //        return reader.ReadToEnd();
    //    }
    //}

    //private string ConvertPdfToTextAndDoc(Stream pdfStream)
    //{
    //    try
    //    {
    //        // Step 1: Extract text from PDF
    //        //string pdfText = ExtractTextFromPdf(pdfStream);

    //        // Step 2: Convert text to Word document
    //        //Stream docStream = ConvertTextToDoc(pdfText);

    //        // Step 3: Extract text from the Word document
    //        //string extractedText = ExtractTextFromDoc(docStream);

    //        return extractedText;
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error in conversion: {ex.Message}");
    //        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
    //        throw new ApplicationException("Error in document conversion", ex);
    //    }
    //}

    //private string ExtractTextFromPdf(Stream pdfStream)
    //{
    //    StringBuilder text = new StringBuilder();

    //    try
    //    {
    //        using (var pdfReader = new iTextSharp.text.pdf.PdfReader(pdfStream))
    //        {
    //            for (int i = 1; i <= pdfReader.NumberOfPages; i++)
    //            {
    //                var strategy = new iTextSharp.text.pdf.parser.SimpleTextExtractionStrategy();
    //                var currentText = iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(pdfReader, i, strategy);
    //                text.Append(currentText);
    //            }
    //        }

    //        // Reset stream position
    //        pdfStream.Seek(0, SeekOrigin.Begin);
    //        return text.ToString();
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error extracting text from PDF: {ex.Message}");
    //        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
    //        throw;
    //    }
    //}
    //private Stream ConvertTextToDoc(string text)
    //{
    //    using (var memoryStream = new MemoryStream())
    //    {
    //        using (var docX = DocX.Create(memoryStream))
    //        {
    //            docX.InsertParagraph(text);
    //            docX.Save();
    //        }

    //        // Reset stream position
    //        memoryStream.Seek(0, SeekOrigin.Begin);
    //        return memoryStream;
    //    }
    //}

    //private string ExtractTextFromDoc(Stream docStream)
    //{
    //    StringBuilder text = new StringBuilder();

    //    try
    //    {
    //        using (var wordDocument = WordprocessingDocument.Open(docStream, false))
    //        {
    //            foreach (var paragraph in wordDocument.MainDocumentPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
    //            {
    //                text.AppendLine(paragraph.InnerText);
    //            }
    //        }

    //        // Reset stream position
    //        docStream.Seek(0, SeekOrigin.Begin);
    //        return text.ToString();
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error extracting text from Word document: {ex.Message}");
    //        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
    //        throw;
    //    }
    //}



    private async Task<string> GenerateOpenAIResponse(string userQuery, string content1)       
    {
        _logger.LogInformation(content1);
        using (HttpClient httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiApiKey}");

            var messages = new List<object>
            {
               new { role = "system", content = "You are a helpful assistant that provides information based on the given content ,Note1:First Understand the user content and read your content word by word then give the response,Note2:Some data in the content will not be in structured you want read that type of content also, Avoid blank space in the content. If the information is not found, respond with 'No Content Found'"},

                new { role = "user", content = $"{userQuery}, Search in the Content and give the response" },
                new { role = "assistant", content=content1 }
            };

            var requestBody = new { model = "gpt-3.5-turbo", messages, max_tokens = 4096 }; // Adjust max_tokens as needed

            var response = await httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);

            // Log response status code and content for troubleshooting
            _logger.LogInformation($"Response Status Code: {response.StatusCode}");
            var result = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Response Content: {result}");

            if (!response.IsSuccessStatusCode)
            {
                // Log additional information for troubleshooting
                _logger.LogError($"Request URL: {httpClient.BaseAddress}{httpClient.DefaultRequestHeaders}");
                _logger.LogError($"Request Body: {requestBody}");
            }

            response.EnsureSuccessStatusCode();
            return result.Trim();
        }
    }
}


import PyPDF2
from io import BytesIO

def extract_text_from_pdf(pdf_stream):
    text = ""

    try:
        # Create a PyPDF2 PDF reader object
        pdf_reader = PyPDF2.PdfReader(pdf_stream)

        for page_number in range(pdf_reader.numPages):
            # Get the text from each page using the "extractText" method
            page = pdf_reader.getPage(page_number)
            current_text = page.extractText()
            text += current_text

        # Reset stream position
        pdf_stream.seek(0)

        return text
    except Exception as ex:
        print(f"Error extracting text from PDF: {str(ex)}")
        raise

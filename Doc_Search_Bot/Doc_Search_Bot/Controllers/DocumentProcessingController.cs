using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using Aspose.Pdf.Text;

[ApiController]
[Route("api/documents")]
public class BlobStorageController : ControllerBase
{
    private const string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=genaipocstoragevj;AccountKey=de21VUFgKmt8u0ULDqX+D5EzjLwSw4NI6QLDVtPy1Uj82qNmiqvCnu68swIjSG0ySH8F4XPGkPgC+AStAICusw==;EndpointSuffix=core.windows.net";
    private const string ContainerName = "genaipocdocuments1";

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(ConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(ContainerName);

            await blobContainerClient.CreateIfNotExistsAsync();

            var blobClient = blobContainerClient.GetBlobClient(file.FileName);
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }

            return Ok(new { Message = "File uploaded successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = ex.Message });
        }
    }


    [HttpDelete("delete/{fileName}")]
    public async Task<IActionResult> DeleteFile(string fileName)
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(ConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
            var blobClient = blobContainerClient.GetBlobClient(fileName);

            if (await blobClient.ExistsAsync())
            {
                await blobClient.DeleteIfExistsAsync();
                return Ok(new { Message = "File deleted successfully" });
            }
            else
            {
                return NotFound(new { Error = "File not found" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = ex.Message });
        }
    }

    [HttpGet("list")]
    public IActionResult GetBlobList()
    {
        try
        {
            var blobServiceClient = new BlobServiceClient(ConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(ContainerName);

            var blobs = blobContainerClient.GetBlobs();
            var blobDetails = blobs.Select(blobItem => new
            {
                Name = blobItem.Name,
                Size = blobItem.Properties.ContentLength, // Get the size of the blob
            }).ToList();

            return Ok(blobDetails);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = ex.Message });
        }
    }
}

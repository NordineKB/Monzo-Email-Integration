using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Monzo.Email.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class EbayEmailController : ControllerBase
	{
		private readonly ILogger<EbayEmailController> _logger;

		public EbayEmailController(ILogger<EbayEmailController> logger)
		{
			_logger = logger;
		}

		[HttpPost]
		public async Task<string> ReadStringDataManual()
		{
			var emailEbayFactor = new EbayEmailFactory();

			var orderList = emailEbayFactor.GetOrderInfoFromEbayEmail();

			var updateToken = await Monzo.UpdateToken();
			var token = updateToken.Value;
			var monzo = new MonzoClient(token);
			var getAccount = await monzo.GetAccountsAsync();
			var accountId = getAccount[1].Id;

			var sinceDays = new PaginationOptions { SinceTime = DateTime.UtcNow.AddDays(-7) };
			var transactions = await monzo.GetTransactionsAsync(accountId, "merchant", sinceDays);

			var blobStorage = new BlobStorage();
			var _container = await blobStorage.Create();

			foreach (var transaction in transactions)
			{
				if (transaction.Notes.Contains("#ebay") && !transaction.Notes.Contains("#automated"))
				{
					var findEmail = orderList.Where(x => x.Price == transaction.Amount && x.OrderDate.Date.DayOfYear >= transaction.Created.Date.DayOfYear);

					if (findEmail.Count() > 0)
					{
						var order = findEmail.First();

						// Do Matching
						var newNote = new Dictionary<string, string>() { { "notes", $"{order.Name} \n #ebay #automated" } };

						//upload to azure
						using (var client = new WebClient())
						{
							client.DownloadFile(order.Image, "temp.jpg");
						}

						var uploadedImageURL = "";

						using (FileStream stream = new FileStream("temp.jpg", FileMode.Open, FileAccess.Read))
						{
							var blockBlob = _container.GetBlockBlobReference($"{transaction.Id}-ebay.jpg");
							blockBlob.Properties.ContentType = "image/jpeg";
							stream.Seek(0, SeekOrigin.Begin);
							await blockBlob.UploadFromStreamAsync(stream);
							uploadedImageURL = blockBlob.StorageUri.PrimaryUri.ToString();
						}

						await monzo.CreateAttachmentAsync(transaction.Id, uploadedImageURL, "image/jpeg");
						await monzo.AnnotateTransactionAsync(transaction.Id, newNote);
					}
				}
			}

			return "ok";
		}
	}
}

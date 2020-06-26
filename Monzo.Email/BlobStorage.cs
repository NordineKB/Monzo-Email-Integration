using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Monzo.Email.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace Monzo.Email
{
	public class BlobStorage
	{
		private static string _storageconn = ConfigurationManager.AppSettings["BlobConnection"];
		private static CloudBlobContainer _container;
		public BlobStorage()
		{
			var storageacc = CloudStorageAccount.Parse(_storageconn);
			var blobClient = storageacc.CreateCloudBlobClient();
			_container = blobClient.GetContainerReference("receipts");
		}
		public async Task<CloudBlobContainer> Create()
		{
			await _container.CreateIfNotExistsAsync();
			return _container;
		}

		public async Task<List<Receipt>> ListBlobsSegmentedInFlatListing(CloudBlobContainer container)
		{
			int i = 0;
			BlobContinuationToken continuationToken = null;
			BlobResultSegment resultSegment = null;

			var list = new List<Receipt>();

			do
			{
				resultSegment = await container.ListBlobsSegmentedAsync("", true, BlobListingDetails.All, 10, continuationToken, null, null);
				if (resultSegment.Results.Count<IListBlobItem>() > 0) { Console.WriteLine("Page {0}:", ++i); }
				foreach (var blobItem in resultSegment.Results)
				{
					CloudBlockBlob thing = (CloudBlockBlob)blobItem;
					list.Add(new Receipt() { Name = thing.Name, Link = thing.StorageUri.PrimaryUri.ToString() });
				}

				continuationToken = resultSegment.ContinuationToken;
			}
			while (continuationToken != null);

			return list;
		}
	}
}

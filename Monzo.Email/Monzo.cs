using Monzo.Email.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Monzo.Email
{
	public static class Monzo
	{
		public static async Task<MyAccessToken> UpdateToken()
		{
			var accessToken = await ReadTokenBlob();
			var monzoAuth = new MonzoAuthorizationClient("#clientId#", "#clientSecret#");
			var ExpiryDate = accessToken.TokenDate.Value.AddSeconds(accessToken.ExpiresIn);

			var accessTokenConv = new AccessToken();
			MyAccessToken myAccessToken = accessToken;

			if (DateTime.Now > ExpiryDate)
			{
				accessTokenConv = await monzoAuth.RefreshAccessTokenAsync(accessToken.RefreshToken);
				myAccessToken = new MyAccessToken()
				{
					ClientId = accessTokenConv.ClientId,
					ExpiresIn = accessTokenConv.ExpiresIn,
					RefreshToken = accessTokenConv.RefreshToken,
					TokenDate = DateTime.Now,
					TokenType = accessTokenConv.TokenType,
					UserId = accessTokenConv.UserId,
					Value = accessTokenConv.Value
				};
				await WriteTokenBlob(myAccessToken);
			}

			return myAccessToken;
		}

		public static bool ContainsKeyValue(IDictionary<string, int> dictionary,
							 string expectedKey, int expectedValue)
		{
			int actualValue;
			return dictionary.TryGetValue(expectedKey, out actualValue) &&
				   actualValue == expectedValue;
		}

		public static async Task<MyAccessToken> ReadTokenBlob()
		{
			var blobStorage = new BlobStorage();
			var _container = await blobStorage.Create();
			var blockBlob = _container.GetBlockBlobReference("monzo.json");
			var ordersJson = await blockBlob.DownloadTextAsync();

			var listOfOrders = JsonConvert.DeserializeObject<MyAccessToken>(ordersJson);
			return listOfOrders;
		}

		public static async Task WriteTokenBlob(MyAccessToken orderList)
		{
			var blobStorage = new BlobStorage();
			var _container = await blobStorage.Create();
			var blockBlob = _container.GetBlockBlobReference("monzo.json");
			string ordersJson = JsonConvert.SerializeObject(orderList);

			await blockBlob.UploadTextAsync(ordersJson);
		}
	}
}

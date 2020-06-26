using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using Monzo.Email.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;

namespace Monzo.Email
{

	public class EbayEmailFactory
	{
		public List<Order> GetOrderInfoFromEbayEmail()
		{
			var orderlist = new List<Order>();

			using (var client = new ImapClient())
			{
				using (var cancel = new CancellationTokenSource())
				{
					client.Connect("outlook.office365.com", 993, true, cancel.Token);
					client.Authenticate(ConfigurationManager.AppSettings["EmailUsername"], ConfigurationManager.AppSettings["EmailPassword"]);

					var inbox = client.Inbox;
					inbox.Open(FolderAccess.ReadWrite, cancel.Token);


					var query = SearchQuery.DeliveredAfter(DateTime.Parse("2020-01-12"))
						.And(SearchQuery.SubjectContains("Order confirmed"))
						.And(SearchQuery.NotSeen);

					var uids = new List<UniqueId>();

					foreach (var uid in inbox.Search(query, cancel.Token))
					{
						var message = inbox.GetMessage(uid, cancel.Token);
						var order = DirtyEbayOrderEmailExtract(message);
						orderlist.Add(order);
						uids.Add(uid);
					}

					inbox.AddFlags(uids, MessageFlags.Seen, true);

					client.Disconnect(true, cancel.Token);
				}
			}

			return orderlist;
		}

		public Order DirtyEbayOrderEmailExtract(MimeMessage message)
		{
			var htmlbody = message.HtmlBody;

			var reducedString = htmlbody;

			var substringStart = htmlbody.IndexOf("https://i.ebayimg.com/");
			if (substringStart == -1)
			{
				substringStart = htmlbody.IndexOf("http://i.ebayimg.com/");
			}
			var substring = htmlbody.Substring(substringStart);
			var substringEnd = substring.IndexOf("Money Back Guarantee");
			reducedString = substring.Substring(0, substringEnd);


			var orderDate = message.Date;

			var imageUrl = "";

			var urlbegin = reducedString.IndexOf("https://i.ebayimg.com/");
			if (urlbegin == -1)
			{
				urlbegin = reducedString.IndexOf("http://i.ebayimg.com/");
			}
			var urlend = reducedString.Substring(urlbegin).IndexOf(".jpg");
			if (urlend == -1)
			{
				urlend = reducedString.Substring(urlbegin).IndexOf(".JPG");
			}
			if (urlend == -1)
			{
				urlend = reducedString.Substring(urlbegin).IndexOf(".PNG");
			}
			if (urlend == -1)
			{
				urlend = reducedString.Substring(urlbegin).IndexOf(".png");
			}
			imageUrl = reducedString.Substring(urlbegin, urlend + 4);

			reducedString = reducedString.Substring(urlbegin);

			var titlebegin = reducedString.IndexOf("alt=\"");
			reducedString = reducedString.Substring(titlebegin + 5);
			var titleend = reducedString.IndexOf("\"");
			var title = reducedString.Substring(0, titleend);

			var pricebegin = reducedString.IndexOf("<b>Total</b>: £");
			reducedString = reducedString.Substring(pricebegin);
			var priceend = reducedString.IndexOf(" </p>");
			var price = reducedString.Substring(15, priceend - 15);

			var priceInPenny = (int)(float.Parse(price) * 100);

			return new Order
			{
				Name = title,
				Image = imageUrl,
				OrderDate = orderDate.DateTime,
				Price = -priceInPenny
			};
		}
	}
}

//
// Program.cs
//
// Author:
//       Eddy Zavaleta <eddy@mictlanix.com>
//
// Copyright (c) 2013 Eddy Zavaleta, Mictlanix, and contributors.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Mictlanix.BE.Model;
using MBETools.Helpers;

namespace MBETools
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			//log4net.Config.XmlConfigurator.Configure ();
			IConfigurationSource source = ConfigurationManager.GetSection ("activeRecord") as IConfigurationSource;
			ActiveRecordStarter.Initialize (typeof(Product).Assembly, source);

			FixCFDs ();
		}

		static void FixCFDs ()
		{
			var query = from x in FiscalDocument.Queryable
						select x;

			foreach (var entity in query.ToList ()) {
				if (!entity.IsCompleted || entity.Version >= 3m)
					continue;

				var item = FiscalDocumentXml.TryFind (entity.Id);

				if (item != null)
					continue;

				var doc = CFDHelpers.SignCFD (entity);

				if (entity.IssuerDigitalSeal != doc.sello) {
					Console.WriteLine ("=======SHIT=======");
					Console.WriteLine ("Id: " + entity.Id);
					Console.WriteLine ("Entity's OriginalString:");
					Console.WriteLine (entity.OriginalString);
					Console.WriteLine ("CFD:");
					Console.WriteLine (doc);
					Console.WriteLine ("==================");
					break;
				}

				item = new FiscalDocumentXml {
					Id = entity.Id,
					Data = doc.ToXmlString ()
				};

				using (var scope = new TransactionScope()) {
					item.CreateAndFlush ();
				}

				Console.WriteLine ("Xml: {0}", item.Id);
			};

			Console.WriteLine ("Done!");
		}
	}
}

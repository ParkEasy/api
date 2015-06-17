using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ParkEasyAPI.Models;

namespace ParkEasyAPI.Parser
{
	public static class PriceParser
	{
		// GETHOURANDDAYPRICE
		// extracts a format like "5,00 € / 15,00 €"
		private static PriceModel GetHourAndDayPrice(string input)
		{
			PriceModel model = new PriceModel();

			// split in half
			string[] halfs = input.Split ('/');

			string pricePerHourStr = halfs [0]
				.Replace ("€", "")
				.Replace ("EUR", "")
				.Replace (",", ".")
				.Trim ();

			string pricePerDayStr = halfs [1]
				.Replace ("€", "")
				.Replace ("EUR", "")
				.Replace (",", ".")
				.Trim ();

			// check for format "2,50 € / Tag"
			if (halfs [1].ToLower ().Contains ("tag")) 
			{
				model.FullDay = Convert.ToDouble (pricePerHourStr);
			} 
			else 
			{
				model.PerHour = new PerHourModel (1, Convert.ToDouble (pricePerHourStr));
				model.FullDay = Convert.ToDouble (pricePerDayStr);
			}

			return model;
		}

		// STRIPALLALPHAS
		// removes all alpha characters from a string
		private static double StripAllAlphas(string input)
		{
			string value = Regex.Replace(input.Replace(",", "."), "[A-Za-züöäÖÄÜß€/]", "").Trim();
			return Convert.ToDouble (value);
		}

		// GETSUBTRINGBYSTRING
		// GetSubstringByString("(", ")", "User name (sales)") -> "sales"
		private static string GetSubstringByString(string a, string b, string c)
		{
			return c.Substring((c.IndexOf(a) + a.Length), (c.IndexOf(b) - c.IndexOf(a) - a.Length));
		}

		// PARSE
		// parses a whole set of price inputs
		public static PriceModel Parse(string input)
		{
			PriceModel model = new PriceModel ();

			if (string.IsNullOrEmpty (input) || 
				string.IsNullOrWhiteSpace (input) || 
				input.Length <= 1) 
			{
				return null;
			}

			// check for "5,00€" or "1,50€ pro Stunde"
			if (input.Count (f => f == '€') == 1 && !input.Contains ("/") && input.Length < 20) 
			{
				model.PerHour = new PerHourModel (1, StripAllAlphas(input));
				return model;
			}

			// check for format "1,70 € / 15,00 €"
			if (input.Contains ("/") && input.Length < 20) 
			{
				return GetHourAndDayPrice (input);
			}

			// check for tiered and special hours
			else 
			{
				// check for "2€ für Nichtkunden; 1€ für Kunden"
				if (input.Contains (";")) 
				{
					string[] semihalfs = input.Split (';');
					foreach (string half in semihalfs) 
					{
						if (half.IndexOf ("€") >= 0) 
						{
							model.PerHour = new PerHourModel (1, StripAllAlphas(half));
							return model;
						}
					}
				}

				// is this a multi liner?
				if (input.Contains("\n")) 
				{
					string[] lines = input.Split ('\n');

					// check for "5 € / 15 €" in first line
					if (lines.First ().Contains ("/") && 
						lines.First ().Count (f => f == '€') == 2 && 
						lines.First ().Length < 20) 
					{
						return GetHourAndDayPrice (lines.First());
					}

					// check for "je angefangene stunde 1.70€"
					if (lines.First ().ToLower ().Contains ("je angefangene stunde")) 
					{
						model.PerHour = new PerHourModel(1, StripAllAlphas(input));
						return model;
					}

					// loop all lines
					foreach (string line in lines) 
					{
						// check for "1,80€ bis 1,90€ / Stunde"
						if (line.Contains ("/ Stunde") || line.Contains("jede weitere angefangene Stunde")) 
						{
							string[] hours = line.Split ('/');
							string value = hours [0];

							if (value.Contains ("bis")) 
							{
								value = value.Substring (0, value.IndexOf ("bis"));
							}

							model.PerHour = new PerHourModel(1, StripAllAlphas(value));
						}

						// check for "14€ / Tag"
						if (line.Contains ("/ Tag") || line.Contains("Tagesmax.")) 
						{
							string[] days = line.Replace("Tagesmax.", "").Split ('/');

							model.FullDay = Convert.ToDouble(StripAllAlphas(days [0]));
						}

						// check for a tiered model
						if (line.Contains ("1. angefangene Stunde")) 
						{
							TierModel tier = new TierModel ();
							tier.PerHour = true;
							tier.Price = Convert.ToDouble(StripAllAlphas(line.Replace("1. angefangene Stunde", "")));
							tier.From = 0.0;
							tier.To = 1.0;

							if (model.Tiered == null) {
								model.Tiered = new List<TierModel> ();
							}

							model.Tiered.Add (tier);
						}

						// extract minutes tie
						if (line.Contains (" min. ")) 
						{
							string frame = line.Substring(0, line.IndexOf(" min. "));
							string price = line.Substring (line.IndexOf ("min.") + 4, line.Length - 5 - line.IndexOf ("min."));

							TierModel tier = new TierModel ();

							if (frame.Contains ("-")) 
							{
								string[] times = frame.Split ('-');
								double start = Convert.ToDouble (times[0]);
								double stop = Convert.ToDouble (times[1]);

								tier.From = start / 60.0;
								tier.To = stop / 60.0;
							}

							tier.PerHour = true;
							tier.Price = Convert.ToDouble (StripAllAlphas(price));

							if (model.Tiered == null) {
								model.Tiered = new List<TierModel> ();
							}

							model.Tiered.Add (tier);
						}

						// special prices at certain hours
						if (line.Contains ("Nachttarif")) 
						{
							string timeframe = GetSubstringByString ("(", ")", line).Replace("Uhr", "").Trim();
							string[] times = timeframe.Split ('-');

							TierModel specialtier = new TierModel ();
							specialtier.PerHour = false;
							specialtier.From = Convert.ToInt32 (times[0].Replace(".", ""));
							specialtier.To = Convert.ToInt32 (times[1].Replace(".", ""));
							specialtier.Price = Convert.ToDouble (StripAllAlphas (line.Split (':') [1]));

							model.SpecialHours = specialtier;
						}
					}

					// only return if values are set
					if (model.FullDay.HasValue || model.PerHour != null) 
					{
						return model;
					}
				} 
				else 
				{
					// check for "je stunde zahlen sie 1,00 Euro das Tagesmaximum liegt bei 20 Euro"
					if(input.Contains("Stunde") && input.Contains("Tages"))
					{
						int idx;
						if (input.Contains (", ")) 
						{
							idx = input.IndexOf (", ");
						} 
						else 
						{
							idx = input.IndexOf ("Tages");
						}

						string hour = input.Substring (0, idx).Replace(", ", "");
						string day = input.Substring (idx, input.Length - idx).Replace(", ", "");

						model.PerHour = new PerHourModel (1, StripAllAlphas (hour));
						model.FullDay = StripAllAlphas (day);
						return model;
					}
				}
			}

			return null;
		}

		// INTERPRET
		// interprets the parking wish of a user per pricemodel
		public static double Interpret(PriceModel pricing, double parkingtime)
		{
			double price = 0.0;

			bool specialHoursMatches = false;

			// check for special hours
			if(pricing.SpecialHours != null)
			{		
				// check if the parking time, from current datetime
				// onwords is inclosed in a spezial parking range
				int start = Convert.ToInt32(string.Format("{0:HHmm}", DateTime.Now));
				int stop = Convert.ToInt32(string.Format("{0:HHmm}", DateTime.Now.AddHours (parkingtime)));

				double specialDuration = Math.Abs((pricing.SpecialHours.To - pricing.SpecialHours.From) % 2400.0) / 100.0;
				bool isParkingTimeWithingSpecialHours = false;

				// is special hours spanning two days?
				if (pricing.SpecialHours.To - pricing.SpecialHours.From < 0) 
				{
					isParkingTimeWithingSpecialHours = (start >= (int)pricing.SpecialHours.From || stop <= (int)pricing.SpecialHours.To);
				} 
				else 
				{
					isParkingTimeWithingSpecialHours = (start >= (int)pricing.SpecialHours.From && stop <= (int)pricing.SpecialHours.To);
				}

				// is the parking start time within the special hours?
				if (isParkingTimeWithingSpecialHours) 
				{
					double timegapToSpecialStart = (pricing.SpecialHours.From - (double)start) / 100.0;
					double timeInSpecial = specialDuration - timegapToSpecialStart;
					parkingtime -= timeInSpecial;

					// apply special time on hour or general basis
					if(pricing.SpecialHours.PerHour == true)
					{
						price += pricing.SpecialHours.Price * timeInSpecial;
					}
					else
					{
						price += pricing.SpecialHours.Price;
					}

					specialHoursMatches = true;
				}
			}

			// check for tiered prices first
			if (pricing.Tiered != null && specialHoursMatches == false) 
			{
				// sort by "from" in order to apply the prices in the correct order
				pricing.Tiered.Sort (delegate(TierModel x, TierModel y) 
				{
					if(x.From > y.From) return 1;
					else if(x.From < y.From) return -1;
					else return 0;
				});

				// loop special tiers
				foreach (TierModel tier in pricing.Tiered) 
				{
					double elapsed = Math.Round (tier.To) - Math.Round (tier.From);

					// use the tier
					if (parkingtime >= elapsed) 
					{
						// decrease parking time by tier amount
						parkingtime -= elapsed;

						// add the tier price to overall parking time
						price += tier.Price;
					}
				}
			}

			double hours = parkingtime % 24.0;
			int days = (int)Math.Floor(parkingtime / 24.0);

			// => DAYS

			// if full day price is available add 
			// the amount per parking days
			if (pricing.FullDay.HasValue) 
			{
				// if there is no PerHour price, the full amount
				// of the first day has to be charged
				if (pricing.PerHour == null && days == 0) 
				{
					price += pricing.FullDay.Value * 1;
				} 
				else 
				{
					price += pricing.FullDay.Value * days;
				}
			} 

			// there is no fullday price
			else 
			{
				// if there are multiple days, charge the per hour price at all days
				if(days > 0) 
				{
					price += pricing.PerHour.Apply (days * 24);
				}
			}

			// => HOURS

			// apply hourly price
			if (pricing.PerHour != null) 
			{
				price += pricing.PerHour.Apply (hours);
			}

			return price;
		}
	}
}


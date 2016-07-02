﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HDT.Plugins.EndGame.Models;
using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Utility.Logging;

using HDTDeck = Hearthstone_Deck_Tracker.Hearthstone.Deck;

namespace HDT.Plugins.EndGame.Services
{
	public class TrackerRepository : ITrackerRepository
	{
		public List<ArchetypeDeck> GetAllArchetypeDecks()
		{
			var decks = DeckList.Instance.Decks
				.Where(d => d.TagList.ToLowerInvariant().Contains("archetype"))
				.ToList();
			var archetypes = new List<ArchetypeDeck>();
			foreach (var d in decks)
			{
				// get the newest version of the deck
				var v = d.VersionsIncludingSelf.OrderByDescending(x => x).FirstOrDefault();
				Log.Info($"Selecting version {v} of {d.Name}");
				d.SelectVersion(v);
				if (d == null)
					continue;
				var ad = new ArchetypeDeck(d.Name, KlassKonverter.FromString(d.Class), d.StandardViable);
				ad.Cards = d.Cards
					.Select(x => new Models.Card(x.Id, x.LocalizedName, x.Count, x.Background.Clone()))
					.ToList();
				archetypes.Add(ad);
			}
			return archetypes;
		}

		public Models.Deck GetOpponentDeck()
		{
			var deck = new Models.Deck();
			if (Core.Game.IsRunning)
			{
				var game = Core.Game.CurrentGameStats;
				if (game != null && game.CanGetOpponentDeck)
				{
					Log.Info("oppoent deck available");
					// hero class
					deck.Klass = KlassKonverter.FromString(game.OpponentHero);
					// standard viable, use temp HDT deck
					var hdtDeck = new Hearthstone_Deck_Tracker.Hearthstone.Deck();
					foreach (var card in game.OpponentCards)
					{
						var c = Database.GetCardFromId(card.Id);
						c.Count = card.Count;
						hdtDeck.Cards.Add(c);
						if (c != null && c != Database.UnknownCard)
						{
							deck.Cards.Add(
								new Models.Card(c.Id, c.LocalizedName, c.Count, c.Background.Clone()));
						}
					}
					deck.IsStandard = hdtDeck.StandardViable;
				}
				else
				{
					// TODO Remove
					deck.Klass = KlassKonverter.FromString(Core.Game.Opponent.Class);
					var tempDeck = new Hearthstone_Deck_Tracker.Hearthstone.Deck();
					tempDeck.Cards = new ObservableCollection<Hearthstone_Deck_Tracker.Hearthstone.Card>(Core.Game.Opponent.OpponentCardList);
					deck.IsStandard = tempDeck.StandardViable;
					deck.Cards = GetLiveDeck();
				}
			}
			return deck;
		}

		public string GetGameNote()
		{
			if (Core.Game.IsRunning && Core.Game.CurrentGameStats != null)
			{
				return Core.Game.CurrentGameStats.Note;
			}
			return null;
		}

		public void UpdateGameNote(string text)
		{
			if (Core.Game.IsRunning && Core.Game.CurrentGameStats != null)
			{
				Core.Game.CurrentGameStats.Note = text;
			}
		}

		private List<Models.Card> GetLiveDeck()
		{
			Log.Info("using live deck");
			var live = Core.Game.Opponent.OpponentCardList; // includes created etc.
			return live.Select(x => new Models.Card(x.Id, x.LocalizedName, x.Count, x.Background.Clone())).ToList();
		}

		public void AddDeck(HDTDeck deck)
		{
			DeckList.Instance.Decks.Add(deck);
		}

		public void AddDeck(Models.Deck deck)
		{
		}
	}
}
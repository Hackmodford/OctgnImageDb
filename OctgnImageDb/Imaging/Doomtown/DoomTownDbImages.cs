﻿using System;
using System.Linq;
using System.Net;
using System.Web.Helpers;
using OctgnImageDb.Imaging.Cache;
using OctgnImageDb.Models;
using OctgnImageDb.Octgn;

namespace OctgnImageDb.Imaging.Doomtown
{
    [ImageProvider("Doomtown-Reloaded")]
    public class DoomtownDbImages : IImageProvider
    {
        private const string ApiBaseUrl = "http://dtdb.co";
        private readonly ImageWriter _imageWriter;
        private readonly ImageCache _cache;

        public DoomtownDbImages(ImageWriter imageWriter, ImageCache cache)
        {
            _imageWriter = imageWriter;
            _cache = cache;
        }

        public void GetCardImages(Game game)
        {
            var wc = new WebClient();

            dynamic apiSets = Json.Decode(wc.DownloadString(ApiBaseUrl + "/api/sets/"));

            foreach (var apiSet in apiSets)
            {
                string setName = apiSet.name;
                var set = game.Sets.FindSetByName(setName);

                if (set == null || !set.ImagesNeeded)
                    continue;

                dynamic apiCards = Json.Decode(wc.DownloadString(ApiBaseUrl + "/api/set/" + apiSet.code));

                foreach (var apiCard in apiCards)
                {
                    var card =
                        set.Cards.FirstOrDefault(
                            c => c.Name.Equals(apiCard.title.ToString(), StringComparison.CurrentCultureIgnoreCase));

                    if (card != null && apiCard.imagesrc != string.Empty)
                    {
                        byte[] image = _cache.GetImage(card.Id);

                        if (image == null)
                        {
                            image = wc.DownloadData(ApiBaseUrl + apiCard.imagesrc);
                            _cache.SaveImage(card.Id, ".jpg", image);
                        }

                        _imageWriter.WriteImage(OctgnPaths.CardImagePath(game.Id, set.Id, card.Id, ".jpg"), image);
                    }
                }
            }
        }
    }
}
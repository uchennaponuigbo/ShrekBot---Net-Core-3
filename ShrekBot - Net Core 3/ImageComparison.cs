using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using FileTypeChecker;
using FileTypeChecker.Extensions;
using FileTypeChecker.Types;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Json;

namespace ShrekBot //change this namespace when copied over
{
    internal enum Media
    {
        None = 0,
        Image = 1,
        Video = 2
    }

    internal struct MediaDetails
    {
        internal ulong Hash { get; }
        internal string DiscordMessageLinkIds { get; }

        public MediaDetails(ulong h, string dmli)
        {
            Hash = h;
            DiscordMessageLinkIds = dmli;
            timestamp_created = "";
        }
        /// <summary>
        /// Retrieved from Database
        /// </summary>
        internal string timestamp_created { get; }

        public override string ToString()
        {
            return $"Hash Value: {Hash} | {DiscordMessageLinkIds} | {timestamp_created}";
        }
    }
    internal class ImageComparison
    {
        private ConcurrentDictionary<ulong, string> _abominations; //key is the hash, value is the hash.ToString()
        private ConcurrentDictionary<ulong, string> _exempt; //hashes that are also not to be put in database
        private DifferenceHash _differenceHash;

        private double _HammingDistanceTolerance = 90.0; //Anything less than this value is assumed not to be the abomination.
        private const ulong _KirbeeDiffHash = 17361654623738299008;

        internal ImageComparison()
        {          
            _abominations = new ConcurrentDictionary<ulong, string>();
            _exempt = new ConcurrentDictionary<ulong, string>();
            _differenceHash = new DifferenceHash();

            FileTypeValidator.RegisterCustomTypes(typeof(CFT_Mov).Assembly);
            
            //variants of the abomination hash that I will check in case Drake does anything to circumvent
            //and this list will be updated in case he does stuff
            //_abominations.GetOrAdd(kirbeeDiffHash, kirbeeDiffHash.ToString());
            _abominations.GetOrAdd(12792664875200447104, 12792664875200447104.ToString()); //kirbeeBright.png
            _abominations.GetOrAdd(12729610064922443904, 12729610064922443904.ToString()); //kirbeeDimmed.png
            _abominations.GetOrAdd(7521223104841945222, 7521223104841945222.ToString()); //kirbeeLeft.png
            _abominations.GetOrAdd(11454614242821208168, 11454614242821208168.ToString()); //kirbeeRight.png
            _abominations.GetOrAdd(1052981330386119282, 1052981330386119282.ToString()); //kirbeeUpsideDown.png
            _abominations.GetOrAdd(1052981330386119218, 1052981330386119218.ToString()); //kirbeeUpsideDownDimmed.png
            _abominations.GetOrAdd(12729610065190879872, 12729610065190879872.ToString()); //kirbee_blurred.jpg
            _abominations.GetOrAdd(17357129016430085256, 17357129016430085256.ToString()); //kirbeeForest.jpg
            _abominations.GetOrAdd(12792664600322540160, 12792664600322540160.ToString()); //kirbeeSprings.jpg

            _exempt.GetOrAdd(6324954117335690628, "ddd.jpg"); 
        }

        /// <summary>
        /// The Stream is Disposed here
        /// </summary>
        /// <param name="stream"></param>
        internal MediaDetails Process(ref Stream stream, ulong discordUserId, string messageLink)
        {
            ulong imageHash = DifferenceHash(stream);
            if (IsThisExemptHash(imageHash))
            {
                if (IsThisVariantOfAbomination(imageHash))
                {
                    double hammed = HammingDistancePercent(imageHash);
                    if (IsInHammingDistance(hammed))
                    {
                        return new MediaDetails(imageHash, messageLink);

                    }
                }
            }
            return new MediaDetails();
        }

        internal MediaDetails Process(ref Image<Rgba32> imageSource, ulong discordUserId, string messageLink)
        {
            ulong imageHash = DifferenceHash(ref imageSource);
            if (IsThisExemptHash(imageHash))
            {
                if (IsThisVariantOfAbomination(imageHash))
                {
                    double hammed = HammingDistancePercent(imageHash);
                    if (IsInHammingDistance(hammed))
                    {
                        return new MediaDetails(imageHash, messageLink);
                    }
                }
            }
            return new MediaDetails();
        }

        internal void AdjustImageDetectionTolerance(double newValue)
        {
            if (newValue <= 20.0 || newValue > 100.0)
                return;
            _HammingDistanceTolerance = newValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hashedPercentage"></param>
        /// <returns> Returns true if the percentage is greater than the internal value. A higher percentage closely matches the original image</returns>
        internal bool IsInHammingDistance(double hashedPercentage)
        {
            return hashedPercentage >= _HammingDistanceTolerance;
        }
        internal int AddAbominationVariant(ulong diffHashVariant, ref Modules.Database.SwampDB dapper)
        {
            if(!IsThisVariantOfAbomination(diffHashVariant))
            {
                _abominations.GetOrAdd(diffHashVariant, diffHashVariant.ToString());
                //delete hash from Image Database, if it exists
                return dapper.DeleteAbominationVariantFrom_Images(diffHashVariant);
            }
            return 0;
        }

        /// <summary>
        /// Does this hash exist within the list of abomination hashes
        /// </summary>
        /// <param name="abominationHash"></param>
        /// <returns></returns>
        internal bool IsThisVariantOfAbomination(ulong abominationHash)
            => _abominations.ContainsKey(abominationHash);

        /// <summary>
        /// Does this hash exist within the list of exempt hashes
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        internal bool IsThisExemptHash(ulong hash)
            => _exempt.ContainsKey(hash);
              
        //hmm...
        internal double HammingDistancePercent(ulong diffHash1, ulong diffHash2)
            => Math.Round(CompareHash.Similarity(diffHash1, diffHash2), 2);

        internal double HammingDistancePercent(ulong diffHash)
            => Math.Round(CompareHash.Similarity(_KirbeeDiffHash, diffHash), 2);

        //may need to change parameter to Discord attachment then to Image<Rgba32>
        //found out how to convert attachment to Stream, so I did this
        internal ulong DifferenceHash(Stream streamFromAttachmentURL)
        {
            streamFromAttachmentURL.Position = 0;
            ulong hash = 0;
            using (Image<Rgba32> imageSource = Image.Load<Rgba32>(streamFromAttachmentURL))
                hash = _differenceHash.Hash(imageSource);
            return hash;
        }

        internal ulong DifferenceHash(ref Image<Rgba32> imageSource)
        {
            return _differenceHash.Hash(imageSource);
        }

        internal ulong VideoHash()
        {
            return 0;
        }


        /// <summary>
        /// Excludes GIFs
        /// </summary>
        /// <param name="image"></param>
        /// <returns><c>Media.Image</c> if true, else <c>Media.None</c></returns>
        internal Media IsAttachmentAnImageType(Stream image)
        {
            //.jpg, .png, bmp, tiff, heic, webp
            //grabbed internal logic from stream.IsImage() because I don't want to include GIFs
            //https://github.com/AJMitev/FileTypeChecker/blob/master/FileTypeChecker/Extensions/StreamExtensions.cs
            bool x = image.Is<JointPhotographicExpertsGroup>()
                || image.Is<PortableNetworkGraphic>()
            || image.Is<Webp>()
            || image.Is<Bitmap>()    
            || image.Is<TaggedImageFileFormat>();
            if (x)
                return Media.Image;
            return Media.None;
            //return image.IsImage();
            //image.IsImageAsync();
        }

        /// <summary>
        /// Excludes .mkv
        /// </summary>
        /// <param name="video"></param>
        /// <returns><c>Media.Video</c> if true, else <c>Media.None</c></returns>
        internal Media IsAttachmentAVideoType(Stream video)
        {
            //.avi, .mp4, .m4v, .mov
            //not doing mkv because you can't play those files on Discord
            bool y = video.Is<Mp4>() 
                || video.Is<M4V>()
                || video.Is<CFT_Mov>()
                || video.Is<AudioVideoInterleaveVideoFormat>();
            if (y)
                return Media.Video;
            return Media.None;
        }
    }
}
